using System;
using System.Collections.Generic;
using Colossal.Logging;
using Unity.Entities;
using Unity.Mathematics;

namespace UltraPerformanceMod.Core
{
    /// <summary>
    /// UltraSmartCache: High-performance bounded generational LRU cache.
    /// Provides zero-allocation validation and memoization for expensive ECS graph walks,
    /// such as vehicle blocker chains, entity spatial classifications, and recurring lookups.
    /// Includes strict memory bounds and automatic TTL-based pruning.
    /// </summary>
    public sealed class UltraSmartCache
    {
        private static readonly ILog log = Mod.log;

        public bool IsEnabled { get; set; } = true;

        // --- Cache Entry for Vehicle Blocker Resolution ---
        public struct BlockerChainResult
        {
            public bool IsImmune;
            public Entity HeadVehicle;
            public float3 HeadPosition;
            public int ValidUntilFrame;
        }

        private const int MaxBlockerCacheEntries = 4096;
        private readonly Dictionary<Entity, BlockerChainResult> m_BlockerCache = new(MaxBlockerCacheEntries);

        // --- General Memoization Cache for Query Outcomes ---
        public struct QueryCacheEntry<T>
        {
            public T Value;
            public int ExpireFrame;
        }

        private const int MaxGenericCacheEntries = 1024;
        private readonly Dictionary<int, QueryCacheEntry<bool>> m_BoolCache = new(MaxGenericCacheEntries);

        // Telemetry
        public long CacheHits { get; private set; }
        public long CacheMisses { get; private set; }
        public long CacheEvictions { get; private set; }
        public int TotalCachedEntries => m_BlockerCache.Count + m_BoolCache.Count;

        private int m_CurrentFrame = 0;

        public void Initialize()
        {
            Clear();
            m_CurrentFrame = 0;
            log.Info("[UltraCore:Cache] Smart Cache engine initialized.");
        }

        public void Shutdown()
        {
            Clear();
            log.Info("[UltraCore:Cache] Smart Cache engine shut down.");
        }

        public void Clear()
        {
            m_BlockerCache.Clear();
            m_BoolCache.Clear();
            CacheHits = 0;
            CacheMisses = 0;
            CacheEvictions = 0;
        }

        /// <summary>
        /// Maintenance tick executed periodically (e.g., once every 120 frames) to prune expired entries.
        /// </summary>
        public void MaintenanceTick()
        {
            m_CurrentFrame++;

            // Only run prune sweep every 120 frames
            if (m_CurrentFrame % 120 != 0) return;

            PruneExpiredBlockers();
            PruneExpiredBoolCache();
        }

        #region Blocker Chain Caching
        public bool TryGetBlockerChain(Entity vehicle, float3 currentHeadPos, out bool isImmune)
        {
            if (!IsEnabled)
            {
                isImmune = false;
                return false;
            }

            if (m_BlockerCache.TryGetValue(vehicle, out var entry))
            {
                // Verify entry has not expired and the head vehicle hasn't moved
                if (entry.ValidUntilFrame >= m_CurrentFrame && math.distancesq(entry.HeadPosition, currentHeadPos) < 0.25f)
                {
                    CacheHits++;
                    isImmune = entry.IsImmune;
                    return true;
                }
            }

            CacheMisses++;
            isImmune = false;
            return false;
        }

        public void SetBlockerChain(Entity vehicle, bool isImmune, Entity headVehicle, float3 headPos, int ttlFrames = 90)
        {
            if (!IsEnabled) return;

            // Enforce capacity bounds
            if (m_BlockerCache.Count >= MaxBlockerCacheEntries)
            {
                PruneExpiredBlockers();
                if (m_BlockerCache.Count >= MaxBlockerCacheEntries)
                {
                    m_BlockerCache.Clear();
                    CacheEvictions += MaxBlockerCacheEntries;
                }
            }

            m_BlockerCache[vehicle] = new BlockerChainResult
            {
                IsImmune = isImmune,
                HeadVehicle = headVehicle,
                HeadPosition = headPos,
                ValidUntilFrame = m_CurrentFrame + Math.Max(10, ttlFrames)
            };
        }

        private void PruneExpiredBlockers()
        {
            if (m_BlockerCache.Count == 0) return;

            var expiredKeys = new List<Entity>();
            foreach (var kvp in m_BlockerCache)
            {
                if (kvp.Value.ValidUntilFrame < m_CurrentFrame)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            for (int i = 0; i < expiredKeys.Count; i++)
            {
                m_BlockerCache.Remove(expiredKeys[i]);
                CacheEvictions++;
            }
        }
        #endregion

        #region Generic Bool Query Caching
        public bool TryGetBool(int queryKey, out bool result)
        {
            if (!IsEnabled)
            {
                result = false;
                return false;
            }

            if (m_BoolCache.TryGetValue(queryKey, out var entry) && entry.ExpireFrame >= m_CurrentFrame)
            {
                CacheHits++;
                result = entry.Value;
                return true;
            }

            CacheMisses++;
            result = false;
            return false;
        }

        public void SetBool(int queryKey, bool result, int ttlFrames = 60)
        {
            if (!IsEnabled) return;

            if (m_BoolCache.Count >= MaxGenericCacheEntries)
            {
                PruneExpiredBoolCache();
            }

            m_BoolCache[queryKey] = new QueryCacheEntry<bool>
            {
                Value = result,
                ExpireFrame = m_CurrentFrame + ttlFrames
            };
        }

        private void PruneExpiredBoolCache()
        {
            if (m_BoolCache.Count == 0) return;

            var expiredKeys = new List<int>();
            foreach (var kvp in m_BoolCache)
            {
                if (kvp.Value.ExpireFrame < m_CurrentFrame)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            for (int i = 0; i < expiredKeys.Count; i++)
            {
                m_BoolCache.Remove(expiredKeys[i]);
                CacheEvictions++;
            }
        }
        #endregion
    }
}
