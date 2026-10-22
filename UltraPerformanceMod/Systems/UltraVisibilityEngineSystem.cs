using System;
using Colossal.Logging;
using Game;
using Game.Rendering;
using Unity.Entities;
using UnityEngine;
using UltraPerformanceMod.Core;

namespace UltraPerformanceMod.Systems
{
    public enum UltraVisibilityMode
    {
        Vanilla = 0,    // 100% Padrão Nativo do Jogo
        Balanced = 1,   // Otimizado (~70% LOD, Memória Rigorosa, FarClip Suave)
        Aggressive = 2, // Agressivo (~45% LOD, Árvores/Props Cortados Longe, FarClip Dinâmico)
        Extreme = 3     // Extremo (~25% LOD, Culling Máximo, FarClip 2500m-3500m, Capping de Luzes)
    }

    /// <summary>
    /// Ultra Visibility Engine (UVE)
    /// Advanced visual culling and rendering optimization system for Cities: Skylines II.
    /// Operates purely on the graphics and rendering pipeline (RenderingSystem, BatchMeshSystem,
    /// Camera Frustum, and QualitySettings) without touching or modifying any simulation ECS entities.
    /// Citizens, vehicles, economy, services, traffic, and buildings remain 100% active and simulated.
    /// </summary>
    public class UltraVisibilityEngineSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static UltraVisibilityEngineSystem? Instance { get; private set; }

        private RenderingSystem? m_RenderingSystem;
        private BatchMeshSystem? m_BatchMeshSystem;
        private CameraUpdateSystem? m_CameraUpdateSystem;

        private bool m_BaselineCaptured = false;
        private float m_VanillaLevelOfDetail = 1.0f;
        private float m_VanillaLodBias = 1.0f;
        private bool m_VanillaLodCrossFade = true;
        private int m_VanillaMaxLightCount = 8192;
        private bool m_VanillaStrictMemory = false;
        private ulong m_VanillaMemoryBudget = 2147483648uL; // 2048 MB default
        private float m_VanillaFarClipPlane = 10000f;
        private bool m_VanillaUseOcclusion = true;

        private UltraVisibilityMode m_CurrentMode = UltraVisibilityMode.Vanilla;
        private bool m_DynamicFarClipEnabled = true;
        private bool m_StrictMeshMemoryEnabled = true;

        private float m_CurrentFarClip = 10000f;
        private float m_CurrentLodFactor = 1.0f;
        private int m_CurrentMaxLights = 8192;
        private uint m_FrameCounter = 0;

        public UltraVisibilityMode CurrentMode => m_CurrentMode;
        public float CurrentFarClip => m_CurrentFarClip;
        public float CurrentLodFactor => m_CurrentLodFactor;
        public int CurrentMaxLights => m_CurrentMaxLights;
        public bool StrictMemoryActive => m_BatchMeshSystem?.strictMemoryBudget ?? false;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_RenderingSystem = World.GetOrCreateSystemManaged<RenderingSystem>();
            m_BatchMeshSystem = World.GetOrCreateSystemManaged<BatchMeshSystem>();
            m_CameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();

