using System;
using Colossal.Logging;

namespace UltraPerformanceMod.Core
{
    public enum AutoTunerProfile
    {
        Auto = 0,
        Conservative = 1,
        Balanced = 2,
        Aggressive = 3
    }

    public enum SystemLoadState
    {
        Optimal,    // > 55 FPS
        Moderate,   // 35 - 55 FPS
        Heavy,      // 20 - 35 FPS
        Critical    // < 20 FPS
    }

    /// <summary>
    /// UltraAutoTuner: Dynamic load-adaptive controller.
    /// Monitors rolling FPS and frame latency to dynamically adapt mod execution budgets,
    /// defer non-essential simulation work during heavy frames, and prevent mod-induced micro-stutters.
    /// Includes hysteresis filters to prevent erratic configuration bouncing.
    /// </summary>
    public sealed class UltraAutoTuner
    {
        private static readonly ILog log = Mod.log;

        public AutoTunerProfile ActiveProfile { get; set; } = AutoTunerProfile.Auto;
        public SystemLoadState CurrentLoadState { get; private set; } = SystemLoadState.Moderate;

        // Dynamic Budgets (Milliseconds per frame allocated to secondary tasks)
        public double CurrentFrameBudgetMs { get; private set; } = 1.2;

        // Interval multiplier for secondary jobs (1.0 = normal, 2.0 = half frequency, 0.5 = double frequency)
        public float TaskIntervalScale { get; private set; } = 1.0f;

        // Hysteresis counters to prevent thrashing
        private int m_StateStabilityFrames = 0;
        private const int RequiredStabilityFrames = 60; // Must sustain new load level for 1 sec before adapting
        private SystemLoadState m_CandidateState = SystemLoadState.Moderate;

        public void Initialize()
        {
            CurrentLoadState = SystemLoadState.Moderate;
            m_CandidateState = SystemLoadState.Moderate;
            m_StateStabilityFrames = 0;
            CurrentFrameBudgetMs = 1.2;
            TaskIntervalScale = 1.0f;
            log.Info("[UltraCore:AutoTuner] Adaptive auto-tuner initialized.");
        }

        public void Shutdown()
        {
            log.Info("[UltraCore:AutoTuner] Adaptive auto-tuner shut down.");
        }

        /// <summary>
        /// Evaluates recent diagnostics metrics and adapts budget/intervals accordingly.
        /// </summary>
        public void Evaluate(UltraDiagnostics diagnostics)
        {
            if (ActiveProfile != AutoTunerProfile.Auto)
            {
                ApplyManualProfile(ActiveProfile);
                return;
            }

            // Determine candidate load state from 1% low and average FPS
            float avgFps = diagnostics.AverageFps;
            float p99Fps = diagnostics.OnePercentLowFps;

            SystemLoadState detectedState;
            if (avgFps >= 55f && p99Fps >= 45f)
            {
                detectedState = SystemLoadState.Optimal;
            }
            else if (avgFps >= 35f && p99Fps >= 25f)
            {
                detectedState = SystemLoadState.Moderate;
            }
            else if (avgFps >= 20f || p99Fps >= 15f)
            {
                detectedState = SystemLoadState.Heavy;
            }
            else
            {
                detectedState = SystemLoadState.Critical;
            }

            // Hysteresis stabilization
            if (detectedState == m_CandidateState)
            {
                m_StateStabilityFrames++;
                if (m_StateStabilityFrames >= RequiredStabilityFrames && CurrentLoadState != detectedState)
                {
                    CurrentLoadState = detectedState;
                    ApplyLoadState(CurrentLoadState);
                }
            }
            else
            {
                m_CandidateState = detectedState;
                m_StateStabilityFrames = 0;
            }
        }

        private void ApplyLoadState(SystemLoadState state)
        {
            switch (state)
            {
                case SystemLoadState.Optimal:
                    CurrentFrameBudgetMs = 2.0;
                    TaskIntervalScale = 0.8f;
                    break;

                case SystemLoadState.Moderate:
                    CurrentFrameBudgetMs = 1.2;
                    TaskIntervalScale = 1.0f;
                    break;

                case SystemLoadState.Heavy:
                    CurrentFrameBudgetMs = 0.6;
                    TaskIntervalScale = 1.5f;
                    break;

                case SystemLoadState.Critical:
                    CurrentFrameBudgetMs = 0.25;
                    TaskIntervalScale = 2.5f;
                    break;
            }
        }

        private void ApplyManualProfile(AutoTunerProfile profile)
        {
            switch (profile)
            {
                case AutoTunerProfile.Conservative:
                    CurrentFrameBudgetMs = 0.5;
                    TaskIntervalScale = 1.8f;
                    CurrentLoadState = SystemLoadState.Heavy;
                    break;

                case AutoTunerProfile.Balanced:
                    CurrentFrameBudgetMs = 1.2;
                    TaskIntervalScale = 1.0f;
                    CurrentLoadState = SystemLoadState.Moderate;
                    break;

                case AutoTunerProfile.Aggressive:
                    CurrentFrameBudgetMs = 2.5;
                    TaskIntervalScale = 0.7f;
                    CurrentLoadState = SystemLoadState.Optimal;
                    break;
            }
        }
    }
}
