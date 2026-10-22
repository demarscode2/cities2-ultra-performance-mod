using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Simulation;
using Game.Vehicles;
using UltraPerformanceMod.Core;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    public class AutoEmergencyServicesSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static AutoEmergencyServicesSystem? Instance { get; private set; }

        private EntityQuery m_SickCitizensQuery = default!;
        private EntityQuery m_AmbulanceRequestsQuery = default!;
        private EntityQuery m_AmbulanceVehiclesQuery = default!;
        private EntityQuery m_CrimeProducerQuery = default!;
        private EntityQuery m_PoliceEmergencyRequestQuery = default!;
        private EntityQuery m_PolicePatrolRequestQuery = default!;
        private EntityQuery m_PoliceCarQuery = default!;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_SickCitizensQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Citizen>(),
                    ComponentType.ReadWrite<HealthProblem>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_AmbulanceRequestsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<HealthcareRequest>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_AmbulanceVehiclesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Ambulance>(),
                    ComponentType.ReadOnly<Vehicle>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_CrimeProducerQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadWrite<CrimeProducer>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_PoliceEmergencyRequestQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<PoliceEmergencyRequest>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_PolicePatrolRequestQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<PolicePatrolRequest>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_PoliceCarQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<PoliceCar>(),
                    ComponentType.ReadOnly<Vehicle>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] High-Performance AutoEmergencyServicesSystem initialized.");
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
            float intervalScale = UltraCoreEngine.Instance.AutoTuner.TaskIntervalScale;
            int scaledInterval = (int)(64 * intervalScale);
            if (m_FrameCounter % System.Math.Max(32, scaledInterval) != 0) return;

            if (Mod.Settings.AutoAmbulance)
            {
                PerformAmbulanceCare();
            }

            if (Mod.Settings.AutoPolice)
            {
                PerformPoliceCare();
            }
        }

        public void HealAllCitizensNow()
        {
            PerformAmbulanceCare();
        }

        public void ClearAllCrimeNow()
        {
            PerformPoliceCare();
        }

        public void HealAndProtectCityNow()
        {
            PerformAmbulanceCare();
            PerformPoliceCare();
        }

        private void PerformAmbulanceCare()
        {
            using (UltraCoreEngine.Instance.Diagnostics.Measure("AmbulanceCare"))
            {
                // 1. Clear sickness and injuries on citizens
                if (!m_SickCitizensQuery.IsEmptyIgnoreFilter)
                {
                    using var chunks = m_SickCitizensQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    var problemType = GetComponentTypeHandle<HealthProblem>(false);

                    for (int c = 0; c < chunks.Length; c++)
                    {
                        var chunk = chunks[c];
                        var problems = chunk.GetNativeArray(ref problemType);

                        for (int i = 0; i < chunk.Count; i++)
                        {
                            var p = problems[i];
                            if ((p.m_Flags & (HealthProblemFlags.Sick | HealthProblemFlags.Injured | HealthProblemFlags.RequireTransport)) != 0)
                            {
                                p.m_Flags &= ~(HealthProblemFlags.Sick | HealthProblemFlags.Injured | HealthProblemFlags.RequireTransport);
                                p.m_HealthcareRequest = Entity.Null;
                                problems[i] = p;
                            }
                        }
                    }
                }

                // 2. Delete lingering ambulance requests (Zero-alloc batch)
                if (!m_AmbulanceRequestsQuery.IsEmptyIgnoreFilter)
                {
                    using var reqChunks = m_AmbulanceRequestsQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    var entityType = GetEntityTypeHandle();
                    var reqType = GetComponentTypeHandle<HealthcareRequest>(true);

                    for (int c = 0; c < reqChunks.Length; c++)
                    {
                        var chunk = reqChunks[c];
                        var entities = chunk.GetNativeArray(entityType);
                        var reqs = chunk.GetNativeArray(ref reqType);

                        for (int i = 0; i < chunk.Count; i++)
                        {
                            if (reqs[i].m_Type == HealthcareRequestType.Ambulance)
                            {
                                EntityManager.AddComponent<Deleted>(entities[i]);
                            }
                        }
                    }
                }

                // 3. Purge active ambulance vehicles
                if (!m_AmbulanceVehiclesQuery.IsEmptyIgnoreFilter)
                {
                    using var chunks = m_AmbulanceVehiclesQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    var entityType = GetEntityTypeHandle();

                    for (int c = 0; c < chunks.Length; c++)
                    {
                        var chunk = chunks[c];
                        var vehicles = chunk.GetNativeArray(entityType);
                        for (int i = 0; i < chunk.Count; i++)
                        {
                            UltraPerformanceMod.Utils.SafeVehicleDespawner.Despawn(EntityManager, vehicles[i]);
                        }
                    }
                }
            }
        }

        private void PerformPoliceCare()
        {
            using (UltraCoreEngine.Instance.Diagnostics.Measure("PoliceCare"))
            {
                // 1. Clear crime on all buildings
                if (!m_CrimeProducerQuery.IsEmptyIgnoreFilter)
                {
                    using var chunks = m_CrimeProducerQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    var crimeType = GetComponentTypeHandle<CrimeProducer>(false);

                    for (int c = 0; c < chunks.Length; c++)
                    {
                        var chunk = chunks[c];
                        var crimes = chunk.GetNativeArray(ref crimeType);
                        for (int i = 0; i < chunk.Count; i++)
                        {
                            var cp = crimes[i];
                            if (cp.m_Crime > 0f || cp.m_PatrolRequest != Entity.Null)
                            {
                                if (cp.m_PatrolRequest != Entity.Null && EntityManager.Exists(cp.m_PatrolRequest))
                                {
                                    EntityManager.AddComponent<Deleted>(cp.m_PatrolRequest);
                                }

                                cp.m_Crime = 0f;
                                cp.m_PatrolRequest = Entity.Null;
                                crimes[i] = cp;
                            }
                        }
                    }
                }

                // 2. Delete police requests (Zero-alloc direct batch)
                if (!m_PoliceEmergencyRequestQuery.IsEmptyIgnoreFilter)
                {
                    EntityManager.AddComponent<Deleted>(m_PoliceEmergencyRequestQuery);
                }

                if (!m_PolicePatrolRequestQuery.IsEmptyIgnoreFilter)
                {
                    EntityManager.AddComponent<Deleted>(m_PolicePatrolRequestQuery);
                }

                // 3. Purge active police cars
                if (!m_PoliceCarQuery.IsEmptyIgnoreFilter)
                {
                    using var chunks = m_PoliceCarQuery.ToArchetypeChunkArray(Allocator.TempJob);
                    var entityType = GetEntityTypeHandle();

                    for (int c = 0; c < chunks.Length; c++)
                    {
                        var chunk = chunks[c];
                        var cars = chunk.GetNativeArray(entityType);
                        for (int i = 0; i < chunk.Count; i++)
                        {
                            UltraPerformanceMod.Utils.SafeVehicleDespawner.Despawn(EntityManager, cars[i]);
                        }
                    }
                }
            }
        }
    }
}
