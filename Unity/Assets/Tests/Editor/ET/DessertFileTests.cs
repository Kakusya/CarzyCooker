using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MemoryPack;
using NUnit.Framework;

namespace ET
{
    [MemoryPackable]
    public sealed partial class DessertLegacyBake : Object
    {
        public int Key { get; set; }
        public int RecipeId { get; set; }
        public int RequiredTicks { get; set; }
        public int ProgressTicks { get; set; }
        public int StationKey { get; set; }
        public int InputKey { get; set; }
        public int OutputDefinitionId { get; set; }
    }

    [MemoryPackable]
    public sealed partial class DessertLegacyPayload : Object
    {
        public SoloSavedStation[] Stations { get; set; }
        public SoloSavedItem[] Items { get; set; }
        public SoloSavedManual[] ManualWork { get; set; }
        public SoloSavedBatch[] Batches { get; set; }
        public DessertLegacyBake[] Baking { get; set; }
        public SoloSavedSupplyBalance[] Supply { get; set; }
        public SoloSavedShipment[] Shipments { get; set; }
        public SoloSavedRenovation[] Renovations { get; set; }
        public int PendingRenovationId { get; set; }
        public int PendingUpgradeStationKey { get; set; }
        public int PendingUpgradeExpectedLevel { get; set; }
        public SavedRestaurantUnlocks Unlocks { get; set; }
        public SavedKitchenGeometry Geometry { get; set; }
    }

