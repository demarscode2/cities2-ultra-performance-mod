using System;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Notifications;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    /// <summary>
    /// Safely hides floating 3D notification warning icons using Game.Tools.Hidden,
    /// avoiding SRP compute buffer crashes in NotificationIconRenderSystem.
    /// </summary>
    public class NotificationIconOptimizerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static NotificationIconOptimizerSystem? Instance { get; private set; }

        private EntityQuery m_VisibleIconsQuery = default!;
        private EntityQuery m_HiddenIconsQuery = default!;
        private bool m_WasHidden = false;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_VisibleIconsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Icon>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Hidden>(), ComponentType.ReadOnly<Deleted>() }
            });

            m_HiddenIconsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Icon>(), ComponentType.ReadOnly<Hidden>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] Safe NotificationIconOptimizerSystem initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null) return;

            bool hideIcons = Mod.Settings.HideNotificationIcons;
            m_FrameCounter++;

            bool stateChanged = (hideIcons != m_WasHidden);
            if (!stateChanged && (m_FrameCounter % 64 != 0)) return;

            if (hideIcons)
            {
                if (!m_VisibleIconsQuery.IsEmptyIgnoreFilter)
                {
                    using var icons = m_VisibleIconsQuery.ToEntityArray(Allocator.TempJob);
                    if (icons.Length > 0)
                    {
                        EntityManager.AddComponent<Hidden>(icons);
                    }
                }
            }
            else if (stateChanged && !hideIcons)
            {
                if (!m_HiddenIconsQuery.IsEmptyIgnoreFilter)
                {
                    using var hiddenIcons = m_HiddenIconsQuery.ToEntityArray(Allocator.TempJob);
                    if (hiddenIcons.Length > 0)
                    {
                        EntityManager.RemoveComponent<Hidden>(hiddenIcons);
                        log.Info($"[UltraPerformanceMod] Restored visibility for {hiddenIcons.Length} notification icons.");
                    }
                }
            }

            m_WasHidden = hideIcons;
        }
    }
}
