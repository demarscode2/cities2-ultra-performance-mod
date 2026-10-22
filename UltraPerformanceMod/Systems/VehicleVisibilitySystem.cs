using System.Collections.Generic;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    public class VehicleVisibilitySystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;

        private EntityQuery m_VisibleAllVehiclesQuery = default!;
        private EntityQuery m_HiddenAllVehiclesQuery = default!;
        private EntityQuery m_VisibleBicyclesQuery = default!;
        private EntityQuery m_HiddenBicyclesQuery = default!;

        private readonly HashSet<Entity> m_MotorcyclePrefabs = new();
        private bool m_LastInvisibleVehicles = false;
        private bool m_LastInvisibleBicycles = false;
        private bool m_LastInvisibleMotorcycles = false;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_VisibleAllVehiclesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Vehicle>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Hidden>(), ComponentType.ReadOnly<Deleted>() }
            });

            m_HiddenAllVehiclesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Vehicle>(), ComponentType.ReadOnly<Hidden>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_VisibleBicyclesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Vehicle>(), ComponentType.ReadOnly<Bicycle>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Hidden>(), ComponentType.ReadOnly<Deleted>() }
            });

            m_HiddenBicyclesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Vehicle>(), ComponentType.ReadOnly<Bicycle>(), ComponentType.ReadOnly<Hidden>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] High-Performance Zero-Alloc VehicleVisibilitySystem initialized.");
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null) return;

            bool invVehicles = Mod.Settings.InvisibleVehicles;
            bool invBicycles = Mod.Settings.InvisibleBicycles;
            bool invMotorcycles = Mod.Settings.InvisibleMotorcycles;

            m_FrameCounter++;
            bool stateChanged = (invVehicles != m_LastInvisibleVehicles ||
                                 invBicycles != m_LastInvisibleBicycles ||
                                 invMotorcycles != m_LastInvisibleMotorcycles);

            // Throttle to every 32 frames unless toggles changed
            if (!stateChanged && (m_FrameCounter % 32 != 0))
            {
                return;
            }

            // Case 1: Hide all vehicles completely (Fastest path: 100% zero-allocation direct ECS batch)
            if (invVehicles)
            {
                if (!m_VisibleAllVehiclesQuery.IsEmptyIgnoreFilter)
                {
                    EntityManager.AddComponent<Hidden>(m_VisibleAllVehiclesQuery);
                    EntityManager.AddComponent<BatchesUpdated>(m_VisibleAllVehiclesQuery);
                }
            }
            // Case 2: Vehicles were hidden and now restored completely
            else if (m_LastInvisibleVehicles && !invVehicles && !invBicycles && !invMotorcycles)
            {
                if (!m_HiddenAllVehiclesQuery.IsEmptyIgnoreFilter)
                {
                    EntityManager.RemoveComponent<Hidden>(m_HiddenAllVehiclesQuery);
                    EntityManager.AddComponent<BatchesUpdated>(m_HiddenAllVehiclesQuery);
                }
            }
            // Case 3: Selective hiding (bicycles / motorcycles)
            else
            {
                // Bicycles handling via dedicated zero-allocation queries
                if (invBicycles)
                {
                    if (!m_VisibleBicyclesQuery.IsEmptyIgnoreFilter)
                    {
                        EntityManager.AddComponent<Hidden>(m_VisibleBicyclesQuery);
                        EntityManager.AddComponent<BatchesUpdated>(m_VisibleBicyclesQuery);
                    }
                }
                else if (m_LastInvisibleBicycles && !invBicycles)
                {
                    if (!m_HiddenBicyclesQuery.IsEmptyIgnoreFilter)
                    {
                        EntityManager.RemoveComponent<Hidden>(m_HiddenBicyclesQuery);
                        EntityManager.AddComponent<BatchesUpdated>(m_HiddenBicyclesQuery);
                    }
                }

                // Motorcycles handling (uses prefab classification cache)
                if (invMotorcycles || (m_LastInvisibleMotorcycles && !invMotorcycles))
                {
                    ProcessMotorcycles(invMotorcycles);
                }
            }

            m_LastInvisibleVehicles = invVehicles;
            m_LastInvisibleBicycles = invBicycles;
            m_LastInvisibleMotorcycles = invMotorcycles;
        }

        private void ProcessMotorcycles(bool hide)
        {
            var query = hide ? m_VisibleAllVehiclesQuery : m_HiddenAllVehiclesQuery;
            if (query.IsEmptyIgnoreFilter) return;

            // Iterate chunks without allocating all entities into a single TempJob array
            using var chunks = query.ToArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetEntityTypeHandle();
            var prefabType = GetComponentTypeHandle<PrefabRef>(true);

            for (int c = 0; c < chunks.Length; c++)
            {
                var chunk = chunks[c];
                if (!chunk.Has(ref prefabType)) continue;

                var entities = chunk.GetNativeArray(entityType);
                var prefabs = chunk.GetNativeArray(ref prefabType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var prefab = prefabs[i].m_Prefab;
                    if (IsMotorcyclePrefab(prefab))
                    {
                        var entity = entities[i];
                        if (hide)
                        {
                            EntityManager.AddComponent<Hidden>(entity);
                            EntityManager.AddComponent<BatchesUpdated>(entity);
                        }
                        else
                        {
                            EntityManager.RemoveComponent<Hidden>(entity);
                            EntityManager.AddComponent<BatchesUpdated>(entity);
                        }
                    }
                }
            }
        }

        private bool IsMotorcyclePrefab(Entity prefab)
        {
            if (m_MotorcyclePrefabs.Contains(prefab)) return true;

            if (EntityManager.HasComponent<CarData>(prefab))
            {
                var carData = EntityManager.GetComponentData<CarData>(prefab);
                if (carData.m_SizeClass == SizeClass.Small)
                {
                    if (EntityManager.HasComponent<PersonalCarData>(prefab))
                    {
                        var pData = EntityManager.GetComponentData<PersonalCarData>(prefab);
                        if (pData.m_PassengerCapacity <= 2)
                        {
                            m_MotorcyclePrefabs.Add(prefab);
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
