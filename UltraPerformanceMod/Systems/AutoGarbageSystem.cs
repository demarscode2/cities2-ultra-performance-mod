using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Simulation;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    public class AutoGarbageSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static AutoGarbageSystem? Instance { get; private set; }

        private EntityQuery m_GarbageProducerQuery = default!;
        private EntityQuery m_GarbageTruckQuery = default!;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_GarbageProducerQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadWrite<GarbageProducer>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_GarbageTruckQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<GarbageTruck>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] AutoGarbageSystem initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null || !Mod.Settings.AutoGarbage) return;

            m_FrameCounter++;
            if (m_FrameCounter % 64 != 0) return;

            CleanAllGarbage();
            PurgeActiveGarbageTrucks();
        }

        public int CleanAllGarbageNow()
        {
            int cleaned = CleanAllGarbage();
            PurgeActiveGarbageTrucks();
            return cleaned;
        }

        private int CleanAllGarbage()
        {
            if (m_GarbageProducerQuery.IsEmptyIgnoreFilter) return 0;

            int cleanedCount = 0;
            using var chunks = m_GarbageProducerQuery.ToArchetypeChunkArray(Allocator.TempJob);
            var producerType = GetComponentTypeHandle<GarbageProducer>(false);

            for (int c = 0; c < chunks.Length; c++)
            {
                var chunk = chunks[c];
                var producers = chunk.GetNativeArray(ref producerType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var producer = producers[i];
                    if (producer.m_Garbage > 0 || producer.m_CollectionRequest != Entity.Null)
                    {
                        if (producer.m_CollectionRequest != Entity.Null && EntityManager.Exists(producer.m_CollectionRequest))
                        {
                            EntityManager.AddComponent<Deleted>(producer.m_CollectionRequest);
                        }

                        producer.m_Garbage = 0;
                        producer.m_CollectionRequest = Entity.Null;
                        producers[i] = producer;
                        cleanedCount++;
                    }
                }
            }

            return cleanedCount;
        }

        private void PurgeActiveGarbageTrucks()
        {
            if (m_GarbageTruckQuery.IsEmptyIgnoreFilter) return;

            using var trucks = m_GarbageTruckQuery.ToEntityArray(Allocator.TempJob);
            for (int i = 0; i < trucks.Length; i++)
            {
                UltraPerformanceMod.Utils.SafeVehicleDespawner.Despawn(EntityManager, trucks[i]);
            }

            if (trucks.Length > 0)
            {
                log.Info($"[UltraPerformanceMod] Purged {trucks.Length} active garbage trucks from streets.");
            }
        }
    }
}
