using Colossal.Logging;
using Game;
using Game.Common;
using Game.Vehicles;
using UltraPerformanceMod.Core;
using UltraPerformanceMod.Utils;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    public class DummyTrafficRemoverSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static DummyTrafficRemoverSystem? Instance { get; private set; }

        private EntityQuery m_PersonalCarQuery = default!;
        private EntityQuery m_DeliveryTruckQuery = default!;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_PersonalCarQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<PersonalCar>(),
                    ComponentType.ReadOnly<Vehicle>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_DeliveryTruckQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<DeliveryTruck>(),
                    ComponentType.ReadOnly<Vehicle>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] High-Performance DummyTrafficRemoverSystem initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null || !Mod.Settings.DisableDummyTraffic) return;

            m_FrameCounter++;
            float intervalScale = UltraCoreEngine.Instance.AutoTuner.TaskIntervalScale;
            int scaledInterval = (int)(64 * intervalScale);
            if (m_FrameCounter % System.Math.Max(32, scaledInterval) != 0) return;

            PurgeDummyTraffic();
        }

        public int PurgeDummyTrafficNow()
        {
            return PurgeDummyTraffic();
        }

        private int PurgeDummyTraffic()
        {
            int purged = 0;

            using (UltraCoreEngine.Instance.Diagnostics.Measure("DummyTrafficRemover"))
            {
                if (!m_PersonalCarQuery.IsEmptyIgnoreFilter)
                {
                    using var chunks = m_PersonalCarQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    var entityType = GetEntityTypeHandle();
                    var carType = GetComponentTypeHandle<PersonalCar>(true);

                    for (int c = 0; c < chunks.Length; c++)
                    {
                        var chunk = chunks[c];
                        var entities = chunk.GetNativeArray(entityType);
                        var cars = chunk.GetNativeArray(ref carType);

                        for (int i = 0; i < chunk.Count; i++)
                        {
                            if ((cars[i].m_State & PersonalCarFlags.DummyTraffic) != 0)
                            {
                                SafeVehicleDespawner.Despawn(EntityManager, entities[i]);
                                purged++;
                            }
                        }
                    }
                }

                if (!m_DeliveryTruckQuery.IsEmptyIgnoreFilter)
                {
                    using var chunks = m_DeliveryTruckQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    var entityType = GetEntityTypeHandle();
                    var truckType = GetComponentTypeHandle<DeliveryTruck>(true);

                    for (int c = 0; c < chunks.Length; c++)
                    {
                        var chunk = chunks[c];
                        var entities = chunk.GetNativeArray(entityType);
                        var trucks = chunk.GetNativeArray(ref truckType);

                        for (int i = 0; i < chunk.Count; i++)
                        {
                            if ((trucks[i].m_State & DeliveryTruckFlags.DummyTraffic) != 0)
                            {
                                SafeVehicleDespawner.Despawn(EntityManager, entities[i]);
                                purged++;
                            }
                        }
                    }
                }
            }

            if (purged > 0)
            {
                log.Info($"[UltraPerformanceMod] Purged {purged} dummy vehicles safely (Chunk-based).");
            }

            return purged;
        }
    }
}
