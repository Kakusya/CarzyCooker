#if !DOTNET
using System;
using MemoryPack;

namespace ET
{
    // 本端只读导航上下文；不序列化、不代表已读取或继续记录。
    public struct SoloSaveSelectionRequested { public string SlotId; public int Revision; }
    public enum SoloSaveRecordKind { Continue = 0, ChapterCompleted = 1 }
    public static class SoloSaveFormat { public const int Current = 12; }

    // 产品文件只存值和本文件内的引用键；不存 Entity、ActorId 或网络快照。
    [MemoryPackable]
    public sealed partial class SoloSaveHeader : Object
    {
        public int Version { get; set; } = SoloSaveFormat.Current;
        public string Platform { get; set; } = "PC";
        public string SlotId { get; set; }
        public string ProfileId { get; set; }
        public int Revision { get; set; } = 1;
        public string ResultId { get; set; }
        public string SourceSlotId { get; set; } = "";
        public int SourceRevision { get; set; }
        public int CompletedLevelId { get; set; }
        public int NextLevelId { get; set; }
        public int Score { get; set; }
        public int Stars { get; set; }
        public long SavedUtcTicks { get; set; }
        // Append-only layout: original v1 records have the first thirteen members and default to Continue.
        public SoloSaveRecordKind RecordKind { get; set; }
        public int ChapterId { get; set; }
        public int ChapterStage { get; set; }
        public int ServedOrders { get; set; }
        public int UnfulfilledOrders { get; set; }
        // v5：个人文件内的谱系与本机领取事实；无来源的新档以自身为根。
        private string sourceProfileId, rootProfileId, rootSlotId;
        private int rootRevision;
        public string SourceProfileId { get => sourceProfileId ?? (SourceSlotId == "" ? "" : ProfileId); set => sourceProfileId = value; }
        public string RootProfileId { get => rootProfileId ?? ProfileId; set => rootProfileId = value; }
        public string RootSlotId { get => rootSlotId ?? SlotId; set => rootSlotId = value; }
        public int RootRevision { get => rootRevision == 0 ? Revision : rootRevision; set => rootRevision = value; }
        public string SourceResultId { get; set; } = "";
        public string AuthorizationId { get; set; } = "";
        public string[] ClaimedResults { get; set; } = Array.Empty<string>();
        public string[] ClaimSlots { get; set; } = Array.Empty<string>();
        public int[] ClaimRevisions { get; set; } = Array.Empty<int>();
        public ProgressCommitReceipt[] Receipts { get; set; } = Array.Empty<ProgressCommitReceipt>();
    }

