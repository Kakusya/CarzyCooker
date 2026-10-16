#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Linq;
using ET.Client;
using Game;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        private IEnumerator ChapterUiCommand(CoopUiRemote remote, string caption)
        {
            yield return WaitCoopUi(remote, () => Presentation?.Ready == true && Hud.GetComponentsInChildren<Button>(false).Any(button =>
                button.GetComponentInChildren<Text>().text == caption), "Current managed button becomes visible: " + caption);
            yield return Command(caption);
        }

        [UnityTest]
        [Category("CooperativeChapterUi")]
        public IEnumerator CooperativeChapterUiOrdinaryHostThreeLevelsRemoteResultFinishAndSameRoomRestart()
        {
            yield return StartModeLauncher();
            Pointer(EntryButton("创建合作房间"));
            yield return Wait(() => Runtime?.Config.Role == RuntimeInstanceRole.Host && Client?.Stage == ProductClientStage.Joined &&
                Entry != null && ((UIGridKitchen)Entry.Hud)?.Available == true, "Ordinary menu creates an empty room");
            ownedInit = Init.Instance;
            var match = (Match)((HostSessionComponent)Runtime.Authority).Match;
            string matchId = match.MatchId;
            StartPreviewAndConfirm(EntryButton("开启内存餐厅"));
            yield return Wait(() => Presentation?.Ready == true, "Ordinary Host begins cooperative chapter");
            Presentation.CloseGridHud();
            yield return null;
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Client managed chapter view");
                Assert.That(remote.Hud.PhaseText.text, Does.Contain("第 1/3 关"));
                remote.Presentation.CloseGridHud();
                Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation?.Ready == true, "Host takes managed view ownership");
                string world = Grid.WorldId;
                for (int stage = 1; stage <= 3; ++stage)
                {
                    Assert.That(Grid.LevelConfigId, Is.EqualTo(400 + stage));
                    Assert.That(Hud.PhaseText.text, Does.Contain($"第 {stage}/3 关"));
                    string buff = Hud.GetComponentsInChildren<Button>(false).First(button =>
                        button.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
                    yield return ChapterUiCommand(remote, buff);
                    yield return ChapterUiCommand(remote, "开始营业");
                    yield return WaitCoopUi(remote, () => Grid.LevelPhase == LevelPhase.Completed && remote.Grid.LevelPhase == LevelPhase.Completed &&
                        (stage == 3 || Grid.RenovationChoiceVersion > 0), "Cooperative front naturally drains stage " + stage, 90);
                    Assert.That(remote.Grid.LevelScore, Is.EqualTo(Grid.LevelScore));
                    Assert.That(remote.Grid.LevelStars, Is.EqualTo(Grid.LevelStars));
                    Assert.That(remote.Grid.UnfulfilledOrders, Is.EqualTo(Grid.UnfulfilledOrders));
                    Assert.That(Grid.WorldId, Is.EqualTo(world));
                    if (stage < 3)
                    {
                        long level = Grid.LevelId;
                        yield return ChapterUiCommand(remote, "下一关");
                        yield return WaitCoopUi(remote, () => remote.Grid.LevelId == Grid.LevelId && Grid.LevelId != level &&
                            Grid.BuffDrawVersion > 0, "Both projections enter next chapter level");
                    }
                }
                Assert.That(Grid.RenovationCandidates, Is.Empty);
                Assert.That(Hud.SettlementText.text, Does.Contain("大关完成").And.Contain("返回房间"));
                Assert.That(Hud.GetComponentsInChildren<Button>(false).Any(button =>
                    button.GetComponentInChildren<Text>().text == "下一关" || button.GetComponentInChildren<Text>().text.StartsWith("装修 ")), Is.False);
                Presentation.CloseGridHud();
                ((Scene)remote.Client.IScene).RemoveComponent<GridPresentationComponent>();
                ((Scene)remote.Client.IScene).AddComponent<GridPresentationComponent>();
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Client reopens terminal result through managed owner");
                Assert.That(remote.Hud.SettlementText.text, Does.Contain("大关完成").And.Contain("等待 Host").And.Contain("返回房间"));
                Assert.That(remote.Hud.GetComponentsInChildren<Button>(false).Any(button =>
                    button.GetComponentInChildren<Text>().text.Contains("返回房间")), Is.False);
                remote.Presentation.CloseGridHud();
                Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation?.Ready == true, "Host terminal confirmation reopens");
                var retired = CaptureButtonCallback(Button("完成大关并返回房间"));
                var client = Client;
                long command = client.NextCommandId;
                var finish = Button("完成大关并返回房间");
                Pointer(finish);
                Pointer(finish);
                Assert.That(client.NextCommandId, Is.EqualTo(command + 1));
                yield return WaitCoopUi(remote, () => (RestaurantRuntime)match.Restaurant == null && !remote.Grid.Confirmed &&
                    Presentation == null && Entry != null && ((UIGridKitchen)Entry.Hud)?.Available == true, "Completion returns the same room");
                Assert.That(match.MatchId, Is.EqualTo(matchId));
                Assert.That(Client.Roster.Count, Is.EqualTo(2));
                Assert.That(((ProcessInnerSender)Runtime.Sender).IsStopped, Is.False);
                StartPreviewAndConfirm(EntryButton("开启内存餐厅"));
                yield return WaitCoopUi(remote, () => Presentation?.Ready == true && remote.Grid.Confirmed, "Same members reopen a fresh chapter");
                Assert.That(Grid.WorldId, Is.Not.EqualTo(world));
                Assert.That(Grid.LevelConfigId, Is.EqualTo(401));
                command = Client.NextCommandId;
                retired();
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
                Assert.That((RestaurantRuntime)match.Restaurant, Is.Not.Null);
            }
        }
    }
}
#endif
