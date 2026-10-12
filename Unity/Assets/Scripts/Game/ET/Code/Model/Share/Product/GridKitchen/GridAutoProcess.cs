#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    [ChildOf(typeof(GridKitchenComponent))]
    public sealed class GridAutoProcess : Entity, IAwake
    {
        public int RecipeId, RequiredTicks, ProgressTicks, OutputDefinitionId, TotalPortions, RemainingPortions, ServingContainerDefinitionId;
        public long PotItemId, StartStationId, StartedTick;
        public GridAutoState State;
        public readonly List<long> InputItemIds = new();
    }
}
#endif
