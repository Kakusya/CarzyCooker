#if !DOTNET
using Game;

namespace ET
{
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(ClientSessionComponent))]
    [FriendOf(typeof(LocalMessageComponent))]
    [FriendOf(typeof(Participant))]
    public static class ProductRestaurantPermissions
    {
        // Permission comes from the live local binding, never a wire role/member field.
        public static bool CanControlRestaurant(this HostPeer peer)
        {
            if (peer == null || !peer.EntryOpen || peer.Closing || peer.MemberId <= 0) return false;
            HostSessionComponent authority = peer.Authority; ProductRuntimeComponent runtime = authority?.Runtime ?? default;
            Match match = authority?.Match ?? default;
            var member = match?.GetChild<Participant>(peer.MemberId);
            if (runtime == null || member == null || !member.Online || member.BindingGeneration != peer.Generation || (HostPeer)member.BoundPeer != peer) return false;
            if (runtime.Config.Role == RuntimeInstanceRole.Solo) return true; // Original trusted in-memory Solo fixture contract.
            if (runtime.Config.Role != RuntimeInstanceRole.Host) return false;
            ClientSessionComponent localClient = runtime.Client; LocalMessageComponent local = localClient?.Local ?? default;
            // The first snapshot precedes the local Join reply. Authority qualification is
            // already proved by the online Participant/Peer binding above; UI readiness
            // separately requires the confirmed Join identity and current actor.
            return localClient != null && local != null &&
                (ClientSessionComponent)local.Client == localClient && (HostPeer)local.Peer == peer;
        }
    }
}
#endif