    public sealed partial class ProductSessionTests
    {
        private GridBakeProcess DessertLongBake(ClientSessionComponent client)
        {
            var table = Tables.Instance.DTGridBakeRecipeConfig;
            var map = (Dictionary<int, DRGridBakeRecipeConfig>)table.DataMap;
            var rows = (List<DRGridBakeRecipeConfig>)table.DataList;
            var source = table.Get(5); int index = rows.IndexOf(source);
            try
            {
                map[5] = rows[index] = HotFoodTimedRecipe(source, 5000);
                return DessertBeginBake(client);
            }
            finally
            {
                map[5] = rows[index] = source;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("DessertFiles")]
        public void DessertActualSoloAndCooperativeClaimFileKeepHalfBatchAndUnfinishedMoldAndResumeWithoutRefill(bool cooperative)
        {
            StartDessert(cooperative); var client = (ClientSessionComponent)host.Client;
            var repository = TestSaveRepository(out string directory);
            try
            {
                if (cooperative)
                {
                    ProveFixtureProfile(client, repository);
                }
                DessertMix(client);
                var source = PrepStation("模具台");
                PrepDo(client, GridAction.TakeCounter, source.Id, source.ItemId); DessertPortion(client);
                var work = DessertLongBake(client);
                ContentComplete(); RenovationChoose(8);
                Assert.That(work.State, Is.EqualTo(GridBakeState.Baking));
                int progress = work.ProgressTicks;
                var header = ExpansionFile(repository, cooperative, CurrentLevel.ConfigId);
                var payload = repository.Select(header.SlotId, out header);
                Assert.That(header.Version, Is.EqualTo(SoloSaveFormat.Current));
                Assert.That(payload.Batches.Single().RemainingPortions, Is.EqualTo(1));
                Assert.That(payload.Baking.Single().CarrierKey, Is.Not.Zero);
                Assert.That(payload.Baking.Single().RequiredTicks, Is.EqualTo(5000));
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                byte[] original = File.ReadAllBytes(path); int count = payload.Items.Length;
                host.StopProduct(); repository.Dispose(); StartSavedFixture(header, payload);
                var restored = Grid.BakeProcesses.Values.Select(value => (GridBakeProcess)value).Single();
                long mold = restored.CarrierItemId;
                Assert.That(restored.ProgressTicks, Is.EqualTo(progress));
                Assert.That(restored.RequiredTicks, Is.EqualTo(5000));
                Assert.That(Grid.Item(mold).DefinitionId, Is.EqualTo(47));
                Assert.That(Grid.Item(mold).Contents, Is.EqualTo(new[] { restored.InputItemId }));
                Assert.That(Grid.Item(restored.InputItemId).HolderId, Is.EqualTo(mold));
                Assert.That(Grid.Item(mold).BakeProcessId, Is.EqualTo(restored.Id));
                Assert.That(Grid.Items.Count, Is.EqualTo(count));
                Assert.That(Grid.AutoProcesses.Values.Select(value => (GridAutoProcess)value).Single().RemainingPortions, Is.EqualTo(1));
                Assert.That(Supply.RemainingPackages[12], Is.EqualTo(4));
                Pump(15); AdvanceTicks(restored.RequiredTicks - restored.ProgressTicks + 3); Pump(15);
                Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 48), Is.EqualTo(1));
                client = (ClientSessionComponent)host.Client;
                PrepDo(client, GridAction.TakeCounter, PrepStation("烤箱").Id, mold); DessertUnmold(client);
                DessertPortion(client);
                Assert.That(Grid.AutoProcesses, Is.Empty);
                Assert.That(Grid.Item(mold).Contents.Count, Is.EqualTo(1));
                Assert.That(Grid.Item(Grid.Item(mold).Contents.Single()).DefinitionId, Is.EqualTo(46));
                Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 47), Is.EqualTo(2));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally
            {
                repository.Dispose(); RemoveOwnSaveTestDirectory(directory);
            }
        }

        [Test, Category("DessertFiles")]
        public void DessertActualV10SevenMemberBakeFileAndSignedClaimRemainDirectAndDoNotAddDessertInventory()
        {
            StartHotFood(); var client = (ClientSessionComponent)host.Client;
            var work = HotFoodStartLongFrozenFrying(client);
            ContentComplete(); RenovationChoose(8);
            int progress = work.ProgressTicks;
            var payload = SoloSaveCapture.Kitchen(Restaurant); var header = SaveHeader(); header.Version = 10;
            var old = new DessertLegacyPayload
            {
                Stations = payload.Stations, Items = payload.Items, ManualWork = payload.ManualWork, Batches = payload.Batches,
                Baking = payload.Baking.Select(bake => new DessertLegacyBake
                {
                    Key = bake.Key, RecipeId = bake.RecipeId, RequiredTicks = bake.RequiredTicks, ProgressTicks = bake.ProgressTicks,
                    StationKey = bake.StationKey, InputKey = bake.InputKey, OutputDefinitionId = bake.OutputDefinitionId
                }).ToArray(),
                Supply = payload.Supply, Shipments = payload.Shipments, Renovations = payload.Renovations,
                PendingRenovationId = payload.PendingRenovationId, PendingUpgradeStationKey = payload.PendingUpgradeStationKey,
                PendingUpgradeExpectedLevel = payload.PendingUpgradeExpectedLevel, Unlocks = payload.Unlocks, Geometry = payload.Geometry
            };
            byte[] body = MemoryPackHelper.Serialize(old);
            Assert.That(MemoryPackHelper.Serialize(old.Baking.Single())[0], Is.EqualTo(7));
            var repository = TestSaveRepository(out string directory);
            try
            {
                byte[] head = MemoryPackHelper.Serialize(header);
                using var memory = new MemoryStream();
                using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
                {
                    writer.Write(Encoding.ASCII.GetBytes("CCSOLO01")); writer.Write(head.Length); writer.Write(body.Length);
                    writer.Write(head); writer.Write(body);
                }
                byte[] original = memory.ToArray(); string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                File.WriteAllBytes(path, original);
                var restored = repository.Select(header.SlotId, out var read);
                Assert.That(restored.Baking.Single().CarrierKey, Is.Zero);
                var fixture = ClaimFileFixture(repository, payload);
                var verified = ProgressClaimCodec.Ticket(fixture.Ticket, repository);
                verified.Success.ContentVersion = 10; verified.Success.Payload = body;
                verified.Ticket.Success = OfflineProfiles.Sign(repository, OfflineProfiles.SuccessResult, MemoryPackHelper.Serialize(verified.Success));
                byte[] ticket = OfflineProfiles.Sign(repository, OfflineProfiles.ClaimTicket, MemoryPackHelper.Serialize(verified.Ticket));
                ProgressClaimFiles.PersistTicket(repository, ticket, fixture.MatchId);
                var receipt = ProgressClaimFiles.Claim(repository, fixture.TicketId);
                var claimed = repository.Select(receipt.SlotId, out var published);
                Assert.That(published.Version, Is.EqualTo(SoloSaveFormat.Current));
                Assert.That(claimed.Baking.Single().CarrierKey, Is.Zero);
                Assert.That(claimed.Baking.Single().ProgressTicks, Is.EqualTo(progress));
                host.StopProduct(); repository.Dispose(); StartSavedFixture(read, restored);
                var cooking = Grid.BakeProcesses.Values.Select(value => (GridBakeProcess)value).Single();
                Assert.That(cooking.CarrierItemId, Is.Zero);
                Assert.That(cooking.ProgressTicks, Is.EqualTo(progress));
                Assert.That(Grid.Items.Values.Any(value => ((GridItem)value).DefinitionId >= 43), Is.False);
                Assert.That(Supply.RemainingPackages.Keys.Any(id => id >= 12), Is.False);
                Pump(15); AdvanceTicks(cooking.RequiredTicks - cooking.ProgressTicks + 3); Pump(15);
                Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 34), Is.EqualTo(1));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally
            {
                repository.Dispose(); RemoveOwnSaveTestDirectory(directory);
            }
        }
    }
}
