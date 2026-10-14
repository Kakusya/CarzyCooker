using System;
using System.IO;
using System.Linq;
using System.Text;
using MemoryPack;
using NUnit.Framework;

namespace ET
{
    [MemoryPackable]
    public sealed partial class UpgradeLegacyStation : Object
    {
        public int Key { get; set; }
        public int DefinitionId { get; set; }
        public int InitialLayoutId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Orientation { get; set; }
        public int ItemKey { get; set; }
        public int[] PoolItemKeys { get; set; } = Array.Empty<int>();
        public int TableDefinitionId { get; set; }
        public bool TableDirty { get; set; }
    }

    [MemoryPackable]
    public sealed partial class UpgradeLegacyPayload : Object
    {
        public UpgradeLegacyStation[] Stations { get; set; }
        public SoloSavedItem[] Items { get; set; }
        public SoloSavedManual[] ManualWork { get; set; }
        public SoloSavedBatch[] Batches { get; set; }
        public SoloSavedBake[] Baking { get; set; }
        public SoloSavedSupplyBalance[] Supply { get; set; }
        public SoloSavedShipment[] Shipments { get; set; }
        public SoloSavedRenovation[] Renovations { get; set; }
        public int PendingRenovationId { get; set; }
    }

    public sealed partial class ProductSessionTests
    {
        [TestCase(1)]
        [TestCase(2)]
        public void StationUpgradeActualPreUpgradeFileDefaultsZeroGradePreservesOriginalBytesAndRestores(int version)
        {
            StartSolo(); var payload = CompleteForSave(); var repository = TestSaveRepository(out string directory);
            try
            {
                var header = SaveHeader(); header.Version = version;
                if (version == 1) { header.CompletedLevelId = 1; header.NextLevelId = 2; }
                var legacy = new UpgradeLegacyPayload
                {
                    Stations = payload.Stations.Select(s => new UpgradeLegacyStation
                    { Key = s.Key, DefinitionId = s.DefinitionId, InitialLayoutId = s.InitialLayoutId, X = s.X, Y = s.Y, Orientation = s.Orientation,
                        ItemKey = s.ItemKey, PoolItemKeys = s.PoolItemKeys, TableDefinitionId = s.TableDefinitionId, TableDirty = s.TableDirty }).ToArray(),
                    Items = payload.Items, ManualWork = payload.ManualWork, Batches = payload.Batches, Baking = payload.Baking,
                    Supply = payload.Supply, Shipments = payload.Shipments, Renovations = payload.Renovations, PendingRenovationId = payload.PendingRenovationId
                };
                byte[] head = version == 2 ? MemoryPackHelper.Serialize(header) : MemoryPackHelper.Serialize(new SoloChapterLegacyHeader
                { SlotId = header.SlotId, ProfileId = header.ProfileId, ResultId = header.ResultId, CompletedLevelId = 1, NextLevelId = 2, SavedUtcTicks = header.SavedUtcTicks });
                byte[] body = MemoryPackHelper.Serialize(legacy);
                using var memory = new MemoryStream(); using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
                { writer.Write(Encoding.ASCII.GetBytes("CCSOLO01")); writer.Write(head.Length); writer.Write(body.Length); writer.Write(head); writer.Write(body); }
                byte[] original = memory.ToArray(); string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write)) file.Write(original, 0, original.Length);
                var restored = repository.Select(header.SlotId, out var selected);
                Assert.That(restored.Stations.All(s => s.UpgradeLevel == 0 && s.UpgradeDefinitionId == 0), Is.True);
                Assert.That(restored.PendingUpgradeStationKey, Is.Zero); Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
                host.StopProduct(); StartSavedFixture(selected, restored); PumpMessages(8);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(header.NextLevelId)); Assert.That(Grid.Stations.Count, Is.EqualTo(17));
                Assert.That(Grid.Stations.Values.Select(s => (GridStation)s).All(s => s.UpgradeLevel == 0 && s.UpgradeDefinitionId == 0), Is.True);
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
