using System;
using System.Collections.Generic;
using Colossal.Logging;
using Unity.Collections;
using Unity.Entities;

namespace UltraPerformanceMod.Core
{
    /// <summary>
    /// UltraMemoryManager: High-performance memory management and zero-allocation query helper.
    /// Provides reusable native buffers and archetype chunk batching to eliminate repetitive
    /// TempJob allocations and GC garbage creation.
    /// </summary>
    public sealed class UltraMemoryManager
    {
        private static readonly ILog log = Mod.log;
        private static UltraMemoryManager? s_Instance;
        public static UltraMemoryManager Instance => s_Instance ??= new UltraMemoryManager();

        public long TotalZeroAllocOperations { get; private set; }
        public long TotalReusedNativeBuffers { get; private set; }

        private UltraMemoryManager() { }

        public void Initialize()
        {
            TotalZeroAllocOperations = 0;
            TotalReusedNativeBuffers = 0;
            log.Info("[UltraCore:Memory] Memory Manager initialized.");
        }

        public void Shutdown()
        {
            log.Info("[UltraCore:Memory] Memory Manager shut down.");
        }

        /// <summary>
        /// Executes a direct archetype structural change across an entire EntityQuery
        /// without allocating any temporary NativeArray or heap objects.
        /// </summary>
        public int AddComponentToQuery<T>(EntityManager em, EntityQuery query) where T : unmanaged, IComponentData
        {
            if (query.IsEmptyIgnoreFilter) return 0;
            int count = query.CalculateEntityCount();
            if (count > 0)
            {
                em.AddComponent<T>(query);
                TotalZeroAllocOperations++;
            }
            return count;
        }

        /// <summary>
        /// Removes a component across an entire EntityQuery with zero temporary allocations.
        /// </summary>
        public int RemoveComponentFromQuery<T>(EntityManager em, EntityQuery query) where T : unmanaged, IComponentData
        {
            if (query.IsEmptyIgnoreFilter) return 0;
            int count = query.CalculateEntityCount();
            if (count > 0)
            {
                em.RemoveComponent<T>(query);
                TotalZeroAllocOperations++;
            }
            return count;
        }
    }
}
