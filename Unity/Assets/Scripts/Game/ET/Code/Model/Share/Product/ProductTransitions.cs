#if !DOTNET
namespace ET
{
    public enum KitchenProcessKind { Manual = 1, Soup = 2, Bake = 3, Juice = 4 }
    public enum KitchenTransitionAction { Complete = 1 }
    public enum KitchenWorkStage { Started = 1, Paused = 2, Resumed = 3, Finished = 4, Cancelled = 5 }
    public enum CustomerFlowAction { Arrive = 1, AdmitSeat = 2, ReachedSeat = 3, OrderExpired = 4, FinishedEating = 5, ReachedExit = 6, ReachedQueue = 7 }
    public enum FrontWorkStage { Claimed = 1, Started = 2, Released = 3, Finished = 4, Cancelled = 5 }
    public enum FrontWorkTransitionAction { Start = 1, Complete = 2 }
    public enum ShipmentTransitionAction { Ready = 1 }
    public enum LevelTransitionAction { Drain = 1, Complete = 2 }

    // 独立输入身份；队列仅持值，源对象和阶段仍由原领域 owner 管理。
    public readonly struct ProductTransitionIdentity
    {
        public readonly int ProtocolVersion;
        public readonly string MatchId, WorldId;
        public readonly long WorldGeneration, LevelId, LevelGeneration, SourceId, SourceInstanceId, Serial;
        public ProductTransitionIdentity(int protocol, string match, string world, long worldGeneration,
            long level, long levelGeneration, long source, long instance, long serial)
        {
            ProtocolVersion = protocol; MatchId = match; WorldId = world; WorldGeneration = worldGeneration;
            LevelId = level; LevelGeneration = levelGeneration; SourceId = source; SourceInstanceId = instance; Serial = serial;
        }
    }

    public readonly struct KitchenWorkTransitionValue
    {
        public readonly ProductTransitionIdentity Identity;
        public readonly KitchenProcessKind Kind;
        public readonly KitchenTransitionAction Action;
        public KitchenWorkTransitionValue(ProductTransitionIdentity identity, KitchenProcessKind kind, KitchenTransitionAction action)
        {
            Identity = identity; Kind = kind; Action = action;
        }
    }

    public readonly struct CustomerFlowTransitionValue
    {
        public readonly ProductTransitionIdentity Identity;
        public readonly CustomerFlowAction Action;
        public CustomerFlowTransitionValue(ProductTransitionIdentity identity, CustomerFlowAction action)
        {
            Identity = identity; Action = action;
        }
    }

    public readonly struct CustomerFlowFinishedValue
    {
        public readonly ProductTransitionIdentity Identity;
        public readonly CustomerPhase Phase;
        public readonly long TableStationId;
        public CustomerFlowFinishedValue(ProductTransitionIdentity identity, CustomerPhase phase, long tableStationId)
        {
            Identity = identity; Phase = phase; TableStationId = tableStationId;
        }
    }

    public readonly struct FrontWorkTransitionValue
    {
        public readonly ProductTransitionIdentity Identity;
        public readonly FrontWorkTransitionAction Action;
        public readonly long ClaimSerial;
        public FrontWorkTransitionValue(ProductTransitionIdentity identity, FrontWorkTransitionAction action, long claimSerial)
        {
            Identity = identity; Action = action; ClaimSerial = claimSerial;
        }
    }

    public readonly struct FrontWorkFinishedValue
    {
        public readonly ProductTransitionIdentity Identity;
        public readonly FrontWorkKind Kind;
        public readonly long StationId;
        public readonly long CustomerId;
        public readonly long ItemId;
        public readonly long ClaimSerial;
        public FrontWorkFinishedValue(ProductTransitionIdentity identity, FrontWorkKind kind, long stationId, long customerId, long itemId, long claimSerial)
        {
            Identity = identity; Kind = kind; StationId = stationId; CustomerId = customerId; ItemId = itemId; ClaimSerial = claimSerial;
        }
    }

    public readonly struct ShipmentTransitionValue
    {
        public readonly ProductTransitionIdentity Identity;
        public readonly ShipmentTransitionAction Action;
        public ShipmentTransitionValue(ProductTransitionIdentity identity, ShipmentTransitionAction action)
        {
            Identity = identity; Action = action;
        }
    }

