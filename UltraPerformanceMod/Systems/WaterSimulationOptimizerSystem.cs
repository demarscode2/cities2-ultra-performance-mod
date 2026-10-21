using System;
using Colossal.Logging;
using Game;
using Game.Simulation;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    /// <summary>
    /// Safely controls water simulation flow without changing timestep equations (CFL condition),
    /// completely preventing tidal waves, oscillations, and flooding while offering Normal,
    /// Medium (half-speed) and Stopped (frozen simulation) modes.
    /// </summary>
    public class WaterSimulationOptimizerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static WaterSimulationOptimizerSystem? Instance { get; private set; }

        private WaterSystem? m_WaterSystem;
        private WaterSimulationMode m_LastMode = WaterSimulationMode.Normal;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            try
            {
                m_WaterSystem = World.GetOrCreateSystemManaged<WaterSystem>();
                log.Info("[UltraPerformanceMod] Safe WaterSimulationOptimizerSystem initialized.");
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraPerformanceMod] Could not locate WaterSystem: {ex.Message}");
            }
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (m_WaterSystem != null)
            {
                m_WaterSystem.TimeStepOverride = -1f;
                m_WaterSystem.WaterSimSpeed = 1;
            }
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null || m_WaterSystem == null) return;

            m_FrameCounter++;
            WaterSimulationMode currentMode = Mod.Settings.WaterSimulation;

            // Ensure CFL stability: never override dt with tiny/broken numbers
            if (m_WaterSystem.TimeStepOverride != -1f)
            {
                m_WaterSystem.TimeStepOverride = -1f;
            }

            switch (currentMode)
            {
                case WaterSimulationMode.Normal:
                    if (m_WaterSystem.WaterSimSpeed != 1)
                    {
                        m_WaterSystem.WaterSimSpeed = 1;
                        log.Info("[UltraPerformanceMod] Water simulation restored to Normal (Vanilla speed).");
                    }
                    break;

                case WaterSimulationMode.Parada:
                    if (m_WaterSystem.WaterSimSpeed != 0)
                    {
                        m_WaterSystem.WaterSimSpeed = 0;
                        log.Info("[UltraPerformanceMod] Water simulation Stopped (WaterSimSpeed = 0, zero physics computation).");
                    }
                    break;

                case WaterSimulationMode.Media:
                    // Half-speed: smoothly alternate WaterSimSpeed between 1 and 0 every frame
                    // Keeps native numerical stability while halving compute progression
                    int targetSpeed = (m_FrameCounter % 2 == 0) ? 1 : 0;
                    if (m_WaterSystem.WaterSimSpeed != targetSpeed)
                    {
                        m_WaterSystem.WaterSimSpeed = targetSpeed;
                    }
                    break;
            }

            m_LastMode = currentMode;
        }
    }
}
