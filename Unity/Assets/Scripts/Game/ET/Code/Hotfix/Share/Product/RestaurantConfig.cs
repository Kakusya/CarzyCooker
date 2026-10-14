#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public static class RestaurantConfig
    {
        public static void ValidateLevelDefinitions(IEnumerable<DRLevelConfig> definitions, Func<GridCell, bool> canSpawn)
        {
            DRLevelConfig[] rows = definitions.ToArray();
            if (rows.Length == 0 || rows.Any(row => row.Id <= 0) || rows.Select(row => row.Id).Distinct().Count() != rows.Length)
                throw new InvalidOperationException("Invalid level definitions");
            var ids = rows.Select(row => row.Id).ToHashSet();
            foreach (var chapter in rows.GroupBy(row => (row.ChapterId, row.MenuVariant)))
            {
                int[] menu = chapter.First().AllowedDishIds.OrderBy(id => id).ToArray();
                int[] drinks = chapter.First().AllowedDrinkIds.OrderBy(id => id).ToArray();
                if (menu.Length == 0 || chapter.Any(row => !row.AllowedDishIds.OrderBy(id => id).SequenceEqual(menu) ||
                    !row.AllowedDrinkIds.OrderBy(id => id).SequenceEqual(drinks)))
                    throw new InvalidOperationException("Chapter levels require the same nonempty menu");
            }
            if (!ids.Contains(1)) throw new InvalidOperationException("Missing first level definition");
            foreach (var row in rows)
            {
                if (row.MenuVariant is not (0 or 1) || row.MenuVariant == 0 && row.AllowedDrinkIds.Count != 0 ||
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
                if (stages.Length != 3 || !stages.Select(row => row.ChapterStage).SequenceEqual(new[] { 1, 2, 3 }) ||
                    stages[0].IsChapterTerminal || stages[1].IsChapterTerminal || !stages[2].IsChapterTerminal ||
                    stages[0].NextLevelConfigId != stages[1].Id || stages[1].NextLevelConfigId != stages[2].Id)
                    throw new InvalidOperationException($"Invalid three-level chapter {chapter.Key}");
            }
            if (rows.Any(row => row.ChapterId < 0 || row.ChapterId == 0 && (row.ChapterStage != 0 || row.IsChapterTerminal)))
                throw new InvalidOperationException("Invalid legacy level chapter");
        }
    }
}
#endif
