#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using ET.Client;
using NUnit.Framework;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        private SoloSavePresentation Selection => Runtime.Root().GetComponent<SoloSavePresentation>();
        private MonoUIGridKitchen SelectionHud => ((UIGridKitchen)Selection.Hud).View;
        private Button SelectionButton(string caption) => SelectionHud.GetComponentsInChildren<Button>(false).Single(value => value.GetComponentInChildren<Text>().text == caption);
        private Button SelectionSlotButton(string slot)
        {
            int index = Array.FindIndex(Selection.Headers, header => header.SlotId == slot) + 1;
            return SelectionHud.GetComponentsInChildren<Button>(false).Single(value => value.GetComponentInChildren<Text>().text.StartsWith($"继续 记录{index} ·"));
        }

        private static SoloSavePayload SelectionFixture()
        {
            // Actual config supplies the file fixture; this test targets selection rather than world simulation.
            var tables = Tables.Instance;
            return new SoloSavePayload
            {
                Stations = tables.DTInitialLayoutConfig.DataList.Where(row => row.MenuVariant == 0).Select(row => new SoloSavedStation
                {
                    Key = row.Id, InitialLayoutId = row.Id, DefinitionId = row.StationDefinitionId, X = row.X, Y = row.Y, Orientation = row.Orientation,
                    ItemKey = row.InitialItemDefinitionId == 0 ? 0 : row.Id, TableDefinitionId = tables.DTTableConfig.DataList.FirstOrDefault(t => t.InitialLayoutId == row.Id)?.Id ?? 0
                }).ToArray(),
                Items = tables.DTInitialLayoutConfig.DataList.Where(row => row.MenuVariant == 0 && row.InitialItemDefinitionId != 0).Select(row => new SoloSavedItem
                    { Key = row.Id, DefinitionId = row.InitialItemDefinitionId, X = row.X, Y = row.Y, Location = (int)GridItemLocation.Counter, HolderKey = row.Id }).ToArray(),
                Supply = tables.DTSupplyConfig.DataList.Where(row => row.MenuVariant == 0).Select(row => new SoloSavedSupplyBalance { SourceId = row.Id, RemainingPackages = row.AvailablePackages }).ToArray()
            };
        }

        private static SoloSaveHeader SelectionHeader(string profile) => new()
        {
            SlotId = Guid.NewGuid().ToString("N"), ResultId = Guid.NewGuid().ToString("N"), ProfileId = profile,
            CompletedLevelId = 1, NextLevelId = Tables.Instance.DTLevelConfig.Get(1).NextLevelConfigId, SavedUtcTicks = DateTime.UtcNow.Ticks
        };

        private static void WriteUnsupportedSelection(SoloSaveRepository repository, SoloSaveHeader header, byte[] body)
        {
            byte[] bytes = MemoryPackHelper.Serialize(header);
            using var stream = new FileStream(Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave"), FileMode.CreateNew, FileAccess.Write);
            using var writer = new BinaryWriter(stream, Encoding.UTF8);
            writer.Write(Encoding.ASCII.GetBytes("CCSOLO01")); writer.Write(bytes.Length); writer.Write(body.Length); writer.Write(bytes); writer.Write(body);
        }

        [UnityTest]
        public IEnumerator SoloSaveSelectionUiBrowsesWithoutWorldCancelsValidLoadRejectsInvalidAndRestoresSelectedFile()
        {
            yield return StartLauncher(false, false);
            ProductRuntimeComponent runtime = Runtime;
            Assert.That((Scene)runtime.HostScene, Is.Null); Assert.That((HostSessionComponent)runtime.Authority, Is.Null);
            var repository = CreateSaveUiRepository(runtime, out string directory, out string profile);
            try
            {
                var payload = SelectionFixture(); var valid = SelectionHeader(profile); repository.Commit(valid, payload);
                var unknown = SelectionHeader(profile); unknown.Version = 999; WriteUnsupportedSelection(repository, unknown, MemoryPackHelper.Serialize(payload));
                var damaged = SelectionHeader(profile); WriteUnsupportedSelection(repository, damaged, new byte[] { 0 });
                Selection.OpenSoloSelection();
                yield return Wait(() => ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Headers.Length == 3, "Actual bounded-header selection browse");
                Assert.That((Scene)runtime.HostScene, Is.Null); Assert.That((HostSessionComponent)runtime.Authority, Is.Null);
                var retired = CaptureButtonCallback(SelectionSlotButton(valid.SlotId)); EntityRef<UIGridKitchen> retiredHud = Selection.Hud;
                Pointer(SelectionSlotButton(valid.SlotId)); Assert.That(Selection.LoadingSelection, Is.True);
                Pointer(SelectionButton("取消加载"));
                yield return Wait(() => ((UIGridKitchen)Selection.Hud)?.Available == true && !Selection.LoadingSelection && Selection.Headers.Length == 3, "Cancellation reopens managed selector");
                yield return null; yield return null;
                Assert.That((Scene)runtime.HostScene, Is.Null); Assert.That((HostSessionComponent)runtime.Authority, Is.Null);
                Assert.That(((UIGridKitchen)retiredHud)?.Available == true, Is.False);
                Assert.That(runtime.Root().GetComponent<UIComponent>().GetComponent<UIGridKitchen>(), Is.SameAs((UIGridKitchen)Selection.Hud)); retired();
                Assert.That(Selection.LoadingSelection, Is.False); Assert.That((Scene)runtime.HostScene, Is.Null);
                Pointer(SelectionSlotButton(unknown.SlotId));
                yield return Wait(() => !Selection.LoadingSelection && Selection.Failure != null, "Unknown version is explicit rejection");
                Assert.That(Selection.Failure, Does.Contain("UnsupportedVersion")); Assert.That((Scene)runtime.HostScene, Is.Null);
                Assert.That(SelectionHud.StatusText.text, Does.Contain("版本不受支持"));
                Pointer(SelectionSlotButton(damaged.SlotId));
                yield return Wait(() => !Selection.LoadingSelection && Selection.Failure != null, "Damaged complete data rejects before world");
                Assert.That((Scene)runtime.HostScene, Is.Null); Assert.That((HostSessionComponent)runtime.Authority, Is.Null);
                Pointer(SelectionSlotButton(valid.SlotId));
                yield return Wait(() => Presentation?.Ready == true, "Valid selected file creates one real restaurant");
                Assert.That(Grid.SaveEnabled, Is.True); Assert.That(Grid.LevelConfigId, Is.EqualTo(valid.NextLevelId));
                Assert.That(Grid.Items.Count, Is.EqualTo(payload.Items.Length)); Assert.That(Grid.Stations.Count, Is.EqualTo(payload.Stations.Length));
                Assert.That(Grid.LevelPhase, Is.EqualTo(LevelPhase.Preparing)); Assert.That(Grid.LastSavedSlotId, Is.EqualTo(valid.SlotId));
                Assert.That(repository.Browse().Length, Is.EqualTo(3));
            }
            finally { runtime.StopProduct(); repository.Dispose(); RemoveOwnSaveUiFiles(directory); }
        }
    }
}
#endif
