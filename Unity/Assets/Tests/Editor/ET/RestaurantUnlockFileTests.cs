using System;
using System.IO;
using System.Linq;
using System.Text;
using MemoryPack;
using NUnit.Framework;

namespace ET
{
    [MemoryPackable]
    public sealed partial class UnlockLegacyPayload : Object
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
    }

    [MemoryPackable]
    public sealed partial class UnlockLegacySuccess : Object
    {
        public string SaveResultId { get; set; }
        public string MatchId { get; set; }
        public string WorldId { get; set; }
        public int CompletedLevelId { get; set; }
        public int NextLevelId { get; set; }
        public int Score { get; set; }
        public int Stars { get; set; }
        public int ServedOrders { get; set; }
        public int UnfulfilledOrders { get; set; }
        public long CompletedUtcTicks { get; set; }
        public byte[] Payload { get; set; }
        public byte[] Authority { get; set; }
    }

    public sealed partial class ProductSessionTests
    {
        private static byte[] UnlockOldBody(SoloSavePayload payload) => MemoryPackHelper.Serialize(new UnlockLegacyPayload
        {
            Stations = payload.Stations, Items = payload.Items, ManualWork = payload.ManualWork,
            Batches = payload.Batches, Baking = payload.Baking, Supply = payload.Supply, Shipments = payload.Shipments,
            Renovations = payload.Renovations, PendingRenovationId = payload.PendingRenovationId,
            PendingUpgradeStationKey = payload.PendingUpgradeStationKey, PendingUpgradeExpectedLevel = payload.PendingUpgradeExpectedLevel
        });

        [TestCase(false)]
        [TestCase(true)]
        [Category("RestaurantUnlockFiles")]
        public void RestaurantUnlockActualSoloAndCooperativeFilesRestoreGrantOnceWithoutCreatingInventory(bool cooperative)
        {
            StartGrowth(cooperative);
            var repository = TestSaveRepository(out string directory);
            try
            {
                if (cooperative)
                {
                    ProveFixtureProfile(host.Client, repository);
                }
                RenovationComplete(); RenovationChoose(7);
                SoloSaveHeader header;
                if (cooperative)
                {
                    PumpUntil(() => ProgressClaimFiles.Browse(repository).Length == 1, "Real growth result ticket reaches Root", 250, true);
                    var receipt = ProgressClaimFiles.Claim(repository, ProgressClaimFiles.Browse(repository).Single().TicketId);
                    repository.Select(receipt.SlotId, out header);
                }
                else
                {
                    header = SaveHeader(); repository.Commit(header, SoloSaveCapture.Kitchen(Restaurant));
                }
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                byte[] original = File.ReadAllBytes(path);
                var payload = repository.Select(header.SlotId, out var read);
                Assert.That(read.Version, Is.EqualTo(SoloSaveFormat.Current));
                Assert.That(payload.Unlocks.DishIds, Does.Contain(3));
                Assert.That(payload.Unlocks.StationIds, Does.Contain(10));
                Assert.That(payload.PendingRenovationId, Is.EqualTo(7));
                int[] items = payload.Items.Select(item => item.DefinitionId).OrderBy(id => id).ToArray();
                int[] stations = payload.Stations.Select(station => station.DefinitionId).OrderBy(id => id).ToArray();
                host.StopProduct(); StartSavedFixture(read, payload); Pump(12);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(cooperative ? 802 : 702));
                Assert.That(CurrentLevel.AllowedDishIds, Does.Contain(3));
                var unlocks = Restaurant.GetComponent<RestaurantUnlocks>();
                Assert.That(unlocks.GrantRestaurantUnlocks(Tables.Instance.DTRenovationConfig.Get(7)), Is.False);
                Assert.That(Renovation.AppliedStations[7], Is.Empty);
                Assert.That(Grid.Items.Values.Select(value => ((GridItem)value).DefinitionId).OrderBy(id => id), Is.EqualTo(items));
                Assert.That(Grid.Stations.Values.Select(value => ((GridStation)value).DefinitionId).OrderBy(id => id), Is.EqualTo(stations));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        [Category("RestaurantUnlockFiles")]
        public void RestaurantUnlockGenuineV7FileAndTwelveMemberSignedResultAdaptReadOnlyAndFirstClaimPublishesV8()
        {
            StartSolo(liveDrinks: true);
            RenovationComplete(); UpgradeChoose(3, PrepStation("切配台"));
            var payload = SoloSaveCapture.Kitchen(Restaurant); var header = SaveHeader(); header.Version = 7;
            var repository = TestSaveRepository(out string directory);
            try
            {
                byte[] head = MemoryPackHelper.Serialize(header), body = UnlockOldBody(payload);
                Assert.That(body[0], Is.EqualTo(11), "Genuine old body has eleven members, no Unlocks field");
                using var memory = new MemoryStream();
                using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
                {
                    writer.Write(Encoding.ASCII.GetBytes("CCSOLO01")); writer.Write(head.Length); writer.Write(body.Length); writer.Write(head); writer.Write(body);
                }
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                byte[] original = memory.ToArray(); File.WriteAllBytes(path, original);
                var restored = repository.Select(header.SlotId, out var read);
                Assert.That(read.Version, Is.EqualTo(7));
                Assert.That(restored.Unlocks.DishIds, Is.EquivalentTo(new[] { 1, 2, 3 }));
                Assert.That(restored.Unlocks.StationIds, Does.Contain(10));

                var fixture = ClaimFileFixture(repository, payload);
                var verified = ProgressClaimCodec.Ticket(fixture.Ticket, repository);
                var value = verified.Success;
                byte[] oldSuccess = MemoryPackHelper.Serialize(new UnlockLegacySuccess
                {
                    SaveResultId = value.SaveResultId, MatchId = value.MatchId, WorldId = value.WorldId,
                    CompletedLevelId = value.CompletedLevelId, NextLevelId = value.NextLevelId, Score = value.Score, Stars = value.Stars,
                    ServedOrders = value.ServedOrders, UnfulfilledOrders = value.UnfulfilledOrders,
                    CompletedUtcTicks = value.CompletedUtcTicks, Payload = body, Authority = value.Authority
                });
                Assert.That(oldSuccess[0], Is.EqualTo(12));
                verified.Ticket.Success = OfflineProfiles.Sign(repository, OfflineProfiles.SuccessResult, oldSuccess);
                byte[] ticket = OfflineProfiles.Sign(repository, OfflineProfiles.ClaimTicket, MemoryPackHelper.Serialize(verified.Ticket));
                Assert.That(ProgressClaimCodec.Ticket(ticket, repository).Success.ContentVersion, Is.Zero);
                ProgressClaimFiles.PersistTicket(repository, ticket, fixture.MatchId);
                var receipt = ProgressClaimFiles.Claim(repository, fixture.TicketId);
                var claimed = repository.Select(receipt.SlotId, out var published);
                Assert.That(published.Version, Is.EqualTo(SoloSaveFormat.Current));
                Assert.That(claimed.Unlocks.DishIds, Is.EquivalentTo(restored.Unlocks.DishIds));
                Assert.That(ProgressClaimFiles.Claim(repository, fixture.TicketId).SlotId, Is.EqualTo(receipt.SlotId));
                Assert.That(repository.Browse().Length, Is.EqualTo(2));
                host.StopProduct(); StartSavedFixture(read, restored); Pump(12);
                Assert.That(Grid.Items.Count, Is.EqualTo(payload.Items.Length));
                Assert.That(Grid.Stations.Count, Is.EqualTo(payload.Stations.Length + Tables.Instance.DTRenovationConfig.Get(payload.PendingRenovationId).Quantity));
                Assert.That(CurrentLevel.AllowedDishIds, Does.Contain(3));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
