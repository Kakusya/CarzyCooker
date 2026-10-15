using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(CooperativeSaveResult))]
    [FriendOf(typeof(CooperativeClaim))]
    [FriendOf(typeof(CooperativeClaimState))]
    [FriendOf(typeof(ClientProgressDelivery))]
    public sealed partial class ProductSessionTests
    {
        private ClientSessionComponent ClaimRuntimeFixture(out SoloSaveRepository repository, out SoloSaveRepository remoteRepository, out string directory)
        {
            StartRoomFixture();
            repository = TestSaveRepository(out directory);
            var own = OfflineProfiles.OwnIdentity(repository);
            ProveFixtureProfile(host.Client, repository);
            var remote = JoinRoomFixture();
            remoteRepository = remote.Root().AddComponent<SoloSaveRepository, string, string>(directory, Guid.NewGuid().ToString("N"));
            var other = OfflineProfiles.OwnIdentity(remoteRepository);
            OfflineProfiles.TrustIdentity(repository, other);
            OfflineProfiles.TrustIdentity(remoteRepository, own);
            ProveFixtureProfile(remote, remoteRepository);
            RoomDo(ProductRoomAction.StartRestaurant);
            PumpUntil(() => remote.GetComponent<GridClientComponent>().Confirmed && RenovationView.Confirmed, "Both actual world projections", 120, true);
            FrontWait(() => Buff.Prepared, 8);
            BuffDo(Buff.Candidates.First());
            return remote;
        }

        private CooperativeSaveResult CompleteClaimRuntime()
        {
            ChapterNaturalComplete();
            FrontWait(() => Renovation.Prepared, 8);
            Assert.That((CooperativeSaveResult)CurrentLevel.ClaimResult, Is.Null, "Unchosen settlement must not leak a mutable result");
            RenovationChoose(Renovation.Candidates.First());
            PumpUntil(() => ((CooperativeSaveResult)CurrentLevel.ClaimResult)?.SignedSuccess != null, "Result signed through Root repository", 200, true);
            return CurrentLevel.ClaimResult;
        }

        [Test]
        [Category("CooperativeClaimsRuntime")]
        public void CooperativeClaimsRealKcpFreezesStartExcludesLateJoinAndRetransmitsStableTicketAfterLostAck()
        {
            var remote = ClaimRuntimeFixture(out var repository, out var remoteRepository, out string directory);
            SoloSaveRepository lateRepository = null;
            try
            {
                Assert.That((CooperativeSaveResult)CurrentLevel.ClaimResult, Is.Null);
                LevelDo(LevelControlAction.Start);
                Assert.That(CurrentLevel.EligibleProfiles.Keys, Is.EquivalentTo(new[] { repository.SaveProfile(), remoteRepository.SaveProfile() }));
                var late = JoinRoomFixture();
                lateRepository = late.Root().AddComponent<SoloSaveRepository, string, string>(directory, Guid.NewGuid().ToString("N"));
                OfflineProfiles.TrustIdentity(repository, OfflineProfiles.OwnIdentity(lateRepository));
                OfflineProfiles.TrustIdentity(lateRepository, OfflineProfiles.OwnIdentity(repository));
                ProveFixtureProfile(late, lateRepository);
                var result = CompleteClaimRuntime();
                Assert.That(result.Claims.Count, Is.EqualTo(2));
                Assert.That(result.Claims.ContainsKey(lateRepository.SaveProfile()), Is.False);
                CooperativeClaim claim = result.Claims[remoteRepository.SaveProfile()];
                PumpUntil(() => claim.AckedBindings.Contains(remote.BindingGeneration), "ACK after durable KCP delivery", 350, true);
                string ticketPath = Path.Combine(remoteRepository.DirectoryPath, "Tickets", claim.TicketId + ".ccticket");
                byte[] original = File.ReadAllBytes(ticketPath);
                claim.AckedBindings.Remove(remote.BindingGeneration);
                PumpUntil(() => claim.AckedBindings.Contains(remote.BindingGeneration), "Lost ACK resends the same ticket", 350, true);
                Assert.That(File.ReadAllBytes(ticketPath), Is.EqualTo(original));
                Assert.That(ProgressClaimFiles.Browse(remoteRepository).Length, Is.EqualTo(1));
                Assert.That(ProgressClaimFiles.Browse(lateRepository), Is.Empty);
                AdvanceTicks(10); Pump(20);
                Assert.That((CooperativeSaveResult)CurrentLevel.ClaimResult, Is.SameAs(result));
                Assert.That(((Match)((HostSessionComponent)host.Authority).Match).GetComponent<CooperativeClaimState>().Results.Count, Is.EqualTo(1));
                RoomDo(ProductRoomAction.CloseRestaurant);
                Assert.That(ProgressClaimFiles.Browse(remoteRepository).Single().State, Is.EqualTo(ProgressReceiptState.Available));
                Assert.That(((Match)((HostSessionComponent)host.Authority).Match).GetComponent<CooperativeClaimState>().Closing, Is.False);
            }
            finally { lateRepository?.Dispose(); remoteRepository.Dispose(); repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("CooperativeClaimsRuntime")]
        public void CooperativeClaimsRealKcpWriteFailureDoesNotAckAndHostSaveFailureDoesNotAdvanceWhileRemoteDoesNotClaim()
        {
            var remote = ClaimRuntimeFixture(out var repository, out var remoteRepository, out string directory);
            try
            {
                string tickets = Path.Combine(remoteRepository.DirectoryPath, "Tickets");
                File.WriteAllText(tickets, "fixture blocks directory creation");
                var result = CompleteClaimRuntime();
                CooperativeClaim remoteClaim = result.Claims[remoteRepository.SaveProfile()];
                PumpUntil(() => remote.GetComponent<ClientProgressDelivery>()?.Failure != null, "Real persistence failure returned", 350, true);
                Assert.That(remoteClaim.AckedBindings.Contains(remote.BindingGeneration), Is.False);
                Assert.That(ProgressClaimFiles.Browse(remoteRepository), Is.Empty);
                File.Delete(tickets);
                PumpUntil(() => remoteClaim.AckedBindings.Contains(remote.BindingGeneration), "Retry persisted exact same identity", 350, true);
                Assert.That(ProgressClaimFiles.Browse(remoteRepository).Single().TicketId, Is.EqualTo(remoteClaim.TicketId));
                CooperativeClaim localClaim = result.Claims[repository.SaveProfile()];
                string blocked = Path.Combine(repository.DirectoryPath, localClaim.TicketId + ".ccsave");
                Directory.CreateDirectory(blocked);
                long oldLevel = CurrentLevel.Id;
                using (var accepted = LevelReply(host.Client, RenovationView.CreateLevelCommand(LevelControlAction.Continue)))
                    Assert.That(accepted.Error, Is.Zero, accepted.Message);
                AdvanceTicks(1);
                PumpUntil(() => result.State == SoloSaveState.Failed, "Host physical save failure stays in settlement", 200, true);
                Assert.That(CurrentLevel.Id, Is.EqualTo(oldLevel));
                Assert.That(repository.Browse(), Is.Empty);
                Assert.That(ProgressClaimFiles.Browse(remoteRepository).Single().State, Is.EqualTo(ProgressReceiptState.Available));
                Directory.Delete(blocked);
                using (var accepted = LevelReply(host.Client, RenovationView.CreateLevelCommand(LevelControlAction.Continue)))
                    Assert.That(accepted.Error, Is.Zero, accepted.Message);
                AdvanceTicks(1);
                PumpUntil(() => result.HostSaveCommitted, "Explicit Host retry commits", 250, true);
                AdvanceTicks(2); Pump(30);
                Assert.That(CurrentLevel.Id, Is.Not.EqualTo(oldLevel));
                Assert.That(repository.Browse().Single().ResultId, Is.EqualTo(result.ResultId));
                Assert.That(ProgressClaimFiles.Browse(remoteRepository).Single().State, Is.EqualTo(ProgressReceiptState.Available));
            }
            finally { remoteRepository.Dispose(); repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("CooperativeClaimsRuntime")]
        public void CooperativeClaimsAbortedOperatingWorldNeverManufacturesResultOrTicket()
        {
            ClaimRuntimeFixture(out var repository, out var remoteRepository, out string directory);
            try
            {
                LevelDo(LevelControlAction.Start);
                Match match = ((HostSessionComponent)host.Authority).Match;
                RoomDo(ProductRoomAction.CloseRestaurant);
                Pump(40);
                Assert.That(match.GetComponent<CooperativeClaimState>().Results, Is.Empty);
                Assert.That(ProgressClaimFiles.Browse(repository), Is.Empty);
                Assert.That(ProgressClaimFiles.Browse(remoteRepository), Is.Empty);
            }
            finally { remoteRepository.Dispose(); repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("CooperativeClaimsPeerFirst")]
        public void CooperativeClaimsPeerAlreadyClaimedHostFailureRetryAndNewRestaurantKeepIndependentFacts()
        {
            var remote = ClaimRuntimeFixture(out var repository, out var other, out string directory);
            try
            {
                var result = CompleteClaimRuntime();
                CooperativeClaim remoteClaim = result.Claims[other.SaveProfile()];
                PumpUntil(() => remoteClaim.AckedBindings.Contains(remote.BindingGeneration), "Peer ticket durable", 350, true);
                var peerSaved = ProgressClaimFiles.Claim(other, remoteClaim.TicketId);
                string peerPath = Path.Combine(other.DirectoryPath, peerSaved.SlotId + ".ccsave");
                byte[] peerBytes = File.ReadAllBytes(peerPath);
                CooperativeClaim hostClaim = result.Claims[repository.SaveProfile()];
                string blocked = Path.Combine(repository.DirectoryPath, hostClaim.TicketId + ".ccsave");
                Directory.CreateDirectory(blocked);
                long oldLevel = CurrentLevel.Id;
                using (var accepted = LevelReply(host.Client, RenovationView.CreateLevelCommand(LevelControlAction.Continue)))
                {
                    Assert.That(accepted.Error, Is.Zero, accepted.Message);
                }
                AdvanceTicks(1);
                PumpUntil(() => result.State == SoloSaveState.Failed, "Host fails after peer commit", 200, true);
                Assert.That(CurrentLevel.Id, Is.EqualTo(oldLevel));
                Assert.That(File.ReadAllBytes(peerPath), Is.EqualTo(peerBytes));
                Directory.Delete(blocked);
                var retry = RenovationView.CreateLevelCommand(LevelControlAction.Continue);
                var duplicate = CopyReplayRequest(retry);
                using (var accepted = LevelReply(host.Client, retry))
                {
                    Assert.That(accepted.Error, Is.Zero, accepted.Message);
                }
                AdvanceTicks(1);
                PumpUntil(() => result.HostSaveCommitted, "Host retry independently commits", 200, true);
                AdvanceTicks(2); Pump(30);
                long next = CurrentLevel.Id;
                using (var repeated = LevelReply(host.Client, duplicate))
                {
                    Assert.That(repeated.Error, Is.Zero, repeated.Message);
                }
                AdvanceTicks(2);
                Assert.That(CurrentLevel.Id, Is.EqualTo(next));
                Assert.That(File.ReadAllBytes(peerPath), Is.EqualTo(peerBytes));
                Assert.That(other.Browse().Length, Is.EqualTo(1));
                string firstWorld = Grid.WorldId;
                RoomDo(ProductRoomAction.CloseRestaurant);
                RoomDo(ProductRoomAction.StartRestaurant);
                PumpUntil(() => RenovationView.Confirmed && remote.GetComponent<GridClientComponent>().Confirmed, "New restaurant in same Match", 100, true);
                FrontWait(() => Buff.Prepared, 8); BuffDo(Buff.Candidates.First());
                var second = CompleteClaimRuntime();
                Assert.That(second.ResultId, Is.Not.EqualTo(result.ResultId));
                Assert.That(Grid.WorldId, Is.Not.EqualTo(firstWorld));
                CooperativeClaim newRemote = second.Claims[other.SaveProfile()];
                PumpUntil(() => newRemote.AckedBindings.Contains(remote.BindingGeneration), "New world issues its own stable ticket", 350, true);
                var state = ((Match)((HostSessionComponent)host.Authority).Match).GetComponent<CooperativeClaimState>();
                Assert.That(state.Closing, Is.False);
                Assert.That(state.Results.Count, Is.EqualTo(2));
                Assert.That(ProgressClaimFiles.Browse(other).Select(row => row.State),
                    Is.EquivalentTo(new[] { ProgressReceiptState.Claimed, ProgressReceiptState.Available }));
            }
            finally { other.Dispose(); repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("CooperativeClaimsClose")]
        public void CooperativeClaimsNormalWholeMatchCloseOnlyAffectsPersistingDeviceAndPreservesClaimedRecord(bool claimed)
        {
            var remote = ClaimRuntimeFixture(out var repository, out var remoteRepository, out string directory);
            try
            {
                var result = CompleteClaimRuntime();
                CooperativeClaim localClaim = result.Claims[repository.SaveProfile()];
                CooperativeClaim remoteClaim = result.Claims[remoteRepository.SaveProfile()];
                PumpUntil(() => localClaim.AckedBindings.Contains(((ClientSessionComponent)host.Client).BindingGeneration) &&
                    remoteClaim.AckedBindings.Contains(remote.BindingGeneration), "Both durable tickets", 350, true);
                ProgressCommitReceipt saved = claimed ? ProgressClaimFiles.Claim(repository, localClaim.TicketId) : null;
                using (var denied = (ProgressCloseMatchResponse)Finish(remote.CallProduct(ProductMessages.Create<ProgressCloseMatchRequest>(request =>
                {
                    request.ProtocolVersion = ProductSessionRules.ProtocolVersion; request.MatchId = remote.MatchId;
                    request.BindingGeneration = remote.BindingGeneration; request.ExpectedRoomVersion = remote.GetComponent<ProductRoomClient>().Current.Version;
                })), 500)) Assert.That(denied.Message, Is.EqualTo("HostControlOnly"));
                ((Session)remote.Session).Dispose();
                Pump(240);
                var local = (ClientSessionComponent)host.Client;
                Match match = ((HostSessionComponent)host.Authority).Match;
                using (var accepted = (ProgressCloseMatchResponse)Finish(local.CallProduct(ProductMessages.Create<ProgressCloseMatchRequest>(request =>
                {
                    request.ProtocolVersion = ProductSessionRules.ProtocolVersion; request.MatchId = local.MatchId;
                    request.BindingGeneration = local.BindingGeneration; request.ExpectedRoomVersion = match.RoomVersion;
                })), 500)) Assert.That(accepted.Error, Is.Zero, accepted.Message);
                var state = match.GetComponent<CooperativeClaimState>();
                PumpUntil(() => state.CloseReady, "Finite normal close after durable observation", 400, true);
                Assert.That(ProgressClaimFiles.Browse(repository).Single().State,
                    Is.EqualTo(claimed ? ProgressReceiptState.Claimed : ProgressReceiptState.Closed));
                Assert.That(ProgressClaimFiles.Browse(remoteRepository).Single().State, Is.EqualTo(ProgressReceiptState.Available));
                Assert.That(repository.Browse().Length, Is.EqualTo(claimed ? 1 : 0));
                if (saved != null)
                {
                    Assert.That(ProgressClaimFiles.Claim(repository, localClaim.TicketId).SlotId, Is.EqualTo(saved.SlotId));
                }
                using var rejected = RoomReply(local, local.CreateRoomCommand(ProductRoomAction.CloseRestaurant, 0));
                Assert.That(rejected.Message, Is.EqualTo("MatchClosing"));
                Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False, "Bounded receipt phase precedes owner teardown");
            }
            finally { remoteRepository.Dispose(); repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("CooperativeClaimsClose")]
        public void CooperativeClaimsReturningProvedProfileGetsSameFrozenTicketOnNewKcpBinding()
        {
            var remote = ClaimRuntimeFixture(out var repository, out var remoteRepository, out string directory);
            try
            {
                LevelDo(LevelControlAction.Start);
                int member = remote.MemberId;
                long binding = remote.BindingGeneration;
                ProductRuntimeComponent runtime = remote.Runtime;
                ((Session)remote.Session).Dispose(); Pump(240);
                var result = CompleteClaimRuntime();
                CooperativeClaim claim = result.Claims[remoteRepository.SaveProfile()];
                string ticketId = claim.TicketId;
                var resumed = runtime.ResumeProduct();
                PumpUntil(() => resumed.Status != UniTaskStatus.Pending, "Return to same member", 500, true);
                resumed.GetAwaiter().GetResult();
                remote = runtime.Client;
                Assert.That(remote.MemberId, Is.EqualTo(member));
                Assert.That(remote.BindingGeneration, Is.GreaterThan(binding));
                ProveFixtureProfile(remote, remoteRepository);
                PumpUntil(() => claim.AckedBindings.Contains(remote.BindingGeneration), "New binding proves same original eligibility", 350, true);
                Assert.That(ProgressClaimFiles.Browse(remoteRepository).Single().TicketId, Is.EqualTo(ticketId));
                Assert.That(result.Claims.Count, Is.EqualTo(2));
                Assert.That(claim.AckedBindings.Contains(binding), Is.False);
            }
            finally { remoteRepository.Dispose(); repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("CooperativeClaimsCompletion")]
        public void CooperativeClaimsTerminalSaveReturnsSameRoomAndCloseTimeoutDoesNotPretendPersistence()
        {
            var remote = ClaimRuntimeFixture(out var repository, out var other, out string directory);
            try
            {
                Match match = ((HostSessionComponent)host.Authority).Match;
                for (int stage = 1; stage <= 3; ++stage)
                {
                    if (stage > 1) { FrontWait(() => Buff.Prepared, 8); BuffDo(Buff.Candidates.First()); }
                    ChapterNaturalComplete();
                    if (stage < 3)
                    {
                        FrontWait(() => Renovation.Prepared, 8);
                        LevelDo(LevelControlAction.SkipRenovation);
                    }
                    PumpUntil(() => ((CooperativeSaveResult)CurrentLevel.ClaimResult)?.SignedSuccess != null, "Terminal or explicit skipped choice freezes result", 200, true);
                    CooperativeSaveResult result = CurrentLevel.ClaimResult;
                    long level = CurrentLevel.Id;
                    using (var accepted = LevelReply(host.Client, RenovationView.CreateLevelCommand(stage == 3 ? LevelControlAction.FinishChapter : LevelControlAction.SaveAndContinue)))
                        Assert.That(accepted.Error, Is.Zero, accepted.Message);
                    AdvanceTicks(1);
                    PumpUntil(() => result.HostSaveCommitted, "Host saves each finite checkpoint", 200, true);
                    AdvanceTicks(2); Pump(30);
                    if (stage < 3)
                    {
                        Assert.That(CurrentLevel.Id, Is.Not.EqualTo(level));
                    }
                }
                PumpUntil(() => (RestaurantRuntime)match.Restaurant == null, "Saved terminal returns room without whole-Match closure", 100, true);
                Assert.That(repository.Browse().Length, Is.EqualTo(3));
                Assert.That(repository.Browse().Count(header => header.RecordKind == SoloSaveRecordKind.ChapterCompleted), Is.EqualTo(1));
                var state = match.GetComponent<CooperativeClaimState>();
                Assert.That(state.Closing, Is.False);
                string blocked = Path.Combine(repository.DirectoryPath, "Closures");
                File.WriteAllText(blocked, "fixture blocks receipt persistence");
                var local = (ClientSessionComponent)host.Client;
                using (var accepted = (ProgressCloseMatchResponse)Finish(local.CallProduct(ProductMessages.Create<ProgressCloseMatchRequest>(request =>
                {
                    request.ProtocolVersion = ProductSessionRules.ProtocolVersion; request.MatchId = local.MatchId;
                    request.BindingGeneration = local.BindingGeneration; request.ExpectedRoomVersion = match.RoomVersion;
                })), 500)) Assert.That(accepted.Error, Is.Zero, accepted.Message);
                PumpUntil(() => state.CloseReady, "Close deadline bounds a live device's storage failure", 700, true);
                CooperativeCloseReceipt ownReceipt = state.CloseReceipts[repository.SaveProfile()];
                Assert.That(ownReceipt.AckedBindings, Is.Empty);
                Assert.That(File.Exists(Path.Combine(repository.DirectoryPath, "Closures", match.MatchId + ".ccclose")), Is.False);
                Assert.That(repository.Browse().Length, Is.EqualTo(3));
                Assert.That(ProgressClaimFiles.Browse(repository).All(summary => summary.State == ProgressReceiptState.Claimed), Is.True);
                Assert.That(remote.Stage, Is.EqualTo(ProductClientStage.Joined));
            }
            finally { other.Dispose(); repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
