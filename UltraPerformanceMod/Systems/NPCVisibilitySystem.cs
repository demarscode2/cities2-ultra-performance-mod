using Colossal.Logging;
using Game;
using Game.Common;
using Game.Creatures;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    public class NPCVisibilitySystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        private EntityQuery m_VisibleCreaturesQuery = default!;
        private EntityQuery m_HiddenCreaturesQuery = default!;
        private bool m_WasInvisible = false;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();

            // Query for creatures that are currently visible (do not have Hidden)
            m_VisibleCreaturesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Creature>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Hidden>(), ComponentType.ReadOnly<Deleted>() }
            });

            // Query for creatures that are currently hidden
            m_HiddenCreaturesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Creature>(), ComponentType.ReadOnly<Hidden>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] NPCVisibilitySystem initialized.");
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null) return;

            bool isCurrentlyInvisible = Mod.Settings.InvisibleNPCs;

            // Run state transition or throttled update every 32 frames
            m_FrameCounter++;
            bool stateChanged = (isCurrentlyInvisible != m_WasInvisible);

            if (!stateChanged && (m_FrameCounter % 32 != 0))
            {
                return;
            }

            if (isCurrentlyInvisible)
            {
                // Hide any visible creature
                if (!m_VisibleCreaturesQuery.IsEmptyIgnoreFilter)
                {
                    using var entities = m_VisibleCreaturesQuery.ToEntityArray(Allocator.TempJob);
                    if (entities.Length > 0)
                    {
                        EntityManager.AddComponent<Hidden>(entities);
                        EntityManager.AddComponent<BatchesUpdated>(entities);
                    }
                }
            }
            else if (stateChanged && !isCurrentlyInvisible)
            {
                // Restore visibility for all hidden creatures
                if (!m_HiddenCreaturesQuery.IsEmptyIgnoreFilter)
                {
                    using var entities = m_HiddenCreaturesQuery.ToEntityArray(Allocator.TempJob);
                    if (entities.Length > 0)
                    {
                        EntityManager.RemoveComponent<Hidden>(entities);
                        EntityManager.AddComponent<BatchesUpdated>(entities);
                        log.Info($"[UltraPerformanceMod] Restored visibility for {entities.Length} NPCs/Creatures.");
                    }
                }
            }

            m_WasInvisible = isCurrentlyInvisible;
        }
    }
}
