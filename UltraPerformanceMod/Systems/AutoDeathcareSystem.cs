using Colossal.Logging;
using Game;
using Game.Citizens;
using Game.Common;
using Game.Simulation;
using Game.Vehicles;
using UltraPerformanceMod.Core;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    public class AutoDeathcareSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static AutoDeathcareSystem? Instance { get; private set; }

        private EntityQuery m_DeadCitizensQuery = default!;
        private EntityQuery m_HearseRequestsQuery = default!;
        private EntityQuery m_HearseVehiclesQuery = default!;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_DeadCitizensQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Citizen>(),
                    ComponentType.ReadOnly<HealthProblem>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_HearseRequestsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<HealthcareRequest>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_HearseVehiclesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Hearse>(),
                    ComponentType.ReadOnly<Vehicle>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] High-Performance AutoDeathcareSystem initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null || !Mod.Settings.AutoDeathcare) return;

            m_FrameCounter++;
            float intervalScale = UltraCoreEngine.Instance.AutoTuner.TaskIntervalScale;
            int scaledInterval = (int)(64 * intervalScale);
            if (m_FrameCounter % System.Math.Max(32, scaledInterval) != 0) return;

            CollectDeceased();
            PurgeActiveHearses();
        }

        public int CollectAllDeceasedNow()
        {
            int collected = CollectDeceased();
            PurgeActiveHearses();
            return collected;
        }

        private int CollectDeceased()
        {
            if (m_DeadCitizensQuery.IsEmptyIgnoreFilter) return 0;

            int collectedCount = 0;
            using (UltraCoreEngine.Instance.Diagnostics.Measure("AutoDeathcare"))
            {
                using var chunks = m_DeadCitizensQuery.ToArchetypeChunkArray(Allocator.TempJob);
                var entityType = GetEntityTypeHandle();
                var healthProblemType = GetComponentTypeHandle<HealthProblem>(true);

                for (int c = 0; c < chunks.Length; c++)
                {
                    var chunk = chunks[c];
                    var entities = chunk.GetNativeArray(entityType);
                    var problems = chunk.GetNativeArray(ref healthProblemType);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        var problem = problems[i];
                        if ((problem.m_Flags & HealthProblemFlags.Dead) != 0)
                        {
                            Entity citizen = entities[i];
                            if (problem.m_HealthcareRequest != Entity.Null && EntityManager.Exists(problem.m_HealthcareRequest))
                            {
                                EntityManager.AddComponent<Deleted>(problem.m_HealthcareRequest);
                            }

                            EntityManager.AddComponent<Deleted>(citizen);
                            collectedCount++;
                        }
                    }
                }

                // Also clean up any lingering hearse service requests whose citizen was already deleted
                if (!m_HearseRequestsQuery.IsEmptyIgnoreFilter)
                {
                    using var reqChunks = m_HearseRequestsQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    var reqEntityType = GetEntityTypeHandle();
                    var reqType = GetComponentTypeHandle<HealthcareRequest>(true);

                    for (int c = 0; c < reqChunks.Length; c++)
                    {
                        var chunk = reqChunks[c];
                        var entities = chunk.GetNativeArray(reqEntityType);
                        var reqs = chunk.GetNativeArray(ref reqType);

                        for (int i = 0; i < chunk.Count; i++)
                        {
                            if (reqs[i].m_Type == HealthcareRequestType.Hearse)
                            {
                                if (reqs[i].m_Citizen == Entity.Null || !EntityManager.Exists(reqs[i].m_Citizen))
                                {
                                    EntityManager.AddComponent<Deleted>(entities[i]);
                                }
                            }
                        }
                    }
                }
            }

            return collectedCount;
        }

        private void PurgeActiveHearses()
        {
            if (m_HearseVehiclesQuery.IsEmptyIgnoreFilter) return;

            int count = 0;
            using var chunks = m_HearseVehiclesQuery.ToArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetEntityTypeHandle();

            for (int c = 0; c < chunks.Length; c++)
            {
                var chunk = chunks[c];
                var hearses = chunk.GetNativeArray(entityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    UltraPerformanceMod.Utils.SafeVehicleDespawner.Despawn(EntityManager, hearses[i]);
                    count++;
                }
            }

            if (count > 0)
            {
                log.Info($"[UltraPerformanceMod] Purged {count} active hearses from streets (Chunk-based).");
            }
        }
    }
}
