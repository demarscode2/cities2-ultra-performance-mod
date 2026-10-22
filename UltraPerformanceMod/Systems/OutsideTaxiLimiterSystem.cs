using Colossal.Logging;
using Game;
using Game.Common;
using Game.Simulation;
using Game.Vehicles;
using UltraPerformanceMod.Core;
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

            log.Info("[UltraPerformanceMod] High-Performance OutsideTaxiLimiterSystem initialized.");
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
            float intervalScale = UltraCoreEngine.Instance.AutoTuner.TaskIntervalScale;
            int scaledInterval = (int)(128 * intervalScale);
            if (m_FrameCounter % System.Math.Max(32, scaledInterval) != 0) return;

            PurgeOutsideTaxis();
        }

        public int PurgeOutsideTaxisNow()
        {
            return PurgeOutsideTaxis();
        }

        private int PurgeOutsideTaxis()
        {
            int purgedCount = 0;

            using (UltraCoreEngine.Instance.Diagnostics.Measure("OutsideTaxiLimiter"))
            {
                if (!m_TaxiQuery.IsEmptyIgnoreFilter)
                {
                    using var chunks = m_TaxiQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    var entityType = GetEntityTypeHandle();
                    var ownerType = GetComponentTypeHandle<Owner>(true);

                    for (int c = 0; c < chunks.Length; c++)
                    {
                        var chunk = chunks[c];
                        var entities = chunk.GetNativeArray(entityType);
                        var owners = chunk.GetNativeArray(ref ownerType);

                        for (int i = 0; i < chunk.Count; i++)
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
                }

                if (!m_TaxiRequestQuery.IsEmptyIgnoreFilter)
                {
                    using var chunks = m_TaxiRequestQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    var entityType = GetEntityTypeHandle();
                    var reqType = GetComponentTypeHandle<TaxiRequest>(true);

                    for (int c = 0; c < chunks.Length; c++)
                    {
                        var chunk = chunks[c];
                        var entities = chunk.GetNativeArray(entityType);
                        var reqs = chunk.GetNativeArray(ref reqType);

                        for (int i = 0; i < chunk.Count; i++)
                        {
                            Entity seeker = reqs[i].m_Seeker;
                            if (seeker != Entity.Null && EntityManager.Exists(seeker))
                            {
                                if (EntityManager.HasComponent<Game.Objects.OutsideConnection>(seeker) ||
                                    EntityManager.HasComponent<Game.Net.OutsideConnection>(seeker))
                                {
                                    EntityManager.AddComponent<Deleted>(entities[i]);
                                    purgedCount++;
                                }
                            }
                        }
                    }
                }
            }

            if (purgedCount > 0)
            {
                log.Info($"[UltraPerformanceMod] Purged {purgedCount} outside connection taxis / requests safely (Chunk-based).");
            }

            return purgedCount;
        }
    }
}
