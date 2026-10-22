using System;
using Colossal.Logging;
using Game;
using Game.Rendering;
using Game.Simulation;
using Unity.Entities;
using UnityEngine.Rendering;

namespace UltraPerformanceMod.Systems
{
    public class TreeManagerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static TreeManagerSystem? Instance { get; private set; }

        private TreeGrowthSystem? m_TreeGrowthSystem;
        private bool m_WasWindDisabled = false;
        private bool m_WasGrowthDisabled = false;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_TreeGrowthSystem = World.GetOrCreateSystemManaged<TreeGrowthSystem>();

            log.Info("[UltraPerformanceMod] TreeManagerSystem initialized (Wind & Growth control).");
        }

        protected override void OnDestroy()
        {
            // Restore default wind and growth behavior on shutdown
            ApplyTreeWindSetting(false);
            if (m_TreeGrowthSystem != null)
            {
                m_TreeGrowthSystem.Enabled = true;
            }

            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null) return;

            m_FrameCounter++;

            bool disableWind = Mod.Settings.DisableTreeWind;
            bool disableGrowth = Mod.Settings.DisableTreeGrowth;

            // 1. Handle Tree Wind
            bool windChanged = (disableWind != m_WasWindDisabled);
            if (windChanged || (disableWind && m_FrameCounter % 120 == 0))
            {
                ApplyTreeWindSetting(disableWind);
                m_WasWindDisabled = disableWind;
            }

            // 2. Handle Tree Growth (Freeze tree aging to save ECS ticks)
            bool growthChanged = (disableGrowth != m_WasGrowthDisabled);
            if (growthChanged)
            {
                if (m_TreeGrowthSystem != null)
                {
                    m_TreeGrowthSystem.Enabled = !disableGrowth;
                    log.Info($"[UltraPerformanceMod] TreeGrowthSystem enabled set to: {!disableGrowth}");
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
