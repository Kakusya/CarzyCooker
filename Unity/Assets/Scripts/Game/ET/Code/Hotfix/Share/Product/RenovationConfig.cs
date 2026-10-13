#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public static class RenovationConfig
    {
        public static void ValidateAll() => Validate(Tables.Instance.DTRenovationConfig.DataList,
            Tables.Instance.DTStationConfig.DataList, Tables.Instance.DTLevelConfig.DataList);

        public static Dictionary<int, DRRenovationConfig> Validate(IEnumerable<DRRenovationConfig> source,
            IEnumerable<DRStationConfig> stations, IEnumerable<DRLevelConfig> levels)
        {
            var definitions = GridStationConfig.ValidateDefinitions(stations);
            var rewards = new Dictionary<int, DRRenovationConfig>();
            foreach (DRRenovationConfig reward in source)
            {
                if (reward.Id <= 0 || !rewards.TryAdd(reward.Id, reward) || string.IsNullOrWhiteSpace(reward.Name) ||
                    reward.Quantity <= 0 || reward.Quantity > (GridKitchenRules.Width - 2) * (GridKitchenRules.Height - 2) ||
                    reward.Orientation < 0 || reward.Orientation > 3 ||
                    !definitions.TryGetValue(reward.StationDefinitionId, out var station) ||
                    station.Kind is not ((int)GridStationKind.Counter or (int)GridStationKind.Chopping))
                    throw new InvalidOperationException($"Invalid renovation / station reference {reward.Id}");
                var cells = Placements(reward).SelectMany(placement => GridStationGeometry.Cells(station.FootprintOffsets, placement.Cell, placement.Orientation)).ToArray();
                if (cells.Any(cell => !GridStationGeometry.Inside(cell)) || cells.Distinct().Count() != cells.Length)
                    throw new InvalidOperationException($"Invalid renovation footprint {reward.Id}");
            }
            foreach (DRLevelConfig level in levels)
                if (level.AllowedRenovationIds.Distinct().Count() != level.AllowedRenovationIds.Count ||
                    level.AllowedRenovationIds.Any(id => !rewards.ContainsKey(id)))
                    throw new InvalidOperationException($"Invalid level renovation references {level.Id}");
            return rewards;
        }

        // 数量沿安装锚点的本地方向排布；实际占地与公共通路仍须完整预检。
        public static IEnumerable<GridStationPlacement> Placements(DRRenovationConfig definition)
        {
            for (int i = 0; i < definition.Quantity; ++i)
                yield return new GridStationPlacement(definition.StationDefinitionId,
                    GridStationGeometry.Cells(new[] { i, 0 }, new GridCell(definition.InstallX, definition.InstallY), definition.Orientation)[0], definition.Orientation);
        }
    }
}
#endif
