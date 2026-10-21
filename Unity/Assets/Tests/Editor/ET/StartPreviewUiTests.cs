#if UNITY_ET_VIEW && UNITY_EDITOR
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
        [UnityTest, Category("EntryExperience"), Category("EntryFlowClarity")]
        public IEnumerator EntryStartPreviewSoloCancelsReopensAndStartsExactChapterWithoutChangingExistingFile()
        {
            yield return StartModeLauncher();
            ProductRuntimeComponent runtime = Runtime;
            var repository = CreateSaveUiRepository(runtime, out string directory, out string profile);
            try
            {
                var header = SelectionHeader(profile);
                repository.Commit(header, SelectionFixture());
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                byte[] originalFile = File.ReadAllBytes(path);
                Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Headers.Length == 1,
                    "Real selector reads the existing file before preview");
                Pointer(SelectionButton("新开大关 2 · 进阶小馆"));
                Assert.That(Selection.StartPreviewVisible, Is.True);
                Assert.That(SelectionHud.SettlementText.text, Does.Contain("进阶小馆").And.Contain("共 3 关"));
                Pointer(SelectionButton("第 1 关 · 鲜榨橙汁"));
                Assert.That((Scene)runtime.HostScene, Is.Null);
                Assert.That((ClientSessionComponent)runtime.Client, Is.Null);
                var retired = CaptureButtonCallback(SelectionButton("确认开始"));
                Pointer(SelectionButton("菜单试玩"));
                Pointer(SelectionButton("内存试玩"));
                Assert.That(SelectionHud.SettlementText.text, Does.Contain("内存试玩：退出后不保留记录"));
                retired();
                Assert.That((Scene)runtime.HostScene, Is.Null, "Previous preview cannot start another selection");
                var closedPreview = CaptureButtonCallback(SelectionButton("确认开始"));
                Selection.CloseSoloSelection();
                Selection.OpenSoloSelection();
                yield return Wait(() => ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Headers.Length == 1,
                    "Managed pool reopen returns to choices");
                closedPreview();
                Assert.That(Selection.StartPreviewVisible, Is.False);
                Assert.That((Scene)runtime.HostScene, Is.Null);
                Pointer(SelectionButton("新开大关 2 · 进阶小馆"));
                var once = CaptureButtonCallback(SelectionButton("确认开始"));
                Pointer(SelectionButton("确认开始"));
                once();
                yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Confirmed preview starts the actual chapter");
                Assert.That(Grid.LevelConfigId, Is.EqualTo(Tables.Instance.DTRestaurantChapterConfig.Get(2).SoloFirstLevelId));
                Assert.That(Grid.SaveEnabled, Is.True);
                Assert.That(repository.Browse().Length, Is.EqualTo(1), "New start writes nothing until original successful settlement");
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(originalFile), "Existing record is unchanged");
                yield return Command("拿 碗台 ·", true);
                Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.Not.Zero);
            }
            finally
            {
                runtime.StopProduct();
                repository.Dispose();
                RemoveOwnSaveUiFiles(directory);
            }
        }

        [UnityTest, Category("EntryExperience")]
        public IEnumerator EntryStartPreviewHostAndActualKcpClientReadOnlyCancelAndRetiredConfirmation()
        {
            yield return StartProgressHostRoom();
            Match match = ((HostSessionComponent)Runtime.Authority).Match;
            long command = Client.NextCommandId;
            Pointer(EntryButton("开启大关 2 · 进阶小馆"));
            Assert.That(EntryHud.SettlementText.text, Does.Contain("共 3 关").And.Contain("本端目录"));
            Pointer(EntryButton("第 1 关 · 鲜榨橙汁"));
            var retired = CaptureButtonCallback(EntryButton("确认开始"));
            Pointer(EntryButton("返回内容列表"));
            Assert.That((RestaurantRuntime)match.Restaurant, Is.Null);
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Entry.CloseProductEntry();
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases shared UI asset");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Client.Stage == ProductClientStage.Joined &&
                    remote.Client.Root().GetComponent<ProductEntryPresentation>() is ProductEntryPresentation entry &&
                    ((UIGridKitchen)entry.Hud)?.Available == true, "Actual KCP Client room entry");
                ProductEntryPresentation remoteEntry = remote.Client.Root().GetComponent<ProductEntryPresentation>();
                MonoUIGridKitchen view = ((UIGridKitchen)remoteEntry.Hud).View;
                long remoteCommand = remote.Client.NextCommandId;
                Pointer(ClaimUiButton(view, "选择章节"));
                Pointer(ClaimUiButton(view, "查看大关 2 · 进阶小馆"));
                Assert.That(view.SettlementText.text, Does.Contain("进阶小馆").And.Contain("本端目录"));
                Assert.That(view.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "确认开始"), Is.False);
                Assert.That(remote.Client.GetComponent<ProductRoomClient>().Current.CanControlRoom, Is.False);
                Pointer(ClaimUiButton(view, "返回内容列表"));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(remoteCommand));
                Assert.That((RestaurantRuntime)match.Restaurant, Is.Null);
                remoteEntry.CloseProductEntry();
                remote.KeepMapHeadlessWhileHostOwnsDisplay();
                yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Client releases shared UI asset");
                Entry.OpenProductEntry();
                yield return WaitCoopUi(remote, () => ((UIGridKitchen)Entry.Hud)?.Available == true, "Host room reopens");
                retired();
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
                Pointer(EntryButton("开启大关 2 · 进阶小馆"));
                var confirmed = CaptureButtonCallback(EntryButton("确认开始"));
                Pointer(EntryButton("确认开始"));
                confirmed();
                Assert.That(Client.NextCommandId, Is.EqualTo(command + 1), "One confirmation sends exactly one real room request");
                yield return WaitCoopUi(remote, () => Presentation?.Ready == true && remote.Grid.Confirmed, "Both clients observe the single confirmed chapter");
                Assert.That(Grid.LevelConfigId, Is.EqualTo(Tables.Instance.DTRestaurantChapterConfig.Get(2).CooperativeFirstLevelId));
                Assert.That(remote.Grid.LevelConfigId, Is.EqualTo(Grid.LevelConfigId));
                yield return ReturnToRoomThroughConfirmation();
                yield return WaitCoopUi(remote, () => (RestaurantRuntime)match.Restaurant == null && ((UIGridKitchen)Entry.Hud)?.Available == true,
                    "Actual room retirement returns to entry");
                command = Client.NextCommandId;
                confirmed();
                Assert.That(Client.NextCommandId, Is.EqualTo(command), "Old restaurant confirmation cannot open the next world");
            }
        }
    }
}
#endif
