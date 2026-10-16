#if !DOTNET
using System.Collections.Generic;
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
                message.ExpectedOrderId = request.ExpectedOrderId; message.RecipeId = request.RecipeId;
                message.LevelId = request.LevelId; message.LevelGeneration = request.LevelGeneration;
                message.ExpectedContentIds.AddRange(request.ExpectedContentIds);
                message.ExpectedRemainingPortions = request.ExpectedRemainingPortions; message.ExpectedLayoutVersion = request.ExpectedLayoutVersion;
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
                : grid == null ? "RestaurantUnavailable"
                : new ProductCommandContext(request.MatchId, request.WorldId, request.WorldGeneration, request.LevelId,
                    request.LevelGeneration, request.BindingGeneration, request.CommandId)
                    .Validate(match.MatchId, grid.WorldId, grid.WorldGeneration, level?.Id ?? 0, level?.Generation ?? 0, peer.Generation)
                    ?? grid.SubmitGridIntent(peer.MemberId, peer.Generation, new GridIntent(request.CommandId, (GridAction)request.Action,
                        new GridCell(request.X, request.Y), request.TargetId, request.ExpectedItemId, request.ExpectedTargetItemId, request.ExpectedProcessId,
                        request.ExpectedContentIds, request.ExpectedRemainingPortions, request.ExpectedLayoutVersion, expectedOrderId: request.ExpectedOrderId, recipeId: request.RecipeId));
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
    [FriendOf(typeof(KitchenSupply))]
    [FriendOf(typeof(SupplyShipment))]
    [FriendOf(typeof(RestaurantRenovation))]
    [FriendOf(typeof(SoloSaveBinding))]
    [FriendOf(typeof(SoloSaveResult))]
    [FriendOf(typeof(CooperativeSaveResult))]
    [FriendOf(typeof(FrontWorkCommands))]
    [FriendOf(typeof(LevelBuff))]
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(RestaurantUnlocks))]
    public static partial class GridBroadcast
    {
        public static GridWorldSnapshot CreateSnapshot(GridKitchenComponent world, HostPeer peer)
        {
            var snapshot = ProductMessages.Create<GridWorldSnapshot>(value =>
            {
                value.MatchId = ((Match)world.Match).MatchId; value.WorldId = world.WorldId; value.WorldGeneration = world.WorldGeneration;
                value.MemberId = peer.MemberId; value.BindingGeneration = peer.Generation; value.Version = world.Version; value.Tick = world.Tick;
                value.Width = world.Width; value.Height = world.Height; value.KitchenBottom = GridKitchenRules.KitchenBottom;
                value.TickMilliseconds = GridKitchenRules.TickMilliseconds; value.StepTicks = GridKitchenRules.StepTicks;
                Level level = world.ProductLevel();
                value.LevelId = level?.Id ?? 0; value.LevelGeneration = level?.Generation ?? 0;
                value.LevelPhase = (int)(level?.Phase ?? default); value.LevelPaused = level?.Paused ?? false;
                value.LevelOperatingTicks = level?.OperatingTicks ?? 0; value.LevelElapsedTicks = level?.ElapsedOperatingTicks ?? 0;
                value.LevelScore = level?.Score ?? 0; value.LevelStars = level?.Stars ?? 0;
                value.ServedOrders = level?.ServedOrders ?? 0; value.UnfulfilledOrders = level?.UnfulfilledOrders ?? 0;
                value.LevelConfigId = level?.ConfigId ?? 0; value.NextLevelConfigId = level?.NextLevelConfigId ?? 0;
                value.LastCommandId = peer.LastCommandContext.CommandId; value.LastCommandKind = (int)peer.LastCommandKind;
                value.LastCommandStatus = (int)peer.LastCommandStatus; value.LastCommandReason = peer.LastCommandReason;
                value.LayoutVersion = world.LayoutVersion;
                if (level != null) value.AllowedDishIds.AddRange(level.AllowedDishIds);
                var unlocked = ((RestaurantRuntime)world.Restaurant)?.GetComponent<RestaurantUnlocks>();
                if (unlocked != null)
                {
                    value.PermissionVersion = unlocked.PermissionVersion;
                    value.UnlockedDishIds.AddRange(unlocked.DishIds);
                    value.UnlockedStationIds.AddRange(unlocked.StationIds);
                }
            });
            foreach (GridActor actor in world.Actors.OrderBy(pair => pair.Key).Select(pair => (GridActor)pair.Value))
            {
                var value = ProductMessages.Create<GridActorState>(state =>
                {
                    state.MemberId = actor.MemberId; state.Generation = actor.Generation; state.X = actor.Cell.X; state.Y = actor.Cell.Y;
                    state.NextX = actor.NextCell.X; state.NextY = actor.NextCell.Y; state.StepStartTick = actor.StepStartTick;
                    state.Motion = (int)actor.Motion; state.Facing = (int)actor.Facing; state.HandItemId = actor.HandItemId;
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
                    value.DefinitionId = station.DefinitionId; value.Orientation = (int)station.Orientation;
                    value.Capabilities = (int)station.Capabilities; value.ItemSlotCapacity = station.ItemSlotCapacity;
                    value.BakeProcessId = station.BakeProcessId;
                    value.UpgradeDefinitionId = station.UpgradeDefinitionId; value.UpgradeLevel = station.UpgradeLevel;
                    var definition = Tables.Instance.DTStationConfig.Get(station.DefinitionId);
                    value.WaterDefinitionId = definition.WaterDefinitionId; value.WaterUnits = definition.WaterUnits;
                });
                foreach (GridCell cell in station.StationFootprint()) state.Footprint.Add(ProductMessages.Create<GridPathCell>(value => { value.X = cell.X; value.Y = cell.Y; }));
                foreach (GridCell cell in station.StationApproaches()) state.Approaches.Add(ProductMessages.Create<GridPathCell>(value => { value.X = cell.X; value.Y = cell.Y; }));
                snapshot.Stations.Add(state);
            }
            foreach (GridItem item in world.Items.Values.Select(value => (GridItem)value))
            {
                var state = ProductMessages.Create<GridItemState>(value => { value.ItemId = item.Id; value.Name = item.Name; value.DefinitionId = item.DefinitionId; value.Location = (int)item.Location; value.HolderId = item.HolderId; value.X = item.Cell.X; value.Y = item.Cell.Y; value.AutoProcessId = item.AutoProcessId; value.BakeProcessId = item.BakeProcessId; });
                state.Contents.AddRange(item.Contents); snapshot.Items.Add(state);
                var definition = Tables.Instance.DTGridItemConfig.Get(item.DefinitionId);
                state.Form = (int)definition.Form; state.Discardable = definition.Discardable;
                state.BoundOrderId = world.ProductLevel()?.DrinkBindings.GetValueOrDefault(item.Id) ?? 0;
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
                    value.RequiredCapability = (int)process.RequiredCapability; value.LockContainer = process.LockContainer;
                });
                state.InputItemIds.AddRange(process.InputItemIds); snapshot.AutoProcesses.Add(state);
            }
            foreach (GridBakeProcess process in world.BakeProcesses.Values.Select(value => (GridBakeProcess)value))
                snapshot.BakeProcesses.Add(ProductMessages.Create<GridBakeProcessState>(value =>
                {
                    value.ProcessId=process.Id;value.RecipeId=process.RecipeId;value.StationId=process.StationId;value.InputItemId=process.InputItemId;
                    value.OutputDefinitionId=process.OutputDefinitionId;value.RequiredTicks=process.RequiredTicks;value.ProgressTicks=process.ProgressTicks;
                    value.OutputItemId=process.OutputItemId;value.State=(int)process.State;value.StartedTick=process.StartedTick;
                    value.CarrierItemId=process.CarrierItemId;
                }));
            CopyFrontState(world, snapshot);
            KitchenSupply supply = world.GetComponent<KitchenSupply>();
            foreach (var source in supply.RemainingPackages)
                snapshot.SupplySources.Add(ProductMessages.Create<GridSupplySourceState>(value =>
                {
                    value.SourceId = source.Key; value.RemainingPackages = source.Value;
                    int layoutId = Tables.Instance.DTSupplyConfig.Get(source.Key).ReceivingLayoutId;
                    value.ReceivingStationId = world.Stations.Values.Select(station => (GridStation)station)
                        .Single(station => station.InitialLayoutId == layoutId).Id;
                }));
            foreach (SupplyShipment shipment in supply.Children.Values.OfType<SupplyShipment>())
                snapshot.SupplyShipments.Add(ProductMessages.Create<GridSupplyShipmentState>(value =>
                {
                    value.ShipmentId = shipment.Id; value.SourceId = shipment.SourceId; value.ReceivingStationId = shipment.ReceivingStationId;
                    value.PackageItemId = shipment.PackageItemId; value.RequiredTicks = shipment.RequiredTicks;
                    value.ProgressTicks = shipment.ProgressTicks; value.Stage = (int)shipment.Stage;
                }));
            RestaurantRenovation renovation = ((RestaurantRuntime)world.Restaurant)?.GetComponent<RestaurantRenovation>();
            if (renovation != null)
            {
                snapshot.RenovationCandidates.AddRange(renovation.Candidates); snapshot.SelectedRenovationId = renovation.SelectedDefinitionId;
                snapshot.RenovationChoiceVersion = renovation.ChoiceVersion; snapshot.RenovationLevelId = renovation.ResultLevelId;
                snapshot.RenovationLevelGeneration = renovation.ResultLevelGeneration;
                snapshot.RenovationFailure = renovation.Failure;
                snapshot.RenovationSelectionFinalized = renovation.SelectionFinalized;
                snapshot.SelectedUpgradeStationId = ((GridStation)renovation.PendingUpgradeStation)?.Id ?? 0;
                snapshot.PendingUpgradeExpectedLevel = renovation.PendingUpgradeExpectedLevel;
                foreach (var target in renovation.UpgradeTargets)
                    snapshot.UpgradeTargets.Add(ProductMessages.Create<GridRenovationUpgradeTarget>(value =>
                    { value.DefinitionId = target.DefinitionId; value.StationId = target.StationId; value.ExpectedLevel = target.ExpectedLevel; }));
                foreach (var applied in renovation.AppliedStations)
                {
                    var state = ProductMessages.Create<GridRenovationAppliedState>(value => value.DefinitionId = applied.Key);
                    state.StationIds.AddRange(applied.Value); snapshot.RenovationApplied.Add(state);
                }
            }
            var saveBinding = ((Match)world.Match).GetComponent<SoloSaveBinding>();
            snapshot.SaveEnabled = saveBinding != null; snapshot.LastSavedSlotId = saveBinding?.SourceSlotId ?? "";
            SoloSaveResult saveResult = saveBinding?.CurrentResult ?? default;
            if (saveResult != null && saveResult.LevelId == snapshot.LevelId && saveResult.LevelGeneration == snapshot.LevelGeneration)
            {
                snapshot.SaveResultId = saveResult.ResultId; snapshot.SaveState = (int)saveResult.State; snapshot.SaveFailure = saveResult.Failure;
            }
            snapshot.CanControlRestaurant = peer.CanControlRestaurant();
            Level current = world.ProductLevel();
            snapshot.CooperativeSave = current?.EligibleProfiles.Count > 0;
            if (snapshot.CooperativeSave && snapshot.CanControlRestaurant)
            {
                snapshot.SaveEnabled = true;
                CooperativeSaveResult result = current.ClaimResult;
                snapshot.SaveResultId = result?.ResultId;
                snapshot.SaveState = (int)(result?.State ?? SoloSaveState.Ready);
                snapshot.SaveFailure = result?.Failure;
                snapshot.LastSavedSlotId = result?.HostSavedReceipt?.SlotId ?? "";
            }
            return snapshot;
        }

        public static void SendCommitted(this GridKitchenComponent world)
        {
            HostSessionComponent authority = ((Scene)world.IScene).GetComponent<HostSessionComponent>();
            foreach (HostPeer peer in authority.Children.Values.Cast<HostPeer>().ToArray())
            {
                if (!peer.EntryOpen || peer.Closing || peer.MemberId == 0)
                {
                    peer.PendingOutcomes.Clear();
                    continue;
                }
                ProductDelivery.Send(peer, CreateSnapshot(world, peer));
                foreach (GridResultValue result in world.PendingResults.Where(value => value.MemberId == peer.MemberId && value.Generation == peer.Generation))
                    ProductDelivery.Send(peer, ProductMessages.Create<GridCommandResult>(value =>
                    {
                        value.MatchId = ((Match)world.Match).MatchId; value.WorldId = world.WorldId; value.WorldGeneration = world.WorldGeneration;
                        value.BindingGeneration = peer.Generation; value.CommandId = result.CommandId; value.Status = (int)result.Status;
                        value.Reason = result.Reason; value.Version = world.Version;
                        value.LevelId = result.LevelId; value.LevelGeneration = result.LevelGeneration;
                    }));
                Level level = world.ProductLevel();
                foreach (ProductCommandOutcomeValue result in peer.PendingOutcomes)
                {
                    long serial = checked(++peer.NextOutcomeSerial);
                    ProductDelivery.Send(peer, ProductMessages.Create<ProductCommandOutcome>(value =>
                    {
                        value.MatchId = result.Context.MatchId; value.WorldId = result.Context.WorldId;
                        value.WorldGeneration = result.Context.WorldGeneration; value.MemberId = peer.MemberId;
                        value.BindingGeneration = result.Context.BindingGeneration; value.Version = world.Version; value.Tick = world.Tick;
                        value.LevelId = result.ChangedLevel ? level.Id : result.Context.LevelId;
                        value.LevelGeneration = result.ChangedLevel ? level.Generation : result.Context.LevelGeneration;
                        value.OriginLevelId = result.Context.LevelId; value.OriginLevelGeneration = result.Context.LevelGeneration;
                        value.Kind = (int)result.Kind; value.CommandId = result.Context.CommandId; value.Status = (int)result.Status;
                        value.Reason = result.Reason; value.OutcomeSerial = serial;
                    }));
                }
                peer.PendingOutcomes.Clear();
                foreach (KitchenWorkOutcomeValue result in world.WorkOutcomes)
                {
                    long serial = checked(++peer.NextOutcomeSerial);
                    ProductDelivery.Send(peer, ProductMessages.Create<KitchenWorkOutcome>(value =>
                    {
                        value.MatchId = ((Match)world.Match).MatchId; value.WorldId = world.WorldId; value.WorldGeneration = world.WorldGeneration;
                        value.LevelId = level?.Id ?? 0; value.LevelGeneration = level?.Generation ?? 0;
                        value.MemberId = peer.MemberId; value.BindingGeneration = peer.Generation; value.Version = world.Version; value.Tick = world.Tick;
                        value.ProcessKind = (int)result.Kind; value.Stage = (int)result.Stage;
                        value.WorkId = result.WorkId; value.WorkInstanceId = result.WorkInstanceId; value.TransitionSerial = result.Serial;
                        value.OutputItemId = result.OutputItemId; value.OutcomeSerial = serial;
                    }));
                }
                foreach (ShipmentOutcomeValue result in world.GetComponent<KitchenSupply>().Outcomes)
                {
                    long serial = checked(++peer.NextOutcomeSerial);
                    ProductDelivery.Send(peer, ProductMessages.Create<ShipmentOutcome>(value =>
                    {
                        value.MatchId = ((Match)world.Match).MatchId; value.WorldId = world.WorldId; value.WorldGeneration = world.WorldGeneration;
                        value.LevelId = level?.Id ?? 0; value.LevelGeneration = level?.Generation ?? 0;
                        value.MemberId = peer.MemberId; value.BindingGeneration = peer.Generation; value.Version = world.Version; value.Tick = world.Tick;
                        value.ShipmentId = result.ShipmentId; value.ShipmentInstanceId = result.ShipmentInstanceId;
                        value.TransitionSerial = result.Serial; value.Stage = (int)result.Stage; value.OutcomeSerial = serial;
                    }));
                }
                if (level != null)
                {
                    LevelBuff buff = level.GetComponent<LevelBuff>();
                    if (buff.NotifyPrepared)
                    {
                        long serial = checked(++peer.NextOutcomeSerial);
                        ProductDelivery.Send(peer, ProductMessages.Create<BuffPrepared>(value =>
                        {
                            value.MatchId = ((Match)world.Match).MatchId; value.WorldId = world.WorldId; value.WorldGeneration = world.WorldGeneration;
                            value.LevelId = level.Id; value.LevelGeneration = level.Generation;
                            value.MemberId = peer.MemberId; value.BindingGeneration = peer.Generation; value.Version = world.Version; value.Tick = world.Tick;
                            value.DrawVersion = buff.DrawVersion; value.OutcomeSerial = serial;
                        }));
                    }
                    RestaurantRenovation renovation = ((RestaurantRuntime)world.Restaurant).GetComponent<RestaurantRenovation>();
                    if (renovation.NotifyPrepared)
                    {
                        long serial = checked(++peer.NextOutcomeSerial);
                        ProductDelivery.Send(peer, ProductMessages.Create<RenovationPrepared>(value =>
                        {
                            value.MatchId = ((Match)world.Match).MatchId; value.WorldId = world.WorldId; value.WorldGeneration = world.WorldGeneration;
                            value.LevelId = renovation.ResultLevelId; value.LevelGeneration = renovation.ResultLevelGeneration;
                            value.MemberId = peer.MemberId; value.BindingGeneration = peer.Generation; value.Version = world.Version; value.Tick = world.Tick;
                            value.ChoiceVersion = renovation.ChoiceVersion; value.OutcomeSerial = serial;
                        }));
                    }
                }
                if (level != null)
                foreach (FrontWorkOutcomeValue result in level.GetComponent<FrontWorkCommands>().Outcomes)
                {
                    long serial = checked(++peer.NextOutcomeSerial);
                    ProductDelivery.Send(peer, ProductMessages.Create<FrontWorkOutcome>(value =>
                    {
                        value.MatchId = ((Match)world.Match).MatchId; value.WorldId = world.WorldId; value.WorldGeneration = world.WorldGeneration;
                        value.LevelId = level.Id; value.LevelGeneration = level.Generation;
                        value.MemberId = peer.MemberId; value.BindingGeneration = peer.Generation; value.Version = world.Version; value.Tick = world.Tick;
                        value.WorkId = result.WorkId; value.WorkInstanceId = result.WorkInstanceId; value.ClaimSerial = result.ClaimSerial;
                        value.Kind = (int)result.Kind; value.Stage = (int)result.Stage; value.OutcomeSerial = serial;
                    }));
                }
                RestaurantRuntime restaurant = world.Restaurant;
                if (restaurant != null)
                foreach (LevelPhaseOutcomeValue result in restaurant.PhaseOutcomes)
                {
                    long serial = checked(++peer.NextOutcomeSerial);
                    ProductDelivery.Send(peer, ProductMessages.Create<LevelPhaseChanged>(value =>
                    {
                        value.MatchId = ((Match)world.Match).MatchId; value.WorldId = world.WorldId; value.WorldGeneration = world.WorldGeneration;
                        value.LevelId = result.LevelId; value.LevelGeneration = result.LevelGeneration;
                        value.MemberId = peer.MemberId; value.BindingGeneration = peer.Generation; value.Version = world.Version; value.Tick = world.Tick;
                        value.Phase = (int)result.Phase; value.Paused = result.Paused; value.OutcomeSerial = serial;
                    }));
                }
            }
            world.PendingResults.Clear();
            world.WorkOutcomes.Clear();
            world.GetComponent<KitchenSupply>().Outcomes.Clear();
            world.ProductLevel()?.GetComponent<FrontWorkCommands>().Outcomes.Clear();
            Level committedLevel = world.ProductLevel();
            if (committedLevel != null) committedLevel.GetComponent<LevelBuff>().NotifyPrepared = false;
            RestaurantRuntime committedRestaurant = world.Restaurant;
            if (committedRestaurant != null) committedRestaurant.GetComponent<RestaurantRenovation>().NotifyPrepared = false;
            if (committedRestaurant != null) committedRestaurant.PhaseOutcomes.Clear();
        }
    }
}
#endif
