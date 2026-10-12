#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public static class GridDishConfig
    {
        public static void ValidateAllMenus()
        {
            Tables tables = Tables.Instance;
            foreach (var level in tables.DTLevelConfig.DataList)
                ValidateMenu(level.AllowedDishIds, tables.DTDishConfig.DataList, tables.DTDishStageConfig.DataList,
                    tables.DTGridItemConfig.DataList, tables.DTGridManualRecipeConfig.DataList, tables.DTGridAutoRecipeConfig.DataList,
                    tables.DTGridBakeRecipeConfig.DataList, tables.DTStationConfig.DataList, tables.DTInitialLayoutConfig.DataList);
        }

        public static void ValidateMenu(IEnumerable<int> menuIds, IEnumerable<DRDishConfig> dishRows,
            IEnumerable<DRDishStageConfig> stageRows, IEnumerable<DRGridItemConfig> itemRows,
            IEnumerable<DRGridManualRecipeConfig> manualRows, IEnumerable<DRGridAutoRecipeConfig> batchRows,
            IEnumerable<DRGridBakeRecipeConfig> bakeRows, IEnumerable<DRStationConfig> stationRows, IEnumerable<DRInitialLayoutConfig> layoutRows)
        {
            var menu = menuIds.ToArray(); var dishes = dishRows.ToDictionary(row => row.Id); var stages = stageRows.ToDictionary(row => row.Id);
            var items = itemRows.ToDictionary(row => row.Id); var manuals = manualRows.ToDictionary(row => row.Id);
            var batches = batchRows.ToDictionary(row => row.Id); var bakes = bakeRows.ToDictionary(row => row.Id);
            var stations = stationRows.ToDictionary(row => row.Id); var layouts = layoutRows.ToArray();
            var supplies = layouts.Where(row => row.InitialItemDefinitionId != 0).GroupBy(row => row.InitialItemDefinitionId).ToDictionary(group => group.Key, group => group.Count());
            var produced = manuals.Values.Select(row => row.OutputDefinitionId).Concat(batches.Values.Select(row => row.OutputDefinitionId)).Concat(bakes.Values.Select(row => row.OutputDefinitionId)).ToHashSet();
            int capabilities = 0;
            foreach (var layout in layouts)
            {
                if (!stations.TryGetValue(layout.StationDefinitionId, out var station)) throw new InvalidOperationException("Missing menu equipment definition");
                capabilities |= station.Capabilities;
            }
            if (menu.Length == 0 || menu.Distinct().Count() != menu.Length) throw new InvalidOperationException("Empty / duplicate menu");
            var visiting = new HashSet<int>(); var available = new Dictionary<int, int>();

            bool Compatible(int container, int content) => items.TryGetValue(container, out var definition) && definition.Kind == 2 &&
                definition.ContainerCapacity > 0 && definition.AcceptedDefinitionIds.Contains(content);
            void NeedCapability(int capability)
            { if ((capabilities & capability) != capability) throw new InvalidOperationException("Missing menu equipment capability"); }
            void NeedInputs(DRDishStageConfig stage, params int[] definitions)
            {
                if (!stage.DependencyIds.Select(id => stages[id].OutputDefinitionId).OrderBy(id => id).SequenceEqual(definitions.OrderBy(id => id)))
                    throw new InvalidOperationException($"Dish stage {stage.Id} has incompatible dependencies");
            }
            int UnitsFor(DRDishStageConfig stage, int definition) => available[stage.DependencyIds.Single(id => stages[id].OutputDefinitionId == definition)];
            bool Whole(int stageId)
            {
                var value = stages[stageId];
                return value.Kind == (int)DishStageKind.Raw && items[value.OutputDefinitionId].Kind == 1 ||
                    value.Kind == (int)DishStageKind.Bake || value.Kind == (int)DishStageKind.Manual && manuals[value.RecipeId].Mode == 1;
            }
            void NeedWhole(DRDishStageConfig stage, int definition)
            { if (!Whole(stage.DependencyIds.Single(id => stages[id].OutputDefinitionId == definition))) throw new InvalidOperationException("Stage requires an actual whole material"); }
            int Visit(int id)
            {
                if (available.TryGetValue(id, out int units)) return units;
                if (!stages.TryGetValue(id, out var stage) || id <= 0 || !items.TryGetValue(stage.OutputDefinitionId, out var output))
                    throw new InvalidOperationException($"Missing dish stage / output {id}");
                if (!visiting.Add(id)) throw new InvalidOperationException("Cyclic dish stage graph");
                if (stage.DependencyIds.Distinct().Count() != stage.DependencyIds.Count) throw new InvalidOperationException("Duplicate stage dependencies");
                foreach (int dependency in stage.DependencyIds) Visit(dependency);
                switch ((DishStageKind)stage.Kind)
                {
                    case DishStageKind.Raw:
                        if (stage.DependencyIds.Count != 0 || stage.RecipeId != 0 || stage.ContainerDefinitionId != 0 || produced.Contains(output.Id) ||
                            !supplies.TryGetValue(output.Id, out units)) throw new InvalidOperationException("Missing raw supply / free intermediate stage");
                        break;
                    case DishStageKind.Manual:
                        if (!manuals.TryGetValue(stage.RecipeId, out var manual) || manual.RequiredTicks <= 0 || manual.OutputDefinitionId != output.Id || output.Kind != 1)
                            throw new InvalidOperationException("Invalid manual stage recipe");
                        if (manual.Mode == 1)
                        { NeedInputs(stage, manual.InputDefinitionId); NeedCapability((int)GridStationCapability.Chop); if (stage.ContainerDefinitionId != 0) throw new InvalidOperationException("Unexpected manual container"); }
                        else if (manual.Mode == 2 && stage.ContainerDefinitionId == GridKitchenRules.Bowl && Compatible(stage.ContainerDefinitionId, manual.InputDefinitionId) && Compatible(stage.ContainerDefinitionId, output.Id))
                            NeedInputs(stage, manual.InputDefinitionId, stage.ContainerDefinitionId);
                        else throw new InvalidOperationException("Invalid held manual stage container");
                        NeedWhole(stage, manual.InputDefinitionId);
                        units = UnitsFor(stage, manual.InputDefinitionId);
                        if (manual.Mode == 2) units = Math.Min(units, UnitsFor(stage, stage.ContainerDefinitionId));
                        break;
                    case DishStageKind.Batch:
                        if (!batches.TryGetValue(stage.RecipeId, out var batch) || batch.OutputDefinitionId != output.Id || batch.ContainerDefinitionId != stage.ContainerDefinitionId ||
                            batch.RequiredTicks <= 0 || batch.OutputPortions <= 0 || batch.InputDefinitionIds.Count == 0 || batch.InputDefinitionIds.Count != batch.InputCounts.Count || batch.InputCounts.Any(count => count <= 0) ||
                            batch.InputDefinitionIds.Distinct().Count() != batch.InputDefinitionIds.Count || batch.InputDefinitionIds.Any(input => !Compatible(batch.ContainerDefinitionId, input)))
                            throw new InvalidOperationException("Invalid batch stage recipe");
                        NeedInputs(stage, batch.InputDefinitionIds.Concat(new[] { batch.ContainerDefinitionId }).ToArray()); NeedCapability((int)GridStationCapability.Cook);
                        int batchCount = int.MaxValue;
                        for (int i = 0; i < batch.InputDefinitionIds.Count; ++i) batchCount = Math.Min(batchCount, UnitsFor(stage, batch.InputDefinitionIds[i]) / batch.InputCounts[i]);
                        units = checked(batchCount * batch.OutputPortions);
                        break;
                    case DishStageKind.Bake:
                        if (!bakes.TryGetValue(stage.RecipeId, out var bake) || bake.OutputDefinitionId != output.Id || output.Kind != 1 || bake.RequiredTicks <= 0 ||
                            bake.InputDefinitionId == bake.OutputDefinitionId || !items.TryGetValue(bake.InputDefinitionId, out var raw) || raw.Kind != 1 ||
                            bake.StationCapability != (int)GridStationCapability.Bake || stage.ContainerDefinitionId != 0)
                            throw new InvalidOperationException("Invalid bake stage recipe");
                        NeedInputs(stage, bake.InputDefinitionId); NeedWhole(stage, bake.InputDefinitionId); NeedCapability(bake.StationCapability); units = UnitsFor(stage, bake.InputDefinitionId);
                        break;
                    case DishStageKind.Portion:
                        if (stage.RecipeId != 0 || !Compatible(stage.ContainerDefinitionId, output.Id)) throw new InvalidOperationException("Invalid portion stage container");
                        NeedInputs(stage, output.Id, stage.ContainerDefinitionId);
                        var producer = stages[stage.DependencyIds.Single(key => stages[key].OutputDefinitionId == output.Id)];
                        if (producer.Kind != (int)DishStageKind.Batch || batches[producer.RecipeId].ServingContainerDefinitionId != stage.ContainerDefinitionId)
                            throw new InvalidOperationException("Portion requires compatible batch");
                        units = Math.Min(UnitsFor(stage, output.Id), UnitsFor(stage, stage.ContainerDefinitionId));
                        break;
                    case DishStageKind.AddToContainer:
                        if (stage.RecipeId != 0 || !Compatible(stage.ContainerDefinitionId, output.Id) || items[stage.ContainerDefinitionId].ContainerCapacity != 1)
                            throw new InvalidOperationException("Invalid plate stage container");
                        NeedInputs(stage, output.Id, stage.ContainerDefinitionId); NeedWhole(stage, output.Id); units = Math.Min(UnitsFor(stage, output.Id), UnitsFor(stage, stage.ContainerDefinitionId));
                        break;
                    default: throw new InvalidOperationException("Unknown dish stage kind");
                }
                if (units <= 0) throw new InvalidOperationException("Insufficient raw supply for dish stage");
                visiting.Remove(id); available.Add(id, units); return units;
            }

            foreach (int id in menu)
            {
                if (!dishes.TryGetValue(id, out var dish) || id <= 0 || string.IsNullOrWhiteSpace(dish.Name) || !Compatible(dish.ContainerDefinitionId, dish.ContentDefinitionId))
                    throw new InvalidOperationException("Invalid menu dish / container");
                Visit(dish.FinalStageId); var final = stages[dish.FinalStageId];
                if (final.Kind is not ((int)DishStageKind.Portion or (int)DishStageKind.AddToContainer) ||
                    final.OutputDefinitionId != dish.ContentDefinitionId || final.ContainerDefinitionId != dish.ContainerDefinitionId)
                    throw new InvalidOperationException("Dish requires its actual final container stage");
            }
        }
    }
}
#endif
