#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    public static class ProductionPermissionRules
    {
        public static void Rebuild(ProductionPermit permit, IEnumerable<int> candidateDishes, IEnumerable<int> unlockedStations)
        {
            permit.Clear();
            var tables = Tables.Instance;
            GridStationCapability capabilities = GridStationCapability.None;
            foreach (int id in unlockedStations)
            {
                permit.StationDefinitions.Add(id);
                capabilities |= (GridStationCapability)tables.DTStationConfig.Get(id).Capabilities;
            }
            foreach (int id in candidateDishes)
            {
                var dish = tables.DTDishConfig.Get(id);
                permit.StageScratch.Clear();
                GridStationCapability needed = RequiredCapabilities(permit, dish.FinalStageId);
                if (dish.RequiresOrderBinding)
                {
                    needed |= GridStationCapability.LabelDrink;
                }
                if ((capabilities & needed) != needed)
                {
                    continue;
                }
                permit.DishIds.Add(id);
                IncludeStage(permit, dish.FinalStageId);
            }
            permit.StageScratch.Clear();
        }

        private static GridStationCapability RequiredCapabilities(ProductionPermit permit, int stageId)
        {
            if (!permit.StageScratch.Add(stageId))
            {
                return GridStationCapability.None;
            }
            var tables = Tables.Instance;
            var stage = tables.DTDishStageConfig.Get(stageId);
            GridStationCapability result = GridStationCapability.None;
            foreach (int dependency in stage.DependencyIds)
            {
                result |= RequiredCapabilities(permit, dependency);
            }
            switch ((DishStageKind)stage.Kind)
            {
                case DishStageKind.Manual:
                    if (tables.DTGridManualRecipeConfig.Get(stage.RecipeId).Mode == GridManualMode.Chop)
                    {
                        result |= GridStationCapability.Chop;
                    }
                    break;
                case DishStageKind.Batch:
                    result |= (GridStationCapability)tables.DTGridAutoRecipeConfig.Get(stage.RecipeId).StationCapability;
                    break;
                case DishStageKind.Boil:
                case DishStageKind.Steam:
                case DishStageKind.Grind:
                case DishStageKind.Bake:
                case DishStageKind.Fry:
                case DishStageKind.BakeInCarrier:
                    result |= (GridStationCapability)tables.DTGridBakeRecipeConfig.Get(stage.RecipeId).StationCapability;
                    break;
            }
            return result;
        }

        private static void AddInput(ProductionPermit permit, int container, int input)
        {
            if (!permit.ContainerInputs.TryGetValue(container, out var values))
            {
                values = new HashSet<int>(8);
                permit.ContainerInputs.Add(container, values);
            }
            values.Add(input);
        }

        private static void IncludeStage(ProductionPermit permit, int stageId)
        {
            if (!permit.Stages.Add(stageId))
            {
                return;
            }
            var tables = Tables.Instance;
            var stage = tables.DTDishStageConfig.Get(stageId);
            foreach (int dependency in stage.DependencyIds)
            {
                IncludeStage(permit, dependency);
            }
            permit.ItemDefinitions.Add(stage.OutputDefinitionId);
            if (stage.ContainerDefinitionId != 0)
            {
                permit.ItemDefinitions.Add(stage.ContainerDefinitionId);
            }
            switch ((DishStageKind)stage.Kind)
            {
                case DishStageKind.Manual:
                    permit.ManualRecipes.Add(stage.RecipeId);
                    var manual = tables.DTGridManualRecipeConfig.Get(stage.RecipeId);
                    if (manual.Mode == GridManualMode.Beat)
                    {
                        AddInput(permit, stage.ContainerDefinitionId, manual.InputDefinitionId);
                    }
                    break;
                case DishStageKind.Blend:
                case DishStageKind.Assembly:
                    permit.ManualRecipes.Add(stage.RecipeId);
                    foreach (int id in tables.DTGridManualRecipeConfig.Get(stage.RecipeId).InputDefinitionIds)
                    {
                        AddInput(permit, stage.ContainerDefinitionId, id);
                    }
                    break;
                case DishStageKind.Batch:
                    permit.AutoRecipes.Add(stage.RecipeId);
                    foreach (int id in tables.DTGridAutoRecipeConfig.Get(stage.RecipeId).InputDefinitionIds)
                    {
                        AddInput(permit, stage.ContainerDefinitionId, id);
                    }
                    break;
                case DishStageKind.Boil:
                case DishStageKind.Steam:
                case DishStageKind.Grind:
                case DishStageKind.Bake:
                case DishStageKind.Fry:
                case DishStageKind.BakeInCarrier:
                    permit.BakeRecipes.Add(stage.RecipeId);
                    break;
                case DishStageKind.AddToContainer:
                case DishStageKind.Portion:
                case DishStageKind.Unmold:
                    AddInput(permit, stage.ContainerDefinitionId, stage.OutputDefinitionId);
                    break;
            }
        }

        public static bool AllowsInput(ProductionPermit permit, int container, int input) =>
            permit.ContainerInputs.TryGetValue(container, out var values) && values.Contains(input);

        public static string Rejection(ProductionPermissionDecision decision) => decision == ProductionPermissionDecision.Allowed ? null : decision.ToString();
    }
}
#endif
