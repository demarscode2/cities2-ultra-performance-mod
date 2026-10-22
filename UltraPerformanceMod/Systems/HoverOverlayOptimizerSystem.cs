using System;
using Colossal.Logging;
using Game;
using Game.Tools;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UltraPerformanceMod.Systems
{
    /// <summary>
    /// Removes the blue/cyan hover highlight overlay on buildings, citizens, vehicles, and props.
    /// Disables HDRP OutlinesWorldUIPass (saving offscreen buffer and fullscreen blit passes)
    /// and strips Highlighted component from hovered entities in ECS.
    /// </summary>
    public class HoverOverlayOptimizerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static HoverOverlayOptimizerSystem? Instance { get; private set; }

        private EntityQuery m_HighlightedQuery = default!;
        private uint m_FrameCounter = 0;
        private bool m_WasDisabled = false;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_HighlightedQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Highlighted>() }
            });

            log.Info("[UltraPerformanceMod] HoverOverlayOptimizerSystem initialized.");
        }

        protected override void OnDestroy()
        {
            SetOutlinesPassEnabled(true);
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null) return;

            bool disableHover = Mod.Settings.DisableHoverOverlay;
            m_FrameCounter++;

            if (disableHover != m_WasDisabled)
            {
                SetOutlinesPassEnabled(!disableHover);
                m_WasDisabled = disableHover;
            }

            if (disableHover)
            {
                // 1. Remove Highlighted from any entity that received it
                if (!m_HighlightedQuery.IsEmptyIgnoreFilter)
                {
                    EntityManager.RemoveComponent<Highlighted>(m_HighlightedQuery);
                }

                // 2. Periodic check for newly spawned CustomPassVolumes
                if (m_FrameCounter % 300 == 0)
                {
                    SetOutlinesPassEnabled(false);
                }
            }
        }

        public void SetOutlinesPassEnabled(bool enabled)
        {
            try
            {
                var volumes = UnityEngine.Object.FindObjectsOfType<CustomPassVolume>();
                foreach (var vol in volumes)
                {
                    if (vol != null && vol.customPasses != null)
                    {
                        foreach (var pass in vol.customPasses)
                        {
                            if (pass != null && (pass.GetType().Name.Contains("OutlinesWorldUI") || pass.name.Contains("OutlinesWorldUI")))
                            {
                                pass.enabled = enabled;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraPerformanceMod] Error setting OutlinesWorldUIPass enabled to {enabled}: {ex.Message}");
            }
        }
    }
}
