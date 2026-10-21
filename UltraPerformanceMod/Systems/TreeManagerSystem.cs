using System;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Objects;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Rendering;

namespace UltraPerformanceMod.Systems
{
    public class TreeManagerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static TreeManagerSystem? Instance { get; private set; }

        private EntityQuery m_VisibleTreesQuery = default!;
        private EntityQuery m_HiddenTreesQuery = default!;
        private TreeGrowthSystem? m_TreeGrowthSystem;

        private bool m_WasInvisible = false;
        private bool m_WasWindDisabled = false;
        private bool m_WasGrowthDisabled = false;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_VisibleTreesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Tree>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Hidden>(), ComponentType.ReadOnly<Deleted>() }
            });

            m_HiddenTreesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Tree>(), ComponentType.ReadOnly<Hidden>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_TreeGrowthSystem = World.GetOrCreateSystemManaged<TreeGrowthSystem>();

            log.Info("[UltraPerformanceMod] TreeManagerSystem initialized.");
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

            bool invisibleTrees = Mod.Settings.InvisibleTrees;
            bool disableWind = Mod.Settings.DisableTreeWind;
            bool disableGrowth = Mod.Settings.DisableTreeGrowth;

            // 1. Handle Tree Visibility
            bool visibilityChanged = (invisibleTrees != m_WasInvisible);
            if (visibilityChanged || (invisibleTrees && m_FrameCounter % 64 == 0))
            {
                if (invisibleTrees)
                {
                    if (!m_VisibleTreesQuery.IsEmptyIgnoreFilter)
                    {
                        using var trees = m_VisibleTreesQuery.ToEntityArray(Allocator.TempJob);
                        if (trees.Length > 0)
                        {
                            EntityManager.AddComponent<Hidden>(trees);
                            EntityManager.AddComponent<BatchesUpdated>(trees);
                        }
                    }
                }
                else if (visibilityChanged && !invisibleTrees)
                {
                    if (!m_HiddenTreesQuery.IsEmptyIgnoreFilter)
                    {
                        using var hiddenTrees = m_HiddenTreesQuery.ToEntityArray(Allocator.TempJob);
                        if (hiddenTrees.Length > 0)
                        {
                            EntityManager.RemoveComponent<Hidden>(hiddenTrees);
                            EntityManager.AddComponent<BatchesUpdated>(hiddenTrees);
                            log.Info($"[UltraPerformanceMod] Restored visibility for {hiddenTrees.Length} trees.");
                        }
                    }
                }
                m_WasInvisible = invisibleTrees;
            }

            // 2. Handle Tree Wind
            bool windChanged = (disableWind != m_WasWindDisabled);
            if (windChanged || (disableWind && m_FrameCounter % 120 == 0))
            {
                ApplyTreeWindSetting(disableWind);
                m_WasWindDisabled = disableWind;
            }

            // 3. Handle Tree Growth
            bool growthChanged = (disableGrowth != m_WasGrowthDisabled);
            if (growthChanged)
            {
                if (m_TreeGrowthSystem != null)
                {
                    m_TreeGrowthSystem.Enabled = !disableGrowth;
                    log.Info($"[UltraPerformanceMod] TreeGrowthSystem enabled state set to: {!disableGrowth}");
                }
                m_WasGrowthDisabled = disableGrowth;
            }
        }

        private void ApplyTreeWindSetting(bool disableWind)
        {
            try
            {
                var stack = VolumeManager.instance?.stack;
                if (stack == null) return;

                var windVolume = stack.GetComponent<WindVolumeComponent>();
                if (windVolume != null)
                {
                    if (disableWind)
                    {
                        windVolume.windTreeBaseStrength.value = 0f;
                        windVolume.windTreeBaseStrength.overrideState = true;

                        windVolume.windTreeGustStrength.value = 0f;
                        windVolume.windTreeGustStrength.overrideState = true;

                        windVolume.windTreeFlutterStrength.value = 0f;
                        windVolume.windTreeFlutterStrength.overrideState = true;

                        windVolume.windTreeFlutterGustStrength.value = 0f;
                        windVolume.windTreeFlutterGustStrength.overrideState = true;
                    }
                    else
                    {
                        windVolume.windTreeBaseStrength.overrideState = false;
                        windVolume.windTreeGustStrength.overrideState = false;
                        windVolume.windTreeFlutterStrength.overrideState = false;
                        windVolume.windTreeFlutterGustStrength.overrideState = false;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraPerformanceMod] Error applying tree wind setting: {ex.Message}");
            }
        }
    }
}
