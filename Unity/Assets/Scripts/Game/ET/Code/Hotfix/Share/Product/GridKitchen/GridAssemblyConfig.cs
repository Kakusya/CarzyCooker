#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public static class GridAssemblyConfig
    {
        public static bool AllowsStoredContents(int carrierDefinitionId, IReadOnlyList<int> definitions)
        {
            if (AllowsContainerContents(carrierDefinitionId, definitions))
            {
                return true;
            }
            if (carrierDefinitionId != GridKitchenRules.ColdDrinkCup)
            {
                return false;
            }
            // 明确清理可移除一种物态，留下配方的有序子序列；恢复不补回缺失内容。
            foreach (var recipe in Tables.Instance.DTGridManualRecipeConfig.DataList)
            {
                if (recipe.Mode != GridManualMode.Assembly || recipe.ContainerDefinitionId != carrierDefinitionId)
                {
                    continue;
                }
                int index = 0;
                foreach (int definition in recipe.InputDefinitionIds.SelectMany((id, offset) => Enumerable.Repeat(id, recipe.InputCounts[offset])))
                {
                    if (index < definitions.Count && definitions[index] == definition)
                    {
                        ++index;
                    }
                }
                if (index == definitions.Count)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool AllowsContainerContents(int carrierDefinitionId, IReadOnlyList<int> definitions)
        {
            var carrier = Tables.Instance.DTGridItemConfig.Get(carrierDefinitionId);
            if (carrierDefinitionId == GridKitchenRules.ColdDrinkCup)
            {
                if (definitions.Count == 0 || definitions.Count == 1 && definitions[0] is GridKitchenRules.IcedMilkTea or GridKitchenRules.PearlMilkTea)
                {
                    return true;
                }
                foreach (var recipe in Tables.Instance.DTGridManualRecipeConfig.DataList)
                {
                    if (recipe.Mode != GridManualMode.Assembly || recipe.ContainerDefinitionId != carrierDefinitionId)
                    {
                        continue;
                    }
                    var ordered = recipe.InputDefinitionIds.SelectMany((id, index) => Enumerable.Repeat(id, recipe.InputCounts[index])).ToArray();
                    if (definitions.Count <= ordered.Length && definitions.SequenceEqual(ordered.Take(definitions.Count)))
                    {
                        return true;
                    }
                }
                return false;
            }
            if (carrierDefinitionId == GridKitchenRules.HotDrinkCup && definitions.Count == 0)
            {
                return true;
            }
            if (carrierDefinitionId == GridKitchenRules.HotDrinkCup && definitions.Count == 1 && Tables.Instance.DTDishConfig.DataList.Any(row =>
                row.ContainerDefinitionId == carrierDefinitionId && row.ContentDefinitionId == definitions[0]))
            {
                return true;
            }
            if (carrierDefinitionId != GridKitchenRules.HotDrinkCup && (definitions.Count <= 1 || !definitions.Any(carrier.ExclusiveDefinitionIds.Contains)))
            {
                return true;
            }
            // 吐司既可独立出餐，也可作为早餐组件；仅真实配方的部分投入可以合流。
            foreach (var recipe in Tables.Instance.DTGridManualRecipeConfig.DataList)
            {
                if (recipe.Mode != GridManualMode.Assembly || recipe.ContainerDefinitionId != carrierDefinitionId ||
                    definitions.Any(id => !recipe.InputDefinitionIds.Contains(id)))
                {
                    continue;
                }
                if (recipe.LastInputDefinitionId != 0 && definitions.Contains(recipe.LastInputDefinitionId) &&
                    (definitions.Count != recipe.InputCounts.Sum() || definitions[definitions.Count - 1] != recipe.LastInputDefinitionId))
                {
                    continue;
                }
                bool fits = true;
                for (int index = 0; index < recipe.InputDefinitionIds.Count; ++index)
                {
                    int count = 0;
                    foreach (int id in definitions)
                    {
                        if (id == recipe.InputDefinitionIds[index])
                        {
                            ++count;
                        }
                    }
                    if (count > recipe.InputCounts[index])
                    {
                        fits = false;
                        break;
                    }
                }
                if (fits)
                {
                    return true;
                }
            }
            return false;
        }

        public static void Validate(DRGridManualRecipeConfig recipe, IReadOnlyDictionary<int, DRGridItemConfig> items)
        {
            if (recipe.Mode != GridManualMode.Assembly || recipe.InputDefinitionId != 0 || recipe.RequiredTicks <= 0 ||
                recipe.InputDefinitionIds.Count == 0 || recipe.InputDefinitionIds.Count != recipe.InputCounts.Count ||
                recipe.InputDefinitionIds.Distinct().Count() != recipe.InputDefinitionIds.Count || recipe.InputCounts.Any(count => count <= 0) ||
                recipe.InputDefinitionIds.Contains(recipe.OutputDefinitionId) ||
                recipe.LastInputDefinitionId != 0 && (!recipe.InputDefinitionIds.Contains(recipe.LastInputDefinitionId) || recipe.InputCounts[recipe.InputDefinitionIds.IndexOf(recipe.LastInputDefinitionId)] != 1) ||
                !items.TryGetValue(recipe.OutputDefinitionId, out var output) || output.Kind != GridItemKind.Material ||
                !items.TryGetValue(recipe.ContainerDefinitionId, out var carrier) || carrier.Kind != GridItemKind.Container ||
                !carrier.AcceptedDefinitionIds.Contains(output.Id) || recipe.InputCounts.Sum() > carrier.ContainerCapacity ||
                recipe.InputDefinitionIds.Any(id => !items.TryGetValue(id, out var input) || input.Kind != GridItemKind.Material ||
                    (input.Form != GridMaterialForm.Solid && !(carrier.Id is GridKitchenRules.HotDrinkCup or GridKitchenRules.ColdDrinkCup && input.Form == GridMaterialForm.Liquid)) || !carrier.AcceptedDefinitionIds.Contains(id)))
            {
                throw new InvalidOperationException("Invalid assembly recipe / quantities / carrier");
            }
        }

        public static bool Matches(DRGridManualRecipeConfig recipe, IReadOnlyList<int> definitions)
        {
            if (recipe.Mode != GridManualMode.Assembly || definitions.Count != recipe.InputCounts.Sum() ||
                recipe.LastInputDefinitionId != 0 && (definitions.Count == 0 || definitions[definitions.Count - 1] != recipe.LastInputDefinitionId))
            {
                return false;
            }
            for (int index = 0; index < recipe.InputDefinitionIds.Count; ++index)
            {
                int count = 0;
                foreach (int definition in definitions)
                {
                    if (definition == recipe.InputDefinitionIds[index])
                    {
                        ++count;
                    }
                }
                if (count != recipe.InputCounts[index])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
#endif
