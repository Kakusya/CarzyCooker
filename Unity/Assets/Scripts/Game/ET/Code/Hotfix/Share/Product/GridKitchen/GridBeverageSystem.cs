#if !DOTNET
using System.Linq;

namespace ET
{
    public static partial class GridKitchenSystem
    {
        private static GridStationCapability AutoCapability(GridAction action) => action switch
        {
            GridAction.StartJuice => GridStationCapability.Juice,
            GridAction.StartMix => GridStationCapability.Mixing,
            GridAction.StartExtract => GridStationCapability.Extract,
            GridAction.HeatMilk => GridStationCapability.HeatMilk,
            GridAction.FoamMilk => GridStationCapability.FoamMilk,
                GridAction.StartBrewTea => GridStationCapability.BrewTea,
                GridAction.StartMixTea => GridStationCapability.MixTea,
            _ => GridStationCapability.Cook
        };

        private static bool IsBeverageCapability(GridStationCapability capability) => capability is
            GridStationCapability.Extract or GridStationCapability.HeatMilk or GridStationCapability.FoamMilk;

        private static int AutoContainer(GridStationCapability capability) => capability switch
        {
            GridStationCapability.Juice => GridKitchenRules.JuiceJug,
            GridStationCapability.Mixing => GridKitchenRules.MixingBowl,
            GridStationCapability.Extract => GridKitchenRules.CoffeeReceiver,
            GridStationCapability.HeatMilk or GridStationCapability.FoamMilk => GridKitchenRules.MilkPitcher,
            GridStationCapability.BrewTea => GridKitchenRules.TeaBarrel,
            GridStationCapability.MixTea => GridKitchenRules.TeaMixingJug,
            _ => GridKitchenRules.Pot
        };

        private static string CheckDrinkBaseAddition(this GridKitchenComponent kitchen, GridItem cup, int definition)
        {
            if (cup.DefinitionId is not (GridKitchenRules.HotDrinkCup or GridKitchenRules.ColdDrinkCup or GridKitchenRules.TeaMixingJug))
            {
                return "IncompatibleContainer";
            }
            if (kitchen.ItemWorking(cup.Id) || kitchen.InputProcess(cup.Id) is { State: not GridWorkState.Completed })
            {
                return "ProcessLocked";
            }
            if (kitchen.ProductLevel()?.DrinkBindings.ContainsKey(cup.Id) == true)
            {
                return "DrinkAlreadyBound";
            }
            if (cup.DefinitionId == GridKitchenRules.TeaMixingJug)
            {
                string recipe = kitchen.CheckPotAddition(cup, definition);
                if (recipe != null)
                {
                    return recipe;
                }
            }
            var carrier = Tables.Instance.DTGridItemConfig.Get(cup.DefinitionId);
            if (cup.Contents.Count >= carrier.ContainerCapacity)
            {
                return "ContainerFull";
            }
            if (!carrier.AcceptedDefinitionIds.Contains(definition) || !GridAssemblyConfig.AllowsContainerContents(cup.DefinitionId,
                cup.Contents.Select(id => kitchen.Item(id).DefinitionId).Append(definition).ToArray()))
            {
                return "WrongDrinkSequence";
            }
            return null;
        }
    }
}
#endif
