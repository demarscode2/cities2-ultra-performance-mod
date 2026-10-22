using Colossal.Logging;
using Game;
using Game.Common;
using Game.Objects;
using UltraPerformanceMod.Core;
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

            log.Info("[UltraPerformanceMod] High-Performance InstantConstructionSystem initialized.");
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

            int count = 0;
            using (UltraCoreEngine.Instance.Diagnostics.Measure("InstantConstruction"))
            {
                using var chunks = m_UnderConstructionQuery.ToArchetypeChunkArray(Allocator.TempJob);
                var ucType = GetComponentTypeHandle<UnderConstruction>(false);

                for (int c = 0; c < chunks.Length; c++)
                {
                    var chunk = chunks[c];
                    var ucArray = chunk.GetNativeArray(ref ucType);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        var uc = ucArray[i];
                        if (uc.m_Progress < byte.MaxValue)
                        {
                            uc.m_Progress = byte.MaxValue;
                            uc.m_Speed = byte.MaxValue;
                            ucArray[i] = uc;
                            count++;
                        }
                    }
                }
            }

            if (count > 0)
            {
                log.Info($"[UltraPerformanceMod] InstantConstruction: Instantly finalized {count} building constructions/upgrades (Direct chunk memory write).");
            }
        }
    }
}
