#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Linq;
using ET.Client;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest]
        [Category("RestaurantArtRegression")]
        public IEnumerator StationUpgradeUiManagedTargetReopenSavedContinueAndRetiredCallbackPreserveStation()
        {
            yield return StartLauncher(false, false);
            var runtime = Runtime; var repository = CreateSaveUiRepository(runtime, out string directory, out string profile);
            try
            {
                var header = SelectionHeader(profile); header.CompletedLevelId = 101; header.NextLevelId = 102;
                repository.Commit(header, SelectionFixture()); Selection.OpenSoloSelection();
                yield return Wait(() => ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Headers.Length == 1, "Actual upgrade fixture selector");
                yield return InspectAndContinueSave(header.SlotId); yield return Wait(() => Presentation?.Ready == true, "Actual file begins second level");
                string buff = Hud.GetComponentsInChildren<Button>(false).First(b => b.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
                yield return Command(buff); yield return Command("开始营业");
                yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.UpgradeTargets.Count > 0, "Actual upgrade targets arrive after natural drain", 75);
                long station = Grid.UpgradeTargets.Single(t => t.DefinitionId == 3).StationId;
                var before = Grid.Stations[station]; long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
                var retired = CaptureButtonCallback(Button("装修 升级切配台", true));
                Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
                yield return Wait(() => Presentation.Ready, "Upgrade HUD managed reopen");
                long command = Client.NextCommandId; retired(); Assert.That(Client.NextCommandId, Is.EqualTo(command));
                yield return Command("装修 升级切配台", true);
                Assert.That(Grid.SelectedRenovationId, Is.EqualTo(3)); Assert.That(Grid.SelectedUpgradeStationId, Is.EqualTo(station)); Assert.That(Grid.Stations[station].UpgradeLevel, Is.Zero);
                Assert.That(Hud.SettlementText.text, Does.Contain("已选装修：升级切配台"));
                long level = Grid.LevelId; var save = Button("保存并继续"); command = Client.NextCommandId;
                Pointer(save); Pointer(save); Assert.That(Client.NextCommandId, Is.EqualTo(command + 1)); Presentation.CloseGridHud();
                yield return Wait(() => Grid.LevelId != level && Grid.LevelPhase == LevelPhase.Preparing, "Match-owned upgrade save survives HUD close", 15);
                Presentation.OpenGridHud(); yield return Wait(() => Presentation.Ready, "Upgraded HUD opens in next level");
                Assert.That(Grid.Stations[station].UpgradeLevel, Is.EqualTo(1)); Assert.That(Grid.Stations[station].Cell, Is.EqualTo(before.Cell));
                Assert.That(Grid.Stations[station].DefinitionId, Is.EqualTo(before.DefinitionId)); Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
                Assert.That(Grid.Stations.Count, Is.EqualTo(16)); Assert.That(repository.Browse().Length, Is.EqualTo(2));
                command = Client.NextCommandId; retired(); Assert.That(Client.NextCommandId, Is.EqualTo(command));
                yield return RenovationUiMoveStation("切配台", before.Cell);
                UnityEngine.Vector2 point = Board.BoardCamera.WorldToScreenPoint(Board.CellToWorld(before.Cell));
                Presentation.ProcessGridPointer(point, true, true, false); Presentation.ProcessGridPointer(point, false, false, true);
                yield return Wait(() => Presentation.SelectedStationId == station, "Actual map selects upgraded station");
                Pointer(Button("查看详情"));
                Assert.That(Hud.DetailsText.text, Does.Contain("等级 1"));
            }
            finally { runtime.StopProduct(); repository.Dispose(); RemoveOwnSaveUiFiles(directory); }
        }
    }
}
#endif
