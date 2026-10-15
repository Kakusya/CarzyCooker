#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Linq;
using ET.Client;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest]
        [Category("ContainerExtraction")]
        [Category("ContainerExtractionFinal")]
        public IEnumerator ContainerExtractionUiRealLauncherReopenAndOldCallbackKeepOneRequest()
        {
            yield return StartLauncher();
            long egg = Grid.Stations.Values.Single(station => station.Name == "食材台").ItemId;
            long bowl = Grid.Stations.Values.Single(station => station.Name == "碗台").ItemId;
            yield return Command("拿 食材台 ·", true);
            yield return Command("添加 碗台 ·", true);
            Pointer(Button("查看详情"));
            yield return Wait(() => Hud.GetComponentsInChildren<UnityEngine.UI.Button>(false).Any(button =>
                button.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith("取出 鸡蛋 ·")), "Actual extraction detail action");
            var old = CaptureButtonCallback(Button("取出 鸡蛋 · 碗台"));
            Presentation.CloseGridHud();
            yield return Wait(() => (UIGridKitchen)Presentation.Hud == null, "Old managed HUD retired");
            Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Managed HUD reopened");
            long command = Client.NextCommandId;
            old();
            Assert.That(Client.NextCommandId, Is.EqualTo(command), "Old UI callback cannot submit to reopened HUD");
            if (!Hud.GetComponentsInChildren<UnityEngine.UI.Button>(false).Any(button =>
                button.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith("取出 鸡蛋 ·")))
            {
                Pointer(Button("查看详情"));
            }
            var oldLevelAction = CaptureButtonCallback(Button("取出 鸡蛋 · 碗台"));
            yield return Command("取出 鸡蛋 · 碗台");
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(egg));
            Assert.That(Grid.Items[bowl].Contents, Is.Empty);
            yield return Command("放 食材台 ·", true);
            yield return Command("拿 碗台 ·", true);
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(bowl), "Default click still takes the outer bowl");
            string buff = Hud.GetComponentsInChildren<UnityEngine.UI.Button>(false).First(button =>
                button.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith("选择 ")).GetComponentInChildren<UnityEngine.UI.Text>().text;
            yield return Command(buff);
            yield return Command("开始营业");
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationChoiceVersion > 0,
                "Natural completion for old-level callback check", 75);
            long oldLevel = Grid.LevelId;
            yield return Command("下一关");
            Assert.That(Grid.LevelId, Is.Not.EqualTo(oldLevel));
            command = Client.NextCommandId;
            oldLevelAction();
            Assert.That(Client.NextCommandId, Is.EqualTo(command), "Old-level extraction callback cannot send into new Level");
        }

        [UnityTest]
        [Category("ContainerExtraction")]
        [Category("ContainerExtractionFinal")]
        public IEnumerator ContainerExtractionUiOrdinaryHostAndRealRemoteDetailsKeepOriginalItem()
        {
            yield return StartCoopHostLauncher();
            var hostGrid = Grid;
            long egg = Grid.Stations.Values.Single(station => station.Name == "食材台").ItemId;
            long bowl = Grid.Stations.Values.Single(station => station.Name == "碗台").ItemId;
            yield return Command("拿 食材台 ·", true);
            yield return Command("添加 碗台 ·", true);
            ((Scene)Client.IScene).RemoveComponent<ET.Client.GridPresentationComponent>();
            yield return null;
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Remote managed HUD ready");
                UnityEngine.UI.Button RemoteButton(string caption) => remote.Hud.GetComponentsInChildren<UnityEngine.UI.Button>(false)
                    .Single(button => button.GetComponentInChildren<UnityEngine.UI.Text>().text == caption);
                long before = remote.Client.NextCommandId;
                Assert.That(remote.Grid.Items[bowl].Contents, Is.EqualTo(new[] { egg }), "Remote snapshot has the original egg in the bowl");
                Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.Zero);
                yield return WaitCoopUi(remote, () => RemoteButton("查看详情").IsInteractable(), "Remote HUD opening transition permits real pointer input");
                Assert.That(remote.Presentation.SelectedLevelId, Is.EqualTo(remote.Grid.LevelId), "HUD remembers the current Level before details input");
                Assert.That(remote.Presentation.SelectedLevelGeneration, Is.EqualTo(remote.Grid.LevelGeneration));
                Assert.That(RemoteButton("查看详情").isActiveAndEnabled, Is.True);
                Pointer(RemoteButton("查看详情"));
                Assert.That(remote.Presentation.DetailsVisible, Is.True, "Remote details toggles in the actual pointer callback");
                yield return null;
                Assert.That(remote.Presentation.DetailsVisible, Is.True, "Actual remote details callback opens the panel");
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(before), "Opening details does not submit a command");
                Assert.That(remote.Hud.GetComponentsInChildren<UnityEngine.UI.Button>(false).Select(button =>
                    button.GetComponentInChildren<UnityEngine.UI.Text>().text), Has.Member("取出 鸡蛋 · 碗台"));
                Pointer(RemoteButton("取出 鸡蛋 · 碗台"));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(before + 1));
                yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == before + 1 &&
                    remote.Grid.LastStatus == GridResultStatus.Executed, "Real KCP extraction reaches authoritative commit");
                Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.EqualTo(egg));
                Assert.That(remote.Grid.Items[bowl].Contents, Is.Empty);
                Assert.That(hostGrid.Items[egg].HolderId, Is.EqualTo(remote.Client.MemberId));
                Assert.That(hostGrid.Items[bowl].Contents, Is.Empty);
            }
        }
    }
}
#endif
