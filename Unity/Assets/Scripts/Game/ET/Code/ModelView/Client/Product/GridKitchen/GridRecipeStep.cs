namespace ET.Client
{
    public enum GridRecipeStepKind
    {
        Production,
        MissingMaterial,
        Waiting,
        Portion,
        Delivery,
        Unavailable,
        Completed,
        BuffChoice,
        Reception,
        FirstOrderComplete
    }

    // A derived reading result; identities still belong to the current client projection.
    public readonly struct GridRecipeStep
    {
        public readonly GridRecipeStepKind Kind;
        public readonly string Description;
        public readonly int StageId;
        public readonly int MissingDefinitionId;
        public readonly long ItemId;
        public readonly long StationId;

        public GridRecipeStep(GridRecipeStepKind kind, string description, int stageId, int missingDefinitionId, long itemId, long stationId)
        {
            Kind = kind;
            Description = description;
            StageId = stageId;
            MissingDefinitionId = missingDefinitionId;
            ItemId = itemId;
            StationId = stationId;
        }
    }
}
