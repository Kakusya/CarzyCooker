#if !DOTNET
using System;
using System.Text;
using Cysharp.Threading.Tasks;

namespace ET
{
    [Invoke((long)SceneType.ProductHost)]
    [FriendOf(typeof(ProductPeerBinding))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(Match))]
    public sealed class ProductHostOnRead : AInvokeHandler<NetComponentOnRead>
    {
        public override void Handle(NetComponentOnRead args)
        {
            Session session = args.Session;
            if (args.Message is not ProductJoinRequest && args.Message is not ProductProbeRequest && args.Message is not ProductLeaveRequest && args.Message is not ProductHeartbeatRequest && args.Message is not GridCommandRequest)
            {
                (args.Message as MessageObject)?.Dispose();
                session.Error = ProductError.InvalidRequest;
                session.Dispose();
                return;
            }
            ProductPeerBinding binding = session.GetComponent<ProductPeerBinding>();
            if (binding == null && args.Message is ProductJoinRequest)
            {
                HostSessionComponent authority = ((Scene)session.IScene).GetComponent<HostSessionComponent>();
                HostPeer peer = ProductAuthority.CreatePeer(authority);
                peer.AddComponent<RemoteDelivery, Session>(session);
                binding = session.AddComponent<ProductPeerBinding, HostPeer>(peer);
            }
            HandleRequest(session, binding, (MessageObject)args.Message).Forget();
        }

        private static async UniTaskVoid HandleRequest(Session session, ProductPeerBinding binding, MessageObject requestObject)
        {
            IRequest wire = (IRequest)requestObject;
            int wireRpcId = wire.RpcId;
            EntityRef<Session> sessionRef = session;
            HostPeer peer = binding?.Peer ?? default;
            EntityRef<HostPeer> peerRef = peer;
            long generation = binding?.Generation ?? 0;
            bool entered = peer != null && peer.Generation == generation && peer.BeginEntry();
            MessageObject responseObject = null;
            try
            {
                if (!entered || wireRpcId <= 0)
                {
                    responseObject = (MessageObject)MessageHelper.CreateResponse(wire.GetType(), wireRpcId, ProductError.PeerClosed);
                }
                else if (wire is ProductProbeRequest invalidProbe &&
                    (string.IsNullOrEmpty(invalidProbe.MatchId) || invalidProbe.Sequence <= 0 || invalidProbe.Payload == null ||
                    Encoding.UTF8.GetByteCount(invalidProbe.Payload) > ProductSessionRules.MaxPayloadBytes))
                {
                    responseObject = (MessageObject)MessageHelper.CreateResponse(wire.GetType(), wireRpcId, ProductError.InvalidRequest);
                }
                else if (wire is ProductLeaveRequest invalidLeave && string.IsNullOrEmpty(invalidLeave.MatchId))
                {
                    responseObject = (MessageObject)MessageHelper.CreateResponse(wire.GetType(), wireRpcId, ProductError.InvalidRequest);
                }
                else if (wire is ProductHeartbeatRequest heartbeat)
                {
                    HostSessionComponent authority = peer.Authority;
                    Match match = authority.Match;
                    int error = heartbeat.ProtocolVersion != ProductSessionRules.ProtocolVersion ? ProductError.IncompatibleProtocol
                        : peer.MemberId == 0 ? ProductError.NotJoined
                        : heartbeat.MatchId != match.MatchId ? ProductError.WrongMatch : 0;
                    responseObject = ProductMessages.Create<ProductHeartbeatResponse>(message => { message.Error = error; });
                }
                else
                {
                    // 独立对象进入本地 Call；其 RpcId 改写不会覆盖 wire 的原编号。
                    IRequest inner = wire switch
                    {
                        ProductJoinRequest join => ProductMessages.Create<ProductJoinRequest>(message => { message.ProtocolVersion = join.ProtocolVersion; }),
                        ProductProbeRequest probe => ProductMessages.Create<ProductProbeRequest>(message => { message.MatchId = probe.MatchId; message.Sequence = probe.Sequence; message.Payload = probe.Payload; }),
                        ProductLeaveRequest leave => ProductMessages.Create<ProductLeaveRequest>(message => { message.MatchId = leave.MatchId; }),
                        GridCommandRequest grid => ProductMessages.Create<GridCommandRequest>(message =>
                        {
                            message.ProtocolVersion = grid.ProtocolVersion; message.MatchId = grid.MatchId; message.WorldId = grid.WorldId;
                            message.WorldGeneration = grid.WorldGeneration; message.BindingGeneration = grid.BindingGeneration;
                            message.CommandId = grid.CommandId; message.Action = grid.Action; message.X = grid.X; message.Y = grid.Y;
                            message.TargetId = grid.TargetId; message.ExpectedItemId = grid.ExpectedItemId;
                        }),
                        _ => throw new InvalidOperationException("Product wire whitelist mismatch")
                    };
                    using var result = (MessageObject)await peer.Root().GetComponent<ProcessInnerSender>().Call(peer.GetActorId(), inner, false);
                    responseObject = CopyResponse(result);
                }
                session = sessionRef;
                peer = peerRef;
                if (session == null || (entered && (peer == null || peer.Generation != generation)))
                {
                    return;
                }
                IResponse response = (IResponse)responseObject;
                response.RpcId = wireRpcId;
                bool committedLeave = response is ProductLeaveResponse { Error: 0, Committed: true };
                if (entered && !peer.EntryOpen && !committedLeave)
                {
                    return;
                }
                if (response is ProductJoinResponse { Error: 0 })
                {
                    session.RemoveComponent<SessionAcceptTimeoutComponent>();
                }
                session.Send((IMessage)responseObject);
                if (committedLeave || response is ProductJoinResponse { Error: not 0 })
                {
                    // KCP Send 入队后由后续 Update 发出，不能立即 Dispose 丢弃该响应。
                    await session.Root().GetComponent<TimerComponent>().WaitAsync(ProductSessionRules.ResponseCloseGrace);
                    session = sessionRef;
                    session?.Dispose();
                }
            }
            catch (Exception e)
            {
                session = sessionRef;
                if (session != null)
                {
                    Log.Warning($"Product wire request failed: {e.Message}");
                    session.Error = ProductError.PeerClosed;
                    session.Dispose();
                }
            }
            finally
            {
                requestObject.Dispose();
                responseObject?.Dispose();
                peer = peerRef;
                if (entered && peer != null)
                {
                    peer.EndEntry();
                }
            }
        }

        private static MessageObject CopyResponse(MessageObject result)
        {
            switch (result)
            {
                case ProductJoinResponse join:
                    var response = ProductMessages.Create<ProductJoinResponse>(message => { message.Error = join.Error; message.Message = join.Message; message.MatchId = join.MatchId; message.MemberId = join.MemberId; message.BindingGeneration = join.BindingGeneration; message.RosterVersion = join.RosterVersion; });
                    foreach (ProductRosterMember member in join.Members)
                    {
                        response.Members.Add(ProductMessages.Create<ProductRosterMember>(message => { message.MemberId = member.MemberId; message.Online = member.Online; }));
                    }
                    return response;
                case ProductProbeResponse probe:
                    return ProductMessages.Create<ProductProbeResponse>(message => { message.Error = probe.Error; message.Message = probe.Message; message.MemberId = probe.MemberId; message.Sequence = probe.Sequence; message.Payload = probe.Payload; });
                case ProductLeaveResponse leave:
                    return ProductMessages.Create<ProductLeaveResponse>(message => { message.Error = leave.Error; message.Message = leave.Message; message.Committed = leave.Committed; });
                case GridCommandResponse grid:
                    return ProductMessages.Create<GridCommandResponse>(message => { message.Error = grid.Error; message.Message = grid.Message; message.CommandId = grid.CommandId; message.Status = grid.Status; });
                default:
                    throw new InvalidOperationException("Unexpected product actor response");
            }
        }
    }

    [Invoke((long)SceneType.ProductClient)]
    [FriendOf(typeof(ProductClientBinding))]
    [FriendOf(typeof(Session))]
    public sealed class ProductClientOnRead : AInvokeHandler<NetComponentOnRead>
    {
        public override void Handle(NetComponentOnRead args)
        {
            Session session = args.Session;
            ClientSessionComponent client = session.GetComponent<ProductClientBinding>()?.Client ?? default;
            MessageObject message = args.Message as MessageObject;
            if (client == null || message == null)
            {
                message?.Dispose();
                return;
            }
            if (message is ProductJoinResponse or ProductProbeResponse or ProductLeaveResponse or ProductHeartbeatResponse or GridCommandResponse)
            {
                IResponse response = (IResponse)message;
                if (!session.requestCallbacks.TryGetValue(response.RpcId, out RpcInfo pending))
                {
                    message.Dispose();
                    return;
                }
                if (OpcodeType.Instance.GetResponseType(pending.RequestType) != message.GetType())
                {
                    message.Dispose();
                    session.Error = ProductError.InvalidRequest;
                    session.Dispose();
                    return;
                }
                session.OnResponse(response);
                return; // 有效响应交调用者消费。
            }
            if (message is ProductRosterSnapshot or ProductMatchClosed or GridWorldSnapshot or GridCommandResult)
            {
                session.Root().GetComponent<ProcessInnerSender>().Send(client.GetActorId(), (IMessage)message);
                return;
            }
            message.Dispose();
            session.Error = ProductError.InvalidRequest;
            session.Dispose();
        }
    }
}
#endif
