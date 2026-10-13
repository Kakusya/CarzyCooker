using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(FrontWorkCommands))]
    public sealed partial class ProductSessionTests
    {
        private CompanionRole FrontRole => CurrentLevel.Children.Values.OfType<CompanionRole>().Single();
        private HostPeer FrontPeer => ((LocalMessageComponent)((ClientSessionComponent)host.Client).Local).Peer;

        private FrontWorkResponse FrontPlayerReply(FrontWorkRequest request)
        {
            var task = ((ClientSessionComponent)host.Client).CallProduct(request); PumpMessages(8);
            Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending));
            return (FrontWorkResponse)task.GetAwaiter().GetResult();
        }

        private FrontWorkReceipt FrontNpcReply(CompanionRole role, FrontWorkIntent intent)
        {
            var task = role.RequestCompanionWork(intent); PumpMessages(8);
            Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending));
            return task.GetAwaiter().GetResult();
        }

        private FrontWorkReceipt FrontRawNpcReply(CompanionRole role, CompanionWorkRequest request)
        {
            var task = role.GetAsyncOwner().AsyncScope.RunAsync(context => context.CallLocalValueAsync(
                role.Root().GetComponent<ProcessInnerSender>(), ((HostSessionComponent)host.Authority).GetActorId(), request,
                response => { var value = (CompanionWorkResponse)response; return new FrontWorkReceipt(value.Error, (GridResultStatus)value.Status, value.Message); }));
            PumpMessages(8); Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending));
            return task.GetAwaiter().GetResult();
        }

        private void FrontPlayerDo(FrontWorkAction action, FrontWork work, string rejection = null, long? serial = null)
        {
            var projection = ((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>();
            using (var receipt = FrontPlayerReply(projection.CreateFrontWorkCommand(action, work.Id, serial ?? work.ClaimSerial))) Assert.That(receipt.Error, Is.Zero, receipt.Message);
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(FrontPeer.LastCommandStatus, Is.EqualTo(rejection == null ? GridResultStatus.Executed : GridResultStatus.Rejected), FrontPeer.LastCommandReason);
            Assert.That(FrontPeer.LastCommandReason, Is.EqualTo(rejection));
        }

        private void FrontWait(Func<bool> condition, int limit = 250)
        {
            for (int tick = 0; tick < limit && !condition(); ++tick) { AdvanceTicks(1); PumpMessages(3); }
            Assert.That(condition(), Is.True, "Real front business steps did not reach the expected state: " +
                string.Join("; ", CurrentLevel.Children.Values.OfType<Customer>().Select(value => $"customer {value.Id} {value.Phase} cell={value.Cell} ask={value.AskWorkId} order={value.OrderId} serial={value.TransitionSerial} queued={value.TransitionQueued} pending={value.SuccessorPending}")) + " / " +
                string.Join("; ", CurrentLevel.Children.Values.OfType<FrontWork>().Select(value => $"work {value.Id} {value.Kind}/{value.State} progress={value.Progress} serial={value.TransitionSerial} queued={value.TransitionQueued} pending={value.SuccessorPending}")));
        }

        private FrontWork InitialWashTarget()
        {
            GridStation source = PrepStation("盘台"); GridItem plate = Grid.Item(source.ItemId); source.ItemId = 0;
            GridStation pool = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Kind == GridStationKind.DirtyPool);
            GridStation wash = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Kind == GridStationKind.Wash);
            plate.Location = GridItemLocation.DirtyPool; plate.HolderId = pool.Id; plate.Cell = pool.Cell; plate.Hygiene = ItemHygiene.Dirty; pool.PoolItemIds.Add(plate.Id);
            var target = new FrontWorkDefinition(FrontWorkKind.Wash, 0, wash.Id, plate.Id, 61);
            return CurrentLevel.AddChild<FrontWork, FrontWorkDefinition, long>(target, CurrentLevel.Generation);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SoloCompanionClaimRealPeerAndRoleCompeteThroughAuthority(bool npcFirst)
        {
            StartSolo(); LevelDo(LevelControlAction.Start);
            Customer customer = InitialSeatedCustomer(); FrontWork work = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId);
            CompanionRole role = FrontRole; var projection = ((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>();
            void Player() { using var response = FrontPlayerReply(projection.CreateFrontWorkCommand(FrontWorkAction.Claim, work.Id)); Assert.That(response.Status, Is.EqualTo((int)GridResultStatus.Accepted)); }
            void Npc() { Assert.That(FrontNpcReply(role, new FrontWorkIntent(FrontWorkAction.Claim, work.Id)).Status, Is.EqualTo(GridResultStatus.Accepted)); }
            if (npcFirst) { Npc(); Player(); } else { Player(); Npc(); }
            Assert.That(work.State, Is.EqualTo(FrontWorkState.Available)); Assert.That(work.Progress, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(work.WorkerKind, Is.EqualTo(npcFirst ? FrontWorkerKind.Companion : FrontWorkerKind.Player));
            Assert.That(work.State, Is.EqualTo(FrontWorkState.Approaching)); Assert.That(work.Progress, Is.Zero);
            Assert.That(npcFirst ? FrontPeer.LastCommandReason : role.LastWorkReason, Is.EqualTo("WorkBusy"));
            AdvanceTicks(1);
            Assert.That(work.Progress, Is.Zero); Assert.That(customer.OrderId, Is.Zero);
            FrontWait(() => work.State == FrontWorkState.Completed);
            Assert.That(work.Progress, Is.EqualTo(1).Within(1e-9)); Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.Ordered));
            Assert.That(CurrentLevel.Children.Values.OfType<Order>().Count(), Is.EqualTo(1));
            Companion identity = role.Companion; var growth = CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion;
            Assert.That(growth.TryGetValue(identity.Id, out int count) ? count : 0, Is.EqualTo(npcFirst ? 1 : 0));
            Assert.That(Grid.Actor(((ClientSessionComponent)host.Client).MemberId).FrontWorkId, Is.Zero);
            if (npcFirst) Assert.That(role.LastNotificationKind, Is.EqualTo(2));
        }

        [TestCase("instance", "RoleChanged")]
        [TestCase("level", "WrongLevel")]
        [TestCase("world", "WrongWorld")]
        [TestCase("protocol", "IncompatibleProtocol")]
        public void SoloCompanionClaimTrustedRoleRejectsStaleIdentity(string field, string reason)
        {
            StartSolo(); LevelDo(LevelControlAction.Start); Customer customer = InitialSeatedCustomer();
            FrontWork work = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId); CompanionRole role = FrontRole;
            var request = role.CreateCompanionWorkRequest(new FrontWorkIntent(FrontWorkAction.Claim, work.Id));
            if (field == "instance") ++request.RoleInstanceId;
            if (field == "level") ++request.LevelId;
            if (field == "world") ++request.WorldGeneration;
            if (field == "protocol") --request.ProtocolVersion;
            FrontWorkReceipt response = FrontRawNpcReply(role, request);
            Assert.That(response.Error, Is.Not.Zero); Assert.That(response.Reason, Is.EqualTo(reason));
            AdvanceTicks(3); Assert.That(work.State, Is.EqualTo(FrontWorkState.Available)); Assert.That(work.Progress, Is.Zero);
            Assert.That(role.LastSubmittedRequestId, Is.Zero); Assert.That(CurrentLevel.GetComponent<FrontWorkCommands>().Pending, Is.Empty);
        }

        [Test]
        public void SoloCompanionClaimClientCannotEnterTrustedNpcOrSubmitHandler()
        {
            StartSolo(); LevelDo(LevelControlAction.Start); Customer customer = InitialSeatedCustomer();
            FrontWork work = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId); CompanionRole role = FrontRole;
            var task = ((ClientSessionComponent)host.Client).CallProduct(role.CreateCompanionWorkRequest(new FrontWorkIntent(FrontWorkAction.Claim, work.Id)));
            PumpMessages(8); using var response = (CompanionWorkResponse)task.GetAwaiter().GetResult();
            Assert.That(response.Message, Is.EqualTo("InternalOnly")); Assert.That(response.Error, Is.Not.Zero);
            var submit = ((ClientSessionComponent)host.Client).CallProduct(ProductMessages.Create<FrontWorkSubmitRequest>(value => value.WorkId = work.Id));
            PumpMessages(8); using var inner = (FrontWorkSubmitResponse)submit.GetAwaiter().GetResult();
            Assert.That(inner.Message, Is.EqualTo("InternalOnly"));
            AdvanceTicks(3); Assert.That(work.State, Is.EqualTo(FrontWorkState.Available)); Assert.That(role.WorkId, Is.Zero);
        }

        [Test]
        public void SoloCompanionClaimOneOwnedPhysicalRequestAndLatestReplayDoNotDuplicateWork()
        {
            StartSolo(); LevelDo(LevelControlAction.Start); Customer customer = InitialSeatedCustomer();
            FrontWork work = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId); CompanionRole role = FrontRole;
            var task = role.RequestCompanionWork(new FrontWorkIntent(FrontWorkAction.Claim, work.Id));
            Assert.That(role.RequestInFlight, Is.True);
            Assert.Throws<InvalidOperationException>(() => role.RequestCompanionWork(new FrontWorkIntent(FrontWorkAction.Claim, work.Id)));
            PumpMessages(8); Assert.That(task.GetAwaiter().GetResult().Error, Is.Zero); Assert.That(role.RequestInFlight, Is.False);
            AdvanceTicks(1); PumpMessages(8); long serial = work.ClaimSerial;
            var replay = role.CreateCompanionWorkRequest(new FrontWorkIntent(FrontWorkAction.Claim, work.Id)); replay.RequestId = role.LastSubmittedRequestId;
            Assert.That(FrontRawNpcReply(role, replay).Status, Is.EqualTo(GridResultStatus.Executed));
            var conflict = role.CreateCompanionWorkRequest(new FrontWorkIntent(FrontWorkAction.Stop, work.Id, serial)); conflict.RequestId = role.LastSubmittedRequestId;
            Assert.That(FrontRawNpcReply(role, conflict).Reason, Is.EqualTo("CommandConflict"));
            AdvanceTicks(2); Assert.That(work.ClaimSerial, Is.EqualTo(serial)); Assert.That(work.Progress, Is.Zero);
        }

        [Test]
        public void SoloCompanionResumePlayerStopsAndNpcContinuesSameActualWash()
        {
            StartSolo(); LevelDo(LevelControlAction.Start); FrontWork clear = InitialDirtyTable();
            FrontPlayerDo(FrontWorkAction.Claim, clear); FrontWait(() => clear.State == FrontWorkState.Completed);
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty, "Player Clear cannot grow the companion");
            FrontWait(() => CurrentLevel.Children.Values.OfType<FrontWork>().Any(value => value.Kind == FrontWorkKind.Wash));
            FrontWork wash = CurrentLevel.Children.Values.OfType<FrontWork>().Single(value => value.Kind == FrontWorkKind.Wash);
            FrontPlayerDo(FrontWorkAction.Claim, wash); FrontWait(() => wash.State == FrontWorkState.Working); AdvanceTicks(17);
            double progress = wash.Progress; long oldSerial = wash.ClaimSerial;
            FrontPlayerDo(FrontWorkAction.Stop, wash);
            Assert.That(wash.Progress, Is.EqualTo(progress)); Assert.That(wash.State, Is.EqualTo(FrontWorkState.Available));
            CompanionRole role = FrontRole;
            Assert.That(FrontNpcReply(role, new FrontWorkIntent(FrontWorkAction.Claim, wash.Id, wash.ClaimSerial)).Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(wash.ClaimSerial, Is.EqualTo(oldSerial + 1)); Assert.That(wash.Progress, Is.EqualTo(progress));
            FrontPlayerDo(FrontWorkAction.Stop, wash, "ClaimChanged", oldSerial);
            Assert.That(wash.WorkerKind, Is.EqualTo(FrontWorkerKind.Companion));
            FrontWait(() => wash.State == FrontWorkState.Completed);
            Assert.That(Grid.Item(wash.ItemId).Location, Is.EqualTo(GridItemLocation.CleanPool));
            Assert.That(Grid.Item(wash.ItemId).Hygiene, Is.EqualTo(ItemHygiene.Clean));
            Assert.That(CurrentLevel.Children.Values.OfType<FrontWork>().Count(value => value.Kind == FrontWorkKind.Wash), Is.EqualTo(1));
            Companion identity = role.Companion;
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion[identity.Id], Is.EqualTo(1));
        }

        [Test]
        public void SoloCompanionResumePauseFreezesStartedWorkButReleasesApproachingClaim()
        {
            StartSolo(); LevelDo(LevelControlAction.Start); Customer customer = InitialSeatedCustomer();
            FrontWork work = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId); CompanionRole role = FrontRole;
            FrontNpcReply(role, new FrontWorkIntent(FrontWorkAction.Claim, work.Id)); AdvanceTicks(2);
            Assert.That(work.State, Is.EqualTo(FrontWorkState.Approaching)); Assert.That(role.Motion, Is.EqualTo(GridMotion.Transit));
            GridCell next = role.NextCell; LevelDo(LevelControlAction.Pause); long tick = Grid.Tick;
            Assert.That(work.State, Is.EqualTo(FrontWorkState.Available)); Assert.That(role.WorkId, Is.Zero); Assert.That(role.Path, Is.Empty);
            AdvanceTicks(20); Assert.That(Grid.Tick, Is.EqualTo(tick)); Assert.That(work.Progress, Is.Zero);
            LevelDo(LevelControlAction.Resume); FrontWait(() => role.Motion == GridMotion.Standing);
            Assert.That(role.Cell, Is.EqualTo(next));
            FrontNpcReply(role, new FrontWorkIntent(FrontWorkAction.Claim, work.Id, work.ClaimSerial)); AdvanceTicks(1);
            FrontWait(() => work.State == FrontWorkState.Working); AdvanceTicks(7);
            double progress = work.Progress; long serial = work.ClaimSerial;
            LevelDo(LevelControlAction.Pause); AdvanceTicks(20);
            Assert.That(work.State, Is.EqualTo(FrontWorkState.Working)); Assert.That(work.Progress, Is.EqualTo(progress));
            Assert.That(work.ClaimSerial, Is.EqualTo(serial)); Assert.That(role.WorkId, Is.EqualTo(work.Id));
            LevelDo(LevelControlAction.Resume); Assert.That(work.Progress, Is.GreaterThan(progress));
            FrontWait(() => work.State == FrontWorkState.Completed); Assert.That(customer.OrderId, Is.Not.Zero);
        }

        [Test]
        public void SoloCompanionResumeMoveAndMemberExitReleaseOnlyOriginalPlayerClaim()
        {
            StartSolo(); LevelDo(LevelControlAction.Start); Customer customer = InitialSeatedCustomer();
            FrontWork work = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId); ClientSessionComponent client = host.Client;
            FrontPlayerDo(FrontWorkAction.Claim, work); FrontWait(() => work.State == FrontWorkState.Working); AdvanceTicks(5);
            GridActor actor = Grid.Actor(client.MemberId); double progress = work.Progress;
            using (var response = SoloReply(client, client.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.Move, new GridCell(2, 6)))) Assert.That(response.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(work.State, Is.EqualTo(FrontWorkState.Available)); Assert.That(work.Progress, Is.EqualTo(progress));
            FrontWait(() => actor.Motion == GridMotion.Standing && !actor.HasIntent);
            FrontPlayerDo(FrontWorkAction.Claim, work); FrontWait(() => work.State == FrontWorkState.Working); AdvanceTicks(3);
            progress = work.Progress;
            using (var response = (ProductLeaveResponse)Finish(client.CallProduct(ProductMessages.Create<ProductLeaveRequest>(value => value.MatchId = client.MatchId)), 100)) Assert.That(response.Error, Is.Zero);
            AdvanceTicks(1);
            Assert.That(work.State, Is.EqualTo(FrontWorkState.Available)); Assert.That(work.Progress, Is.EqualTo(progress));
            Assert.That(fibers[0].Root.GetComponent<ProcessInnerSender>().IsStopped, Is.False);
        }

        [Test]
        public void SoloCompanionGrowthThirdActualAskAcceleratesSameTickNextWashWithCeil()
        {
            StartSolo(); LevelDo(LevelControlAction.Start); CompanionRole role = FrontRole; Companion identity = role.Companion;
            for (int number = 1; number <= 2; ++number)
            {
                Customer customer = InitialSeatedCustomer(); FrontWork work = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId);
                FrontNpcReply(role, new FrontWorkIntent(FrontWorkAction.Claim, work.Id)); AdvanceTicks(1); FrontWait(() => work.State == FrontWorkState.Completed);
                customer.Phase = CustomerPhase.Gone;
                Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion[identity.Id], Is.EqualTo(number));
            }
            Customer third = InitialSeatedCustomer(); FrontWork ask = CurrentLevel.GetChild<FrontWork>(third.AskWorkId);
            FrontWork wash = InitialWashTarget();
            FrontNpcReply(role, new FrontWorkIntent(FrontWorkAction.Claim, ask.Id)); AdvanceTicks(1); FrontWait(() => ask.State == FrontWorkState.Working);
            AdvanceTicks(ask.BaseTicks);
            Assert.That(ask.State, Is.EqualTo(FrontWorkState.Working)); Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion[identity.Id], Is.EqualTo(2));
            FrontNpcReply(role, new FrontWorkIntent(FrontWorkAction.Claim, wash.Id)); AdvanceTicks(1); PumpMessages(8);
            Assert.That(ask.State, Is.EqualTo(FrontWorkState.Completed)); Assert.That(wash.FrozenTicks, Is.EqualTo(31));
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion[identity.Id], Is.EqualTo(3));
            FrontWait(() => wash.State == FrontWorkState.Working); int frozen = wash.FrozenTicks;
            AdvanceTicks(30); Assert.That(wash.State, Is.EqualTo(FrontWorkState.Working)); Assert.That(wash.FrozenTicks, Is.EqualTo(frozen));
            AdvanceTicks(1); Assert.That(wash.State, Is.EqualTo(FrontWorkState.Working)); Assert.That(wash.TransitionQueued, Is.True);
            AdvanceTicks(1); Assert.That(wash.State, Is.EqualTo(FrontWorkState.Completed));
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion[identity.Id], Is.EqualTo(4));
            AdvanceTicks(2); Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion[identity.Id], Is.EqualTo(4));
            CompanionWorkRequest old = role.CreateCompanionWorkRequest(new FrontWorkIntent(FrontWorkAction.Stop, wash.Id, wash.ClaimSerial));
            foreach (Customer customer in CurrentLevel.Children.Values.OfType<Customer>()) customer.Phase = CustomerPhase.Gone;
            foreach (GridStation station in Grid.Stations.Values.Select(value => (GridStation)value).Where(value => value.Kind == GridStationKind.Table))
            { station.GetComponent<FrontTable>().CustomerId = 0; station.GetComponent<FrontTable>().State = FrontTableState.Free; }
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks + 1); LevelDo(LevelControlAction.Continue);
            Assert.That(role.IsDisposed, Is.True); Assert.That((Companion)FrontRole.Companion, Is.SameAs(identity));
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
            Assert.That(FrontRawNpcReply(FrontRole, old).Reason, Is.EqualTo("RoleChanged"));
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
        }

        [Test]
        public void SoloCompanionGrowthCancellationAndDuplicateNotificationsNeverAddCompletion()
        {
            StartSolo(); LevelDo(LevelControlAction.Start); Customer customer = InitialSeatedCustomer();
            FrontWork work = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId); CompanionRole role = FrontRole;
            FrontNpcReply(role, new FrontWorkIntent(FrontWorkAction.Claim, work.Id)); AdvanceTicks(1); FrontWait(() => work.State == FrontWorkState.Working); AdvanceTicks(7);
            double progress = work.Progress;
            FrontNpcReply(role, new FrontWorkIntent(FrontWorkAction.Stop, work.Id, work.ClaimSerial)); AdvanceTicks(1);
            Assert.That(work.Progress, Is.EqualTo(progress)); Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
            long expectedSerial = work.ClaimSerial;
            FrontNpcReply(role, new FrontWorkIntent(FrontWorkAction.Claim, work.Id, expectedSerial)); AdvanceTicks(1); FrontWait(() => work.State == FrontWorkState.Completed);
            Companion identity = role.Companion; var growth = CurrentLevel.GetComponent<CompanionGrowth>();
            Assert.That(growth.CompletedByCompanion[identity.Id], Is.EqualTo(1));
            var replay = role.CreateCompanionWorkRequest(new FrontWorkIntent(FrontWorkAction.Claim, work.Id, expectedSerial)); replay.RequestId = role.LastSubmittedRequestId;
            Assert.That(FrontRawNpcReply(role, replay).Error, Is.Zero);
            fibers[0].Root.GetComponent<ProcessInnerSender>().Send(role.GetActorId(), ProductMessages.Create<CompanionWorkResult>(message =>
            { message.RoleId = role.Id; message.RoleInstanceId = role.InstanceId; message.LevelGeneration = role.LevelGeneration;
                message.RequestId = role.LastCommittedRequestId; message.WorkId = work.Id; message.EventKind = 2; message.Status = (int)GridResultStatus.Executed; }));
            PumpMessages(8); AdvanceTicks(2);
            Assert.That(growth.CompletedByCompanion[identity.Id], Is.EqualTo(1)); Assert.That(CurrentLevel.Children.Values.OfType<Order>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void SoloCompanionGrowthClearIsPlayerOnlyAndDoesNotCount()
        {
            StartSolo(); LevelDo(LevelControlAction.Start); FrontWork clear = InitialDirtyTable(); CompanionRole role = FrontRole;
            FrontNpcReply(role, new FrontWorkIntent(FrontWorkAction.Claim, clear.Id)); AdvanceTicks(1); PumpMessages(8);
            Assert.That(role.LastWorkReason, Is.EqualTo("PlayerWorkOnly")); Assert.That(clear.State, Is.EqualTo(FrontWorkState.Available));
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
            FrontPlayerDo(FrontWorkAction.Claim, clear); FrontWait(() => clear.State == FrontWorkState.Completed);
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
        }

        [Test]
        public void SoloCompanionClaimDisposedOwnedRoleRejectsItsLatePhysicalRequestAfterSameIdReuse()
        {
            StartSolo(); LevelDo(LevelControlAction.Start); Customer customer = InitialSeatedCustomer();
            FrontWork work = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId); CompanionRole oldRole = FrontRole; Companion identity = oldRole.Companion;
            HostSessionComponent authority = host.Authority;
            ProcessInnerSender sender = fibers[0].Root.GetComponent<ProcessInnerSender>();
            var locked = fibers[0].Root.GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.Mailbox, authority.InstanceId);
            PumpMessages(3); var gate = locked.GetAwaiter().GetResult();
            try
            {
                var pending = oldRole.RequestCompanionWork(new FrontWorkIntent(FrontWorkAction.Claim, work.Id)); PumpMessages(5);
                Assert.That(pending.Status, Is.EqualTo(UniTaskStatus.Pending)); Assert.That(oldRole.RequestInFlight, Is.True);
                int requests = sender.requestCallback.Count; Assert.That(requests, Is.GreaterThan(0));
                long id = oldRole.Id, oldInstance = oldRole.InstanceId; oldRole.Dispose();
                Assert.That(pending.Status, Is.EqualTo(UniTaskStatus.Canceled)); Assert.Throws<OperationCanceledException>(() => pending.GetAwaiter().GetResult());
                Assert.That(sender.IsStopped, Is.False); Assert.That(sender.requestCallback.Count, Is.EqualTo(requests));
                CompanionRole replacement = CurrentLevel.AddChildWithId<CompanionRole, Companion, long>(id, identity, CurrentLevel.Generation);
                Assert.That(replacement.InstanceId, Is.Not.EqualTo(oldInstance));
                gate.Dispose(); gate = null; PumpMessages(12);
                Assert.That(sender.requestCallback, Is.Empty); Assert.That(replacement.LastSubmittedRequestId, Is.Zero);
                Assert.That(replacement.RequestInFlight, Is.False); Assert.That(work.State, Is.EqualTo(FrontWorkState.Available));
                Assert.That(FrontNpcReply(replacement, new FrontWorkIntent(FrontWorkAction.Claim, work.Id)).Error, Is.Zero);
                AdvanceTicks(1); Assert.That(work.WorkerInstanceId, Is.EqualTo(replacement.InstanceId));
            }
            finally { gate?.Dispose(); }
        }

        [Test]
        public void SoloCompanionResumePauseCancelsAcceptedButUnexecutedClaim()
        {
            StartSolo(); LevelDo(LevelControlAction.Start); Customer customer = InitialSeatedCustomer();
            FrontWork work = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId); CompanionRole role = FrontRole;
            Assert.That(FrontNpcReply(role, new FrontWorkIntent(FrontWorkAction.Claim, work.Id)).Status, Is.EqualTo(GridResultStatus.Accepted));
            Assert.That(CurrentLevel.GetComponent<FrontWorkCommands>().Pending.Count, Is.EqualTo(1));
            LevelDo(LevelControlAction.Pause);
            Assert.That(CurrentLevel.GetComponent<FrontWorkCommands>().Pending, Is.Empty);
            Assert.That(role.LastResultStatus, Is.EqualTo(GridResultStatus.Cancelled)); Assert.That(role.LastWorkReason, Is.EqualTo("Paused"));
            Assert.That(work.State, Is.EqualTo(FrontWorkState.Available)); Assert.That(work.Progress, Is.Zero); Assert.That(work.ClaimSerial, Is.Zero);
            AdvanceTicks(10); Assert.That(role.WorkId, Is.Zero);
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
            LevelDo(LevelControlAction.Resume);
            FrontNpcReply(role, new FrontWorkIntent(FrontWorkAction.Claim, work.Id)); AdvanceTicks(1);
            Assert.That(work.WorkerId, Is.EqualTo(role.Id)); Assert.That(work.ClaimSerial, Is.EqualTo(1));
        }

        [Test]
        public void SoloCompanionResumeDisposedWorkingRoleKeepsProgressForItsReplacement()
        {
            StartSolo(); LevelDo(LevelControlAction.Start); Customer customer = InitialSeatedCustomer();
            FrontWork work = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId); CompanionRole role = FrontRole; Companion identity = role.Companion;
            FrontNpcReply(role, new FrontWorkIntent(FrontWorkAction.Claim, work.Id)); AdvanceTicks(1); FrontWait(() => work.State == FrontWorkState.Working); AdvanceTicks(7);
            double progress = work.Progress; long id = role.Id, serial = work.ClaimSerial;
            var stale = role.CreateCompanionWorkRequest(new FrontWorkIntent(FrontWorkAction.Stop, work.Id, serial));
            role.Dispose(); CompanionRole replacement = CurrentLevel.AddChildWithId<CompanionRole, Companion, long>(id, identity, CurrentLevel.Generation);
            AdvanceTicks(1);
            Assert.That(work.State, Is.EqualTo(FrontWorkState.Available)); Assert.That(work.Progress, Is.EqualTo(progress));
            Assert.That(replacement.WorkId, Is.Zero); Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion, Is.Empty);
            Assert.That(FrontRawNpcReply(replacement, stale).Reason, Is.EqualTo("RoleChanged"));
            FrontNpcReply(replacement, new FrontWorkIntent(FrontWorkAction.Claim, work.Id, serial)); AdvanceTicks(1);
            Assert.That(work.Progress, Is.EqualTo(progress)); Assert.That(work.ClaimSerial, Is.EqualTo(serial + 1));
            FrontWait(() => work.State == FrontWorkState.Completed);
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion[identity.Id], Is.EqualTo(1));
            Assert.That(fibers[0].Root.GetComponent<ProcessInnerSender>().IsStopped, Is.False);
        }
    }
}
