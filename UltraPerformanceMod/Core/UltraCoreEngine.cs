using System;
using System.Collections.Generic;
using Colossal.Logging;

namespace UltraPerformanceMod.Core
{
    public enum UltraCoreStatus
    {
        Disabled,
        Initializing,
        Running,
        Throttled,
        Degraded,
        Error
    }

    /// <summary>
    /// UltraCore Engine: The central high-performance orchestration kernel of the
    /// Ultra Performance & Simulation Booster mod.
    /// Manages sub-engines, lifecycle, error isolation, time-slicing budgets,
    /// diagnostics, and adaptive auto-tuning without any external dependencies.
    /// </summary>
    public sealed class UltraCoreEngine
    {
        private static readonly ILog log = Mod.log;
        private static UltraCoreEngine? s_Instance;
        public static UltraCoreEngine Instance => s_Instance ??= new UltraCoreEngine();

        public UltraCoreStatus Status { get; private set; } = UltraCoreStatus.Disabled;
        public UltraDiagnostics Diagnostics { get; }
        public UltraTaskScheduler Scheduler { get; }
        public UltraAutoTuner AutoTuner { get; }
        public UltraSmartCache Cache { get; }
        public UltraMemoryManager Memory { get; }

        private readonly Dictionary<string, int> m_ModuleErrorCounts = new();
        private const int MaxAllowedModuleErrors = 5;

        private UltraCoreEngine()
        {
            Diagnostics = new UltraDiagnostics();
            Scheduler = new UltraTaskScheduler();
            AutoTuner = new UltraAutoTuner();
            Cache = new UltraSmartCache();
            Memory = UltraMemoryManager.Instance;
        }

        public void Initialize()
        {
            if (Status == UltraCoreStatus.Running) return;

            Status = UltraCoreStatus.Initializing;
            log.Info("[UltraCore] Initializing UltraCore Performance Engine...");

            try
            {
                Memory.Initialize();
                Cache.Initialize();
                Scheduler.Initialize();
                Diagnostics.Initialize();
                AutoTuner.Initialize();

                Status = UltraCoreStatus.Running;
                log.Info("[UltraCore] Engine online and operating at maximum efficiency.");
            }
            catch (Exception ex)
            {
                Status = UltraCoreStatus.Error;
                log.Error($"[UltraCore] Critical failure during initialization: {ex}");
            }
        }

        public void Shutdown()
        {
            if (Status == UltraCoreStatus.Disabled) return;

            log.Info("[UltraCore] Shutting down UltraCore Performance Engine...");
            try
            {
                AutoTuner.Shutdown();
                Diagnostics.Shutdown();
                Scheduler.Shutdown();
                Cache.Shutdown();
                Memory.Shutdown();
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraCore] Warning during shutdown: {ex.Message}");
            }
            finally
            {
                Status = UltraCoreStatus.Disabled;
                m_ModuleErrorCounts.Clear();
                log.Info("[UltraCore] Engine successfully deactivated.");
            }
        }

        public void Update(float deltaTime)
        {
            if (Status != UltraCoreStatus.Running && Status != UltraCoreStatus.Throttled) return;

            // 1. Diagnostics tick: Sample frame time, FPS, GC memory
            Diagnostics.SampleFrame(deltaTime);

            // 2. Auto-Tuner: Adapt budgets and optimization levels to current load
            AutoTuner.Evaluate(Diagnostics);

            // 3. Scheduler: Execute budgeted tasks within frame limit
            Scheduler.ExecuteBudgetedTasks(AutoTuner.CurrentFrameBudgetMs);

            // 4. Cache maintenance: Periodic cleanup of expired entries
            Cache.MaintenanceTick();
        }

        /// <summary>
        /// Safely executes an action with module-level error isolation.
        /// If a module throws repeated unhandled exceptions, it is automatically
        /// disabled to prevent crashing the game or corrupting simulation state.
        /// </summary>
        public bool TryExecute(string moduleName, Action action)
        {
            if (m_ModuleErrorCounts.TryGetValue(moduleName, out int errors) && errors >= MaxAllowedModuleErrors)
            {
                return false; // Module disabled due to instability
            }

            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                int currentErrors = (m_ModuleErrorCounts.TryGetValue(moduleName, out int err) ? err : 0) + 1;
                m_ModuleErrorCounts[moduleName] = currentErrors;

                log.Error($"[UltraCore] Exception in module '{moduleName}' (Failure {currentErrors}/{MaxAllowedModuleErrors}): {ex.Message}");

                if (currentErrors >= MaxAllowedModuleErrors)
                {
                    log.Critical($"[UltraCore] Module '{moduleName}' exceeded error threshold and has been safely deactivated to protect game stability.");
                    Status = UltraCoreStatus.Degraded;
                }

                return false;
            }
        }

        public void ResetModuleErrors(string moduleName)
        {
            if (m_ModuleErrorCounts.ContainsKey(moduleName))
            {
                m_ModuleErrorCounts[moduleName] = 0;
            }
        }
    }
}
