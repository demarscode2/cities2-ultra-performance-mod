using System;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Events;
using Game.Objects;
using Game.Vehicles;
using UltraPerformanceMod.Core;
using UltraPerformanceMod.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace UltraPerformanceMod.Systems
{
    /// <summary>
    /// Accurately detects and clears genuine traffic deadlocks without removing vehicles
    /// that are behaving normally (waiting at traffic lights, pedestrian crossings, bridges,
    /// boarding/disembarking passengers, or loading/unloading cargo).
    /// Accelerated by UltraCore SmartCache and budgeted time-slicing.
    /// </summary>
    public class TrafficDeadlockBreakerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static TrafficDeadlockBreakerSystem? Instance { get; private set; }

        private EntityQuery m_VehiclesOnLaneQuery = default!;
        private EntityQuery m_TrafficAccidentEventQuery = default!;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_VehiclesOnLaneQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Vehicle>(),
                    ComponentType.ReadOnly<CarCurrentLane>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_TrafficAccidentEventQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<TrafficAccident>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] Smart TrafficDeadlockBreakerSystem with UltraCache initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null || !Mod.Settings.AutoClearDeadlocks) return;

            m_FrameCounter++;
            // Check once every 256 frames (~4 seconds) or adapt via AutoTuner
            float intervalScale = UltraCoreEngine.Instance.AutoTuner.TaskIntervalScale;
            int scaledInterval = (int)(256 * intervalScale);
            if (m_FrameCounter % Math.Max(64, scaledInterval) != 0) return;

            ClearDeadlockedVehicles(Mod.Settings.DeadlockTimeoutSeconds);
        }

        public int ClearAllTrafficDeadlocksNow()
        {
            int deadlocks = ClearDeadlockedVehicles(Math.Max(30, Mod.Settings?.DeadlockTimeoutSeconds ?? 60));
            int accidents = ClearAllAccidents();
            return deadlocks + accidents;
        }

        public int ClearAllAccidentsNow()
        {
            return ClearAllAccidents();
        }

        private int ClearDeadlockedVehicles(int timeoutSeconds)
        {
            if (m_VehiclesOnLaneQuery.IsEmptyIgnoreFilter) return 0;

            int deadlocksRemoved = 0;
            using (UltraCoreEngine.Instance.Diagnostics.Measure("TrafficDeadlockBreaker"))
            {
                using var chunks = m_VehiclesOnLaneQuery.ToArchetypeChunkArray(Allocator.TempJob);
                var entityType = GetEntityTypeHandle();

                for (int c = 0; c < chunks.Length; c++)
                {
                    var chunk = chunks[c];
                    var entities = chunk.GetNativeArray(entityType);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        var entity = entities[i];
                        if (!EntityManager.Exists(entity) || EntityManager.HasComponent<Deleted>(entity)) continue;

                        if (IsVehicleActuallyDeadlocked(entity, timeoutSeconds))
                        {
                            SafeVehicleDespawner.Despawn(EntityManager, entity);
                            deadlocksRemoved++;
                        }
                    }
                }
            }

            if (deadlocksRemoved > 0)
            {
                log.Info($"[UltraPerformanceMod] Smart Deadlock Breaker: Cleared {deadlocksRemoved} truly deadlocked/frozen vehicles.");
            }

            return deadlocksRemoved;
        }

        private bool IsVehicleActuallyDeadlocked(Entity entity, int timeoutSeconds)
        {
            // 1. If the vehicle is actively moving, it is NOT deadlocked
            if (EntityManager.HasComponent<Moving>(entity))
            {
                var moving = EntityManager.GetComponentData<Moving>(entity);
                if (math.lengthsq(moving.m_Velocity) > 0.05f)
                {
                    return false;
                }
            }

            if (!EntityManager.HasComponent<CarCurrentLane>(entity)) return false;
            var lane = EntityManager.GetComponentData<CarCurrentLane>(entity);

            // 2. Ignore parking, entering road, queueing, waypoints, transform targets
            if ((lane.m_LaneFlags & (CarLaneFlags.ParkingSpace | CarLaneFlags.EnteringRoad | CarLaneFlags.Queue | CarLaneFlags.TransformTarget)) != 0)
            {
                return false;
            }

            // 3. Ignore vehicles where citizens are boarding or disembarking
            if (IsBoardingOrOperating(entity))
            {
                return false;
            }

            // 4. Inspect Blocker component and trace blocker chain
            if (!EntityManager.HasComponent<Blocker>(entity))
            {
                // Without a blocker, a vehicle is NOT in a deadlock.
                // Only if stranded motionless with an extreme duration (> 240s) without path should it be despawned.
                return lane.m_Duration >= 240f && (lane.m_LaneFlags & CarLaneFlags.EndOfPath) != 0;
            }

            // Check SmartCache for previously evaluated blocker chain
            float3 currentHeadPos = float3.zero;
            if (EntityManager.HasComponent<Transform>(entity))
            {
                currentHeadPos = EntityManager.GetComponentData<Transform>(entity).m_Position;
                if (UltraCoreEngine.Instance.Cache.TryGetBlockerChain(entity, currentHeadPos, out bool isCachedImmune))
                {
                    if (isCachedImmune) return false;
                }
            }

            var blocker = EntityManager.GetComponentData<Blocker>(entity);

            // If directly waiting for normal traffic signals, crossing, bridge limit, yield or caution:
            if (IsNormalBlockerType(blocker.m_Type))
            {
                UltraCoreEngine.Instance.Cache.SetBlockerChain(entity, true, entity, currentHeadPos, 120);
                return false;
            }

            // 5. Trace the blocker chain (up to 16 links) to detect circular deadlocks or normal queueing
            Entity currentEntity = entity;
            Entity currentBlocker = blocker.m_Blocker;
            int steps = 0;

            // Simple fixed-capacity set to detect cycles
            Entity b0 = entity, b1 = Entity.Null, b2 = Entity.Null, b3 = Entity.Null;
            Entity b4 = Entity.Null, b5 = Entity.Null, b6 = Entity.Null, b7 = Entity.Null;

            while (currentBlocker != Entity.Null && steps < 16)
            {
                // Check if blocker loops back (cyclic deadlock: A blocks B blocks ... blocks A)
                if (currentBlocker == b0 || currentBlocker == b1 || currentBlocker == b2 || currentBlocker == b3 ||
                    currentBlocker == b4 || currentBlocker == b5 || currentBlocker == b6 || currentBlocker == b7)
                {
                    // A true circular deadlock detected!
                    // Despawn this vehicle to break the deadlock and allow the entire loop to move!
                    return true;
                }

                if (!EntityManager.Exists(currentBlocker) || EntityManager.HasComponent<Deleted>(currentBlocker))
                {
                    break;
                }

                // If any vehicle in the chain is boarding, loading, or disembarking, the queue is NOT deadlocked!
                if (IsBoardingOrOperating(currentBlocker))
                {
                    UltraCoreEngine.Instance.Cache.SetBlockerChain(entity, true, currentBlocker, currentHeadPos, 90);
                    return false;
                }

                if (!EntityManager.HasComponent<Blocker>(currentBlocker))
                {
                    // The lead vehicle in front is not blocked by anything! It will move.
                    UltraCoreEngine.Instance.Cache.SetBlockerChain(entity, true, currentBlocker, currentHeadPos, 90);
                    return false;
                }

                var nextBlocker = EntityManager.GetComponentData<Blocker>(currentBlocker);
                if (IsNormalBlockerType(nextBlocker.m_Type))
                {
                    // The head of the queue is waiting at a traffic light, crossing, bridge, or merging.
                    // Therefore, ALL vehicles behind it are simply in normal queue! NEVER despawn them!
                    UltraCoreEngine.Instance.Cache.SetBlockerChain(entity, true, currentBlocker, currentHeadPos, 120);
                    return false;
                }

                // Store in cycle detector
                switch (steps)
                {
                    case 0: b1 = currentBlocker; break;
                    case 1: b2 = currentBlocker; break;
                    case 2: b3 = currentBlocker; break;
                    case 3: b4 = currentBlocker; break;
                    case 4: b5 = currentBlocker; break;
                    case 5: b6 = currentBlocker; break;
                    case 6: b7 = currentBlocker; break;
                }

                currentEntity = currentBlocker;
                currentBlocker = nextBlocker.m_Blocker;
                steps++;
            }

            // If the vehicle has been stationary for longer than the timeout AND its duration is very high
            if (lane.m_Duration >= timeoutSeconds && lane.m_Duration >= 90f)
            {
                return true;
            }

            return false;
        }

        private static bool IsNormalBlockerType(BlockerType type)
        {
            return type == BlockerType.Signal ||
                   type == BlockerType.Crossing ||
                   type == BlockerType.Limit ||
                   type == BlockerType.Temporary ||
                   type == BlockerType.Caution ||
                   type == BlockerType.Spawn;
        }

        private bool IsBoardingOrOperating(Entity entity)
        {
            if (EntityManager.HasComponent<PersonalCar>(entity))
            {
                var car = EntityManager.GetComponentData<PersonalCar>(entity);
                if ((car.m_State & (PersonalCarFlags.Boarding | PersonalCarFlags.Disembarking)) != 0)
                {
                    return true;
                }
            }

            if (EntityManager.HasComponent<PublicTransport>(entity))
            {
                var pt = EntityManager.GetComponentData<PublicTransport>(entity);
                if ((pt.m_State & (PublicTransportFlags.Boarding | PublicTransportFlags.Arriving)) != 0)
                {
                    return true;
                }
            }

            if (EntityManager.HasComponent<Taxi>(entity))
            {
                var taxi = EntityManager.GetComponentData<Taxi>(entity);
                if ((taxi.m_State & (TaxiFlags.Boarding | TaxiFlags.Disembarking | TaxiFlags.Requested | TaxiFlags.Arriving)) != 0)
                {
                    return true;
                }
            }

            if (EntityManager.HasComponent<DeliveryTruck>(entity))
            {
                var truck = EntityManager.GetComponentData<DeliveryTruck>(entity);
                if ((truck.m_State & (DeliveryTruckFlags.Buying | DeliveryTruckFlags.Delivering | DeliveryTruckFlags.StorageTransfer | DeliveryTruckFlags.UpkeepDelivery)) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private int ClearAllAccidents()
        {
            if (!m_TrafficAccidentEventQuery.IsEmptyIgnoreFilter)
            {
                int count = m_TrafficAccidentEventQuery.CalculateEntityCount();
                if (count > 0)
                {
                    EntityManager.AddComponent<Deleted>(m_TrafficAccidentEventQuery);
                    log.Info($"[UltraPerformanceMod] Cleared {count} active traffic accident events (Zero-alloc batch).");
                    return count;
                }
            }
            return 0;
        }
    }
}
