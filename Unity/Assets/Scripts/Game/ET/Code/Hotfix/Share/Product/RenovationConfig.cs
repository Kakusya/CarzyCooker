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
            StationUpgradeConfig.ValidateAll();
            var definitions = GridStationConfig.ValidateDefinitions(stations);
            var rewards = new Dictionary<int, DRRenovationConfig>();
            foreach (DRRenovationConfig reward in source)
            {
                if (reward.RewardKind == RenovationRewardKind.ExpandSpace)
                {
                    var table = Tables.Instance.DTTableConfig.GetOrDefault(reward.TableDefinitionId);
                    if (reward.Id <= 0 || !rewards.TryAdd(reward.Id, reward) || string.IsNullOrWhiteSpace(reward.Name) ||
                        !KitchenGeometryConfig.Bounded(reward.ExpansionWidth, reward.ExpansionHeight) ||
                        reward.ExpansionWidth == GridKitchenRules.Width && reward.ExpansionHeight == GridKitchenRules.Height ||
                        reward.Quantity != 1 || reward.Orientation < GridOrientation.Unrotated || reward.Orientation > GridOrientation.CounterClockwise270 || reward.UpgradeDefinitionId != 0 ||
                        reward.UnlockDishIds.Count != 0 || reward.UnlockStationIds.Count != 0 || table == null || table.InitialLayoutId != 0 ||
                        !definitions.TryGetValue(reward.StationDefinitionId, out var expandedStation) || expandedStation.Kind != GridStationKind.Table)
                    {
                        throw new InvalidOperationException("InvalidExpansionReward:" + reward.Id);
                    }
                    KitchenGeometryConfig.ValidateExpansionLayout(reward, Tables.Instance.DTInitialLayoutConfig.DataList.Select(row =>
                        new GridStationPlacement(row.StationDefinitionId, new GridCell(row.X, row.Y), row.Orientation)),
                        Tables.Instance.DTLevelConfig.DataList.Select(row => new GridCell(row.SpawnX, row.SpawnY)));
                    continue;
                }
                if (reward.ExpansionWidth != 0 || reward.ExpansionHeight != 0 || reward.TableDefinitionId != 0)
                {
                    throw new InvalidOperationException("UnexpectedExpansionRewardFields:" + reward.Id);
                }
                if (reward.RewardKind == RenovationRewardKind.UnlockDefinitions)
                {
                    if (reward.Id <= 0 || !rewards.TryAdd(reward.Id, reward) || string.IsNullOrWhiteSpace(reward.Name) ||
                        reward.StationDefinitionId != 0 || reward.Quantity != 0 || reward.InstallX != 0 || reward.InstallY != 0 ||
                        reward.Orientation != GridOrientation.Unrotated || reward.UpgradeDefinitionId != 0 ||
                        reward.UnlockDishIds.Count + reward.UnlockStationIds.Count == 0 ||
                        reward.UnlockDishIds.Distinct().Count() != reward.UnlockDishIds.Count ||
                        reward.UnlockStationIds.Distinct().Count() != reward.UnlockStationIds.Count ||
                        reward.UnlockDishIds.Any(id => Tables.Instance.DTDishConfig.GetOrDefault(id) == null) ||
                        reward.UnlockStationIds.Any(id => !definitions.ContainsKey(id)))
                    {
                        throw new InvalidOperationException("InvalidUnlockReward:" + reward.Id);
                    }
                    continue;
                }
                if (reward.UnlockDishIds.Count != 0 || reward.UnlockStationIds.Count != 0)
                {
                    throw new InvalidOperationException("UnexpectedUnlockRewardFields:" + reward.Id);
                }
                if (reward.RewardKind == RenovationRewardKind.UpgradeStation)
                {
                    if (reward.Id <= 0 || !rewards.TryAdd(reward.Id, reward) || string.IsNullOrWhiteSpace(reward.Name) ||
                        reward.StationDefinitionId != 0 || reward.Quantity != 0 || reward.InstallX != 0 || reward.InstallY != 0 || reward.Orientation != GridOrientation.Unrotated ||
                        Tables.Instance.DTStationUpgradeConfig.GetOrDefault(reward.UpgradeDefinitionId) == null)
                        throw new InvalidOperationException("InvalidUpgradeReward:" + reward.Id);
                    continue;
                }
                if (reward.Id <= 0 || !rewards.TryAdd(reward.Id, reward) || string.IsNullOrWhiteSpace(reward.Name) ||
                    reward.RewardKind != RenovationRewardKind.InstallStation || reward.UpgradeDefinitionId != 0 ||
                    reward.Quantity <= 0 || reward.Quantity > (GridKitchenRules.Width - 2) * (GridKitchenRules.Height - 2) ||
                    reward.Orientation < GridOrientation.Unrotated || reward.Orientation > GridOrientation.CounterClockwise270 ||
                    !definitions.TryGetValue(reward.StationDefinitionId, out var station) ||
                    station.Kind is not (GridStationKind.Counter or GridStationKind.Chopping))
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
