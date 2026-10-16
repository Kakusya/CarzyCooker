#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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
        private static SoloSavePayload InspectionRichFixture()
        {
            SoloSavePayload payload = SelectionFixture();
            var recipe = Tables.Instance.DTGridAutoRecipeConfig.Get(1);
            SoloSavedItem pot = payload.Items.Single(item => item.DefinitionId == recipe.ContainerDefinitionId);
            payload.Stations.Single(station => station.ItemKey == pot.Key).ItemKey = 0;
            var stove = payload.Stations.First(station =>
                (Tables.Instance.DTStationConfig.Get(station.DefinitionId).Capabilities & recipe.StationCapability) != 0 && station.ItemKey == 0);
            pot.HolderKey = stove.Key; pot.X = stove.X; pot.Y = stove.Y;
            stove.ItemKey = pot.Key;
            var items = payload.Items.ToList();
            var inputs = new List<int>();
            int key = items.Max(item => item.Key);
            for (int index = 0; index < recipe.InputDefinitionIds.Count; ++index)
            {
                for (int count = 0; count < recipe.InputCounts[index]; ++count)
                {
                    var item = new SoloSavedItem
                    {
                        Key = ++key, DefinitionId = recipe.InputDefinitionIds[index], Location = (int)GridItemLocation.Container,
                        HolderKey = pot.Key, X = pot.X, Y = pot.Y
                    };
                    items.Add(item); inputs.Add(item.Key);
                }
            }
            pot.ContentKeys = inputs.ToArray(); payload.Items = items.ToArray();
            payload.Batches = new[] { new SoloSavedBatch
            {
                Key = 1, RecipeId = recipe.Id, PotKey = pot.Key, StationKey = stove.Key, InputKeys = pot.ContentKeys,
                RequiredTicks = recipe.RequiredTicks, ProgressTicks = recipe.RequiredTicks, State = (int)GridAutoState.Ready,
                OutputDefinitionId = recipe.OutputDefinitionId, TotalPortions = recipe.OutputPortions, RemainingPortions = 1,
                ServingContainerDefinitionId = recipe.ServingContainerDefinitionId, RequiredCapability = (int)recipe.StationCapability,
                LockContainer = recipe.LockContainer
            } };
            var source = Tables.Instance.DTSupplyConfig.DataList.First(row => row.MenuVariant == ProductMenuVariant.Basic);
            payload.Supply.Single(balance => balance.SourceId == source.Id).RemainingPackages--;
            payload.Shipments = new[] { new SoloSavedShipment
            {
                Key = 1, SourceId = source.Id, MaterialDefinitionId = source.MaterialDefinitionId, PackageDefinitionId = source.PackageDefinitionId,
                PackageUnits = source.PackageUnits, RequiredTicks = source.TransitTicks + 10000, ProgressTicks = 1,
                ReceivingStationKey = payload.Stations.Single(station => station.InitialLayoutId == source.ReceivingLayoutId).Key,
                Stage = (int)ShipmentStage.InTransit
            } };
            return payload;
        }

        private static SoloSaveHeader InspectionCompletedHeader(string profile)
        {
            var header = SelectionHeader(profile);
            var terminal = Tables.Instance.DTLevelConfig.Get(103);
            header.CompletedLevelId = terminal.Id; header.NextLevelId = 0; header.ChapterId = terminal.ChapterId;
            header.ChapterStage = terminal.ChapterStage; header.RecordKind = SoloSaveRecordKind.ChapterCompleted;
            return header;
        }

        [UnityTest, Category("EntryExperience")]
        public IEnumerator EntrySaveInspectionRichFileRequiresReconfirmAfterRevisionAndRestoresHalfBatchAndShipment()
        {
            yield return StartLauncher(false, false);
            ProductRuntimeComponent runtime = Runtime;
            var repository = CreateSaveUiRepository(runtime, out string directory, out string profile);
            try
            {
                SoloSavePayload payload = InspectionRichFixture();
                SoloSaveHeader selected = SelectionHeader(profile);
                repository.Commit(selected, payload);
                SoloSaveHeader older = SelectionHeader(profile); older.SavedUtcTicks = selected.SavedUtcTicks - 10;
                repository.Commit(older, SelectionFixture());
                repository.Commit(InspectionCompletedHeader(profile), SelectionFixture());
                Selection.OpenSoloSelection();
                yield return Wait(() => ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Headers.Length == 3, "Actual rich file headers browse");
                Assert.That(Selection.Headers.Take(2).All(header => header.RecordKind == SoloSaveRecordKind.Continue), Is.True);
                Assert.That(Selection.Headers[0].SlotId, Is.EqualTo(selected.SlotId));
                Pointer(SelectionButton("已完成记录"));
                Assert.That(SelectionHud.GetComponentsInChildren<Button>(false).Count(button => button.GetComponentInChildren<Text>().text.StartsWith("查看完成 记录")), Is.EqualTo(1));
                Pointer(SelectionButton("可继续记录"));
                Assert.That(SelectionHud.GetComponentsInChildren<Button>(false).Count(button => button.GetComponentInChildren<Text>().text.StartsWith("继续 记录")), Is.EqualTo(2));
                Pointer(SelectionSlotButton(selected.SlotId));
                yield return Wait(() => Selection.InspectedSave?.SlotId == selected.SlotId && !Selection.LoadingSelection, "Full validation produces display summary");
                Assert.That(Selection.SaveInspectionRows.Any(row => row.Contains("剩余 1/2 份")), Is.True);
                Assert.That(Selection.SaveInspectionRows.Any(row => row.Contains("运输中")), Is.True);
                Assert.That((Scene)runtime.HostScene, Is.Null);
                Assert.That((ClientSessionComponent)runtime.Client, Is.Null);
                long render = Selection.RenderVersion;
                Pointer(SelectionButton("刷新记录"));
                yield return Wait(() => Selection.RenderVersion > render, "Refresh completes");
                Assert.That(Selection.InspectedSave?.SlotId, Is.EqualTo(selected.SlotId), "Valid selection survives header refresh");
                var retired = CaptureButtonCallback(SelectionButton("确认继续此记录"));
                selected.Revision = 2; selected.ResultId = Guid.NewGuid().ToString("N"); selected.SavedUtcTicks++;
                string path = Path.Combine(repository.DirectoryPath, selected.SlotId + ".ccsave");
                byte[] changed = SoloSaveFile.Encode(selected, payload);
                File.WriteAllBytes(path, changed);
                Pointer(SelectionButton("确认继续此记录"));
                yield return Wait(() => Selection.InspectedSave?.Revision == 2 && !Selection.LoadingSelection, "Changed file replaces preview instead of starting");
                Assert.That(Selection.Failure, Is.EqualTo("RecordChanged"));
                Assert.That((Scene)runtime.HostScene, Is.Null);
                retired();
                Assert.That((Scene)runtime.HostScene, Is.Null);
                Pointer(SelectionButton("确认继续此记录"));
                yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Explicit reconfirmation restores fresh payload");
                Assert.That(Grid.AutoProcesses.Values.Single().RemainingPortions, Is.EqualTo(1));
                Assert.That(Grid.SupplyShipments.Values.Single().Stage, Is.EqualTo(ShipmentStage.InTransit));
                Match match = ((HostSessionComponent)runtime.Authority).Match;
                Assert.That(match.GetComponent<SoloSaveBinding>().SourceRevision, Is.EqualTo(2));
                Assert.That(Grid.LastSavedSlotId, Is.EqualTo(selected.SlotId));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(changed));
                Assert.That(repository.Browse().Length, Is.EqualTo(3));
                yield return Command("拿 碗台 ·", true);
            }
            finally
            {
                runtime.StopProduct(); repository.Dispose(); RemoveOwnSaveUiFiles(directory);
            }
        }

        [UnityTest, Category("EntryExperience")]
        public IEnumerator EntrySaveInspectionLegacyFileCorruptionCancellationPoolAndCompletedRecordStayReadOnly()
        {
            yield return StartLauncher(false, false);
            ProductRuntimeComponent runtime = Runtime;
            var repository = CreateSaveUiRepository(runtime, out string directory, out string profile);
            try
            {
                SoloSaveHeader legacy = SelectionHeader(profile); legacy.Version = 1;
                repository.Commit(legacy, SelectionFixture());
                SoloSaveHeader completed = InspectionCompletedHeader(profile);
                repository.Commit(completed, SelectionFixture());
                Selection.OpenSoloSelection();
                yield return Wait(() => ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Headers.Length == 2, "Legacy and completed files browse");
                Pointer(SelectionSlotButton(legacy.SlotId));
                yield return Wait(() => Selection.InspectedSave != null && !Selection.LoadingSelection, "Old valid format fully validates for inspection");
                Assert.That(Selection.InspectedSave.Version, Is.EqualTo(1));
                var oldConfirm = CaptureButtonCallback(SelectionButton("确认继续此记录"));
                string path = Path.Combine(repository.DirectoryPath, legacy.SlotId + ".ccsave");
                byte[] original = File.ReadAllBytes(path);
                File.WriteAllBytes(path, new byte[] { 0 });
                Pointer(SelectionButton("确认继续此记录"));
                yield return Wait(() => !Selection.LoadingSelection && Selection.Failure != null, "Corruption is rejected before Host creation");
                Assert.That(Selection.InspectedSave, Is.Null);
                Assert.That((Scene)runtime.HostScene, Is.Null);
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(new byte[] { 0 }), "UI never repairs or writes a file");
                File.WriteAllBytes(path, original);
                Pointer(SelectionSlotButton(legacy.SlotId));
                Pointer(SelectionButton("取消加载"));
                yield return Wait(() => ((UIGridKitchen)Selection.Hud)?.Available == true && !Selection.LoadingSelection, "Cancellation reopens original owner");
                oldConfirm();
                Assert.That(Selection.InspectedSave, Is.Null);
                Assert.That((Scene)runtime.HostScene, Is.Null);
                Pointer(SelectionButton("已完成记录"));
                var complete = SelectionHud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text.StartsWith("查看完成 记录"));
                Pointer(complete);
                yield return Wait(() => Selection.InspectedSave?.SlotId == completed.SlotId && !Selection.LoadingSelection, "Completed file is inspected without running");
                Assert.That(SelectionHud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "确认继续此记录"), Is.False);
                Assert.That(SelectionHud.SettlementText.text, Does.Contain("已完成").And.Contain("仅供查看"));
                Assert.That((Scene)runtime.HostScene, Is.Null);
                Pointer(SelectionButton("返回记录列表"));
                Assert.That(Selection.SaveFilter, Is.EqualTo(SoloSaveListFilter.Completed));
                Assert.That(repository.Browse().Length, Is.EqualTo(2));
            }
            finally
            {
                runtime.StopProduct(); repository.Dispose(); RemoveOwnSaveUiFiles(directory);
            }
        }
    }
}
#endif
