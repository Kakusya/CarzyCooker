using System;
using System.IO;
using System.Linq;
using System.Text;
using MemoryPack;
using NUnit.Framework;

namespace ET
{
    [MemoryPackable]
    public sealed partial class FirstDrinkLegacyBatch : Object
    {
        public int Key { get; set; }
        public int RecipeId { get; set; }
        public int RequiredTicks { get; set; }
        public int ProgressTicks { get; set; }
        public int PotKey { get; set; }
        public int StationKey { get; set; }
        public int State { get; set; }
        public int OutputDefinitionId { get; set; }
        public int TotalPortions { get; set; }
        public int RemainingPortions { get; set; }
        public int ServingContainerDefinitionId { get; set; }
        public int[] InputKeys { get; set; }
    }

    [MemoryPackable]
    public sealed partial class FirstDrinkLegacyPayload : Object
    {
        public SoloSavedStation[] Stations { get; set; }
        public SoloSavedItem[] Items { get; set; }
        public SoloSavedManual[] ManualWork { get; set; }
        public FirstDrinkLegacyBatch[] Batches { get; set; }
        public SoloSavedBake[] Baking { get; set; }
        public SoloSavedSupplyBalance[] Supply { get; set; }
        public SoloSavedShipment[] Shipments { get; set; }
        public SoloSavedRenovation[] Renovations { get; set; }
        public int PendingRenovationId { get; set; }
        public int PendingUpgradeStationKey { get; set; }
        public int PendingUpgradeExpectedLevel { get; set; }
    }

    public sealed partial class ProductSessionTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void FirstDrinkLegacyVersionCannotSmuggleJuiceLockOrCapability(bool lockContainer)
        {
            StartSolo(); long pot = SoupFillPot(host.Client); var batch = SoupBegin(host.Client, pot);
            var payload = CompleteForSave(); var header = SaveHeader(); header.Version = 3;
            var value = payload.Batches.Single();
            if (lockContainer) value.LockContainer = true;
            else value.RequiredCapability = (int)GridStationCapability.Juice;
            Assert.That(Assert.Throws<InvalidOperationException>(() => SoloSaveValidation.Payload(header, payload, SaveProfileId)).Message, Is.EqualTo("InvalidBatchLock"));
            Assert.That(batch.LockContainer, Is.False); Assert.That(batch.RequiredCapability, Is.EqualTo((int)GridStationCapability.Cook));
            Assert.That(batch.RemainingPortions, Is.EqualTo(2));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void FirstDrinkLegacyActualTwelveMemberSoupBatchFileRestoresWithoutAddingDrinksOrRewriting(int version)
        {
            StartSolo(); long pot = SoupFillPot(host.Client); var batch = SoupBegin(host.Client, pot);
            var payload = CompleteForSave(); var header = SaveHeader(); header.Version = version;
            if (version == 1) { header.CompletedLevelId = 1; header.NextLevelId = 2; }
            var repository = TestSaveRepository(out string directory);
            try
            {
                var legacy = new FirstDrinkLegacyPayload
                {
                    Stations = payload.Stations, Items = payload.Items, ManualWork = payload.ManualWork,
                    Batches = payload.Batches.Select(p => new FirstDrinkLegacyBatch
                    {
                        Key = p.Key, RecipeId = p.RecipeId, RequiredTicks = p.RequiredTicks, ProgressTicks = p.ProgressTicks,
                        PotKey = p.PotKey, StationKey = p.StationKey, State = p.State, OutputDefinitionId = p.OutputDefinitionId,
                        TotalPortions = p.TotalPortions, RemainingPortions = p.RemainingPortions,
                        ServingContainerDefinitionId = p.ServingContainerDefinitionId, InputKeys = p.InputKeys
                    }).ToArray(),
                    Baking = payload.Baking, Supply = payload.Supply, Shipments = payload.Shipments, Renovations = payload.Renovations,
                    PendingRenovationId = payload.PendingRenovationId
                };
                byte[] head = MemoryPackHelper.Serialize(header), body = MemoryPackHelper.Serialize(legacy);
                using var memory = new MemoryStream(); using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
                { writer.Write(Encoding.ASCII.GetBytes("CCSOLO01")); writer.Write(head.Length); writer.Write(body.Length); writer.Write(head); writer.Write(body); }
                byte[] original = memory.ToArray(); string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write)) file.Write(original, 0, original.Length);
                var copy = repository.Select(header.SlotId, out var selected);
                Assert.That(copy.Batches.Single().RequiredCapability, Is.Zero); Assert.That(copy.Batches.Single().LockContainer, Is.False);
                host.StopProduct(); StartSavedFixture(selected, copy); Pump(8);
                var restored = Grid.AutoProcesses.Values.Select(p => (GridAutoProcess)p).Single();
                Assert.That(restored.RequiredCapability, Is.EqualTo((int)GridStationCapability.Cook)); Assert.That(restored.LockContainer, Is.False);
                Assert.That(restored.RemainingPortions, Is.EqualTo(2)); Assert.That(Supply.RemainingPackages.Keys, Is.EquivalentTo(new[] { 1, 2, 3 }));
                Assert.That(CurrentLevel.MenuVariant, Is.Zero); Assert.That(CurrentLevel.DrinkBindings, Is.Empty);
                Assert.That(Grid.Items.Values.Select(i => (GridItem)i).All(i => Tables.Instance.DTGridItemConfig.Get(i.DefinitionId).MenuVariant == 0), Is.True);
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally { RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
