#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public readonly struct GridCell : IEquatable<GridCell>
    {
        public int X { get; }
        public int Y { get; }
        public GridCell(int x, int y) { X = x; Y = y; }
        public bool Equals(GridCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCell other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public static bool operator ==(GridCell a, GridCell b) => a.Equals(b);
        public static bool operator !=(GridCell a, GridCell b) => !a.Equals(b);
        public override string ToString() => $"({X},{Y})";
    }

    public enum GridAction { Move = 1, TakeCounter = 2, PlaceCounter = 3, PickupDrop = 4, AddToContainer = 5, StartStationWork = 6, StartHeldWork = 7, StopWork = 8, PourContents = 9, StartAutoWork = 10, PortionSoup = 11, StartBake = 12, FillWater = 13, DrainLiquid = 14, DiscardSolid = 15, ReceiveSupply = 16, TakeSupplyPortion = 17, StartJuice = 18, PortionJuice = 19, BindDrink = 20, UnbindDrink = 21 }
    public enum GridMotion { Standing, Transit, Waiting }
    public enum GridItemLocation { Counter = 1, Hand = 2, Ground = 3, Container = 4, Table = 5, DirtyPool = 6, CleanPool = 7 }
    public enum GridStationKind { Counter, Chopping, Stove, Oven, Table, Wash, DirtyPool, CleanPool, Trash, Juicer, LabelDesk }
    [Flags]
    public enum GridStationCapability { None = 0, Chop = 1, Cook = 2, Bake = 4, Serve = 8, Wash = 16, DirtyPool = 32, CleanPool = 64, FillWater = 128, DrainLiquid = 256, Trash = 512, Juice = 1024, LabelDrink = 2048 }
    public enum GridMaterialForm { Solid = 1, Liquid = 2, Container = 3 }
    public enum GridAutoState { Cooking, Ready }
    public enum GridWorkState { Paused, Working, Completed }
    public enum GridResultStatus { Accepted = 1, PendingApproach = 2, Executed = 3, Rejected = 4, Cancelled = 5 }

    public static class GridKitchenRules
    {
        // 原型参数，非最终地图大小、平衡或人数上限。
        public const int Width = 7;
        public const int Height = 18;
        public const int KitchenBottom = 6;
        public const int TickMilliseconds = 50;
        public const int StepTicks = 5;
        public const int StandingCapacity = 2;
        public const int Tomato = 1, TomatoChunks = 2, Egg = 3, EggLiquid = 4, Bowl = 5;
        public const int Pot = 6, Soup = 7;
        public const int Bread = 8, Toast = 9, Plate = 10, Water = 11;
        public const int Orange = 15, OrangeChunks = 16, JuiceJug = 17, OrangeJuice = 18, DisposableCup = 19;
    }

    public readonly struct GridIntent : IEquatable<GridIntent>
    {
        public long CommandId { get; }
        public GridAction Action { get; }
        public GridCell Cell { get; }
        public long TargetId { get; }
        public long ExpectedItemId { get; }
        public long ExpectedTargetItemId { get; }
        public long ExpectedProcessId { get; }
        public long ExpectedOrderId { get; }
        // 清理意图冻结完整内容顺序及设施布局；不引用待回收请求的列表。
        private readonly IReadOnlyList<long> expectedContentIds;
        public IReadOnlyList<long> ExpectedContentIds => expectedContentIds ?? Array.Empty<long>();
        public int ExpectedRemainingPortions { get; }
        public long ExpectedLayoutVersion { get; }
        public ProductCommandKind OriginKind { get; }
        public GridIntent(long commandId, GridAction action, GridCell cell, long targetId = 0, long expectedItemId = 0, long expectedTargetItemId = 0, long expectedProcessId = 0,
            IEnumerable<long> expectedContents = null, int expectedRemainingPortions = 0, long expectedLayoutVersion = 0, ProductCommandKind originKind = ProductCommandKind.Grid, long expectedOrderId = 0)
        {
            CommandId = commandId; Action = action; Cell = cell; TargetId = targetId; ExpectedItemId = expectedItemId;
            ExpectedTargetItemId = expectedTargetItemId; ExpectedProcessId = expectedProcessId;
            expectedContentIds = expectedContents == null ? null : Array.AsReadOnly(expectedContents.ToArray());
            ExpectedRemainingPortions = expectedRemainingPortions; ExpectedLayoutVersion = expectedLayoutVersion;
            OriginKind = originKind;
            ExpectedOrderId = expectedOrderId;
        }
        public bool Equals(GridIntent other) => CommandId == other.CommandId && Action == other.Action && Cell == other.Cell && TargetId == other.TargetId && ExpectedItemId == other.ExpectedItemId && ExpectedTargetItemId == other.ExpectedTargetItemId && ExpectedProcessId == other.ExpectedProcessId
            && ExpectedRemainingPortions == other.ExpectedRemainingPortions && ExpectedLayoutVersion == other.ExpectedLayoutVersion && OriginKind == other.OriginKind && ExpectedOrderId == other.ExpectedOrderId && ExpectedContentIds.SequenceEqual(other.ExpectedContentIds);
    }

    public readonly struct GridMemberChange
    {
        public int MemberId { get; }
        public long Generation { get; }
        public bool Joined { get; }
        public GridMemberChange(int memberId, long generation, bool joined)
        { MemberId = memberId; Generation = generation; Joined = joined; }
    }

    public readonly struct GridResultValue
    {
        public int MemberId { get; }
        public long Generation { get; }
        public long CommandId { get; }
        public GridResultStatus Status { get; }
        public string Reason { get; }
        public long LevelId { get; }
        public long LevelGeneration { get; }
        public GridResultValue(int memberId, long generation, long commandId, GridResultStatus status, string reason, long levelId = 0, long levelGeneration = 0)
        { MemberId = memberId; Generation = generation; CommandId = commandId; Status = status; Reason = reason; LevelId = levelId; LevelGeneration = levelGeneration; }
    }

    [ComponentOf]
    public sealed class GridKitchenComponent : Entity, IAwake, IDestroy
    {
        public string WorldId;
        public long WorldGeneration;
        public long Tick;
        public long Version;
        public long LayoutVersion;
        public long LastFrameTime;
        public long Accumulator;
        public bool Stopped;
        public bool Dirty;
        public EntityRef<Match> Match;
        public EntityRef<RestaurantRuntime> Restaurant;
        public readonly HashSet<GridCell> Blocked = new();
        // 角色归当前 Level（旧 Host 样机仍为厨房 Child），其余对象归厨房；这里只索引。
        public readonly Dictionary<int, EntityRef<GridActor>> Actors = new();
        public readonly Dictionary<long, EntityRef<GridStation>> Stations = new();
        public readonly Dictionary<long, EntityRef<GridItem>> Items = new();
        public readonly Dictionary<long, EntityRef<GridManualProcess>> Processes = new();
        public readonly Dictionary<long, EntityRef<GridAutoProcess>> AutoProcesses = new();
        public readonly Dictionary<long, EntityRef<GridBakeProcess>> BakeProcesses = new();
        public readonly Queue<GridMemberChange> MemberChanges = new();
        public readonly Queue<GridStationLayoutEnvelope> LayoutCommands = new();
        public readonly Dictionary<int, GridIntent> Incoming = new();
        public readonly List<GridResultValue> Results = new();
        public readonly List<GridResultValue> PendingResults = new();
        public readonly Queue<KitchenWorkTransitionValue> WorkTransitions = new();
        public readonly List<KitchenWorkOutcomeValue> WorkOutcomes = new();
        // 固定步同步快照；MemberId 排序后逐次解引用，不能跨 await 或被容量查询覆盖。
        public readonly List<KeyValuePair<int,EntityRef<GridActor>>> ActorStepBuffer = new(8);
        public readonly List<EntityRef<GridManualProcess>> ManualStepBuffer = new(16);
        // 固定 7×18 厨房的同步 BFS 工作区；公开路线复制，不能跨搜索借用这些前驱或路径。
        public readonly int[] SearchQueue = new int[GridKitchenRules.Width*GridKitchenRules.Height];
        public readonly int[] SearchPrevious = new int[GridKitchenRules.Width*GridKitchenRules.Height];
        public readonly List<GridCell> ActorRouteBuffer = new(GridKitchenRules.Width*GridKitchenRules.Height);
    }

    [ChildOf]
    public sealed class GridActor : Entity, IAwake
    {
        public int MemberId;
        public long Generation;
        // Cell 只在一步完成时提交，断线落物不使用显示插值。
        public GridCell Cell;
        public GridCell NextCell;
        public long StepStartTick;
        public GridMotion Motion;
        public int Facing;
        public long HandItemId;
        public long WorkId;
        public long FrontWorkId, FrontClaimSerial;
        public GridCell Goal;
        public GridCell StopCell;
        public bool HasStopReservation;
        public readonly List<GridCell> Path = new();
        public int PathIndex;
        public GridIntent ActiveIntent;
        public bool HasIntent;
        // 业务编号属于绑定；与外层/内部 RpcId 无关。
        public long LastReceivedCommandId;
        public GridIntent LastReceivedIntent;
        public long LastResultCommandId;
        public GridResultStatus LastResultStatus;
        public string LastResultReason;
    }

    [ChildOf(typeof(GridKitchenComponent))]
    public sealed class GridStation : Entity, IAwake
    {
        public int InitialLayoutId;
        public int DefinitionId, Orientation, ItemSlotCapacity;
        public int UpgradeDefinitionId, UpgradeLevel;
        public GridStationCapability Capabilities;
        public GridCell Cell;
        public string Name;
        public long ItemId;
        public GridStationKind Kind;
        public long AutoProcessId;
        public long BakeProcessId;
        public readonly List<long> PoolItemIds = new();
    }

    [ChildOf(typeof(GridKitchenComponent))]
    public sealed class GridItem : Entity, IAwake
    {
        public ItemHygiene Hygiene;
        public string Name;
        public int DefinitionId;
        public GridItemLocation Location;
        public long HolderId;
        public GridCell Cell;
        public long AutoProcessId;
        public long BakeProcessId;
        // 内容始终属于这个容器，拿放不重新创建内容。
        public readonly List<long> Contents = new();
    }

    [ChildOf(typeof(GridKitchenComponent))]
    public sealed class GridManualProcess : Entity, IAwake
    {
        public long TransitionSerial;
        public bool CompletionQueued, CompletionReceived;
        public int RecipeId, RequiredTicks, ProgressTicks, WorkerMemberId;
        public long InputItemId, TargetItemId, StationId, WorkerGeneration, OutputItemId, StartedTick;
        public GridWorkState State;
    }

    public struct GridWorldCommitted { public long Version; }
    public struct GridProjectionApplied { public long Version; }
}
#endif
