#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using ET.Client;
using NUnit.Framework;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest, Category("RoomReadinessUi"), Timeout(180000)]
        public IEnumerator EntryRoomReadinessActualKcpUiTwoPreparedRoundsRetireAndOldWorldCallbacks()
            => ProductEntryUiOrdinaryHostRoomRealKcpReopenAndRetiredWorldCallbacks();

        [UnityTest, Category("RoomReadinessUi"), Timeout(120000)]
        public IEnumerator EntryRoomReadinessActualHostUiPublishPrepareCancelAndPooledCallbacksDoNotStart()
        {
            yield return StartModeLauncher();
            Pointer(EntryButton("创建合作房间"));
            yield return Wait(() => Client?.Stage == ProductClientStage.Joined && Entry != null && ((UIGridKitchen)Entry.Hud)?.Available == true &&
                Client.GetComponent<ProductRoomClient>().Current.Version > 0, "Ordinary Host empty room before publication");
            ownedInit = Init.Instance;
            Assert.That(Presentation, Is.Null);
            Pointer(EntryButton("开启内存餐厅"));
            Pointer(EntryButton("发布房间提案"));
            yield return Wait(() => !Entry.Busy && Client.GetComponent<ProductRoomClient>().Current.HasProposal, "Publication creates shared content, no kitchen");
            Assert.That(Presentation, Is.Null);
            Assert.That(EntryHud.SettlementText.text, Does.Contain("未准备").And.Contain("所有当前连接成员"));
            long command = Client.NextCommandId;
            Pointer(EntryButton("开局（等待全员准备）"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(EntryHud.StatusText.text, Does.Contain("不能强开"));
            Pointer(EntryButton("准备本提案"));
            yield return Wait(() => !Entry.Busy && UiPeerReady(Client), "Actual Host Ready button");
            Assert.That(EntryHud.SettlementText.text, Does.Contain("已准备"));
            Assert.That(Presentation, Is.Null, "Readiness itself cannot create a restaurant");
            Pointer(EntryButton("取消准备"));
            yield return Wait(() => !Entry.Busy && !UiPeerReady(Client), "Actual cancel readiness");
            var oldReady = CaptureButtonCallback(EntryButton("准备本提案"));
            Entry.CloseProductEntry(); yield return null; Entry.OpenProductEntry();
            yield return Wait(() => ((UIGridKitchen)Entry.Hud)?.Available == true, "Pooled room UI reopens");
            command = Client.NextCommandId;
            oldReady();
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(UiPeerReady(Client), Is.False);
            Assert.That(Presentation, Is.Null);
        }

        private IEnumerator WaitRoomUi(Func<bool> condition, string reason)
        {
            yield return Wait(() =>
            {
                foreach (var remote in ownedCoopUiRemotes)
                {
                    remote.Tick();
                }
                return condition();
            }, reason);
        }

        private static bool UiPeerReady(ClientSessionComponent client) => client.GetComponent<ProductRoomClient>().Current.ReadyMembers
            .Any(member => member.MemberId == client.MemberId && member.Binding == client.BindingGeneration && member.Ready);

        private IEnumerator PreparePublishedRoomUi()
        {
            foreach (var remote in ownedCoopUiRemotes)
            {
                remote.Client?.Root().GetComponent<ProductEntryPresentation>()?.CloseProductEntry();
            }
            Entry?.OpenProductEntry();
            yield return WaitRoomUi(() => Entry != null && ((UIGridKitchen)Entry.Hud)?.Available == true &&
                Client.GetComponent<ProductRoomClient>().Current.HasProposal && !Entry.Busy, "Host displays published proposal before explicit preparation");
            var remotes = ownedCoopUiRemotes.Where(remote => remote.Client?.Stage == ProductClientStage.Joined && remote.Client.MatchId == Client.MatchId).ToArray();
            foreach (var remote in remotes)
            {
                Entry.CloseProductEntry();
                var remoteEntry = remote.Client.Root().GetComponent<ProductEntryPresentation>();
                foreach (var other in remotes)
                {
                    other.Client.Root().GetComponent<ProductEntryPresentation>()?.CloseProductEntry();
                }
                yield return WaitRoomUi(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Release original room display before peer preparation");
                remoteEntry.OpenProductEntry();
                yield return WaitRoomUi(() => ((UIGridKitchen)remoteEntry.Hud)?.Available == true && remote.Client.GetComponent<ProductRoomClient>().Current.HasProposal && !remoteEntry.Busy,
                    "KCP peer sees same proposal in its own managed UI");
                var view = ((UIGridKitchen)remoteEntry.Hud).View;
                if (!UiPeerReady(remote.Client))
                {
                    Pointer(view.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text == "准备本提案"));
                    yield return WaitRoomUi(() => !remoteEntry.Busy && UiPeerReady(remote.Client), "KCP peer explicitly confirms Ready through real button");
                }
                remoteEntry.CloseProductEntry();
            }
            Entry.OpenProductEntry();
            yield return WaitRoomUi(() => ((UIGridKitchen)Entry.Hud)?.Available == true && !Entry.Busy, "Host room UI returns after peer preparation");
            if (!UiPeerReady(Client))
            {
                Pointer(EntryButton("准备本提案"));
                yield return WaitRoomUi(() => !Entry.Busy && UiPeerReady(Client), "Host explicitly prepares too");
            }
            yield return WaitRoomUi(() => Client.GetComponent<ProductRoomClient>().Current.AllReady, "Shared all-ready fact enables Host start");
            foreach (var remote in remotes)
            {
                remote.KeepMapHeadlessWhileHostOwnsDisplay();
            }
            Pointer(EntryButton("开始本轮餐厅"));
            yield return WaitRoomUi(() => Presentation?.Ready == true && Grid?.Confirmed == true, "Only explicit all-ready Host start creates and displays the kitchen");
        }
    }
}
#endif
