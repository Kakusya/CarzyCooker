#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    public readonly struct GridStationPlacement
    {
        public readonly int DefinitionId, Orientation;
        public readonly GridCell Cell;
        public GridStationPlacement(int definition, GridCell cell, int orientation)
        { DefinitionId = definition; Cell = cell; Orientation = orientation; }
    }

    public static class GridStationGeometry
    {
        // 配置按 x,y 成对排列；朝向每次逆时针转 90 度。
        public static GridCell[] Cells(IReadOnlyList<int> offsets, GridCell origin, int orientation)
        {
            var cells = new GridCell[offsets.Count / 2];
            for (int i = 0; i < cells.Length; ++i)
            {
                int x = offsets[i * 2], y = offsets[i * 2 + 1];
                for (int turn = 0; turn < orientation; ++turn) (x, y) = (-y, x);
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
