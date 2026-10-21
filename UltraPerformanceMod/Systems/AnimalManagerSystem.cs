using Colossal.Logging;
using Game;
using Game.Common;
using Game.Creatures;
using Game.Objects;
using Game.Rendering;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    public enum AnimalHandlingMode
    {
        KeepVanilla = 0,
        Invisible = 1,
        RemoveCompletely = 2
    }

    /// <summary>
    /// Safely manages domestic pets and wild animals.
    /// Domestic pets are hidden via Game.Tools.Hidden to eliminate draw calls and skinning
    /// without breaking citizen GroupCreature/household leashes.
    /// Wild animals (deer, bears, wolves, birds) can be safely purged.
    /// </summary>
    public class AnimalManagerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static AnimalManagerSystem? Instance { get; private set; }

        private EntityQuery m_WildAnimalsQuery = default!;
        private EntityQuery m_VisibleAnimalsQuery = default!;
        private EntityQuery m_VisiblePetsQuery = default!;
        private EntityQuery m_HiddenCreaturesQuery = default!;

        private AnimalHandlingMode m_LastMode = AnimalHandlingMode.KeepVanilla;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            // Wild animals only: has Animal but NOT Pet
            m_WildAnimalsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Animal>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Pet>(), ComponentType.ReadOnly<Deleted>() }
            });

            m_VisibleAnimalsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Animal>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Hidden>(), ComponentType.ReadOnly<Deleted>() }
            });

            m_VisiblePetsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Pet>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Hidden>(), ComponentType.ReadOnly<Deleted>() }
            });

            m_HiddenCreaturesQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any = new ComponentType[] { ComponentType.ReadOnly<Animal>(), ComponentType.ReadOnly<Pet>() },
                All = new ComponentType[] { ComponentType.ReadOnly<Hidden>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] Safe AnimalManagerSystem initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null) return;

            AnimalHandlingMode currentMode = Mod.Settings.AnimalHandling;
            m_FrameCounter++;

            bool modeChanged = (currentMode != m_LastMode);
            if (!modeChanged && (m_FrameCounter % 64 != 0)) return;

            switch (currentMode)
            {
                case AnimalHandlingMode.KeepVanilla:
                    if (modeChanged)
                    {
                        RestoreVisibility();
                    }
                    break;

                case AnimalHandlingMode.Invisible:
                    HideAnimalsAndPets();
                    break;

                case AnimalHandlingMode.RemoveCompletely:
                    // Hide domestic pets so they cost zero render, and purge wild animals
                    HideAnimalsAndPets();
                    PurgeWildAnimals();
                    break;
            }

            m_LastMode = currentMode;
        }

        public void PurgeAllAnimalsNow()
        {
            HideAnimalsAndPets();
            PurgeWildAnimals();
        }

        private void HideAnimalsAndPets()
        {
            if (!m_VisibleAnimalsQuery.IsEmptyIgnoreFilter)
            {
                using var animals = m_VisibleAnimalsQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < animals.Length; i++)
                {
                    HideEntityAndSubObjects(animals[i]);
                }
            }

            if (!m_VisiblePetsQuery.IsEmptyIgnoreFilter)
            {
                using var pets = m_VisiblePetsQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < pets.Length; i++)
                {
                    HideEntityAndSubObjects(pets[i]);
                }
            }
        }

        private void HideEntityAndSubObjects(Entity entity)
        {
            if (!EntityManager.Exists(entity) || EntityManager.HasComponent<Deleted>(entity)) return;

            if (!EntityManager.HasComponent<Hidden>(entity))
            {
                EntityManager.AddComponent<Hidden>(entity);
                EntityManager.AddComponent<BatchesUpdated>(entity);
            }

            if (EntityManager.HasComponent<Game.Objects.SubObject>(entity))
            {
                var subObjects = EntityManager.GetBuffer<Game.Objects.SubObject>(entity);
                for (int j = 0; j < subObjects.Length; j++)
                {
                    var child = subObjects[j].m_SubObject;
                    if (EntityManager.Exists(child) && !EntityManager.HasComponent<Hidden>(child) && !EntityManager.HasComponent<Deleted>(child))
                    {
                        EntityManager.AddComponent<Hidden>(child);
                        EntityManager.AddComponent<BatchesUpdated>(child);
                    }
                }
            }
        }

        private void RestoreVisibility()
        {
            if (!m_HiddenCreaturesQuery.IsEmptyIgnoreFilter)
            {
                using var entities = m_HiddenCreaturesQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    if (EntityManager.Exists(entity) && EntityManager.HasComponent<Hidden>(entity))
                    {
                        EntityManager.RemoveComponent<Hidden>(entity);
                        EntityManager.AddComponent<BatchesUpdated>(entity);

                        if (EntityManager.HasComponent<Game.Objects.SubObject>(entity))
                        {
                            var subObjects = EntityManager.GetBuffer<Game.Objects.SubObject>(entity);
                            for (int j = 0; j < subObjects.Length; j++)
                            {
                                var child = subObjects[j].m_SubObject;
                                if (EntityManager.Exists(child) && EntityManager.HasComponent<Hidden>(child))
                                {
                                    EntityManager.RemoveComponent<Hidden>(child);
                                    EntityManager.AddComponent<BatchesUpdated>(child);
                                }
                            }
                        }
                    }
                }
                log.Info($"[UltraPerformanceMod] Restored visibility for {entities.Length} animals/pets.");
            }
        }

        private void PurgeWildAnimals()
        {
            if (m_WildAnimalsQuery.IsEmptyIgnoreFilter) return;

            using var wildAnimals = m_WildAnimalsQuery.ToEntityArray(Allocator.TempJob);
            int count = 0;

            for (int i = 0; i < wildAnimals.Length; i++)
            {
                var animal = wildAnimals[i];
                if (EntityManager.Exists(animal) && !EntityManager.HasComponent<Deleted>(animal))
                {
                    EntityManager.AddComponent<Deleted>(animal);
                    count++;
                }
            }

            if (count > 0)
            {
                log.Info($"[UltraPerformanceMod] AnimalManagerSystem: Safely purged {count} wild animals from simulation.");
            }
        }
    }
}
