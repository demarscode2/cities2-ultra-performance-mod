using System;
using System.Collections.Generic;
using System.Diagnostics;
using Colossal.Logging;

namespace UltraPerformanceMod.Core
{
    public enum TaskPriority
    {
        Critical = 0,
        Normal = 1,
        Idle = 2
    }

    /// <summary>
    /// UltraTaskScheduler: Intelligent time-sliced task scheduler for mod background optimizations.
    /// Distributes secondary simulation checks (deadlock detection, props cleanup, homeless dispatch,
    /// and building cleanups) across multiple frames, respecting a strict millisecond execution budget.
    /// Prevents CPU frame spikes and hitching in mega-cities.
    /// </summary>
    public sealed class UltraTaskScheduler
    {
        private static readonly ILog log = Mod.log;

        public sealed class ScheduledTask
        {
            public string Name { get; set; } = string.Empty;
            public Action Action { get; set; } = null!;
            public TaskPriority Priority { get; set; } = TaskPriority.Normal;
            public int IntervalFrames { get; set; } = 60;
            public int LastFrameExecuted { get; set; } = -1;
            public bool Enabled { get; set; } = true;
            public int ExecutionCount { get; set; } = 0;
            public int DeferralCount { get; set; } = 0;
        }

        private readonly List<ScheduledTask> m_Tasks = new();
        private int m_CurrentFrame = 0;
        private int m_RoundRobinIndex = 0;

        public int TasksExecutedLastFrame { get; private set; }
        public int TasksDeferredLastFrame { get; private set; }
        public double LastExecutionDurationMs { get; private set; }
        public int RegisteredTaskCount => m_Tasks.Count;

        public void Initialize()
        {
            m_CurrentFrame = 0;
            m_RoundRobinIndex = 0;
            TasksExecutedLastFrame = 0;
            TasksDeferredLastFrame = 0;
            LastExecutionDurationMs = 0;
            log.Info("[UltraCore:Scheduler] Task Scheduler initialized.");
        }

        public void Shutdown()
        {
            m_Tasks.Clear();
            log.Info("[UltraCore:Scheduler] Task Scheduler shut down.");
        }

        public void RegisterTask(string name, Action action, TaskPriority priority = TaskPriority.Normal, int intervalFrames = 60)
        {
            // Update existing or add new
            for (int i = 0; i < m_Tasks.Count; i++)
            {
                if (m_Tasks[i].Name == name)
                {
                    m_Tasks[i].Action = action;
                    m_Tasks[i].Priority = priority;
                    m_Tasks[i].IntervalFrames = Math.Max(1, intervalFrames);
                    return;
                }
            }

            m_Tasks.Add(new ScheduledTask
            {
                Name = name,
                Action = action,
                Priority = priority,
                IntervalFrames = Math.Max(1, intervalFrames),
                LastFrameExecuted = -m_Tasks.Count * 7 // Stagger initial runs across frames
            });
        }

        public void UnregisterTask(string name)
        {
            m_Tasks.RemoveAll(t => t.Name == name);
        }

        public void SetTaskEnabled(string name, bool enabled)
        {
            for (int i = 0; i < m_Tasks.Count; i++)
            {
                if (m_Tasks[i].Name == name)
                {
                    m_Tasks[i].Enabled = enabled;
                    break;
                }
            }
        }

        /// <summary>
        /// Executes pending tasks under the provided frame budget (in milliseconds).
        /// If budget runs out, lower priority tasks are cleanly deferred to upcoming frames.
        /// </summary>
        public void ExecuteBudgetedTasks(double budgetMs)
        {
            m_CurrentFrame++;
            int count = m_Tasks.Count;
            if (count == 0 || budgetMs <= 0.05)
            {
                TasksExecutedLastFrame = 0;
                TasksDeferredLastFrame = 0;
                return;
            }

            long startTick = Stopwatch.GetTimestamp();
            long budgetTicks = (long)(budgetMs * Stopwatch.Frequency / 1000.0);
            int executed = 0;
            int deferred = 0;

            // 1. First run Critical priority tasks that are due
            for (int i = 0; i < count; i++)
            {
                var task = m_Tasks[i];
                if (!task.Enabled || task.Priority != TaskPriority.Critical) continue;

                if (m_CurrentFrame - task.LastFrameExecuted >= task.IntervalFrames)
                {
                    ExecuteTaskSafe(task);
                    executed++;

                    // Critical tasks always run, but check budget
                    if (Stopwatch.GetTimestamp() - startTick >= budgetTicks)
                    {
                        break;
                    }
                }
            }

            // 2. Run Normal and Idle tasks using round-robin staggered order
            long currentTicks = Stopwatch.GetTimestamp() - startTick;
            if (currentTicks < budgetTicks)
            {
                int startIndex = m_RoundRobinIndex % count;
                for (int step = 0; step < count; step++)
                {
                    int idx = (startIndex + step) % count;
                    var task = m_Tasks[idx];

                    if (!task.Enabled || task.Priority == TaskPriority.Critical) continue;

                    if (m_CurrentFrame - task.LastFrameExecuted >= task.IntervalFrames)
                    {
                        if (Stopwatch.GetTimestamp() - startTick < budgetTicks)
                        {
                            ExecuteTaskSafe(task);
                            executed++;
                            m_RoundRobinIndex = (idx + 1) % count;
                        }
                        else
                        {
                            task.DeferralCount++;
                            deferred++;
                        }
                    }
                }
            }

            TasksExecutedLastFrame = executed;
            TasksDeferredLastFrame = deferred;
            long totalElapsed = Stopwatch.GetTimestamp() - startTick;
            LastExecutionDurationMs = (double)totalElapsed / Stopwatch.Frequency * 1000.0;
        }

        private void ExecuteTaskSafe(ScheduledTask task)
        {
            task.LastFrameExecuted = m_CurrentFrame;
            task.ExecutionCount++;

            UltraCoreEngine.Instance.TryExecute(task.Name, () =>
            {
                using (UltraCoreEngine.Instance.Diagnostics.Measure(task.Name))
                {
                    task.Action();
                }
            });
        }
    }
}
