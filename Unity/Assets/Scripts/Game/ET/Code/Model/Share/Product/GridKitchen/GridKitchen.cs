#if !DOTNET
using System;
using System.Collections.Generic;

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

    public enum GridAction { Move = 1, TakeCounter = 2, PlaceCounter = 3, PickupDrop = 4 }
    public enum GridMotion { Standing, Transit, Waiting }
    public enum GridItemLocation { Counter = 1, Hand = 2, Ground = 3 }
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
    }

    public readonly struct GridIntent : IEquatable<GridIntent>
    {
        public long CommandId { get; }
        public GridAction Action { get; }
        public GridCell Cell { get; }
        public long TargetId { get; }
        public long ExpectedItemId { get; }
        public GridIntent(long commandId, GridAction action, GridCell cell, long targetId = 0, long expectedItemId = 0)
        { CommandId = commandId; Action = action; Cell = cell; TargetId = targetId; ExpectedItemId = expectedItemId; }
        public bool Equals(GridIntent other) => CommandId == other.CommandId && Action == other.Action && Cell == other.Cell && TargetId == other.TargetId && ExpectedItemId == other.ExpectedItemId;
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
        public GridResultValue(int memberId, long generation, long commandId, GridResultStatus status, string reason)
        { MemberId = memberId; Generation = generation; CommandId = commandId; Status = status; Reason = reason; }
    }

    [ComponentOf(typeof(Match))]
    public sealed class GridKitchenComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public string WorldId;
        public long WorldGeneration;
        public long Tick;
        public long Version;
        public long LastFrameTime;
        public long Accumulator;
        public bool Stopped;
        public bool Dirty;
        public readonly HashSet<GridCell> Blocked = new();
        // 仅索引本 owner 的 Children；不另持可写角色/物品副本。
        public readonly Dictionary<int, EntityRef<GridActor>> Actors = new();
        public readonly Dictionary<long, EntityRef<GridStation>> Stations = new();
        public readonly Dictionary<long, EntityRef<GridItem>> Items = new();
        public readonly Queue<GridMemberChange> MemberChanges = new();
        public readonly Dictionary<int, GridIntent> Incoming = new();
        public readonly List<GridResultValue> Results = new();
        public readonly List<GridResultValue> PendingResults = new();
    }

    [ChildOf(typeof(GridKitchenComponent))]
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
        public GridCell Cell;
        public string Name;
        public long ItemId;
    }

    [ChildOf(typeof(GridKitchenComponent))]
    public sealed class GridItem : Entity, IAwake
    {
        public string Name;
        public GridItemLocation Location;
        public long HolderId;
        public GridCell Cell;
        // 内容始终属于这个容器，拿放不重新创建内容。
        public readonly List<string> Contents = new();
    }

    public struct GridWorldCommitted { public long Version; }
    public struct GridProjectionApplied { public long Version; }
}
#endif
