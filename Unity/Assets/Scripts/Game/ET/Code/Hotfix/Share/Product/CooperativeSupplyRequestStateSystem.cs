#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(CooperativeSupplyRequests))]
    [FriendOf(typeof(CooperativeSupplyRequests))]
    [FriendOf(typeof(Participant))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(GridKitchenComponent))]
    public static partial class CooperativeSupplyRequestStateSystem
    {
        [EntitySystem]
        private static void Awake(this CooperativeSupplyRequests self)
        {
            self.Members.Clear(); self.NextRevision = 0;
        }

        public static string CheckCooperativePurchase(this Level level, Match match, HostPeer processor, SupplyIntent intent, out SupplyRequestValue request)
        {
            request = default;
            if (intent.CooperativeRequestId == 0 && intent.CooperativeRevision == 0)
            {
                return null;
            }
            if (processor == null || !processor.EntryOpen || processor.Closing)
            {
                return "EntryClosed";
            }
            if (intent.Action != SupplyAction.Purchase || intent.Quantity != 1)
            {
                return "SupplyRequestChanged";
            }
            foreach (var value in level.GetComponent<CooperativeSupplyRequests>().Members.Values)
            {
                if (value.Id != intent.CooperativeRequestId)
                {
                    continue;
                }
                Participant publisher = match.GetChild<Participant>(value.MemberId);
                HostPeer peer = publisher?.BoundPeer;
                if (value.Revision != intent.CooperativeRevision || value.SourceId != intent.SourceId || publisher?.Online != true ||
                    publisher.BindingGeneration != value.BindingGeneration || peer == null || !peer.EntryOpen || peer.Closing)
                {
                    return "SupplyRequestChanged";
                }
                if (value.State != SupplyRequestState.Pending)
                {
                    return "SupplyRequestHandled";
                }
                request = value;
                return null;
            }
            return "SupplyRequestChanged";
        }

        public static void CompleteCooperativePurchase(this Level level, SupplyRequestValue request, long shipmentId)
        {
            if (request.Id == 0)
            {
                return;
            }
            level.GetComponent<CooperativeSupplyRequests>().Members[request.MemberId] = new SupplyRequestValue(request.Id,
                request.Revision, request.MemberId, request.BindingGeneration, request.SourceId, SupplyRequestState.Ordered, shipmentId);
        }

        public static bool PruneSupplyRequests(this GridKitchenComponent kitchen)
        {
            var state = kitchen.ProductLevel()?.GetComponent<CooperativeSupplyRequests>();
            if (state == null || state.Members.Count == 0)
            {
                return false;
            }
            Match match = kitchen.Match;
            List<int> removed = null;
            foreach (var request in state.Members.Values)
            {
                Participant member = match.GetChild<Participant>(request.MemberId);
                HostPeer peer = member?.BoundPeer;
                if (member?.Online != true || member.BindingGeneration != request.BindingGeneration || peer == null || !peer.EntryOpen || peer.Closing)
                {
                    (removed ??= new List<int>()).Add(request.MemberId);
                }
            }
            if (removed == null)
            {
                return false;
            }
            foreach (int member in removed)
            {
                state.Members.Remove(member);
            }
            return true;
        }
    }
}
#endif
