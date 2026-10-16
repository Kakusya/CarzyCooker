#if !DOTNET
using System;
using Game;

namespace ET
{
    [EntitySystemOf(typeof(Match))]
    [FriendOf(typeof(Match))]
    public static partial class MatchSystem
    {
        [EntitySystem]
        private static void Awake(this Match self)
        {
            self.MatchId = Guid.NewGuid().ToString("N");
            self.RosterVersion = 0;
            self.NextMemberId = 0;
            self.Restaurant = default;
            self.RoomVersion = 1;
            self.RoomMenuVariant = ProductMenuVariant.Basic;
        }
    }

    [EntitySystemOf(typeof(Participant))]
    [FriendOf(typeof(Participant))]
    public static partial class ParticipantSystem
    {
        [EntitySystem]
        private static void Awake(this Participant self)
        {
            self.MemberId = 0;
            self.Online = false;
            self.BoundPeer = default;
            self.BindingGeneration = 0;
            self.ResumeCredential = null;
            self.LastResumeAttempt = 0;
            self.ProfileId = self.ProfileKeyId = null;
        }
    }

    [EntitySystemOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(Match))]
    public static partial class HostSessionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HostSessionComponent self, Match match, ProductRuntimeComponent runtime)
        {
            self.Match = match;
            self.Runtime = runtime;
            self.NextBindingGeneration = 0;
        }

        [EntitySystem]
        private static void Update(this HostSessionComponent self)
        {
            Match match = self.Match;
            if (match != null && (RestaurantRuntime)match.Restaurant == null) match.ProductKitchen()?.AdvanceLegacyGridClock();
        }
    }

    [EntitySystemOf(typeof(HostPeer))]
    [FriendOf(typeof(HostPeer))]
    public static partial class HostPeerSystem
    {
        [EntitySystem]
        private static void Awake(this HostPeer self, HostSessionComponent authority, long generation)
        {
            self.Authority = authority;
            self.Generation = generation;
            self.MemberId = 0;
            self.EntryOpen = true;
            self.Closing = false;
            self.CloseFinished = false;
            self.InFlight = 0;
            self.NextOutcomeSerial = 0; self.PendingOutcomes.Clear();
            self.LastCommandContext = default;
            self.LastCommandKind = default;
            self.LastGridCommand = default;
            self.LastLevelCommand = default;
            self.LastLayoutCommand = default;
            self.LastFrontWorkCommand = default;
            self.LastServeCommand = default;
            self.LastBuffCommand = default;
            self.LastSupplyCommand = default;
            self.LastRenovationCommand = default;
            self.LastRoomCommand = default;
            self.ProfileChallenge = null;
            self.ProfileProved = false;
            self.LastBaselineSignature = null;
            self.LastBaselineRoomVersion = 0;
            self.LastCommandStatus = default;
            self.LastCommandReason = null;
        }
    }

    [EntitySystemOf(typeof(LocalDelivery))]
    [FriendOf(typeof(LocalDelivery))]
    public static partial class LocalDeliverySystem
    {
        [EntitySystem]
        private static void Awake(this LocalDelivery self, ClientSessionComponent client)
        {
            self.Client = client;
        }
    }

    [EntitySystemOf(typeof(RemoteDelivery))]
    [FriendOf(typeof(RemoteDelivery))]
    public static partial class RemoteDeliverySystem
    {
        [EntitySystem]
        private static void Awake(this RemoteDelivery self, Session session)
        {
            self.Session = session;
        }
    }

    [EntitySystemOf(typeof(LocalMessageComponent))]
    [FriendOf(typeof(LocalMessageComponent))]
    public static partial class LocalMessageComponentSystem
    {
        [EntitySystem]
        private static void Awake(this LocalMessageComponent self, ClientSessionComponent client, HostPeer peer)
        {
            self.Client = client;
            self.Peer = peer;
        }
    }

    [FriendOf(typeof(HostPeer))]
    [EntitySystemOf(typeof(ProductPeerBinding))]
    [FriendOf(typeof(ProductPeerBinding))]
    public static partial class ProductPeerBindingSystem
    {
        [EntitySystem]
        private static void Awake(this ProductPeerBinding self, HostPeer peer)
        {
            self.Peer = peer;
            self.Generation = peer.Generation;
        }

        [EntitySystem]
        private static void Destroy(this ProductPeerBinding self)
        {
            HostPeer peer = self.Peer;
            self.Peer = default;
            if (peer != null && peer.Generation == self.Generation)
            {
                peer.CloseEntry();
            }
        }
    }

    [FriendOf(typeof(ClientSessionComponent))]
    [EntitySystemOf(typeof(ProductClientBinding))]
    [FriendOf(typeof(ProductClientBinding))]
    public static partial class ProductClientBindingSystem
    {
        [EntitySystem]
        private static void Awake(this ProductClientBinding self, ClientSessionComponent client)
        {
            self.Client = client;
        }

        [EntitySystem]
        private static void Destroy(this ProductClientBinding self)
        {
            ClientSessionComponent client = self.Client;
            self.Client = default;
            if (client != null && client.Stage != ProductClientStage.Stopping && client.Stage != ProductClientStage.Stopped)
            {
                client.FailClient("ProductConnectionClosed");
            }
        }
    }
}
#endif
