#if !DOTNET
using System.Collections.Generic;
using System;

namespace ET
{
    public enum CompanionPost { FrontService = 1 }
    public enum FrontWorkKind { Ask = 1, Clear = 2, Wash = 3 }
    public enum FrontWorkState { Available, Approaching, Working, Completed, Cancelled }
    public enum FrontWorkerKind { None, Player, Companion }
    public enum FrontWorkAction { Claim = 1, Stop = 2 }

    public readonly struct FrontWorkIntent : IEquatable<FrontWorkIntent>
    {
        public readonly FrontWorkAction Action;
        public readonly long WorkId, ClaimSerial;
        public FrontWorkIntent(FrontWorkAction action, long work, long serial = 0) { Action = action; WorkId = work; ClaimSerial = serial; }
        public bool Equals(FrontWorkIntent other) => Action == other.Action && WorkId == other.WorkId && ClaimSerial == other.ClaimSerial;
    }

    public readonly struct FrontWorkEnvelope
    {
        public readonly FrontWorkerKind Kind;
        public readonly ProductCommandContext Context;
        public readonly FrontWorkIntent Intent;
        public readonly EntityRef<HostPeer> Peer;
        public readonly EntityRef<CompanionRole> Role;
        public readonly long RequestId;
        public FrontWorkEnvelope(ProductCommandContext context, FrontWorkIntent intent, HostPeer peer)
        { Kind = FrontWorkerKind.Player; Context = context; Intent = intent; Peer = peer; Role = default; RequestId = context.CommandId; }
        public FrontWorkEnvelope(FrontWorkIntent intent, CompanionRole role, long request)
        { Kind = FrontWorkerKind.Companion; Context = default; Intent = intent; Peer = default; Role = role; RequestId = request; }
    }

    [ComponentOf(typeof(Level))]
    public sealed class FrontWorkCommands : Entity, IAwake
    {
        public readonly Queue<FrontWorkEnvelope> Pending = new();
    }

    [ChildOf(typeof(RestaurantRuntime))]
    public sealed class Companion : Entity, IAwake<DRCompanionConfig>
    {
        public int DefinitionId;
        public string Name;
        public CompanionPost Post;
    }

    [ChildOf(typeof(Level))]
    public sealed class CompanionRole : Entity, IAwake<Companion, long>
    {
        public EntityRef<Companion> Companion;
        public long LevelGeneration;
        public GridCell Cell;
        public GridCell NextCell;
        public GridMotion Motion;
        public long StepStartTick;
        public readonly List<GridCell> Path = new();
        public int PathIndex;
        public long WorkId;
        public long ClaimSerial;
        // 只有一项在途物理请求；生命周期绑定本 Role 的 AsyncOwnerComponent。
        public bool RequestInFlight;
        public long NextRequestId;
        public long LastSubmittedRequestId;
        public long LastCommittedRequestId;
        public string LastWorkReason;
        public FrontWorkIntent LastSubmittedIntent;
        public GridResultStatus LastResultStatus;
        public int LastNotificationKind;
        public long NextScheduleTick;
    }

    [ComponentOf(typeof(Level))]
    public sealed class CompanionGrowth : Entity, IAwake
    {
        public readonly Dictionary<long, int> CompletedByCompanion = new();
    }

    // Customer change 创建的目标值。工作只保存身份，不保存顾客状态的副本。
    public readonly struct FrontWorkDefinition
    {
        public readonly FrontWorkKind Kind;
        public readonly long CustomerId;
        public readonly long StationId;
        public readonly long ItemId;
        public readonly int BaseTicks;
        public FrontWorkDefinition(FrontWorkKind kind, long customerId, long stationId, long itemId, int baseTicks)
        { Kind = kind; CustomerId = customerId; StationId = stationId; ItemId = itemId; BaseTicks = baseTicks; }
    }

    [ChildOf(typeof(Level))]
    public sealed class FrontWork : Entity, IAwake<FrontWorkDefinition, long>, ILevelFrontActivity
    {
        public FrontWorkKind Kind;
        public long LevelGeneration;
        public long CustomerId;
        public long StationId;
        public long ItemId;
        public int BaseTicks;
        public FrontWorkState State;
        public double Progress;
        public FrontWorkerKind WorkerKind;
        public long WorkerId;
        public long WorkerInstanceId;
        public long WorkerGeneration;
        public long ClaimSerial;
        public int FrozenTicks;
        public GridCell OperationCell;
        public long StartedTick;
        public long WorkerCommandId;
        public bool BlocksLevelCompletion => State != FrontWorkState.Completed && State != FrontWorkState.Cancelled;
    }
}
#endif
