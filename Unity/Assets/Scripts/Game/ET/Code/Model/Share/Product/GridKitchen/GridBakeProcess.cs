#if !DOTNET
namespace ET
{
    public enum DishStageKind { Raw = 0, Manual = 1, Batch = 2, Bake = 3, Portion = 4, AddToContainer = 5, Assembly = 6, Fry = 7, BakeInCarrier = 8, Unmold = 9, Blend = 10, Grind = 11 }
    public enum GridBakeState { Baking, Completed }

    [ChildOf(typeof(GridKitchenComponent))]
    public sealed class GridBakeProcess : Entity, IAwake
    {
        public long TransitionSerial;
        public bool CompletionQueued, CompletionReceived;
        public int RecipeId, RequiredTicks, ProgressTicks, OutputDefinitionId;
        public long StationId, InputItemId, OutputItemId, StartedTick, CarrierItemId;
        public GridBakeState State;
    }
}
#endif
