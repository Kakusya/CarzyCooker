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
                message.ExpectedTargetItemId = request.ExpectedTargetItemId; message.ExpectedProcessId = request.ExpectedProcessId;
                message.LevelId = request.LevelId; message.LevelGeneration = request.LevelGeneration;
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
    [FriendOf(typeof(Level))]
    public sealed class GridSubmitHandler : ProductActorRequestHandler<HostSessionComponent, GridSubmitRequest, GridSubmitResponse>
    {
        protected override UniTask Run(HostSessionComponent authority, GridSubmitRequest request, GridSubmitResponse response)
        {
            HostPeer peer = authority.GetChild<HostPeer>(request.PeerId);
            Match match = authority.Match;
            GridKitchenComponent grid = match.ProductKitchen();
            Level level = grid.ProductLevel();
            string reason = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.PeerGeneration ||
                  peer.Generation != request.BindingGeneration || !peer.EntryOpen || peer.Closing ? "EntryClosed"
                : new ProductCommandContext(request.MatchId, request.WorldId, request.WorldGeneration, request.LevelId,
                    request.LevelGeneration, request.BindingGeneration, request.CommandId)
                    .Validate(match.MatchId, grid.WorldId, grid.WorldGeneration, level?.Id ?? 0, level?.Generation ?? 0, peer.Generation)
                    ?? grid.SubmitGridIntent(peer.MemberId, peer.Generation, new GridIntent(request.CommandId, (GridAction)request.Action,
                        new GridCell(request.X, request.Y), request.TargetId, request.ExpectedItemId, request.ExpectedTargetItemId, request.ExpectedProcessId));
            response.CommandId = request.CommandId;
            response.Error = reason == null ? 0 : ProductError.InvalidRequest;
            response.Message = reason ?? peer.LastCommandReason;
            response.Status = (int)(reason == null ? peer.LastCommandStatus : GridResultStatus.Rejected);
            return UniTask.CompletedTask;
        }
    }

    [FriendOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(GridActor))]
    [FriendOf(typeof(GridStation))]
    [FriendOf(typeof(GridItem))]
    [FriendOf(typeof(GridManualProcess))]
    [FriendOf(typeof(GridAutoProcess))]
    [FriendOf(typeof(GridBakeProcess))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(Level))]
    public static partial class GridBroadcast
    {
        public static GridWorldSnapshot CreateSnapshot(GridKitchenComponent world, HostPeer peer)
        {
            var snapshot = ProductMessages.Create<GridWorldSnapshot>(value =>
            {
                value.MatchId = ((Match)world.Match).MatchId; value.WorldId = world.WorldId; value.WorldGeneration = world.WorldGeneration;
                value.MemberId = peer.MemberId; value.BindingGeneration = peer.Generation; value.Version = world.Version; value.Tick = world.Tick;
                value.Width = GridKitchenRules.Width; value.Height = GridKitchenRules.Height; value.KitchenBottom = GridKitchenRules.KitchenBottom;
                value.TickMilliseconds = GridKitchenRules.TickMilliseconds; value.StepTicks = GridKitchenRules.StepTicks;
                Level level = world.ProductLevel();
                value.LevelId = level?.Id ?? 0; value.LevelGeneration = level?.Generation ?? 0;
                value.LevelPhase = (int)(level?.Phase ?? default); value.LevelPaused = level?.Paused ?? false;
                value.LevelOperatingTicks = level?.OperatingTicks ?? 0; value.LevelElapsedTicks = level?.ElapsedOperatingTicks ?? 0;
                value.LevelScore = level?.Score ?? 0; value.LevelStars = level?.Stars ?? 0;
                value.LevelConfigId = level?.ConfigId ?? 0; value.NextLevelConfigId = level?.NextLevelConfigId ?? 0;
                value.LastCommandId = peer.LastCommandContext.CommandId; value.LastCommandKind = (int)peer.LastCommandKind;
                value.LastCommandStatus = (int)peer.LastCommandStatus; value.LastCommandReason = peer.LastCommandReason;
                value.LayoutVersion = world.LayoutVersion;
                if (level != null) value.AllowedDishIds.AddRange(level.AllowedDishIds);
            });
            foreach (GridActor actor in world.Actors.OrderBy(pair => pair.Key).Select(pair => (GridActor)pair.Value))
            {
                var value = ProductMessages.Create<GridActorState>(state =>
                {
                    state.MemberId = actor.MemberId; state.Generation = actor.Generation; state.X = actor.Cell.X; state.Y = actor.Cell.Y;
                    state.NextX = actor.NextCell.X; state.NextY = actor.NextCell.Y; state.StepStartTick = actor.StepStartTick;
                    state.Motion = (int)actor.Motion; state.Facing = actor.Facing; state.HandItemId = actor.HandItemId;
                    state.GoalX = actor.Goal.X; state.GoalY = actor.Goal.Y; state.LastCommandId = actor.LastResultCommandId;
                    state.LastStatus = (int)actor.LastResultStatus; state.LastReason = actor.LastResultReason; state.WorkId = actor.WorkId;
                    state.FrontWorkId = actor.FrontWorkId; state.FrontClaimSerial = actor.FrontClaimSerial;
                });
                foreach (GridCell cell in actor.Path.Skip(actor.PathIndex)) value.Path.Add(ProductMessages.Create<GridPathCell>(state => { state.X = cell.X; state.Y = cell.Y; }));
                snapshot.Actors.Add(value);
            }
            foreach (GridStation station in world.Stations.Values.Select(value => (GridStation)value))
            {
                var state = ProductMessages.Create<GridStationState>(value =>
                {
                    value.StationId = station.Id; value.Name = station.Name; value.X = station.Cell.X; value.Y = station.Cell.Y;
                    value.ItemId = station.ItemId; value.Kind = (int)station.Kind; value.AutoProcessId = station.AutoProcessId;
                    value.DefinitionId = station.DefinitionId; value.Orientation = station.Orientation;
                    value.Capabilities = (int)station.Capabilities; value.ItemSlotCapacity = station.ItemSlotCapacity;
                    value.BakeProcessId = station.BakeProcessId;
                });
                foreach (GridCell cell in station.StationFootprint()) state.Footprint.Add(ProductMessages.Create<GridPathCell>(value => { value.X = cell.X; value.Y = cell.Y; }));
                foreach (GridCell cell in station.StationApproaches()) state.Approaches.Add(ProductMessages.Create<GridPathCell>(value => { value.X = cell.X; value.Y = cell.Y; }));
                snapshot.Stations.Add(state);
            }
            foreach (GridItem item in world.Items.Values.Select(value => (GridItem)value))
            {
                var state = ProductMessages.Create<GridItemState>(value => { value.ItemId = item.Id; value.Name = item.Name; value.DefinitionId = item.DefinitionId; value.Location = (int)item.Location; value.HolderId = item.HolderId; value.X = item.Cell.X; value.Y = item.Cell.Y; value.AutoProcessId = item.AutoProcessId; value.BakeProcessId = item.BakeProcessId; });
                state.Contents.AddRange(item.Contents); snapshot.Items.Add(state);
            }
            foreach (GridManualProcess p in world.Processes.Values.Select(value => (GridManualProcess)value))
                snapshot.Processes.Add(ProductMessages.Create<GridManualProcessState>(value =>
                {
                    value.ProcessId = p.Id; value.RecipeId = p.RecipeId; value.InputItemId = p.InputItemId; value.TargetItemId = p.TargetItemId;
                    value.StationId = p.StationId; value.RequiredTicks = p.RequiredTicks; value.ProgressTicks = p.ProgressTicks;
                    value.State = (int)p.State; value.WorkerMemberId = p.WorkerMemberId; value.WorkerGeneration = p.WorkerGeneration; value.OutputItemId = p.OutputItemId;
                }));
            foreach (GridAutoProcess process in world.AutoProcesses.Values.Select(value => (GridAutoProcess)value))
            {
                var state = ProductMessages.Create<GridAutoProcessState>(value =>
                {
                    value.ProcessId = process.Id; value.RecipeId = process.RecipeId; value.PotItemId = process.PotItemId; value.StartStationId = process.StartStationId;
                    value.StartedTick = process.StartedTick; value.RequiredTicks = process.RequiredTicks; value.ProgressTicks = process.ProgressTicks;
                    value.OutputDefinitionId = process.OutputDefinitionId; value.TotalPortions = process.TotalPortions; value.RemainingPortions = process.RemainingPortions;
                    value.ServingContainerDefinitionId = process.ServingContainerDefinitionId; value.State = (int)process.State;
                });
                state.InputItemIds.AddRange(process.InputItemIds); snapshot.AutoProcesses.Add(state);
            }
            foreach (GridBakeProcess process in world.BakeProcesses.Values.Select(value => (GridBakeProcess)value))
                snapshot.BakeProcesses.Add(ProductMessages.Create<GridBakeProcessState>(value =>
                {
                    value.ProcessId=process.Id;value.RecipeId=process.RecipeId;value.StationId=process.StationId;value.InputItemId=process.InputItemId;
                    value.OutputDefinitionId=process.OutputDefinitionId;value.RequiredTicks=process.RequiredTicks;value.ProgressTicks=process.ProgressTicks;
                    value.OutputItemId=process.OutputItemId;value.State=(int)process.State;value.StartedTick=process.StartedTick;
                }));
            CopyFrontState(world, snapshot);
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
                        value.MatchId = ((Match)world.Match).MatchId; value.WorldId = world.WorldId; value.WorldGeneration = world.WorldGeneration;
                        value.BindingGeneration = peer.Generation; value.CommandId = result.CommandId; value.Status = (int)result.Status;
                        value.Reason = result.Reason; value.Version = world.Version;
                        value.LevelId = result.LevelId; value.LevelGeneration = result.LevelGeneration;
                    }));
            }
            world.PendingResults.Clear();
        }
    }
}
#endif
