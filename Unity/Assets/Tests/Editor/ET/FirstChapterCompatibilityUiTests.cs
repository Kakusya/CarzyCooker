#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using ET.Client;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest, Category("FirstChapterCompatibility")]
        public IEnumerator FirstChapterCompatibilityExistingFilesKeepNextLayoutStockAndCompletedScores()
        {
            yield return StartLauncher(chooseGame: false);
            var repository = CreateSaveUiRepository(Runtime, out string directory, out string profile);
            try
            {
                var payload = SelectionFixture(ProductMenuVariant.Growth);
                payload.Stations[0].Orientation = 1;
                payload.Supply[0].RemainingPackages = 2;
                var supply = Tables.Instance.DTSupplyConfig.Get(payload.Supply[0].SourceId);
                payload.Shipments = Enumerable.Range(1, supply.AvailablePackages - 2).Select(index => new SoloSavedShipment
                {
                    Key = 1000 + index, SourceId = supply.Id, MaterialDefinitionId = supply.MaterialDefinitionId,
                    PackageDefinitionId = supply.PackageDefinitionId, PackageUnits = supply.PackageUnits,
                    RequiredTicks = supply.TransitTicks, ProgressTicks = 0, Stage = (int)ShipmentStage.InTransit,
                    ReceivingStationKey = payload.Stations.Single(station => station.InitialLayoutId == supply.ReceivingLayoutId).Key
                }).ToArray();
                var checkpoint = SelectionHeader(profile);
                checkpoint.CompletedLevelId = 1101; checkpoint.NextLevelId = 1102;
                checkpoint.ChapterId = 11; checkpoint.ChapterStage = 1; checkpoint.Score = 30; checkpoint.Stars = 3; checkpoint.ServedOrders = 3;
                repository.Commit(checkpoint, payload);
                var complete = SelectionHeader(profile);
                complete.CompletedLevelId = 1103; complete.NextLevelId = 0;
                complete.ChapterId = 11; complete.ChapterStage = 3; complete.RecordKind = SoloSaveRecordKind.ChapterCompleted;
                complete.Score = 70; complete.Stars = 3; complete.ServedOrders = 7;
                repository.Commit(complete, payload);
                string checkpointPath = Path.Combine(repository.DirectoryPath, checkpoint.SlotId + ".ccsave");
                string completedPath = Path.Combine(repository.DirectoryPath, complete.SlotId + ".ccsave");
                byte[] previousCheckpoint = File.ReadAllBytes(checkpointPath), previousComplete = File.ReadAllBytes(completedPath);
                Selection.OpenSoloSelection();
                yield return Wait(() => ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Headers.Length == 2, "Old valid files are browsed through the real entry");
                yield return ReachAndClick(SelectionButton("继续记录"));
                var record = SelectionHud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text.StartsWith("查看完成 记录"));
                yield return ReachAndClick(record);
                yield return Wait(() => Selection.ViewedResult?.SlotId == complete.SlotId, "Existing completed result is read only");
                Assert.That(Selection.ViewedResult.Score, Is.EqualTo(70)); Assert.That(Selection.ViewedResult.Stars, Is.EqualTo(3));
                Assert.That((Scene)Runtime.HostScene, Is.Null);
                yield return ReachAndClick(SelectionButton("返回记录列表"));
                yield return InspectAndContinueSave(checkpoint.SlotId);
                yield return Wait(() => Presentation?.Ready == true && Grid.LevelConfigId == 1102, "Existing saved Next remains 1102");
                Assert.That(Tables.Instance.DTLevelConfig.Get(Grid.LevelConfigId).OperatingTicks, Is.EqualTo(2100));
                var savedCell = new GridCell(payload.Stations[0].X, payload.Stations[0].Y);
                Assert.That(Grid.Stations.Values.Single(station => station.Cell.Equals(savedCell)).Orientation, Is.EqualTo((GridOrientation)1));
                Assert.That(Grid.Items.Count, Is.EqualTo(payload.Items.Length));
                Assert.That(Grid.SupplySources[payload.Supply[0].SourceId].RemainingPackages, Is.EqualTo(2));
                Assert.That(File.ReadAllBytes(checkpointPath), Is.EqualTo(previousCheckpoint));
                Assert.That(File.ReadAllBytes(completedPath), Is.EqualTo(previousComplete), "New timing never recalculates a historical completion record");
            }
            finally
            {
                Runtime.StopProduct(); repository.Dispose(); RemoveOwnSaveUiFiles(directory);
            }
        }
    }
}
#endif
