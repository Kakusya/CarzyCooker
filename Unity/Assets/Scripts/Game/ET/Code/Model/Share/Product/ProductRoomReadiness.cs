#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    [ComponentOf(typeof(Match))]
    public sealed class ProductRoomReadiness : Entity, IAwake
    {
        public long ProposalVersion;
        public bool HasProposal;
        public ProductMenuVariant MenuVariant;
        public int ContentChapterId;
        public int FirstLevelConfigId;
        public readonly HashSet<(int Member, long Binding)> Ready = new();
    }

    public readonly struct ProductRoomReadyMember
    {
        public readonly int MemberId;
        public readonly long Binding;
        public readonly bool Online, Ready;
        public ProductRoomReadyMember(int member, long binding, bool online, bool ready)
        {
            MemberId = member; Binding = binding; Online = online; Ready = ready;
        }
    }
}
#endif