    public readonly struct LevelTransitionValue
    {
        public readonly ProductTransitionIdentity Identity;
        public readonly LevelTransitionAction Action;
        public LevelTransitionValue(ProductTransitionIdentity identity, LevelTransitionAction action)
        {
            Identity = identity; Action = action;
        }
    }

    public readonly struct ProductCommandOutcomeValue
    {
        public readonly ProductCommandContext Context;
        public readonly ProductCommandKind Kind;
        public readonly GridResultStatus Status;
        public readonly string Reason;
        public readonly bool ChangedLevel;
        public ProductCommandOutcomeValue(ProductCommandContext context, ProductCommandKind kind, GridResultStatus status, string reason, bool changedLevel = false)
        { Context = context; Kind = kind; Status = status; Reason = reason; ChangedLevel = changedLevel; }
    }

    public readonly struct KitchenWorkOutcomeValue
    {
        public readonly KitchenProcessKind Kind;
        public readonly KitchenWorkStage Stage;
        public readonly long WorkId, WorkInstanceId, Serial, OutputItemId;
        public KitchenWorkOutcomeValue(KitchenProcessKind kind, KitchenWorkStage stage, Entity work, long serial, long output = 0)
        { Kind = kind; Stage = stage; WorkId = work.Id; WorkInstanceId = work.InstanceId; Serial = serial; OutputItemId = output; }
        public KitchenWorkOutcomeValue(KitchenProcessKind kind, KitchenWorkStage stage, long work, long instance, long serial, long output)
        { Kind = kind; Stage = stage; WorkId = work; WorkInstanceId = instance; Serial = serial; OutputItemId = output; }
    }

    public struct ProductCommandOutcomeApplied { public ProductCommandOutcomeValue Value; }
    public struct KitchenWorkOutcomeApplied { public KitchenWorkOutcomeValue Value; }

    public readonly struct ShipmentOutcomeValue
    {
        public readonly long ShipmentId, ShipmentInstanceId, Serial;
        public readonly ShipmentStage Stage;
        public ShipmentOutcomeValue(long shipment, long instance, long serial, ShipmentStage stage)
        { ShipmentId = shipment; ShipmentInstanceId = instance; Serial = serial; Stage = stage; }
    }
    public struct ShipmentOutcomeApplied { public ShipmentOutcomeValue Value; }

    public readonly struct FrontWorkCreateValue
    {
        public readonly ProductTransitionIdentity Identity;
        public readonly FrontWorkKind Kind;
        public FrontWorkCreateValue(ProductTransitionIdentity identity, FrontWorkKind kind) { Identity = identity; Kind = kind; }
    }
    public readonly struct FrontWorkOutcomeValue
    {
        public readonly long WorkId, WorkInstanceId, ClaimSerial;
        public readonly FrontWorkKind Kind;
        public readonly FrontWorkStage Stage;
        public FrontWorkOutcomeValue(long work, long instance, long claim, FrontWorkKind kind, FrontWorkStage stage)
        { WorkId = work; WorkInstanceId = instance; ClaimSerial = claim; Kind = kind; Stage = stage; }
    }
    public struct FrontWorkOutcomeApplied { public FrontWorkOutcomeValue Value; }
    public readonly struct BuffPreparedValue
    {
        public readonly long LevelId, LevelGeneration, DrawVersion;
        public BuffPreparedValue(long level, long generation, long draw) { LevelId = level; LevelGeneration = generation; DrawVersion = draw; }
    }
    public struct BuffPreparedApplied { public BuffPreparedValue Value; }
    public readonly struct RenovationPreparedValue
    {
        public readonly long LevelId, LevelGeneration, ChoiceVersion;
        public RenovationPreparedValue(long level, long generation, long choice) { LevelId = level; LevelGeneration = generation; ChoiceVersion = choice; }
    }
    public struct RenovationPreparedApplied { public RenovationPreparedValue Value; }
    public readonly struct LevelPhaseOutcomeValue
    {
        public readonly long LevelId, LevelGeneration;
        public readonly LevelPhase Phase;
        public readonly bool Paused;
        public LevelPhaseOutcomeValue(long level, long generation, LevelPhase phase, bool paused)
        { LevelId = level; LevelGeneration = generation; Phase = phase; Paused = paused; }
    }
    public struct LevelPhaseChangedApplied { public LevelPhaseOutcomeValue Value; }
}
#endif
