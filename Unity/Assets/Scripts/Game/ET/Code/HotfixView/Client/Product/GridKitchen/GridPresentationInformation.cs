using System.Linq;

namespace ET.Client
{
    [FriendOf(typeof(GridPresentationComponent))]
    [FriendOf(typeof(GridClientComponent))]
    public static partial class GridPresentationSystem
    {
        public static string SelectedTargetDescription(this GridPresentationComponent self, GridClientComponent grid)
        {
            if (self.SelectedLevelId != grid.LevelId || self.SelectedLevelGeneration != grid.LevelGeneration ||
                !grid.Stations.TryGetValue(self.SelectedStationId, out GridStationProjection station))
            {
                return "目标：点击地图工位，或选择下方操作";
            }
            return $"目标：{station.Name} · {grid.ItemDescription(station.ItemId)}";
        }

        public static string SelectedTargetDetails(this GridPresentationComponent self, GridClientComponent grid)
        {
            if (!grid.Stations.TryGetValue(self.SelectedStationId, out GridStationProjection station))
            {
                return "选择工位后可查看内容、加工与操作位置。";
            }
            string position = $"位置：({station.Cell.X}, {station.Cell.Y})\n内容：{grid.ItemDescription(station.ItemId)}";
            position += station.UpgradeLevel == 0 ? "\n工位：未升级" : $"\n工位：等级 {station.UpgradeLevel} · {Tables.Instance.DTStationUpgradeConfig.Get(station.UpgradeDefinitionId).Name}";
            string work = string.Join("\n", grid.Processes.Values.Where(value => value.StationId == station.Id)
                .Select(GridClientSystem.WorkDescription));
            if (work.Length > 0)
            {
                position += "\n" + work;
            }
            if (grid.BakeProcesses.TryGetValue(station.BakeProcessId, out GridBakeProjection baking))
            {
                position += "\n" + GridClientSystem.BakeWorkDescription(baking);
            }
            if (station.PoolItemIds.Count > 0)
            {
                position += "\n餐具：" + string.Join("、", station.PoolItemIds.Select(grid.ItemDescription));
            }
            return position + StationCapabilityDetails(grid, station);
        }

        private static bool IsSelectedTarget(this GridPresentationComponent self, long stationId)
        {
            return stationId != 0 && self.SelectedStationId == stationId;
        }
    }
}