            CaptureBaseline();
            log.Info("[UltraPerformanceMod] UltraVisibilityEngineSystem initialized.");
        }

        protected override void OnDestroy()
        {
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
                    m_VanillaMaxLightCount = m_RenderingSystem.maxLightCount > 0 ? m_RenderingSystem.maxLightCount : 8192;
                    m_VanillaLodBias = QualitySettings.lodBias > 0.05f ? QualitySettings.lodBias : 1.0f;

                    if (m_BatchMeshSystem != null)
                    {
                        m_VanillaStrictMemory = m_BatchMeshSystem.strictMemoryBudget;
                        m_VanillaMemoryBudget = m_BatchMeshSystem.memoryBudget;
                    }

                    var cam = m_CameraUpdateSystem?.activeCamera;
                    if (cam != null)
                    {
                        m_VanillaFarClipPlane = cam.farClipPlane > 100f ? cam.farClipPlane : 10000f;
                        m_VanillaUseOcclusion = cam.useOcclusionCulling;
                    }

                    m_BaselineCaptured = true;
                    log.Info($"[UltraVisibilityEngine] Baseline captured: LOD={m_VanillaLevelOfDetail}, LodBias={m_VanillaLodBias}, FarClip={m_VanillaFarClipPlane}, Lights={m_VanillaMaxLightCount}");
                }
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraVisibilityEngine] Could not capture baseline: {ex.Message}");
            }
        }

        public void RestoreBaseline()
        {
            try
            {
                if (!m_BaselineCaptured) return;

                if (m_RenderingSystem != null)
                {
                    m_RenderingSystem.levelOfDetail = m_VanillaLevelOfDetail;
                    m_RenderingSystem.lodCrossFade = m_VanillaLodCrossFade;
                    m_RenderingSystem.maxLightCount = m_VanillaMaxLightCount;
                }

                QualitySettings.lodBias = m_VanillaLodBias;

                if (m_BatchMeshSystem != null)
                {
                    m_BatchMeshSystem.strictMemoryBudget = m_VanillaStrictMemory;
                    m_BatchMeshSystem.memoryBudget = m_VanillaMemoryBudget;
                }

                var cam = m_CameraUpdateSystem?.activeCamera;
                if (cam != null)
                {
                    cam.farClipPlane = m_VanillaFarClipPlane;
                    cam.useOcclusionCulling = m_VanillaUseOcclusion;
                }

                m_CurrentFarClip = m_VanillaFarClipPlane;
                m_CurrentLodFactor = m_VanillaLevelOfDetail;
                m_CurrentMaxLights = m_VanillaMaxLightCount;

                log.Info("[UltraVisibilityEngine] Baseline restored to 100% vanilla graphics.");
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraVisibilityEngine] Error restoring baseline: {ex.Message}");
            }
        }

        public void ApplyVisibilityMode(UltraVisibilityMode mode, bool dynamicFarClip = true, bool strictMeshMemory = true)
        {
            m_CurrentMode = mode;
            m_DynamicFarClipEnabled = dynamicFarClip;
            m_StrictMeshMemoryEnabled = strictMeshMemory;

            EnforceCurrentSettings();
        }

        public void EnforceCurrentSettings()
        {
            UltraCoreEngine.Instance.TryExecute("UltraVisibilityEngine", () =>
            {
                if (!m_BaselineCaptured)
                {
                    CaptureBaseline();
                }

                if (m_RenderingSystem == null) return;

                switch (m_CurrentMode)
                {
                    case UltraVisibilityMode.Vanilla:
                        RestoreBaseline();
                        break;

                    case UltraVisibilityMode.Balanced:
                        // Otimizado: ~70% LOD geral, corte suave de horizonte, dithered cross-fade
                        m_CurrentLodFactor = 0.70f;
                        m_RenderingSystem.levelOfDetail = 0.70f;
                        m_RenderingSystem.lodCrossFade = true;
                        QualitySettings.lodBias = 0.75f;

                        if (m_BatchMeshSystem != null && m_StrictMeshMemoryEnabled)
                        {
                            m_BatchMeshSystem.strictMemoryBudget = true;
                        }

                        m_CurrentMaxLights = 4096;
                        m_RenderingSystem.maxLightCount = 4096;
                        break;

                    case UltraVisibilityMode.Aggressive:
                        // Agressivo: ~45% LOD, árvores e props só perto da câmera, far-clip dinâmico
                        m_CurrentLodFactor = 0.45f;
                        m_RenderingSystem.levelOfDetail = 0.45f;
                        m_RenderingSystem.lodCrossFade = true;
                        QualitySettings.lodBias = 0.50f;

                        if (m_BatchMeshSystem != null && m_StrictMeshMemoryEnabled)
                        {
                            m_BatchMeshSystem.strictMemoryBudget = true;
                        }

                        m_CurrentMaxLights = 2048;
                        m_RenderingSystem.maxLightCount = 2048;
                        break;

                    case UltraVisibilityMode.Extreme:
                        // Extremo: ~25% LOD, culling máximo de folhagem/props, far-clip compacto
                        m_CurrentLodFactor = 0.25f;
                        m_RenderingSystem.levelOfDetail = 0.25f;
                        m_RenderingSystem.lodCrossFade = true;
                        QualitySettings.lodBias = 0.30f;

                        if (m_BatchMeshSystem != null && m_StrictMeshMemoryEnabled)
                        {
                            m_BatchMeshSystem.strictMemoryBudget = true;
                        }

                        m_CurrentMaxLights = 2048;
                        m_RenderingSystem.maxLightCount = 2048;
                        break;
                }

                // Ensure camera occlusion culling is enabled on active camera
                var cam = m_CameraUpdateSystem?.activeCamera;
                if (cam != null && m_CurrentMode != UltraVisibilityMode.Vanilla)
                {
                    cam.useOcclusionCulling = true;
                }
            });
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null) return;

            m_FrameCounter++;

            // Capture baseline once scene is ready
            if (!m_BaselineCaptured && m_FrameCounter < 300 && m_FrameCounter % 30 == 0)
            {
                CaptureBaseline();
            }

            if (m_CurrentMode == UltraVisibilityMode.Vanilla) return;

            // 1. Dynamic Frustum & Far-Clip Plane Optimization
            if (m_DynamicFarClipEnabled && m_CameraUpdateSystem != null)
            {
                var cam = m_CameraUpdateSystem.activeCamera;
                if (cam != null)
                {
                    float camHeight = cam.transform.position.y;
                    float targetFarClip = m_VanillaFarClipPlane;

                    switch (m_CurrentMode)
                    {
                        case UltraVisibilityMode.Balanced:
                            // Street level (<150m): 4,500m; Mid (150-500m): 6,500m; High: 8,500m+
                            targetFarClip = camHeight < 150f ? 4500f : (camHeight < 500f ? 6500f : 8500f);
                            break;

                        case UltraVisibilityMode.Aggressive:
                            // Street level (<150m): 3,200m; Mid (150-500m): 5,000m; High: 7,500m
                            targetFarClip = camHeight < 150f ? 3200f : (camHeight < 500f ? 5000f : 7500f);
                            break;

                        case UltraVisibilityMode.Extreme:
                            // Street level (<150m): 2,400m; Mid (150-500m): 3,800m; High: 6,000m
                            targetFarClip = camHeight < 150f ? 2400f : (camHeight < 500f ? 3800f : 6000f);
                            break;
                    }

                    // Smooth transition to prevent jarring popping
                    cam.farClipPlane = Mathf.MoveTowards(cam.farClipPlane, targetFarClip, UnityEngine.Time.deltaTime * 2500f);
                    m_CurrentFarClip = cam.farClipPlane;
                }
            }

            // 2. Periodic enforcement in case native game menu overrode properties
            if (m_FrameCounter % 90 == 0)
            {
                EnforceCurrentSettings();
            }
        }
    }
}
