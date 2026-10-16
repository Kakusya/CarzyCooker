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
        [Category("CooperativeUpgrade")]
        [Category("SettlementNetwork")]
        public IEnumerator CoopUpgradeUiOrdinaryHostChoiceRemoteTargetsAndNewWorkKeepStation()
        {
            yield return StartCoopHostLauncher();
            long station = Grid.Stations.Values.Single(value => value.Name == "切配台").Id;
            var original = Grid.Stations[station];
            long tomato = Grid.Stations.Values.Single(value => value.Name == "备料台").ItemId;
            yield return Command("拿 备料台 ·", true);
            yield return Command("放 切配台 ·", true);
            string buff = Hud.GetComponentsInChildren<Button>(false).First(button =>
                button.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
            yield return Command(buff);
            yield return Command("开始营业");
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationChoiceVersion > 0,
                "Ordinary cooperative settlement offers upgrades", 90);
            Assert.That(Grid.RenovationCandidates, Has.Member(3));
            var retired = CaptureButtonCallback(Button("装修 升级切配台", true));
            Presentation.CloseGridHud();
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Retain Host owner while releasing singleton HUD");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Remote reads current upgrade targets");
                long readCommand = remote.Client.NextCommandId;
                Pointer(ClaimUiButton(remote.Hud, "结算详情"));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("可升级目标：").And.Contain("切配台").And.Contain("等级 0→1"));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(readCommand));
                Assert.That(remote.Hud.GetComponentsInChildren<Button>(false).Any(button =>
                    button.GetComponentInChildren<Text>().text.StartsWith("装修 ") ||
                    button.GetComponentInChildren<Text>().text == "下一关"), Is.False);
                Assert.That(remote.Grid.UpgradeTargets.Any(target => target.DefinitionId == 3 && target.StationId == station), Is.True);
                remote.Presentation.CloseGridHud();
                yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Remote releases singleton HUD");
                Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation?.Ready == true && Hud.GetComponentsInChildren<Button>(false).Any(button =>
                    button.GetComponentInChildren<Text>().text.StartsWith("装修 升级切配台")), "Host managed controls reopen");
                long before = Client.NextCommandId;
                retired();
                Assert.That(Client.NextCommandId, Is.EqualTo(before));
                yield return Command("装修 升级切配台", true);
                Assert.That(Grid.SelectedUpgradeStationId, Is.EqualTo(station));
                Assert.That(Grid.Stations[station].UpgradeLevel, Is.Zero);
                Assert.That(Hud.SettlementText.text, Does.Contain("目标：切配台").And.Contain("等级 0→1"));
                long level = Grid.LevelId;
                yield return Command("下一关");
                yield return WaitCoopUi(remote, () => remote.Grid.LevelId != level && remote.Grid.Stations[station].UpgradeLevel == 1,
                    "KCP sees original station upgraded in the next level");
                Assert.That(Grid.Stations[station].Cell, Is.EqualTo(original.Cell));
                Assert.That(Grid.Stations[station].ItemId, Is.EqualTo(tomato));
                Assert.That(Grid.Stations[station].DefinitionId, Is.EqualTo(original.DefinitionId));
                Assert.That(Grid.SelectedBuffId, Is.Zero);
                Pointer(Button("全部动作"));
                yield return Command("切配 切配台 ·", true);
                var work = Grid.Processes.Values.Single(value => value.InputItemId == tomato);
                Assert.That(work.RequiredTicks, Is.EqualTo(45), "First new work uses the installed 3/4 upgrade");
                yield return WaitCoopUi(remote, () => remote.Grid.Processes.ContainsKey(work.Id), "Remote receives new work frozen duration");
                Assert.That(remote.Grid.Processes[work.Id].RequiredTicks, Is.EqualTo(45));
                yield return WaitCoopUi(remote, () => Grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.TomatoChunks),
                    "New upgraded work completes through ordinary Tick");
                Assert.That(Grid.RenovationApplied[3], Is.EqualTo(new[] { station }));
                before = Client.NextCommandId;
                retired();
                Assert.That(Client.NextCommandId, Is.EqualTo(before));
            }
        }
    }
}
#endif
