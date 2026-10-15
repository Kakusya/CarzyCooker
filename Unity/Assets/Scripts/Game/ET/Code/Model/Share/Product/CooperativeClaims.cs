#if !DOTNET
using System;
using System.Collections.Generic;
using MemoryPack;

namespace ET
{
    [MemoryPackable]
    public sealed partial class ProgressAuthorityValue : Object
    {
        public string AuthorityId { get; set; }
        public string MatchId { get; set; }
        public string WorldId { get; set; }
        public string HostProfileId { get; set; }
        public string HostKeyId { get; set; }
        public byte[] BaselineAuthorization { get; set; }
    }

    // 共同结果是纯成功事实，不伪造尚未领取的个人SaveSlot。
    [MemoryPackable]
    public sealed partial class CooperativeSuccessValue : Object
    {
        public string SaveResultId { get; set; }
        public string MatchId { get; set; }
        public string WorldId { get; set; }
        public int CompletedLevelId { get; set; }
        public int NextLevelId { get; set; }
        public int Score { get; set; }
        public int Stars { get; set; }
        public int ServedOrders { get; set; }
        public int UnfulfilledOrders { get; set; }
        public long CompletedUtcTicks { get; set; }
        public byte[] Payload { get; set; }
        public byte[] Authority { get; set; }
        public int ContentVersion { get; set; } = SoloSaveFormat.Current;
    }

    [MemoryPackable]
    public sealed partial class ProgressTicketValue : Object
    {
        public string TicketId { get; set; }
        public string EligibleProfileId { get; set; }
        public string EligibleKeyId { get; set; }
        public byte[] Success { get; set; }
    }

    [MemoryPackable]
    public sealed partial class ProgressCloseValue : Object
    {
        public string ReceiptId { get; set; }
        public string MatchId { get; set; }
        public string ProfileId { get; set; }
        public byte[] Authority { get; set; }
    }

    public enum ProgressReceiptState { Available = 0, Claimed = 1, Closed = 2 }
    public enum ProgressDeliveryKind { Ticket = 1, NormalClose = 2 }
    public enum ProgressRepositoryOperation
    {
        PrepareResult = 1, PersistTicket = 2, PersistClose = 3, Claim = 4, Browse = 5, SignClose = 6
    }

    [MemoryPackable]
    public sealed partial class ProgressTicketSummary : Object
    {
        public string TicketId { get; set; }
        public string SaveResultId { get; set; }
        public string MatchId { get; set; }
        public int CompletedLevelId { get; set; }
        public int Score { get; set; }
        public int Stars { get; set; }
        public ProgressReceiptState State { get; set; }
        public string SlotId { get; set; }
    }

    [ComponentOf(typeof(Match))]
    public sealed class CooperativeClaimState : Entity, IAwake, IUpdate, IDestroy
    {
        public long NextPulse;
        public bool PulsePending, Closing, CloseReady;
        public long CloseDeadline;
        public readonly Dictionary<string, EntityRef<CooperativeSaveResult>> Results = new();
        public readonly Dictionary<string, EntityRef<CooperativeCloseReceipt>> CloseReceipts = new();
    }

    [ChildOf(typeof(Match))]
    public sealed class CooperativeSaveResult : Entity, IAwake, IDestroy
    {
        public string ResultId, WorldId;
        public long LevelId, LevelGeneration;
        public byte[] UnsignedSuccess, SignedSuccess;
        public SoloSaveState State;
        public string Failure;
        public bool Preparing, HostSaveRequired, HostSaveCommitted;
        public ProgressCommitReceipt HostSavedReceipt;
        public readonly Dictionary<string, EntityRef<CooperativeClaim>> Claims = new();
        public LevelControlEnvelope? PendingControl;
    }

    [ChildOf(typeof(CooperativeSaveResult))]
    public sealed class CooperativeClaim : Entity, IAwake, IDestroy
    {
        public string TicketId, ProfileId, KeyId;
        public byte[] Document;
        public readonly HashSet<long> AckedBindings = new();
    }

    [ChildOf(typeof(Match))]
    public sealed class CooperativeCloseReceipt : Entity, IAwake, IDestroy
    {
        public string ReceiptId, ProfileId;
        public bool Preparing;
        public byte[] Document;
        public readonly HashSet<long> AckedBindings = new();
    }

    public readonly struct ProgressDeliveryValue
    {
        public readonly string Id, MatchId;
        public readonly long BindingGeneration;
        public readonly ProgressDeliveryKind Kind;
        public readonly byte[] Document;
        public ProgressDeliveryValue(string id, string matchId, long binding, ProgressDeliveryKind kind, byte[] document)
        {
            Id = id; MatchId = matchId; BindingGeneration = binding; Kind = kind; Document = document;
        }
    }

    [ComponentOf(typeof(ClientSessionComponent))]
    public sealed class ClientProgressDelivery : Entity, IAwake, IDestroy
    {
        public readonly Queue<ProgressDeliveryValue> Pending = new();
        public readonly HashSet<string> PendingIds = new();
        public bool Running;
        public string Failure;
    }

    public struct ProgressInboxChanged { }
    public struct ProgressClaimsOverlayChanged { public bool Visible; }
}
#endif
