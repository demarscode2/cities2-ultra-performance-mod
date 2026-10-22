using System;
using Colossal.Logging;
using Game;
using Game.Rendering;
using Unity.Entities;
using UnityEngine;
using UltraPerformanceMod.Core;

namespace UltraPerformanceMod.Systems
{
    public enum TreeLodMode
    {
        Vanilla = 0,    // 100% Padrão Nativo do Jogo
        Moderate = 1,   // Moderado (~65% LOD - Árvores no horizonte reduzidas)
        Aggressive = 2, // Agressivo (~40% LOD - Árvores surgem ao aproximar a câmera)
        Extreme = 3     // Extremo (~20% LOD - Culling máximo de folhagem)
    }

    /// <summary>
    /// Experimental LOD Optimizer targeting foliage and distant tree rendering distance.
    /// Operates purely on the rendering pipeline (RenderingSystem & QualitySettings)
    /// without modifying simulation entities, QuadTrees or savegame data.
    /// </summary>
    public class TreeLodOptimizerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static TreeLodOptimizerSystem? Instance { get; private set; }

        private RenderingSystem? m_RenderingSystem;
        private bool m_BaselineCaptured = false;
        private float m_VanillaLevelOfDetail = 1.0f;
        private float m_VanillaLodBias = 1.0f;
        private bool m_VanillaLodCrossFade = true;

        private TreeLodMode m_CurrentMode = TreeLodMode.Vanilla;
        private uint m_FrameCounter = 0;

        public TreeLodMode CurrentMode => m_CurrentMode;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_RenderingSystem = World.GetOrCreateSystemManaged<RenderingSystem>();
            CaptureBaseline();

            log.Info("[UltraPerformanceMod] TreeLodOptimizerSystem initialized (Experimental LOD & Foliage Control).");
        }

        protected override void OnDestroy()
        {
            // Unconditionally restore vanilla settings when system is destroyed
            RestoreBaseline();

            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        private void CaptureBaseline()
        {
            if (m_BaselineCaptured) return;

            try
            {
                if (m_RenderingSystem != null && m_RenderingSystem.levelOfDetail > 0.05f)
                {
                    m_VanillaLevelOfDetail = m_RenderingSystem.levelOfDetail;
                    m_VanillaLodCrossFade = m_RenderingSystem.lodCrossFade;
                    m_VanillaLodBias = QualitySettings.lodBias > 0.05f ? QualitySettings.lodBias : 1.0f;
                    m_BaselineCaptured = true;
                    log.Info($"[UltraPerformanceMod] Tree LOD baseline captured: LevelOfDetail={m_VanillaLevelOfDetail}, LodBias={m_VanillaLodBias}");
                }
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraPerformanceMod] Could not capture baseline LOD: {ex.Message}");
            }
        }

        private void RestoreBaseline()
        {
            try
            {
                if (m_RenderingSystem != null && m_BaselineCaptured)
                {
                    m_RenderingSystem.levelOfDetail = m_VanillaLevelOfDetail;
                    m_RenderingSystem.lodCrossFade = m_VanillaLodCrossFade;
                    QualitySettings.lodBias = m_VanillaLodBias;
                    log.Info("[UltraPerformanceMod] Tree LOD baseline restored.");
                }
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraPerformanceMod] Could not restore baseline LOD: {ex.Message}");
            }
        }

        public void ApplyLodMode(TreeLodMode mode)
        {
            m_CurrentMode = mode;
            EnforceCurrentLod();
        }

        public void EnforceCurrentLod()
        {
            UltraCoreEngine.Instance.TryExecute("TreeLodOptimizerSystem", () =>
            {
                if (!m_BaselineCaptured)
                {
                    CaptureBaseline();
                }

                if (m_RenderingSystem == null) return;

                switch (m_CurrentMode)
                {
                    case TreeLodMode.Vanilla:
                        if (m_BaselineCaptured)
                        {
                            m_RenderingSystem.levelOfDetail = m_VanillaLevelOfDetail;
                            m_RenderingSystem.lodCrossFade = m_VanillaLodCrossFade;
                            QualitySettings.lodBias = m_VanillaLodBias;
                        }
                        break;

                    case TreeLodMode.Moderate:
                        // Moderado: árvores no horizonte distante são reduzidas (~65% LOD)
                        m_RenderingSystem.levelOfDetail = 0.65f;
                        m_RenderingSystem.lodCrossFade = true;
                        QualitySettings.lodBias = 0.70f;
                        break;

                    case TreeLodMode.Aggressive:
                        // Agressivo: árvores surgem apenas conforme a câmera se aproxima (~40% LOD)
                        m_RenderingSystem.levelOfDetail = 0.40f;
                        m_RenderingSystem.lodCrossFade = true;
                        QualitySettings.lodBias = 0.45f;
                        break;

                    case TreeLodMode.Extreme:
                        // Extremo: apenas árvores na proximidade imediata da câmera (~20% LOD)
                        m_RenderingSystem.levelOfDetail = 0.20f;
                        m_RenderingSystem.lodCrossFade = true;
                        QualitySettings.lodBias = 0.25f;
                        break;
                }
            });
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null) return;

            m_FrameCounter++;

            // Ensure baseline is captured after the scene has loaded
            if (!m_BaselineCaptured && m_FrameCounter < 300 && m_FrameCounter % 30 == 0)
            {
                CaptureBaseline();
            }

            // Sync with settings if mode changed externally
            TreeLodMode targetMode = Mod.Settings.TreeLodMode;
            if (targetMode != m_CurrentMode)
            {
                ApplyLodMode(targetMode);
            }
            else if (targetMode != TreeLodMode.Vanilla && m_FrameCounter % 120 == 0)
            {
                // Keep enforced if native graphics options or dynamic scene changes occurred
                EnforceCurrentLod();
            }
        }
    }
}
