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
        [Category("EntryExperience")]
        [Category("ReadableHud")]
        public IEnumerator EntryNavigationReturnsFromProgressAndRejectsRetiredPageButtons()
        {
            yield return StartModeLauncher();
            ProductRuntimeComponent runtime = Runtime;
            var repository = CreateSaveUiRepository(runtime, out string directory, out _);
            try
            {
                Assert.That(Entry.Page, Is.EqualTo(ProductEntryPage.Home));
                Assert.That(EntryHud.ClockText.text, Does.Contain("主页"));
                Assert.That(EntryHud.EntryAddressVisible, Is.False);
                var oldSolo = CaptureButtonCallback(EntryButton("单机餐厅"));
                Pointer(EntryButton("合作房间"));
                Assert.That(Entry.Page, Is.EqualTo(ProductEntryPage.Cooperation));
                Assert.That(EntryHud.ClockText.text, Does.Contain("合作房间"));
                Assert.That((Scene)runtime.HostScene, Is.Null, "Browsing must not create a host or listener");
                Assert.That((ClientSessionComponent)runtime.Client, Is.Null);
                oldSolo();
                Assert.That(runtime.Root().GetComponent<SoloSavePresentation>(), Is.Null, "Old home button cannot change the next page");
                var oldCreate = CaptureButtonCallback(EntryButton("创建合作房间"));
                Pointer(EntryButton("返回开始页"));
                oldCreate();
                Assert.That(Runtime, Is.SameAs(runtime));
                Assert.That(Entry.Page, Is.EqualTo(ProductEntryPage.Home));
                Assert.That((Scene)runtime.HostScene, Is.Null);

                Pointer(EntryButton("合作进度领取"));
                yield return Wait(() => Entry.ClaimsVisible && !Entry.Busy && !Entry.ClaimsNeedsRefresh, "Actual local progress list query completes");
                Assert.That(repository.Browse(), Is.Empty);
                Assert.That((Scene)runtime.HostScene, Is.Null);
                Pointer(EntryButton("返回经营或房间"));
                yield return Wait(() => ((UIGridKitchen)Entry.Hud)?.Available == true && !Entry.ClaimsVisible, "Progress returns to managed home");
                Assert.That(Entry.Page, Is.EqualTo(ProductEntryPage.Home));
                Assert.That(EntryButton("继续记录"), Is.Not.Null);
                Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Home enters the real solo selector");
                Assert.That((Scene)runtime.HostScene, Is.Null);
                Pointer(SelectionButton("返回模式选择"));
                yield return Wait(() => Runtime != runtime && Entry != null && ((UIGridKitchen)Entry.Hud)?.Available == true, "Selector returns through initialization owner");
                ownedInit = Init.Instance;
                oldCreate();
                Assert.That((Scene)Runtime.HostScene, Is.Null);
            }
            finally
            {
                repository.Dispose();
                RemoveOwnSaveUiFiles(directory);
            }
        }

        [UnityTest]
        [Category("EntryExperience")]
        public IEnumerator EntryNavigationRoomSeparatesChaptersAndTrialMenusWithoutStarting()
        {
            yield return StartProgressHostRoom();
            var authority = (HostSessionComponent)Runtime.Authority;
            Match match = authority.Match;
            long command = Client.NextCommandId;
            Pointer(EntryButton("选择章节"));
            Assert.That(Entry.Page, Is.EqualTo(ProductEntryPage.Chapters));
            Assert.That(EntryHud.ClockText.text, Does.Contain("选择章节"));
            Assert.That(EntryHud.GetComponentsInChildren<Button>(false)
                .Count(button => button.GetComponentInChildren<Text>().text.StartsWith("开启大关 ")), Is.EqualTo(2));
            Assert.That((RestaurantRuntime)match.Restaurant, Is.Null);
            var retiredChapter = CaptureButtonCallback(EntryButton("开启大关 1 · 家常开张"));
            Pointer(EntryButton("返回房间首页"));
            Pointer(EntryButton("菜单试玩"));
            Assert.That(Entry.Page, Is.EqualTo(ProductEntryPage.TrialMenus));
            Assert.That(EntryHud.SettlementText.text, Does.Contain("原菜单链"));
            Assert.That(EntryButton("开启咖啡餐厅"), Is.Not.Null);
            retiredChapter();
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That((RestaurantRuntime)match.Restaurant, Is.Null);
            Pointer(EntryButton("返回房间首页"));
            var retiredTrial = CaptureButtonCallback(EntryButton("菜单试玩"));
            Entry.CloseProductEntry();
            yield return null;
            Entry.OpenProductEntry();
            yield return Wait(() => ((UIGridKitchen)Entry.Hud)?.Available == true, "Room managed navigation reopens");
            retiredTrial();
            Assert.That(Entry.Page, Is.EqualTo(ProductEntryPage.Home));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
        }
    }
}
#endif
