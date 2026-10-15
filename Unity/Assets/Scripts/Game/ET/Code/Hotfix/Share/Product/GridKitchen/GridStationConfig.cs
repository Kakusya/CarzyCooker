#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public static class GridStationConfig
    {
        // 样机共用的前厅入口。出生位另由 LevelConfig 提供。
        public static GridCell Entrance() => new(3, 1);

        public static Dictionary<int, DRStationConfig> ValidateDefinitions(IEnumerable<DRStationConfig> definitions)
        {
            var rows = new Dictionary<int, DRStationConfig>();
            foreach (var row in definitions)
            {
                var footprint = ReadOffsets(row.FootprintOffsets);
                var approach = ReadOffsets(row.ApproachOffsets);
                bool pool = row.Kind is (int)GridStationKind.DirtyPool or (int)GridStationKind.CleanPool;
                int capability = row.Kind == (int)GridStationKind.Juicer ? (int)GridStationCapability.Juice
                    : row.Kind == (int)GridStationKind.LabelDesk ? (int)GridStationCapability.LabelDrink
                    : row.Kind == (int)GridStationKind.Grinder ? (int)GridStationCapability.Grind
                    : row.Kind == (int)GridStationKind.CoffeeMachine ? (int)GridStationCapability.Extract
                    : row.Kind == (int)GridStationKind.SteamMachine ? (int)(GridStationCapability.HeatMilk | GridStationCapability.FoamMilk)
                    : row.Kind == (int)GridStationKind.Mixer ? (int)GridStationCapability.Mixing
                    : row.Kind == (int)GridStationKind.Frying ? (int)GridStationCapability.Fry
                    : row.Kind == (int)GridStationKind.Trash ? (int)GridStationCapability.Trash
                    : row.Kind == 0 ? 0 : 1 << (row.Kind - 1);
                int sink = (int)(GridStationCapability.FillWater | GridStationCapability.DrainLiquid);
                bool isSink = row.Kind == (int)GridStationKind.Wash && row.Capabilities == (capability | sink);
                if (row.Id <= 0 || !rows.TryAdd(row.Id, row) || string.IsNullOrWhiteSpace(row.Name) ||
                    row.Kind < 0 || row.Kind > (int)GridStationKind.SteamMachine || (!isSink && row.Capabilities != capability) ||
                    (isSink ? row.WaterDefinitionId <= 0 || row.WaterUnits != 1 : row.WaterDefinitionId != 0 || row.WaterUnits != 0) ||
                    (pool ? row.ItemSlotCapacity <= 0 : row.ItemSlotCapacity != 1) ||
                    !footprint.Contains(new GridCell(0, 0)) || approach.Any(cell => footprint.Contains(cell) ||
                        !footprint.Any(part => Math.Abs(cell.X - part.X) + Math.Abs(cell.Y - part.Y) == 1)))
                    throw new InvalidOperationException($"Invalid station definition {row.Id}");
            }
            if (rows.Count == 0) throw new InvalidOperationException("Missing station definitions");
            return rows;
        }

        private static GridCell[] ReadOffsets(IReadOnlyList<int> values)
        {
            if (values.Count == 0 || values.Count % 2 != 0)
                throw new InvalidOperationException("Station offsets require nonempty x,y pairs");
            var cells = GridStationGeometry.Cells(values, default, 0);
            if (cells.Distinct().Count() != cells.Length) throw new InvalidOperationException("Duplicate station offsets");
            return cells;
        }

        public static HashSet<GridCell> ValidateInitialLayout(IEnumerable<DRStationConfig> definitions,
            IEnumerable<DRInitialLayoutConfig> layouts, IEnumerable<DRGridItemConfig> items, IEnumerable<GridCell> spawns)
        {
            var rows = ValidateDefinitions(definitions);
            var initial = layouts.ToArray();
            var itemIds = items.Select(row => row.Id).ToHashSet();
            if (initial.Length == 0 || initial.Any(row => row.Id <= 0 || string.IsNullOrWhiteSpace(row.Name) ||
                row.InitialItemDefinitionId != 0 && !itemIds.Contains(row.InitialItemDefinitionId)) ||
                initial.Select(row => row.Id).Distinct().Count() != initial.Length)
                throw new InvalidOperationException("Invalid initial station layout / item reference");
            return ValidateTerrain(rows, initial.Select(row => new GridStationPlacement(row.StationDefinitionId,
                new GridCell(row.X, row.Y), row.Orientation)), spawns);
        }

        public static HashSet<GridCell> ValidateTerrain(IReadOnlyDictionary<int, DRStationConfig> definitions,
            IEnumerable<GridStationPlacement> placements, IEnumerable<GridCell> requiredCells,
            int width = GridKitchenRules.Width, int height = GridKitchenRules.Height)
        {
            var blocked = new HashSet<GridCell>();
            var approaches = new List<GridCell[]>();
            foreach (var station in placements)
            {
                if (!definitions.TryGetValue(station.DefinitionId, out var definition) || station.Orientation < 0 || station.Orientation > 3)
                    throw new InvalidOperationException("Invalid station placement definition / orientation");
                foreach (var cell in GridStationGeometry.Cells(definition.FootprintOffsets, station.Cell, station.Orientation))
                    if (!GridStationGeometry.Inside(cell, width, height) || !blocked.Add(cell))
                        throw new InvalidOperationException("Station footprint overlaps / outside map");
                approaches.Add(GridStationGeometry.Cells(definition.ApproachOffsets, station.Cell, station.Orientation));
            }
            var reachable = Reachable(blocked, Entrance(), width, height);
            if (requiredCells.Any(cell => !reachable.Contains(cell)) || approaches.Any(cells => !cells.Any(reachable.Contains)))
                throw new InvalidOperationException("Station layout blocks entrance / spawn / approach");
            return blocked;
        }

        public static HashSet<GridCell> Reachable(HashSet<GridCell> blocked, GridCell start,
            int width = GridKitchenRules.Width, int height = GridKitchenRules.Height)
        {
            var seen = new HashSet<GridCell>(); var queue = new Queue<GridCell>();
            if (!GridStationGeometry.Inside(start, width, height) || blocked.Contains(start)) return seen;
            seen.Add(start); queue.Enqueue(start);
            while (queue.Count > 0)
                foreach (var next in GridStationGeometry.Neighbours(queue.Dequeue()))
                    if (GridStationGeometry.Inside(next, width, height) && !blocked.Contains(next) && seen.Add(next)) queue.Enqueue(next);
            return seen;
        }
    }
}
#endif
