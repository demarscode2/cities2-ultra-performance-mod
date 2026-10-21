using Colossal.Logging;
using Game;
using Game.Common;
using Game.Vehicles;
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
                All = new ComponentType[] { ComponentType.ReadOnly<ParkedCar>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] ParkingOptimizerSystem initialized.");
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
            if (m_FrameCounter % 128 != 0) return;

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
            using var entities = m_ParkedCarQuery.ToEntityArray(Allocator.TempJob);
            using var parkedCars = m_ParkedCarQuery.ToComponentDataArray<ParkedCar>(Allocator.TempJob);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity lane = parkedCars[i].m_Lane;
                // If lane is Null or does not exist in EntityManager, the car is an orphaned ghost car blocking pathfind searches
                if (lane == Entity.Null || !EntityManager.Exists(lane))
                {
                    UltraPerformanceMod.Utils.SafeVehicleDespawner.Despawn(EntityManager, entities[i]);
                    purged++;
                }
            }

            if (purged > 0)
            {
                log.Info($"[UltraPerformanceMod] Purged {purged} invalid/orphaned parked cars.");
            }

            return purged;
        }
    }
}
