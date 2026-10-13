#if !DOTNET
using System;
using System.Linq;
using Game;

namespace ET
{
    [EntitySystemOf(typeof(RestaurantRenovation))]
    [FriendOf(typeof(RestaurantRenovation))]
    public static partial class RestaurantRenovationSystem
    {
        [EntitySystem]
        private static void Awake(this RestaurantRenovation self)
        {
            self.ResultLevelId = 0; self.ResultLevelGeneration = 0; self.ChoiceVersion = 0;
            self.SelectedDefinitionId = 0; self.SelectedName = null;
            self.Candidates = Array.Empty<int>(); self.PendingPlacements = Array.Empty<GridStationPlacement>();
            self.AppliedStations.Clear(); self.Choices.Clear();
            self.Preparations.Clear();
            self.RequestedLevelId = self.RequestedLevelGeneration = 0; self.PreparationSerial = 0;
            self.Prepared = self.PreparationQueued = self.NotifyPrepared = false;
        }
    }

    [FriendOf(typeof(RestaurantRenovation))]
    [FriendOf(typeof(GridStation))]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(ProductRuntimeComponent))]
    public static partial class RestaurantRuntimeSystem
    {
        private static bool IsSoloRenovation(this RestaurantRuntime self)
        {
            HostSessionComponent authority = ((Scene)self.IScene).GetComponent<HostSessionComponent>();
            ProductRuntimeComponent runtime = authority?.Runtime ?? default;
            return runtime?.Config.Role == RuntimeInstanceRole.Solo;
        }

        public static string QueueRenovationChoice(this RestaurantRuntime self, HostPeer peer, ProductCommandContext context, RenovationChoiceIntent intent)
        {
            GridKitchenComponent kitchen = self.Kitchen; Level level = self.CurrentLevel; Match match = self.Match;
            if (self.Stopped || peer == null || !kitchen.HasBinding(peer.MemberId, peer.Generation)) return "EntryClosed";
            if (!self.IsSoloRenovation()) return "SoloRestaurantOnly";
            string error = context.Validate(match.MatchId, kitchen.WorldId, kitchen.WorldGeneration, level.Id, level.Generation, peer.Generation);
            if (error != null) return error;
            error = peer.CheckProductCommand(context, ProductCommandKind.Renovation, intent.Equals(peer.LastRenovationCommand), out bool replay);
            if (error != null || replay) return error;
            if (level.Phase != LevelPhase.Completed) return "WrongPhase";
            peer.RecordProductCommand(context, ProductCommandKind.Renovation); peer.LastRenovationCommand = intent;
            self.GetComponent<RestaurantRenovation>().Choices.Enqueue(new RenovationChoiceEnvelope(context, intent, peer));
            return null;
        }

        private static void PrepareRenovationChoices(this RestaurantRuntime self, Level level)
        {
            if (!self.IsSoloRenovation()) return;
            RestaurantRenovation renovation = self.GetComponent<RestaurantRenovation>(); GridKitchenComponent kitchen = self.Kitchen;
            if (renovation.RequestedLevelId == level.Id && renovation.RequestedLevelGeneration == level.Generation) return;
            renovation.RequestedLevelId = level.Id; renovation.RequestedLevelGeneration = level.Generation;
            renovation.Prepared = renovation.PreparationQueued = renovation.NotifyPrepared = false;
            renovation.ResultLevelId = renovation.ResultLevelGeneration = 0;
            renovation.Candidates = Array.Empty<int>(); renovation.SelectedDefinitionId = 0; renovation.SelectedName = null;
            renovation.PendingPlacements = Array.Empty<GridStationPlacement>();
            self.Root().GetComponent<ProcessInnerSender>().Send(((Scene)self.IScene).GetComponent<HostSessionComponent>().GetActorId(), ProductMessages.Create<LevelFinished>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = ((Match)self.Match).MatchId;
                value.WorldId = kitchen.WorldId; value.WorldGeneration = kitchen.WorldGeneration; value.LevelId = level.Id; value.LevelGeneration = level.Generation;
                value.SourceId = level.Id; value.SourceInstanceId = level.InstanceId; value.TransitionSerial = level.CompletionSerial;
            }));
        }

        private static void AdvanceRenovationPreparation(this RestaurantRuntime self, Level level)
        {
            if (!self.IsSoloRenovation() || level.Phase != LevelPhase.Completed || level.Paused) return;
            RestaurantRenovation renovation = self.GetComponent<RestaurantRenovation>(); GridKitchenComponent kitchen = self.Kitchen;
            while (self.FinishedTransitions.Count > 0)
            {
                ProductTransitionIdentity identity = self.FinishedTransitions.Dequeue();
                if (identity.LevelId != level.Id || identity.LevelGeneration != level.Generation || identity.SourceId != level.Id ||
                    identity.SourceInstanceId != level.InstanceId || identity.Serial != level.CompletionSerial ||
                    renovation.RequestedLevelId != level.Id || renovation.RequestedLevelGeneration != level.Generation || renovation.Prepared || renovation.PreparationQueued) continue;
                renovation.PreparationSerial = identity.Serial; renovation.PreparationQueued = true;
                self.Root().GetComponent<ProcessInnerSender>().Send(((Scene)self.IScene).GetComponent<HostSessionComponent>().GetActorId(), ProductMessages.Create<RenovationPrepare>(value =>
                {
                    value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = ((Match)self.Match).MatchId;
                    value.WorldId = kitchen.WorldId; value.WorldGeneration = kitchen.WorldGeneration; value.LevelId = level.Id; value.LevelGeneration = level.Generation;
                    value.SourceId = level.Id; value.SourceInstanceId = level.InstanceId; value.TransitionSerial = renovation.PreparationSerial;
                }));
            }
            while (renovation.Preparations.Count > 0)
            {
                ProductTransitionIdentity identity = renovation.Preparations.Dequeue();
                if (identity.LevelId != level.Id || identity.LevelGeneration != level.Generation || identity.SourceId != level.Id ||
                    identity.SourceInstanceId != level.InstanceId || identity.Serial != renovation.PreparationSerial ||
                    renovation.RequestedLevelId != level.Id || !renovation.PreparationQueued || renovation.Prepared) continue;
                self.CompleteRenovationPreparation(level);
                renovation.Prepared = renovation.NotifyPrepared = true; renovation.PreparationQueued = false; ++renovation.PreparationSerial;
                kitchen.Dirty = true;
            }
        }

        private static void CompleteRenovationPreparation(this RestaurantRuntime self, Level level)
        {
            RestaurantRenovation renovation = self.GetComponent<RestaurantRenovation>(); GridKitchenComponent kitchen = self.Kitchen;
            renovation.ResultLevelId = level.Id; renovation.ResultLevelGeneration = level.Generation;
            renovation.ChoiceVersion = checked(renovation.ChoiceVersion + 1); renovation.SelectedDefinitionId = 0; renovation.SelectedName = null;
            renovation.PendingPlacements = Array.Empty<GridStationPlacement>();
            renovation.Candidates = Array.AsReadOnly(level.AllowedRenovationIds.Where(id => !renovation.AppliedStations.ContainsKey(id) &&
                kitchen.CheckRenovationInstallation(RenovationConfig.Placements(Tables.Instance.DTRenovationConfig.Get(id)).ToArray(), out _) == null).OrderBy(id => id).ToArray());
        }

        private static void AdvanceRenovationChoices(this RestaurantRuntime self, Level level)
        {
            RestaurantRenovation renovation = self.GetComponent<RestaurantRenovation>(); GridKitchenComponent kitchen = self.Kitchen; Match match = self.Match;
            while (renovation.Choices.Count > 0)
            {
                RenovationChoiceEnvelope command = renovation.Choices.Dequeue(); HostPeer peer = command.Peer;
                string error = peer == null || !kitchen.HasBinding(peer.MemberId, command.Context.BindingGeneration) ? "EntryClosed"
                    : command.Context.Validate(match.MatchId, kitchen.WorldId, kitchen.WorldGeneration, level.Id, level.Generation, peer.Generation);
                if (error == null) error = level.Phase != LevelPhase.Completed ? "WrongPhase"
                    : !renovation.Prepared ? "RenovationNotReady"
                    : renovation.ResultLevelId != level.Id || renovation.ResultLevelGeneration != level.Generation ||
                        command.Intent.ChoiceVersion != renovation.ChoiceVersion ? "RenovationChanged"
                    : renovation.SelectedDefinitionId != 0 ? "RenovationLocked"
                    : !renovation.Candidates.Contains(command.Intent.DefinitionId) ? "InvalidRenovationChoice" : null;
                if (error == null)
                {
                    DRRenovationConfig definition = Tables.Instance.DTRenovationConfig.Get(command.Intent.DefinitionId);
                    var plan = RenovationConfig.Placements(definition).ToArray();
                    error = kitchen.CheckRenovationInstallation(plan, out _);
                    if (error == null)
                    {
                        renovation.SelectedDefinitionId = definition.Id; renovation.SelectedName = definition.Name;
                        renovation.PendingPlacements = Array.AsReadOnly(plan);
                    }
                }
                peer?.RecordProductResult(command.Context, ProductCommandKind.Renovation, error == null ? GridResultStatus.Executed : GridResultStatus.Rejected, error);
                kitchen.Dirty = true;
            }
        }

        private static string CheckPendingRenovation(this RestaurantRuntime self)
        {
            RestaurantRenovation renovation = self.GetComponent<RestaurantRenovation>();
            Level level = self.CurrentLevel;
            if (self.IsSoloRenovation() && (!renovation.Prepared || renovation.ResultLevelId != level.Id || renovation.ResultLevelGeneration != level.Generation))
                return "RenovationNotReady";
            if (renovation.SelectedDefinitionId == 0 || renovation.AppliedStations.ContainsKey(renovation.SelectedDefinitionId)) return null;
            return ((GridKitchenComponent)self.Kitchen).CheckRenovationInstallation(renovation.PendingPlacements, out _);
        }

        private static void ApplyPendingRenovation(this RestaurantRuntime self)
        {
            RestaurantRenovation renovation = self.GetComponent<RestaurantRenovation>(); GridKitchenComponent kitchen = self.Kitchen;
            if (renovation.SelectedDefinitionId == 0 || renovation.AppliedStations.ContainsKey(renovation.SelectedDefinitionId)) return;
            // Continue已完整预检；只创建配置工位，绝不附加初始物品。
            var ids = new long[renovation.PendingPlacements.Count];
            for (int i = 0; i < ids.Length; ++i)
            {
                GridStationPlacement placement = renovation.PendingPlacements[i]; DRStationConfig definition = Tables.Instance.DTStationConfig.Get(placement.DefinitionId);
                GridStation station = kitchen.AddChild<GridStation>();
                station.InitialLayoutId = 0; station.DefinitionId = definition.Id; station.Name = renovation.SelectedName;
                station.Cell = placement.Cell; station.Orientation = placement.Orientation; station.ItemSlotCapacity = definition.ItemSlotCapacity;
                station.Kind = (GridStationKind)definition.Kind; station.Capabilities = (GridStationCapability)definition.Capabilities;
                station.ItemId = 0; station.AutoProcessId = 0; station.BakeProcessId = 0;
                kitchen.Stations.Add(station.Id, station); ids[i] = station.Id;
            }
            renovation.AppliedStations.Add(renovation.SelectedDefinitionId, Array.AsReadOnly(ids));
            kitchen.Blocked.Clear(); kitchen.Blocked.UnionWith(GridStationConfig.ValidateTerrain(
                GridStationConfig.ValidateDefinitions(Tables.Instance.DTStationConfig.DataList),
                kitchen.Stations.Values.Select(value => (GridStation)value).Select(value => new GridStationPlacement(value.DefinitionId, value.Cell, value.Orientation)),
                Tables.Instance.DTLevelConfig.DataList.Select(row => new GridCell(row.SpawnX, row.SpawnY))));
            ++kitchen.LayoutVersion; kitchen.Dirty = true;
        }

        private static void CancelRenovationChoices(this RestaurantRuntime self, string reason)
        {
            RestaurantRenovation renovation = self.GetComponent<RestaurantRenovation>();
            if (renovation == null) return;
            while (renovation.Choices.Count > 0)
            {
                RenovationChoiceEnvelope command = renovation.Choices.Dequeue();
                command.Peer?.RecordProductResult(command.Context, ProductCommandKind.Renovation, GridResultStatus.Cancelled, reason);
            }
        }
    }
}
#endif
