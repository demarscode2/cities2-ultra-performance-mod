using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Simulation;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace UltraPerformanceMod.Systems
{
    public class AutoMaintenanceSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static AutoMaintenanceSystem? Instance { get; private set; }

        private EntityQuery m_NetConditionQuery = default!;
        private EntityQuery m_LaneConditionQuery = default!;
        private EntityQuery m_ParkQuery = default!;
        private EntityQuery m_MaintenanceRequestQuery = default!;
        private EntityQuery m_RoadMaintenanceVehicleQuery = default!;
        private EntityQuery m_ParkMaintenanceVehicleQuery = default!;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_NetConditionQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadWrite<NetCondition>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_LaneConditionQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadWrite<LaneCondition>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_ParkQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadWrite<Park>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_MaintenanceRequestQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<MaintenanceRequest>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_RoadMaintenanceVehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<RoadMaintenanceVehicle>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_ParkMaintenanceVehicleQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<ParkMaintenanceVehicle>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] AutoMaintenanceSystem initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null) return;

            m_FrameCounter++;
            if (m_FrameCounter % 128 != 0) return;

            if (Mod.Settings.AutoRoadMaintenance)
            {
                PerformRoadMaintenance();
            }

            if (Mod.Settings.AutoParkMaintenance)
            {
                PerformParkMaintenance();
            }
        }

        public void MaintainAllNow()
        {
            PerformRoadMaintenance();
            PerformParkMaintenance();
        }

        public void PerformRoadMaintenanceNow()
        {
            PerformRoadMaintenance();
        }

        public void PerformParkMaintenanceNow()
        {
            PerformParkMaintenance();
        }

        private void PerformRoadMaintenance()
        {
            // 1. Reset wear on all network road segments
            if (!m_NetConditionQuery.IsEmptyIgnoreFilter)
            {
                using var chunks = m_NetConditionQuery.ToArchetypeChunkArray(Allocator.TempJob);
                var netType = GetComponentTypeHandle<NetCondition>(false);

                for (int c = 0; c < chunks.Length; c++)
                {
                    var chunk = chunks[c];
                    var conditions = chunk.GetNativeArray(ref netType);
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        var cond = conditions[i];
                        if (cond.m_Wear.x > 0f || cond.m_Wear.y > 0f)
                        {
                            cond.m_Wear = float2.zero;
                            conditions[i] = cond;
                        }
                    }
                }
            }

            // 2. Reset wear on individual lanes
            if (!m_LaneConditionQuery.IsEmptyIgnoreFilter)
            {
                using var chunks = m_LaneConditionQuery.ToArchetypeChunkArray(Allocator.TempJob);
                var laneType = GetComponentTypeHandle<LaneCondition>(false);

                for (int c = 0; c < chunks.Length; c++)
                {
                    var chunk = chunks[c];
                    var conditions = chunk.GetNativeArray(ref laneType);
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        var cond = conditions[i];
                        if (cond.m_Wear > 0f)
                        {
                            cond.m_Wear = 0f;
                            conditions[i] = cond;
                        }
                    }
                }
            }

            // 3. Purge road maintenance vehicles from the streets
            if (!m_RoadMaintenanceVehicleQuery.IsEmptyIgnoreFilter)
            {
                using var vehicles = m_RoadMaintenanceVehicleQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < vehicles.Length; i++)
                {
                    UltraPerformanceMod.Utils.SafeVehicleDespawner.Despawn(EntityManager, vehicles[i]);
                }
            }

            // 4. Delete lingering road maintenance requests
            DeleteMaintenanceRequests();
        }

        private void PerformParkMaintenance()
        {
            // 1. Restore 100% maintenance on all parks
            if (!m_ParkQuery.IsEmptyIgnoreFilter)
            {
                using var chunks = m_ParkQuery.ToArchetypeChunkArray(Allocator.TempJob);
                var parkType = GetComponentTypeHandle<Park>(false);

                for (int c = 0; c < chunks.Length; c++)
                {
                    var chunk = chunks[c];
                    var parks = chunk.GetNativeArray(ref parkType);
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        var park = parks[i];
                        if (park.m_Maintenance < 1000)
                        {
                            park.m_Maintenance = 1000;
                            parks[i] = park;
                        }
                    }
                }
            }

            // 2. Purge park maintenance vehicles from the streets
            if (!m_ParkMaintenanceVehicleQuery.IsEmptyIgnoreFilter)
            {
                using var vehicles = m_ParkMaintenanceVehicleQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < vehicles.Length; i++)
                {
                    UltraPerformanceMod.Utils.SafeVehicleDespawner.Despawn(EntityManager, vehicles[i]);
                }
            }

            DeleteMaintenanceRequests();
        }

        private void DeleteMaintenanceRequests()
        {
            if (!m_MaintenanceRequestQuery.IsEmptyIgnoreFilter)
            {
                using var requests = m_MaintenanceRequestQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < requests.Length; i++)
                {
                    EntityManager.AddComponent<Deleted>(requests[i]);
                }
            }
        }
    }
}
