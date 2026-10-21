using Colossal.Logging;
using Game;
using Game.Common;
using Game.Objects;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    public class InstantConstructionSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static InstantConstructionSystem? Instance { get; private set; }

        private EntityQuery m_UnderConstructionQuery = default!;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_UnderConstructionQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadWrite<UnderConstruction>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] InstantConstructionSystem initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null || !Mod.Settings.InstantConstruction) return;

            m_FrameCounter++;
            // Check every 32 frames for newly started constructions
            if (m_FrameCounter % 32 != 0) return;

            CompleteAllUnderConstruction();
        }

        public void CompleteAllConstructionNow()
        {
            CompleteAllUnderConstruction();
        }

        private void CompleteAllUnderConstruction()
        {
            if (m_UnderConstructionQuery.IsEmptyIgnoreFilter) return;

            using var entities = m_UnderConstructionQuery.ToEntityArray(Allocator.TempJob);
            int count = 0;

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (!EntityManager.Exists(entity) || EntityManager.HasComponent<Deleted>(entity)) continue;

                var uc = EntityManager.GetComponentData<UnderConstruction>(entity);
                if (uc.m_Progress < byte.MaxValue)
                {
                    uc.m_Progress = byte.MaxValue;
                    uc.m_Speed = byte.MaxValue;
                    EntityManager.SetComponentData(entity, uc);
                    count++;
                }
            }

            if (count > 0)
            {
                log.Info($"[UltraPerformanceMod] InstantConstruction: Instantly finalized {count} building constructions/upgrades.");
            }
        }
    }
}
