#if !DOTNET
using System;
using Cysharp.Threading.Tasks;

namespace ET
{
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(ProcessInnerSender))]
    public static partial class HostPeerSystem
    {
        public static bool BeginEntry(this HostPeer self)
        {
            if (!self.EntryOpen || self.Closing || self.IsDisposed)
            {
                return false;
            }
            ++self.InFlight;
            return true;
        }

        public static string CheckProductCommand(this HostPeer self, ProductCommandContext context,
            ProductCommandKind kind, bool samePayload, out bool replay)
        {
            replay = false;
            if (context.CommandId <= 0) return "InvalidCommand";
            long previous = self.LastCommandContext.CommandId;
            if (context.CommandId > previous) return null;
            if (context.CommandId < previous) return "ExpiredCommand";
            if (!context.Equals(self.LastCommandContext) || kind != self.LastCommandKind || !samePayload)
                return "CommandConflict";
            replay = true;
            return null;
        }

        public static void RecordProductCommand(this HostPeer self, ProductCommandContext context, ProductCommandKind kind)
        {
            self.LastCommandContext = context; self.LastCommandKind = kind;
            self.LastCommandStatus = GridResultStatus.Accepted; self.LastCommandReason = null;
        }

        public static void RecordProductResult(this HostPeer self, long commandId, GridResultStatus status, string reason)
        {
            if (self.LastCommandContext.CommandId != commandId) return;
            self.LastCommandStatus = status; self.LastCommandReason = reason;
        }

        public static void RecordProductResult(this HostPeer self, ProductCommandContext context, ProductCommandKind kind,
            GridResultStatus status, string reason, bool changedLevel = false)
        {
            self.RecordProductResult(context.CommandId, status, reason);
            if (kind != ProductCommandKind.Grid && status is GridResultStatus.Executed or GridResultStatus.Rejected or GridResultStatus.Cancelled)
            {
                self.PendingOutcomes.Add(new ProductCommandOutcomeValue(context, kind, status, reason, changedLevel));
            }
        }

        public static void EndEntry(this HostPeer self)
        {
            if (self.InFlight <= 0)
            {
                throw new InvalidOperationException("Product peer entry count underflow");
            }
            --self.InFlight;
            self.TryRelease();
        }

        private static void TryRelease(this HostPeer self)
        {
            if (self.Closing && self.CloseFinished && self.InFlight == 0)
            {
                self.Dispose();
            }
        }

        public static void CloseEntry(this HostPeer self)
        {
            if (self.IsDisposed || self.Closing)
            {
                return;
            }
            self.EntryOpen = false;
            self.Closing = true;
            HostSessionComponent authority = self.Authority;
            ProductRuntimeComponent runtime = authority?.Runtime ?? default;
            if (runtime == null || runtime.IsDisposed || runtime.Stage is ProductStage.Stopping or ProductStage.Stopped)
            {
                return; // 整体 Stop 持有树，不能在 Dispose 中重新提交。
            }
            CloseAsync(self).Forget();
        }

        private static async UniTaskVoid CloseAsync(HostPeer peer)
        {
            EntityRef<HostPeer> peerRef = peer;
            ++peer.InFlight;
            try
            {
                using ProductControlResponse response = await peer.Forward(ProductControlAction.Close);
                peer = peerRef;
                if (peer != null)
                {
                    peer.CloseFinished = response.Error == 0;
                }
            }
            catch (Exception e)
            {
                peer = peerRef;
                if (peer != null && !peer.Root().GetComponent<ProcessInnerSender>().IsStopped)
                {
                    Log.Warning($"Product peer close failed: {e.Message}");
                }
            }
            finally
            {
                peer = peerRef;
                if (peer != null)
                {
                    peer.EndEntry();
                }
            }
        }

        public static async UniTask<ProductControlResponse> Forward(this HostPeer peer, ProductControlAction action,
            int version = 0, string matchId = null, int sequence = 0, string payload = null)
        {
            HostSessionComponent authority = peer.Authority;
            var request = ProductMessages.Create<ProductControlRequest>(message => { message.Action = (int)action; message.PeerId = peer.Id; message.PeerInstanceId = peer.InstanceId; message.BindingGeneration = peer.Generation; message.ProtocolVersion = version; message.MatchId = matchId; message.Sequence = sequence; message.Payload = payload; });
            return (ProductControlResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(), request, false);
        }
    }

    [FriendOf(typeof(HostPeer))]
    [MessageHandler(SceneType.ProductHost)]
    public sealed class ProductPeerJoinHandler : ProductActorRequestHandler<HostPeer, ProductJoinRequest, ProductJoinResponse>
    {
        protected override async UniTask Run(HostPeer peer, ProductJoinRequest request, ProductJoinResponse response)
        {
            if (!peer.EntryOpen)
            {
                response.Error = ProductError.PeerClosed;
                return;
            }
            using ProductControlResponse result = await peer.Forward(ProductControlAction.Join, request.ProtocolVersion);
            response.Error = result.Error;
            response.Message = result.Message;
            response.MatchId = result.MatchId;
            response.MemberId = result.MemberId;
            response.BindingGeneration = result.BindingGeneration;
            response.RosterVersion = result.RosterVersion;
            foreach (ProductRosterMember member in result.Members)
            {
                response.Members.Add(ProductMessages.Create<ProductRosterMember>(message => { message.MemberId = member.MemberId; message.Online = member.Online; }));
            }
        }
    }

    [FriendOf(typeof(HostPeer))]
    [MessageHandler(SceneType.ProductHost)]
    public sealed class ProductPeerProbeHandler : ProductActorRequestHandler<HostPeer, ProductProbeRequest, ProductProbeResponse>
    {
        protected override async UniTask Run(HostPeer peer, ProductProbeRequest request, ProductProbeResponse response)
        {
            if (!peer.EntryOpen)
            {
                response.Error = ProductError.PeerClosed;
                return;
            }
            using ProductControlResponse result = await peer.Forward(ProductControlAction.Probe, matchId: request.MatchId, sequence: request.Sequence, payload: request.Payload);
            response.Error = result.Error;
            response.Message = result.Message;
            response.MemberId = result.MemberId;
            response.Sequence = result.Sequence;
            response.Payload = result.Payload;
        }
    }

    [FriendOf(typeof(HostPeer))]
    [MessageHandler(SceneType.ProductHost)]
    public sealed class ProductPeerLeaveHandler : ProductActorRequestHandler<HostPeer, ProductLeaveRequest, ProductLeaveResponse>
    {
        protected override async UniTask Run(HostPeer peer, ProductLeaveRequest request, ProductLeaveResponse response)
        {
            if (!peer.EntryOpen)
            {
                response.Error = ProductError.PeerClosed;
                return;
            }
            using ProductControlResponse result = await peer.Forward(ProductControlAction.Leave, matchId: request.MatchId);
            response.Error = result.Error;
            response.Message = result.Message;
            response.Committed = result.Committed;
        }
    }
}
#endif
