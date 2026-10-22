using System;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Creatures;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    /// <summary>
    /// Detects and clears pedestrians trapped with CreatureLaneFlags.Stuck on glitched sidewalks,
    /// crosswalks, or station steps, preventing infinite pathfind retries.
    /// </summary>
    public class StuckPedestrianOptimizerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static StuckPedestrianOptimizerSystem? Instance { get; private set; }

        private EntityQuery m_StuckPedestrianQuery = default!;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_StuckPedestrianQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Human>(),
                    ComponentType.ReadOnly<HumanCurrentLane>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] StuckPedestrianOptimizerSystem initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            // Passive system: triggered on demand via Quick Action or Deadlock Breaker
        }

        public int ClearStuckPedestriansNow()
        {
            if (m_StuckPedestrianQuery.IsEmptyIgnoreFilter) return 0;

            int cleared = 0;
            try
            {
                using var entities = m_StuckPedestrianQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    if (!EntityManager.Exists(entity) || EntityManager.HasComponent<Deleted>(entity))
                        continue;

                    var lane = EntityManager.GetComponentData<HumanCurrentLane>(entity);
                    if ((lane.m_Flags & CreatureLaneFlags.Stuck) != 0)
                    {
                        EntityManager.AddComponent<Deleted>(entity);
                        cleared++;
                    }
                }

                log.Info($"[UltraPerformanceMod] Cleared {cleared} stuck pedestrians.");
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraPerformanceMod] Error clearing stuck pedestrians: {ex.Message}");
            }

            return cleared;
        }
    }
}
