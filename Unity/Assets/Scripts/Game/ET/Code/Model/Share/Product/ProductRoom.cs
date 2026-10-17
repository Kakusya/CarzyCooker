#if !DOTNET
using System;

namespace ET
{
    public enum ProductRoomAction { StartRestaurant = 1, CloseRestaurant = 2, PublishProposal = 3, Ready = 4, CancelReady = 5 }

    public readonly struct ProductRoomIntent : IEquatable<ProductRoomIntent>
    {
        public readonly long Version;
        public readonly long ProposalVersion;
        public readonly ProductRoomAction Action;
        public readonly ProductMenuVariant MenuVariant;
        public readonly int ContentChapterId;

        public ProductRoomIntent(long version, ProductRoomAction action, ProductMenuVariant menuVariant, int contentChapterId = 0, long proposalVersion = 0)
        {
            Version = version;
            ProposalVersion = proposalVersion;
            Action = action;
            MenuVariant = menuVariant;
            ContentChapterId = contentChapterId;
        }

        public bool Equals(ProductRoomIntent other) => Version == other.Version && ProposalVersion == other.ProposalVersion && Action == other.Action && MenuVariant == other.MenuVariant && ContentChapterId == other.ContentChapterId;
    }

    // 只保存已复制的房间值；无厨房时 WorldId 为空，不制造临时世界。
    public readonly struct ProductRoomProjection
    {
        public readonly string MatchId, WorldId;
        public readonly long BindingGeneration, Version, WorldGeneration;
        public readonly int MemberId;
        public readonly ProductMenuVariant MenuVariant;
        public readonly bool CanControlRoom;
        public readonly string VerifiedProfileId, BaselineOwnerId, BaselineSlotId;
        public readonly int BaselineRevision;
        public readonly byte[] HostIdentity;
        public readonly bool HasProposal, AllReady;
        public readonly long ProposalVersion;
        public readonly ProductMenuVariant ProposalMenu;
        public readonly int ProposalChapter;
        public readonly int ProposalFirstLevel;
        public readonly System.Collections.Generic.IReadOnlyList<ProductRoomReadyMember> ReadyMembers;
        public bool HasRestaurant => !string.IsNullOrEmpty(WorldId);

        public ProductRoomProjection(ProductRoomSnapshot snapshot)
        {
            MatchId = snapshot.MatchId;
            MemberId = snapshot.MemberId;
            BindingGeneration = snapshot.BindingGeneration;
            Version = snapshot.RoomVersion;
            WorldId = snapshot.WorldId;
            WorldGeneration = snapshot.WorldGeneration;
            MenuVariant = (ProductMenuVariant)snapshot.MenuVariant;
            CanControlRoom = snapshot.CanControlRoom;
            VerifiedProfileId = snapshot.VerifiedProfileId;
            BaselineOwnerId = snapshot.BaselineOwnerId;
            BaselineSlotId = snapshot.BaselineSlotId;
            BaselineRevision = snapshot.BaselineRevision;
            HostIdentity = snapshot.HostIdentity == null ? null : (byte[])snapshot.HostIdentity.Clone();
            HasProposal = snapshot.HasProposal;
            AllReady = snapshot.AllReady;
            ProposalVersion = snapshot.ProposalVersion;
            ProposalMenu = (ProductMenuVariant)snapshot.ProposalMenu;
            ProposalChapter = snapshot.ProposalChapter;
            ProposalFirstLevel = snapshot.ProposalFirstLevel;
            var members = new ProductRoomReadyMember[snapshot.ReadyMembers.Count];
            for (int index = 0; index < members.Length; ++index)
            {
                var member = snapshot.ReadyMembers[index];
                members[index] = new ProductRoomReadyMember(member.MemberId, member.ReadyBinding, member.Online, member.Ready);
            }
            ReadyMembers = Array.AsReadOnly(members);
        }
    }

    [ComponentOf(typeof(ClientSessionComponent))]
    public sealed class ProductRoomClient : Entity, IAwake, IDestroy
    {
        public ProductRoomProjection Current;
        public ProductRoomProjection Candidate;
    }

    public struct ProductRoomApplied { public bool RestaurantRetired; }
}
#endif
