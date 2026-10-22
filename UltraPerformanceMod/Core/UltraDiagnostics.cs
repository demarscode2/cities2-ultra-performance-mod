using System;
using System.Collections.Generic;
using System.Diagnostics;
using Colossal.Logging;

namespace UltraPerformanceMod.Core
{
    /// <summary>
    /// UltraDiagnostics: High-precision, zero-allocation real-time performance and simulation telemetry.
    /// Samples frame times, FPS (instant, average, 1% low), managed memory footprint, GC collections,
    /// and mod CPU overhead per task.
    /// </summary>
    public sealed class UltraDiagnostics
    {
        private static readonly ILog log = Mod.log;

        // --- Rolling Frame Buffer for Instant, Avg, and 1% Low FPS ---
        private const int FrameBufferSize = 120;
        private readonly float[] m_FrameTimes = new float[FrameBufferSize];
        private readonly float[] m_SortedFrameTimes = new float[FrameBufferSize]; // Preallocated for 1% low calculation
        private int m_FrameIndex = 0;
        private int m_SampleCount = 0;

        // --- Metrics (Public Properties for UI and Auto-Tuner) ---
        public float InstantFps { get; private set; }
        public float AverageFps { get; private set; }
        public float OnePercentLowFps { get; private set; }
        public float FrameTimeMs { get; private set; }
        public float SimulationDeltaTime { get; private set; }

        public double ManagedMemoryMb { get; private set; }
        public int GcGen0Count { get; private set; }
        public int GcGen1Count { get; private set; }
        public int GcGen2Count { get; private set; }

        public double TotalModCpuTimeMs { get; private set; }
        public int ActiveOptimizationsCount { get; set; }

        // --- Module Performance Instrumentation ---
        private readonly Dictionary<string, ModuleTiming> m_ModuleTimings = new();
        private readonly Stopwatch m_FrameStopwatch = new();
        private long m_LastGcCheckTick = 0;
        private const long GcSampleIntervalTicks = TimeSpan.TicksPerSecond / 2; // Sample GC every 500ms

        public sealed class ModuleTiming
        {
            public string Name { get; set; } = string.Empty;
            public double LastExecutionMs { get; set; }
            public double PeakExecutionMs { get; set; }
            public double TotalExecutionMs { get; set; }
            public long ExecutionCount { get; set; }
        }

        public IReadOnlyDictionary<string, ModuleTiming> ModuleTimings => m_ModuleTimings;

        public void Initialize()
        {
            Array.Clear(m_FrameTimes, 0, m_FrameTimes.Length);
            m_FrameIndex = 0;
            m_SampleCount = 0;
            m_ModuleTimings.Clear();
            m_FrameStopwatch.Restart();
            m_LastGcCheckTick = DateTime.UtcNow.Ticks;
            SampleGcMemory();
            log.Info("[UltraCore:Diagnostics] Diagnostics engine initialized.");
        }

        public void Shutdown()
        {
            m_ModuleTimings.Clear();
            m_FrameStopwatch.Stop();
            log.Info("[UltraCore:Diagnostics] Diagnostics engine shut down.");
        }

        /// <summary>
        /// Called once per frame in PreRender/Update to record frame delta and update FPS rolling metrics.
        /// Zero heap allocation.
        /// </summary>
        public void SampleFrame(float deltaTime)
        {
            if (deltaTime <= 0.00001f) return;

            SimulationDeltaTime = deltaTime;
            FrameTimeMs = deltaTime * 1000f;
            InstantFps = 1f / deltaTime;

            m_FrameTimes[m_FrameIndex] = deltaTime;
            m_FrameIndex = (m_FrameIndex + 1) % FrameBufferSize;
            if (m_SampleCount < FrameBufferSize) m_SampleCount++;

            // Calculate Average FPS and 1% Low FPS
            float sum = 0f;
            for (int i = 0; i < m_SampleCount; i++)
            {
                sum += m_FrameTimes[i];
                m_SortedFrameTimes[i] = m_FrameTimes[i];
            }

            float avgDelta = sum / m_SampleCount;
            AverageFps = avgDelta > 0.00001f ? 1f / avgDelta : 0f;

            // 1% Low: 99th percentile slowest frame time in the window
            Array.Sort(m_SortedFrameTimes, 0, m_SampleCount);
            int p99Index = (int)Math.Floor(m_SampleCount * 0.99f);
            if (p99Index >= m_SampleCount) p99Index = m_SampleCount - 1;
            float p99Delta = m_SortedFrameTimes[p99Index];
            OnePercentLowFps = p99Delta > 0.00001f ? 1f / p99Delta : 0f;

            // Sample GC and Memory every 500ms to avoid sampling overhead
            long currentTick = DateTime.UtcNow.Ticks;
            if (currentTick - m_LastGcCheckTick >= GcSampleIntervalTicks)
            {
                m_LastGcCheckTick = currentTick;
                SampleGcMemory();
            }
        }

        private void SampleGcMemory()
        {
            try
            {
                ManagedMemoryMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
                GcGen0Count = GC.CollectionCount(0);
                GcGen1Count = GC.CollectionCount(1);
                GcGen2Count = GC.CollectionCount(2);
            }
            catch
            {
                // Safety guard for memory sampling
            }
        }

        /// <summary>
        /// Record the execution duration of an internal mod routine.
        /// </summary>
        public void RecordModuleExecution(string moduleName, double elapsedMs)
        {
            if (!m_ModuleTimings.TryGetValue(moduleName, out var timing))
            {
                timing = new ModuleTiming { Name = moduleName };
                m_ModuleTimings[moduleName] = timing;
            }

            timing.LastExecutionMs = elapsedMs;
            if (elapsedMs > timing.PeakExecutionMs) timing.PeakExecutionMs = elapsedMs;
            timing.TotalExecutionMs += elapsedMs;
            timing.ExecutionCount++;

            // Update rolling total mod CPU time
            TotalModCpuTimeMs = (TotalModCpuTimeMs * 0.9) + (elapsedMs * 0.1);
        }

        /// <summary>
        /// Lightweight IDisposable scope to measure execution time with zero allocations.
        /// </summary>
        public TimingScope Measure(string moduleName)
        {
            return new TimingScope(this, moduleName);
        }

        public readonly struct TimingScope : IDisposable
        {
            private readonly UltraDiagnostics m_Diagnostics;
            private readonly string m_ModuleName;
            private readonly long m_StartTimestamp;

            public TimingScope(UltraDiagnostics diagnostics, string moduleName)
            {
                m_Diagnostics = diagnostics;
                m_ModuleName = moduleName;
                m_StartTimestamp = Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                long elapsed = Stopwatch.GetTimestamp() - m_StartTimestamp;
                double ms = (double)elapsed / Stopwatch.Frequency * 1000.0;
                m_Diagnostics.RecordModuleExecution(m_ModuleName, ms);
            }
        }
    }
}
