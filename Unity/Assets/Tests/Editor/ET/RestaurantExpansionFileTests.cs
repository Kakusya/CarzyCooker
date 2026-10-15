using System;
using System.IO;
using System.Linq;
using System.Text;
using MemoryPack;
using NUnit.Framework;

namespace ET
{
    [MemoryPackable]
    public sealed partial class ExpansionLegacyPayload : Object
    {
        public SoloSavedStation[] Stations { get; set; }
        public SoloSavedItem[] Items { get; set; }
        public SoloSavedManual[] ManualWork { get; set; }
        public SoloSavedBatch[] Batches { get; set; }
        public SoloSavedBake[] Baking { get; set; }
        public SoloSavedSupplyBalance[] Supply { get; set; }
        public SoloSavedShipment[] Shipments { get; set; }
        public SoloSavedRenovation[] Renovations { get; set; }
        public int PendingRenovationId { get; set; }
        public int PendingUpgradeStationKey { get; set; }
        public int PendingUpgradeExpectedLevel { get; set; }
        public SavedRestaurantUnlocks Unlocks { get; set; }
    }

    public sealed partial class ProductSessionTests
    {
        private SoloSaveHeader ExpansionFile(SoloSaveRepository repository, bool cooperative, int completedLevel)
        {
            if (!cooperative)
            {
                var header = SaveHeader(); repository.Commit(header, SoloSaveCapture.Kitchen(Restaurant)); return header;
            }
            PumpUntil(() => ProgressClaimFiles.Browse(repository).Any(ticket => ticket.CompletedLevelId == completedLevel), "Actual expansion ticket reaches Root", 250, true);
            var ticket = ProgressClaimFiles.Browse(repository).Single(value => value.CompletedLevelId == completedLevel);
            var receipt = ProgressClaimFiles.Claim(repository, ticket.TicketId);
            repository.Select(receipt.SlotId, out var result); return result;
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("RestaurantExpansionFiles")]
        public void RestaurantExpansionActualSoloAndClaimFilesRestorePendingAndCommittedGeometryWithoutReplenishment(bool cooperative)
        {
            StartGrowth(cooperative);
            long bowl = PrepEggBowl(host.Client), egg = Grid.Item(bowl).Contents.Single();
            PrepDo(host.Client, GridAction.StartHeldWork, bowl, egg, bowl); var work = Grid.InputProcess(egg); PrepTick(5);
            PrepDo(host.Client, GridAction.StopWork, input: egg, expectedTarget: bowl, process: work.Id);
            PrepDo(host.Client, GridAction.PlaceCounter, PrepStation("碗台").Id, bowl); int ticks = work.ProgressTicks;
            var repository = TestSaveRepository(out string directory);
            try
            {
                if (cooperative)
                {
                    ProveFixtureProfile(host.Client, repository);
                }
                RenovationComplete(); RenovationChoose(8);
                var pendingHeader = ExpansionFile(repository, cooperative, CurrentLevel.ConfigId);
                var pending = repository.Select(pendingHeader.SlotId, out pendingHeader);
                Assert.That(pending.Geometry.Width, Is.EqualTo(7)); Assert.That(pending.PendingRenovationId, Is.EqualTo(8));
                long priorLevel = CurrentLevel.Id;
                using (var reply = LevelReply(host.Client, RenovationView.CreateLevelCommand(LevelControlAction.Continue)))
                {
                    Assert.That(reply.Error, Is.Zero, reply.Message);
                }
                PumpUntil(() => CurrentLevel.Id != priorLevel, "Root-owned cooperative save finishes before continuation", 300, true);
                Assert.That(Grid.Width, Is.EqualTo(9));
                var cargo = SupplyOrderReady(1); int balance = Supply.RemainingPackages[1];
                Assert.That(cargo.Stage, Is.EqualTo(ShipmentStage.Ready));
                RenovationComplete(); RenovationChoose(7);
                var header = ExpansionFile(repository, cooperative, CurrentLevel.ConfigId);
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave"); byte[] original = File.ReadAllBytes(path);
                var payload = repository.Select(header.SlotId, out header);
                Assert.That(header.Version, Is.EqualTo(SoloSaveFormat.Current)); Assert.That(payload.Geometry.Width, Is.EqualTo(9));
                Assert.That(payload.Geometry.Height, Is.EqualTo(18)); Assert.That(payload.Stations.Count(station => station.TableDefinitionId == 3), Is.EqualTo(1));
                Assert.That(payload.ManualWork.Single().ProgressTicks, Is.EqualTo(ticks));
                host.StopProduct(); StartSavedFixture(header, payload); Pump(15);
                Assert.That(Grid.Width, Is.EqualTo(9)); Assert.That(Grid.Height, Is.EqualTo(18));
                Assert.That(Grid.Stations.Count, Is.EqualTo(payload.Stations.Length)); Assert.That(Grid.Items.Count, Is.EqualTo(payload.Items.Length));
                var table = Grid.Stations.Values.Select(value => (GridStation)value).Single(station => station.Cell == new GridCell(7, 3));
                Assert.That(table.GetComponent<FrontTable>(), Is.Not.Null); Assert.That(table.Name, Is.EqualTo("扩建单客桌"));
                Assert.That(Renovation.AppliedStations[8], Is.EqualTo(new[] { table.Id }));
                Assert.That(Supply.RemainingPackages[1], Is.EqualTo(balance));
                Assert.That(Supply.Children.Values.OfType<SupplyShipment>().Single().Stage, Is.EqualTo(ShipmentStage.Ready));
                Assert.That(Grid.Processes.Values.Select(value => (GridManualProcess)value).Single().ProgressTicks, Is.EqualTo(ticks));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
                host.StopProduct(); StartSavedFixture(pendingHeader, pending); Pump(15);
                Assert.That(Grid.Width, Is.EqualTo(9), "Reading a pending reward installs it exactly once");
                Assert.That(Grid.Stations.Count, Is.EqualTo(pending.Stations.Length + 1)); Assert.That(Grid.Items.Count, Is.EqualTo(pending.Items.Length));
                Assert.That(Renovation.AppliedStations[8].Count, Is.EqualTo(1));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test, Category("RestaurantExpansionFiles")]
        public void RestaurantExpansionGenuineTwelveMemberV8FileKeepsOldBoundsAndDoesNotInstallOptionalTable()
        {
            StartSolo(); var payload = CompleteForSave(); var header = SaveHeader(); header.Version = 8;
            byte[] body = MemoryPackHelper.Serialize(new ExpansionLegacyPayload
            {
                Stations = payload.Stations, Items = payload.Items, ManualWork = payload.ManualWork, Batches = payload.Batches, Baking = payload.Baking,
                Supply = payload.Supply, Shipments = payload.Shipments, Renovations = payload.Renovations,
                PendingRenovationId = payload.PendingRenovationId, PendingUpgradeStationKey = payload.PendingUpgradeStationKey,
                PendingUpgradeExpectedLevel = payload.PendingUpgradeExpectedLevel, Unlocks = payload.Unlocks
            });
            Assert.That(body[0], Is.EqualTo(12)); byte[] head = MemoryPackHelper.Serialize(header);
            var repository = TestSaveRepository(out string directory);
            try
            {
                using var memory = new MemoryStream();
                using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
                {
                    writer.Write(Encoding.ASCII.GetBytes("CCSOLO01")); writer.Write(head.Length); writer.Write(body.Length); writer.Write(head); writer.Write(body);
                }
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave"); byte[] original = memory.ToArray(); File.WriteAllBytes(path, original);
                var selected = repository.Select(header.SlotId, out var read);
                Assert.That(read.Version, Is.EqualTo(8)); Assert.That(selected.Geometry.Width, Is.EqualTo(7));
                var fixture = ClaimFileFixture(repository, payload);
                var verified = ProgressClaimCodec.Ticket(fixture.Ticket, repository);
                verified.Success.ContentVersion = 8; verified.Success.Payload = body;
                verified.Ticket.Success = OfflineProfiles.Sign(repository, OfflineProfiles.SuccessResult, MemoryPackHelper.Serialize(verified.Success));
                byte[] ticket = OfflineProfiles.Sign(repository, OfflineProfiles.ClaimTicket, MemoryPackHelper.Serialize(verified.Ticket));
                ProgressClaimFiles.PersistTicket(repository, ticket, fixture.MatchId);
                var receipt = ProgressClaimFiles.Claim(repository, fixture.TicketId);
                var claimed = repository.Select(receipt.SlotId, out var current);
                Assert.That(current.Version, Is.EqualTo(SoloSaveFormat.Current)); Assert.That(claimed.Geometry.Width, Is.EqualTo(7));
                Assert.That(ProgressClaimFiles.Claim(repository, fixture.TicketId).SlotId, Is.EqualTo(receipt.SlotId));
                host.StopProduct(); StartSavedFixture(read, selected); Pump(12);
                Assert.That(Grid.Width, Is.EqualTo(7)); Assert.That(Grid.Height, Is.EqualTo(18));
                Assert.That(Grid.Stations.Values.Count(value => ((GridStation)value).GetComponent<FrontTable>() != null), Is.EqualTo(2));
                Assert.That(Grid.Items.Count, Is.EqualTo(payload.Items.Length)); Assert.That(Grid.IsWalkable(new GridCell(6, 3)), Is.False);
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
