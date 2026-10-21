using Colossal.Logging;
using Game;
using Game.Common;
using Game.Simulation;
using Game.Vehicles;
using UltraPerformanceMod.Utils;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    public class OutsideTaxiLimiterSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static OutsideTaxiLimiterSystem? Instance { get; private set; }

        private EntityQuery m_TaxiQuery = default!;
        private EntityQuery m_TaxiRequestQuery = default!;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_TaxiQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Taxi>(),
                    ComponentType.ReadOnly<Owner>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_TaxiRequestQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<TaxiRequest>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] OutsideTaxiLimiterSystem initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null || !Mod.Settings.DisableOutsideTaxis) return;

            m_FrameCounter++;
            if (m_FrameCounter % 64 != 0) return;

            PurgeOutsideTaxis();
        }

        public int PurgeOutsideTaxisNow()
        {
            return PurgeOutsideTaxis();
        }

        private int PurgeOutsideTaxis()
        {
            int purgedCount = 0;

            if (!m_TaxiQuery.IsEmptyIgnoreFilter)
            {
                using var entities = m_TaxiQuery.ToEntityArray(Allocator.TempJob);
                using var owners = m_TaxiQuery.ToComponentDataArray<Owner>(Allocator.TempJob);

                for (int i = 0; i < entities.Length; i++)
                {
                    Entity owner = owners[i].m_Owner;
                    if (owner != Entity.Null && EntityManager.Exists(owner))
                    {
                        if (EntityManager.HasComponent<Game.Objects.OutsideConnection>(owner) ||
                            EntityManager.HasComponent<Game.Net.OutsideConnection>(owner))
                        {
                            SafeVehicleDespawner.Despawn(EntityManager, entities[i]);
                            purgedCount++;
                        }
                    }
                }
            }

            if (!m_TaxiRequestQuery.IsEmptyIgnoreFilter)
            {
                using var reqEntities = m_TaxiRequestQuery.ToEntityArray(Allocator.TempJob);
                using var reqs = m_TaxiRequestQuery.ToComponentDataArray<TaxiRequest>(Allocator.TempJob);

                for (int i = 0; i < reqEntities.Length; i++)
                {
                    Entity seeker = reqs[i].m_Seeker;
                    if (seeker != Entity.Null && EntityManager.Exists(seeker))
                    {
                        if (EntityManager.HasComponent<Game.Objects.OutsideConnection>(seeker) ||
                            EntityManager.HasComponent<Game.Net.OutsideConnection>(seeker))
                        {
                            EntityManager.AddComponent<Deleted>(reqEntities[i]);
                            purgedCount++;
                        }
                    }
                }
            }

            if (purgedCount > 0)
            {
                log.Info($"[UltraPerformanceMod] Purged {purgedCount} outside connection taxis / requests safely.");
            }

            return purgedCount;
        }
    }
}
