#if !DOTNET
using System;
using MemoryPack;

namespace ET
{
    public struct SoloSaveSelectionRequested { }

    // 产品文件只存值和本文件内的引用键；不存 Entity、ActorId 或网络快照。
    [MemoryPackable]
    public sealed partial class SoloSaveHeader : Object
    {
        public int Version { get; set; } = 1;
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
    public sealed class SoloSaveRepository : Entity, IAwake<string, string>
    {
        public string DirectoryPath;
        public string ProfileId;
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
        public readonly System.Collections.Generic.Queue<SoloSaveCompletion> Completions = new();
    }

    public enum SoloSaveState { Ready, Writing, Failed, Committed }

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
