#if !DOTNET
using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Cysharp.Threading.Tasks;

namespace ET
{
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(Participant))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(LocalDelivery))]
    [FriendOf(typeof(RemoteDelivery))]
    [FriendOf(typeof(ProductPeerBinding))]
    public static class ProductAuthority
    {
        public static HostPeer CreatePeer(HostSessionComponent authority)
        {
            if (authority.NextBindingGeneration == long.MaxValue)
            {
                throw new InvalidOperationException("Product binding generation exhausted");
            }
            HostPeer peer = authority.AddChild<HostPeer, HostSessionComponent, long>(authority, ++authority.NextBindingGeneration);
            peer.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
            return peer;
        }

        public static void Commit(HostSessionComponent authority, ProductControlRequest request, ProductControlResponse response)
        {
            ProductRuntimeComponent runtime = authority.Runtime;
            Match match = authority.Match;
            HostPeer peer = authority.GetChild<HostPeer>(request.PeerId);
            if (runtime == null || match == null || runtime.Stage is ProductStage.Stopping or ProductStage.Stopped or ProductStage.Failed)
            {
                response.Error = ProductError.RuntimeStopped;
                return;
            }
            if (peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.BindingGeneration)
            {
                response.Error = ProductError.PeerClosed;
                return;
            }
            ProductControlAction action = (ProductControlAction)request.Action;
            if (action == ProductControlAction.Close)
            {
                Participant member = match.GetChild<Participant>(peer.MemberId);
                if (member != null && member.BindingGeneration == peer.Generation && (HostPeer)member.BoundPeer == peer && member.Online)
                {
                    member.Online = false;
                    member.BoundPeer = default;
                    match.ProductKitchen()?.QueueGridMember(member.MemberId, member.BindingGeneration, false);
                    Changed(authority, match);
                }
                peer.CloseFinished = true;
                return;
            }
            // 第二次核对发生在共同有序 Mailbox 内，不能仅信 Peer 接收时的资格。
            if (!peer.EntryOpen || peer.Closing)
            {
                response.Error = ProductError.PeerClosed;
                return;
            }
            switch (action)
            {
                case ProductControlAction.Join:
                    if (request.ProtocolVersion != ProductSessionRules.ProtocolVersion)
                    {
                        response.Error = ProductError.IncompatibleProtocol;
                        return;
                    }
                    if (peer.MemberId == 0)
                    {
                        if (match.NextMemberId == int.MaxValue)
                        {
                            response.Error = ProductError.InvalidRequest;
                            return;
                        }
                        int memberId = ++match.NextMemberId;
                        Participant member = match.AddChildWithId<Participant>(memberId);
                        member.MemberId = memberId;
                        byte[] credential = new byte[32];
                        using (RandomNumberGenerator random = RandomNumberGenerator.Create()) { random.GetBytes(credential); }
                        member.ResumeCredential = Convert.ToBase64String(credential);
                        member.Online = true;
                        member.BoundPeer = peer;
                        member.BindingGeneration = peer.Generation;
                        peer.MemberId = memberId;
                        match.ProductKitchen()?.QueueGridMember(memberId, peer.Generation, true);
                        Changed(authority, match);
                    }
                    FillJoin(match, peer, response);
                    return;
                case ProductControlAction.Resume:
                    if (request.ProtocolVersion != ProductSessionRules.ProtocolVersion)
                    {
                        response.Error = ProductError.IncompatibleProtocol;
                        return;
                    }
                    if (request.PlatformId != ProductSessionRules.CurrentPlatformId)
                    {
                        response.Error = ProductError.InvalidRequest; response.Message = "PlatformMismatch";
                        return;
                    }
                    if (request.MatchId != match.MatchId)
                    {
                        response.Error = ProductError.WrongMatch;
                        return;
                    }
                    Participant returning = match.Children.Values.OfType<Participant>().FirstOrDefault(member =>
                        member.ResumeCredential != null && member.ResumeCredential == request.ResumeCredential);
                    if (returning == null || request.ResumeAttempt <= 0 || peer.GetComponent<RemoteDelivery>() == null)
                    {
                        response.Error = ProductError.InvalidRequest;
                        response.Message = "InvalidResumeCredential";
                        return;
                    }
                    if (peer.MemberId != 0)
                    {
                        if (peer.MemberId != returning.MemberId || (HostPeer)returning.BoundPeer != peer ||
                            returning.LastResumeAttempt != request.ResumeAttempt)
                        {
                            response.Error = ProductError.InvalidRequest; response.Message = "ResumeAttemptConflict";
                            return;
                        }
                        FillJoin(match, peer, response); response.ResumeAttempt = request.ResumeAttempt;
                        return;
                    }
                    if (returning.LastResumeAttempt == request.ResumeAttempt)
                    {
                        response.Error = ProductError.InvalidRequest; response.Message = "ResumeAttemptConflict";
                        return;
                    }
                    if (authority.NextBindingGeneration == long.MaxValue)
                    {
                        response.Error = ProductError.InvalidRequest;
                        return;
                    }
                    // 接入 Peer 可能先创建、后恢复；新绑定代次只能在串行提交点分配。
                    peer.Generation = ++authority.NextBindingGeneration;
                    Session remoteSession = ((RemoteDelivery)peer.GetComponent<RemoteDelivery>()).Session;
                    remoteSession.GetComponent<ProductPeerBinding>().Generation = peer.Generation;
                    HostPeer previous = returning.BoundPeer;
                    match.ProductKitchen()?.QueueGridMember(returning.MemberId, returning.BindingGeneration, false);
                    previous?.CloseEntry();
                    returning.Online = true; returning.BoundPeer = peer; returning.BindingGeneration = peer.Generation;
                    returning.LastResumeAttempt = request.ResumeAttempt; peer.MemberId = returning.MemberId;
                    match.ProductKitchen()?.QueueGridMember(returning.MemberId, peer.Generation, true);
                    Changed(authority, match);
                    FillJoin(match, peer, response); response.ResumeAttempt = request.ResumeAttempt;
                    return;
                case ProductControlAction.Probe:
                case ProductControlAction.Leave:
                    Participant participant = match.GetChild<Participant>(peer.MemberId);
                    if (participant == null || !participant.Online || (HostPeer)participant.BoundPeer != peer || participant.BindingGeneration != peer.Generation)
                    {
                        response.Error = ProductError.NotJoined;
                        return;
                    }
                    if (string.IsNullOrEmpty(request.MatchId) || request.MatchId != match.MatchId)
                    {
                        response.Error = ProductError.WrongMatch;
                        return;
                    }
                    if (action == ProductControlAction.Probe)
                    {
                        if (request.Sequence <= 0 || request.Payload == null || Encoding.UTF8.GetByteCount(request.Payload) > ProductSessionRules.MaxPayloadBytes)
                        {
                            response.Error = ProductError.InvalidRequest;
                            return;
                        }
                        response.MemberId = participant.MemberId;
                        response.Sequence = request.Sequence;
                        response.Payload = request.Payload;
                        return;
                    }
                    match.ProductKitchen()?.QueueGridMember(participant.MemberId, participant.BindingGeneration, false);
                    participant.Dispose();
                    peer.EntryOpen = false;
                    peer.Closing = true;
                    peer.CloseFinished = true;
                    response.Committed = true;
                    Changed(authority, match);
                    return;
                default:
                    response.Error = ProductError.InvalidRequest;
                    return;
            }
        }

        private static void FillJoin(Match match, HostPeer peer, ProductControlResponse response)
        {
            response.MatchId = match.MatchId;
            response.MemberId = peer.MemberId;
            response.BindingGeneration = peer.Generation;
            response.RosterVersion = match.RosterVersion;
            response.ResumeCredential = match.GetChild<Participant>(peer.MemberId).ResumeCredential;
            foreach (Participant member in match.Children.Values.OfType<Participant>())
            {
                response.Members.Add(ProductMessages.Create<ProductRosterMember>(message => { message.MemberId = member.MemberId; message.Online = member.Online; }));
            }
        }

        private static void Changed(HostSessionComponent authority, Match match)
        {
            match.InvalidateRoomReadiness();
            ++match.RoomVersion;
            ++match.RosterVersion;
            foreach (HostPeer peer in authority.Children.Values.Cast<HostPeer>().ToArray())
            {
                if (!peer.EntryOpen || peer.MemberId == 0)
                {
                    continue;
                }
                var snapshot = ProductMessages.Create<ProductRosterSnapshot>(message => { message.MatchId = match.MatchId; message.BindingGeneration = peer.Generation; message.RosterVersion = match.RosterVersion; });
                foreach (Participant member in match.Children.Values.OfType<Participant>())
                {
                    snapshot.Members.Add(ProductMessages.Create<ProductRosterMember>(message => { message.MemberId = member.MemberId; message.Online = member.Online; }));
                }
                ProductDelivery.Send(peer, snapshot);
            }
            authority.BroadcastRoom();
            EventSystem.Instance.Publish((Scene)authority.IScene, new ProductMemberChanged { MatchId = match.MatchId, RosterVersion = match.RosterVersion });
        }


    }

    [MessageHandler(SceneType.ProductHost)]
    public sealed class ProductControlHandler : ProductActorRequestHandler<HostSessionComponent, ProductControlRequest, ProductControlResponse>
    {
        protected override UniTask Run(HostSessionComponent authority, ProductControlRequest request, ProductControlResponse response)
        {
            ProductAuthority.Commit(authority, request, response);
            return UniTask.CompletedTask;
        }
    }
}
#endif
