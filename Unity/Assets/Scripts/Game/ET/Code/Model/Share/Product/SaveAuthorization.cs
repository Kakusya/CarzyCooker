#if !DOTNET
using System;
using MemoryPack;

namespace ET
{
    public enum ProgressCommitKind { Claim = 1, ImportBranch = 2 }
    public enum SaveAuthorizationKind { None = 0, ImportBranch = 1, Replace = 2 }
    public enum ProgressFileAction { ExportIdentity = 1, Inspect = 2, TrustIdentity = 3, ExportReadRun = 4 }
    public enum SaveAuthorizationAction
    {
        ExportImport = 1, ImportBranch = 2, ExportReplace = 3, PreviewReplace = 4, Replace = 5, RejectUnusedGrant = 6, PrepareOwnReplace = 7
    }

    // 这是已经提交的元数据，不是可恢复旧载荷；目标后来变化也保留最初修订。
    [MemoryPackable]
    public sealed partial class ProgressCommitReceipt : Object
    {
        public ProgressCommitKind Kind { get; set; }
        public string ResultId { get; set; } = "";
        public string GrantId { get; set; } = "";
        public string ConsumerProfileId { get; set; }
        public string ProfileId { get; set; }
        public string SlotId { get; set; }
        public int Revision { get; set; }
    }

    [MemoryPackable]
    public sealed partial class ImportBranchGrantValue : Object
    {
        public string GrantId { get; set; }
        public long ExpiresUtcTicks { get; set; }
        public byte[] Record { get; set; }
    }

    [MemoryPackable]
    public sealed partial class ReplaceSaveGrantValue : Object
    {
        public string GrantId { get; set; }
        public long ExpiresUtcTicks { get; set; }
        public string TargetProfileId { get; set; }
        public string TargetSlotId { get; set; }
        public int ExpectedRevision { get; set; }
        public string RootProfileId { get; set; }
        public string RootSlotId { get; set; }
        public int RootRevision { get; set; }
        public string RecipientProfileId { get; set; }
        public string RecipientKeyId { get; set; }
    }

    [MemoryPackable]
    public sealed partial class ProgressReplacePreview : Object
    {
        public string TicketId { get; set; }
        public string GrantId { get; set; }
        public string ProfileId { get; set; }
        public string SlotId { get; set; }
        public int Revision { get; set; }
        public string TargetResultId { get; set; }
        public string ResultId { get; set; }
        public string ConsumerProfileId { get; set; }
    }
}
#endif
