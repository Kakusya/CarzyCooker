#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    public readonly struct GridStationPlacement
    {
        public readonly int DefinitionId;
        public readonly GridOrientation Orientation;
        public readonly GridCell Cell;
        public GridStationPlacement(int definition, GridCell cell, GridOrientation orientation)
        { DefinitionId = definition; Cell = cell; Orientation = orientation; }
    }

    public static class GridStationGeometry
    {
        public static GridOrientation NextOrientation(GridOrientation value) => value switch
        {
            GridOrientation.Unrotated => GridOrientation.CounterClockwise90,
            GridOrientation.CounterClockwise90 => GridOrientation.HalfTurn,
            GridOrientation.HalfTurn => GridOrientation.CounterClockwise270,
            GridOrientation.CounterClockwise270 => GridOrientation.Unrotated,
            _ => throw new System.ArgumentOutOfRangeException(nameof(value))
        };

        // 配置按 x,y 成对排列；朝向每次逆时针转 90 度。
        public static GridCell[] Cells(IReadOnlyList<int> offsets, GridCell origin, GridOrientation orientation)
        {
            if (orientation is < GridOrientation.Unrotated or > GridOrientation.CounterClockwise270)
            {
                throw new System.ArgumentOutOfRangeException(nameof(orientation));
            }
            var cells = new GridCell[offsets.Count / 2];
            for (int i = 0; i < cells.Length; ++i)
            {
                int x = offsets[i * 2], y = offsets[i * 2 + 1];
                for (int turn = 0; turn < (int)orientation; ++turn) (x, y) = (-y, x);
                cells[i] = new GridCell(origin.X + x, origin.Y + y);
            }
            return cells;
        }

        public static bool Inside(GridCell cell, int width = GridKitchenRules.Width, int height = GridKitchenRules.Height) =>
            cell.X > 0 && cell.X < width - 1 && cell.Y > 0 && cell.Y < height - 1;

        public static GridCell[] Neighbours(GridCell cell) => new[] { new GridCell(cell.X, cell.Y + 1),
            new GridCell(cell.X + 1, cell.Y), new GridCell(cell.X, cell.Y - 1), new GridCell(cell.X - 1, cell.Y) };
    }
}
#endif
