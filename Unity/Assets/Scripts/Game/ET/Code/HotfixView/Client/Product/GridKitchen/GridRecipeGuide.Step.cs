using System.Linq;

namespace ET.Client
{
    public static partial class GridRecipeGuide
    {
        private static bool GuideAvailable(GridClientComponent grid, GridItemProjection item)
        {
            if (item.Hygiene != ItemHygiene.Clean || item.AutoProcessId != 0 || item.BakeProcessId != 0 ||
                grid.Processes.Values.Any(process => process.State != GridWorkState.Completed &&
                    (process.InputItemId == item.Id || process.TargetItemId == item.Id)))
            {
                return false;
            }
            if (item.Location == GridItemLocation.Container)
            {
                return grid.Items.TryGetValue(item.HolderId, out var holder) && holder.DefinitionId != GridKitchenRules.BakingMold &&
                    holder.BoundOrderId == 0 && GuideAvailable(grid, holder);
            }
            return true;
        }

        private static GridRecipeStep ResolveStep(GridClientComponent grid, DRDishConfig dish, long selectedOrderId,
            int stageId, long itemId, long stationId, GridRecipeStepKind kind, string description)
        {
            if (selectedOrderId != 0 && (!grid.Orders.TryGetValue(selectedOrderId, out var selected) || selected.State != OrderState.Waiting || selected.DishId != dish.Id))
            {
                return new GridRecipeStep(GridRecipeStepKind.Unavailable, description, 0, 0, 0, 0);
            }
            if (grid.LevelPhase == LevelPhase.Completed)
            {
                return new GridRecipeStep(GridRecipeStepKind.Completed, description, 0, 0, 0, 0);
            }
            if (itemId != 0 || stageId == 0)
            {
                if (stationId != 0 && grid.Items.TryGetValue(itemId, out var ready) && ready.Location == GridItemLocation.Hand && ready.HolderId == grid.MemberId)
                {
                    itemId = 0;
                }
                return new GridRecipeStep(kind, description, stageId, 0, itemId, stationId);
            }
            DRDishStageConfig stage = Tables.Instance.DTDishStageConfig.Get(stageId);
            DishStageKind stageKind = (DishStageKind)stage.Kind;
            int definition = 0;
            bool empty = false;
            GridStationCapability capability = default;
            GridItemProjection hand = grid.Actors.TryGetValue(grid.MemberId, out var actor) && grid.Items.TryGetValue(actor.HandItemId, out var held) ? held : default;
            GridItemProjection Find(int id, bool requireEmpty = false) => grid.Items.Values.Where(value => value.DefinitionId == id &&
                GuideAvailable(grid, value) && (!requireEmpty || value.Contents.Count == 0)).OrderBy(value => value.Id).FirstOrDefault();

            if (stageKind == DishStageKind.Raw)
            {
                definition = stage.OutputDefinitionId;
                kind = GridRecipeStepKind.MissingMaterial;
            }
            else if (stageKind == DishStageKind.Manual)
            {
                var recipe = Tables.Instance.DTGridManualRecipeConfig.Get(stage.RecipeId);
                var work = grid.Processes.Values.FirstOrDefault(value => value.RecipeId == stage.RecipeId && value.State != GridWorkState.Completed);
                if (work.Id != 0)
                {
                    return new GridRecipeStep(GridRecipeStepKind.Waiting, description, stageId, 0, work.TargetItemId, work.StationId);
                }
                definition = recipe.InputDefinitionId;
                if (recipe.Mode == GridManualMode.Beat)
                {
                    var bowl = grid.Items.Values.FirstOrDefault(value => value.DefinitionId == stage.ContainerDefinitionId && GuideAvailable(grid, value) &&
                        value.Contents.Any(key => grid.Items.TryGetValue(key, out var content) && content.DefinitionId == recipe.InputDefinitionId));
                    if (bowl.Id != 0)
                    {
                        itemId = bowl.Id;
                    }
                    else if (hand.DefinitionId == recipe.InputDefinitionId)
                    {
                        definition = stage.ContainerDefinitionId;
                        empty = true;
                    }
                }
                else if (hand.DefinitionId == recipe.InputDefinitionId)
                {
                    capability = GridStationCapability.Chop;
                    definition = 0;
                }
            }
            else if (stageKind == DishStageKind.Batch)
            {
                var recipe = Tables.Instance.DTGridAutoRecipeConfig.Get(stage.RecipeId);
                var work = grid.AutoProcesses.Values.FirstOrDefault(value => value.RecipeId == stage.RecipeId && value.State == GridAutoState.Cooking);
                if (work.Id != 0)
                {
                    return new GridRecipeStep(GridRecipeStepKind.Waiting, description, stageId, 0, work.PotItemId, work.StartStationId);
                }
                var carrier = Find(recipe.ContainerDefinitionId);
                for (int index = 0; index < recipe.InputDefinitionIds.Count; ++index)
                {
                    int input = recipe.InputDefinitionIds[index];
                    if (carrier.Contents == null || carrier.Contents.Count(key => grid.Items.TryGetValue(key, out var content) && content.DefinitionId == input) < recipe.InputCounts[index])
                    {
                        definition = input;
                        break;
                    }
                }
                if (definition == 0 || hand.DefinitionId == definition || hand.Contents?.Any(key => grid.Items.TryGetValue(key, out var content) && content.DefinitionId == definition) == true)
                {
                    definition = recipe.ContainerDefinitionId;
                }
            }
            else if (stageKind is DishStageKind.Portion or DishStageKind.AddToContainer)
            {
                kind = GridRecipeStepKind.Portion;
                definition = stage.ContainerDefinitionId;
                empty = true;
                if (hand.DefinitionId == definition && hand.Hygiene == ItemHygiene.Clean && hand.Contents.Count == 0)
                {
                    var batch = grid.AutoProcesses.Values.FirstOrDefault(value => value.OutputDefinitionId == stage.OutputDefinitionId && value.State == GridAutoState.Ready && value.RemainingPortions > 0);
                    itemId = batch.PotItemId;
                    if (itemId == 0)
                    {
                        definition = stage.OutputDefinitionId;
                        empty = false;
                    }
                }
            }
            else if (stage.DependencyIds.Count > 0)
            {
                definition = Tables.Instance.DTDishStageConfig.Get(stage.DependencyIds[0]).OutputDefinitionId;
            }
            if (itemId == 0 && definition != 0)
            {
                itemId = Find(definition, empty).Id;
            }
            if (capability != default)
            {
                stationId = grid.Stations.Values.Where(value => (value.Capabilities & capability) == capability).OrderBy(value => value.Id).FirstOrDefault().Id;
            }
            return new GridRecipeStep(kind, description, stageId, itemId == 0 ? definition : 0, itemId, stationId);
        }
    }
}
