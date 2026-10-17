#if !DOTNET
namespace ET
{
    // 仅本批真实基底的可信转移关系；其他批次仍要求原ServingContainerDefinitionId。
    public static class GridTeaRules
    {
        public static bool IsTeaCapability(GridStationCapability capability) => capability is GridStationCapability.BrewTea or GridStationCapability.MixTea;

        public static bool CanServe(DRGridAutoRecipeConfig recipe, int container) =>
            CanServe(recipe.StationCapability, recipe.ContainerDefinitionId, recipe.OutputDefinitionId, recipe.ServingContainerDefinitionId, container);

        public static bool CanServe(GridStationCapability capability, int source, int output, int configured, int target)
        {
            if (capability == GridStationCapability.BrewTea)
            {
                return source == GridKitchenRules.TeaBarrel && output == GridKitchenRules.BlackTea && configured == GridKitchenRules.HotDrinkCup &&
                    target is GridKitchenRules.HotDrinkCup or GridKitchenRules.TeaMixingJug;
            }
            if (capability == GridStationCapability.MixTea)
            {
                return source == GridKitchenRules.TeaMixingJug && output == GridKitchenRules.MilkTeaBase &&
                    configured == GridKitchenRules.ColdDrinkCup && target == configured;
            }
            return target == configured;
        }

        public static bool SelectiveCleanup(int container) => container is GridKitchenRules.ColdDrinkCup or GridKitchenRules.TeaMixingJug;
    }
}
#endif
