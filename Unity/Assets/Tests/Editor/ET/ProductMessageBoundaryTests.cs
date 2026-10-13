#if !DOTNET
using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [Event(SceneType.ProductClient)]
    [FriendOf(typeof(GridClientComponent))]
    public sealed class ProductOutcomeTestObserver : AEvent<Scene, ProductCommandOutcomeApplied>
    {
        public static readonly List<(ProductCommandOutcomeValue Value, long Version, int Remaining)> Received = new();
        protected override UniTask Run(Scene scene, ProductCommandOutcomeApplied args)
        {
            var view = scene.GetComponent<ClientSessionComponent>().GetComponent<GridClientComponent>();
            Received.Add((args.Value, view.Version, view.SupplySources.Values.Sum(source => source.RemainingPackages)));
            return UniTask.CompletedTask;
        }
    }

    [Event(SceneType.ProductClient)]
    public sealed class KitchenOutcomeTestObserver : AEvent<Scene, KitchenWorkOutcomeApplied>
    {
        public static readonly List<KitchenWorkOutcomeValue> Received = new();
        protected override UniTask Run(Scene scene, KitchenWorkOutcomeApplied args)
        {
            Received.Add(args.Value);
            return UniTask.CompletedTask;
        }
    }

    [Event(SceneType.ProductClient)]
    [FriendOf(typeof(GridClientComponent))]
    public sealed class ShipmentOutcomeTestObserver : AEvent<Scene, ShipmentOutcomeApplied>
    {
        public static readonly List<(ShipmentOutcomeValue Value, ShipmentStage Installed)> Received = new();
        protected override UniTask Run(Scene scene, ShipmentOutcomeApplied args)
        {
            var view = scene.GetComponent<ClientSessionComponent>().GetComponent<GridClientComponent>();
            Received.Add((args.Value, view.SupplyShipments[args.Value.ShipmentId].Stage));
            return UniTask.CompletedTask;
        }
    }

    [FriendOf(typeof(FrontLevelState))]
    [FriendOf(typeof(FrontWorkCommands))]
    [FriendOf(typeof(KitchenSupply))]
    [FriendOf(typeof(LevelBuff))]
    [FriendOf(typeof(RestaurantRenovation))]
    public sealed partial class ProductSessionTests
    {
        private MessageObject TransitionNotice(string name, Entity source)
        {
            Type type = typeof(KitchenWorkTransition).Assembly.GetType("ET." + name);
            var message = (MessageObject)ObjectPool.Instance.Fetch(type, false);
            void Set(string property, object value) => type.GetProperty(property).SetValue(message, value);
            Set("ProtocolVersion", ProductSessionRules.ProtocolVersion); Set("MatchId", ((Match)Grid.Match).MatchId);
            Set("WorldId", Grid.WorldId); Set("WorldGeneration", Grid.WorldGeneration);
            Set("LevelId", CurrentLevel.Id); Set("LevelGeneration", CurrentLevel.Generation);
            Set("SourceId", source.Id); Set("SourceInstanceId", source.InstanceId); Set("TransitionSerial", 1L);
            if (type.GetProperty("Action") != null) Set("Action", 1);
            if (type.GetProperty("ProcessKind") != null) Set("ProcessKind", (int)KitchenProcessKind.Manual);
            if (type.GetProperty("Kind") != null) Set("Kind", (int)FrontWorkKind.Ask);
            return message;
        }

        private Entity TransitionSource(string name)
        {
            switch (name)
            {
                case nameof(KitchenWorkTransition): return Grid.AddChild<GridManualProcess>();
                case nameof(CustomerFlowTransition): return CurrentLevel.GetComponent<FrontLevelState>();
                case nameof(CustomerFlowFinished):
                case nameof(FrontWorkCreateRequest):
                    var flow = CurrentLevel.GetComponent<FrontLevelState>();
                    return CurrentLevel.AddChild<Customer, DRCustomerFlowConfig, long, int>(Tables.Instance.DTCustomerFlowConfig.Get(flow.FlowDefinitionId), CurrentLevel.Generation, 1);
                case nameof(FrontWorkTransition):
                case nameof(FrontWorkFinished):
                    return CurrentLevel.AddChild<FrontWork, FrontWorkDefinition, long>(new FrontWorkDefinition(FrontWorkKind.Ask, 0, 0, 0, 1), CurrentLevel.Generation);
                case nameof(ShipmentTransition): return Supply.AddChild<SupplyShipment>();
                case nameof(BuffPrepare): return CurrentLevel.GetComponent<LevelBuff>();
                default: return CurrentLevel;
            }
        }

        private int PendingTransitionCount() => Grid.WorkTransitions.Count +
            CurrentLevel.GetComponent<FrontLevelState>().Transitions.Count + CurrentLevel.GetComponent<FrontLevelState>().FinishedTransitions.Count +
            CurrentLevel.GetComponent<FrontWorkCommands>().Transitions.Count + CurrentLevel.GetComponent<FrontWorkCommands>().FinishedTransitions.Count + CurrentLevel.GetComponent<FrontWorkCommands>().Creations.Count +
            Supply.Transitions.Count + Restaurant.Transitions.Count + Restaurant.FinishedTransitions.Count + CurrentLevel.GetComponent<LevelBuff>().Preparations.Count + Renovation.Preparations.Count;
        [TestCase(nameof(KitchenWorkTransition))]
        [TestCase(nameof(CustomerFlowTransition))]
        [TestCase(nameof(CustomerFlowFinished))]
        [TestCase(nameof(FrontWorkTransition))]
        [TestCase(nameof(FrontWorkFinished))]
        [TestCase(nameof(ShipmentTransition))]
        [TestCase(nameof(LevelTransition))]
        [TestCase(nameof(BuffPrepare))]
        [TestCase(nameof(RenovationPrepare))]
        [TestCase(nameof(FrontWorkCreateRequest))]
        [TestCase(nameof(LevelFinished))]
        public void ProductMessageBoundaryTypedTransitionsReachOwnedQueueBeforeBusinessCommit(string name)
        {
            StartSolo();
            Entity source = TransitionSource(name);
            MessageObject message = TransitionNotice(name, source);
            int pending = PendingTransitionCount(); long tick = Grid.Tick, time = clock.Milliseconds;
            var authority = (HostSessionComponent)host.Authority;
            ((ProcessInnerSender)host.Sender).Send(authority.GetActorId(), message);
            Assert.That(PendingTransitionCount(), Is.EqualTo(pending), "Not delivered yet");
            PumpMessages(8);
            Assert.That(PendingTransitionCount(), Is.EqualTo(pending + 1), name);
            Assert.That(Grid.Tick, Is.EqualTo(tick)); Assert.That(clock.Milliseconds, Is.EqualTo(time));
            message.GetType().GetProperty("SourceId").SetValue(message, 0L);
            if (message is KitchenWorkTransition)
            {
                Assert.That(Grid.WorkTransitions.Peek().Identity.SourceId, Is.EqualTo(source.Id), "Independent copied values");
            }
        }

        [TestCase("world")]
        [TestCase("instance")]
        [TestCase("peer")]
        public void ProductMessageBoundaryInvalidTransitionCannotEnterAnotherOwner(string fault)
        {
            StartSolo();
            Entity source = TransitionSource(nameof(KitchenWorkTransition));
            var message = (KitchenWorkTransition)TransitionNotice(nameof(KitchenWorkTransition), source);
            if (fault == "world") message.WorldId = "retired world";
            if (fault == "instance") message.SourceInstanceId++;
            var target = fault == "peer" ? ((HostPeer)((LocalMessageComponent)((ClientSessionComponent)host.Client).Local).Peer).GetActorId()
                : ((HostSessionComponent)host.Authority).GetActorId();
            ((ProcessInnerSender)host.Sender).Send(target, message); PumpMessages(8);
            Assert.That(Grid.WorkTransitions, Is.Empty);
        }

        [Test]
        public void ProductMessageBoundaryTwoCommandsEachNotifyAfterSnapshotAndReplayDoesNotNotifyTwice()
        {
            StartSolo();
            ClientSessionComponent client = host.Client;
            int remaining = SupplyView.SupplySources.Values.Sum(source => source.RemainingPackages);
            var first = SupplyView.CreateSupplyCommand(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id));
            var second = SupplyView.CreateSupplyCommand(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id));
            long firstId = first.CommandId, secondId = second.CommandId;
            var replay = CopyReplayRequest(second);
            using (var accepted = SupplyReply(first)) Assert.That(accepted.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            using (var accepted = SupplyReply(second)) Assert.That(accepted.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            Assert.That(ProductOutcomeTestObserver.Received, Is.Empty);
            AdvanceTicks(1); PumpMessages(8);
            var received = ProductOutcomeTestObserver.Received.Where(value => value.Value.Kind == ProductCommandKind.Supply).ToArray();
            Assert.That(received.Select(value => value.Value.Context.CommandId), Is.EqualTo(new[] { firstId, secondId }));
            Assert.That(received.All(value => value.Version == Grid.Version && value.Remaining == remaining - 2), Is.True, "Snapshot precedes both outcomes");
            Assert.That(received.All(value => value.Value.Status == GridResultStatus.Executed), Is.True);
            Assert.That(SupplyView.LastCommandId, Is.EqualTo(secondId));
            using (var repeated = SupplyReply(replay)) Assert.That(repeated.Status, Is.EqualTo((int)GridResultStatus.Executed));
            PumpMessages(8);
            Assert.That(ProductOutcomeTestObserver.Received.Count, Is.EqualTo(2));
        }

        [TestCase(ProductCommandKind.Level)]
        [TestCase(ProductCommandKind.Layout)]
        [TestCase(ProductCommandKind.Buff)]
        [TestCase(ProductCommandKind.Supply)]
        [TestCase(ProductCommandKind.Renovation)]
        [TestCase(ProductCommandKind.Serve)]
        [TestCase(ProductCommandKind.FrontWork)]
        public void ProductMessageBoundaryEachAcceptedCommandFamilyHasExplicitTerminal(ProductCommandKind kind)
        {
            StartSolo(liveBuff: kind == ProductCommandKind.Buff);
            ClientSessionComponent client = host.Client;
            var view = client.GetComponent<GridClientComponent>();
            if (kind == ProductCommandKind.Renovation) RenovationComplete();
            if (kind is ProductCommandKind.Serve or ProductCommandKind.FrontWork) LevelDo(LevelControlAction.Start);
            ProductOutcomeTestObserver.Received.Clear();
            IRequest request = kind switch
            {
                ProductCommandKind.Level => view.CreateLevelCommand(LevelControlAction.Start),
                ProductCommandKind.Layout => view.CreateStationLayout(PrepStation("切配台").Id, PrepStation("切配台").Cell, 0),
                ProductCommandKind.Buff => view.CreateBuffChoice(Buff.Candidates[0], Buff.DrawVersion),
                ProductCommandKind.Supply => view.CreateSupplyCommand(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id)),
                ProductCommandKind.Renovation => view.CreateRenovationChoice(Renovation.Candidates[0], Renovation.ChoiceVersion),
                ProductCommandKind.Serve => view.CreateServeOrder(long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue),
                _ => view.CreateFrontWorkCommand(FrontWorkAction.Claim, long.MaxValue)
            };
            IResponse response = FinishMessages(client.CallProduct(request), "family receipt");
            try { Assert.That(response.Error, Is.Zero, response.Message); }
            finally { ((MessageObject)response).Dispose(); }
            Assert.That(ProductOutcomeTestObserver.Received, Is.Empty, "Receipt does not report completed");
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(ProductOutcomeTestObserver.Received.Count, Is.EqualTo(1), kind.ToString());
            var outcome = ProductOutcomeTestObserver.Received.Single();
            Assert.That(outcome.Value.Kind, Is.EqualTo(kind));
            Assert.That(outcome.Value.Status, Is.EqualTo(kind is ProductCommandKind.Serve or ProductCommandKind.FrontWork
                ? GridResultStatus.Rejected : GridResultStatus.Executed));
            Assert.That(outcome.Version, Is.EqualTo(Grid.Version));
            Assert.That(view.LastProductOutcome.Context.CommandId, Is.EqualTo(outcome.Value.Context.CommandId));
        }

        [Test]
        public void ProductMessageBoundaryPooledOutcomeCopiesValueAndRejectsDuplicateFutureOrOldWorld()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            var view = client.GetComponent<GridClientComponent>();
            ProductCommandOutcome Message(long serial) => ProductMessages.Create<ProductCommandOutcome>(value =>
            {
                value.MatchId = view.MatchId; value.WorldId = view.WorldId; value.WorldGeneration = view.WorldGeneration;
                value.LevelId = view.LevelId; value.LevelGeneration = view.LevelGeneration;
                value.OriginLevelId = view.LevelId; value.OriginLevelGeneration = view.LevelGeneration;
                value.MemberId = view.MemberId; value.BindingGeneration = view.BindingGeneration; value.Version = view.Version;
                value.Kind = (int)ProductCommandKind.Layout; value.CommandId = 20; value.Status = (int)GridResultStatus.Rejected;
                value.Reason = "Original reason"; value.OutcomeSerial = serial;
            });
            var source = Message(1);
            var pooled = ProductCommandOutcome.Create(true);
            foreach (var property in typeof(ProductCommandOutcome).GetProperties().Where(property => property.Name != "IsFromPool"))
            {
                if (property.CanRead && property.CanWrite) property.SetValue(pooled, property.GetValue(source));
            }
            var sender = (ProcessInnerSender)host.Sender;
            sender.Send(client.GetActorId(), pooled); PumpMessages(8);
            var reused = ProductCommandOutcome.Create(true); reused.Reason = "Reused";
            Assert.That(ReferenceEquals(reused, pooled), Is.True, "Actual pooled DTO reuse");
            Assert.That(view.LastProductOutcome.Reason, Is.EqualTo("Original reason")); reused.Dispose();
            sender.Send(client.GetActorId(), Message(1)); PumpMessages(8);
            var future = Message(2); future.Version++; sender.Send(client.GetActorId(), future); PumpMessages(8);
            var retired = Message(2); retired.WorldGeneration++; sender.Send(client.GetActorId(), retired); PumpMessages(8);
            Assert.That(view.LastProductOutcomeSerial, Is.EqualTo(1));
            Assert.That(ProductOutcomeTestObserver.Received.Count, Is.EqualTo(1));
        }

        [Test]
        public void ProductMessageBoundaryOldGridCancellationStillHasOutcomeWithoutReplacingNewCommand()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            var view = client.GetComponent<GridClientComponent>(); GridActor actor = Grid.Actor(client.MemberId);
            var first = view.CreateGridCommand(GridAction.Move, actor.Cell); long firstId = first.CommandId;
            using (var reply = (GridCommandResponse)FinishMessages(client.CallProduct(first), "first grid receipt")) Assert.That(reply.Error, Is.Zero);
            var second = view.CreateGridCommand(GridAction.Move, actor.Cell); long secondId = second.CommandId;
            using (var reply = (GridCommandResponse)FinishMessages(client.CallProduct(second), "replacement grid receipt")) Assert.That(reply.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            var observed = ProductOutcomeTestObserver.Received.Where(value => value.Value.Kind == ProductCommandKind.Grid).ToArray();
            Assert.That(observed.Any(value => value.Value.Context.CommandId == firstId && value.Value.Status == GridResultStatus.Cancelled), Is.True);
            Assert.That(observed.Any(value => value.Value.Context.CommandId == secondId && value.Value.Status == GridResultStatus.Executed), Is.True);
            Assert.That(view.LastCommandId, Is.EqualTo(secondId));
        }

        [TestCase(nameof(KitchenWorkTransition))]
        [TestCase(nameof(CustomerFlowTransition))]
        [TestCase(nameof(CustomerFlowFinished))]
        [TestCase(nameof(FrontWorkTransition))]
        [TestCase(nameof(FrontWorkFinished))]
        [TestCase(nameof(ShipmentTransition))]
        [TestCase(nameof(LevelTransition))]
        [TestCase(nameof(BuffPrepare))]
        [TestCase(nameof(RenovationPrepare))]
        [TestCase(nameof(FrontWorkCreateRequest))]
        [TestCase(nameof(LevelFinished))]
        public void ProductMessageBoundaryNewInternalTypesCannotEnterHostFromWire(string name)
        {
            StartHost(); var remote = CoopRemote();
            MessageObject message = TransitionNotice(name, TransitionSource(name));
            int pending = PendingTransitionCount();
            ((Session)remote.Session).Send(message); message.Dispose(); Pump(80);
            Assert.That(PendingTransitionCount(), Is.EqualTo(pending));
            Match match = ((HostSessionComponent)host.Authority).Match;
            Assert.That(match.GetChild<Participant>(remote.MemberId).Online, Is.False);
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
        }

        private GridManualProcess BoundaryManual(ClientSessionComponent client, bool held)
        {
            if (held)
            {
                long bowl = PrepEggBowl(client), egg = Grid.Item(bowl).Contents.Single();
                PrepDo(client, GridAction.StartHeldWork, bowl, egg, bowl);
                return Grid.InputProcess(egg);
            }
            GridStation source = PrepStation("备料台"), station = PrepStation("切配台");
            long tomato = source.ItemId;
            PrepDo(client, GridAction.TakeCounter, source.Id, tomato); PrepDo(client, GridAction.PlaceCounter, station.Id, tomato);
            PrepDo(client, GridAction.StartStationWork, station.Id, tomato, tomato);
            return Grid.InputProcess(tomato);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ProductMessageBoundaryManualDeadlineNeedsDeliveryThenCompletesOnce(bool held)
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            GridManualProcess work = BoundaryManual(client, held);
            long input = work.InputItemId;
            PumpMessages(8);
            Assert.That(KitchenOutcomeTestObserver.Received.Any(value => value.WorkId == work.Id && value.Stage == KitchenWorkStage.Started), Is.True);
            int remaining = work.RequiredTicks - work.ProgressTicks;
            for (int i = 0; i < remaining; ++i) Grid.AdvanceGridTick();
            Assert.That(work.ProgressTicks, Is.EqualTo(work.RequiredTicks));
            Assert.That(work.State, Is.EqualTo(GridWorkState.Working)); Assert.That(Grid.Item(input), Is.Not.Null);
            Assert.That(work.CompletionQueued, Is.True); Assert.That(work.OutputItemId, Is.Zero);
            long serial = work.TransitionSerial;
            PumpMessages(8);
            Assert.That(work.State, Is.EqualTo(GridWorkState.Working), "Delivery only enqueues the transition");
            Grid.AdvanceGridTick(); Grid.CommitGridState(); PumpMessages(8);
            Assert.That(work.State, Is.EqualTo(GridWorkState.Completed)); Assert.That(Grid.Item(input), Is.Null);
            Assert.That(work.OutputItemId, Is.Not.EqualTo(input));
            int items = Grid.Items.Count;
            var replay = (KitchenWorkTransition)TransitionNotice(nameof(KitchenWorkTransition), work); replay.TransitionSerial = serial;
            ((ProcessInnerSender)host.Sender).Send(((HostSessionComponent)host.Authority).GetActorId(), replay);
            PumpMessages(8); AdvanceTicks(3); PumpMessages(8);
            Assert.That(Grid.Items.Count, Is.EqualTo(items));
            Assert.That(KitchenOutcomeTestObserver.Received.Count(value => value.WorkId == work.Id && value.Stage == KitchenWorkStage.Finished), Is.EqualTo(1));
        }

        [Test]
        public void ProductMessageBoundaryManualStopResumeKeepsProgressAndInvalidatesOldCompletion()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            GridManualProcess work = BoundaryManual(client, false);
            for (int i = work.ProgressTicks; i < work.RequiredTicks; ++i) Grid.AdvanceGridTick();
            var stop = client.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.StopWork,
                expected: work.InputItemId, expectedTarget: work.TargetItemId, process: work.Id);
            using (var reply = (GridCommandResponse)FinishMessages(client.CallProduct(stop), "Stop before completion commit")) Assert.That(reply.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(work.State, Is.EqualTo(GridWorkState.Paused)); Assert.That(work.OutputItemId, Is.Zero);
            int progress = work.ProgressTicks; long id = work.Id;
            LevelDo(LevelControlAction.Pause); AdvanceTicks(8);
            Assert.That(work.ProgressTicks, Is.EqualTo(progress)); Assert.That(work.OutputItemId, Is.Zero);
            LevelDo(LevelControlAction.Resume);
            PrepDo(client, GridAction.StartStationWork, work.StationId, work.InputItemId, work.TargetItemId, work.Id);
            AdvanceTicks(3); PumpMessages(8);
            Assert.That(work.Id, Is.EqualTo(id)); Assert.That(work.State, Is.EqualTo(GridWorkState.Completed));
            Assert.That(KitchenOutcomeTestObserver.Received.Any(value => value.WorkId == id && value.Stage == KitchenWorkStage.Paused), Is.True);
            Assert.That(KitchenOutcomeTestObserver.Received.Any(value => value.WorkId == id && value.Stage == KitchenWorkStage.Resumed), Is.True);
            Assert.That(KitchenOutcomeTestObserver.Received.Count(value => value.WorkId == id && value.Stage == KitchenWorkStage.Finished), Is.EqualTo(1));
        }

        [TestCase(KitchenProcessKind.Soup)]
        [TestCase(KitchenProcessKind.Bake)]
        public void ProductMessageBoundaryAutomaticDeadlineWaitsForQueueAndCompletesOnce(KitchenProcessKind kind)
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            Entity source; int remaining; long serial;
            if (kind == KitchenProcessKind.Soup)
            {
                var soup = SoupBegin(client, SoupFillPot(client)); source = soup; remaining = soup.RequiredTicks - soup.ProgressTicks; serial = soup.TransitionSerial;
            }
            else
            {
                var bake = DishBegin(client); source = bake; remaining = bake.RequiredTicks - bake.ProgressTicks; serial = bake.TransitionSerial;
            }
            int items = Grid.Items.Count;
            for (int i = 0; i < remaining; ++i) Grid.AdvanceGridTick();
            Assert.That(Grid.Items.Count, Is.EqualTo(items), "No automatic completion before delivery");
            if (source is GridAutoProcess waitingSoup) Assert.That(waitingSoup.State, Is.EqualTo(GridAutoState.Cooking));
            if (source is GridBakeProcess waitingBake) Assert.That(waitingBake.OutputItemId, Is.Zero);
            PumpMessages(8);
            Grid.AdvanceGridTick(); Grid.CommitGridState(); PumpMessages(8);
            if (source is GridAutoProcess completedSoup) Assert.That(completedSoup.RemainingPortions, Is.EqualTo(completedSoup.TotalPortions));
            if (source is GridBakeProcess completedBake) Assert.That(completedBake.State, Is.EqualTo(GridBakeState.Completed));
            var replay = (KitchenWorkTransition)TransitionNotice(nameof(KitchenWorkTransition), source);
            replay.ProcessKind = (int)kind; replay.TransitionSerial = serial;
            ((ProcessInnerSender)host.Sender).Send(((HostSessionComponent)host.Authority).GetActorId(), replay);
            PumpMessages(8); AdvanceTicks(3); PumpMessages(8);
            Assert.That(KitchenOutcomeTestObserver.Received.Count(value => value.WorkId == source.Id && value.Stage == KitchenWorkStage.Finished), Is.EqualTo(1));
        }

        [Test]
        public void ProductMessageBoundarySoupRetainsOwnIdentityAcrossLevelAndPausePendingCompletion()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            long pot = SoupFillPot(client);
            LevelDo(LevelControlAction.Start); PrepDo(client, GridAction.Move, cell: new GridCell(1, 9));
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks - 5);
            var soup = SoupBegin(client, pot); long id = soup.Id;
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks + 2); PumpMessages(8);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed));
            FrontWait(() => Renovation.Prepared, 8); RenovationChoose(Renovation.Candidates.First()); LevelDo(LevelControlAction.Continue);
            Assert.That(Grid.AutoProcess(id), Is.SameAs(soup));
            for (int i = soup.ProgressTicks; i < soup.RequiredTicks; ++i) Grid.AdvanceGridTick();
            LevelDo(LevelControlAction.Pause); AdvanceTicks(5);
            Assert.That(soup.State, Is.EqualTo(GridAutoState.Cooking));
            Assert.That(soup.ProgressTicks, Is.EqualTo(soup.RequiredTicks));
            LevelDo(LevelControlAction.Resume); AdvanceTicks(1); PumpMessages(8);
            Assert.That(soup.State, Is.EqualTo(GridAutoState.Ready));
            Assert.That(Grid.Item(pot).AutoProcessId, Is.EqualTo(id));
            Assert.That(KitchenOutcomeTestObserver.Received.Count(value => value.WorkId == id && value.Stage == KitchenWorkStage.Finished), Is.EqualTo(1));
        }

        [Test]
        public void ProductMessageBoundaryShipmentDeadlineWaitsForDeliveryAndReadyReplayCannotMintCargo()
        {
            StartSolo(); SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id));
            var shipment = Supply.Children.Values.OfType<SupplyShipment>().Single();
            int items = Grid.Items.Count; long serial = shipment.TransitionSerial;
            while (shipment.ProgressTicks < shipment.RequiredTicks) Grid.AdvanceGridTick();
            Grid.CommitGridState();
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.InTransit));
            Assert.That(shipment.ReadyQueued, Is.True); Assert.That(Grid.Items.Count, Is.EqualTo(items));
            Grid.AdvanceGridTick(); Assert.That(shipment.ProgressTicks, Is.EqualTo(shipment.RequiredTicks));
            PumpMessages(8); Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.InTransit));
            Grid.AdvanceGridTick(); Grid.CommitGridState(); PumpMessages(8);
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Ready)); Assert.That(Grid.Items.Count, Is.EqualTo(items));
            var replay = (ShipmentTransition)TransitionNotice(nameof(ShipmentTransition), shipment); replay.TransitionSerial = serial;
            ((ProcessInnerSender)host.Sender).Send(((HostSessionComponent)host.Authority).GetActorId(), replay);
            PumpMessages(8); AdvanceTicks(2); PumpMessages(8);
            var outcomes = ShipmentOutcomeTestObserver.Received.Where(value => value.Value.ShipmentId == shipment.Id).ToArray();
            Assert.That(outcomes.Select(value => value.Value.Stage), Is.EqualTo(new[] { ShipmentStage.InTransit, ShipmentStage.Ready }));
            Assert.That(outcomes.All(value => value.Installed == value.Value.Stage), Is.True, "Snapshot installed before each stage");
            Assert.That(Grid.Items.Count, Is.EqualTo(items));
            Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4));
        }

        [Test]
        public void ProductMessageBoundaryShipmentOriginalWorldSurvivesLevelAndPausedReadyDelivery()
        {
            StartSolo(); LevelDo(LevelControlAction.Start);
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks - 5);
            SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id));
            var shipment = Supply.Children.Values.OfType<SupplyShipment>().Single(); long id = shipment.Id;
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks + 2);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed));
            FrontWait(() => Renovation.Prepared, 8); RenovationChoose(Renovation.Candidates.First()); LevelDo(LevelControlAction.Continue);
            Assert.That(Supply.GetChild<SupplyShipment>(id), Is.SameAs(shipment));
            while (shipment.ProgressTicks < shipment.RequiredTicks) Grid.AdvanceGridTick();
            LevelDo(LevelControlAction.Pause); AdvanceTicks(5); PumpMessages(8);
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.InTransit));
            LevelDo(LevelControlAction.Resume); AdvanceTicks(1); PumpMessages(8);
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Ready));
            Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4));
            Assert.That(ShipmentOutcomeTestObserver.Received.Count(value => value.Value.ShipmentId == id && value.Value.Stage == ShipmentStage.Ready), Is.EqualTo(1));
        }

        [Test]
        public void ProductMessageBoundarySupplyReceivePortionAndRejectionKeepSupplyTerminalAndOriginalItems()
        {
            StartSolo(); var shipment = SupplyOrderReady(TomatoSupply.Id);
            long receive = SupplyDo(ReceiveIntent(shipment)); GridStation station = Grid.Station(shipment.ReceivingStationId);
            long package = shipment.PackageItemId; long[] contents = Grid.Item(package).Contents.ToArray();
            long portion = SupplyDo(PortionIntent(station, package, contents[0]));
            long rejected = SupplyDo(PortionIntent(station, package, contents[1]), "HandFull");
            var values = ProductOutcomeTestObserver.Received.Where(value => new[] { receive, portion, rejected }.Contains(value.Value.Context.CommandId)).ToArray();
            Assert.That(values.Select(value => value.Value.Kind), Is.EqualTo(Enumerable.Repeat(ProductCommandKind.Supply, 3)));
            Assert.That(values.Select(value => value.Value.Status), Is.EqualTo(new[] { GridResultStatus.Executed, GridResultStatus.Executed, GridResultStatus.Rejected }));
            Assert.That(values.Last().Value.Reason, Is.EqualTo("HandFull"));
            Assert.That(Grid.Actor(SupplyView.MemberId).HandItemId, Is.EqualTo(contents[0]));
            Assert.That(Grid.Item(package).Contents, Is.EqualTo(new[] { contents[1] }));
            Assert.That(ShipmentOutcomeTestObserver.Received.Last().Value.Stage, Is.EqualTo(ShipmentStage.Received));
        }

        [Test]
        public void ProductMessageBoundaryReplacingSupplyApproachCancelsSupplyWithoutReceiving()
        {
            StartSolo(); ClientSessionComponent client = host.Client; var shipment = SupplyOrderReady(TomatoSupply.Id);
            PrepDo(client, GridAction.Move, cell: new GridCell(1, 16));
            var request = SupplyView.CreateSupplyCommand(ReceiveIntent(shipment)); long old = request.CommandId;
            using (var reply = SupplyReply(request)) Assert.That(reply.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(Grid.Actor(client.MemberId).HasIntent, Is.True);
            PrepDo(client, GridAction.Move, cell: new GridCell(1, 16)); PumpMessages(8);
            var outcome = ProductOutcomeTestObserver.Received.Single(value => value.Value.Context.CommandId == old);
            Assert.That(outcome.Value.Kind, Is.EqualTo(ProductCommandKind.Supply));
            Assert.That(outcome.Value.Status, Is.EqualTo(GridResultStatus.Cancelled));
            Assert.That(outcome.Value.Reason, Is.EqualTo("Replaced"));
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Ready)); Assert.That(shipment.PackageItemId, Is.Zero);
        }

        [Test]
        public void ProductMessageBoundaryCleanupHasThreeExplicitGridTerminalsAndKeepsOriginalContainers()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            GridStation bowlCounter = PrepStation("碗台"); long bowl = bowlCounter.ItemId;
            PrepDo(client, GridAction.TakeCounter, bowlCounter.Id, bowl);
            long fill = CleanupDo(client, GridAction.FillWater, CleanupFacility(GridStationKind.Wash));
            long water = Grid.Item(bowl).Contents.Single();
            long drain = CleanupDo(client, GridAction.DrainLiquid, CleanupFacility(GridStationKind.Wash));
            Assert.That(Grid.Item(water), Is.Null); Assert.That(Grid.Item(bowl).Contents, Is.Empty);
            PrepDo(client, GridAction.PlaceCounter, bowlCounter.Id, bowl);
            GridStation tomatoes = PrepStation("备料台"); long tomato = tomatoes.ItemId;
            PrepDo(client, GridAction.TakeCounter, tomatoes.Id, tomato);
            long discard = CleanupDo(client, GridAction.DiscardSolid, CleanupFacility(GridStationKind.Trash)); PumpMessages(8);
            var values = ProductOutcomeTestObserver.Received.Where(value => new[] { fill, drain, discard }.Contains(value.Value.Context.CommandId) && value.Value.Status == GridResultStatus.Executed).ToArray();
            Assert.That(values.Select(value => value.Value.Context.CommandId), Is.EqualTo(new[] { fill, drain, discard }));
            Assert.That(values.All(value => value.Value.Kind == ProductCommandKind.Grid), Is.True);
            Assert.That(Grid.Item(tomato), Is.Null); Assert.That(Grid.Station(bowlCounter.Id).ItemId, Is.EqualTo(bowl));
        }

        private void BoundaryFrontWithoutDeliveryUntil(Func<bool> condition, int limit = 500)
        {
            for (int step = 0; step < limit && !condition(); ++step) Restaurant.AdvanceRestaurantStep();
            Assert.That(condition(), Is.True, "Front domain without delivery did not reach its pending boundary");
        }

        [Test]
        public void ProductMessageBoundaryFrontCustomerTransitionsAndAskSuccessorWaitForTheirOwnDelivery()
        {
            ConfigureFrontFlow(interval: 100000); StartSolo(liveFront: true); FrontRole.NextScheduleTick = 100000;
            LevelDo(LevelControlAction.Start); FrontLevelState state = CurrentLevel.GetComponent<FrontLevelState>();
            BoundaryFrontWithoutDeliveryUntil(() => state.ArrivalQueued);
            Assert.That(CurrentLevel.Children.Values.OfType<Customer>(), Is.Empty);
            PumpMessages(8); Assert.That(CurrentLevel.Children.Values.OfType<Customer>(), Is.Empty);
            Restaurant.AdvanceRestaurantStep(); Customer customer = CurrentLevel.Children.Values.OfType<Customer>().Single();
            Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.Entering));
            BoundaryFrontWithoutDeliveryUntil(() => customer.TransitionQueued);
            Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.Entering)); Assert.That(customer.TableStationId, Is.Zero);
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.Queued)); Assert.That(customer.TableStationId, Is.Zero);
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.WalkingToSeat));
            BoundaryFrontWithoutDeliveryUntil(() => customer.TransitionQueued);
            Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.WalkingToSeat));
            Assert.That(customer.Cell, Is.EqualTo(Grid.Station(customer.TableStationId).FrontSeat())); Assert.That(customer.AskWorkId, Is.Zero);
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.WaitingAsk)); Assert.That(customer.SuccessorPending, Is.True); Assert.That(customer.AskWorkId, Is.Zero);
            long completedSerial = customer.TransitionSerial;
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            Assert.That(customer.SuccessorQueued, Is.True); Assert.That(customer.AskWorkId, Is.Zero);
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            Assert.That(customer.AskWorkId, Is.Not.Zero); Assert.That(customer.SuccessorPending, Is.False); Assert.That(customer.OrderId, Is.Zero);
            var replay = (FrontWorkCreateRequest)TransitionNotice(nameof(FrontWorkCreateRequest), customer); replay.TransitionSerial = completedSerial;
            ((ProcessInnerSender)host.Sender).Send(((HostSessionComponent)host.Authority).GetActorId(), replay);
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            Assert.That(CurrentLevel.Children.Values.OfType<FrontWork>().Count(value => value.CustomerId == customer.Id), Is.EqualTo(1));
            FrontWork ask = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId);
            FrontPlayerDo(FrontWorkAction.Claim, ask); FrontWait(() => ask.State == FrontWorkState.Completed);
            Order order = CurrentLevel.GetChild<Order>(customer.OrderId);
            BoundaryFrontWithoutDeliveryUntil(() => order.TransitionQueued);
            Assert.That(order.State, Is.EqualTo(OrderState.Waiting)); Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.Ordered));
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            Assert.That(order.State, Is.EqualTo(OrderState.Expired)); Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.Leaving));
            BoundaryFrontWithoutDeliveryUntil(() => customer.TransitionQueued);
            Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.Leaving)); Assert.That(customer.BlocksLevelCompletion, Is.True);
            Assert.That(customer.Cell, Is.EqualTo(GridStationConfig.Entrance()));
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.Gone)); Assert.That(CurrentLevel.Score, Is.Zero);
        }

        [Test]
        public void ProductMessageBoundaryFrontWorkAtPositionAndDeadlineWaitsAndStaleClaimCannotFinish()
        {
            ConfigureFrontFlow(interval: 100000); StartSolo(liveFront: true); FrontRole.NextScheduleTick = 100000; LevelDo(LevelControlAction.Start);
            FrontWait(() => CurrentLevel.Children.Values.OfType<Customer>().Any(value => value.AskWorkId != 0));
            Customer customer = CurrentLevel.Children.Values.OfType<Customer>().Single(); FrontWork ask = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId);
            FrontPlayerDo(FrontWorkAction.Claim, ask);
            BoundaryFrontWithoutDeliveryUntil(() => ask.TransitionQueued);
            Assert.That(ask.State, Is.EqualTo(FrontWorkState.Approaching)); Assert.That(ask.Progress, Is.Zero);
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            Assert.That(ask.State, Is.EqualTo(FrontWorkState.Working));
            BoundaryFrontWithoutDeliveryUntil(() => ask.Progress >= 1 - 1e-9);
            Assert.That(ask.TransitionQueued, Is.True); Assert.That(customer.OrderId, Is.Zero);
            long originalSerial = ask.TransitionSerial, originalClaim = ask.ClaimSerial;
            using (var stopped = FrontPlayerReply(SupplyView.CreateFrontWorkCommand(FrontWorkAction.Stop, ask.Id, originalClaim))) Assert.That(stopped.Error, Is.Zero);
            Restaurant.AdvanceRestaurantStep();
            Assert.That(ask.State, Is.EqualTo(FrontWorkState.Available)); Assert.That(customer.OrderId, Is.Zero);
            FrontPlayerDo(FrontWorkAction.Claim, ask);
            var stale = (FrontWorkTransition)TransitionNotice(nameof(FrontWorkTransition), ask);
            stale.TransitionSerial = originalSerial; stale.ClaimSerial = originalClaim; stale.Action = (int)FrontWorkTransitionAction.Complete;
            ((ProcessInnerSender)host.Sender).Send(((HostSessionComponent)host.Authority).GetActorId(), stale); PumpMessages(8);
            Assert.That(customer.OrderId, Is.Zero); FrontWait(() => ask.State == FrontWorkState.Completed); PumpMessages(8);
            Assert.That(CurrentLevel.Children.Values.OfType<Order>().Count(value => value.CustomerId == customer.Id), Is.EqualTo(1));
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty, "Player completion does not grow companion");
            Assert.That(SupplyView.LastFrontWorkOutcome.Stage, Is.EqualTo(FrontWorkStage.Finished));
        }

        [Test]
        public void ProductMessageBoundaryFrontExitClearWashPendingGapsKeepNaturalLevelOpenAndOriginalBowl()
        {
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(1), duration: 120));
            ConfigureFrontFlow(interval: 100000); StartSolo(liveFront: true); long bowl = PrepareServingMeal(1);
            LevelDo(LevelControlAction.Start); Order order = WaitActualOrder(1); Customer customer = CurrentLevel.GetChild<Customer>(order.CustomerId);
            ServeDo(order, bowl); Assert.That(CurrentLevel.Score, Is.EqualTo(10));
            FrontWait(() => customer.Phase == CustomerPhase.Leaving);
            BoundaryFrontWithoutDeliveryUntil(() => customer.TransitionQueued);
            Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.Leaving));
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.Gone)); Assert.That(customer.SuccessorPending, Is.True);
            Assert.That(CurrentLevel.Children.Values.OfType<FrontWork>().Any(value => value.Kind == FrontWorkKind.Clear), Is.False);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Draining));
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            Assert.That(customer.SuccessorQueued, Is.True); Assert.That(customer.SuccessorPending, Is.True);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Draining));
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            FrontWork clear = CurrentLevel.Children.Values.OfType<FrontWork>().Single(value => value.Kind == FrontWorkKind.Clear);
            Assert.That(clear.ItemId, Is.EqualTo(bowl)); Assert.That(customer.SuccessorPending, Is.False);
            FrontPlayerDo(FrontWorkAction.Claim, clear); FrontWait(() => clear.State == FrontWorkState.Working);
            BoundaryFrontWithoutDeliveryUntil(() => clear.Progress >= 1 - 1e-9);
            Assert.That(Grid.Item(bowl).Location, Is.EqualTo(GridItemLocation.Table));
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            Assert.That(clear.State, Is.EqualTo(FrontWorkState.Completed)); Assert.That(clear.SuccessorPending, Is.True);
            Assert.That(Grid.Item(bowl).Location, Is.EqualTo(GridItemLocation.DirtyPool)); Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Draining));
            Assert.That(CurrentLevel.Children.Values.OfType<FrontWork>().Any(value => value.Kind == FrontWorkKind.Wash), Is.False);
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            Assert.That(clear.SuccessorQueued, Is.True); Assert.That(clear.SuccessorPending, Is.True); Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Draining));
            PumpMessages(8); Restaurant.AdvanceRestaurantStep();
            FrontWork wash = CurrentLevel.Children.Values.OfType<FrontWork>().Single(value => value.Kind == FrontWorkKind.Wash);
            Assert.That(wash.ItemId, Is.EqualTo(bowl)); Assert.That(clear.SuccessorPending, Is.False);
            FrontWait(() => wash.State == FrontWorkState.Completed); FrontWait(() => CurrentLevel.Phase == LevelPhase.Completed);
            Assert.That(Grid.Item(bowl).Location, Is.EqualTo(GridItemLocation.CleanPool)); Assert.That(Grid.Item(bowl).Hygiene, Is.EqualTo(ItemHygiene.Clean));
            Assert.That(CurrentLevel.Score, Is.EqualTo(10)); Assert.That(CurrentLevel.Stars, Is.EqualTo(1));
        }

        [Test]
        public void ProductMessageBoundaryBuffPreparationGatesNewLevelAndReportsPreparedAfterSnapshotOnce()
        {
            StartSolo(liveBuff: true); BuffDo(Buff.Candidates[0]); RenovationComplete(); long continuation = ContinueBeforePreparation();
            LevelBuff buff = Buff; Assert.That(buff.Prepared, Is.False); Assert.That(buff.Candidates, Is.Empty); Assert.That(SupplyView.BuffDrawVersion, Is.Zero);
            int seed = buff.Seed; long serial = buff.PreparationSerial;
            var request = SupplyView.CreateLevelCommand(LevelControlAction.Start); long command = request.CommandId;
            using (var receipt = (LevelCommandResponse)FinishMessages(((ClientSessionComponent)host.Client).CallProduct(request), "premature start receipt")) Assert.That(receipt.Error, Is.Zero);
            Restaurant.AdvanceRestaurantStep();
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing)); Assert.That(buff.Prepared, Is.False);
            Assert.That(FrontPeer.LastCommandReason, Is.EqualTo("BuffNotReady"));
            PumpMessages(8);
            Assert.That(buff.Prepared, Is.False, "Delivery does not prepare candidates before business step");
            Restaurant.AdvanceRestaurantStep(); PumpMessages(8);
            Assert.That(buff.Prepared, Is.True); Assert.That(buff.Seed, Is.EqualTo(seed));
            Assert.That(buff.Candidates, Is.EqualTo(BuffConfig.Draw(buff.AllowedIds, buff.CandidateCount, seed)));
            Assert.That(SupplyView.LastBuffPrepared.LevelId, Is.EqualTo(CurrentLevel.Id));
            Assert.That(SupplyView.LastBuffPrepared.DrawVersion, Is.EqualTo(SupplyView.BuffDrawVersion));
            Assert.That(ProductOutcomeTestObserver.Received.Single(value => value.Value.Context.CommandId == continuation).Value.Status, Is.EqualTo(GridResultStatus.Executed));
            Assert.That(ProductOutcomeTestObserver.Received.Single(value => value.Value.Context.CommandId == continuation).Value.ChangedLevel, Is.True);
            long outcomeSerial = SupplyView.LastBuffPreparedSerial; int[] candidates = buff.Candidates.ToArray();
            var replay = (BuffPrepare)TransitionNotice(nameof(BuffPrepare), buff); replay.TransitionSerial = serial;
            ((ProcessInnerSender)host.Sender).Send(((HostSessionComponent)host.Authority).GetActorId(), replay); PumpMessages(8);
            Restaurant.AdvanceRestaurantStep(); PumpMessages(8);
            Assert.That(buff.Candidates, Is.EqualTo(candidates)); Assert.That(SupplyView.LastBuffPreparedSerial, Is.EqualTo(outcomeSerial));
            Assert.That(ProductOutcomeTestObserver.Received.Single(value => value.Value.Context.CommandId == command).Value.Reason, Is.EqualTo("BuffNotReady"));
            BuffDo(buff.Candidates[0]); LevelDo(LevelControlAction.Start); Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Operating));
        }

        [Test]
        public void ProductMessageBoundaryBuffAndLayoutSuccessAndRefusalEachHaveExplicitTerminal()
        {
            StartSolo(liveBuff: true); int choice = Buff.Candidates[0];
            ProductOutcomeTestObserver.Received.Clear();
            BuffDo(999, "InvalidBuffChoice"); BuffDo(choice);
            GridStation station = PrepStation("备料台"); long item = station.ItemId;
            LayoutDo(station, new GridCell(0, 12), rejection: "LayoutBlocked"); LayoutDo(station, station.Cell);
            var values = ProductOutcomeTestObserver.Received.Select(value => value.Value).ToArray();
            Assert.That(values.Select(value => value.Kind), Is.EqualTo(new[] { ProductCommandKind.Buff, ProductCommandKind.Buff, ProductCommandKind.Layout, ProductCommandKind.Layout }));
            Assert.That(values.Select(value => value.Status), Is.EqualTo(new[] { GridResultStatus.Rejected, GridResultStatus.Executed, GridResultStatus.Rejected, GridResultStatus.Executed }));
            Assert.That(values.Select(value => value.Context.CommandId).Distinct().Count(), Is.EqualTo(4));
            Assert.That(values[0].Reason, Is.EqualTo("InvalidBuffChoice")); Assert.That(values[2].Reason, Is.EqualTo("LayoutBlocked"));
            Assert.That(Buff.SelectedId, Is.EqualTo(choice)); Assert.That(station.ItemId, Is.EqualTo(item)); Assert.That(Grid.Item(item).HolderId, Is.EqualTo(station.Id));
        }

        [Test]
        public void ProductMessageBoundaryRenovationCompletionAndPrepareNeedSeparateDeliveryAndContinueInstallsOnce()
        {
            StartSolo(); LevelDo(LevelControlAction.Start);
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Operating));
            PumpMessages(8); Restaurant.AdvanceRestaurantStep(); PumpMessages(8);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Draining));
            Restaurant.AdvanceRestaurantStep();
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed)); Assert.That(Renovation.Prepared, Is.False); Assert.That(Renovation.Candidates, Is.Empty);
            var early = SupplyView.CreateLevelCommand(LevelControlAction.Continue);
            using (var receipt = (LevelCommandResponse)FinishMessages(((ClientSessionComponent)host.Client).CallProduct(early), "continue before preparation receipt")) Assert.That(receipt.Error, Is.Zero);
            Restaurant.AdvanceRestaurantStep();
            Assert.That(FrontPeer.LastCommandReason, Is.EqualTo("RenovationNotReady"));
            Assert.That(Renovation.PreparationQueued, Is.True); Assert.That(Renovation.Prepared, Is.False); Assert.That(Renovation.Candidates, Is.Empty);
            long serial = Renovation.PreparationSerial;
            PumpMessages(8); Assert.That(Renovation.Prepared, Is.False);
            Restaurant.AdvanceRestaurantStep(); PumpMessages(8);
            Assert.That(Renovation.Prepared, Is.True); Assert.That(Renovation.Candidates, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(SupplyView.LastRenovationPrepared.ChoiceVersion, Is.EqualTo(SupplyView.RenovationChoiceVersion));
            Assert.That(SupplyView.LastRenovationPrepared.LevelId, Is.EqualTo(CurrentLevel.Id));
            long notification = SupplyView.LastRenovationPreparedSerial, choiceVersion = Renovation.ChoiceVersion;
            var replayPrepare = (RenovationPrepare)TransitionNotice(nameof(RenovationPrepare), CurrentLevel); replayPrepare.TransitionSerial = serial;
            var replayFinished = (LevelFinished)TransitionNotice(nameof(LevelFinished), CurrentLevel); replayFinished.TransitionSerial = CurrentLevel.CompletionSerial;
            ((ProcessInnerSender)host.Sender).Send(((HostSessionComponent)host.Authority).GetActorId(), replayPrepare);
            ((ProcessInnerSender)host.Sender).Send(((HostSessionComponent)host.Authority).GetActorId(), replayFinished);
            PumpMessages(8); Restaurant.AdvanceRestaurantStep(); PumpMessages(8);
            Assert.That(Renovation.ChoiceVersion, Is.EqualTo(choiceVersion)); Assert.That(SupplyView.LastRenovationPreparedSerial, Is.EqualTo(notification));
            int stations = Grid.Stations.Count; RenovationChoose(1);
            var next = SupplyView.CreateLevelCommand(LevelControlAction.Continue); var repeatNext = CopyReplayRequest(next);
            using (var accepted = (LevelCommandResponse)FinishMessages(((ClientSessionComponent)host.Client).CallProduct(next), "continue install receipt")) Assert.That(accepted.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(Grid.Stations.Count, Is.EqualTo(stations + 1)); Assert.That(Renovation.AppliedStations.Keys, Is.EqualTo(new[] { 1 }));
            long newLevel = CurrentLevel.Id;
            FrontWait(() => Buff.Prepared, 8);
            using (var repeated = (LevelCommandResponse)FinishMessages(((ClientSessionComponent)host.Client).CallProduct(repeatNext), "continue replay")) Assert.That(repeated.Status, Is.EqualTo((int)GridResultStatus.Executed));
            Assert.That(CurrentLevel.Id, Is.EqualTo(newLevel)); Assert.That(Grid.Stations.Count, Is.EqualTo(stations + 1));
        }

        [Test]
        public void ProductMessageBoundaryLevelDeadlineAndCompletionWaitForDeliveryAndPauseFreezesQueuedTransitions()
        {
            StartSolo(); LevelDo(LevelControlAction.Start); Level level = CurrentLevel;
            Assert.That(SupplyView.LastLevelPhase.Phase, Is.EqualTo(LevelPhase.Operating));
            AdvanceTicks(level.OperatingTicks - level.ElapsedOperatingTicks - 1);
            Restaurant.AdvanceRestaurantStep(); long serial = level.TransitionSerial;
            Assert.That(level.ElapsedOperatingTicks, Is.EqualTo(level.OperatingTicks)); Assert.That(level.Phase, Is.EqualTo(LevelPhase.Operating));
            Assert.That(level.TransitionQueued, Is.True);
            Restaurant.AdvanceRestaurantStep(); Assert.That(level.ElapsedOperatingTicks, Is.EqualTo(level.OperatingTicks));
            using (var pause = (LevelCommandResponse)FinishMessages(((ClientSessionComponent)host.Client).CallProduct(SupplyView.CreateLevelCommand(LevelControlAction.Pause)), "pause pending drain")) Assert.That(pause.Error, Is.Zero);
            Restaurant.AdvanceRestaurantStep(); long tick = Grid.Tick;
            Assert.That(level.Paused, Is.True); Assert.That(level.Phase, Is.EqualTo(LevelPhase.Operating));
            Restaurant.AdvanceRestaurantStep(); Assert.That(Grid.Tick, Is.EqualTo(tick));
            using (var resume = (LevelCommandResponse)FinishMessages(((ClientSessionComponent)host.Client).CallProduct(SupplyView.CreateLevelCommand(LevelControlAction.Resume)), "resume drain")) Assert.That(resume.Error, Is.Zero);
            Restaurant.AdvanceRestaurantStep(); PumpMessages(8);
            Assert.That(level.Phase, Is.EqualTo(LevelPhase.Draining)); Assert.That(level.TransitionQueued, Is.True);
            Assert.That(SupplyView.LastLevelPhase.Phase, Is.EqualTo(LevelPhase.Draining));
            using (var pause = (LevelCommandResponse)FinishMessages(((ClientSessionComponent)host.Client).CallProduct(SupplyView.CreateLevelCommand(LevelControlAction.Pause)), "pause pending complete")) Assert.That(pause.Error, Is.Zero);
            Restaurant.AdvanceRestaurantStep(); tick = Grid.Tick;
            Assert.That(level.Phase, Is.EqualTo(LevelPhase.Draining)); Restaurant.AdvanceRestaurantStep(); Assert.That(Grid.Tick, Is.EqualTo(tick));
            using (var resume = (LevelCommandResponse)FinishMessages(((ClientSessionComponent)host.Client).CallProduct(SupplyView.CreateLevelCommand(LevelControlAction.Resume)), "resume complete")) Assert.That(resume.Error, Is.Zero);
            Restaurant.AdvanceRestaurantStep(); PumpMessages(8);
            Assert.That(level.Phase, Is.EqualTo(LevelPhase.Completed)); Assert.That(level.Stars, Is.Zero);
            Assert.That(SupplyView.LastLevelPhase.Phase, Is.EqualTo(LevelPhase.Completed)); Assert.That(SupplyView.LastLevelPhase.Paused, Is.False);
            long outcome = SupplyView.LastLevelPhaseSerial;
            var replay = (LevelTransition)TransitionNotice(nameof(LevelTransition), level); replay.TransitionSerial = serial;
            ((ProcessInnerSender)host.Sender).Send(((HostSessionComponent)host.Authority).GetActorId(), replay); PumpMessages(8);
            Restaurant.AdvanceRestaurantStep(); PumpMessages(8);
            Assert.That(level.Phase, Is.EqualTo(LevelPhase.Completed)); Assert.That(SupplyView.LastLevelPhaseSerial, Is.EqualTo(outcome));
        }

        [Test]
        public void ProductMessageBoundaryLevelContinueReadinessAndRetiredTransitionCannotChangeNewRound()
        {
            StartSolo(); RenovationComplete(); Level old = CurrentLevel;
            var stale = (LevelTransition)TransitionNotice(nameof(LevelTransition), old); stale.TransitionSerial = old.TransitionSerial; stale.Action = (int)LevelTransitionAction.Complete;
            ContinueBeforePreparation(); Level next = CurrentLevel;
            Assert.That(next.Phase, Is.EqualTo(LevelPhase.Preparing)); Assert.That(Buff.Prepared, Is.False);
            LevelDo(LevelControlAction.Start, "BuffNotReady"); FrontWait(() => Buff.Prepared, 8); LevelDo(LevelControlAction.Start);
            long generation = next.Generation; int elapsed = next.ElapsedOperatingTicks;
            ((ProcessInnerSender)host.Sender).Send(((HostSessionComponent)host.Authority).GetActorId(), stale); PumpMessages(8);
            Assert.That(Restaurant.Transitions, Is.Empty); Assert.That(CurrentLevel, Is.SameAs(next)); Assert.That(next.Generation, Is.EqualTo(generation));
            Assert.That(next.Phase, Is.EqualTo(LevelPhase.Operating)); Assert.That(next.ElapsedOperatingTicks, Is.EqualTo(elapsed));
        }

        private long ContinueBeforePreparation()
        {
            var request = SupplyView.CreateLevelCommand(LevelControlAction.Continue); long command = request.CommandId;
            var replay = CopyReplayRequest(request); long oldLevel = CurrentLevel.Id;
            using (var receipt = (LevelCommandResponse)FinishMessages(((ClientSessionComponent)host.Client).CallProduct(request), "continue receipt"))
                Assert.That(receipt.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(CurrentLevel.Id, Is.Not.EqualTo(oldLevel)); Assert.That(Buff.Prepared, Is.False);
            Assert.That(FrontPeer.LastCommandStatus, Is.EqualTo(GridResultStatus.Accepted));
            Assert.That(ProductOutcomeTestObserver.Received.Any(value => value.Value.Context.CommandId == command), Is.False);
            long installedLevel = CurrentLevel.Id;
            using (var receipt = (LevelCommandResponse)FinishMessages(((ClientSessionComponent)host.Client).CallProduct(replay), "continue pending replay"))
                Assert.That(receipt.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            Assert.That(CurrentLevel.Id, Is.EqualTo(installedLevel)); Assert.That(CurrentLevel.PreparationControl.HasValue, Is.True);
            return command;
        }
    }
}
#endif
