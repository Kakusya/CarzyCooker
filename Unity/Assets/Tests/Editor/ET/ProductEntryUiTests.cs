#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using ET.Client;
using Game;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        private ProductEntryPresentation Entry => Runtime?.Root().GetComponent<ProductEntryPresentation>();
        private MonoUIGridKitchen EntryHud => ((UIGridKitchen)Entry.Hud).View;
        private Button EntryButton(string caption)
        {
            Button found = EntryHud.GetComponentsInChildren<Button>(false)
                .SingleOrDefault(button => button.GetComponentInChildren<Text>().text == caption);
            if (found != null)
            {
                return found;
            }
            string parent = caption is "创建合作房间" or "加入合作房间" ? "合作房间"
                : caption.StartsWith("开启大关 ") ? "选择章节"
                : caption.StartsWith("开启") ? "菜单试玩" : null;
            Assert.That(parent, Is.Not.Null, "Missing actual entry: " + caption);
            Pointer(EntryHud.GetComponentsInChildren<Button>(false)
                .Single(button => button.GetComponentInChildren<Text>().text == parent));
            return EntryHud.GetComponentsInChildren<Button>(false)
                .Single(button => button.GetComponentInChildren<Text>().text == caption);
        }

        private IEnumerator StartModeLauncher()
        {
            Assert.That(Init.Instance, Is.Null);
            Assert.That(FiberManager.Instance, Is.Null);
            RuntimeInstanceContext.Reset();
            Application.runInBackground = true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Launcher.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return Wait(() => Game.GameEntry.RuntimeOwner is Component, "Ordinary game owner starts");
            ownedRoot = ((Component)Game.GameEntry.RuntimeOwner).transform.root.gameObject;
            yield return Wait(() => Entry != null && ((UIGridKitchen)Entry.Hud)?.Available == true, "Ordinary mode selector available");
            ownedInit = Init.Instance;
        }

        [UnityTest]
        [Category("ProductEntry")]
        [Category("PlayerLanUi")]
        public IEnumerator ProductEntryUiOrdinaryModesRejectInvalidAddressCancelJoinAndStartSoloWithoutListener()
        {
            bool originalBackground = Application.runInBackground;
            yield return StartModeLauncher();
            ProductRuntimeComponent initial = Runtime;
            Assert.That(initial.HostScene, Is.EqualTo(default(EntityRef<Scene>)));
            EntryHud.ShowEntryAddress("192.0.2.1:12345");
            Pointer(EntryButton("加入合作房间"));
            Assert.That(Runtime, Is.SameAs(initial));
            Assert.That(Entry.Failure, Does.Contain("loopback"));
            using (var reserved = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                reserved.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                string endpoint = reserved.LocalEndPoint.ToString();
                EntryHud.ShowEntryAddress(endpoint);
                var retired = CaptureButtonCallback(EntryButton("创建合作房间"));
                Pointer(EntryButton("加入合作房间"));
                yield return Wait(() => Runtime != null && Runtime.Config.Role == RuntimeInstanceRole.Client && Entry != null &&
                    ((UIGridKitchen)Entry.Hud)?.Available == true, "Real Client initialization offers cancel while joining");
                ownedInit = Init.Instance;
                Assert.That(Application.runInBackground, Is.True, "The Client keeps its message pump while another Player has focus.");
                Assert.That((Scene)Runtime.HostScene, Is.Null);
                Pointer(EntryButton("取消加入"));
                yield return Wait(() => Runtime != null && Runtime.Config.ShowModeMenu && Entry != null &&
                    ((UIGridKitchen)Entry.Hud)?.Available == true, "Cancel returns through a new initialization");
                ownedInit = Init.Instance;
                ProductRuntimeComponent current = Runtime;
                Assert.That(Application.runInBackground, Is.EqualTo(originalBackground), "The retired network run restores its previous setting.");
                retired();
                yield return null;
                Assert.That(Runtime, Is.SameAs(current), "Retired mode callback cannot switch the next run");
            }
            Pointer(EntryButton("单机餐厅"));
            yield return Wait(() => Runtime.Root().GetComponent<SoloSavePresentation>() is SoloSavePresentation selector &&
                ((UIGridKitchen)selector.Hud)?.Available == true, "Solo selector remains managed");
            var selection = Runtime.Root().GetComponent<SoloSavePresentation>();
            var view = ((UIGridKitchen)selection.Hud).View;
            StartPreviewAndConfirm(view.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text == "内存试玩"));
            yield return Wait(() => Presentation?.Ready == true, "Chosen Solo enters a real map");
            Assert.That(Runtime.Config.Role, Is.EqualTo(RuntimeInstanceRole.Solo));
            Assert.That(((Scene)Runtime.HostScene).GetComponent<NetComponent>(), Is.Null);
            yield return Command("拿 食材台 ·", true);
            yield return Command("添加 碗台 ·", true);
            Pointer(Button("查看详情"));
            yield return Command("取出 鸡蛋 · 碗台");
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.Not.Zero);
        }

        [UnityTest]
        [Category("ProductEntry")]
        [Category("PlayerLanUi")]
        public IEnumerator ProductEntryUiOrdinaryHostRoomRealKcpReopenAndRetiredWorldCallbacks()
        {
            bool previousBackground = Application.runInBackground;
            try
            {
                yield return StartModeLauncher();
                Application.runInBackground = false;
                Pointer(EntryButton("创建合作房间"));
                yield return Wait(() => Runtime != null && Runtime.Config.Role == RuntimeInstanceRole.Host && Client?.Stage == ProductClientStage.Joined &&
                    Entry != null && ((UIGridKitchen)Entry.Hud)?.Available == true &&
                    Client.GetComponent<ProductRoomClient>().Current.Version > 0, "Ordinary Host room confirms without kitchen");
                ownedInit = Init.Instance;
                HostSessionComponent authority = Runtime.Authority;
                Assert.That(Application.runInBackground, Is.True, "The Host keeps its message pump while the Client Player has focus.");
                Match match = authority.Match;
                string matchId = match.MatchId;
                Assert.That((RestaurantRuntime)match.Restaurant, Is.Null);
                Assert.That(Presentation, Is.Null);
                Assert.That(EntryHud.HintText.text, Does.Contain(Runtime.ListenEndpoint));
                Entry.CloseProductEntry();
                using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
                {
                    ownedCoopUiRemotes.Add(remote);
                    yield return WaitCoopUi(remote, () => remote.Client.Stage == ProductClientStage.Joined &&
                        remote.Client.Root().GetComponent<ProductEntryPresentation>() is ProductEntryPresentation entry &&
                        ((UIGridKitchen)entry.Hud)?.Available == true, "KCP Client displays the room, without a board");
                    var remoteEntry = remote.Client.Root().GetComponent<ProductEntryPresentation>();
                    var remoteView = ((UIGridKitchen)remoteEntry.Hud).View;
                    Assert.That(remote.Presentation, Is.Null);
                    Assert.That(remote.Grid.Confirmed, Is.False);
                    Assert.That(remoteView.SettlementText.text, Does.Contain("玩家"));
                    Assert.That(remoteView.GetComponentsInChildren<Button>(false).Any(button =>
                        button.GetComponentInChildren<Text>().text == "开启内存餐厅"), Is.False);
                    remoteEntry.CloseProductEntry();
                    remote.KeepMapHeadlessWhileHostOwnsDisplay();
                    yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"),
                        "The shared Editor UI asset is released before Host display ownership");
                    Entry.OpenProductEntry();
                    yield return WaitCoopUi(remote, () => ((UIGridKitchen)Entry.Hud)?.Available == true, "Host room controls reopen through owner");
                    long command = Client.NextCommandId;
                    StartPreviewAndConfirm(EntryButton("开启内存餐厅"));
                    Assert.That(Client.NextCommandId, Is.EqualTo(command + 1));
                    yield return WaitCoopUi(remote, () => Presentation?.Ready == true && remote.Grid.Confirmed,
                        "Host opens one kitchen and both identities confirm a world");
                    string world = Grid.WorldId;
                    var retired = CaptureButtonCallback(Button("返回房间"));
                    yield return Command("拿 碗台 ·", true);
                    command = Client.NextCommandId;
                    yield return ReturnToRoomThroughConfirmation();
                    Assert.That(Client.NextCommandId, Is.EqualTo(command + 1));
                    yield return WaitCoopUi(remote, () => (RestaurantRuntime)match.Restaurant == null && Presentation == null &&
                        Entry != null && ((UIGridKitchen)Entry.Hud)?.Available == true && !remote.Grid.Confirmed,
                        "Room retirement removes both old maps");
                    Assert.That(match.MatchId, Is.EqualTo(matchId));
                    Assert.That(((ProcessInnerSender)Runtime.Sender).IsStopped, Is.False);
                    Assert.That(Client.Roster.Count, Is.EqualTo(2));
                    command = Client.NextCommandId;
                    retired();
                    Assert.That(Client.NextCommandId, Is.EqualTo(command));
                    Entry.CloseProductEntry();
                    remote.KeepMapHeadlessWhileHostOwnsDisplay();
                    yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"),
                        "The next map retains a single managed Editor display owner");
                    Entry.OpenProductEntry();
                    yield return WaitCoopUi(remote, () => ((UIGridKitchen)Entry.Hud)?.Available == true, "Host controls reopen before the next world");
                    StartPreviewAndConfirm(EntryButton("开启内存餐厅"));
                    yield return WaitCoopUi(remote, () => Presentation?.Ready == true && remote.Grid.Confirmed,
                        "Same room opens an independent new world");
                    Assert.That(Grid.WorldId, Is.Not.EqualTo(world));
                    Assert.That(remote.Grid.WorldId, Is.EqualTo(Grid.WorldId));
                    Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.Zero);
                    command = Client.NextCommandId;
                    retired();
                    Assert.That(Client.NextCommandId, Is.EqualTo(command));
                }
            }
            finally
            {
                ownedInit?.Shutdown();
                Application.runInBackground = previousBackground;
            }
        }
    }
}
#endif
