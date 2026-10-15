using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(CooperativeSaveBinding))]
    [FriendOf(typeof(SelectedSaveBaseline))]
    public sealed partial class ProductSessionTests
    {
        private ProductProfileResponse ProfileReply(ClientSessionComponent client, int action, byte[] proof = null)
        {
            return (ProductProfileResponse)Finish(client.CallProduct(ProductMessages.Create<ProductProfileRequest>(request =>
            {
                request.ProtocolVersion = ProductSessionRules.ProtocolVersion;
                request.MatchId = client.MatchId;
                request.BindingGeneration = client.BindingGeneration;
                request.Action = action;
                request.Proof = proof;
            })), 500);
        }

        private byte[] ProfileProofFor(ClientSessionComponent client, SoloSaveRepository repository)
        {
            using var challenge = ProfileReply(client, 1);
            Assert.That(challenge.Error, Is.Zero, challenge.Message);
            using var signed = (ProductProfileSignResponse)Finish(client.Root().GetComponent<ProcessInnerSender>().Call(repository.GetActorId(),
                ProductMessages.Create<ProductProfileSignRequest>(request =>
                {
                    request.MatchId = client.MatchId;
                    request.BindingGeneration = client.BindingGeneration;
                    request.Challenge = challenge.Challenge;
                }), false));
            Assert.That(signed.Error, Is.Zero, signed.Message);
            return (byte[])signed.Proof.Clone();
        }

        private void ProveFixtureProfile(ClientSessionComponent client, SoloSaveRepository repository)
        {
            using var proved = ProfileReply(client, 2, ProfileProofFor(client, repository));
            Assert.That(proved.Error, Is.Zero, proved.Message);
            Assert.That(proved.ProfileId, Is.EqualTo(repository.SaveProfile()));
            Pump(30);
        }

        private CooperativeBaselineResponse BaselineReply(ClientSessionComponent client, CooperativeBaselineRequest request)
        {
            return (CooperativeBaselineResponse)Finish(client.CallProduct(request), 500);
        }

        [Test]
        [Category("CooperativeBaseline")]
        [Category("CooperativeBaselineAuthorization")]
        public void CooperativeBaselineActualRsaIdentityPinsKeysRejectsTamperPurposeAndMissingPrivateKey()
        {
            StartRoomFixture();
            var repository = TestSaveRepository(out string directory);
            try
            {
                var own = OfflineProfiles.OwnIdentity(repository);
                Assert.That(own.ProfileId, Is.EqualTo(SaveProfileId));
                Assert.That(own.PublicKeyXml, Does.Not.Contain("<D>"));
                byte[] signed = OfflineProfiles.Sign(repository, OfflineProfiles.ProfileProof, new byte[] { 1, 2, 3 });
                var verified = OfflineProfiles.Verify(signed, OfflineProfiles.ProfileProof, repository);
                Assert.That(verified.Body, Is.EqualTo(new byte[] { 1, 2, 3 }));
                Assert.Throws<InvalidOperationException>(() => OfflineProfiles.Verify(signed, OfflineProfiles.BaselineReadRun, repository));
                byte[] unknownVersion = (byte[])signed.Clone();
                unknownVersion[8] = 2;
                Assert.That(Assert.Throws<InvalidOperationException>(() => OfflineProfiles.Verify(unknownVersion, OfflineProfiles.ProfileProof, repository)).Message,
                    Is.EqualTo("UnsupportedVersion"));
                byte[] wrongPlatform = (byte[])signed.Clone();
                Assert.That(wrongPlatform[12], Is.EqualTo(2));
                wrongPlatform[13] = (byte)'X';
                Assert.That(Assert.Throws<InvalidOperationException>(() => OfflineProfiles.Verify(wrongPlatform, OfflineProfiles.ProfileProof, repository)).Message,
                    Is.EqualTo("WrongPlatform"));
                byte[] altered = (byte[])signed.Clone();
                altered[altered.Length - 1] ^= 1;
                Assert.Throws<InvalidOperationException>(() => OfflineProfiles.Verify(altered, OfflineProfiles.ProfileProof, repository));
                var changed = CopySaveValue(own);
                changed.KeyId = Guid.NewGuid().ToString("N");
                Assert.That(Assert.Throws<InvalidOperationException>(() => OfflineProfiles.TrustIdentity(repository, changed)).Message, Is.EqualTo("ProfileKeyConflict"));
                string privatePath = Path.Combine(repository.DirectoryPath, "Identity", "own.cckey");
                File.Move(privatePath, privatePath + ".owned-test-held");
                Assert.That(Assert.Throws<InvalidOperationException>(() => OfflineProfiles.OwnIdentity(repository)).Message, Is.EqualTo("ProfilePrivateKeyMissing"));
                Assert.That(repository.Browse(), Is.Empty);
                Assert.That(File.Exists(privatePath), Is.False);
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("CooperativeBaseline")]
        public void CooperativeBaselineRealKcpProofRequiresTrustedKeyAndFreshBindingChallenge()
        {
            StartRoomFixture();
            var repository = TestSaveRepository(out string directory);
            SoloSaveRepository other = null;
            try
            {
                OfflineProfiles.OwnIdentity(repository);
                var remote = JoinRoomFixture();
                other = remote.Root().AddComponent<SoloSaveRepository, string, string>(directory, Guid.NewGuid().ToString("N"));
                var identity = OfflineProfiles.OwnIdentity(other);
                byte[] proof = ProfileProofFor(remote, other);
                using (var untrusted = ProfileReply(remote, 2, proof))
                {
                    Assert.That(untrusted.Message, Is.EqualTo("ProfileNotTrusted"));
                }
                OfflineProfiles.TrustIdentity(repository, identity);
                using (var proved = ProfileReply(remote, 2, proof))
                {
                    Assert.That(proved.Error, Is.Zero, proved.Message);
                }
                using (var duplicate = ProfileReply(remote, 2, proof))
                {
                    Assert.That(duplicate.Message, Is.EqualTo("ProfileChallengeRequired"));
                }
                byte[] first = ProfileProofFor(remote, other);
                using (var next = ProfileReply(remote, 1))
                {
                    Assert.That(next.Error, Is.Zero);
                }
                using (var stale = ProfileReply(remote, 2, first))
                {
                    Assert.That(stale.Message, Is.EqualTo("ProfileChallengeChanged"));
                }
                ProveFixtureProfile(remote, other);
                Match match = ((HostSessionComponent)host.Authority).Match;
                Assert.That(match.GetChild<Participant>(remote.MemberId).ProfileId, Is.EqualTo(identity.ProfileId));
                Assert.That(remote.GetComponent<ProductRoomClient>().Current.VerifiedProfileId, Is.EqualTo(identity.ProfileId));
                Assert.That((RestaurantRuntime)match.Restaurant, Is.Null);
                int member = remote.MemberId;
                long binding = remote.BindingGeneration;
                ProductRuntimeComponent remoteRuntime = remote.Runtime;
                ((Session)remote.Session).Dispose();
                Pump(240);
                Assert.That(remoteRuntime.Stage, Is.EqualTo(ProductStage.Disconnected));
                var resume = remoteRuntime.ResumeProduct();
                PumpUntil(() => resume.Status != UniTaskStatus.Pending, "Resume empty room preserves member but not binding proof", 500, advanceTime: true);
                resume.GetAwaiter().GetResult();
                remote = remoteRuntime.Client;
                PumpUntil(() => remote.GetComponent<ProductRoomClient>().Current.Version > 0, "Resumed room projection", 100, advanceTime: true);
                Assert.That(remote.MemberId, Is.EqualTo(member));
                Assert.That(remote.BindingGeneration, Is.GreaterThan(binding));
                Assert.That(match.GetChild<Participant>(member).ProfileId, Is.EqualTo(identity.ProfileId));
                Assert.That(remote.GetComponent<ProductRoomClient>().Current.VerifiedProfileId, Is.Null);
                Assert.That(((HostPeer)match.GetChild<Participant>(member).BoundPeer).ProfileProved, Is.False);
                using (var issued = ProfileReply(remote, 1))
                {
                    Assert.That(issued.Error, Is.Zero);
                }
                using (var oldBinding = ProfileReply(remote, 2, proof))
                {
                    Assert.That(oldBinding.Message, Is.EqualTo("ProfileChallengeChanged"));
                }
                ProveFixtureProfile(remote, other);
                Assert.That(remote.GetComponent<ProductRoomClient>().Current.VerifiedProfileId, Is.EqualTo(identity.ProfileId));
            }
            finally { other?.Dispose(); repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("CooperativeBaseline")]
        [Category("CooperativeBaselineLoad")]
        [Category("CooperativeBaselineAuthorization")]
        public void CooperativeBaselineForeignOwnerActualFileLoadsOnceThroughHostAndKcpWithoutChangingFile(bool actualLegacy)
        {
            StartSolo();
            var payload = CompleteForSave();
            var header = SaveHeader();
            var owner = TestSaveRepository(out string directory);
            SoloSaveRepository repository = null;
            try
            {
                if (actualLegacy)
                {
                    header.Version = 1;
                    header.CompletedLevelId = 1;
                    header.NextLevelId = 2;
                    var legacy = new SoloChapterLegacyHeader
                    {
                        SlotId = header.SlotId, ProfileId = header.ProfileId, ResultId = header.ResultId,
                        CompletedLevelId = 1, NextLevelId = 2, SavedUtcTicks = header.SavedUtcTicks
                    };
                    byte[] head = MemoryPackHelper.Serialize(legacy), body = MemoryPackHelper.Serialize(payload);
                    using var memory = new MemoryStream();
                    using (var writer = new BinaryWriter(memory, System.Text.Encoding.UTF8, true))
                    {
                        writer.Write(System.Text.Encoding.ASCII.GetBytes("CCSOLO01"));
                        writer.Write(head.Length);
                        writer.Write(body.Length);
                        writer.Write(head);
                        writer.Write(body);
                    }
                    File.WriteAllBytes(Path.Combine(owner.DirectoryPath, header.SlotId + ".ccsave"), memory.ToArray());
                }
                else
                {
                    owner.Commit(header, payload);
                }
                var ownerIdentity = OfflineProfiles.OwnIdentity(owner);
                byte[] original = File.ReadAllBytes(Path.Combine(owner.DirectoryPath, header.SlotId + ".ccsave"));
                string oldWorld = Grid.WorldId;
                host.StopProduct();
                Game.RuntimeInstanceContext.Reset();
                StartRoomFixture();
                repository = host.Root().AddComponent<SoloSaveRepository, string, string>(directory, Guid.NewGuid().ToString("N"));
                var hostIdentity = OfflineProfiles.OwnIdentity(repository);
                OfflineProfiles.TrustIdentity(repository, ownerIdentity);
                OfflineProfiles.TrustIdentity(owner, hostIdentity);
                var local = (ClientSessionComponent)host.Client;
                ProveFixtureProfile(local, repository);
                var remote = JoinRoomFixture();
                byte[] authorization = CooperativeBaselineSystem.Authorize(owner, header.SlotId, local.MatchId, hostIdentity);
                using (var denied = BaselineReply(remote, remote.CreateBaselineCommand(authorization)))
                {
                    Assert.That(denied.Message, Is.EqualTo("HostControlOnly"));
                }
                byte[] altered = (byte[])authorization.Clone();
                altered[altered.Length - 1] ^= 1;
                using (var denied = BaselineReply(local, local.CreateBaselineCommand(altered)))
                {
                    Assert.That(denied.Message, Is.EqualTo("InvalidSignature"));
                }
                Assert.That(Grid, Is.Null);
                var request = local.CreateBaselineCommand(authorization);
                var replay = CopyReplayRequest(request);
                var conflict = CopyReplayRequest(request);
                conflict.Authorization = CooperativeBaselineSystem.Authorize(owner, header.SlotId, local.MatchId, hostIdentity);
                using (var loaded = BaselineReply(local, request))
                {
                    Assert.That(loaded.Error, Is.Zero, loaded.Message);
                    Assert.That(loaded.Status, Is.EqualTo((int)GridResultStatus.Executed));
                }
                PumpUntil(() => remote.GetComponent<GridClientComponent>().Confirmed, "KCP confirms only restored world", 100, advanceTime: true);
                string restoredWorld = Grid.WorldId;
                Assert.That(restoredWorld, Is.Not.EqualTo(oldWorld));
                Assert.That(Grid.Items.Count, Is.EqualTo(payload.Items.Length));
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(header.NextLevelId));
                var room = remote.GetComponent<ProductRoomClient>().Current;
                Assert.That(room.BaselineOwnerId, Is.EqualTo(ownerIdentity.ProfileId));
                Assert.That(room.BaselineOwnerId, Is.Not.EqualTo(hostIdentity.ProfileId));
                Assert.That(room.BaselineSlotId, Is.EqualTo(header.SlotId));
                Assert.That(room.BaselineRevision, Is.EqualTo(1));
                Assert.That(remote.GetComponent<GridClientComponent>().Items.Count, Is.EqualTo(payload.Items.Length));
                using (var repeated = BaselineReply(local, replay))
                {
                    Assert.That(repeated.Status, Is.EqualTo((int)GridResultStatus.Executed));
                }
                using (var different = BaselineReply(local, conflict))
                {
                    Assert.That(different.Message, Is.EqualTo("CommandConflict"));
                }
                var oldRoom = local.CreateBaselineCommand(authorization);
                --oldRoom.ExpectedRoomVersion;
                using (var stale = BaselineReply(local, oldRoom))
                {
                    Assert.That(stale.Message, Is.EqualTo("RoomChanged"));
                }
                using (var existing = BaselineReply(local, local.CreateBaselineCommand(authorization)))
                {
                    Assert.That(existing.Message, Is.EqualTo("RestaurantAlreadyOpen"));
                }
                Assert.That(Grid.WorldId, Is.EqualTo(restoredWorld));
                Assert.That(File.ReadAllBytes(Path.Combine(owner.DirectoryPath, header.SlotId + ".ccsave")), Is.EqualTo(original));
                Assert.That(repository.Browse(), Is.Empty);
                RoomDo(ProductRoomAction.CloseRestaurant);
                Assert.That(((Match)((HostSessionComponent)host.Authority).Match).GetComponent<CooperativeSaveBinding>(), Is.Null);
                RoomDo(ProductRoomAction.StartRestaurant);
                Assert.That(Grid.WorldId, Is.Not.EqualTo(restoredWorld));
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(301));
                Assert.That(((Match)((HostSessionComponent)host.Authority).Match).Children.Values.OfType<SelectedSaveBaseline>(), Is.Empty);
            }
            finally { repository?.Dispose(); owner.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("CooperativeBaseline")]
        [Category("CooperativeBaselineLoad")]
        public void CooperativeBaselineRestoresHalfBatchTransitAndUpgradedFrozenBakeWithoutRefill()
        {
            StartSolo();
            var originalClient = (ClientSessionComponent)host.Client;
            long pot = SoupFillPot(originalClient);
            var batch = SoupBegin(originalClient, pot);
            AdvanceTicks(batch.RequiredTicks);
            PumpMessages(8);
            var bowlStation = PrepStation("碗台");
            long bowl = bowlStation.ItemId;
            PrepDo(originalClient, GridAction.TakeCounter, bowlStation.Id, bowl);
            PrepDo(originalClient, GridAction.PortionSoup, PrepStation("灶台").Id, bowl, pot, batch.Id);
            PrepDo(originalClient, GridAction.PlaceCounter, bowlStation.Id, bowl);
            RenovationComplete();
            UpgradeChoose(4, PrepStation("烤箱"));
            LevelDo(LevelControlAction.Continue);
            var oven = PrepStation("烤箱");
            var bread = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => Grid.Item(value.ItemId)?.DefinitionId == GridKitchenRules.Bread);
            long breadId = bread.ItemId;
            PrepDo(originalClient, GridAction.TakeCounter, bread.Id, breadId);
            PrepDo(originalClient, GridAction.PlaceCounter, oven.Id, breadId);
            PrepDo(originalClient, GridAction.StartBake, oven.Id, breadId, breadId);
            var baking = Grid.BakeProcesses.Values.Select(value => (GridBakeProcess)value).Single();
            baking.RequiredTicks = 1600;
            CurrentLevel.GetComponent<FrontLevelState>().NextArrivalOperatingTick = 100000;
            LevelDo(LevelControlAction.Start);
            SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id));
            CurrentLevel.ElapsedOperatingTicks = CurrentLevel.OperatingTicks - 1;
            AdvanceTicks(3);
            PumpMessages(8);
            RenovationChoose(1);
            var payload = SoloSaveCapture.Kitchen(Restaurant);
            var header = SaveHeader();
            Assert.That(payload.Batches.Single().RemainingPortions, Is.EqualTo(1));
            Assert.That(payload.Shipments.Single().Stage, Is.EqualTo((int)ShipmentStage.InTransit));
            var repository = TestSaveRepository(out string directory);
            try
            {
                repository.Commit(header, payload);
                OfflineProfiles.OwnIdentity(repository);
                repository.Dispose();
                host.StopProduct();
                Game.RuntimeInstanceContext.Reset();
                StartRoomFixture();
                repository = host.Root().AddComponent<SoloSaveRepository, string, string>(directory, SaveProfileId);
                var identity = OfflineProfiles.OwnIdentity(repository);
                var client = (ClientSessionComponent)host.Client;
                ProveFixtureProfile(client, repository);
                var remote = JoinRoomFixture();
                byte[] authorization = CooperativeBaselineSystem.Authorize(repository, header.SlotId, client.MatchId, identity);
                using (var loaded = BaselineReply(client, client.CreateBaselineCommand(authorization)))
                {
                    Assert.That(loaded.Error, Is.Zero, loaded.Message);
                }
                PumpUntil(() => client.GetComponent<GridClientComponent>().Confirmed,
                    "Local restored world confirms before level controls", 100, advanceTime: true);
                LevelDo(LevelControlAction.Pause);
                PumpUntil(() => remote.GetComponent<GridClientComponent>().Confirmed && remote.GetComponent<GridClientComponent>().LevelPaused,
                    "KCP sees restored frozen kitchen", 100, advanceTime: true);
                var restoredBatch = Grid.AutoProcesses.Values.Select(value => (GridAutoProcess)value).Single();
                var restoredBake = Grid.BakeProcesses.Values.Select(value => (GridBakeProcess)value).Single();
                var shipment = Supply.Children.Values.OfType<SupplyShipment>().Single();
                Assert.That(restoredBatch.RemainingPortions, Is.EqualTo(1));
                Assert.That(restoredBatch.RequiredTicks, Is.EqualTo(payload.Batches.Single().RequiredTicks));
                Assert.That(restoredBake.RequiredTicks, Is.EqualTo(1600));
                Assert.That(restoredBake.ProgressTicks, Is.InRange(payload.Baking.Single().ProgressTicks, payload.Baking.Single().ProgressTicks + 2));
                Assert.That(Grid.Station(restoredBake.StationId).UpgradeLevel, Is.EqualTo(1));
                Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.InTransit));
                Assert.That(shipment.ProgressTicks, Is.InRange(payload.Shipments.Single().ProgressTicks, payload.Shipments.Single().ProgressTicks + 2));
                Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4));
                Assert.That(Grid.Items.Count, Is.EqualTo(payload.Items.Length));
                Assert.That(remote.GetComponent<GridClientComponent>().AutoProcesses[restoredBatch.Id].RemainingPortions, Is.EqualTo(1));
                Assert.That(remote.GetComponent<GridClientComponent>().BakeProcesses[restoredBake.Id].RequiredTicks, Is.EqualTo(1600));
                Assert.That(repository.Browse().Length, Is.EqualTo(1));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
