#if !DOTNET
namespace ET
{
    [FriendOf(typeof(GridStation))]
    public static class GridStationAccess
    {
        public static GridCell[] StationFootprint(this GridStation station) => GridStationGeometry.Cells(
            Tables.Instance.DTStationConfig.Get(station.DefinitionId).FootprintOffsets, station.Cell, station.Orientation);

        public static GridCell[] StationApproaches(this GridStation station) => GridStationGeometry.Cells(
            Tables.Instance.DTStationConfig.Get(station.DefinitionId).ApproachOffsets, station.Cell, station.Orientation);
    }
}
#endif
