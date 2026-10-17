#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(CooperativeOrderIntents))]
    [FriendOf(typeof(CooperativeOrderIntents))]
    [FriendOf(typeof(Participant))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(Order))]
    [FriendOf(typeof(Level))]
    public static partial class CooperativeOrderIntentStateSystem
    {
        [EntitySystem]
        private static void Awake(this CooperativeOrderIntents self) => self.Members.Clear();

        public static bool PruneOrderIntents(this Level level, Match match, long tick)
        {
            var state = level.GetComponent<CooperativeOrderIntents>();
            List<int> removed = null;
            foreach (var pair in state.Members)
            {
                Order order = level.GetChild<Order>(pair.Value.OrderId);
                Participant member = match.GetChild<Participant>(pair.Key);
                HostPeer peer = member?.BoundPeer;
                if (order == null || order.State != OrderState.Waiting || order.CustomerId != pair.Value.CustomerId ||
                    order.LevelGeneration != level.Generation || tick >= order.DeadlineTick || member?.Online != true ||
                    peer == null || !peer.EntryOpen || peer.Closing || member.BindingGeneration != pair.Value.BindingGeneration)
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