    [MemoryPackable]
    public sealed partial class SoloSavePayload : Object
    {
        public SoloSavedStation[] Stations { get; set; } = Array.Empty<SoloSavedStation>();
        public SoloSavedItem[] Items { get; set; } = Array.Empty<SoloSavedItem>();
        public SoloSavedManual[] ManualWork { get; set; } = Array.Empty<SoloSavedManual>();
        public SoloSavedBatch[] Batches { get; set; } = Array.Empty<SoloSavedBatch>();
        public SoloSavedBake[] Baking { get; set; } = Array.Empty<SoloSavedBake>();
        public SoloSavedSupplyBalance[] Supply { get; set; } = Array.Empty<SoloSavedSupplyBalance>();
        public SoloSavedShipment[] Shipments { get; set; } = Array.Empty<SoloSavedShipment>();
        public SoloSavedRenovation[] Renovations { get; set; } = Array.Empty<SoloSavedRenovation>();
        public int PendingRenovationId { get; set; }
        public int PendingUpgradeStationKey { get; set; }
        public int PendingUpgradeExpectedLevel { get; set; }
        public SavedRestaurantUnlocks Unlocks { get; set; }
        public SavedKitchenGeometry Geometry { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SavedKitchenGeometry : Object
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SoloSavedStation : Object
    {
        public int Key { get; set; }
        public int DefinitionId { get; set; }
        public int InitialLayoutId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Orientation { get; set; }
        public int ItemKey { get; set; }
        public int[] PoolItemKeys { get; set; } = Array.Empty<int>();
        public int TableDefinitionId { get; set; }
        public bool TableDirty { get; set; }
        public int UpgradeDefinitionId { get; set; }
        public int UpgradeLevel { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SoloSavedItem : Object
    {
        public int Key { get; set; }
        public int DefinitionId { get; set; }
        public int Location { get; set; }
        // StationKey for counter/table/pool, ItemKey for container, zero for ground.
        public int HolderKey { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Hygiene { get; set; }
        public int[] ContentKeys { get; set; } = Array.Empty<int>();
    }

    [MemoryPackable]
    public sealed partial class SoloSavedManual : Object
    {
        public int Key { get; set; }
        public int RecipeId { get; set; }
        public int RequiredTicks { get; set; }
        public int ProgressTicks { get; set; }
        public int InputKey { get; set; }
        public int TargetKey { get; set; }
        public int StationKey { get; set; }
        // Only unfinished work is persistent; it resumes paused without a worker.
    }

    [MemoryPackable]
    public sealed partial class SoloSavedBatch : Object
    {
        public int Key { get; set; }
        public int RecipeId { get; set; }
        public int RequiredTicks { get; set; }
        public int ProgressTicks { get; set; }
        public int PotKey { get; set; }
        public int StationKey { get; set; }
        public int State { get; set; }
        public int OutputDefinitionId { get; set; }
        public int TotalPortions { get; set; }
        public int RemainingPortions { get; set; }
        public int ServingContainerDefinitionId { get; set; }
        public int[] InputKeys { get; set; } = Array.Empty<int>();
        public int RequiredCapability { get; set; }
        public bool LockContainer { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SoloSavedBake : Object
    {
        public int Key { get; set; }
        public int RecipeId { get; set; }
        public int RequiredTicks { get; set; }
        public int ProgressTicks { get; set; }
        public int StationKey { get; set; }
        public int InputKey { get; set; }
        public int OutputDefinitionId { get; set; }
        public int CarrierKey { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SoloSavedSupplyBalance : Object
    {
        public int SourceId { get; set; }
        public int RemainingPackages { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SoloSavedShipment : Object
    {
        public int Key { get; set; }
        public int SourceId { get; set; }
        public int MaterialDefinitionId { get; set; }
        public int PackageDefinitionId { get; set; }
        public int PackageUnits { get; set; }
        public int RequiredTicks { get; set; }
        public int ProgressTicks { get; set; }
        public int ReceivingStationKey { get; set; }
        public int Stage { get; set; }
        // Received shipments retain their fact even if the empty shell was discarded.
        public int PackageKey { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SoloSavedRenovation : Object
    {
        public int DefinitionId { get; set; }
        public int[] StationKeys { get; set; } = Array.Empty<int>();
    }

    [ComponentOf(typeof(Scene))]
    public sealed class SoloSaveRepository : Entity, IAwake<string, string>, IAwake<string, string, Game.ProductStoragePlatform>
    {
        public string DirectoryPath;
        public string ProfileId;
        public Game.ProductStoragePlatform Platform;
        public ProductPreferencesValue Preferences;
        public string PreferencesFailure;
        public long PreferencesVersion;
    }

    [ChildOf(typeof(Match))]
    public sealed class SelectedSaveBaseline : Entity, IAwake<SoloSaveHeader, SoloSavePayload>
    {
        public SoloSaveHeader Header;
        public SoloSavePayload Payload;
    }

    [ComponentOf(typeof(Match))]
    public sealed class SoloSaveBinding : Entity, IAwake<string>
    {
        public string ProfileId;
        public EntityRef<SelectedSaveBaseline> Baseline;
        public string SourceSlotId;
        public int SourceRevision;
        public EntityRef<SoloSaveResult> CurrentResult;
        public SoloSaveHeader SourceHeader;
        public readonly System.Collections.Generic.Queue<SoloSaveCompletion> Completions = new();
    }

    public enum SoloSaveState { Ready = 0, Writing = 1, Failed = 2, Committed = 3 }

    [ChildOf(typeof(Match))]
    public sealed class SoloSaveResult : Entity, IAwake<SoloSaveHeader, SoloSavePayload, ProductCommandContext>
    {
        public string ResultId, SlotId;
        public long LevelId, LevelGeneration, WorldGeneration;
        public string WorldId;
        public byte[] Record;
        public SoloSaveState State;
        public string Failure;
        public EntityRef<HostPeer> Waiter;
        public ProductCommandContext Command;
    }

    public readonly struct SoloSaveCompletion
    {
        public readonly EntityRef<SoloSaveResult> Result;
        public readonly bool Committed;
        public readonly string Failure;
        public SoloSaveCompletion(SoloSaveResult result, bool committed, string failure)
        { Result = result; Committed = committed; Failure = failure; }
    }
}
#endif
