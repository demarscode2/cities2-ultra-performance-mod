using System;
using Colossal.Logging;
using Game;
using Game.Settings;

namespace UltraPerformanceMod.Systems
{
    public class VisualEffectsOptimizerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;

        protected override void OnCreate()
        {
            base.OnCreate();
            log.Info("[UltraPerformanceMod] VisualEffectsOptimizerSystem initialized.");
        }

        protected override void OnUpdate()
        {
            // Passive system; settings are applied on change via ApplyEffectsOptimization
        }

        public static void ApplyEffectsOptimization(bool disableHeavyEffects)
        {
            try
            {
                var graphics = SharedSettings.instance?.graphics;
                if (graphics == null) return;

                var clouds = graphics.GetQualitySetting<CloudsQualitySettings>();
                if (clouds != null)
                {
                    if (disableHeavyEffects)
                    {
                        clouds.volumetricCloudsEnabled = false;
                        clouds.volumetricCloudsShadows = false;
                    }
                    else
                    {
                        clouds.volumetricCloudsEnabled = true;
                    }
                    clouds.Apply();
                }

                var fog = graphics.GetQualitySetting<FogQualitySettings>();
                if (fog != null)
                {
                    if (disableHeavyEffects)
                    {
                        fog.SetLevel(QualitySetting.Level.Disabled, apply: true);
                    }
                    else
                    {
                        fog.SetLevel(QualitySetting.Level.Medium, apply: true);
                    }
                }

                var motionBlur = graphics.GetQualitySetting<MotionBlurQualitySettings>();
                if (motionBlur != null)
                {
                    if (disableHeavyEffects)
                    {
                        motionBlur.SetLevel(QualitySetting.Level.Disabled, apply: true);
                    }
                }

                var depthOfField = graphics.GetQualitySetting<DepthOfFieldQualitySettings>();
                if (depthOfField != null)
                {
                    if (disableHeavyEffects)
                    {
                        depthOfField.SetLevel(QualitySetting.Level.Disabled, apply: true);
                    }
                }

                graphics.Apply();
                log.Info($"[UltraPerformanceMod] Heavy effects optimization set to: {disableHeavyEffects}");
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraPerformanceMod] Failed to apply graphics effect settings: {ex.Message}");
            }
        }
    }
}
