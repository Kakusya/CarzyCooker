#if !DOTNET
using System.Linq;
using Cysharp.Threading.Tasks;

namespace ET
{
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class GridPeerHandler : ProductActorRequestHandler<HostPeer, GridCommandRequest, GridCommandResponse>
    {
        protected override async UniTask Run(HostPeer peer, GridCommandRequest request, GridCommandResponse response)
        {
            response.CommandId = request.CommandId;
            if (!peer.EntryOpen) { response.Error = ProductError.PeerClosed; response.Status = (int)GridResultStatus.Rejected; return; }
            var inner = ProductMessages.Create<GridSubmitRequest>(message =>
            {
                message.PeerId = peer.Id; message.PeerInstanceId = peer.InstanceId; message.PeerGeneration = peer.Generation;
                message.ProtocolVersion = request.ProtocolVersion; message.MatchId = request.MatchId; message.WorldId = request.WorldId;
                message.WorldGeneration = request.WorldGeneration; message.BindingGeneration = request.BindingGeneration;
                message.CommandId = request.CommandId; message.Action = request.Action; message.X = request.X; message.Y = request.Y;
                message.TargetId = request.TargetId; message.ExpectedItemId = request.ExpectedItemId;
            });
            HostSessionComponent authority = peer.Authority;
            using var result = (GridSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(), inner, false);
            response.Error = result.Error; response.Message = result.Message; response.Status = result.Status;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(GridKitchenComponent))]
    public sealed class GridSubmitHandler : ProductActorRequestHandler<HostSessionComponent, GridSubmitRequest, GridSubmitResponse>
    {
        protected override UniTask Run(HostSessionComponent authority, GridSubmitRequest request, GridSubmitResponse response)
        {
            HostPeer peer = authority.GetChild<HostPeer>(request.PeerId);
            Match match = authority.Match;
            GridKitchenComponent grid = match.GetComponent<GridKitchenComponent>();
            string reason = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.PeerGeneration ||
                  peer.Generation != request.BindingGeneration || !peer.EntryOpen || peer.Closing ? "EntryClosed"
                : match.MatchId != request.MatchId ? "WrongMatch"
                : grid.WorldId != request.WorldId || grid.WorldGeneration != request.WorldGeneration ? "WrongWorld"
                : grid.SubmitGridIntent(peer.MemberId, peer.Generation, new GridIntent(request.CommandId, (GridAction)request.Action,
                    new GridCell(request.X, request.Y), request.TargetId, request.ExpectedItemId));
            response.CommandId = request.CommandId;
            response.Error = reason == null ? 0 : ProductError.InvalidRequest;
            response.Message = reason; response.Status = (int)(reason == null ? GridResultStatus.Accepted : GridResultStatus.Rejected);
            return UniTask.CompletedTask;
        }
    }

    [FriendOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(GridActor))]
    [FriendOf(typeof(GridStation))]
    [FriendOf(typeof(GridItem))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(HostPeer))]
    public static class GridBroadcast
    {
        public static GridWorldSnapshot CreateSnapshot(GridKitchenComponent world, HostPeer peer)
        {
            var snapshot = ProductMessages.Create<GridWorldSnapshot>(value =>
            {
                value.MatchId = world.GetParent<Match>().MatchId; value.WorldId = world.WorldId; value.WorldGeneration = world.WorldGeneration;
                value.MemberId = peer.MemberId; value.BindingGeneration = peer.Generation; value.Version = world.Version; value.Tick = world.Tick;
                value.Width = GridKitchenRules.Width; value.Height = GridKitchenRules.Height; value.KitchenBottom = GridKitchenRules.KitchenBottom;
                value.TickMilliseconds = GridKitchenRules.TickMilliseconds; value.StepTicks = GridKitchenRules.StepTicks;
            });
            foreach (GridActor actor in world.Actors.OrderBy(pair => pair.Key).Select(pair => (GridActor)pair.Value))
            {
                var value = ProductMessages.Create<GridActorState>(state =>
                {
                    state.MemberId = actor.MemberId; state.Generation = actor.Generation; state.X = actor.Cell.X; state.Y = actor.Cell.Y;
                    state.NextX = actor.NextCell.X; state.NextY = actor.NextCell.Y; state.StepStartTick = actor.StepStartTick;
                    state.Motion = (int)actor.Motion; state.Facing = actor.Facing; state.HandItemId = actor.HandItemId;
                    state.GoalX = actor.Goal.X; state.GoalY = actor.Goal.Y; state.LastCommandId = actor.LastResultCommandId;
                    state.LastStatus = (int)actor.LastResultStatus; state.LastReason = actor.LastResultReason;
                });
                foreach (GridCell cell in actor.Path.Skip(actor.PathIndex)) value.Path.Add(ProductMessages.Create<GridPathCell>(state => { state.X = cell.X; state.Y = cell.Y; }));
                snapshot.Actors.Add(value);
            }
            foreach (GridStation station in world.Stations.Values.Select(value => (GridStation)value))
                snapshot.Stations.Add(ProductMessages.Create<GridStationState>(value => { value.StationId = station.Id; value.Name = station.Name; value.X = station.Cell.X; value.Y = station.Cell.Y; value.ItemId = station.ItemId; }));
            foreach (GridItem item in world.Items.Values.Select(value => (GridItem)value))
            {
                var state = ProductMessages.Create<GridItemState>(value => { value.ItemId = item.Id; value.Name = item.Name; value.Location = (int)item.Location; value.HolderId = item.HolderId; value.X = item.Cell.X; value.Y = item.Cell.Y; });
                state.Contents.AddRange(item.Contents); snapshot.Items.Add(state);
            }
            return snapshot;
        }

        public static void SendCommitted(this GridKitchenComponent world)
        {
            HostSessionComponent authority = ((Scene)world.IScene).GetComponent<HostSessionComponent>();
            foreach (HostPeer peer in authority.Children.Values.Cast<HostPeer>().ToArray())
            {
                if (!peer.EntryOpen || peer.Closing || peer.MemberId == 0) continue;
                ProductDelivery.Send(peer, CreateSnapshot(world, peer));
                foreach (GridResultValue result in world.PendingResults.Where(value => value.MemberId == peer.MemberId && value.Generation == peer.Generation))
                    ProductDelivery.Send(peer, ProductMessages.Create<GridCommandResult>(value =>
                    {
                        value.MatchId = world.GetParent<Match>().MatchId; value.WorldId = world.WorldId; value.WorldGeneration = world.WorldGeneration;
                        value.BindingGeneration = peer.Generation; value.CommandId = result.CommandId; value.Status = (int)result.Status;
                        value.Reason = result.Reason; value.Version = world.Version;
                    }));
            }
            world.PendingResults.Clear();
        }
    }
}
#endif
