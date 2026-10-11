#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    public readonly struct GridActorProjection
    {
        public readonly int MemberId, Facing;
        public readonly long Generation, StepStartTick, HandItemId, LastCommandId;
        public readonly GridCell Cell, NextCell, Goal;
        public readonly GridMotion Motion;
        public readonly GridResultStatus LastStatus;
        public readonly string LastReason;
        public readonly IReadOnlyList<GridCell> Path;
        public GridActorProjection(int member, long generation, GridCell cell, GridCell next, long start,
            GridMotion motion, int facing, long hand, GridCell goal, IReadOnlyList<GridCell> path, long command, GridResultStatus status, string reason)
        { MemberId = member; Generation = generation; Cell = cell; NextCell = next; StepStartTick = start; Motion = motion;
            Facing = facing; HandItemId = hand; Goal = goal; Path = path; LastCommandId = command; LastStatus = status; LastReason = reason; }
    }
    public readonly struct GridStationProjection
    {
        public readonly long Id, ItemId;
        public readonly GridCell Cell;
        public readonly string Name;
        public GridStationProjection(long id, GridCell cell, string name, long item) { Id = id; Cell = cell; Name = name; ItemId = item; }
    }
    public readonly struct GridItemProjection
    {
        public readonly long Id, HolderId;
        public readonly GridCell Cell;
        public readonly string Name;
        public readonly GridItemLocation Location;
        public readonly IReadOnlyList<string> Contents;
        public GridItemProjection(long id, string name, GridItemLocation location, long holder, GridCell cell, IReadOnlyList<string> contents)
        { Id = id; Name = name; Location = location; HolderId = holder; Cell = cell; Contents = contents; }
    }

    [ComponentOf(typeof(ClientSessionComponent))]
    public sealed class GridClientComponent : Entity, IAwake, IDestroy
    {
        public string MatchId, WorldId;
        public long WorldGeneration, BindingGeneration, Version, Tick, ReceivedFrameTime, NextCommandId, LastCommandId;
        public int MemberId, Width, Height, KitchenBottom, TickMilliseconds, StepTicks;
        public GridResultStatus LastStatus;
        public string LastReason;
        public bool Confirmed;
        public readonly Dictionary<int, GridActorProjection> Actors = new();
        public readonly Dictionary<long, GridStationProjection> Stations = new();
        public readonly Dictionary<long, GridItemProjection> Items = new();
    }
}
#endif
