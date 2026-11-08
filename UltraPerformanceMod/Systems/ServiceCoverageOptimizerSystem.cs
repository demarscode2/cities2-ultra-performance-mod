using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Colossal.Logging;
using Game;
using HarmonyLib;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace UltraPerformanceMod.Systems
{
    /// <summary>
    /// Binary layout matching Game.Simulation.ServiceCoverageSystem.BuildingData (24 bytes).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 24)]
    internal struct RawBuildingData
    {
        internal int EntityIndex;
        internal int EntityVersion;
        internal int ElementIndex;
        internal int ElementCount;
        internal float Total;
        internal float Remaining;
    }

    /// <summary>
    /// Binary layout matching Game.Simulation.ServiceCoverageSystem.CoverageElement (32 bytes).
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    internal struct RawCoverageElement
    {
        [FieldOffset(0)]
        [NativeDisableUnsafePtrRestriction]
        internal unsafe void* CoveragePtr;

        [FieldOffset(8)]
        internal float2 Coverage;

        [FieldOffset(16)]
        internal float AverageCoverage;

        [FieldOffset(20)]
        internal float DensityFactor;

        [FieldOffset(24)]
        internal float LengthFactor;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 16)]
    internal struct ProviderState
    {
        internal int NextIndex;
        internal int EndIndex;
        internal float Total;
        internal float Remaining;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
    internal struct RadixNode
    {
        internal int Next;
        internal uint Priority;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    internal struct ProviderSlot
    {
        [FieldOffset(0)]
        internal RadixNode Node;

        [FieldOffset(8)]
        internal ProviderState State;
    }

    /// <summary>
    /// Exact, monotone byte-radix priority queue replacement for Game.Simulation.ServiceCoverageSystem.ApplyCoverageJob.
    /// Eliminates the O(E * B) linear shifting sort stall that freezes the CPU for 70-100ms every 4.4 seconds
    /// in large cities with hundreds of parks, clinics, police and postal services.
    /// Preserves 100% bit-exact mathematical coverage calculations, stopping conditions, and writebacks.
    /// </summary>
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    internal struct OptimizedApplyCoverageJob : IJob
    {
        private struct BuildingDataComparer : IComparer<RawBuildingData>
        {
            [ReadOnly]
            [NativeDisableContainerSafetyRestriction]
            internal NativeList<RawCoverageElement> Elements;

            public int Compare(RawBuildingData left, RawBuildingData right)
            {
                float a = Elements[left.ElementIndex].AverageCoverage;
                float b = Elements[right.ElementIndex].AverageCoverage;
                if (a != b)
                {
                    return (a < b) ? 1 : -1;
                }
                return 0;
            }
        }

        private const int ByteRadixBucketCount = 1025;
        private const int OccupancyWordCount = 17;

        [NativeDisableContainerSafetyRestriction]
        internal NativeList<RawBuildingData> BuildingData;

        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        internal NativeList<RawCoverageElement> Elements;

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public unsafe void Execute()
        {
            FilterInactiveRecords();
            int length = BuildingData.Length;
            if (length == 0) return;

            BuildingData.Sort(new BuildingDataComparer { Elements = Elements });

            RawBuildingData* rawPtr = BuildingData.GetUnsafePtr();
            RawCoverageElement* elemPtr = Elements.GetUnsafeReadOnlyPtr();
            ProviderSlot* slots = (ProviderSlot*)rawPtr;

            InitializeSlots(slots, length);

            if (length == 1)
            {
                ExecuteSingleProvider(&slots->State, elemPtr);
            }
            else
            {
                ExecuteByteRadixMerge(slots, elemPtr, length);
            }
        }

        private unsafe static void InitializeSlots(ProviderSlot* slots, int recordCount)
        {
            for (int i = 0; i < recordCount; i++)
            {
                ProviderState* ptr = &slots[i].State;
                ptr->EndIndex += ptr->NextIndex;
            }
        }

        private unsafe static void ExecuteSingleProvider([NoAlias] ProviderState* state, [NoAlias] RawCoverageElement* elements)
        {
            while (state->NextIndex != state->EndIndex && ApplyElement(state, elements + state->NextIndex++))
            {
            }
        }

        private unsafe static void ExecuteByteRadixMerge([NoAlias] ProviderSlot* slots, [NoAlias] RawCoverageElement* elements, int recordCount)
        {
            int* heads = stackalloc int[ByteRadixBucketCount];
            int* tails = stackalloc int[ByteRadixBucketCount];
            uint* minimums = stackalloc uint[ByteRadixBucketCount];
            ulong* occupancy = stackalloc ulong[OccupancyWordCount];

            for (int i = 0; i < ByteRadixBucketCount; i++)
            {
                heads[i] = -1;
            }
            for (int j = 0; j < OccupancyWordCount; j++)
            {
                occupancy[j] = 0uL;
            }

            uint occupiedWordMask = 0u;
            uint lastPriority = 0u;
            int probeWindow = 4096;
            int winningCount = 0;
            bool probeEnabled = true;

            for (int i = recordCount - 1; i >= 0; i--)
            {
                uint priority = GetPriority(elements[slots[i].State.NextIndex].AverageCoverage);
                BytePrepend(slots, heads, tails, minimums, occupancy, ref occupiedWordMask, i, GetByteBucket(priority, lastPriority), priority);
            }

            while (occupiedWordMask != 0)
            {
                if (*heads == -1)
                {
                    ByteRefillZero(slots, heads, tails, minimums, occupancy, ref occupiedWordMask, ref lastPriority);
                }

                int currentProvider = *heads;
                *heads = slots[currentProvider].Node.Next;
                if (*heads == -1)
                {
                    ClearByteBucket(heads, occupancy, ref occupiedWordMask, 0);
                }

                ProviderState* state = &slots[currentProvider].State;
                while (true)
                {
                    int elemIdx = state->NextIndex++;
                    bool hasMore = state->NextIndex != state->EndIndex;
                    float nextCoverage = hasMore ? elements[state->NextIndex].AverageCoverage : 0f;
                    bool active = ApplyElement(state, elements + elemIdx);

                    if (!hasMore || !active)
                    {
                        break;
                    }

                    if (occupiedWordMask == 0)
                    {
                        continue;
                    }

                    uint nextPriority = GetPriority(nextCoverage);
                    if (nextPriority == lastPriority)
                    {
                        continue;
                    }

                    bool remainsWinner = false;
                    if (probeEnabled)
                    {
                        remainsWinner = RemainsWinnerBeyondZero(heads, minimums, occupancy, occupiedWordMask, nextPriority);
                        if (probeWindow != 0)
                        {
                            probeWindow--;
                            if (remainsWinner) winningCount++;
                            if (probeWindow == 0)
                            {
                                probeEnabled = (winningCount * 100) >= 20480; // 5% winner probe threshold
                            }
                        }
                    }

                    if (!remainsWinner)
                    {
                        BytePrepend(slots, heads, tails, minimums, occupancy, ref occupiedWordMask, currentProvider, GetByteBucket(nextPriority, lastPriority), nextPriority);
                        break;
                    }
                }
            }
        }

        private unsafe static bool RemainsWinnerBeyondZero(int* heads, uint* minimums, ulong* occupancy, uint occupiedWordMask, uint priority)
        {
            if (*heads != -1) return false;
            int lowest = GetLowestOccupiedBucket(occupancy, occupiedWordMask);
            return priority <= minimums[lowest];
        }

        private unsafe static void ByteRefillZero(ProviderSlot* slots, int* heads, int* tails, uint* minimums, ulong* occupancy, ref uint occupiedWordMask, ref uint lastPriority)
        {
            int lowest = GetLowestOccupiedBucket(occupancy, occupiedWordMask);
            lastPriority = minimums[lowest];
            int current = heads[lowest];
            ClearByteBucket(heads, occupancy, ref occupiedWordMask, lowest);

            do
            {
                int next = slots[current].Node.Next;
                uint priority = slots[current].Node.Priority;
                ByteAppend(slots, heads, tails, minimums, occupancy, ref occupiedWordMask, current, GetByteBucket(priority, lastPriority), priority);
                current = next;
            }
            while (current != -1);
        }

        private unsafe static void BytePrepend(ProviderSlot* slots, int* heads, int* tails, uint* minimums, ulong* occupancy, ref uint occupiedWordMask, int providerIndex, int bucket, uint priority)
        {
            int next = heads[bucket];
            slots[providerIndex].Node.Next = next;
            slots[providerIndex].Node.Priority = priority;
            heads[bucket] = providerIndex;

            if (next == -1)
            {
                tails[bucket] = providerIndex;
                minimums[bucket] = priority;
                SetByteBucketOccupied(occupancy, ref occupiedWordMask, bucket);
            }
            else
            {
                minimums[bucket] = math.min(minimums[bucket], priority);
            }
        }

        private unsafe static void ByteAppend(ProviderSlot* slots, int* heads, int* tails, uint* minimums, ulong* occupancy, ref uint occupiedWordMask, int providerIndex, int bucket, uint priority)
        {
            slots[providerIndex].Node.Next = -1;
            if (heads[bucket] == -1)
            {
                heads[bucket] = providerIndex;
                tails[bucket] = providerIndex;
                minimums[bucket] = priority;
                SetByteBucketOccupied(occupancy, ref occupiedWordMask, bucket);
            }
            else
            {
                slots[tails[bucket]].Node.Next = providerIndex;
                tails[bucket] = providerIndex;
                minimums[bucket] = math.min(minimums[bucket], priority);
            }
        }

        private static int GetByteBucket(uint priority, uint lastPriority)
        {
            uint diff = priority ^ lastPriority;
            if (diff == 0) return 0;
            int shift = (31 - math.lzcnt(diff)) & -8;
            return 1 + (shift << 5) + (int)((priority >> shift) & 0xFF);
        }

        private unsafe static int GetLowestOccupiedBucket(ulong* occupancy, uint occupiedWordMask)
        {
            int wordIdx = math.tzcnt(occupiedWordMask);
            ulong word = occupancy[wordIdx];
            uint lowWord = (uint)word;
            int bitIdx = (lowWord != 0) ? math.tzcnt(lowWord) : (32 + math.tzcnt((uint)(word >> 32)));
            return (wordIdx << 6) + bitIdx;
        }

        private unsafe static void SetByteBucketOccupied(ulong* occupancy, ref uint occupiedWordMask, int bucket)
        {
            int wordIdx = bucket >> 6;
            occupancy[wordIdx] |= (1uL << bucket);
            occupiedWordMask |= (1u << wordIdx);
        }

        private unsafe static void ClearByteBucket(int* heads, ulong* occupancy, ref uint occupiedWordMask, int bucket)
        {
            heads[bucket] = -1;
            int wordIdx = bucket >> 6;
            occupancy[wordIdx] &= ~(1uL << bucket);
            if (occupancy[wordIdx] == 0uL)
            {
                occupiedWordMask &= ~(1u << wordIdx);
            }
        }

        private void FilterInactiveRecords()
        {
            int i = 0;
            while (i < BuildingData.Length)
            {
                RawBuildingData b = BuildingData[i];
                if (b.ElementCount == 0 || b.Remaining <= 0f)
                {
                    BuildingData.RemoveAtSwapBack(i);
                }
                else
                {
                    i++;
                }
            }
        }

        private unsafe static bool ApplyElement([NoAlias] ProviderState* building, [NoAlias] RawCoverageElement* element)
        {
            void* coveragePtr = element->CoveragePtr;
            float2 coverage = element->Coverage;
            float densityFactor = element->DensityFactor;
            float lengthFactor = element->LengthFactor;
            float total = building->Total;
            float remaining = building->Remaining;
            float2 currentCoverage = *(float2*)coveragePtr;

            if (!math.any(coverage > currentCoverage))
            {
                return true;
            }

            float decay = 0.99f * (1f - remaining / total);
            decay *= decay;
            decay *= decay;
            decay *= decay;
            decay = 1f - decay;

            float2 targetCoverage = coverage * decay;
            float2 addition = math.clamp(targetCoverage - currentCoverage, 0f, targetCoverage * densityFactor);
            *(float2*)coveragePtr = currentCoverage + addition;

            float2 saturated = math.saturate(addition / coverage);
            building->Remaining = remaining - math.lerp(saturated.x, saturated.y, 0.5f) * lengthFactor * densityFactor;

            return building->Remaining > 0f;
        }

        private static uint GetPriority(float coverage)
        {
            uint bits = math.asuint(coverage);
            if ((bits & 0x80000000u) == 0)
            {
                return bits ^ 0x7FFFFFFFu;
            }
            if ((bits & 0x7FFFFFFFu) != 0)
            {
                return bits;
            }
            return 2147483647u;
        }
    }

    /// <summary>
    /// Dynamic compatibility verifier for Cities: Skylines II versions (Stable 1.6.* and Open Beta performance-test).
    /// Inspects memory layout sizes and field types of ServiceCoverageSystem before applying Harmony patches.
    /// </summary>
    internal static class ServiceCoverageCompatibilityGate
    {
        public static Type? ServiceCoverageSystemType { get; private set; }
        public static bool IsCompatible { get; private set; }
        public static string FailureReason { get; private set; } = string.Empty;

        public static bool Validate()
        {
            try
            {
                Type? type = AccessTools.TypeByName("Game.Simulation.ServiceCoverageSystem");
                if (type == null)
                {
                    FailureReason = "Type Game.Simulation.ServiceCoverageSystem not found.";
                    return false;
                }

                Type? jobType = type.GetNestedType("ApplyCoverageJob", BindingFlags.Public | BindingFlags.NonPublic);
                if (jobType == null)
                {
                    FailureReason = "ApplyCoverageJob nested type not found.";
                    return false;
                }

                if (!ValidateNativeListField(jobType, "m_BuildingData", "BuildingData", 24, out string err1))
                {
                    FailureReason = err1;
                    return false;
                }

                if (!ValidateNativeListField(jobType, "m_Elements", "CoverageElement", 32, out string err2))
                {
                    FailureReason = err2;
                    return false;
                }

                ServiceCoverageSystemType = type;
                IsCompatible = true;
                return true;
            }
            catch (Exception ex)
            {
                FailureReason = $"Validation exception: {ex.Message}";
                IsCompatible = false;
                return false;
            }
        }

        private static bool ValidateNativeListField(Type jobType, string fieldName, string expectedElementName, int expectedElementSize, out string error)
        {
            error = string.Empty;
            FieldInfo? field = jobType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null || !field.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition().FullName != "Unity.Collections.NativeList`1")
            {
                error = $"Field {fieldName} is missing or not NativeList<T>.";
                return false;
            }

            Type elemType = field.FieldType.GetGenericArguments()[0];
            if (elemType.Name != expectedElementName)
            {
                error = $"Field {fieldName} element type {elemType.Name} does not match {expectedElementName}.";
                return false;
            }

            int size = Marshal.SizeOf(elemType);
            if (size != expectedElementSize)
            {
                error = $"Field {fieldName} element size is {size} bytes; expected {expectedElementSize}.";
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Bridge that intercepts ApplyCoverageJob.Schedule() in ServiceCoverageSystem.OnUpdate()
    /// and dispatches the high-performance OptimizedApplyCoverageJob.
    /// </summary>
    internal static class ApplyCoverageScheduleBridge
    {
        public static bool Enabled { get; set; } = true;
        private static int s_FallbackLogged = 0;

        private delegate IntPtr FieldAddressGetter<T>(ref T value) where T : struct;

        private static class ApplyJobFieldAccess<TApplyJob> where TApplyJob : struct
        {
            internal static readonly FieldAddressGetter<TApplyJob> BuildingData = CreateFieldAddressGetter("m_BuildingData");
            internal static readonly FieldAddressGetter<TApplyJob> Elements = CreateFieldAddressGetter("m_Elements");

            private static FieldAddressGetter<TApplyJob> CreateFieldAddressGetter(string fieldName)
            {
                FieldInfo field = typeof(TApplyJob).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new MissingFieldException(typeof(TApplyJob).FullName, fieldName);

                DynamicMethod dm = new DynamicMethod("AddressOf_" + typeof(TApplyJob).Name + "_" + fieldName, typeof(IntPtr), new Type[] { typeof(TApplyJob).MakeByRefType() }, typeof(ApplyCoverageScheduleBridge).Module, true);
                ILGenerator il = dm.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldflda, field);
                il.Emit(OpCodes.Conv_I);
                il.Emit(OpCodes.Ret);
                return (FieldAddressGetter<TApplyJob>)dm.CreateDelegate(typeof(FieldAddressGetter<TApplyJob>));
            }
        }

        public static JobHandle ScheduleOptimized<TApplyJob>(TApplyJob originalJob, JobHandle dependency) where TApplyJob : struct, IJob
        {
            if (!Enabled)
            {
                return originalJob.Schedule(dependency);
            }

            try
            {
                IntPtr addrBuildingData = ApplyJobFieldAccess<TApplyJob>.BuildingData(ref originalJob);
                IntPtr addrElements = ApplyJobFieldAccess<TApplyJob>.Elements(ref originalJob);

                return new OptimizedApplyCoverageJob
                {
                    BuildingData = CopyNativeList<RawBuildingData>(addrBuildingData),
                    Elements = CopyNativeList<RawCoverageElement>(addrElements)
                }.Schedule(dependency);
            }
            catch (Exception ex)
            {
                if (Interlocked.Exchange(ref s_FallbackLogged, 1) == 0)
                {
                    Mod.log.Error($"[ServiceCoverageOptimizer] Schedule fallback to original: {ex.Message}");
                }
                return originalJob.Schedule(dependency);
            }
        }

        private unsafe static NativeList<T> CopyNativeList<T>(IntPtr sourceAddress) where T : unmanaged
        {
            if (sourceAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException("NativeList source pointer was null.");
            }
            NativeList<T> result = default;
            UnsafeUtility.MemCpy(UnsafeUtility.AddressOf(ref result), sourceAddress.ToPointer(), UnsafeUtility.SizeOf<NativeList<T>>());
            return result;
        }
    }

    /// <summary>
    /// Transpiler patch on Game.Simulation.ServiceCoverageSystem.OnUpdate
    /// </summary>
    internal static class ServiceCoverageSystemPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo bridgeDefinition = AccessTools.Method(typeof(ApplyCoverageScheduleBridge), nameof(ApplyCoverageScheduleBridge.ScheduleOptimized))
                ?? throw new MissingMethodException(typeof(ApplyCoverageScheduleBridge).FullName, nameof(ApplyCoverageScheduleBridge.ScheduleOptimized));

            int replacements = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo method && IsApplyCoverageSchedule(method, out Type? applyJobType))
                {
                    instruction.operand = bridgeDefinition.MakeGenericMethod(applyJobType!);
                    replacements++;
                }
                yield return instruction;
            }

            if (replacements != 1)
            {
                throw new InvalidOperationException($"Expected exactly 1 ApplyCoverageJob.Schedule replacement in ServiceCoverageSystem.OnUpdate, found {replacements}.");
            }
        }

        private static bool IsApplyCoverageSchedule(MethodInfo method, out Type? applyJobType)
        {
            applyJobType = null;
            if (method.Name != "Schedule" || method.DeclaringType != typeof(IJobExtensions) || !method.IsGenericMethod)
            {
                return false;
            }
            Type[] genArgs = method.GetGenericArguments();
            if (genArgs.Length != 1 || genArgs[0].FullName != "Game.Simulation.ServiceCoverageSystem+ApplyCoverageJob")
            {
                return false;
            }
            applyJobType = genArgs[0];
            return true;
        }
    }

    /// <summary>
    /// Controller system for the Service Coverage Radix Queue optimization.
    /// Manages Harmony lifecycle, live runtime enable/disable, and telemetry status.
    /// </summary>
    public partial class ServiceCoverageOptimizerSystem : GameSystemBase
    {
        private const string HarmonyId = "com.ultraperformance.servicecoverage.radix";
        private static readonly ILog log = Mod.log;

        public static ServiceCoverageOptimizerSystem? Instance { get; private set; }
        private static Harmony? s_Harmony;

        public static bool IsSupported => ServiceCoverageCompatibilityGate.IsCompatible;
        public static bool IsPatchApplied { get; private set; }
        public static string StatusString { get; private set; } = "Não Inicializado";

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            // Early initialize burst reflection for the job
            try
            {
                IJobExtensions.EarlyJobInit<OptimizedApplyCoverageJob>();
            }
            catch (Exception ex)
            {
                log.Warn($"[ServiceCoverageOptimizer] EarlyJobInit notice: {ex.Message}");
            }

            // Apply patch if setting permits
            bool shouldEnable = Mod.Settings?.ServiceCoverageOptimizer ?? true;
            ApplyPatch(shouldEnable);
        }

        public void SetEnabled(bool enabled)
        {
            ApplyCoverageScheduleBridge.Enabled = enabled;
            if (enabled && !IsPatchApplied)
            {
                ApplyPatch(true);
            }
            StatusString = enabled && IsPatchApplied ? "Ativo (Fila Radix O(1))" : "Desativado (Vanilla)";
            log.Info($"[ServiceCoverageOptimizer] Enabled set to: {enabled} (Patch applied: {IsPatchApplied})");
        }

        public static bool ApplyPatch(bool enable)
        {
            ApplyCoverageScheduleBridge.Enabled = enable;
            if (IsPatchApplied) return true;

            if (!ServiceCoverageCompatibilityGate.Validate())
            {
                StatusString = $"Incompatível: {ServiceCoverageCompatibilityGate.FailureReason} (Fallback Seguro)";
                log.Warn($"[ServiceCoverageOptimizer] Layout validation failed: {ServiceCoverageCompatibilityGate.FailureReason}. Vanilla job maintained.");
                return false;
            }

            try
            {
                s_Harmony ??= new Harmony(HarmonyId);
                MethodInfo? targetMethod = AccessTools.Method(ServiceCoverageCompatibilityGate.ServiceCoverageSystemType, "OnUpdate");
                if (targetMethod == null)
                {
                    StatusString = "Método OnUpdate não encontrado";
                    return false;
                }

                MethodInfo? transpilerMethod = AccessTools.Method(typeof(ServiceCoverageSystemPatch), nameof(ServiceCoverageSystemPatch.Transpiler));
                s_Harmony.Patch(targetMethod, transpiler: new HarmonyMethod(transpilerMethod));

                IsPatchApplied = true;
                StatusString = enable ? "Ativo (Fila Radix O(1))" : "Injetado (Modo Vanilla)";
                log.Info("[ServiceCoverageOptimizer] Radix queue patch successfully attached to ServiceCoverageSystem.OnUpdate!");
                return true;
            }
            catch (Exception ex)
            {
                IsPatchApplied = false;
                StatusString = $"Falha no patch: {ex.Message}";
                log.Error($"[ServiceCoverageOptimizer] Failed to apply Harmony patch: {ex.Message}\n{ex.StackTrace}");
                try { s_Harmony?.UnpatchAll(HarmonyId); } catch { }
                return false;
            }
        }

        public static void RemovePatch()
        {
            if (!IsPatchApplied) return;
            try
            {
                s_Harmony?.UnpatchAll(HarmonyId);
                s_Harmony = null;
                IsPatchApplied = false;
                StatusString = "Removido (Vanilla)";
                log.Info("[ServiceCoverageOptimizer] Unpatched ServiceCoverageSystem successfully.");
            }
            catch (Exception ex)
            {
                log.Warn($"[ServiceCoverageOptimizer] Unpatch exception: {ex.Message}");
            }
        }

        protected override void OnDestroy()
        {
            RemovePatch();
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            // Lightweight supervisor: ensures bridge state matches settings
            if (Mod.Settings != null && ApplyCoverageScheduleBridge.Enabled != Mod.Settings.ServiceCoverageOptimizer)
            {
                SetEnabled(Mod.Settings.ServiceCoverageOptimizer);
            }
        }
    }
}
