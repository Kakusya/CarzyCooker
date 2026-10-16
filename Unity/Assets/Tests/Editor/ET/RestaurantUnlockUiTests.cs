#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
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
        private IEnumerator UnlockUiStartOperating()
        {
            string buff = Hud.GetComponentsInChildren<Button>(false).First(button => button.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
            yield return Command(buff);
            yield return Command("开始营业");
        }

        private IEnumerator UnlockUiNext(bool cooperative)
        {
            yield return Wait(() => Grid.RenovationLevelId == Grid.LevelId && Grid.RenovationLevelGeneration == Grid.LevelGeneration &&
                Grid.RenovationChoiceVersion > 0, "Current level renovation preparation finishes before continuation");
            if (Grid.RenovationCandidates.Contains(8) && Grid.SelectedRenovationId == 0 && !Grid.RenovationSelectionFinalized)
            {
                yield return Command("装修 扩建单客桌");
            }
            long previous = Grid.LevelId;
            string caption = cooperative ? "下一关" : "保存并继续";
            yield return Wait(() => Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == caption),
                "Current managed continuation button appears for level " + Grid.LevelConfigId, 20);
            Pointer(Button(caption));
            yield return Wait(() => Grid.LevelId != previous && Grid.LevelPhase == LevelPhase.Preparing && Grid.BuffDrawVersion > 0, "Actual level continuation preserves the growth kitchen", 20);
        }

        private IEnumerator UnlockUiJourney(bool cooperative)
        {
            if (cooperative)
            {
                yield return StartProgressHostRoom(); StartPreviewAndConfirm(EntryButton("开启经营成长餐厅"));
            }
            else
            {
                yield return StartModeLauncher(); CreateSaveUiRepository(Runtime, out _, out _);
                Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Ordinary Solo selector opens");
                StartPreviewAndConfirm(SelectionButton("新开经营成长餐厅"));
            }
            yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Ordinary growth entry opens");
            Assert.That(Grid.LevelConfigId, Is.EqualTo(cooperative ? 801 : 701));
            Assert.That(Grid.AllowedDishIds, Has.No.Member(3));
            Assert.That(Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text.StartsWith("受限采购 橙子")), Is.True);
            yield return CoopFrontUiPrepareSoup();
            long bowl = Grid.Stations.Values.Single(station => station.Name == "碗台").ItemId;
            long pot = Grid.Stations.Values.Single(station => station.Name == "灶台").ItemId;
            var batch = Grid.AutoProcesses.Values.Single(process => process.PotItemId == pot);
            int portions = batch.RemainingPortions;
            Assert.That(portions, Is.EqualTo(1));
            yield return Command("开始营业");
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationCandidates.Contains(7), "First growth stage naturally settles", 70);
            int itemCount = Grid.Items.Count, stationCount = Grid.Stations.Count;
            var retiredReward = CaptureButtonCallback(Button("装修 解锁现榨菜单"));
            yield return Command("装修 解锁现榨菜单");
            Assert.That(Grid.UnlockedDishIds, Does.Contain(3)); Assert.That(Grid.UnlockedStationIds, Does.Contain(10));
            Assert.That(Hud.SettlementText.text, Does.Contain("定义已解锁，不补发食材或设备"));
            Assert.That(Grid.Items.Count, Is.EqualTo(itemCount)); Assert.That(Grid.Stations.Count, Is.EqualTo(stationCount));
            yield return UnlockUiNext(cooperative);
            long command = Client.NextCommandId; retiredReward(); Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.AllowedDishIds, Does.Contain(3));
            yield return Command("拿 橙子台 ·", true); yield return Command("放 切配台 ·", true); yield return Command("切配 切配台 ·", true);
            yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.OrangeChunks), "Unlocked orange is actually chopped");
            yield return Command("拿 切配台 ·", true); yield return Command("添加 果汁壶台 ·", true);
            yield return Command("拿 果汁壶台 ·", true); yield return Command("放 榨汁机 ·", true); yield return Command("榨汁 榨汁机 ·", true);
            yield return Wait(() => Grid.AutoProcesses.Values.Any(process => process.State == GridAutoState.Ready && process.LockContainer), "Unlocked juicer actually completes");
            yield return Command("拿 杯台 ·", true); long cup = Grid.Actors[Client.MemberId].HandItemId;
            yield return Command("盛一份 榨汁机 ·", true); yield return Command("放 杯台 ·", true);
            Assert.That(Grid.Items[cup].Contents.Count, Is.EqualTo(1));
            yield return Command("拿 第二碗台 ·", true);
            var retiredPortion = CaptureButtonCallback(Button("盛一份 灶台 ·", true));
            yield return Command("放 第二碗台 ·", true);
            yield return UnlockUiStartOperating();
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationChoiceVersion > 0, "Second growth stage naturally settles", 70);
            var items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            yield return UnlockUiNext(cooperative);
            Assert.That(Grid.AllowedDishIds, Is.EqualTo(new[] { 4 }));
            Assert.That(Grid.UnlockedDishIds, Does.Contain(3), "Permanent unlock survives narrower current menu");
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            Assert.That(Grid.Items[bowl].Contents.Count, Is.EqualTo(1)); Assert.That(Grid.Items[cup].Contents.Count, Is.EqualTo(1));
            Assert.That(Grid.AutoProcesses[batch.Id].RemainingPortions, Is.EqualTo(portions));
            command = Client.NextCommandId; retiredPortion(); Assert.That(Client.NextCommandId, Is.EqualTo(command), "Previous-level portion callback cannot send");
            CoopUiRemote remote = null;
            if (cooperative)
            {
                long movement = Client.NextCommandId + 1; Presentation.ClickGridCell(new GridCell(2, 10));
                yield return Wait(() => Grid.LastCommandId == movement && Grid.LastStatus == GridResultStatus.Executed, "Host moves aside through map input");
                Presentation.CloseGridHud();
                remote = new CoopUiRemote(Runtime.ListenEndpoint); ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.Confirmed, "Independent KCP player sees the narrowed kitchen");
            }
            try
            {
                var client = remote?.Client ?? Client; var view = remote?.Grid ?? Grid;
                MonoUIGridKitchen CurrentHud() => remote?.Hud ?? Hud;
                Assert.That(view.AllowedDishIds, Is.EqualTo(new[] { 4 })); Assert.That(view.UnlockedDishIds, Does.Contain(3));
                Assert.That(view.Items[bowl].Contents.Count, Is.EqualTo(1)); Assert.That(view.Items[cup].Contents.Count, Is.EqualTo(1));
                yield return AssemblyUiCommand(remote, "拿 第二碗台 ·", true);
                long empty = view.Actors[client.MemberId].HandItemId;
                long rejected = client.NextCommandId + 1;
                Pointer(ClaimUiButton(CurrentHud(), "盛一份 灶台 ·", true));
                bool Rejected() => view.LastCommandId == rejected && view.LastStatus == GridResultStatus.Rejected;
                if (remote == null)
                {
                    yield return Wait(Rejected, "Solo completed batch rejects new portion");
                }
                else
                {
                    yield return WaitCoopUi(remote, Rejected, "KCP completed batch rejects new portion");
                }
                Assert.That(view.LastReason, Is.EqualTo(nameof(ProductionPermissionDecision.MenuRestricted)));
                Assert.That(CurrentHud().StatusText.text, Does.Contain("本关未开放该菜品").And.Contain("新盛装"));
                Assert.That(view.Items[empty].Contents, Is.Empty);
                Assert.That(view.AutoProcesses[batch.Id].RemainingPortions, Is.EqualTo(portions));
                Assert.That(view.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            }
            finally { remote?.Dispose(); }
        }

        [UnityTest, Category("RestaurantUnlockUi")]
        public IEnumerator RestaurantUnlockUiSoloOrdinaryUnlockMakeAndNarrowWithoutLosingOldFood() => UnlockUiJourney(false);

        [UnityTest, Category("RestaurantUnlockUi")]
        public IEnumerator RestaurantUnlockUiCooperativeOrdinaryUnlockMakeAndRemoteRestrictedPortion() => UnlockUiJourney(true);
    }
}
#endif
