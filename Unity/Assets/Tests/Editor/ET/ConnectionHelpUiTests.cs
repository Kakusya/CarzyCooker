#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using ET.Client;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest, Category("EntryExperience")]
        public IEnumerator EntryConnectionHelpCopiesCurrentHostEndpointAndRetiredCopyCannotOverwriteClipboard()
        {
            string previous = GUIUtility.systemCopyBuffer;
            try
            {
                yield return StartProgressHostRoom();
                string endpoint = Runtime.ListenEndpoint;
                long command = Client.NextCommandId;
                Pointer(EntryButton("复制加入地址"));
                Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo(endpoint));
                var retired = CaptureButtonCallback(EntryButton("复制加入地址"));
                Pointer(EntryButton("连接帮助"));
                Assert.That(EntryHud.SettlementText.text, Does.Contain("只指").And.Contain("显式LAN").And.Contain("不能判断防火墙"));
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
                Pointer(EntryButton("返回房间首页"));
                GUIUtility.systemCopyBuffer = "clipboard-owned-by-test";
                retired();
                Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo("clipboard-owned-by-test"));
                Entry.CloseProductEntry();
                Entry.OpenProductEntry();
                yield return Wait(() => ((UIGridKitchen)Entry.Hud)?.Available == true, "Copy reopens through original HUD owner");
                Pointer(EntryButton("复制加入地址"));
                string copied = GUIUtility.systemCopyBuffer;
                Entry.CloseProductEntry();
                yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases shared UI");
                using (var remote = new CoopUiRemote(copied))
                {
                    ownedCoopUiRemotes.Add(remote);
                    yield return WaitCoopUi(remote, () => remote.Client.Stage == ProductClientStage.Joined,
                        "Copied actual endpoint joins real KCP room");
                    Assert.That(Client.Roster.Count, Is.EqualTo(2));
                    Match match = ((HostSessionComponent)Runtime.Authority).Match;
                    Assert.That((RestaurantRuntime)match.Restaurant, Is.Null);
                    Assert.That(Client.NextCommandId, Is.EqualTo(command), "Copy and help do not send gameplay commands");
                }
            }
            finally
            {
                GUIUtility.systemCopyBuffer = previous;
            }
        }

        [UnityTest, Category("EntryExperience")]
        public IEnumerator EntryConnectionHelpPasteRetainsInvalidInputAndRetriesThroughOrdinaryKcpClient()
        {
            string previous = GUIUtility.systemCopyBuffer;
            try
            {
                yield return StartModeLauncher();
                ProductRuntimeComponent original = Runtime;
                Pointer(EntryButton("合作房间"));
                Assert.That(EntryHud.SettlementText.text, Does.Contain("127.0.0.1").And.Contain("另一台电脑"));
                GUIUtility.systemCopyBuffer = "not-an-ip:0";
                Pointer(EntryButton("粘贴地址"));
                Assert.That(EntryHud.EntryAddress, Is.EqualTo("not-an-ip:0"));
                Pointer(EntryButton("连接帮助"));
                Pointer(EntryButton("返回合作入口"));
                Assert.That(EntryHud.EntryAddress, Is.EqualTo("not-an-ip:0"));
                var retiredJoin = CaptureButtonCallback(EntryButton("加入合作房间"));
                Pointer(EntryButton("加入合作房间"));
                Assert.That(Runtime, Is.SameAs(original));
                Assert.That(EntryHud.EntryAddress, Is.EqualTo("not-an-ip:0"));
                Assert.That(EntryHud.StatusText.text, Does.Contain("校验"));
                Assert.That((ClientSessionComponent)Runtime.Client, Is.Null);
                using (var server = new EntryUiHost("127.0.0.1:0"))
                {
                    GUIUtility.systemCopyBuffer = server.Runtime.ListenEndpoint;
                    Pointer(EntryButton("粘贴地址"));
                    string endpoint = EntryHud.EntryAddress;
                    Pointer(EntryButton("加入合作房间"));
                    yield return WaitEntryHost(server, () => Runtime != original && Client?.Stage == ProductClientStage.Joined &&
                        ((UIGridKitchen)Entry.Hud)?.Available == true && server.Client.Stage == ProductClientStage.Joined,
                        "Ordinary pasted address retries using a fresh actual KCP Client");
                    ownedInit = Init.Instance;
                    ProductRuntimeComponent joined = Runtime;
                    retiredJoin();
                    Assert.That(Runtime, Is.SameAs(joined));
                    Assert.That(Runtime.Config.Address + ":" + Runtime.Config.Port, Is.EqualTo(endpoint));
                    Assert.That((Scene)Runtime.HostScene, Is.Null);
                    Assert.That(Client.Roster.Count, Is.EqualTo(2));
                    Assert.That(Client.NextCommandId, Is.Zero);
                    Assert.That(Client.GetComponent<ProductRoomClient>().Current.HasRestaurant, Is.False);
                }
            }
            finally
            {
                GUIUtility.systemCopyBuffer = previous;
            }
        }
    }
}
#endif
