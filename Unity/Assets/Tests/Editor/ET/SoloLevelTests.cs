using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(Level))]
    public sealed partial class ProductSessionTests
    {
        private RestaurantRuntime Restaurant => ((Match)((HostSessionComponent)host.Authority).Match).Restaurant;
        private Level CurrentLevel => Restaurant.CurrentLevel;

        private LevelCommandResponse LevelReply(ClientSessionComponent client, LevelCommandRequest request)
        {
            var task = client.CallProduct(request); PumpMessages(8);
            Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending));
            return (LevelCommandResponse)task.GetAwaiter().GetResult();
        }

        private void LevelDo(LevelControlAction action, string rejection = null)
        {
            ClientSessionComponent client = host.Client;
            using (var reply = LevelReply(client, client.GetComponent<GridClientComponent>().CreateLevelCommand(action)))
                Assert.That(reply.Error, Is.Zero, reply.Message);
            AdvanceTicks(1); PumpMessages(8);
            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            Assert.That(peer.LastCommandStatus, Is.EqualTo(rejection == null ? GridResultStatus.Executed : GridResultStatus.Rejected), peer.LastCommandReason);
            Assert.That(peer.LastCommandReason, Is.EqualTo(rejection));
        }

        private static T CopyReplayRequest<T>(T request) where T : MessageObject, IRequest
        {
            // Capture ordinary payload before the original physical call disposes its input.
            var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
                .Where(property => property.CanRead && property.CanWrite && property.Name != "RpcId");
            var values = properties.Select(property => (property, value: property.GetValue(request))).ToArray();
            return ProductMessages.Create<T>(copy =>
            {
                foreach (var entry in values) entry.property.SetValue(copy, entry.value);
            });
        }

        [TestCase(ProductCommandKind.Level)]
        [TestCase(ProductCommandKind.Layout)]
        [TestCase(ProductCommandKind.FrontWork)]
        [TestCase(ProductCommandKind.Serve)]
        public void SoloLevelCarryExactOldReplayFromAnotherPeerRejectsCurrentLevel(ProductCommandKind kind)
        {
            StartSolo(); ClientSessionComponent first = host.Client, other = GridJoin();
            var projection = first.GetComponent<GridClientComponent>();
            if (kind == ProductCommandKind.FrontWork || kind == ProductCommandKind.Serve) LevelDo(LevelControlAction.Start);
            IRequest initial, replay;
            switch (kind)
            {
                case ProductCommandKind.Level:
                    var level = projection.CreateLevelCommand(LevelControlAction.Pause);
                    initial = level; replay = CopyReplayRequest(level); break;
                case ProductCommandKind.Layout:
                    GridStation oven = PrepStation("烤箱");
                    var layout = projection.CreateStationLayout(oven.Id, oven.Cell, 1);
                    initial = layout; replay = CopyReplayRequest(layout); break;
                case ProductCommandKind.FrontWork:
                    var work = projection.CreateFrontWorkCommand(FrontWorkAction.Claim, long.MaxValue);
                    initial = work; replay = CopyReplayRequest(work); break;
                default:
                    var serve = projection.CreateServeOrder(long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue);
                    initial = serve; replay = CopyReplayRequest(serve); break;
            }
            IResponse Reply(ClientSessionComponent client, IRequest request)
            {
                var task = client.CallProduct(request); PumpMessages(8);
                Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending)); return task.GetAwaiter().GetResult();
            }
            IResponse accepted = Reply(first, initial);
            using ((MessageObject)accepted) Assert.That(accepted.Error, Is.Zero, accepted.Message);
            AdvanceTicks(1); PumpMessages(8);
            HostPeer originalPeer = ((LocalMessageComponent)first.Local).Peer;
            ProductCommandContext oldContext = originalPeer.LastCommandContext;
            long oldLevel = CurrentLevel.Id;
            void OtherControl(LevelControlAction action)
            {
                using (var reply = LevelReply(other, other.GetComponent<GridClientComponent>().CreateLevelCommand(action)))
                    Assert.That(reply.Error, Is.Zero, reply.Message);
                AdvanceTicks(1); PumpMessages(8);
            }
            if (CurrentLevel.Paused) OtherControl(LevelControlAction.Resume);
            if (CurrentLevel.Phase == LevelPhase.Preparing) OtherControl(LevelControlAction.Start);
            AdvanceTicks(CurrentLevel.OperatingTicks + 2); PumpMessages(8);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed));
            OtherControl(LevelControlAction.Continue);
            Assert.That(CurrentLevel.Id, Is.Not.EqualTo(oldLevel));
            Assert.That(originalPeer.LastCommandContext.Equals(oldContext), Is.True);
            long current = CurrentLevel.Id, layoutVersion = Grid.LayoutVersion;
            long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            IResponse rejected = Reply(first, replay);
            using ((MessageObject)rejected)
            {
                Assert.That(rejected.Error, Is.Not.Zero);
                Assert.That(rejected.Message, Is.EqualTo("WrongLevel"));
            }
            Assert.That(CurrentLevel.Id, Is.EqualTo(current)); Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing));
            Assert.That(CurrentLevel.Score, Is.Zero); Assert.That(Grid.LayoutVersion, Is.EqualTo(layoutVersion));
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
        }

        private DRLevelConfig LevelDefinition(int id, int next, int x = 1, int y = 10, int ticks = 400)
        {
            var buffer = new Luban.ByteBuf(); buffer.WriteInt(id); buffer.WriteInt(next); buffer.WriteInt(ticks);
            buffer.WriteSize(3); buffer.WriteInt(10); buffer.WriteInt(20); buffer.WriteInt(30); buffer.WriteInt(x); buffer.WriteInt(y);
            buffer.WriteSize(2); buffer.WriteInt(1); buffer.WriteInt(2);
            buffer.WriteSize(0);buffer.WriteInt(0);buffer.WriteBool(false);
            return new DRLevelConfig(buffer);
        }

        private void UseOptionalBuffFixture()
        {
            var rows=(System.Collections.Generic.List<DRLevelConfig>)Tables.Instance.DTLevelConfig.DataList;
            var map=(System.Collections.Generic.Dictionary<int,DRLevelConfig>)Tables.Instance.DTLevelConfig.DataMap;
            for(int i=0;i<rows.Count;++i)
            {
                DRLevelConfig row=rows[i];var buffer=new Luban.ByteBuf();buffer.WriteInt(row.Id);buffer.WriteInt(row.NextLevelConfigId);buffer.WriteInt(row.OperatingTicks);
                buffer.WriteSize(row.StarThresholds.Count);foreach(int star in row.StarThresholds)buffer.WriteInt(star);
                buffer.WriteInt(row.SpawnX);buffer.WriteInt(row.SpawnY);buffer.WriteSize(row.AllowedDishIds.Count);foreach(int dish in row.AllowedDishIds)buffer.WriteInt(dish);
                buffer.WriteSize(row.AllowedBuffIds.Count);foreach(int buff in row.AllowedBuffIds)buffer.WriteInt(buff);
                buffer.WriteInt(row.CandidateCount);buffer.WriteBool(false);rows[i]=new DRLevelConfig(buffer);map[row.Id]=rows[i];
            }
        }

        [Test]
        public void SoloLevelConfigRejectsBeforeCreatingWorld()
        {
            var actual = Tables.Instance.DTLevelConfig.DataList;
            RestaurantConfig.ValidateLevelDefinitions(actual, GridKitchenSystem.IsInitialGridWalkable);
            Assert.Throws<InvalidOperationException>(() => RestaurantConfig.ValidateLevelDefinitions(actual.Where(row => row.Id != 2), GridKitchenSystem.IsInitialGridWalkable));
            Assert.Throws<InvalidOperationException>(() => RestaurantConfig.ValidateLevelDefinitions(new[] { LevelDefinition(1, 2, 1, 11), actual.Single(row => row.Id == 2) }, GridKitchenSystem.IsInitialGridWalkable));
            Assert.That(host, Is.Null);
            Assert.That(fibers.Count, Is.Zero);
        }

        [Test]
        public void SoloLevelOwnershipUsesPersistentKitchenAndLevelRoles()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            Match match = ((HostSessionComponent)host.Authority).Match;
            Assert.That(match.GetComponent<GridKitchenComponent>(), Is.Null);
            Assert.That(Restaurant.Parent, Is.SameAs(match));
            Assert.That(Grid.Parent, Is.SameAs(Restaurant));
            Assert.That(CurrentLevel.Parent, Is.SameAs(Restaurant));
            Assert.That(Grid.Actor(client.MemberId).Parent, Is.SameAs(CurrentLevel));
            Assert.That(Grid.Children.Values.OfType<GridActor>(), Is.Empty);
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(1));
            Assert.That(client.Roster.Count, Is.EqualTo(1));
            GridKitchenComponent kitchen = Grid; RestaurantRuntime restaurant = Restaurant; Level level = CurrentLevel;
            host.StopProduct();
            Assert.That(kitchen.IsDisposed && restaurant.IsDisposed && level.IsDisposed, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SoloLevelConfigActualBootstrapLeavesNoHalfRestaurant(bool badSpawn)
        {
            var rows = (System.Collections.Generic.List<DRLevelConfig>)Tables.Instance.DTLevelConfig.DataList;
            var original = rows.ToArray();
            try
            {
                if (badSpawn) rows[0] = LevelDefinition(1, 2, 1, 11);
                else rows.RemoveAll(row => row.Id == 2);
                Assert.Throws<InvalidOperationException>(() => StartSolo());
                Assert.That((Scene)host.HostScene, Is.Null);
                Assert.That((HostSessionComponent)host.Authority, Is.Null);
                Assert.That(host.Stage, Is.EqualTo(ProductStage.Stopped));
            }
            finally { rows.Clear(); rows.AddRange(original); }
        }

        [Test]
        public void SoloLevelControlHasOneBusinessClockAndResumeUsesLocalMailbox()
        {
            StartSolo(); ClientSessionComponent client = host.Client; var projection = client.GetComponent<GridClientComponent>();
            long tick = Grid.Tick;
            using (var receipt = LevelReply(client, projection.CreateLevelCommand(LevelControlAction.Start)))
                Assert.That(receipt.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing));
            PumpMessages(20); Assert.That(Grid.Tick, Is.EqualTo(tick));
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(Grid.Tick, Is.EqualTo(tick + 1));
            Assert.That(CurrentLevel.ElapsedOperatingTicks, Is.EqualTo(1));
            Assert.That(projection.LevelPhase, Is.EqualTo(LevelPhase.Operating));
            LevelDo(LevelControlAction.Pause);
            tick = Grid.Tick; int elapsed = CurrentLevel.ElapsedOperatingTicks;
            AdvanceTicks(25); PumpMessages(10);
            Assert.That(Grid.Tick, Is.EqualTo(tick)); Assert.That(CurrentLevel.ElapsedOperatingTicks, Is.EqualTo(elapsed));
            using (var reply = SoloReply(client, projection.CreateGridCommand(GridAction.Move, new GridCell(2, 9))))
                Assert.That(reply.Message, Is.EqualTo("Paused"));
            LevelDo(LevelControlAction.Resume);
            Assert.That(Grid.Tick, Is.EqualTo(tick + 1));
            Assert.That(CurrentLevel.ElapsedOperatingTicks, Is.EqualTo(elapsed + 1));
            Assert.That(projection.LevelPaused, Is.False);
        }

        [Test]
        public void SoloLevelPauseCancelsFuturePathAndPendingIntentButKeepsCurrentStep()
        {
            StartSolo(); ClientSessionComponent client = host.Client; var projection = client.GetComponent<GridClientComponent>();
            GridActor actor = Grid.Actor(client.MemberId);
            using (var receipt = SoloReply(client, projection.CreateGridCommand(GridAction.Move, new GridCell(4, 10)))) Assert.That(receipt.Error, Is.Zero);
            AdvanceTicks(2); PumpMessages(5);
            Assert.That(actor.Motion, Is.EqualTo(GridMotion.Transit));
            GridCell cell = actor.Cell, next = actor.NextCell; long started = actor.StepStartTick, tick = Grid.Tick;
            LevelDo(LevelControlAction.Pause);
            Assert.That(actor.HasIntent, Is.False); Assert.That(actor.Path, Is.Empty);
            Assert.That(actor.Cell, Is.EqualTo(cell)); Assert.That(actor.NextCell, Is.EqualTo(next)); Assert.That(actor.StepStartTick, Is.EqualTo(started));
            AdvanceTicks(30); Assert.That(Grid.Tick, Is.EqualTo(tick));
            LevelDo(LevelControlAction.Resume); AdvanceTicks(GridKitchenRules.StepTicks); PumpMessages(5);
            Assert.That(actor.Cell, Is.EqualTo(next)); Assert.That(actor.Motion, Is.EqualTo(GridMotion.Standing));
            Assert.That(actor.Cell, Is.Not.EqualTo(new GridCell(4, 10)));
            GridStation source = PrepStation("备料台"); long item = source.ItemId;
            using (var receipt = SoloReply(client, projection.CreateGridCommand(GridAction.TakeCounter, target: source.Id, expected: item))) Assert.That(receipt.Error, Is.Zero);
            LevelDo(LevelControlAction.Pause);
            Assert.That(Grid.Incoming, Is.Empty); Assert.That(source.ItemId, Is.EqualTo(item));
            LevelDo(LevelControlAction.Resume); AdvanceTicks(20); PumpMessages(5);
            Assert.That(source.ItemId, Is.EqualTo(item)); Assert.That(actor.HandItemId, Is.Zero);
        }

        [Test]
        public void SoloLevelPauseFreezesStartedManualWorkAndRetainsClaim()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            GridStation source = PrepStation("备料台"), chopping = PrepStation("切配台"); long tomato = source.ItemId;
            PrepDo(client, GridAction.TakeCounter, source.Id, tomato); PrepDo(client, GridAction.PlaceCounter, chopping.Id, tomato);
            PrepDo(client, GridAction.StartStationWork, chopping.Id, tomato, tomato);
            GridManualProcess work = Grid.InputProcess(tomato); AdvanceTicks(7); int progress = work.ProgressTicks;
            LevelDo(LevelControlAction.Pause); AdvanceTicks(40);
            Assert.That(work.ProgressTicks, Is.EqualTo(progress)); Assert.That(work.State, Is.EqualTo(GridWorkState.Working));
            Assert.That(work.WorkerMemberId, Is.EqualTo(client.MemberId)); Assert.That(Grid.Actor(client.MemberId).WorkId, Is.EqualTo(work.Id));
            LevelDo(LevelControlAction.Resume); Assert.That(work.ProgressTicks, Is.EqualTo(progress + 1));
        }

        [Test]
        public void SoloLevelPauseFreezesActualAutomaticBatch()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            long pot = SoupFillPot(client); GridAutoProcess batch = SoupBegin(client, pot);
            AdvanceTicks(7); int progress = batch.ProgressTicks; long[] inputs = batch.InputItemIds.ToArray();
            LevelDo(LevelControlAction.Pause); AdvanceTicks(40);
            Assert.That(batch.ProgressTicks, Is.EqualTo(progress)); Assert.That(Grid.Item(pot).Contents, Is.EqualTo(inputs));
            LevelDo(LevelControlAction.Resume); Assert.That(batch.ProgressTicks, Is.EqualTo(progress + 1));
        }

        [Test]
        public void SoloLevelCarryMissingNextPreservesEverythingAndOldProjectionCannotApply()
        {
            StartSolo(); ClientSessionComponent client = host.Client; var projection = client.GetComponent<GridClientComponent>();
            GridStation bowlStation = PrepStation("碗台"); long bowl = bowlStation.ItemId;
            PrepDo(client, GridAction.TakeCounter, bowlStation.Id, bowl);
            LevelDo(LevelControlAction.Start); AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks + 1); PumpMessages(8);
            Level originalLevel = CurrentLevel; GridActor originalRole = Grid.Actor(client.MemberId); GridCell cell = originalRole.Cell;
            long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            var definitions = (System.Collections.Generic.Dictionary<int, DRLevelConfig>)Tables.Instance.DTLevelConfig.DataMap; DRLevelConfig next = definitions[2];
            try
            {
                definitions.Remove(2);
                LevelDo(LevelControlAction.Continue, "MissingNextLevel");
                Assert.That(CurrentLevel, Is.SameAs(originalLevel)); Assert.That(Grid.Actor(client.MemberId), Is.SameAs(originalRole));
                Assert.That(originalRole.HandItemId, Is.EqualTo(bowl)); Assert.That(originalRole.Cell, Is.EqualTo(cell));
                Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            }
            finally { definitions.Add(2, next); }
            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            using GridWorldSnapshot old = GridBroadcast.CreateSnapshot(Grid, peer);
            LevelDo(LevelControlAction.Continue);
            long levelId = projection.LevelId, version = projection.Version;
            old.Version = version + 1; old.Items.Clear();
            projection.ApplyGridSnapshot(old);
            Assert.That(projection.LevelId, Is.EqualTo(levelId)); Assert.That(projection.Version, Is.EqualTo(version));
            Assert.That(projection.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            Assert.That(Grid.Item(bowl).Location, Is.EqualTo(GridItemLocation.Ground)); Assert.That(Grid.Item(bowl).Cell, Is.EqualTo(cell));
        }

        [Test]
        public void SoloLevelCarryPreservesCookingPotAndRejectsOldLevelAndDuplicateContinue()
        {
            StartSolo(); ClientSessionComponent client = host.Client; var projection = client.GetComponent<GridClientComponent>();
            long pot = SoupFillPot(client); PrepDo(client, GridAction.Move, cell: new GridCell(1, 9));
            LevelDo(LevelControlAction.Start);
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks - 80);
            GridAutoProcess process = SoupBegin(client, pot); long batch = process.Id;
            PrepDo(client, GridAction.TakeCounter, PrepStation("灶台").Id, pot);
            Level old = CurrentLevel; GridActor oldRole = Grid.Actor(client.MemberId); GridCell drop = oldRole.Cell;
            AdvanceTicks(old.OperatingTicks - old.ElapsedOperatingTicks + 1); PumpMessages(8);
            Assert.That(old.Phase, Is.EqualTo(LevelPhase.Completed)); Assert.That(old.Score, Is.Zero); Assert.That(old.Stars, Is.Zero);
            int progress = process.ProgressTicks; Assert.That(progress, Is.LessThan(process.RequiredTicks));
            AdvanceTicks(20); Assert.That(process.ProgressTicks, Is.EqualTo(progress));
            string worldId = Grid.WorldId; long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            var oldMove = projection.CreateGridCommand(GridAction.Move, oldRole.Cell);
            var continuation = projection.CreateLevelCommand(LevelControlAction.Continue);
            long command = continuation.CommandId, levelId = continuation.LevelId, generation = continuation.LevelGeneration;
            int nextConfig = continuation.NextLevelConfigId;
            using (var reply = LevelReply(client, continuation)) Assert.That(reply.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(old.IsDisposed && oldRole.IsDisposed, Is.True);
            Assert.That(CurrentLevel.Generation, Is.EqualTo(generation + 1)); Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing));
            Assert.That(Grid.WorldId, Is.EqualTo(worldId)); Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            Assert.That(Grid.Item(pot).Location, Is.EqualTo(GridItemLocation.Ground)); Assert.That(Grid.Item(pot).Cell, Is.EqualTo(drop));
            Assert.That(Grid.AutoProcess(batch), Is.SameAs(process)); Assert.That(process.ProgressTicks, Is.EqualTo(progress));
            Assert.That(Grid.Actor(client.MemberId).Parent, Is.SameAs(CurrentLevel)); Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.Zero);
            using (var reply = SoloReply(client, oldMove)) Assert.That(reply.Message, Is.EqualTo("WrongLevel"));
            var replay = ProductMessages.Create<LevelCommandRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = client.MatchId; value.WorldId = projection.WorldId;
                value.WorldGeneration = projection.WorldGeneration; value.BindingGeneration = client.BindingGeneration; value.CommandId = command;
                value.LevelId = levelId; value.LevelGeneration = generation; value.Action = (int)LevelControlAction.Continue; value.NextLevelConfigId = nextConfig;
            });
            long newId = CurrentLevel.Id;
            using (var reply = LevelReply(client, replay)) Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Executed));
            Assert.That(CurrentLevel.Id, Is.EqualTo(newId)); Assert.That(Grid.Items.Count, Is.EqualTo(items.Length));
        }
    }
}
