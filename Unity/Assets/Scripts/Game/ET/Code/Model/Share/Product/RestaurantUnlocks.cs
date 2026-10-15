#if !DOTNET
using System;
using System.Collections.Generic;
using MemoryPack;

namespace ET
{
    public enum ProductionPermissionDecision { Allowed = 0, MenuRestricted = 1, EquipmentLocked = 2 }

    [MemoryPackable]
    public sealed partial class SavedRestaurantUnlocks : Object
    {
        public int[] DishIds { get; set; } = Array.Empty<int>();
        public int[] StationIds { get; set; } = Array.Empty<int>();
    }

    [ComponentOf(typeof(RestaurantRuntime))]
    public sealed class RestaurantUnlocks : Entity, IAwake<DRLevelConfig>
    {
        public long PermissionVersion;
        public readonly HashSet<int> DishIds = new(16);
        public readonly HashSet<int> StationIds = new(16);
    }

    // 由Level或Client持有的可重建许可索引；解锁事实只属于RestaurantUnlocks。
    public sealed class ProductionPermit : Object
    {
        public readonly HashSet<int> DishIds = new(16);
        public readonly HashSet<int> ItemDefinitions = new(64);
        public readonly HashSet<int> ManualRecipes = new(16);
        public readonly HashSet<int> AutoRecipes = new(8);
        public readonly HashSet<int> BakeRecipes = new(8);
        public readonly HashSet<int> StationDefinitions = new(16);
        public readonly Dictionary<int, HashSet<int>> ContainerInputs = new(8);
        public readonly HashSet<int> Stages = new(64);
        public readonly HashSet<int> StageScratch = new(64);

        public void Clear()
        {
            DishIds.Clear(); ItemDefinitions.Clear(); ManualRecipes.Clear(); AutoRecipes.Clear(); BakeRecipes.Clear();
            StationDefinitions.Clear(); Stages.Clear(); StageScratch.Clear();
            foreach (var inputs in ContainerInputs.Values)
            {
                inputs.Clear();
            }
        }
    }
}
#endif
