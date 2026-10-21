using System;
using Colossal.Logging;
using Game;
using Game.Simulation;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    public enum WaterSimulationMode
    {
        Vanilla = 0,
        Throttled = 1,
        Disabled = 2
    }

    /// <summary>
    /// Safely controls water simulation flow without toggling WaterSystem.Enabled,
    /// avoiding native GPU readback crashes and keeping the rendering pipeline stable.
    /// </summary>
    public class WaterSimulationOptimizerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static WaterSimulationOptimizerSystem? Instance { get; private set; }

        private WaterSystem? m_WaterSystem;
        private WaterSimulationMode m_LastMode = WaterSimulationMode.Vanilla;

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
            }
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null || m_WaterSystem == null) return;

            WaterSimulationMode currentMode = Mod.Settings.WaterSimulation;
            if (currentMode == m_LastMode) return;

            switch (currentMode)
            {
                case WaterSimulationMode.Vanilla:
                    // Default delta time calculation
                    m_WaterSystem.TimeStepOverride = -1f;
                    log.Info("[UltraPerformanceMod] Water simulation restored to vanilla speed.");
                    break;

                case WaterSimulationMode.Throttled:
                    // Greatly slows down fluid simulation step to save CPU/GPU cycles
                    m_WaterSystem.TimeStepOverride = 0.05f;
                    log.Info("[UltraPerformanceMod] Water simulation throttled to 0.05 timestep.");
                    break;

                case WaterSimulationMode.Disabled:
                    // Virtually freeze water flow while keeping GPU pipeline healthy
                    m_WaterSystem.TimeStepOverride = 0.00001f;
                    log.Info("[UltraPerformanceMod] Water simulation flow frozen (static water mesh retained).");
                    break;
            }

            m_LastMode = currentMode;
        }
    }
}
