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
        private EntityQuery m_AllVehiclesQuery = default!;

        private bool m_LastInvisibleVehicles = false;
        private bool m_LastInvisibleBicycles = false;
        private bool m_LastInvisibleMotorcycles = false;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_AllVehiclesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Vehicle>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] VehicleVisibilitySystem initialized.");
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

            // Periodically check every 32 frames or immediately when toggles change
            if (!stateChanged && (m_FrameCounter % 32 != 0))
            {
                return;
            }

            if (m_AllVehiclesQuery.IsEmptyIgnoreFilter)
            {
                m_LastInvisibleVehicles = invVehicles;
                m_LastInvisibleBicycles = invBicycles;
                m_LastInvisibleMotorcycles = invMotorcycles;
                return;
            }

            using var entities = m_AllVehiclesQuery.ToEntityArray(Allocator.TempJob);
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (!EntityManager.Exists(entity) || EntityManager.HasComponent<Deleted>(entity)) continue;

                bool isBicycle = EntityManager.HasComponent<Bicycle>(entity);
                bool isMotorcycle = false;

                if (!isBicycle && EntityManager.HasComponent<PrefabRef>(entity))
                {
                    var prefab = EntityManager.GetComponentData<PrefabRef>(entity).m_Prefab;
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
                                    isMotorcycle = true;
                                }
                            }
                        }
                    }
                }

                bool shouldHide = invVehicles;
                if (!shouldHide)
                {
                    if (isBicycle && invBicycles) shouldHide = true;
                    if (isMotorcycle && invMotorcycles) shouldHide = true;
                }

                bool isCurrentlyHidden = EntityManager.HasComponent<Hidden>(entity);

                if (shouldHide && !isCurrentlyHidden)
                {
                    SetHiddenRecursive(entity, true);
                }
                else if (!shouldHide && isCurrentlyHidden)
                {
                    SetHiddenRecursive(entity, false);
                }
            }

            m_LastInvisibleVehicles = invVehicles;
            m_LastInvisibleBicycles = invBicycles;
            m_LastInvisibleMotorcycles = invMotorcycles;
        }

        private void SetHiddenRecursive(Entity vehicle, bool hide)
        {
            if (hide)
            {
                if (!EntityManager.HasComponent<Hidden>(vehicle))
                {
                    EntityManager.AddComponent<Hidden>(vehicle);
                    EntityManager.AddComponent<BatchesUpdated>(vehicle);
                }
            }
            else
            {
                if (EntityManager.HasComponent<Hidden>(vehicle))
                {
                    EntityManager.RemoveComponent<Hidden>(vehicle);
                    EntityManager.AddComponent<BatchesUpdated>(vehicle);
                }
            }

            // Also hide/unhide child trailers in LayoutElement
            if (EntityManager.HasComponent<LayoutElement>(vehicle))
            {
                var layout = EntityManager.GetBuffer<LayoutElement>(vehicle);
                for (int j = 0; j < layout.Length; j++)
                {
                    var sub = layout[j].m_Vehicle;
                    if (EntityManager.Exists(sub) && !EntityManager.HasComponent<Deleted>(sub))
                    {
                        if (hide && !EntityManager.HasComponent<Hidden>(sub))
                        {
                            EntityManager.AddComponent<Hidden>(sub);
                            EntityManager.AddComponent<BatchesUpdated>(sub);
                        }
                        else if (!hide && EntityManager.HasComponent<Hidden>(sub))
                        {
                            EntityManager.RemoveComponent<Hidden>(sub);
                            EntityManager.AddComponent<BatchesUpdated>(sub);
                        }
                    }
                }
            }

            // Also hide/unhide SubObjects (cargo containers, accessories)
            if (EntityManager.HasComponent<Game.Objects.SubObject>(vehicle))
            {
                var subObjects = EntityManager.GetBuffer<Game.Objects.SubObject>(vehicle);
                for (int j = 0; j < subObjects.Length; j++)
                {
                    var child = subObjects[j].m_SubObject;
                    if (EntityManager.Exists(child) && !EntityManager.HasComponent<Deleted>(child))
                    {
                        if (hide && !EntityManager.HasComponent<Hidden>(child))
                        {
                            EntityManager.AddComponent<Hidden>(child);
                            EntityManager.AddComponent<BatchesUpdated>(child);
                        }
                        else if (!hide && EntityManager.HasComponent<Hidden>(child))
                        {
                            EntityManager.RemoveComponent<Hidden>(child);
                            EntityManager.AddComponent<BatchesUpdated>(child);
                        }
                    }
                }
            }
        }
    }
}
