#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public static class KitchenGeometryConfig
    {
        // 仅限制配置与一次性寻路缓冲的容量；玩家不能提交任意尺寸。
        public static bool Bounded(int width, int height) => width >= GridKitchenRules.Width && height >= GridKitchenRules.Height && (long)width * height <= 1024;

        public static void AdaptLegacy(SoloSaveHeader header, SoloSavePayload payload)
        {
            if (header.Version < 9 && payload.Geometry == null)
            {
                payload.Geometry = new SavedKitchenGeometry { Width = GridKitchenRules.Width, Height = GridKitchenRules.Height };
            }
        }

        public static SavedKitchenGeometry Read(SoloSaveHeader header, SoloSavePayload payload)
        {
            var value = payload.Geometry;
            if (value == null && header.Version < 9)
            {
                value = new SavedKitchenGeometry { Width = GridKitchenRules.Width, Height = GridKitchenRules.Height };
            }
            if (value == null || !Bounded(value.Width, value.Height))
            {
                throw new InvalidOperationException("InvalidKitchenGeometry");
            }
            var expansions = payload.Renovations.Select(row => Tables.Instance.DTRenovationConfig.GetOrDefault(row.DefinitionId))
                .Where(row => row?.RewardKind == RenovationRewardKind.ExpandSpace).ToArray();
            bool original = value.Width == GridKitchenRules.Width && value.Height == GridKitchenRules.Height;
            if (original ? expansions.Length != 0 : header.Version < 9 || expansions.Length != 1 ||
                expansions[0].ExpansionWidth != value.Width || expansions[0].ExpansionHeight != value.Height)
            {
                throw new InvalidOperationException("UnownedKitchenGeometry");
            }
            return value;
        }

        public static GridCell ExpansionSeat(DRRenovationConfig reward)
        {
            var table = Tables.Instance.DTTableConfig.Get(reward.TableDefinitionId);
            return GridStationGeometry.Cells(new[] { table.SeatOffsetX, table.SeatOffsetY },
                new GridCell(reward.InstallX, reward.InstallY), reward.Orientation)[0];
        }

        public static HashSet<GridCell> ValidateExpansionLayout(DRRenovationConfig reward, IEnumerable<GridStationPlacement> existing,
            IEnumerable<GridCell> required)
        {
            var definitions = GridStationConfig.ValidateDefinitions(Tables.Instance.DTStationConfig.DataList);
            var placement = new GridStationPlacement(reward.StationDefinitionId, new GridCell(reward.InstallX, reward.InstallY), reward.Orientation);
            GridCell seat = ExpansionSeat(reward);
            if (!GridStationGeometry.Cells(definitions[reward.StationDefinitionId].ApproachOffsets, placement.Cell, placement.Orientation).Contains(seat))
            {
                throw new InvalidOperationException("InvalidExpansionSeat");
            }
            return GridStationConfig.ValidateTerrain(definitions, existing.Append(placement), required.Append(seat), reward.ExpansionWidth, reward.ExpansionHeight);
        }
    }
}
#endif
