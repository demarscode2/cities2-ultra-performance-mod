using Game.Citizens;
using Game.Common;
using Game.Vehicles;
using Unity.Entities;

namespace UltraPerformanceMod.Utils
{
    /// <summary>
    /// Safely despawns vehicles by first evicting all passengers/drivers from CurrentTransport,
    /// deleting child trailers in LayoutElement, and cleaning attached SubObjects.
    /// This prevents native crashes in Burst movement/behavior jobs caused by dangling entity references.
    /// </summary>
    public static class SafeVehicleDespawner
    {
        public static void Despawn(EntityManager entityManager, Entity vehicle)
        {
            if (!entityManager.Exists(vehicle) || entityManager.HasComponent<Deleted>(vehicle)) return;

            // 1. Evict passengers and drivers cleanly
            if (entityManager.HasBuffer<Passenger>(vehicle))
            {
                var passengers = entityManager.GetBuffer<Passenger>(vehicle);
                for (int p = 0; p < passengers.Length; p++)
                {
                    Entity passenger = passengers[p].m_Passenger;
                    if (entityManager.Exists(passenger) && entityManager.HasComponent<CurrentTransport>(passenger))
                    {
                        entityManager.RemoveComponent<CurrentTransport>(passenger);
                    }
                }
            }

            // 2. Delete child trailers in LayoutElement
            if (entityManager.HasBuffer<LayoutElement>(vehicle))
            {
                var layout = entityManager.GetBuffer<LayoutElement>(vehicle);
                for (int j = 1; j < layout.Length; j++)
                {
                    Entity trailer = layout[j].m_Vehicle;
                    if (trailer != Entity.Null && entityManager.Exists(trailer) && !entityManager.HasComponent<Deleted>(trailer))
                    {
                        if (entityManager.HasBuffer<Passenger>(trailer))
                        {
                            var tPassengers = entityManager.GetBuffer<Passenger>(trailer);
                            for (int tp = 0; tp < tPassengers.Length; tp++)
                            {
                                Entity tPass = tPassengers[tp].m_Passenger;
                                if (entityManager.Exists(tPass) && entityManager.HasComponent<CurrentTransport>(tPass))
                                {
                                    entityManager.RemoveComponent<CurrentTransport>(tPass);
                                }
                            }
                        }
                        entityManager.AddComponent<Deleted>(trailer);
                    }
                }
            }

            // 3. Delete attached SubObjects (cargo containers, attachments)
            if (entityManager.HasBuffer<Game.Objects.SubObject>(vehicle))
            {
                var subObjects = entityManager.GetBuffer<Game.Objects.SubObject>(vehicle);
                for (int j = 0; j < subObjects.Length; j++)
                {
                    Entity sub = subObjects[j].m_SubObject;
                    if (sub != Entity.Null && entityManager.Exists(sub) && !entityManager.HasComponent<Deleted>(sub))
                    {
                        entityManager.AddComponent<Deleted>(sub);
                    }
                }
            }

            entityManager.AddComponent<Deleted>(vehicle);
        }
    }
}
