#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public static class RestaurantConfig
    {
        public static DRLevelConfig FirstCooperativeLevel(ProductMenuVariant menuVariant)
        {
            return Tables.Instance.DTLevelConfig.DataList.Single(row => row.ChapterId == 2 && row.ChapterStage == 1 && row.MenuVariant == menuVariant);
        }

        public static int ChapterLevelCount(DRLevelConfig definition)
        {
            int count = 0;
            foreach (DRLevelConfig row in Tables.Instance.DTLevelConfig.DataList)
            {
                if (row.ChapterId == definition.ChapterId && row.MenuVariant == definition.MenuVariant)
                {
                    ++count;
                }
            }
            return count;
        }

        public static void ValidateLevelDefinitions(IEnumerable<DRLevelConfig> definitions, Func<GridCell, bool> canSpawn)
        {
            DRLevelConfig[] rows = definitions.ToArray();
            if (rows.Length == 0 || rows.Any(row => row.Id <= 0) || rows.Select(row => row.Id).Distinct().Count() != rows.Length)
                throw new InvalidOperationException("Invalid level definitions");
            var ids = rows.Select(row => row.Id).ToHashSet();
            if (!ids.Contains(1)) throw new InvalidOperationException("Missing first level definition");
            foreach (var row in rows)
            {
                if (row.AllowedDishIds.Count + row.AllowedDrinkIds.Count == 0 ||
                    row.AllowedDishIds.Distinct().Count() != row.AllowedDishIds.Count)
                {
                    throw new InvalidOperationException("Empty / duplicate level menu:" + row.Id);
                }
                var unlocked = RestaurantUnlockValues.Initial(row);
                RestaurantUnlockValues.Validate(unlocked);
                var permit = new ProductionPermit();
                ProductionPermissionRules.Rebuild(permit, row.AllowedDishIds.Concat(row.AllowedDrinkIds).Intersect(unlocked.DishIds), unlocked.StationIds);
                if (row.ChapterStage <= 1 && permit.DishIds.Count == 0)
                {
                    throw new InvalidOperationException("No initially producible dish:" + row.Id);
                }
                if (!ProductMenuCatalog.IsValid(row.MenuVariant) || row.MenuVariant == ProductMenuVariant.Basic && row.AllowedDrinkIds.Count != 0 ||
                    row.AllowedDrinkIds.Distinct().Count() != row.AllowedDrinkIds.Count || row.AllowedDrinkIds.Intersect(row.AllowedDishIds).Any())
                    throw new InvalidOperationException("Invalid level menu variant:" + row.Id);
                if ((row.IsChapterTerminal ? row.NextLevelConfigId != 0 || row.ChapterId <= 0 || row.AllowedRenovationIds.Count != 0
                        : !ids.Contains(row.NextLevelConfigId)) || row.OperatingTicks <= 0 || row.StarThresholds.Count != 3 ||
                    row.StarThresholds.Any(value => value <= 0) || !row.StarThresholds.SequenceEqual(row.StarThresholds.Distinct().OrderBy(value => value)) ||
                    !canSpawn(new GridCell(row.SpawnX, row.SpawnY)))
                    throw new InvalidOperationException($"Invalid level definition {row.Id}: next / duration / stars / spawn");
            }
            foreach (var chapter in rows.Where(row => row.ChapterId > 0).GroupBy(row => (row.ChapterId, row.MenuVariant)))
            {
                var stages = chapter.OrderBy(row => row.ChapterStage).ToArray();
                for (int i = 0; i < stages.Length; ++i)
                {
                    bool terminal = i == stages.Length - 1;
                    if (stages[i].ChapterStage != i + 1 || stages[i].IsChapterTerminal != terminal ||
                        stages[i].NextLevelConfigId != (terminal ? 0 : stages[i + 1].Id))
                    {
                        throw new InvalidOperationException($"Invalid chapter sequence {chapter.Key}");
                    }
                }
            }
            if (rows.Any(row => row.ChapterId < 0 || row.ChapterId == 0 && (row.ChapterStage != 0 || row.IsChapterTerminal)))
                throw new InvalidOperationException("Invalid legacy level chapter");
        }
    }
}
#endif
