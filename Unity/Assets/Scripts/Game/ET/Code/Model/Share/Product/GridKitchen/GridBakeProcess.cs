#if !DOTNET
namespace ET
{
    public enum GridBakeState { Baking = 0, Completed = 1 }

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
