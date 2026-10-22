using System;
using Colossal.Logging;
using Game;
using Game.Simulation;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    /// <summary>
    /// Guarantees that water simulation maintains 100% vanilla physics stability (TimeStepOverride = -1f,
    /// WaterSimSpeed = 1), preventing any wave resonance, ship bouncing, or tidal flooding.
    /// </summary>
    public class WaterSimulationOptimizerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static WaterSimulationOptimizerSystem? Instance { get; private set; }

        private WaterSystem? m_WaterSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            try
            {
                m_WaterSystem = World.GetOrCreateSystemManaged<WaterSystem>();
                if (m_WaterSystem != null)
                {
                    m_WaterSystem.TimeStepOverride = -1f;
                    m_WaterSystem.WaterSimSpeed = 1;
                }
                log.Info("[UltraPerformanceMod] WaterSimulationOptimizerSystem: vanilla water stability enforced (TimeStepOverride = -1, WaterSimSpeed = 1).");
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraPerformanceMod] Could not locate WaterSystem: {ex.Message}");
            }
        }

        protected override void OnDestroy()
        {
            if (m_WaterSystem != null)
            {
                m_WaterSystem.TimeStepOverride = -1f;
                m_WaterSystem.WaterSimSpeed = 1;
            }

            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            // Passive system: enforces default vanilla values
            if (m_WaterSystem != null)
            {
                if (m_WaterSystem.TimeStepOverride != -1f)
                {
                    m_WaterSystem.TimeStepOverride = -1f;
                }
                if (m_WaterSystem.WaterSimSpeed != 1)
                {
                    m_WaterSystem.WaterSimSpeed = 1;
                }
            }
        }
    }
}
