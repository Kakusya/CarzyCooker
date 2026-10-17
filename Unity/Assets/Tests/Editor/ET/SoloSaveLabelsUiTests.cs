#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
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
        private Button LabelledSelectionRow(string slot)
        {
            int number = Array.FindIndex(Selection.Headers, value => value.SlotId == slot) + 1;
            return SelectionHud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text.Contains("记录" + number + " ·"));
        }

        [UnityTest, Category("SoloSaveLabels"), Timeout(120000)]
        public IEnumerator EntrySaveLabelsActualSelectorNamesFavoritesCancelOldRowsPoolRevisionAndContinue()
        {
            yield return StartLauncher(false, false);
            var repository = CreateSaveUiRepository(Runtime, out _, out string profile);
            var first = SelectionHeader(profile); var second = SelectionHeader(profile);
            var payload = SelectionFixture();
            repository.Commit(first, payload); repository.Commit(second, payload);
            string firstPath = Path.Combine(repository.DirectoryPath, first.SlotId + ".ccsave"), secondPath = Path.Combine(repository.DirectoryPath, second.SlotId + ".ccsave");
            byte[] firstBytes = File.ReadAllBytes(firstPath), secondBytes = File.ReadAllBytes(secondPath);
            Selection.OpenSoloSelection();
            yield return Wait(() => Selection.Headers.Length == 2 && Selection.Labels != null, "Actual records and local metadata browse");
            Pointer(LabelledSelectionRow(first.SlotId));
            yield return Wait(() => Selection.InspectedSave?.SlotId == first.SlotId && !Selection.LoadingSelection, "Full first record validation");
            Assert.That(Selection.SaveInspectionRows.Any(value => value.Contains(first.SlotId.Substring(0, 8))), Is.True);
            Pointer(SelectionButton("编辑名称与收藏"));
            SelectionHud.ShowEntryAddress("常用小馆", 4096);
            Pointer(SelectionButton("收藏此记录（草稿）"));
            var retiredSave = CaptureButtonCallback(SelectionButton("保存名称与收藏"));
            Pointer(SelectionButton("保存名称与收藏"));
            yield return Wait(() => Selection.LabelDraft?.Busy == false && Selection.LabelDraft.Feedback.Contains("已保存"), "Actual label/favorite file publication");
            Pointer(SelectionButton("取消名称编辑"));
            Pointer(SelectionButton("返回记录列表"));
            Pointer(SelectionButton("收藏记录"));
            Assert.That(LabelledSelectionRow(first.SlotId).GetComponentInChildren<Text>().text, Does.StartWith("★ 常用小馆 ·"));
            Assert.That(SelectionHud.GetComponentsInChildren<Button>(false).Count(button => button.GetComponentInChildren<Text>().text.Contains("记录") && button.GetComponentInChildren<Text>().text.Contains(" · ")), Is.EqualTo(1));
            Pointer(SelectionButton("全部记录"));
            Pointer(LabelledSelectionRow(second.SlotId));
            yield return Wait(() => Selection.InspectedSave?.SlotId == second.SlotId && !Selection.LoadingSelection, "Second independent slot");
            Pointer(SelectionButton("编辑名称与收藏"));
            SelectionHud.ShowEntryAddress("取消的名字", 4096);
            retiredSave();
            Assert.That(Selection.LabelDraft.SlotId, Is.EqualTo(second.SlotId));
            Assert.That(SelectionHud.EntryAddress, Is.EqualTo("取消的名字"));
            Pointer(SelectionButton("取消名称编辑"));
            Assert.That(repository.ReadSaveLabels().Entries.Any(value => value.SlotId == second.SlotId), Is.False);
            Selection.CloseSoloSelection(); yield return null; Selection.OpenSoloSelection();
            yield return Wait(() => ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Labels?.Entries.Length == 1, "Pooled selector reloads committed labels");
            retiredSave();
            Assert.That(Selection.LabelDraft, Is.Null);
            Pointer(LabelledSelectionRow(first.SlotId));
            yield return Wait(() => Selection.InspectedSave?.SlotId == first.SlotId && !Selection.LoadingSelection, "Original named record selected again");
            Assert.That(SelectionHud.SettlementText.text, Does.Contain("常用小馆").And.Contain("已收藏"));
            var retiredContinue = CaptureButtonCallback(SelectionButton("确认继续此记录"));
            first.Revision = 2; first.ResultId = Guid.NewGuid().ToString("N"); first.SavedUtcTicks++;
            byte[] revised = SoloSaveFile.Encode(first, payload);
            File.WriteAllBytes(firstPath, revised);
            Pointer(SelectionButton("确认继续此记录"));
            yield return Wait(() => Selection.InspectedSave?.Revision == 2 && !Selection.LoadingSelection, "Original Select detects changed revision despite same alias");
            Assert.That(Selection.Failure, Is.EqualTo("RecordChanged"));
            Assert.That((Scene)Runtime.HostScene, Is.Null);
            retiredContinue();
            Assert.That((Scene)Runtime.HostScene, Is.Null);
            Assert.That(repository.ReadSaveLabels().Entries.Single().Name, Is.EqualTo("常用小馆"));
            Pointer(SelectionButton("确认继续此记录"));
            yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Original explicit reconfirm restores current revision");
            yield return Command("拿 碗台 ·", true);
            Assert.That(File.ReadAllBytes(firstPath), Is.EqualTo(revised));
            Assert.That(File.ReadAllBytes(secondPath), Is.EqualTo(secondBytes));
            Assert.That(firstBytes, Is.Not.EqualTo(revised), "Only the controlled test revision fixture changed the record");
        }

        [UnityTest, Category("SoloSaveLabels"), Timeout(120000)]
        public IEnumerator EntrySaveLabelsActualSelectorLockedSaveRetryAndCorruptMetadataStillAllowsOriginalSelect()
        {
            yield return StartLauncher(false, false);
            var repository = CreateSaveUiRepository(Runtime, out _, out string profile);
            var header = SelectionHeader(profile);
            repository.Commit(header, SelectionFixture());
            repository.WriteSaveLabel(header.SlotId, header.Revision, header.ResultId, "原名称", false);
            string metadata = Path.Combine(repository.DirectoryPath, SoloSaveRepositorySystem.LabelsFileName);
            byte[] original = File.ReadAllBytes(metadata);
            string record = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
            byte[] recordBytes = File.ReadAllBytes(record);
            Selection.OpenSoloSelection();
            yield return Wait(() => Selection.Headers.Length == 1 && Selection.Labels != null, "Actual labelled record browse");
            Pointer(LabelledSelectionRow(header.SlotId));
            yield return Wait(() => Selection.InspectedSave != null && !Selection.LoadingSelection, "Actual full file inspection");
            Pointer(SelectionButton("编辑名称与收藏"));
            SelectionHud.ShowEntryAddress("新名称", 4096);
            using (var locked = new FileStream(metadata, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Pointer(SelectionButton("保存名称与收藏"));
                yield return Wait(() => Selection.LabelDraft?.Busy == false, "Actual locked Replace returns failure");
                Assert.That(Selection.LabelDraft.Feedback, Does.Contain("失败"));
                Assert.That(Selection.Labels.Entries.Single().Name, Is.EqualTo("原名称"));
                Assert.That(SelectionHud.EntryAddress, Is.EqualTo("新名称"));
            }
            Assert.That(File.ReadAllBytes(metadata), Is.EqualTo(original));
            Pointer(SelectionButton("保存名称与收藏"));
            yield return Wait(() => Selection.LabelDraft?.Busy == false && Selection.LabelDraft.Feedback.Contains("已保存"), "Explicit UI retry succeeds");
            Pointer(SelectionButton("取消名称编辑"));
            File.WriteAllBytes(metadata, new byte[] { 222, 1, 7 });
            long render = Selection.RenderVersion;
            Pointer(SelectionButton("刷新记录"));
            yield return Wait(() => Selection.RenderVersion > render && Selection.LabelsFailure != null, "Metadata corruption is visible without invalidating the product record");
            Assert.That(SelectionHud.StatusText.text, Does.Contain("名称与收藏").And.Contain("进度记录仍可查看"));
            Assert.That(Selection.InspectedSave.SlotId, Is.EqualTo(header.SlotId));
            Assert.That(File.ReadAllBytes(record), Is.EqualTo(recordBytes));
            Pointer(SelectionButton("确认继续此记录"));
            yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Valid original progress still restores with corrupt optional metadata");
            Assert.That(File.ReadAllBytes(record), Is.EqualTo(recordBytes));
            Assert.That(File.ReadAllBytes(metadata), Is.EqualTo(new byte[] { 222, 1, 7 }));
        }
    }
}
#endif
