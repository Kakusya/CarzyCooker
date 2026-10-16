#if !DOTNET
using System;

namespace ET
{
    public enum ProductRoomAction { StartRestaurant = 1, CloseRestaurant = 2 }

    public readonly struct ProductRoomIntent : IEquatable<ProductRoomIntent>
    {
        public readonly long Version;
        public readonly ProductRoomAction Action;
        public readonly ProductMenuVariant MenuVariant;
        public readonly int ContentChapterId;

        public ProductRoomIntent(long version, ProductRoomAction action, ProductMenuVariant menuVariant, int contentChapterId = 0)
        {
            Version = version;
            Action = action;
            MenuVariant = menuVariant;
            ContentChapterId = contentChapterId;
        }

        public bool Equals(ProductRoomIntent other) => Version == other.Version && Action == other.Action && MenuVariant == other.MenuVariant && ContentChapterId == other.ContentChapterId;
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
        }
    }

    [ComponentOf(typeof(ClientSessionComponent))]
    public sealed class ProductRoomClient : Entity, IAwake, IDestroy
    {
        public ProductRoomProjection Current;
        public ProductRoomProjection Candidate;
    }

    public struct ProductRoomApplied { }
}
#endif
