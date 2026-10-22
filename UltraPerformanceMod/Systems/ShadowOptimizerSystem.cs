using System;
using Colossal.Logging;
using Game;
using Game.Settings;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UltraPerformanceMod.Systems
{
    /// <summary>
    /// Optimizes shadow rendering in megalopolises:
    /// - Caps excessive 1500m+ shadow render distance to a crisp, high-density 450m via HDRP HDShadowSettings.
    /// - Eliminates terrain shadow casting (which re-renders terrain tiles into shadow maps on large maps).
    /// - Raises shadow culling threshold to prevent thousands of micro-props from generating shadow draw calls.
    /// </summary>
    public class ShadowOptimizerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static ShadowOptimizerSystem? Instance { get; private set; }

        private bool m_WasOptimized = false;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            log.Info("[UltraPerformanceMod] ShadowOptimizerSystem initialized.");
        }

        protected override void OnDestroy()
        {
            ApplyShadowOptimization(false);
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null) return;

            m_FrameCounter++;
            bool optimizeShadows = Mod.Settings.OptimizeShadows;

            if (optimizeShadows != m_WasOptimized || (optimizeShadows && m_FrameCounter % 300 == 0))
            {
                ApplyShadowOptimization(optimizeShadows);
                m_WasOptimized = optimizeShadows;
            }
        }

        public static void ApplyShadowOptimization(bool optimize)
        {
            try
            {
                // 1. Quality Settings: micro-props & terrain shadow culling
                var graphics = SharedSettings.instance?.graphics;
                if (graphics != null)
                {
                    var shadows = graphics.GetQualitySetting<ShadowsQualitySettings>();
                    if (shadows != null)
                    {
                        if (optimize)
                        {
                            shadows.terrainCastShadows = false;
                            shadows.shadowCullingThresholdVolume = 0.5f;
                            shadows.shadowCullingThresholdHeight = 0.5f;
                        }
                        else
                        {
                            shadows.terrainCastShadows = true;
                            shadows.shadowCullingThresholdVolume = 0.05f;
                            shadows.shadowCullingThresholdHeight = 0.05f;
                        }
                    }
                }

                // 2. HDRP Volume Stack: cap max shadow distance
                var stack = VolumeManager.instance?.stack;
                if (stack != null)
                {
                    var hdShadow = stack.GetComponent<HDShadowSettings>();
                    if (hdShadow != null)
                    {
                        if (optimize)
                        {
                            hdShadow.maxShadowDistance.value = 450f;
                            hdShadow.maxShadowDistance.overrideState = true;
                        }
                        else
                        {
                            hdShadow.maxShadowDistance.overrideState = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraPerformanceMod] Error in ApplyShadowOptimization: {ex.Message}");
            }
        }
    }
}
