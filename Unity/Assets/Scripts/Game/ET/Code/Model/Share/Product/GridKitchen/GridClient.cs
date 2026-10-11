#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    public readonly struct GridActorProjection
    {
        public readonly int MemberId, Facing;
        public readonly long Generation, StepStartTick, HandItemId, LastCommandId, WorkId;
        public readonly GridCell Cell, NextCell, Goal;
        public readonly GridMotion Motion;
        public readonly GridResultStatus LastStatus;
        public readonly string LastReason;
        public readonly IReadOnlyList<GridCell> Path;
        public GridActorProjection(int member, long generation, GridCell cell, GridCell next, long start,
            GridMotion motion, int facing, long hand, GridCell goal, IReadOnlyList<GridCell> path, long command, GridResultStatus status, string reason, long work = 0)
        { MemberId = member; Generation = generation; Cell = cell; NextCell = next; StepStartTick = start; Motion = motion;
            Facing = facing; HandItemId = hand; Goal = goal; Path = path; LastCommandId = command; LastStatus = status; LastReason = reason; WorkId = work; }
    }
    public readonly struct GridStationProjection
    {
        public readonly long Id, ItemId;
        public readonly GridCell Cell;
        public readonly string Name;
        public readonly GridStationKind Kind;
        public GridStationProjection(long id, GridCell cell, string name, long item, GridStationKind kind = GridStationKind.Counter) { Id = id; Cell = cell; Name = name; ItemId = item; Kind = kind; }
    }
    public readonly struct GridItemProjection
    {
        public readonly long Id, HolderId;
        public readonly GridCell Cell;
        public readonly string Name;
        public readonly GridItemLocation Location;
        public readonly int DefinitionId;
        public readonly IReadOnlyList<long> Contents;
        public GridItemProjection(long id, string name, GridItemLocation location, long holder, GridCell cell, IReadOnlyList<long> contents, int definition = 0)
        { Id = id; Name = name; Location = location; HolderId = holder; Cell = cell; Contents = contents; DefinitionId = definition; }
    }

    public readonly struct GridProcessProjection
    {
        public readonly long Id, InputItemId, TargetItemId, StationId, WorkerGeneration, OutputItemId;
        public readonly int RecipeId, RequiredTicks, ProgressTicks, WorkerMemberId;
        public readonly GridWorkState State;
        public GridProcessProjection(long id, int recipe, long input, long target, long station, int required, int progress, GridWorkState state, int worker, long generation, long output)
        { Id=id; RecipeId=recipe; InputItemId=input; TargetItemId=target; StationId=station; RequiredTicks=required; ProgressTicks=progress; State=state; WorkerMemberId=worker; WorkerGeneration=generation; OutputItemId=output; }
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
        public readonly Dictionary<long, GridProcessProjection> Processes = new();
    }
}
#endif
