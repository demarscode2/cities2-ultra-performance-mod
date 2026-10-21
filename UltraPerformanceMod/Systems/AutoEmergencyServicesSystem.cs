using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Objects;
using Game.Simulation;
using Game.Vehicles;
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

            // Ambulance / Healthcare queries
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
                All = new ComponentType[] { ComponentType.ReadOnly<Ambulance>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            // Police queries
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
                All = new ComponentType[] { ComponentType.ReadOnly<PoliceCar>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] AutoEmergencyServicesSystem initialized.");
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
            if (m_FrameCounter % 64 != 0) return;

            if (Mod.Settings.AutoAmbulance)
            {
                PerformAmbulanceCare();
            }

            if (Mod.Settings.AutoPolice)
            {
                PerformPoliceCare();
            }
        }

        public void HealAndProtectCityNow()
        {
            PerformAmbulanceCare();
            PerformPoliceCare();
        }

        public void PerformAmbulanceCareNow()
        {
            PerformAmbulanceCare();
        }

        public void PerformPoliceCareNow()
        {
            PerformPoliceCare();
        }

        private void PerformAmbulanceCare()
        {
            // 1. Heal all sick/injured citizens waiting for ambulance
            if (!m_SickCitizensQuery.IsEmptyIgnoreFilter)
            {
                using var chunks = m_SickCitizensQuery.ToArchetypeChunkArray(Allocator.TempJob);
                var healthType = GetComponentTypeHandle<HealthProblem>(false);

                for (int c = 0; c < chunks.Length; c++)
                {
                    var chunk = chunks[c];
                    var problems = chunk.GetNativeArray(ref healthType);
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        var p = problems[i];
                        // If citizen is sick or injured (not dead), heal them immediately
                        if ((p.m_Flags & HealthProblemFlags.Dead) == 0 &&
                            ((p.m_Flags & (HealthProblemFlags.Sick | HealthProblemFlags.Injured | HealthProblemFlags.RequireTransport)) != 0))
                        {
                            if (p.m_HealthcareRequest != Entity.Null && EntityManager.Exists(p.m_HealthcareRequest))
                            {
                                EntityManager.AddComponent<Deleted>(p.m_HealthcareRequest);
                            }

                            p.m_Flags &= ~(HealthProblemFlags.Sick | HealthProblemFlags.Injured | HealthProblemFlags.RequireTransport);
                            p.m_HealthcareRequest = Entity.Null;
                            problems[i] = p;
                        }
                    }
                }
            }

            // 2. Delete lingering ambulance requests
            if (!m_AmbulanceRequestsQuery.IsEmptyIgnoreFilter)
            {
                using var reqEntities = m_AmbulanceRequestsQuery.ToEntityArray(Allocator.TempJob);
                using var reqData = m_AmbulanceRequestsQuery.ToComponentDataArray<HealthcareRequest>(Allocator.TempJob);
                for (int i = 0; i < reqEntities.Length; i++)
                {
                    if (reqData[i].m_Type == HealthcareRequestType.Ambulance)
                    {
                        EntityManager.AddComponent<Deleted>(reqEntities[i]);
                    }
                }
            }

            // 3. Purge active ambulance vehicles
            if (!m_AmbulanceVehiclesQuery.IsEmptyIgnoreFilter)
            {
                using var vehicles = m_AmbulanceVehiclesQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < vehicles.Length; i++)
                {
                    UltraPerformanceMod.Utils.SafeVehicleDespawner.Despawn(EntityManager, vehicles[i]);
                }
            }
        }

        private void PerformPoliceCare()
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

            // 2. Delete police requests
            if (!m_PoliceEmergencyRequestQuery.IsEmptyIgnoreFilter)
            {
                using var reqs = m_PoliceEmergencyRequestQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < reqs.Length; i++)
                {
                    EntityManager.AddComponent<Deleted>(reqs[i]);
                }
            }

            if (!m_PolicePatrolRequestQuery.IsEmptyIgnoreFilter)
            {
                using var reqs = m_PolicePatrolRequestQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < reqs.Length; i++)
                {
                    EntityManager.AddComponent<Deleted>(reqs[i]);
                }
            }

            // 3. Purge active police cars
            if (!m_PoliceCarQuery.IsEmptyIgnoreFilter)
            {
                using var cars = m_PoliceCarQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < cars.Length; i++)
                {
                    UltraPerformanceMod.Utils.SafeVehicleDespawner.Despawn(EntityManager, cars[i]);
                }
            }
        }
    }
}
