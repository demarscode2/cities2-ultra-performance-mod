using System;
using Colossal.Logging;
using Game;
using Game.Citizens;
using Game.Common;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    /// <summary>
    /// Solves the infamous CS2 Homeless Loop Bug where thousands of homeless citizens get trapped
    /// camping in parks or streets, flooding pathfind threads with endless calculation requests.
    /// Provides an automated periodic evacuation and a manual 1-click Quick Action button.
    /// </summary>
    public class HomelessOptimizerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static HomelessOptimizerSystem? Instance { get; private set; }

        private EntityQuery m_HomelessHouseholdQuery = default!;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_HomelessHouseholdQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<HomelessHousehold>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] HomelessOptimizerSystem initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null) return;

            m_FrameCounter++;

            // Periodic auto-evacuation every ~30 seconds (1800 frames) if enabled
            if (Mod.Settings.AutoEvacuateHomeless && m_FrameCounter % 1800 == 0)
            {
                int count = PurgeStrandedHomelessNow();
                if (count > 0)
                {
                    log.Info($"[UltraPerformanceMod] Auto-evacuated {count} stranded homeless households.");
                }
            }
        }

        public int PurgeStrandedHomelessNow()
        {
            if (m_HomelessHouseholdQuery.IsEmptyIgnoreFilter) return 0;

            int purged = 0;
            try
            {
                using var entities = m_HomelessHouseholdQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < entities.Length; i++)
                {
                    var household = entities[i];
                    if (!EntityManager.Exists(household) || EntityManager.HasComponent<Deleted>(household))
                        continue;

                    // If household has citizens, also mark them Deleted so no orphan ghost citizens remain
                    if (EntityManager.HasBuffer<HouseholdCitizen>(household))
                    {
                        var citizenBuffer = EntityManager.GetBuffer<HouseholdCitizen>(household);
                        for (int c = 0; c < citizenBuffer.Length; c++)
                        {
                            var cim = citizenBuffer[c].m_Citizen;
                            if (EntityManager.Exists(cim) && !EntityManager.HasComponent<Deleted>(cim))
                            {
                                EntityManager.AddComponent<Deleted>(cim);
                            }
                        }
                    }

                    EntityManager.AddComponent<Deleted>(household);
                    purged++;
                }

                log.Info($"[UltraPerformanceMod] Purged {purged} stranded homeless households.");
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraPerformanceMod] Error purging homeless households: {ex.Message}");
            }

            return purged;
        }
    }
}
