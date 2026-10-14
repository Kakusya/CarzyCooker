#if !DOTNET
using System.Linq;

namespace ET
{
    [FriendOf(typeof(FrontWorkCommands))]
    [FriendOf(typeof(FrontWork))]
    [FriendOf(typeof(Customer))]
    public static partial class GridKitchenSystem
    {
        private static void RequestFrontCreation(this GridKitchenComponent self, Level level, Entity source, long serial, FrontWorkKind kind)
        {
            self.Root().GetComponent<ProcessInnerSender>().Send(((Scene)self.IScene).GetComponent<HostSessionComponent>().GetActorId(), ProductMessages.Create<FrontWorkCreateRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = ((Match)self.Match).MatchId;
                value.WorldId = self.WorldId; value.WorldGeneration = self.WorldGeneration; value.LevelId = level.Id; value.LevelGeneration = level.Generation;
                value.SourceId = source.Id; value.SourceInstanceId = source.InstanceId; value.TransitionSerial = serial; value.Kind = (int)kind;
            }));
        }

        private static void AdvanceFrontCreations(this GridKitchenComponent self, Level level, FrontWorkCommands commands)
        {
            while (commands.FinishedTransitions.Count > 0)
            {
                FrontWorkFinishedValue finished = commands.FinishedTransitions.Dequeue();
                FrontWork source = level.GetChild<FrontWork>(finished.Identity.SourceId);
                if (source == null || source.InstanceId != finished.Identity.SourceInstanceId || source.TransitionSerial != finished.Identity.Serial ||
                    source.ClaimSerial != finished.ClaimSerial || source.Kind != FrontWorkKind.Clear || source.State != FrontWorkState.Completed ||
                    !source.SuccessorPending || source.SuccessorQueued || source.ItemId != finished.ItemId || source.StationId != finished.StationId) continue;
                source.SuccessorQueued = true; self.RequestFrontCreation(level, source, source.TransitionSerial, FrontWorkKind.Wash);
            }
            while (commands.Creations.Count > 0)
            {
                FrontWorkCreateValue value = commands.Creations.Dequeue(); ProductTransitionIdentity identity = value.Identity;
                if (identity.LevelId != level.Id || identity.LevelGeneration != level.Generation) continue;
                if (value.Kind == FrontWorkKind.Wash)
                {
                    FrontWork clear = level.GetChild<FrontWork>(identity.SourceId);
                    if (clear == null || clear.InstanceId != identity.SourceInstanceId || clear.TransitionSerial != identity.Serial ||
                        clear.Kind != FrontWorkKind.Clear || clear.State != FrontWorkState.Completed || !clear.SuccessorPending || !clear.SuccessorQueued) continue;
                    GridItem item = self.Item(clear.ItemId);
                    if (item?.Location != GridItemLocation.DirtyPool || item.Hygiene != ItemHygiene.Dirty) continue;
                    GridStation wash = self.Stations.Values.Select(station => (GridStation)station).OrderBy(station => station.Id).FirstOrDefault(station => station.Kind == GridStationKind.Wash);
                    if (wash == null) continue;
                    var definition = new FrontWorkDefinition(FrontWorkKind.Wash, 0, wash.Id, item.Id, Tables.Instance.DTCompanionConfig.DataList.Single().WashTicks);
                    level.AddChild<FrontWork, FrontWorkDefinition, long>(definition, level.Generation);
                    clear.SuccessorPending = false;
                }
                else
                {
                    Customer customer = level.GetChild<Customer>(identity.SourceId);
                    if (customer == null || customer.InstanceId != identity.SourceInstanceId || customer.TransitionSerial != identity.Serial ||
                        !customer.SuccessorPending || !customer.SuccessorQueued) continue;
                    GridStation station = self.Station(customer.TableStationId);
                    if (station == null) continue;
                    var config = Tables.Instance.DTCompanionConfig.DataList.Single();
                    if (value.Kind == FrontWorkKind.Ask && customer.Phase == CustomerPhase.WaitingAsk && customer.AskWorkId == 0)
                    {
                        var definition = new FrontWorkDefinition(FrontWorkKind.Ask, customer.Id, station.Id, 0, config.AskTicks);
                        customer.AskWorkId = level.AddChild<FrontWork, FrontWorkDefinition, long>(definition, level.Generation).Id;
                    }
                    else if (value.Kind == FrontWorkKind.Clear && customer.Phase == CustomerPhase.Gone && station.GetComponent<FrontTable>()?.State == FrontTableState.Dirty)
                    {
                        var definition = new FrontWorkDefinition(FrontWorkKind.Clear, customer.Id, station.Id, station.ItemId, config.ClearTicks);
                        level.AddChild<FrontWork, FrontWorkDefinition, long>(definition, level.Generation);
                    }
                    else continue;
                    customer.SuccessorPending = false;
                }
                self.Dirty = true;
            }
        }

        private static void RequestFrontTransition(this GridKitchenComponent self, Level level, FrontWork work, FrontWorkTransitionAction action)
        {
            work.TransitionQueued = true;
            self.Root().GetComponent<ProcessInnerSender>().Send(((Scene)self.IScene).GetComponent<HostSessionComponent>().GetActorId(), ProductMessages.Create<FrontWorkTransition>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = ((Match)self.Match).MatchId;
                value.WorldId = self.WorldId; value.WorldGeneration = self.WorldGeneration; value.LevelId = level.Id; value.LevelGeneration = level.Generation;
                value.SourceId = work.Id; value.SourceInstanceId = work.InstanceId; value.TransitionSerial = work.TransitionSerial;
                value.Action = (int)action; value.ClaimSerial = work.ClaimSerial;
            }));
        }

        private static void AdvanceFrontTransitions(this GridKitchenComponent self, Level level, FrontWorkCommands commands)
        {
            while (commands.Transitions.Count > 0)
            {
                FrontWorkTransitionValue value = commands.Transitions.Dequeue(); ProductTransitionIdentity identity = value.Identity;
                FrontWork work = level.GetChild<FrontWork>(identity.SourceId);
                if (identity.LevelId != level.Id || identity.LevelGeneration != level.Generation || work == null ||
                    work.InstanceId != identity.SourceInstanceId || work.TransitionSerial != identity.Serial || work.ClaimSerial != value.ClaimSerial || !work.TransitionQueued) continue;
                if (value.Action == FrontWorkTransitionAction.Start && work.State == FrontWorkState.Approaching) work.StartReceived = true;
                if (value.Action == FrontWorkTransitionAction.Complete && work.State == FrontWorkState.Working && work.Progress >= 1 - 1e-9) work.CompletionReceived = true;
            }
        }

        private static void PublishFrontStage(this GridKitchenComponent self, Level level, FrontWork work, FrontWorkStage stage)
        {
            level.GetComponent<FrontWorkCommands>().Outcomes.Add(new FrontWorkOutcomeValue(work.Id, work.InstanceId, work.ClaimSerial, work.Kind, stage));
            self.Dirty = true;
        }

        private static void NotifyFrontFinished(this GridKitchenComponent self, Level level, FrontWork work)
        {
            self.Root().GetComponent<ProcessInnerSender>().Send(((Scene)self.IScene).GetComponent<HostSessionComponent>().GetActorId(), ProductMessages.Create<FrontWorkFinished>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = ((Match)self.Match).MatchId;
                value.WorldId = self.WorldId; value.WorldGeneration = self.WorldGeneration; value.LevelId = level.Id; value.LevelGeneration = level.Generation;
                value.SourceId = work.Id; value.SourceInstanceId = work.InstanceId; value.TransitionSerial = work.TransitionSerial; value.ClaimSerial = work.ClaimSerial;
                value.Kind = (int)work.Kind; value.StationId = work.StationId; value.CustomerId = work.CustomerId; value.ItemId = work.ItemId;
            }));
        }
    }
}
#endif
