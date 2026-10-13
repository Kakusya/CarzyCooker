#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    public readonly struct GridActorProjection
    {
        public readonly int MemberId, Facing;
        public readonly long Generation, StepStartTick, HandItemId, LastCommandId, WorkId;
        public readonly long FrontWorkId, FrontClaimSerial;
        public readonly GridCell Cell, NextCell, Goal;
        public readonly GridMotion Motion;
        public readonly GridResultStatus LastStatus;
        public readonly string LastReason;
        public readonly IReadOnlyList<GridCell> Path;
        public GridActorProjection(int member, long generation, GridCell cell, GridCell next, long start,
            GridMotion motion, int facing, long hand, GridCell goal, IReadOnlyList<GridCell> path, long command, GridResultStatus status, string reason, long work = 0, long frontWork = 0, long frontClaim = 0)
        { MemberId = member; Generation = generation; Cell = cell; NextCell = next; StepStartTick = start; Motion = motion;
            Facing = facing; HandItemId = hand; Goal = goal; Path = path; LastCommandId = command; LastStatus = status; LastReason = reason; WorkId = work; FrontWorkId=frontWork;FrontClaimSerial=frontClaim; }
    }
    public readonly struct GridStationProjection
    {
        public readonly long Id, ItemId;
        public readonly GridCell Cell;
        public readonly string Name;
        public readonly GridStationKind Kind;
        public readonly long AutoProcessId, BakeProcessId;
        public readonly int DefinitionId, Orientation, ItemSlotCapacity;
        public readonly int WaterDefinitionId, WaterUnits;
        public readonly GridStationCapability Capabilities;
        public readonly IReadOnlyList<GridCell> Footprint, Approaches;
        public readonly IReadOnlyList<long> PoolItemIds;
        public GridStationProjection(long id, GridCell cell, string name, long item, GridStationKind kind = GridStationKind.Counter,
            long autoProcess = 0, int definition = 0, int orientation = 0, int capacity = 1, GridStationCapability capabilities = default,
            IReadOnlyList<GridCell> footprint = null, IReadOnlyList<GridCell> approaches = null, long bakeProcess = 0, IReadOnlyList<long> pool = null,
            int waterDefinition = 0, int waterUnits = 0)
        {
            Id = id; Cell = cell; Name = name; ItemId = item; Kind = kind; AutoProcessId = autoProcess;
            BakeProcessId = bakeProcess;
            PoolItemIds = pool ?? System.Array.Empty<long>();
            DefinitionId = definition; Orientation = orientation; ItemSlotCapacity = capacity; Capabilities = capabilities;
            WaterDefinitionId = waterDefinition; WaterUnits = waterUnits;
            Footprint = footprint ?? new[] { cell };
            Approaches = approaches ?? new[] { new GridCell(cell.X, cell.Y + 1), new GridCell(cell.X + 1, cell.Y), new GridCell(cell.X, cell.Y - 1), new GridCell(cell.X - 1, cell.Y) };
        }
    }
    public readonly struct GridItemProjection
    {
        public readonly long Id, HolderId;
        public readonly GridCell Cell;
        public readonly string Name;
        public readonly GridItemLocation Location;
        public readonly int DefinitionId;
        public readonly long AutoProcessId, BakeProcessId;
        public readonly IReadOnlyList<long> Contents;
        public readonly ItemHygiene Hygiene;
        public readonly GridMaterialForm Form;
        public readonly bool Discardable;
        public GridItemProjection(long id, string name, GridItemLocation location, long holder, GridCell cell, IReadOnlyList<long> contents, int definition = 0, long autoProcess = 0, long bakeProcess = 0, ItemHygiene hygiene = default,
            GridMaterialForm form = default, bool discardable = false)
        { Id = id; Name = name; Location = location; HolderId = holder; Cell = cell; Contents = contents; DefinitionId = definition; AutoProcessId = autoProcess; BakeProcessId = bakeProcess; Hygiene=hygiene; Form=form; Discardable=discardable; }
    }

    public readonly struct GridProcessProjection
    {
        public readonly long Id, InputItemId, TargetItemId, StationId, WorkerGeneration, OutputItemId;
        public readonly int RecipeId, RequiredTicks, ProgressTicks, WorkerMemberId;
        public readonly GridWorkState State;
        public GridProcessProjection(long id, int recipe, long input, long target, long station, int required, int progress, GridWorkState state, int worker, long generation, long output)
        { Id=id; RecipeId=recipe; InputItemId=input; TargetItemId=target; StationId=station; RequiredTicks=required; ProgressTicks=progress; State=state; WorkerMemberId=worker; WorkerGeneration=generation; OutputItemId=output; }
    }

    public readonly struct GridAutoProcessProjection
    {
        public readonly long Id, PotItemId, StartStationId, StartedTick;
        public readonly int RecipeId, RequiredTicks, ProgressTicks, OutputDefinitionId, TotalPortions, RemainingPortions, ServingContainerDefinitionId;
        public readonly GridAutoState State;
        public readonly IReadOnlyList<long> InputItemIds;
        public GridAutoProcessProjection(long id, int recipe, long pot, long station, long started, int required, int progress,
            int output, int total, int remaining, int serving, GridAutoState state, IReadOnlyList<long> inputs)
        {
            Id=id; RecipeId=recipe; PotItemId=pot; StartStationId=station; StartedTick=started; RequiredTicks=required; ProgressTicks=progress;
            OutputDefinitionId=output; TotalPortions=total; RemainingPortions=remaining; ServingContainerDefinitionId=serving; State=state; InputItemIds=inputs;
        }
    }

    public readonly struct GridBakeProjection
    {
        public readonly long Id, StationId, InputItemId, OutputItemId, StartedTick;
        public readonly int RecipeId, OutputDefinitionId, RequiredTicks, ProgressTicks;
        public readonly GridBakeState State;
        public GridBakeProjection(long id, int recipe, long station, long input, int outputDefinition, int required, int progress,
            long output, GridBakeState state, long started)
        { Id=id;RecipeId=recipe;StationId=station;InputItemId=input;OutputDefinitionId=outputDefinition;RequiredTicks=required;ProgressTicks=progress;OutputItemId=output;State=state;StartedTick=started; }
    }

    [ComponentOf(typeof(ClientSessionComponent))]
    public sealed class GridClientComponent : Entity, IAwake, IDestroy
    {
        public string MatchId, WorldId;
        public long WorldGeneration, BindingGeneration, Version, Tick, ReceivedFrameTime, NextCommandId, LastCommandId;
        public long LevelId, LevelGeneration;
        public long LayoutVersion;
        public IReadOnlyList<int> AllowedDishIds;
        public LevelPhase LevelPhase;
        public bool LevelPaused;
        public int LevelOperatingTicks, LevelElapsedTicks, LevelScore, LevelStars, LevelConfigId, NextLevelConfigId;
        public ProductCommandKind LastCommandKind;
        public int MemberId, Width, Height, KitchenBottom, TickMilliseconds, StepTicks;
        public GridResultStatus LastStatus;
        public string LastReason;
        public bool Confirmed;
        public readonly Dictionary<int, GridActorProjection> Actors = new();
        public readonly Dictionary<long, GridStationProjection> Stations = new();
        public readonly Dictionary<long, GridItemProjection> Items = new();
        public readonly Dictionary<long, GridProcessProjection> Processes = new();
        public readonly Dictionary<long, GridAutoProcessProjection> AutoProcesses = new();
        public readonly Dictionary<long, GridBakeProjection> BakeProcesses = new();
        public readonly Dictionary<long, GridCompanionProjection> Companions = new();
        public readonly Dictionary<long, GridFrontWorkProjection> FrontWorks = new();
        public readonly Dictionary<long, GridCustomerProjection> Customers = new();
        public readonly Dictionary<long, GridOrderProjection> Orders = new();
        public readonly Dictionary<long, GridTableProjection> Tables = new();
        public IReadOnlyList<long> WaitingCustomers;
        public int SkippedArrivals;
        public int BuffSeed, SelectedBuffId;
        public long BuffDrawVersion;
        public bool RequireBuffChoice;
        public IReadOnlyList<int> BuffCandidates;
        public readonly Dictionary<int, SupplySourceProjection> SupplySources = new();
        public readonly Dictionary<long, SupplyShipmentProjection> SupplyShipments = new();
        public IReadOnlyList<int> RenovationCandidates;
        public int SelectedRenovationId;
        public long RenovationChoiceVersion, RenovationLevelId, RenovationLevelGeneration;
        public readonly Dictionary<int, IReadOnlyList<long>> RenovationApplied = new();
        public bool SaveEnabled;
        public bool CanControlRestaurant;
        public string SaveResultId, SaveFailure, LastSavedSlotId;
        public SoloSaveState SaveState;
    }
}
#endif
