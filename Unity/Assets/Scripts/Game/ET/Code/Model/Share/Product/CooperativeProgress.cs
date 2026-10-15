#if !DOTNET
using MemoryPack;

namespace ET
{
    [MemoryPackable]
    public sealed partial class OfflineProfileIdentity : Object
    {
        public int Version { get; set; } = 1;
        public string Platform { get; set; } = "PC";
        public string ProfileId { get; set; }
        public string KeyId { get; set; }
        public string DisplayName { get; set; } = "本机玩家";
        public string PublicKeyXml { get; set; }
    }

    [MemoryPackable]
    public sealed partial class ProductProfileProofValue : Object
    {
        public string MatchId { get; set; }
        public long BindingGeneration { get; set; }
        public string Challenge { get; set; }
    }

    // Owner 对 ReadRun 及指定 Host/Match 结果签发的完整授权；不含覆盖权。
    [MemoryPackable]
    public sealed partial class CooperativeBaselineValue : Object
    {
        public string GrantId { get; set; }
        public string MatchId { get; set; }
        public string HostProfileId { get; set; }
        public string HostKeyId { get; set; }
        public bool DelegateMatchResults { get; set; }
        public string RootProfileId { get; set; }
        public string RootSlotId { get; set; }
        public int RootRevision { get; set; }
        public byte[] Record { get; set; }
    }

    [ComponentOf(typeof(Match))]
    public sealed class CooperativeSaveBinding : Entity, IAwake
    {
        public EntityRef<SelectedSaveBaseline> Baseline;
        public byte[] Authorization;
        public string RootProfileId, RootSlotId;
        public int RootRevision;
    }
}
#endif
