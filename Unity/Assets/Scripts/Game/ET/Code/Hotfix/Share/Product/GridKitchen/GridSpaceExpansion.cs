#if !DOTNET
using System;
using System.Collections.Generic;

namespace ET
{
    public static partial class GridKitchenSystem
    {
        public static void ResizeGridGeometry(this GridKitchenComponent self, int width, int height)
        {
            if (!KitchenGeometryConfig.Bounded(width, height))
            {
                throw new InvalidOperationException("InvalidKitchenGeometry");
            }
            self.Width = width; self.Height = height;
            int count = checked(width * height);
            if (self.SearchQueue.Length < count)
            {
                Array.Resize(ref self.SearchQueue, count); Array.Resize(ref self.SearchPrevious, count);
            }
            if (self.ActorRouteBuffer.Capacity < count)
            {
                self.ActorRouteBuffer.Capacity = count;
            }
            self.ActorRouteBuffer.Clear();
        }

        public static string CheckSpaceExpansion(this GridKitchenComponent self, DRRenovationConfig reward, out HashSet<GridCell> terrain)
        {
            terrain = null;
            if (reward.RewardKind != RenovationRewardKind.ExpandSpace || reward.ExpansionWidth < self.Width || reward.ExpansionHeight < self.Height ||
                reward.ExpansionWidth == self.Width && reward.ExpansionHeight == self.Height)
            {
                return "ExpansionAlreadyInstalled";
            }
            string error = self.CheckRenovationInstallation(new[] { new GridStationPlacement(reward.StationDefinitionId,
                new GridCell(reward.InstallX, reward.InstallY), reward.Orientation) }, out terrain, reward.ExpansionWidth, reward.ExpansionHeight, true);
            if (error != null)
            {
                return error;
            }
            var reachable = GridStationConfig.Reachable(terrain, GridStationConfig.Entrance(), reward.ExpansionWidth, reward.ExpansionHeight);
            return reachable.Contains(KitchenGeometryConfig.ExpansionSeat(reward)) ? null : "RenovationBlocked";
        }

        // 只由已完整预检的装修继续或已验证的成功档恢复调用。
        public static GridStation InstallSpaceExpansion(this GridKitchenComponent self, DRRenovationConfig reward, HashSet<GridCell> terrain)
        {
            self.ResizeGridGeometry(reward.ExpansionWidth, reward.ExpansionHeight);
            var station = self.RestoreSaveStation(reward.StationDefinitionId, 0, new GridCell(reward.InstallX, reward.InstallY), reward.Orientation);
            station.Name = reward.Name;
            station.AddComponent<FrontTable, DRTableConfig>(Tables.Instance.DTTableConfig.Get(reward.TableDefinitionId));
            self.Blocked.Clear(); self.Blocked.UnionWith(terrain);
            ++self.LayoutVersion; self.Dirty = true;
            return station;
        }
    }
}
#endif
