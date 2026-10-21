using System;
using Colossal.Collections;
using Colossal.Logging;
using Colossal.Mathematics;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Rendering;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace UltraPerformanceMod.Systems
{
    public struct ModHiddenTree : IComponentData, IQueryTypeParameter
    {
        public BoundsMask m_OriginalMask;
        public Bounds3 m_OriginalBounds;
        public byte m_Category; // 0 = Wild, 1 = BuildingOrPark, 2 = Roadside
    }

    public class TreeManagerSystem : GameSystemBase
    {
        private static readonly ILog log = Mod.log;
        public static TreeManagerSystem? Instance { get; private set; }

        private EntityQuery m_AllTreesQuery = default!;
        private EntityQuery m_HiddenTreesQuery = default!;
        private TreeGrowthSystem? m_TreeGrowthSystem;
        private Game.Objects.SearchSystem? m_ObjectSearchSystem;

        private bool m_LastHideWild = false;
        private bool m_LastHideBuilding = false;
        private bool m_LastHideRoad = false;
        private bool m_WasWindDisabled = false;
        private bool m_WasGrowthDisabled = false;
        private uint m_FrameCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            m_AllTreesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Tree>(), ComponentType.ReadOnly<CullingInfo>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_HiddenTreesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Tree>(), ComponentType.ReadOnly<ModHiddenTree>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            m_TreeGrowthSystem = World.GetOrCreateSystemManaged<TreeGrowthSystem>();
            m_ObjectSearchSystem = World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();

            log.Info("[UltraPerformanceMod] TreeManagerSystem initialized with granular tree categories.");
        }

        protected override void OnDestroy()
        {
            RestoreAllHiddenTrees();

            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings == null) return;

            m_FrameCounter++;

            bool hideWild = Mod.Settings.InvisibleWildTrees;
            bool hideBuilding = Mod.Settings.InvisibleBuildingTrees;
            bool hideRoad = Mod.Settings.InvisibleRoadTrees;
            bool disableWind = Mod.Settings.DisableTreeWind;
            bool disableGrowth = Mod.Settings.DisableTreeGrowth;

            bool stateChanged = (hideWild != m_LastHideWild ||
                                 hideBuilding != m_LastHideBuilding ||
                                 hideRoad != m_LastHideRoad);

            // 1. Handle Granular Tree Visibility
            if (stateChanged || (m_FrameCounter % 120 == 0))
            {
                UpdateTreeVisibility(hideWild, hideBuilding, hideRoad, stateChanged);
                m_LastHideWild = hideWild;
                m_LastHideBuilding = hideBuilding;
                m_LastHideRoad = hideRoad;
            }

            // 2. Handle Tree Wind
            bool windChanged = (disableWind != m_WasWindDisabled);
            if (windChanged || (disableWind && m_FrameCounter % 120 == 0))
            {
                ApplyTreeWindSetting(disableWind);
                m_WasWindDisabled = disableWind;
            }

            // 3. Handle Tree Growth
            bool growthChanged = (disableGrowth != m_WasGrowthDisabled);
            if (growthChanged)
            {
                if (m_TreeGrowthSystem != null)
                {
                    m_TreeGrowthSystem.Enabled = !disableGrowth;
                    log.Info($"[UltraPerformanceMod] TreeGrowthSystem enabled state set to: {!disableGrowth}");
                }
                m_WasGrowthDisabled = disableGrowth;
            }
        }

        private void UpdateTreeVisibility(bool hideWild, bool hideBuilding, bool hideRoad, bool stateChanged)
        {
            if (m_AllTreesQuery.IsEmptyIgnoreFilter && m_HiddenTreesQuery.IsEmptyIgnoreFilter) return;

            // If all hiding options are disabled, restore everything immediately
            if (!hideWild && !hideBuilding && !hideRoad)
            {
                if (!m_HiddenTreesQuery.IsEmptyIgnoreFilter)
                {
                    RestoreAllHiddenTrees();
                }
                return;
            }

            NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree = default;
            bool hasSearchTree = false;
            if (m_ObjectSearchSystem != null)
            {
                try
                {
                    searchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: false, out var dep);
                    dep.Complete();
                    hasSearchTree = searchTree.IsCreated;
                }
                catch (Exception ex)
                {
                    log.Warn($"[UltraPerformanceMod] Could not acquire static search tree: {ex.Message}");
                }
            }

            try
            {
                // First: Check if any currently hidden tree should now be restored
                if (!m_HiddenTreesQuery.IsEmptyIgnoreFilter)
                {
                    using var hiddenEntities = m_HiddenTreesQuery.ToEntityArray(Allocator.TempJob);
                    for (int i = 0; i < hiddenEntities.Length; i++)
                    {
                        var entity = hiddenEntities[i];
                        if (!EntityManager.Exists(entity) || EntityManager.HasComponent<Deleted>(entity)) continue;

                        var modHidden = EntityManager.GetComponentData<ModHiddenTree>(entity);
                        bool shouldStillHide = (modHidden.m_Category == 0 && hideWild) ||
                                               (modHidden.m_Category == 1 && hideBuilding) ||
                                               (modHidden.m_Category == 2 && hideRoad);

                        if (!shouldStillHide)
                        {
                            RestoreSingleTree(entity, modHidden, ref searchTree, hasSearchTree);
                        }
                    }
                }

                // Second: Check visible trees that should now be hidden
                using var allEntities = m_AllTreesQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < allEntities.Length; i++)
                {
                    var entity = allEntities[i];
                    if (!EntityManager.Exists(entity) || EntityManager.HasComponent<Deleted>(entity)) continue;
                    if (EntityManager.HasComponent<ModHiddenTree>(entity)) continue;

                    byte category = GetTreeCategory(entity);
                    bool shouldHide = (category == 0 && hideWild) ||
                                      (category == 1 && hideBuilding) ||
                                      (category == 2 && hideRoad);

                    if (shouldHide)
                    {
                        HideSingleTree(entity, category, ref searchTree, hasSearchTree);
                    }
                }
            }
            finally
            {
                if (hasSearchTree && m_ObjectSearchSystem != null)
                {
                    m_ObjectSearchSystem.AddStaticSearchTreeWriter(Dependency);
                }
            }
        }

        private void HideSingleTree(Entity entity, byte category, ref NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree, bool hasSearchTree)
        {
            if (!EntityManager.HasComponent<CullingInfo>(entity)) return;

            var culling = EntityManager.GetComponentData<CullingInfo>(entity);

            // Record original state in ModHiddenTree
            EntityManager.AddComponentData(entity, new ModHiddenTree
            {
                m_OriginalMask = culling.m_Mask,
                m_OriginalBounds = culling.m_Bounds,
                m_Category = category
            });

            // Set CullingInfo to culled/debug state so PreCullingSystem fails it
            culling.m_Mask = BoundsMask.Debug;
            culling.m_PassedCulling = 0;
            culling.m_Bounds = new Bounds3(new float3(0f, -999999f, 0f), new float3(0f, -999999f, 0f));
            EntityManager.SetComponentData(entity, culling);

            // Update QuadTree so camera culling skips traversal
            if (hasSearchTree)
            {
                searchTree.Update(entity, new QuadTreeBoundsXZ(new Bounds3(new float3(0f, -999999f, 0f), new float3(0f, -999999f, 0f)), BoundsMask.Debug, 255));
            }

            // Notify BatchInstanceSystem to clear GPU batch instances
            EntityManager.AddComponent<BatchesUpdated>(entity);

            if (EntityManager.HasBuffer<MeshBatch>(entity))
            {
                EntityManager.GetBuffer<MeshBatch>(entity).Clear();
            }
        }

        private void RestoreSingleTree(Entity entity, ModHiddenTree modHidden, ref NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree, bool hasSearchTree)
        {
            if (EntityManager.HasComponent<CullingInfo>(entity))
            {
                var culling = EntityManager.GetComponentData<CullingInfo>(entity);
                BoundsMask restoredMask = modHidden.m_OriginalMask;
                if ((restoredMask & BoundsMask.NormalLayers) == 0)
                {
                    restoredMask |= BoundsMask.NormalLayers | BoundsMask.IsTree | BoundsMask.NotOverridden;
                }

                culling.m_Mask = restoredMask;
                culling.m_Bounds = modHidden.m_OriginalBounds;
                EntityManager.SetComponentData(entity, culling);

                if (hasSearchTree)
                {
                    searchTree.Update(entity, new QuadTreeBoundsXZ(modHidden.m_OriginalBounds, restoredMask, 0));
                }
            }

            EntityManager.RemoveComponent<ModHiddenTree>(entity);
            EntityManager.AddComponent<Updated>(entity);
            EntityManager.AddComponent<BatchesUpdated>(entity);
        }

        public void RestoreAllHiddenTrees()
        {
            if (m_HiddenTreesQuery.IsEmptyIgnoreFilter) return;

            NativeQuadTree<Entity, QuadTreeBoundsXZ> searchTree = default;
            bool hasSearchTree = false;
            if (m_ObjectSearchSystem != null)
            {
                try
                {
                    searchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: false, out var dep);
                    dep.Complete();
                    hasSearchTree = searchTree.IsCreated;
                }
                catch {}
            }

            try
            {
                using var hiddenEntities = m_HiddenTreesQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < hiddenEntities.Length; i++)
                {
                    var entity = hiddenEntities[i];
                    if (!EntityManager.Exists(entity) || EntityManager.HasComponent<Deleted>(entity)) continue;

                    var modHidden = EntityManager.GetComponentData<ModHiddenTree>(entity);
                    RestoreSingleTree(entity, modHidden, ref searchTree, hasSearchTree);
                }
                log.Info($"[UltraPerformanceMod] Restored visibility for {hiddenEntities.Length} trees.");
            }
            finally
            {
                if (hasSearchTree && m_ObjectSearchSystem != null)
                {
                    m_ObjectSearchSystem.AddStaticSearchTreeWriter(Dependency);
                }
            }
        }

        public byte GetTreeCategory(Entity tree)
        {
            // Category 2: Roadside / Street trees
            if (EntityManager.HasComponent<NetObject>(tree))
            {
                return 2;
            }

            if (EntityManager.HasComponent<Owner>(tree))
            {
                Entity current = EntityManager.GetComponentData<Owner>(tree).m_Owner;
                int depth = 0;
                while (EntityManager.Exists(current) && depth < 4)
                {
                    if (EntityManager.HasComponent<Edge>(current) ||
                        EntityManager.HasComponent<Game.Net.Node>(current) ||
                        EntityManager.HasComponent<Curve>(current) ||
                        EntityManager.HasComponent<Lane>(current))
                    {
                        return 2; // Roadside
                    }

                    if (EntityManager.HasComponent<Building>(current) ||
                        EntityManager.HasComponent<Park>(current) ||
                        EntityManager.HasComponent<Game.Areas.Lot>(current) ||
                        EntityManager.HasComponent<Game.Buildings.Lot>(current))
                    {
                        return 1; // BuildingOrPark
                    }

                    if (EntityManager.HasComponent<Owner>(current))
                    {
                        current = EntityManager.GetComponentData<Owner>(current).m_Owner;
                        depth++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // Category 0: Wild / Nature trees (outside of lots and nets)
            return 0;
        }

        private void ApplyTreeWindSetting(bool disableWind)
        {
            try
            {
                var stack = VolumeManager.instance?.stack;
                if (stack == null) return;

                var windVolume = stack.GetComponent<WindVolumeComponent>();
                if (windVolume != null)
                {
                    if (disableWind)
                    {
                        windVolume.windTreeBaseStrength.value = 0f;
                        windVolume.windTreeBaseStrength.overrideState = true;

                        windVolume.windTreeGustStrength.value = 0f;
                        windVolume.windTreeGustStrength.overrideState = true;

                        windVolume.windTreeFlutterStrength.value = 0f;
                        windVolume.windTreeFlutterStrength.overrideState = true;

                        windVolume.windTreeFlutterGustStrength.value = 0f;
                        windVolume.windTreeFlutterGustStrength.overrideState = true;
                    }
                    else
                    {
                        windVolume.windTreeBaseStrength.overrideState = false;
                        windVolume.windTreeGustStrength.overrideState = false;
                        windVolume.windTreeFlutterStrength.overrideState = false;
                        windVolume.windTreeFlutterGustStrength.overrideState = false;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warn($"[UltraPerformanceMod] Error applying tree wind setting: {ex.Message}");
            }
        }
    }
}
