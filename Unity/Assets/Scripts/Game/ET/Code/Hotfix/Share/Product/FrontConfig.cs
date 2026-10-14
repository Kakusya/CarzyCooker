#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public static class FrontConfig
    {
        public static void Validate(IEnumerable<DRCustomerFlowConfig> flowRows, IEnumerable<DROrderConfig> orderRows, IEnumerable<DRTableConfig> tableRows)
        {
            DRCustomerFlowConfig[] flows = flowRows.ToArray(); DROrderConfig[] orders = orderRows.ToArray(); DRTableConfig[] tables = tableRows.ToArray();
            var levels = Tables.Instance.DTLevelConfig.DataList.ToDictionary(row => row.Id);
            var dishes = Tables.Instance.DTDishConfig.DataList.ToDictionary(row => row.Id);
            var layouts = Tables.Instance.DTInitialLayoutConfig.DataList.ToDictionary(row => row.Id);
            var stations = GridStationConfig.ValidateDefinitions(Tables.Instance.DTStationConfig.DataList);
            if (flows.Length != levels.Count || flows.Select(row => row.Id).Distinct().Count() != flows.Length ||
                flows.Select(row => row.LevelConfigId).Distinct().Count() != flows.Length || orders.Length == 0 ||
                orders.Select(row => row.Id).Distinct().Count() != orders.Length || tables.Length == 0 ||
                tables.Select(row => row.Id).Distinct().Count() != tables.Length || tables.Select(row => row.InitialLayoutId).Distinct().Count() != tables.Length)
                throw new InvalidOperationException("Invalid front definitions / duplicate references");
            foreach (DROrderConfig order in orders)
                if (order.Id <= 0 || order.Score <= 0 || !dishes.ContainsKey(order.DishId))
                    throw new InvalidOperationException($"Invalid order definition {order.Id}");
            var orderMap = orders.ToDictionary(row => row.Id);
            var terrain = GridStationConfig.ValidateInitialLayout(stations.Values, layouts.Values, Tables.Instance.DTGridItemConfig.DataList,
                levels.Values.Select(row => new GridCell(row.SpawnX, row.SpawnY)));
            var reachable = GridStationConfig.Reachable(terrain, GridStationConfig.Entrance());
            foreach (DRCustomerFlowConfig flow in flows)
                if (flow.Id <= 0 || !levels.TryGetValue(flow.LevelConfigId, out var level) || flow.FirstArrivalTicks <= 0 ||
                    flow.ArrivalIntervalTicks <= 0 || flow.QueueCapacity <= 0 || flow.OrderWaitTicks <= 0 || flow.EatTicks <= 0 ||
                    flow.OrderConfigIds.Count == 0 || flow.OrderConfigIds.Any(id => !orderMap.ContainsKey(id) || !level.AllowedDishIds.Concat(level.AllowedDrinkIds).Contains(orderMap[id].DishId)) ||
                    new GridCell(flow.EntranceX, flow.EntranceY) != GridStationConfig.Entrance() || !reachable.Contains(new GridCell(flow.QueueX, flow.QueueY)))
                    throw new InvalidOperationException($"Invalid customer flow definition {flow.Id}");
            var tableLayouts = layouts.Values.Where(row => stations[row.StationDefinitionId].Kind == (int)GridStationKind.Table).ToArray();
            if (tableLayouts.Length != tables.Length) throw new InvalidOperationException("Each table requires one target definition");
            foreach (DRTableConfig table in tables)
            {
                if (table.Id <= 0 || !layouts.TryGetValue(table.InitialLayoutId, out var layout) ||
                    stations[layout.StationDefinitionId].Kind != (int)GridStationKind.Table || layout.InitialItemDefinitionId != 0)
                    throw new InvalidOperationException($"Invalid table definition {table.Id}");
                GridCell seat = GridStationGeometry.Cells(new[] { table.SeatOffsetX, table.SeatOffsetY }, new GridCell(layout.X, layout.Y), layout.Orientation)[0];
                if (!reachable.Contains(seat) || !GridStationGeometry.Cells(stations[layout.StationDefinitionId].ApproachOffsets,
                        new GridCell(layout.X, layout.Y), layout.Orientation).Contains(seat))
                    throw new InvalidOperationException($"Invalid table seat {table.Id}");
            }
            if (!layouts.Values.Any(row => stations[row.StationDefinitionId].Kind == (int)GridStationKind.Wash))
                throw new InvalidOperationException("Missing wash station");
            var utensilDefinitions = dishes.Values.Where(row => !Tables.Instance.DTGridItemConfig.Get(row.ContainerDefinitionId).Disposable).Select(row => row.ContainerDefinitionId).ToHashSet();
            int utensils = layouts.Values.Count(row => utensilDefinitions.Contains(row.InitialItemDefinitionId));
            foreach (GridStationKind kind in new[] { GridStationKind.DirtyPool, GridStationKind.CleanPool })
            {
                var pools = layouts.Values.Where(row => stations[row.StationDefinitionId].Kind == (int)kind).ToArray();
                if (pools.Length == 0 || pools.Any(row => row.InitialItemDefinitionId != 0) ||
                    pools.Sum(row => stations[row.StationDefinitionId].ItemSlotCapacity) < utensils)
                    throw new InvalidOperationException($"Insufficient finite utensil pool {kind}");
            }
        }
    }
}
#endif
