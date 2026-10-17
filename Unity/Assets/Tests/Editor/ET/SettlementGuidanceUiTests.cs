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
        [UnityTest, Category("EntrySettlement"), Category("SettlementNetwork"), Timeout(300000)]
        public IEnumerator EntrySettlementActualKcpMemoryThreeZeroStarResultsReadonlyDetailsAndTerminalRoomReturn()
        {
            yield return StartProgressHostRoom();
            yield return StartPreviewAndConfirm(EntryButton("开启内存餐厅"));
            yield return Wait(() => Presentation?.Ready == true, "Ordinary Host first chapter");
            Match match = ((HostSessionComponent)Runtime.Authority).Match;
            string matchId = match.MatchId;
            Presentation.CloseGridHud();
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases singleton display, keeps owner");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Actual KCP reader");
                remote.Presentation.CloseGridHud();
                yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Remote releases singleton for Host");
                Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation.Ready, "Host opens retained owner");
                for (int stage = 1; stage <= 3; ++stage)
                {
                    RestaurantRuntime restaurant = match.Restaurant;
                    ((Level)restaurant.CurrentLevel).OperatingTicks = 100;
                    ((GridKitchenComponent)restaurant.Kitchen).Dirty = true;
                    yield return ContentUiPump(UnlockUiStartOperating(), remote);
                    yield return WaitCoopUi(remote, () => Grid.LevelPhase == LevelPhase.Completed && remote.Grid.LevelPhase == LevelPhase.Completed &&
                        (stage == 3 || Grid.RenovationChoiceVersion > 0), "Real front closure and committed result " + stage, 75);
                    Assert.That(Grid.LevelStars, Is.Zero);
                    Assert.That(Hud.SettlementText.text, Does.Contain("内存合作").And.Not.Contain("记录已提交"));
                    long before = Client.NextCommandId;
                    if (stage == 1)
                    {
                        // 隔离模拟尚未收到候选的显示投影，不修改Host状态，不运行帧或发请求。
                        long version = Grid.RenovationChoiceVersion;
                        Grid.RenovationChoiceVersion = 0; Presentation.RefreshGridHud();
                        Assert.That(Hud.SettlementText.text, Does.Contain("等待装修候选准备"));
                        Assert.That(Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "下一关"), Is.False);
                        Grid.RenovationChoiceVersion = version; Presentation.RefreshGridHud();
                        Assert.That(Client.NextCommandId, Is.EqualTo(before));
                        // 保存协议本身由真实文件用例覆盖；这里隔离检查各已提交显示输入，不提交业务。
                        bool enabled = Grid.SaveEnabled;
                        SoloSaveState state = Grid.SaveState;
                        Grid.SaveEnabled = true; Grid.SaveState = SoloSaveState.Writing; Presentation.RefreshGridHud();
                        Assert.That(Hud.SettlementText.text, Does.Contain("保存中；尚未确认提交").And.Contain("等待保存完成"));
                        Assert.That(Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "保存并继续"), Is.False);
                        Grid.SaveState = SoloSaveState.Committed; Presentation.RefreshGridHud();
                        Assert.That(Hud.SettlementText.text, Does.Contain("记录已提交"));
                        Grid.SaveEnabled = enabled; Grid.SaveState = state; Presentation.RefreshGridHud();
                        Assert.That(Client.NextCommandId, Is.EqualTo(before));
                    }
                    Pointer(Button("结算详情"));
                    if (stage < 3)
                    {
                        var next = Tables.Instance.DTLevelConfig.Get(Grid.NextLevelConfigId);
                        foreach (int dish in next.AllowedDishIds.Concat(next.AllowedDrinkIds))
                        {
                            Assert.That(Hud.DetailsText.text, Does.Contain(Tables.Instance.DTDishConfig.Get(dish).Name));
                        }
                        Assert.That(Hud.DetailsText.text, Does.Contain("新增：").And.Contain("移出：").And.Contain("个人已领取/可领取/关闭"));
                    }
                    else
                    {
                        Assert.That(Hud.DetailsText.text, Does.Contain("返回房间，无下一关").And.Contain("零星不阻止完成"));
                    }
                    Pointer(Button("关闭结算详情"));
                    Assert.That(Client.NextCommandId, Is.EqualTo(before));
                    Presentation.CloseGridHud();
                    yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases for current remote result");
                    remote.Presentation.OpenGridHud();
                    yield return WaitCoopUi(remote, () => remote.Presentation.Ready, "Remote reads current result");
                    long remoteCount = remote.Client.NextCommandId;
                    Pointer(ClaimUiButton(remote.Hud, "结算详情"));
                    Assert.That(remote.Hud.SettlementText.text, Does.Contain("等待 Host"));
                    Assert.That(remote.Hud.DetailsText.text, Does.Contain("结算、文件提交和个人领取是不同状态"));
                    if (stage == 1)
                    {
                        bool cooperativeSave = remote.Grid.CooperativeSave;
                        remote.Grid.CooperativeSave = true; remote.Presentation.RefreshGridHud();
                        Assert.That(remote.Hud.SettlementText.text, Does.Contain("合作结算；个人领取另查凭证").And.Not.Contain("记录已提交"));
                        remote.Grid.CooperativeSave = cooperativeSave; remote.Presentation.RefreshGridHud();
                        Assert.That(remote.Client.NextCommandId, Is.EqualTo(remoteCount));
                    }
                    Assert.That(remote.Hud.GetComponentsInChildren<Button>(false).Any(button =>
                        new[] { "下一关", "保存并继续", "完成大关并返回房间" }.Contains(button.GetComponentInChildren<Text>().text)), Is.False);
                    var oldDetails = CaptureButtonCallback(ClaimUiButton(remote.Hud, "关闭结算详情"));
                    remote.Presentation.CloseGridHud();
                    yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Remote closes current result");
                    oldDetails(); Assert.That(remote.Client.NextCommandId, Is.EqualTo(remoteCount));
                    Presentation.OpenGridHud();
                    yield return WaitCoopUi(remote, () => Presentation.Ready, "Host reopens result without repeated operation");
                    if (stage < 3)
                    {
                        yield return WaitCoopUi(remote, () => Hud.GetComponentsInChildren<Button>(false).Any(button =>
                            button.GetComponentInChildren<Text>().text == "下一关"), "Host controls reopen for actual next action: stage=" + stage +
                            "; config=" + Grid.LevelConfigId + "; control=" + Grid.CanControlRestaurant + "; save=" + Grid.SaveEnabled +
                            "; renovation=" + Grid.RenovationChoiceVersion + "; summary=" + Hud.SettlementText.text);
                        var staleContinue = CaptureButtonCallback(Button("下一关"));
                        long level = Grid.LevelId;
                        yield return ContentUiPump(Command("下一关"), remote);
                        yield return WaitCoopUi(remote, () => remote.Grid.LevelId == Grid.LevelId && Grid.LevelId != level, "Both advance once");
                        long command = Client.NextCommandId; staleContinue();
                        Assert.That(Client.NextCommandId, Is.EqualTo(command));
                    }
                }
                var retired = CaptureButtonCallback(Button("完成大关并返回房间"));
                long count = Client.NextCommandId; Pointer(Button("完成大关并返回房间")); retired();
                Assert.That(Client.NextCommandId, Is.EqualTo(count + 1));
                yield return WaitCoopUi(remote, () => Presentation == null && (RestaurantRuntime)match.Restaurant == null &&
                    ((UIGridKitchen)Entry?.Hud)?.Available == true, "Actual terminal returns same room, no fourth level");
                Assert.That(match.MatchId, Is.EqualTo(matchId));
                Assert.That(Client.Roster.Count, Is.EqualTo(2));
                Assert.That(((ProcessInnerSender)Runtime.Sender).IsStopped, Is.False);
                Assert.That(remote.Grid.Confirmed, Is.False);
            }
        }
    }
}
#endif
