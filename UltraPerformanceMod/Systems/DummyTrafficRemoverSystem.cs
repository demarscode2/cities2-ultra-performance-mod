using Colossal.Logging;
using Game;
using Game.Common;
using Game.Vehicles;
using UltraPerformanceMod.Utils;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    public class DummyTrafficRemoverSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static DummyTrafficRemoverSystem? Instance { get; private set; }

        private EntityQuery m_PersonalCarQuery = default!;
        private EntityQuery m_DeliveryTruckQuery = default!;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_PersonalCarQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<PersonalCar>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_DeliveryTruckQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<DeliveryTruck>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            log.Info("[UltraPerformanceMod] DummyTrafficRemoverSystem initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null || !Mod.Settings.DisableDummyTraffic) return;

            m_FrameCounter++;
            if (m_FrameCounter % 64 != 0) return;

            PurgeDummyTraffic();
        }

        public int PurgeDummyTrafficNow()
        {
            return PurgeDummyTraffic();
        }

        private int PurgeDummyTraffic()
        {
            int purged = 0;

            if (!m_PersonalCarQuery.IsEmptyIgnoreFilter)
            {
                using var carEntities = m_PersonalCarQuery.ToEntityArray(Allocator.TempJob);
                using var carData = m_PersonalCarQuery.ToComponentDataArray<PersonalCar>(Allocator.TempJob);

                for (int i = 0; i < carEntities.Length; i++)
                {
                    if ((carData[i].m_State & PersonalCarFlags.DummyTraffic) != 0)
                    {
                        SafeVehicleDespawner.Despawn(EntityManager, carEntities[i]);
                        purged++;
                    }
                }
            }

            if (!m_DeliveryTruckQuery.IsEmptyIgnoreFilter)
            {
                using var truckEntities = m_DeliveryTruckQuery.ToEntityArray(Allocator.TempJob);
                using var truckData = m_DeliveryTruckQuery.ToComponentDataArray<DeliveryTruck>(Allocator.TempJob);

                for (int i = 0; i < truckEntities.Length; i++)
                {
                    if ((truckData[i].m_State & DeliveryTruckFlags.DummyTraffic) != 0)
                    {
                        SafeVehicleDespawner.Despawn(EntityManager, truckEntities[i]);
                        purged++;
                    }
                }
            }

            if (purged > 0)
            {
                log.Info($"[UltraPerformanceMod] Purged {purged} dummy vehicles safely.");
            }

            return purged;
        }
    }
}
