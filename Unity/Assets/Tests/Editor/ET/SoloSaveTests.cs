using System;
using System.IO;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace ET
{
    [FriendOf(typeof(SoloSaveRepository))]
    public sealed partial class ProductSessionTests
    {
        private string SaveProfileId => "0123456789abcdef0123456789abcdef";
        private SoloSaveHeader SaveHeader() => new()
        {
            SlotId = Guid.NewGuid().ToString("N"), ProfileId = SaveProfileId, ResultId = Guid.NewGuid().ToString("N"),
            CompletedLevelId = CurrentLevel.ConfigId, NextLevelId = CurrentLevel.NextLevelConfigId,
            Score = CurrentLevel.Score, Stars = CurrentLevel.Stars, SavedUtcTicks = DateTime.UtcNow.Ticks
        };

        private SoloSavePayload CompleteForSave()
        {
            RenovationComplete(); RenovationChoose(Renovation.Candidates.First());
            return SoloSaveCapture.Kitchen(Restaurant);
        }

        private SoloSaveRepository TestSaveRepository(out string testDirectory)
        {
            testDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/solo-save-tests", Guid.NewGuid().ToString("N")));
            return host.Root().AddComponent<SoloSaveRepository, string, string>(testDirectory, SaveProfileId);
        }

        private static void RemoveOwnSaveTestDirectory(string directory)
        {
            string parent = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/solo-save-tests"));
            Assert.That(Path.GetDirectoryName(Path.GetFullPath(directory)), Is.EqualTo(parent));
            if (!Directory.Exists(directory)) return;
            // Only files and empty directories created by this one fixture; no recursive deletion.
            foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)) File.Delete(file);
            foreach (string child in Directory.EnumerateDirectories(directory, "*", SearchOption.AllDirectories).OrderByDescending(s => s.Length)) Directory.Delete(child);
            Directory.Delete(directory);
        }

        private static T CopySaveValue<T>(T value) where T : class
        {
            byte[] bytes = MemoryPackHelper.Serialize(value);
            return (T)MemoryPackHelper.Deserialize(typeof(T), bytes, 0, bytes.Length);
        }

        [Test]
        public void SoloSavePayloadNaturalZeroStarCaptureUsesSaveKeysAndDoesNotDropActualHand()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            long bowl = PrepEggBowl(client), actorId = Grid.Actor(client.MemberId).Id;
            SoloSavePayload payload = CompleteForSave(); var header = SaveHeader();
            Assert.That(header.Stars, Is.Zero); Assert.That(payload.PendingRenovationId, Is.EqualTo(1));
            SoloSaveValidation.Payload(header, payload, SaveProfileId);
            Assert.That(Grid.Item(bowl).Location, Is.EqualTo(GridItemLocation.Hand));
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(bowl));
            var savedBowl = payload.Items.Single(item => item.DefinitionId == GridKitchenRules.Bowl && item.ContentKeys.Length == 1);
            Assert.That(savedBowl.Location, Is.EqualTo((int)GridItemLocation.Ground)); Assert.That(savedBowl.HolderKey, Is.Zero);
            Assert.That(payload.Items.Single(i => i.Key == savedBowl.ContentKeys.Single()).HolderKey, Is.EqualTo(savedBowl.Key));
            Assert.That(payload.Items.Any(i => i.Key == bowl || i.Key == actorId), Is.False);
            using var stream = new MemoryStream(SoloSaveFile.Encode(header, payload));
            var copy = SoloSaveFile.ReadPayload(stream, SaveProfileId, out var copiedHeader);
            Assert.That(copiedHeader.ResultId, Is.EqualTo(header.ResultId)); Assert.That(copy.Items.Length, Is.EqualTo(Grid.Items.Count));
            Assert.That(copy.Supply.All(s => s.RemainingPackages == 5), Is.True);
        }

        [TestCase("version")]
        [TestCase("platform")]
        [TestCase("owner")]
        [TestCase("next")]
        [TestCase("source")]
        [TestCase("location")]
        [TestCase("duplicate")]
        [TestCase("holder")]
        [TestCase("capacity")]
        [TestCase("missingSupply")]
        [TestCase("quota")]
        [TestCase("overlap")]
        public void SoloSavePayloadInvalidValuesRejectWithoutChangingKitchen(string fault)
        {
            StartSolo(); SoloSavePayload payload = CompleteForSave(); var header = SaveHeader();
            long[] ids = Grid.Items.Keys.ToArray(); long level = CurrentLevel.Id;
            var bowl = payload.Items.First(i => i.DefinitionId == GridKitchenRules.Bowl);
            switch (fault)
            {
                case "version": header.Version = 99; break;
                case "platform": header.Platform = "Android"; break;
                case "owner": header.ProfileId = Guid.NewGuid().ToString("N"); break;
                case "next": header.NextLevelId = int.MaxValue; break;
                case "source": header.SourceSlotId = "../foreign"; break;
                case "location": bowl.Location = (int)GridItemLocation.Hand; break;
                case "duplicate": payload.Items[1].Key = payload.Items[0].Key; break;
                case "holder": bowl.HolderKey = 9000; break;
                case "capacity": bowl.ContentKeys = new[] { payload.Items[0].Key, payload.Items[0].Key }; break;
                case "missingSupply": payload.Supply = Array.Empty<SoloSavedSupplyBalance>(); break;
                case "quota": payload.Supply[0].RemainingPackages--; break;
                case "overlap": payload.Stations[1].X = payload.Stations[0].X; payload.Stations[1].Y = payload.Stations[0].Y; break;
            }
            Assert.Throws<InvalidOperationException>(() => SoloSaveValidation.Payload(header, payload, SaveProfileId));
            Assert.That(Grid.Items.Keys, Is.EqualTo(ids)); Assert.That(CurrentLevel.Id, Is.EqualTo(level));
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed));
        }

        [Test]
        public void SoloSavePayloadRetainsPausedHeldWorkFrozenTicksAndActualTransitReservation()
        {
            StartSolo(); ClientSessionComponent client = host.Client; long bowl = PrepEggBowl(client), egg = Grid.Item(bowl).Contents.Single();
            LevelDo(LevelControlAction.Start);
            PrepDo(client, GridAction.StartHeldWork, bowl, egg, bowl);
            GridManualProcess work = Grid.InputProcess(egg); work.RequiredTicks = 320; AdvanceTicks(7);
            int source = TomatoSupply.Id; SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: source));
            CurrentLevel.ElapsedOperatingTicks = CurrentLevel.OperatingTicks - 1;
            AdvanceTicks(3); PumpMessages(8);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed)); Assert.That(work.State, Is.EqualTo(GridWorkState.Paused));
            RenovationChoose(1); var payload = SoloSaveCapture.Kitchen(Restaurant); var header = SaveHeader();
            SoloSaveValidation.Payload(header, payload, SaveProfileId);
            var savedWork = payload.ManualWork.Single(); Assert.That(savedWork.RequiredTicks, Is.EqualTo(320));
            Assert.That(savedWork.ProgressTicks, Is.EqualTo(work.ProgressTicks));
            var shipment = payload.Shipments.Single(); Assert.That(shipment.Stage, Is.EqualTo((int)ShipmentStage.InTransit));
            Assert.That(shipment.ProgressTicks, Is.EqualTo(Supply.Children.Values.OfType<SupplyShipment>().Single().ProgressTicks));
            Assert.That(payload.Supply.Single(s => s.SourceId == source).RemainingPackages, Is.EqualTo(4));
            var invalid = CopySaveValue(payload); invalid.ManualWork[0].ProgressTicks = 320;
            Assert.Throws<InvalidOperationException>(() => SoloSaveValidation.Payload(header, invalid, SaveProfileId));
            invalid = CopySaveValue(payload); invalid.Shipments[0].ReceivingStationKey = 9999;
            Assert.Throws<InvalidOperationException>(() => SoloSaveValidation.Payload(header, invalid, SaveProfileId));
        }

        [Test]
        public void SoloSavePayloadHalfSoupHygieneAndAppliedRenovationRemainPureValues()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            long pot = SoupFillPot(client); var process = SoupBegin(client, pot);
            AdvanceTicks(process.RequiredTicks); PumpMessages(8);
            var station = PrepStation("碗台"); long bowl = station.ItemId;
            PrepDo(client, GridAction.TakeCounter, station.Id, bowl);
            PrepDo(client, GridAction.PortionSoup, PrepStation("灶台").Id, bowl, pot, process.Id);
            Assert.That(process.RemainingPortions, Is.EqualTo(1));
            CompleteForSave(); LevelDo(LevelControlAction.Continue);
            var empty = Grid.Items.Values.Select(value => (GridItem)value).First(i => i.DefinitionId == GridKitchenRules.Bowl && i.Contents.Count == 0);
            empty.Hygiene = ItemHygiene.Dirty;
            var payload = CompleteForSave(); var header = SaveHeader();
            SoloSaveValidation.Payload(header, payload, SaveProfileId);
            var batch = payload.Batches.Single(); Assert.That(batch.RemainingPortions, Is.EqualTo(1));
            Assert.That(batch.ProgressTicks, Is.EqualTo(process.ProgressTicks)); Assert.That(batch.RequiredTicks, Is.EqualTo(process.RequiredTicks));
            Assert.That(payload.Items.Count(i => i.Hygiene == (int)ItemHygiene.Dirty), Is.EqualTo(1));
            Assert.That(payload.Renovations.Single().DefinitionId, Is.EqualTo(1)); Assert.That(payload.PendingRenovationId, Is.EqualTo(2));
            batch.RemainingPortions = batch.TotalPortions + 1;
            Assert.Throws<InvalidOperationException>(() => SoloSaveValidation.Payload(header, payload, SaveProfileId));
        }

        [Test]
        public void SoloSavePayloadCorruptEnvelopeAndMissingDataRejectClearly()
        {
            StartSolo(); var payload = CompleteForSave(); byte[] bytes = SoloSaveFile.Encode(SaveHeader(), payload);
            using (var truncated = new MemoryStream(bytes.Take(bytes.Length - 1).ToArray()))
                Assert.Throws<InvalidOperationException>(() => SoloSaveFile.ReadPayload(truncated, SaveProfileId, out _));
            bytes[0] = 0;
            using (var damaged = new MemoryStream(bytes)) Assert.Throws<InvalidOperationException>(() => SoloSaveFile.ReadPayload(damaged, SaveProfileId, out _));
            payload.Items = null;
            Assert.Throws<InvalidOperationException>(() => SoloSaveValidation.Payload(SaveHeader(), payload, SaveProfileId));
        }

        [Test]
        public void SoloSaveRepositoryRealNewFileRoundTripRetryAfterResponseLossAndStableProfile()
        {
            StartSolo(); var payload = CompleteForSave(); var header = SaveHeader(); var repository = TestSaveRepository(out string directory);
            try
            {
                string profile = SoloSaveRepositorySystem.LocalProfile(directory);
                Assert.That(SoloSaveRepositorySystem.LocalProfile(directory), Is.EqualTo(profile));
                var sender = host.Root().GetComponent<ProcessInnerSender>(); byte[] record = SoloSaveFile.Encode(header, payload);
                var task = sender.Call(repository.GetActorId(), ProductMessages.Create<SoloSaveWriteRequest>(r => r.Record = record), false);
                PumpMessages(8); Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending));
                using (var receipt = (SoloSaveWriteResponse)task.GetAwaiter().GetResult()) Assert.That(receipt.Error, Is.Zero, receipt.Message);
                // A different suggested SlotId still resolves the original success, including after reopening.
                header.SlotId = Guid.NewGuid().ToString("N"); repository.Dispose();
                repository = host.Root().AddComponent<SoloSaveRepository, string, string>(directory, SaveProfileId);
                var result = repository.Commit(header, payload); Assert.That(result.SlotId, Is.Not.EqualTo(header.SlotId));
                Assert.That(Directory.GetFiles(repository.DirectoryPath, "*.ccsave").Length, Is.EqualTo(1));
                var load = repository.Select(result.SlotId, out var loadedHeader);
                Assert.That(loadedHeader.ResultId, Is.EqualTo(header.ResultId)); Assert.That(load.Items.Length, Is.EqualTo(payload.Items.Length));
                Assert.That(Directory.GetFiles(repository.DirectoryPath, "*.tmp"), Is.Empty);
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        public void SoloSaveRepositoryConflictAndPublicationFailurePreserveOldBytesAndCleanOnlyOwnTemporary()
        {
            StartSolo(); var payload = CompleteForSave(); var header = SaveHeader(); var repository = TestSaveRepository(out string directory);
            try
            {
                repository.Commit(header, payload); string oldPath = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                byte[] oldBytes = File.ReadAllBytes(oldPath); var conflict = CopySaveValue(header); conflict.ResultId = Guid.NewGuid().ToString("N");
                Assert.Throws<IOException>(() => repository.Commit(conflict, payload));
                Assert.That(File.ReadAllBytes(oldPath), Is.EqualTo(oldBytes));
                var fail = CopySaveValue(header); fail.ResultId = Guid.NewGuid().ToString("N"); fail.SlotId = Guid.NewGuid().ToString("N");
                string blockedTarget = Path.Combine(repository.DirectoryPath, fail.SlotId + ".ccsave"); Directory.CreateDirectory(blockedTarget);
                string foreignTemp = Path.Combine(repository.DirectoryPath, ".foreign.tmp"); File.WriteAllText(foreignTemp, "do not remove");
                Assert.Throws<IOException>(() => repository.Commit(fail, payload));
                Assert.That(Directory.Exists(blockedTarget), Is.True); Assert.That(File.ReadAllText(foreignTemp), Is.EqualTo("do not remove"));
                Assert.That(Directory.GetFiles(repository.DirectoryPath, ".save-*.tmp"), Is.Empty);
                Assert.That(File.ReadAllBytes(oldPath), Is.EqualTo(oldBytes)); Assert.That(repository.Browse().Length, Is.EqualTo(1));
                Assert.Throws<InvalidOperationException>(() => repository.Select("../" + header.SlotId, out _));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        public void SoloSaveRepositoryBrowseReadsOnlyHeaderAndSelectRejectsDamagedPayloadWithoutWorldCreation()
        {
            StartSolo(); var payload = CompleteForSave(); var header = SaveHeader(); var repository = TestSaveRepository(out string directory);
            try
            {
                repository.Commit(header, payload); string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                long oldWorld = Grid.WorldGeneration; int count = Grid.Items.Count;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    SoloSaveFile.ReadHeader(stream); stream.WriteByte(255); // Invalid MemoryPack object member count.
                }
                Assert.That(repository.Browse().Single().SlotId, Is.EqualTo(header.SlotId));
                Assert.Catch(() => repository.Select(header.SlotId, out _));
                Assert.That(Grid.WorldGeneration, Is.EqualTo(oldWorld)); Assert.That(Grid.Items.Count, Is.EqualTo(count));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        private void StartSavedFixture(SoloSaveHeader header, SoloSavePayload payload, bool join = true)
        {
            var config = Game.RuntimeInstanceConfig.Create("solo-save-restore-test", "test", "solo", null);
            Game.RuntimeInstanceContext.Reset(); Game.RuntimeInstanceContext.Prepare(config); var fiber = CreateFiber();
            host = fiber.Root.AddComponent<ProductRuntimeComponent, Game.RuntimeInstanceConfig>(config);
            host.InitializeSavedProduct(SaveProfileId, header, payload, join);
        }

        [Test]
        public void SoloSaveRestoreRealFileRebuildsNewIdentityAndRemapsContainersSupplyAndRenovationExactlyOnce()
        {
            StartSolo(); ClientSessionComponent oldClient = host.Client; var oldProjection = oldClient.GetComponent<GridClientComponent>();
            long bowl = PrepEggBowl(oldClient), egg = Grid.Item(bowl).Contents.Single();
            LevelDo(LevelControlAction.Start); PrepDo(oldClient, GridAction.StartHeldWork, bowl, egg, bowl);
            var work = Grid.InputProcess(egg); work.RequiredTicks = 320; AdvanceTicks(7);
            SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id));
            CurrentLevel.ElapsedOperatingTicks = CurrentLevel.OperatingTicks - 1; AdvanceTicks(3); PumpMessages(8);
            RenovationChoose(1); var header = SaveHeader(); var payload = SoloSaveCapture.Kitchen(Restaurant);
            string worldId = Grid.WorldId; long generation = Grid.WorldGeneration, levelId = CurrentLevel.Id;
            long[] oldItems = Grid.Items.Keys.ToArray(); var oldCommand = oldProjection.CreateLevelCommand(LevelControlAction.Continue);
            var repository = TestSaveRepository(out string directory);
            try
            {
                repository.Commit(header, payload); var selected = repository.Select(header.SlotId, out var selectedHeader);
                host.StopProduct(); StartSavedFixture(selectedHeader, selected);
                Assert.That(Grid.WorldId, Is.Not.EqualTo(worldId)); Assert.That(Grid.WorldGeneration, Is.Not.EqualTo(generation));
                Assert.That(CurrentLevel.Id, Is.Not.EqualTo(levelId)); Assert.That(CurrentLevel.ConfigId, Is.EqualTo(header.NextLevelId));
                Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing)); Assert.That(Grid.Tick, Is.Zero);
                Assert.That(Grid.Items.Count, Is.EqualTo(oldItems.Length)); Assert.That(Grid.Items.Keys.Intersect(oldItems), Is.Empty);
                Assert.That(Grid.Stations.Count, Is.EqualTo(17)); Assert.That(Renovation.AppliedStations.Keys, Is.EqualTo(new[] { 1 }));
                var restoredBowl = Grid.Items.Values.Select(value => (GridItem)value).Single(i => i.DefinitionId == GridKitchenRules.Bowl && i.Contents.Count == 1);
                Assert.That(restoredBowl.Location, Is.EqualTo(GridItemLocation.Ground));
                Assert.That(Grid.Item(restoredBowl.Contents.Single()).HolderId, Is.EqualTo(restoredBowl.Id));
                var restoredWork = Grid.Processes.Values.Select(value => (GridManualProcess)value).Single();
                Assert.That(restoredWork.RequiredTicks, Is.EqualTo(320)); Assert.That(restoredWork.ProgressTicks, Is.EqualTo(payload.ManualWork.Single().ProgressTicks));
                Assert.That(restoredWork.State, Is.EqualTo(GridWorkState.Paused)); Assert.That(restoredWork.WorkerMemberId, Is.Zero);
                var shipment = Supply.Children.Values.OfType<SupplyShipment>().Single();
                Assert.That(shipment.ProgressTicks, Is.EqualTo(payload.Shipments.Single().ProgressTicks));
                Assert.That(shipment.RequiredTicks, Is.EqualTo(payload.Shipments.Single().RequiredTicks));
                Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4));
                Assert.That(Grid.Station(shipment.ReceivingStationId).InitialLayoutId, Is.EqualTo(TomatoSupply.ReceivingLayoutId));
                PumpMessages(8); AdvanceTicks(1); PumpMessages(8);
                using var reply = LevelReply(host.Client, oldCommand); Assert.That(reply.Error, Is.Not.Zero);
                Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing)); Assert.That(Grid.Stations.Count, Is.EqualTo(17));
                Assert.That(Grid.Items.Count, Is.EqualTo(oldItems.Length)); Assert.That(shipment.ProgressTicks, Is.EqualTo(payload.Shipments.Single().ProgressTicks + 1));
                AdvanceTicks(shipment.RequiredTicks); PumpMessages(8);
                Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Ready)); Assert.That(Grid.Items.Count, Is.EqualTo(oldItems.Length));
                ClientSessionComponent restoredClient = host.Client; var receiver = Grid.Station(shipment.ReceivingStationId);
                var spareStation = Grid.Stations.Values.Select(value => (GridStation)value).First(s => Grid.Item(s.ItemId)?.DefinitionId == GridKitchenRules.Bowl && Grid.Item(s.ItemId).Contents.Count == 0);
                long spareBowl = spareStation.ItemId;
                PrepDo(restoredClient, GridAction.TakeCounter, spareStation.Id, spareBowl);
                PrepDo(restoredClient, GridAction.PlaceCounter, receiver.Id, spareBowl);
                SupplyDo(ReceiveIntent(shipment), "ReceivingFull"); Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Ready));
                PrepDo(restoredClient, GridAction.TakeCounter, receiver.Id, spareBowl);
                PrepDo(restoredClient, GridAction.PlaceCounter, spareStation.Id, spareBowl);
                SupplyDo(ReceiveIntent(shipment)); Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.Received));
                Assert.That(Grid.Items.Count, Is.EqualTo(oldItems.Length + shipment.PackageUnits + 1));
                Assert.That(Supply.RemainingPackages[TomatoSupply.Id], Is.EqualTo(4));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        public void SoloSaveRestoreHalfSoupAndBakingFrozenProgressDoNotRefillOrRecalculate()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            long pot = SoupFillPot(client); var batch = SoupBegin(client, pot); AdvanceTicks(batch.RequiredTicks); PumpMessages(8);
            GridStation bowlStation = PrepStation("碗台"); long bowl = bowlStation.ItemId;
            PrepDo(client, GridAction.TakeCounter, bowlStation.Id, bowl);
            PrepDo(client, GridAction.PortionSoup, PrepStation("灶台").Id, bowl, pot, batch.Id);
            PrepDo(client, GridAction.PlaceCounter, bowlStation.Id, bowl);
            CompleteForSave(); LevelDo(LevelControlAction.Continue);
            var emptyBowl = Grid.Items.Values.Select(value => (GridItem)value).First(i => i.DefinitionId == GridKitchenRules.Bowl && i.Contents.Count == 0);
            emptyBowl.Hygiene = ItemHygiene.Dirty;
            GridStation oven = Grid.Stations.Values.Select(value => (GridStation)value).Single(s => s.Kind == GridStationKind.Oven);
            GridStation breadStation = Grid.Stations.Values.Select(value => (GridStation)value).Single(s => Grid.Item(s.ItemId)?.DefinitionId == GridKitchenRules.Bread);
            long bread = breadStation.ItemId;
            PrepDo(client, GridAction.TakeCounter, breadStation.Id, bread);
            PrepDo(client, GridAction.PlaceCounter, oven.Id, bread);
            PrepDo(client, GridAction.StartBake, oven.Id, bread, bread);
            var baking = Grid.BakeProcesses.Values.Select(value => (GridBakeProcess)value).Single(); baking.RequiredTicks = 1600;
            var payload = CompleteForSave(); var header = SaveHeader(); int frozenBatchTicks = batch.RequiredTicks;
            Assert.That(payload.Batches.Single().RemainingPortions, Is.EqualTo(1)); Assert.That(payload.Baking.Single().RequiredTicks, Is.EqualTo(1600));
            var repository = TestSaveRepository(out string directory);
            try
            {
                repository.Commit(header, payload); var selected = repository.Select(header.SlotId, out var selectedHeader);
                host.StopProduct(); StartSavedFixture(selectedHeader, selected, false);
                Assert.That(Grid.Items.Count, Is.EqualTo(payload.Items.Length));
                Assert.That(Grid.Items.Values.Select(value => (GridItem)value).Count(i => i.Hygiene == ItemHygiene.Dirty), Is.EqualTo(1));
                Assert.That(Grid.Stations.Count, Is.EqualTo(18)); Assert.That(Renovation.AppliedStations.Keys.OrderBy(id => id), Is.EqualTo(new[] { 1, 2 }));
                var restoredBatch = Grid.AutoProcesses.Values.Select(value => (GridAutoProcess)value).Single();
                Assert.That(restoredBatch.RemainingPortions, Is.EqualTo(1)); Assert.That(restoredBatch.RequiredTicks, Is.EqualTo(frozenBatchTicks));
                Assert.That(restoredBatch.InputItemIds, Is.EqualTo(Grid.Item(restoredBatch.PotItemId).Contents));
                var restoredBake = Grid.BakeProcesses.Values.Select(value => (GridBakeProcess)value).Single();
                Assert.That(restoredBake.RequiredTicks, Is.EqualTo(1600)); Assert.That(restoredBake.ProgressTicks, Is.EqualTo(payload.Baking.Single().ProgressTicks));
                Assert.That(Grid.Station(restoredBake.StationId).BakeProcessId, Is.EqualTo(restoredBake.Id));
                AdvanceTicks(1); Assert.That(restoredBake.ProgressTicks, Is.EqualTo(payload.Baking.Single().ProgressTicks + 1));
                Assert.That(restoredBatch.RemainingPortions, Is.EqualTo(1));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        public void SoloSaveSelectionInvalidBaselineRejectsBeforeHostAndNewGameCreatesNoFakeLoadedSave()
        {
            StartSolo(); var header = SaveHeader(); var payload = CompleteForSave(); host.StopProduct();
            header.Version = 999;
            Assert.Throws<InvalidOperationException>(() => StartSavedFixture(header, payload));
            Assert.That((Scene)host.HostScene, Is.Null); Assert.That((HostSessionComponent)host.Authority, Is.Null);
            host.StopProduct(); StartSavedFixture(null, null, false);
            var match = (Match)((HostSessionComponent)host.Authority).Match;
            Assert.That(match.GetComponent<SoloSaveBinding>(), Is.Not.Null);
            Assert.That(match.Children.Values.OfType<SelectedSaveBaseline>(), Is.Empty);
            Assert.That(Grid.Items.Count, Is.EqualTo(7)); Assert.That(CurrentLevel.ConfigId, Is.EqualTo(1));
        }
    }
}
