using Colossal.Logging;
using Game;
using Game.Common;
using Game.Vehicles;
using UltraPerformanceMod.Core;
using UltraPerformanceMod.Utils;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    public class ParkingOptimizerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static ParkingOptimizerSystem? Instance { get; private set; }

        private EntityQuery m_ParkedCarQuery = default!;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_ParkedCarQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<ParkedCar>(),
                    ComponentType.ReadOnly<Vehicle>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] High-Performance ParkingOptimizerSystem initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null || !Mod.Settings.OptimizeParking) return;

            m_FrameCounter++;
            float intervalScale = UltraCoreEngine.Instance.AutoTuner.TaskIntervalScale;
            int scaledInterval = (int)(128 * intervalScale);
            if (m_FrameCounter % System.Math.Max(32, scaledInterval) != 0) return;

            PurgeInvalidParkedCars();
        }

        public int PurgeInvalidParkedCarsNow()
        {
            return PurgeInvalidParkedCars();
        }

        private int PurgeInvalidParkedCars()
        {
            if (m_ParkedCarQuery.IsEmptyIgnoreFilter) return 0;

            int purged = 0;
            using (UltraCoreEngine.Instance.Diagnostics.Measure("ParkingOptimizer"))
            {
                using var chunks = m_ParkedCarQuery.ToArchetypeChunkArray(Allocator.TempJob);
                var entityType = GetEntityTypeHandle();
                var parkedCarType = GetComponentTypeHandle<ParkedCar>(true);

                for (int c = 0; c < chunks.Length; c++)
                {
                    var chunk = chunks[c];
                    var entities = chunk.GetNativeArray(entityType);
                    var parkedCars = chunk.GetNativeArray(ref parkedCarType);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        Entity lane = parkedCars[i].m_Lane;
                        // If lane is Null or does not exist in EntityManager, the car is an orphaned ghost car blocking pathfind searches
                        if (lane == Entity.Null || !EntityManager.Exists(lane))
                        {
                            SafeVehicleDespawner.Despawn(EntityManager, entities[i]);
                            purged++;
                        }
                    }
                }
            }

            if (purged > 0)
            {
                log.Info($"[UltraPerformanceMod] Purged {purged} invalid/orphaned parked cars (Chunk-based).");
            }

            return purged;
        }
    }
}
