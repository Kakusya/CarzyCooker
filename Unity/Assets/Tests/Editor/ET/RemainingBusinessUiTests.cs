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
        [UnityTest, Category("RemainingBusinessUi")]
        public IEnumerator RemainingBusinessUiActualSoloMenuBuffPartialRecipeAndMixerUpgradeContinue()
        {
            yield return StartModeLauncher(); CreateSaveUiRepository(Runtime, out _, out _);
            Pointer(EntryButton("单机餐厅"));
            yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Ordinary Solo content selection");
            yield return StartPreviewAndConfirm(SelectionButton("新开甜品餐厅"));
            yield return Wait(() => Presentation?.Ready == true && Grid.BuffDrawVersion > 0, "Actual dessert menu and candidate pool");
            Assert.That(Grid.BuffCandidates, Is.EquivalentTo(new[] { 13, 14 }));
            Assert.That(Button("选择 顺滑搅拌", true).GetComponentInChildren<Text>().text, Does.Contain("面糊").And.Not.Contain("煮汤"));
            yield return Command("选择 顺滑搅拌", true); Assert.That(Grid.SelectedBuffId, Is.EqualTo(13));
            yield return HotUiBuyRaw(null, 12, "甜点预拌粉"); yield return Command("添加 搅拌机 ·", true);
            long command = Client.NextCommandId;
            Pointer(Button("菜谱 原味小蛋糕")); yield return null;
            Assert.That(Hud.DetailsText.text, Does.Contain("还缺").And.Contain("鸡蛋 ×1").And.Contain("牛奶 ×1"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command)); Pointer(Button("关闭菜谱"));
            yield return HotUiDiscardPackage(null);
            yield return Command("开始营业");
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.UpgradeTargets.Any(target => target.DefinitionId == 12), "Natural dessert settlement offers real mixer upgrade", 140);
            long mixer = Grid.Stations.Values.Single(station => station.Name == "搅拌机").Id;
            var before = Grid.Stations[mixer];
            yield return Command("装修 高效搅拌机", true);
            Assert.That(Grid.SelectedUpgradeStationId, Is.EqualTo(mixer));
            long level = Grid.LevelId; Pointer(Button("保存并继续"));
            yield return Wait(() => Grid.LevelId != level && Grid.LevelPhase == LevelPhase.Preparing, "Actual file commit precedes new-level installation", 20);
            Assert.That(Grid.Stations[mixer].UpgradeLevel, Is.EqualTo(1));
            Assert.That(Grid.Stations[mixer].Cell, Is.EqualTo(before.Cell));
            Assert.That(Grid.Stations[mixer].ItemId, Is.EqualTo(before.ItemId));
        }

        [UnityTest, Category("RemainingBusinessUi")]
        public IEnumerator RemainingBusinessUiActualKcpClientReadsPartialRecipeAndApplicableBuffWithoutControl()
        {
            yield return StartProgressHostRoom(); yield return StartPreviewAndConfirm(EntryButton("开启甜品餐厅"));
            yield return Wait(() => Presentation?.Ready == true && Grid.BuffDrawVersion > 0, "Ordinary Host dessert entry");
            yield return Command("选择 顺滑搅拌", true);
            yield return HotUiBuyRaw(null, 12, "甜点预拌粉"); yield return Command("添加 搅拌机 ·", true);
            Presentation.CloseGridHud();
            var remote = new CoopUiRemote(Runtime.ListenEndpoint); ownedCoopUiRemotes.Add(remote);
            yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.SelectedBuffId == 13, "Independent KCP receives current content and effect");
            long command = remote.Client.NextCommandId;
            var recipe = remote.Hud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text == "菜谱 原味小蛋糕");
            Pointer(recipe); yield return null;
            Assert.That(remote.Hud.DetailsText.text, Does.Contain("还缺").And.Contain("鸡蛋 ×1").And.Contain("牛奶 ×1"));
            Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
            Assert.That(remote.Grid.CanControlRestaurant, Is.False);
            Assert.That(remote.Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text.StartsWith("选择 ")), Is.False);
        }
    }
}
#endif
