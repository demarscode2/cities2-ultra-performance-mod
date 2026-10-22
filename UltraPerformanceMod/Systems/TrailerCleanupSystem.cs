using System;
using System.Collections.Generic;
using Colossal.Logging;
using Game;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using UltraPerformanceMod.Core;
using UltraPerformanceMod.Utils;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Systems
{
    /// <summary>
    /// Safely cleans up orphaned trailers, loose containers, trailer props, and delivery van props
    /// (e.g. "Trailer pequeno Prop", "Trailer intermodal prop", "trailer para tronco", "Reboque prop",
    /// "trailer de viagem", "vans de entrega") to eliminate the "yo-yo" glitch and boost simulation FPS.
    /// Uses archetype chunk iteration and prefab memoization cache to prevent memory churn.
    /// </summary>
    public class TrailerCleanupSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static TrailerCleanupSystem? Instance { get; private set; }

        private PrefabSystem m_PrefabSystem = default!;
        private EntityQuery m_CarTrailerAttachedQuery = default!;
        private EntityQuery m_CarTrailerOwnerQuery = default!;
        private EntityQuery m_StaticPropQuery = default!;
        private readonly Dictionary<Entity, bool> m_PrefabMatchCache = new();
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
                    ComponentType.ReadOnly<Vehicle>(),
                    ComponentType.ReadOnly<Game.Buildings.Building>(),
                    ComponentType.ReadOnly<Game.Net.Node>(),
                    ComponentType.ReadOnly<Game.Net.Edge>(),
                    ComponentType.ReadOnly<Deleted>()
                }
            });

            log.Info("[UltraPerformanceMod] High-Performance TrailerCleanupSystem initialized.");
        }

        protected override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            m_PrefabMatchCache.Clear();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null) return;

            m_FrameCounter++;
            float intervalScale = UltraCoreEngine.Instance.AutoTuner.TaskIntervalScale;
            int scaledInterval = (int)(256 * intervalScale);
            if (m_FrameCounter % Math.Max(64, scaledInterval) != 0) return;

            PurgeOrphanVehicleTrailers();
        }

        public int PurgeOrphanTrailersNow()
        {
            return ClearAllTrailersNow();
        }

        public int PurgeAllTrailerAndVanPropsNow()
        {
            return ClearAllTrailersNow();
        }

        public int ClearAllTrailersNow()
        {
            using (UltraCoreEngine.Instance.Diagnostics.Measure("TrailerCleanup"))
            {
                int vehicleTrailers = PurgeOrphanVehicleTrailers();
                int propTrailers = PurgeTrailerAndVanProps();
                return vehicleTrailers + propTrailers;
            }
        }

        public int PurgeOrphanVehicleTrailers()
        {
            int purged = 0;

            // 1. Scan vehicle trailers with Attached component
            if (!m_CarTrailerAttachedQuery.IsEmptyIgnoreFilter)
            {
                using var chunks = m_CarTrailerAttachedQuery.ToArchetypeChunkArray(Allocator.TempJob);
                var entityType = GetEntityTypeHandle();
                var attachedType = GetComponentTypeHandle<Attached>(true);

                for (int c = 0; c < chunks.Length; c++)
                {
                    var chunk = chunks[c];
                    var entities = chunk.GetNativeArray(entityType);
                    var attached = chunk.GetNativeArray(ref attachedType);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        Entity parent = attached[i].m_Parent;
                        if (parent == Entity.Null || !EntityManager.Exists(parent) || EntityManager.HasComponent<Deleted>(parent))
                        {
                            SafeVehicleDespawner.Despawn(EntityManager, entities[i]);
                            purged++;
                        }
                    }
                }
            }

            // 2. Scan vehicle trailers with Owner component
            if (!m_CarTrailerOwnerQuery.IsEmptyIgnoreFilter)
            {
                using var chunks = m_CarTrailerOwnerQuery.ToArchetypeChunkArray(Allocator.TempJob);
                var entityType = GetEntityTypeHandle();
                var ownerType = GetComponentTypeHandle<Owner>(true);

                for (int c = 0; c < chunks.Length; c++)
                {
                    var chunk = chunks[c];
                    var entities = chunk.GetNativeArray(entityType);
                    var owners = chunk.GetNativeArray(ref ownerType);

                    for (int i = 0; i < chunk.Count; i++)
                    {
                        Entity owner = owners[i].m_Owner;
                        if (owner == Entity.Null || !EntityManager.Exists(owner) || EntityManager.HasComponent<Deleted>(owner))
                        {
                            SafeVehicleDespawner.Despawn(EntityManager, entities[i]);
                            purged++;
                        }
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

            using var chunks = m_StaticPropQuery.ToArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetEntityTypeHandle();
            var prefabType = GetComponentTypeHandle<PrefabRef>(true);

            int purged = 0;
            for (int c = 0; c < chunks.Length; c++)
            {
                var chunk = chunks[c];
                var entities = chunk.GetNativeArray(entityType);
                var prefabs = chunk.GetNativeArray(ref prefabType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entities[i];
                    if (!EntityManager.Exists(entity) || EntityManager.HasComponent<Deleted>(entity)) continue;

                    if (IsTargetPropPrefab(prefabs[i].m_Prefab))
                    {
                        EntityManager.AddComponent<Deleted>(entity);
                        purged++;
                    }
                }
            }

            if (purged > 0)
            {
                log.Info($"[UltraPerformanceMod] Purged {purged} loose trailer/van props.");
            }

            return purged;
        }

        private bool IsTargetPropPrefab(Entity prefab)
        {
            if (m_PrefabMatchCache.TryGetValue(prefab, out bool match)) return match;

            string prefabName = m_PrefabSystem.GetPrefabName(prefab);
            if (string.IsNullOrEmpty(prefabName))
            {
                m_PrefabMatchCache[prefab] = false;
                return false;
            }

            string lower = prefabName.ToLowerInvariant();
            match = lower.Contains("trailer") ||
                    lower.Contains("reboque") ||
                    lower.Contains("intermodal") ||
                    lower.Contains("tronco") ||
                    lower.Contains("logtrailer") ||
                    lower.Contains("caravan") ||
                    lower.Contains("traveltrailer") ||
                    lower.Contains("deliveryvan") ||
                    lower.Contains("delivery_van") ||
                    (lower.Contains("delivery") && lower.Contains("van")) ||
                    (lower.Contains("entrega") && lower.Contains("van"));

            m_PrefabMatchCache[prefab] = match;
            return match;
        }
    }
}
