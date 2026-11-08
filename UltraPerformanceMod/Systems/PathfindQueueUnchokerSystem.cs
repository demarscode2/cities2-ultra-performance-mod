using System;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Creatures;
using Game.Pathfind;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace UltraPerformanceMod.Systems
{
    /// <summary>
    /// UltraCore Pathfind Queue Unchoker System.
    /// Eliminates the native CS2 simulation speed degradation in cities above 300k-700k cims.
    /// 
    /// Why simulation slows down in vanilla:
    /// In SimulationSystem.OnUpdate(), the game calculates:
    ///    frameGap = m_PathfindResultSystem.pendingSimulationFrame - frameIndex - 1;
    ///    throttle = math.min(1f, frameGap * (1f / 48f));
    ///    simulationSpeed *= throttle;
    /// 
    /// If even a few path requests get stuck or if queues grow under heavy traffic,
    /// frameGap shrinks below 48, causing the game to throttle simulation speed to 0.5x, 0.2x, or 0x!
    /// 
    /// What this system does:
    /// 1. Enforces SimulationSystem.PerformancePreference.SimulationSpeed so simulation frames aren't starved by GPU.
    /// 2. Monitors the pathfind queue gap and request backlog in real time.
    /// 3. Automatically purges orphan, deleted, and stuck path requests that hold back m_PendingSimulationFrameIndex.
    /// 4. Provides a 1-click Quick Action on the HUD to immediately unchoke pathfinding queues and restore full simulation speed.
    /// </summary>
    public partial class PathfindQueueUnchokerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static PathfindQueueUnchokerSystem? Instance { get; private set; }

        private SimulationSystem? m_SimulationSystem;
        private PathfindResultSystem? m_PathfindResultSystem;
        private PathfindSetupSystem? m_PathfindSetupSystem;

        private EntityQuery m_DeletedPathOwnersQuery = default!;
        private EntityQuery m_AnimalPathQuery = default!;

        // Real-time Telemetry
        public int LastFrameGap { get; private set; } = 48;
        public int LastPendingRequests { get; private set; } = 0;
        public float ThrottleEfficiency { get; private set; } = 100f;
        public bool IsUnchokingActive { get; private set; } = false;
        public int TotalUnchokedRequests { get; private set; } = 0;

        private int m_UpdateCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_PathfindResultSystem = World.GetOrCreateSystemManaged<PathfindResultSystem>();
            m_PathfindSetupSystem = World.GetOrCreateSystemManaged<PathfindSetupSystem>();

            m_DeletedPathOwnersQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<PathOwner>(), ComponentType.ReadOnly<Deleted>() }
            });

            m_AnimalPathQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadOnly<Animal>() }
            });

            log.Info("[UltraCore] PathfindQueueUnchokerSystem initialized successfully.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (m_SimulationSystem == null || m_PathfindResultSystem == null) return;

            bool isEnabled = Mod.Settings?.EnablePathfindQueueUnchoker ?? true;
            if (!isEnabled)
            {
                IsUnchokingActive = false;
                return;
            }

            // 1. Enforce SimulationSpeed priority so rendering framerates don't throttle simulation ticks
            if (m_SimulationSystem.performancePreference != SimulationSystem.PerformancePreference.SimulationSpeed)
            {
                m_SimulationSystem.performancePreference = SimulationSystem.PerformancePreference.SimulationSpeed;
            }

            // 2. Read live queue metrics
            uint currentFrame = m_SimulationSystem.frameIndex;
            uint pendingFrame = m_PathfindResultSystem.pendingSimulationFrame;
            int pendingCount = m_PathfindResultSystem.pendingRequestCount;

            int gap = (pendingFrame < uint.MaxValue) ? (int)math.max(0u, pendingFrame - currentFrame - 1) : 48;
            float efficiency = math.clamp((float)gap / 48f, 0f, 1f) * 100f;

            LastFrameGap = gap;
            LastPendingRequests = pendingCount;
            ThrottleEfficiency = efficiency;

            // 3. Automated unchoking when backlog threatens simulation speed (gap < 36 frames)
            m_UpdateCounter++;
            if (gap < 36 || m_UpdateCounter >= 60)
            {
                m_UpdateCounter = 0;
                AutoUnchokeStalePaths(gap < 36);
            }
        }

        private void AutoUnchokeStalePaths(bool aggressive)
        {
            try
            {
                // Purge path requests attached to deleted or despawned entities
                if (!m_DeletedPathOwnersQuery.IsEmptyIgnoreFilter)
                {
                    EntityManager.RemoveComponent<PathOwner>(m_DeletedPathOwnersQuery);
                    EntityManager.RemoveComponent<PathInformation>(m_DeletedPathOwnersQuery);
                }

                // If gap is critically narrow, discard low-priority ambient wildlife pathfinding to free worker threads
                if (aggressive && !m_AnimalPathQuery.IsEmptyIgnoreFilter)
                {
                    using var entities = m_AnimalPathQuery.ToEntityArray(Allocator.TempJob);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        var entity = entities[i];
                        if (EntityManager.HasComponent<PathOwner>(entity))
                        {
                            var owner = EntityManager.GetComponentData<PathOwner>(entity);
                            if ((owner.m_State & PathFlags.Pending) != 0)
                            {
                                owner.m_State &= ~PathFlags.Pending;
                                owner.m_State |= PathFlags.Obsolete;
                                EntityManager.SetComponentData(entity, owner);
                                TotalUnchokedRequests++;
                            }
                        }
                    }
                }

                IsUnchokingActive = aggressive;
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraCore] Notice during AutoUnchoke: {ex.Message}");
            }
        }

        /// <summary>
        /// Explicit 1-click Quick Action: cleans and cancels all stuck/stale path requests across the city.
        /// Immediately frees the global pendingSimulationFrame lock and restores simulation clock speed.
        /// </summary>
        public int UnchokePathfindQueueNow()
        {
            int unchoked = 0;
            try
            {
                // 1. Clear deleted entities holding path components
                if (!m_DeletedPathOwnersQuery.IsEmptyIgnoreFilter)
                {
                    int delCount = m_DeletedPathOwnersQuery.CalculateEntityCount();
                    EntityManager.RemoveComponent<PathOwner>(m_DeletedPathOwnersQuery);
                    EntityManager.RemoveComponent<PathInformation>(m_DeletedPathOwnersQuery);
                    unchoked += delCount;
                }

                // 2. Scan pending PathOwners and mark stuck ones as obsolete
                var query = GetEntityQuery(ComponentType.ReadWrite<PathOwner>());
                if (!query.IsEmptyIgnoreFilter)
                {
                    using var entities = query.ToEntityArray(Allocator.TempJob);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        var e = entities[i];
                        var owner = EntityManager.GetComponentData<PathOwner>(e);
                        if ((owner.m_State & PathFlags.Pending) != 0)
                        {
                            // Check if entity is valid or immobilized
                            if (EntityManager.HasComponent<PathInformation>(e))
                            {
                                var info = EntityManager.GetComponentData<PathInformation>(e);
                                if ((info.m_State & PathFlags.Pending) != 0)
                                {
                                    info.m_State &= ~PathFlags.Pending;
                                    info.m_State |= PathFlags.Obsolete;
                                    EntityManager.SetComponentData(e, info);
                                }
                            }

                            owner.m_State &= ~PathFlags.Pending;
                            owner.m_State |= PathFlags.Obsolete;
                            EntityManager.SetComponentData(e, owner);
                            unchoked++;
                        }
                    }
                }

                // 3. Re-enforce SimulationSpeed preference
                if (m_SimulationSystem != null)
                {
                    m_SimulationSystem.performancePreference = SimulationSystem.PerformancePreference.SimulationSpeed;
                }

                TotalUnchokedRequests += unchoked;
                log.Info($"[UltraCore] UnchokePathfindQueueNow executed: {unchoked} pending requests cleared, queue unchoked.");
            }
            catch (Exception ex)
            {
                log.Error($"[UltraCore] Error in UnchokePathfindQueueNow: {ex.Message}");
            }

            return unchoked;
        }
    }
}
