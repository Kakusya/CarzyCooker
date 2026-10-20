#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Linq;
using ET.Client;
using Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest, Category("ContextualActions")]
        public IEnumerator ContextualActionsOriginalCandidatesCompactSummaryAndWrongDirectionPreserveItems()
        {
            yield return StartLauncher();
            yield return Wait(() => Grid.BuffDrawVersion > 0, "Stable original preparation candidates");
            Assert.That(Hud.MoreActionsVisible, Is.False);
            long command = Client.NextCommandId;
            var candidates = Hud.TargetsRectTransform.GetComponentsInChildren<Button>(true).Select(button => button.GetInstanceID()).OrderBy(id => id).ToArray();
            yield return ReachAndClick(Button("更多动作"));
            Assert.That(Hud.TargetsRectTransform.GetComponentsInChildren<Button>(false).Select(button => button.GetInstanceID()).OrderBy(id => id), Is.EqualTo(candidates));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return ReadableCommand("拿 碗台 ·", true);
            Assert.That(Hud.ContextActionCount, Is.InRange(1, 3));
            Assert.That(Button("放 碗台 ·", true).GetComponentInChildren<Text>().text, Does.Contain("碗").And.Contain("碗台"));
            yield return ReachAndClick(Button("收起更多动作"));
            yield return ReadableCommand("放 碗台 ·", true);
            yield return ReadableCommand("拿 面包台 ·", true);
            yield return ReadableCommand("放 烤箱 ·", true);
            yield return ReadableCommand("烘烤 烤箱 ·", true);
            Assert.That(Hud.TargetText.text, Does.Contain("物品正在加工"), "Locked work explains why the current item cannot be taken");
            yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.Toast), "Actual toast finishes");
            yield return ReadableCommand("拿 盘台 ·", true);
            long plate = Grid.Actors[Client.MemberId].HandItemId;
            long toast = Grid.Stations.Values.Single(station => station.Name == "烤箱").ItemId;
            int items = Grid.Items.Count;
            command = Client.NextCommandId + 1;
            yield return ReachAndClick(Button("添加 烤箱 ·", true));
            yield return Wait(() => Grid.LastCommandId == command && Grid.LastStatus == GridResultStatus.Rejected, "Original direction is rejected without an automatic swap");
            Assert.That(Hud.TargetText.text, Does.Contain("先放下盘"));
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(plate));
            Assert.That(Grid.Stations.Values.Single(station => station.Name == "烤箱").ItemId, Is.EqualTo(toast));
            Assert.That(Grid.Items.Count, Is.EqualTo(items));
            Assert.That(Hud.PrimaryActionsRectTransform.GetComponentsInChildren<Button>(false).Any(button => button.name.StartsWith("添加 烤箱")), Is.False);
            yield return ReachAndClick(Button("查看详情"));
            Assert.That(Hud.DetailsText.text, Does.Contain("把盘放到空台面").And.Contain("不会自动交换"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            var retired = CaptureButtonCallback(Button("收起详情"));
            Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Current contextual HUD reopens");
            retired();
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Vector2 point = Board.InputScreenRect.center;
            Presentation.ProcessGridPointer(point, true, true, false);
            Presentation.ProcessGridPointer(point + new Vector2(0, 100), false, true, false);
            Presentation.ProcessGridPointer(point + new Vector2(0, 100), false, false, true);
            Assert.That(Client.NextCommandId, Is.EqualTo(command), "Dragging/detail input never retries the rejected action");
        }

        [UnityTest, Category("ContextualActions"), Category("ProductFeedbackBoundary")]
        public IEnumerator ContextualActionsRealKcpOldCandidateCannotTakeReturnedSameInstance()
        {
            yield return StartCoopHostLauncher();
            var hostClient = Client;
            var hostGrid = Grid;
            Presentation.GetAsyncOwner().AsyncScope.Stop();
            Presentation.CloseGridHud();
            ((GridKitchenBoard)Presentation.Board).Visible = false;
            var sound = Game.GameEntry.Sound.GetSoundGroup(ProductAudioSettings.Group);
            bool muted = sound.Mute; float volume = sound.Volume;
            try
            {
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "KCP projection and managed HUD");
                var feedback = ((Scene)remote.Client.IScene).GetComponent<ProductFeedbackComponent>();
                Assert.That(feedback.Requested.Sum(), Is.Zero, "Joining establishes a silent current-state baseline");
                sound.Mute = false; sound.Volume = .6f;
                Button Remote(string label, bool prefix = false) => ClaimUiButton(remote.Hud, label, prefix);
                yield return ContentUiPump(ReachAndClick(Remote("工位说明")), remote);
                var select = remote.Hud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text.StartsWith("工位 ") && button.GetComponentInChildren<Text>().text.EndsWith("· 碗台"));
                yield return ContentUiPump(ReachAndClick(select), remote);
                var station = remote.Grid.Stations.Values.Single(value => value.Name == "碗台");
                long bowl = station.ItemId;
                var oldTake = CaptureButtonCallback(Remote("拿 碗台 ·", true));
                long remoteCommand = remote.Client.NextCommandId;
                bool finished = false;
                string error = null;
                var request = hostGrid.CreateGridCommand(GridAction.TakeCounter, station.Cell, station.Id, bowl);
                Runtime.GetAsyncOwner().Run(async context =>
                {
                    error = await hostClient.CallProductValueAsync(context, request, response => response.Error == 0 ? null : response.Message);
                    finished = true;
                });
                yield return WaitCoopUi(remote, () => finished && remote.Grid.Stations[station.Id].ItemId == 0, "Another real member takes the selected object");
                Assert.That(error, Is.Null);
                oldTake();
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(remoteCommand));
                finished = false;
                var place = hostGrid.CreateGridCommand(GridAction.PlaceCounter, station.Cell, station.Id, bowl);
                Runtime.GetAsyncOwner().Run(async context =>
                {
                    error = await hostClient.CallProductValueAsync(context, place, response => response.Error == 0 ? null : response.Message);
                    finished = true;
                });
                yield return WaitCoopUi(remote, () => finished && remote.Grid.Stations[station.Id].ItemId == bowl, "Same bowl returns through the original local mailbox");
                oldTake();
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(remoteCommand), "Retired candidate does not revive even for the same item instance");
                finished = false;
                var move = hostGrid.CreateGridCommand(GridAction.Move, new GridCell(3, 6));
                Runtime.GetAsyncOwner().Run(async context =>
                {
                    error = await hostClient.CallProductValueAsync(context, move, response => response.Error == 0 ? null : response.Message);
                    finished = true;
                });
                yield return WaitCoopUi(remote, () => finished && hostGrid.Actors[hostClient.MemberId].Cell.Equals(new GridCell(3, 6)), "The other member leaves the shared approach through its original mailbox");
                yield return ContentUiPump(ReachAndClick(Remote("拿 碗台 ·", true)), remote);
                yield return WaitCoopUi(remote, () => remote.Grid.Actors[remote.Client.MemberId].HandItemId == bowl, "Fresh remote click takes only the current bowl");
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(remoteCommand + 1));
                Assert.That(hostGrid.CanControlRestaurant, Is.True);
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
                yield return WaitCoopUi(remote, () => feedback.Requested[(int)ProductFeedbackKind.Pickup] == 1, "The remote player's actual terminal result emits one pickup cue");
                remote.Presentation.CloseGridHud(); yield return null; remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready, "KCP HUD reopens without replaying feedback");
                Assert.That(feedback.Requested[(int)ProductFeedbackKind.Pickup], Is.EqualTo(1));
                feedback.EmitFeedback(ProductFeedbackKind.LevelComplete);
                yield return WaitCoopUi(remote, () => feedback.Playing.Any(playback => playback.Kind == ProductFeedbackKind.LevelComplete && playback.Agent?.IsPlaying == true), "Owned cue is actually playing before pause");
                var pause = hostGrid.CreateLevelCommand(LevelControlAction.Pause);
                Runtime.GetAsyncOwner().Run(async context =>
                {
                    await hostClient.CallProductValueAsync(context, pause, response => response.Error);
                });
                yield return WaitCoopUi(remote, () => remote.Grid.LevelPaused && feedback.Playing.Count == 0, "Committed Host pause stops the remote feedback owner");
                int requests = feedback.Requested.Sum();
                var resume = hostGrid.CreateLevelCommand(LevelControlAction.Resume);
                Runtime.GetAsyncOwner().Run(async context =>
                {
                    await hostClient.CallProductValueAsync(context, resume, response => response.Error);
                });
                yield return WaitCoopUi(remote, () => !remote.Grid.LevelPaused, "Host resumes the same world");
                Assert.That(feedback.Requested.Sum(), Is.EqualTo(requests), "Resuming never queues historical audio");
            }
            using (var returned = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                yield return WaitCoopUi(returned, () => returned.Presentation?.Ready == true, "New real KCP binding joins the existing world");
                var feedback = ((Scene)returned.Client.IScene).GetComponent<ProductFeedbackComponent>();
                Assert.That(feedback.Requested.Sum(), Is.Zero, "The new binding never replays the previous member's pickup or pause cue");
            }
            }
            finally
            {
                sound.Mute = muted; sound.Volume = volume;
            }
        }
    }
}
#endif
