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
            int[] menu = rows[0].AllowedDishIds.OrderBy(id => id).ToArray();
            if (menu.Length == 0 || rows.Any(row => !row.AllowedDishIds.OrderBy(id => id).SequenceEqual(menu)))
                throw new InvalidOperationException("Prototype levels require the same nonempty menu");
            if (!ids.Contains(1)) throw new InvalidOperationException("Missing first level definition");
            foreach (var row in rows)
            {
                if (!ids.Contains(row.NextLevelConfigId) || row.OperatingTicks <= 0 || row.StarThresholds.Count != 3 ||
                    row.StarThresholds.Any(value => value <= 0) || !row.StarThresholds.SequenceEqual(row.StarThresholds.Distinct().OrderBy(value => value)) ||
                    !canSpawn(new GridCell(row.SpawnX, row.SpawnY)))
                    throw new InvalidOperationException($"Invalid level definition {row.Id}: next / duration / stars / spawn");
            }
        }
    }
}
#endif
