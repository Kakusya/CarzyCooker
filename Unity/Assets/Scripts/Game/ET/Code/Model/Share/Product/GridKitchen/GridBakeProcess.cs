#if !DOTNET
namespace ET
{
    public enum DishStageKind { Raw, Manual, Batch, Bake, Portion, AddToContainer }
    public enum GridBakeState { Baking, Completed }

    [ChildOf(typeof(GridKitchenComponent))]
    public sealed class GridBakeProcess : Entity, IAwake
    {
        public long TransitionSerial;
        public bool CompletionQueued, CompletionReceived;
        public int RecipeId, RequiredTicks, ProgressTicks, OutputDefinitionId;
        public long StationId, InputItemId, OutputItemId, StartedTick;
        public GridBakeState State;
    }
}
#endif
