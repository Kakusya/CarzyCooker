#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(CooperativeTargetPings))]
    [FriendOf(typeof(CooperativeTargetPings))]
    [FriendOf(typeof(Participant))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(GridKitchenComponent))]
    public static partial class CooperativeTargetPingStateSystem
    {
        [EntitySystem]
        private static void Awake(this CooperativeTargetPings self)
        {
            self.Members.Clear(); self.NextSerial = 0;
        }

        public static bool PingTargetExists(this GridKitchenComponent kitchen, TargetPingKind kind, long targetId)
        {
            if (kind == TargetPingKind.Item)
            {
                return kitchen.Items.ContainsKey(targetId);
            }
            if (!kitchen.Stations.TryGetValue(targetId, out var reference))
            {
                return false;
            }
            GridStation station = reference;
            return station != null && (kind == TargetPingKind.Station || kind == TargetPingKind.Table && station.GetComponent<FrontTable>() != null);
        }

        public static bool PruneTargetPings(this GridKitchenComponent kitchen)
        {
            Level level = kitchen.ProductLevel();
            var state = level?.GetComponent<CooperativeTargetPings>();
            if (state == null || state.Members.Count == 0)
            {
                return false;
            }
            Match match = kitchen.Match;
            List<int> removed = null;
            foreach (var pair in state.Members)
            {
                Participant member = match.GetChild<Participant>(pair.Key);
                HostPeer peer = member?.BoundPeer;
                if (kitchen.Tick >= pair.Value.ExpiresTick || !kitchen.PingTargetExists(pair.Value.Kind, pair.Value.TargetId) ||
                    member?.Online != true || peer == null || !peer.EntryOpen || peer.Closing || member.BindingGeneration != pair.Value.BindingGeneration)
                {
                    (removed ??= new List<int>()).Add(pair.Key);
                }
            }
            if (removed == null)
            {
                return false;
            }
            foreach (int memberId in removed)
            {
                state.Members.Remove(memberId);
            }
            return true;
        }
    }
}
#endif
