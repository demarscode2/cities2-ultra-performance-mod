using System;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using UltraPerformanceMod.Utils;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    /// <summary>
    /// Safely cleans up orphaned trailers, loose containers, trailer props, and delivery van props
    /// (e.g. "Trailer pequeno Prop", "Trailer intermodal prop", "trailer para tronco", "Reboque prop",
    /// "trailer de viagem", "vans de entrega") to eliminate the "yo-yo" glitch and boost simulation FPS.
    /// </summary>
    public class TrailerCleanupSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static TrailerCleanupSystem? Instance { get; private set; }

        private PrefabSystem m_PrefabSystem = default!;
        private EntityQuery m_CarTrailerAttachedQuery = default!;
        private EntityQuery m_CarTrailerOwnerQuery = default!;
        private EntityQuery m_StaticPropQuery = default!;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            // 1. Vehicle trailers with Attached component
            m_CarTrailerAttachedQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Vehicle>(),
                    ComponentType.ReadOnly<CarTrailer>(),
                    ComponentType.ReadOnly<Attached>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            // 2. Vehicle trailers with Owner component
            m_CarTrailerOwnerQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Vehicle>(),
                    ComponentType.ReadOnly<CarTrailer>(),
                    ComponentType.ReadOnly<Owner>()
                },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            // 3. Static/placed props with PrefabRef and Transform
            m_StaticPropQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<Transform>()
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Game.Buildings.Building>(),
                    ComponentType.ReadOnly<Game.Net.Node>(),
                    ComponentType.ReadOnly<Game.Net.Edge>()
                }
            });

            log.Info("[UltraPerformanceMod] Advanced TrailerCleanupSystem initialized.");
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
            if (m_FrameCounter % 64 != 0) return;

            PurgeOrphanedTrailers();
        }

        public int PurgeOrphanTrailersNow()
        {
            int vehicles = PurgeOrphanedTrailers();
            int props = PurgeTrailerAndVanProps();
            return vehicles + props;
        }

        public int PurgeAllTrailerAndVanPropsNow()
        {
            return PurgeOrphanTrailersNow();
        }

        private int PurgeOrphanedTrailers()
        {
            int purged = 0;

            // 1. Scan vehicle trailers with Attached component
            if (!m_CarTrailerAttachedQuery.IsEmptyIgnoreFilter)
            {
                using var entities = m_CarTrailerAttachedQuery.ToEntityArray(Allocator.TempJob);
                using var attachedData = m_CarTrailerAttachedQuery.ToComponentDataArray<Attached>(Allocator.TempJob);

                for (int i = 0; i < entities.Length; i++)
                {
                    Entity parent = attachedData[i].m_Parent;
                    if (parent == Entity.Null || !EntityManager.Exists(parent) || EntityManager.HasComponent<Deleted>(parent))
                    {
                        SafeVehicleDespawner.Despawn(EntityManager, entities[i]);
                        purged++;
                    }
                }
            }

            // 2. Scan vehicle trailers with Owner component
            if (!m_CarTrailerOwnerQuery.IsEmptyIgnoreFilter)
            {
                using var trailerEntities = m_CarTrailerOwnerQuery.ToEntityArray(Allocator.TempJob);
                using var ownerData = m_CarTrailerOwnerQuery.ToComponentDataArray<Owner>(Allocator.TempJob);

                for (int i = 0; i < trailerEntities.Length; i++)
                {
                    Entity owner = ownerData[i].m_Owner;
                    if (owner == Entity.Null || !EntityManager.Exists(owner) || EntityManager.HasComponent<Deleted>(owner))
                    {
                        SafeVehicleDespawner.Despawn(EntityManager, trailerEntities[i]);
                        purged++;
                    }
                }
            }

            if (purged > 0)
            {
                log.Info($"[UltraPerformanceMod] Cleaned up {purged} genuinely orphaned vehicle trailers.");
            }

            return purged;
        }

        public int PurgeTrailerAndVanProps()
        {
            if (m_StaticPropQuery.IsEmptyIgnoreFilter) return 0;

            using var entities = m_StaticPropQuery.ToEntityArray(Allocator.TempJob);
            using var prefabRefs = m_StaticPropQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob);

            int purged = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (!EntityManager.Exists(entity) || EntityManager.HasComponent<Deleted>(entity)) continue;

                string prefabName = m_PrefabSystem.GetPrefabName(prefabRefs[i].m_Prefab);
                if (string.IsNullOrEmpty(prefabName)) continue;

                string lower = prefabName.ToLowerInvariant();

                // Target: "Trailer pequeno Prop", "Trailer intermodal prop", "trailer para tronco", "Reboque prop", "trailer de viagem", "vans de entrega"
                if (lower.Contains("trailer") ||
                    lower.Contains("reboque") ||
                    lower.Contains("intermodal") ||
                    lower.Contains("tronco") ||
                    lower.Contains("logtrailer") ||
                    lower.Contains("caravan") ||
                    lower.Contains("traveltrailer") ||
                    lower.Contains("deliveryvan") ||
                    lower.Contains("delivery_van") ||
                    (lower.Contains("delivery") && lower.Contains("van")) ||
                    (lower.Contains("entrega") && lower.Contains("van")))
                {
                    if (EntityManager.HasComponent<Vehicle>(entity))
                    {
                        SafeVehicleDespawner.Despawn(EntityManager, entity);
                    }
                    else
                    {
                        EntityManager.AddComponent<Deleted>(entity);
                    }
                    purged++;
                }
            }

            if (purged > 0)
            {
                log.Info($"[UltraPerformanceMod] Purged {purged} trailer/reboque/van props from map.");
            }

            return purged;
        }
    }
}
