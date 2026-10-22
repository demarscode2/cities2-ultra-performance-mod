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
                EntityManager.AddComponent<Hidden>(m_VisibleAnimalsQuery);
                EntityManager.AddComponent<BatchesUpdated>(m_VisibleAnimalsQuery);
            }

            if (!m_VisiblePetsQuery.IsEmptyIgnoreFilter)
            {
                EntityManager.AddComponent<Hidden>(m_VisiblePetsQuery);
                EntityManager.AddComponent<BatchesUpdated>(m_VisiblePetsQuery);
            }
        }

        private void RestoreVisibility()
        {
            if (!m_HiddenCreaturesQuery.IsEmptyIgnoreFilter)
            {
                int count = m_HiddenCreaturesQuery.CalculateEntityCount();
                EntityManager.RemoveComponent<Hidden>(m_HiddenCreaturesQuery);
                EntityManager.AddComponent<BatchesUpdated>(m_HiddenCreaturesQuery);
                log.Info($"[UltraPerformanceMod] Restored visibility for {count} animals/pets (Zero-alloc batch).");
            }
        }

        private void PurgeWildAnimals()
        {
            if (m_WildAnimalsQuery.IsEmptyIgnoreFilter) return;

            int count = m_WildAnimalsQuery.CalculateEntityCount();
            if (count > 0)
            {
                EntityManager.AddComponent<Deleted>(m_WildAnimalsQuery);
                log.Info($"[UltraPerformanceMod] AnimalManagerSystem: Safely purged {count} wild animals from simulation (Zero-alloc batch).");
            }
        }
    }
}
