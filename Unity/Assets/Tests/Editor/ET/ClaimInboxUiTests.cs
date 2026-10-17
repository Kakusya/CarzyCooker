#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
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
        private static string InboxUiTicket(SoloSaveRepository repository, string match, out byte[] authority)
        {
            var own = OfflineProfiles.OwnIdentity(repository);
            authority = OfflineProfiles.Sign(repository, OfflineProfiles.MatchAuthority, MemoryPackHelper.Serialize(new ProgressAuthorityValue
            {
                AuthorityId = Guid.NewGuid().ToString("N"), MatchId = match, WorldId = Guid.NewGuid().ToString("N"),
                HostProfileId = own.ProfileId, HostKeyId = own.KeyId
            }));
            string ticket = Guid.NewGuid().ToString("N");
            byte[] success = OfflineProfiles.Sign(repository, OfflineProfiles.SuccessResult, MemoryPackHelper.Serialize(new CooperativeSuccessValue
            {
                SaveResultId = Guid.NewGuid().ToString("N"), MatchId = match, WorldId = ProgressClaimCodec.Decode<ProgressAuthorityValue>(
                    OfflineProfiles.Verify(authority, OfflineProfiles.MatchAuthority).Body).WorldId,
                CompletedLevelId = 1, NextLevelId = Tables.Instance.DTLevelConfig.Get(1).NextLevelConfigId,
                CompletedUtcTicks = DateTime.UtcNow.Ticks, Payload = MemoryPackHelper.Serialize(SelectionFixture()), Authority = authority
            }));
            ProgressClaimFiles.PersistTicket(repository, OfflineProfiles.Sign(repository, OfflineProfiles.ClaimTicket, MemoryPackHelper.Serialize(new ProgressTicketValue
            {
                TicketId = ticket, EligibleProfileId = own.ProfileId, EligibleKeyId = own.KeyId, Success = success
            })), match);
            return ticket;
        }

        private static string InboxUiTitle(string ticket) => "第 1 关 · 0 分 · " + ticket.Substring(0, 6);

        [UnityTest, Category("EntryInbox")]
        public IEnumerator EntryInboxActualFilesCountsFiltersClaimDestinationFailureCancelAndStaleCallbacks()
        {
            yield return StartModeLauncher();
            Assert.That(Runtime.Root().GetComponent<SoloSaveRepository>(), Is.Null, "Passive badge must not create a profile or repository");
            Assert.That(Entry.ClaimInboxState, Is.EqualTo(ClaimInboxDisplayState.Unknown));
            Assert.That(EntryHud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "合作待领：未查询 · 查看"), Is.True);
            var repository = CreateSaveUiRepository(Runtime, out _, out _);
            yield return Wait(() => Entry.ClaimInboxState == ClaimInboxDisplayState.Ready, "Existing repository queried through Root mailbox");
            Assert.That(Entry.AvailableClaimCount, Is.Zero);
            EventSystem.Instance.Publish(Runtime.Root(), new ProgressInboxChanged());
            yield return Wait(() => Entry.ClaimInboxState == ClaimInboxDisplayState.Ready, "Notice without a persisted ticket reads actual empty inbox");
            Assert.That(Entry.AvailableClaimCount, Is.Zero);
            string claimed = InboxUiTicket(repository, Guid.NewGuid().ToString("N"), out _);
            ProgressClaimFiles.Claim(repository, claimed);
            string available = InboxUiTicket(repository, Guid.NewGuid().ToString("N"), out _);
            string closedMatch = Guid.NewGuid().ToString("N");
            string closed = InboxUiTicket(repository, closedMatch, out byte[] authority);
            ProgressClaimFiles.PersistClose(repository, OfflineProfiles.Sign(repository, OfflineProfiles.NormalClose, MemoryPackHelper.Serialize(new ProgressCloseValue
            {
                ReceiptId = Guid.NewGuid().ToString("N"), MatchId = closedMatch, ProfileId = repository.ProfileId, Authority = authority
            })), closedMatch);
            string[] files = Directory.GetFiles(repository.DirectoryPath, "*", SearchOption.AllDirectories).OrderBy(path => path).ToArray();
            yield return Wait(() => Entry.ClaimInboxState == ClaimInboxDisplayState.Ready && Entry.AvailableClaimCount == 1, "Only durable available ticket is counted");
            Assert.That(Directory.GetFiles(repository.DirectoryPath, "*", SearchOption.AllDirectories).OrderBy(path => path), Is.EqualTo(files));
            EventSystem.Instance.Publish(Runtime.Root(), new ProgressInboxChanged());
            yield return Wait(() => Entry.ClaimInboxState == ClaimInboxDisplayState.Ready, "Notification alone queries actual durable files");
            Assert.That(Entry.AvailableClaimCount, Is.EqualTo(1));
            Pointer(EntryButton("合作待领：1 · 查看"));
            yield return Wait(() => Entry.ClaimsVisible && !Entry.Busy && Entry.ClaimRows.Length == 3, "Actual list opens with three states");
            Pointer(EntryProgressButton("仅可领取"));
            Assert.That(EntryHud.GetComponentsInChildren<Button>(false).Count(button => button.GetComponentInChildren<Text>().text.StartsWith("查看结果 ")), Is.EqualTo(1));
            Pointer(EntryProgressButton("查看结果 " + InboxUiTitle(available)));
            Assert.That(EntryHud.SettlementText.text, Does.Contain("可领取").And.Contain("尚未新建个人记录"));
            var retired = CaptureButtonCallback(EntryProgressButton("领取此结果"));
            Pointer(EntryProgressButton("返回领取列表")); retired();
            Assert.That(repository.Browse().Length, Is.EqualTo(1), "Viewing and retired confirmation do not claim");
            Pointer(EntryProgressButton("仅已关闭"));
            Pointer(EntryProgressButton("查看结果 " + InboxUiTitle(closed)));
            Assert.That(EntryHud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "领取此结果"), Is.False);
            Pointer(EntryProgressButton("返回领取列表"));
            Pointer(EntryProgressButton("仅可领取"));
            Pointer(EntryProgressButton("查看结果 " + InboxUiTitle(available)));
            Pointer(EntryProgressButton("领取此结果"));
            yield return Wait(() => !Entry.Busy && Entry.ClaimRows.Single(row => row.TicketId == available).State == ProgressReceiptState.Claimed,
                "Explicit original claim publishes one record and receipt");
            Assert.That(Entry.AvailableClaimCount, Is.Zero); Assert.That(repository.Browse().Length, Is.EqualTo(2));
            string slot = Entry.ClaimRows.Single(row => row.TicketId == available).SlotId;
            Pointer(EntryProgressButton("查看已领记录"));
            yield return Wait(() => !Entry.Busy && Entry.ProgressHeaders.Any(header => header.SlotId == slot), "Original record selector reads current header");
            Assert.That(EntryHud.SettlementText.text, Does.Contain(slot).And.Contain("当前修订：1").And.Contain("尚未加载"));
            Assert.That((Scene)Runtime.HostScene, Is.Null); Assert.That(Client, Is.Null);
            Pointer(EntryProgressButton("返回领取列表"));
            Pointer(EntryProgressButton("仅已领取"));
            Assert.That(EntryHud.GetComponentsInChildren<Button>(false).Count(button => button.GetComponentInChildren<Text>().text.StartsWith("已领取 ")), Is.EqualTo(2));
            string foregroundCorrupt = Path.Combine(repository.DirectoryPath, "Tickets", Guid.NewGuid().ToString("N") + ".ccticket");
            File.WriteAllText(foregroundCorrupt, "owned foreground invalid ticket");
            Pointer(EntryProgressButton("刷新领取状态"));
            yield return Wait(() => !Entry.Busy && Entry.ClaimInboxState == ClaimInboxDisplayState.Failed, "Foreground query failure retires old eligibility rows");
            Assert.That(Entry.ClaimRows, Is.Empty);
            Assert.That(EntryHud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text.StartsWith("领取新档 ") ||
                button.GetComponentInChildren<Text>().text.StartsWith("查看结果 ")), Is.False);
            File.Delete(foregroundCorrupt);
            Pointer(EntryProgressButton("刷新领取状态"));
            yield return Wait(() => !Entry.Busy && Entry.ClaimRows.Length == 3 && Entry.ClaimInboxState == ClaimInboxDisplayState.Ready,
                "Explicit retry restores only current durable list");
            Pointer(EntryProgressButton("返回经营或房间"));
            yield return Wait(() => !Entry.ClaimsVisible && ((UIGridKitchen)Entry.Hud)?.Available == true, "Mode badge resumes");
            Entry.InvalidateClaimInbox(); Entry.RefreshProductEntry();
            Assert.That(Entry.ClaimInboxState, Is.EqualTo(ClaimInboxDisplayState.Loading));
            Pointer(EntryButton("合作待领：查询中 · 取消"));
            yield return null; yield return null;
            Assert.That(Entry.ClaimInboxState, Is.EqualTo(ClaimInboxDisplayState.Unknown), "Late read cannot overwrite cancelled display");
            Assert.That(repository.Browse().Length, Is.EqualTo(2));
            string corrupt = Path.Combine(repository.DirectoryPath, "Tickets", Guid.NewGuid().ToString("N") + ".ccticket");
            File.WriteAllText(corrupt, "owned invalid ticket");
            Entry.InvalidateClaimInbox();
            yield return Wait(() => Entry.ClaimInboxState == ClaimInboxDisplayState.Failed, "Actual malformed ticket makes query unknown, not zero");
            Assert.That(EntryButton("合作待领：查询失败 · 重试"), Is.Not.Null);
            File.Delete(corrupt);
            Pointer(EntryButton("合作待领：查询失败 · 重试"));
            yield return Wait(() => Entry.ClaimInboxState == ClaimInboxDisplayState.Ready, "Explicit retry reads repaired owned files");
            Assert.That(Entry.AvailableClaimCount, Is.Zero);
            var oldOpen = CaptureButtonCallback(EntryButton("合作待领：0 · 查看"));
            Entry.CloseProductEntry(); Entry.OpenProductEntry();
            yield return Wait(() => ((UIGridKitchen)Entry.Hud)?.Available == true, "Badge HUD pool reopen");
            oldOpen(); Assert.That(Entry.ClaimsVisible, Is.False); Assert.That(repository.Browse().Length, Is.EqualTo(2));
            Pointer(EntryButton("合作待领：0 · 查看"));
            yield return Wait(() => Entry.ClaimsVisible && !Entry.Busy && Entry.ClaimRows.Length == 3, "Reopen original durable rows before offline selection");
            Pointer(EntryProgressButton("查看结果 " + InboxUiTitle(available)));
            Pointer(EntryProgressButton("查看已领记录"));
            yield return Wait(() => !Entry.Busy && Entry.ProgressHeaders.Any(header => header.SlotId == slot), "Current record header before explicit solo handoff");
            var oldNavigation = CaptureButtonCallback(EntryProgressButton("前往单机选档"));
            Pointer(EntryProgressButton("前往单机选档"));
            yield return Wait(() => Selection?.InspectedSave?.SlotId == slot && !Selection.LoadingSelection && ((UIGridKitchen)Selection.Hud)?.Available == true,
                "Original Solo selector fully validates focused record without loading it");
            Assert.That(Selection.InspectedSave.Revision, Is.EqualTo(1));
            Assert.That(Client, Is.Null); Assert.That((Scene)Runtime.HostScene, Is.Null);
            var inspected = Selection; oldNavigation();
            Assert.That(Selection, Is.SameAs(inspected)); Assert.That(repository.Browse().Length, Is.EqualTo(2));
            EventSystem.Instance.Publish(Runtime.Root(), new SoloSaveSelectionRequested { SlotId = slot, Revision = 2 });
            yield return Wait(() => Selection != inspected && !Selection.LoadingSelection && Selection.Failure == "RecordChanged",
                "Same Root receiver rejects an outdated expected revision using actual current file header");
            Assert.That(Selection.InspectedSave, Is.Null); Assert.That(Client, Is.Null); Assert.That((Scene)Runtime.HostScene, Is.Null);
            string ownKey = Path.Combine(repository.DirectoryPath, "Identity", "own.cckey");
            string ownPublic = Path.Combine(repository.DirectoryPath, "Identity", "own.ccidentity");
            File.Move(ownKey, ownKey + ".held"); File.Move(ownPublic, ownPublic + ".held");
            Entry.InvalidateClaimInbox();
            yield return Wait(() => Entry.ClaimInboxState == ClaimInboxDisplayState.Failed, "Passive query fails on missing existing identity");
            Assert.That(File.Exists(ownKey), Is.False); Assert.That(File.Exists(ownPublic), Is.False, "Browse must not generate a replacement identity");
            File.Move(ownKey + ".held", ownKey); File.Move(ownPublic + ".held", ownPublic);
            Entry.InvalidateClaimInbox();
            yield return Wait(() => Entry.ClaimInboxState == ClaimInboxDisplayState.Ready, "Restored owned identity reads original tickets without reinitializing");
            Assert.That(Entry.AvailableClaimCount, Is.Zero); Assert.That(repository.Browse().Length, Is.EqualTo(2));
        }
        [UnityTest, Category("EntryInbox"), Timeout(180000)]
        public IEnumerator EntryInboxActualKcpDurableResultReturnRoomThenBothExplicitlyClaimAndLocateOriginalRecords()
        {
            yield return StartProgressHostRoom();
            var repository = CreateSaveUiRepository(Runtime, out string directory, out string profile);
            yield return VerifyProgressHostIdentity(profile);
            Pointer(EntryProgressButton("返回房间"));
            Entry.CloseProductEntry();
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                ProductEntryPresentation OtherEntry() => remote.Client.Root().GetComponent<ProductEntryPresentation>();
                MonoUIGridKitchen OtherHud() => ((UIGridKitchen)OtherEntry().Hud).View;
                yield return WaitCoopUi(remote, () => remote.Client.Stage == ProductClientStage.Joined && ((UIGridKitchen)OtherEntry()?.Hud)?.Available == true,
                    "Actual KCP room member before eligibility freeze");
                string otherProfile = Guid.NewGuid().ToString("N");
                var other = remote.Client.Root().AddComponent<SoloSaveRepository, string, string>(directory, otherProfile);
                OfflineProfiles.TrustIdentity(repository, OfflineProfiles.OwnIdentity(other));
                OfflineProfiles.TrustIdentity(other, OfflineProfiles.OwnIdentity(repository));
                Pointer(ClaimUiButton(OtherHud(), "存档与身份"));
                Pointer(ClaimUiButton(OtherHud(), "身份与高级操作"));
                Pointer(ClaimUiButton(OtherHud(), "验证本机身份"));
                yield return WaitCoopUi(remote, () => !OtherEntry().Busy && remote.Client.GetComponent<ProductRoomClient>().Current.VerifiedProfileId == otherProfile,
                    "Original proof validates remote identity");
                OtherEntry().CloseProductEntry();
                yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Remote room form releases shared Editor UI");
                Entry.OpenProductEntry();
                yield return WaitCoopUi(remote, () => ((UIGridKitchen)Entry.Hud)?.Available == true, "Host original room form");
                yield return StartPreviewAndConfirm(EntryButton("开启内存餐厅"));
                yield return WaitCoopUi(remote, () => Presentation != null && remote.Presentation != null && Grid.Confirmed && remote.Grid.Confirmed,
                    "Both original projections exist");
                remote.KeepMapHeadlessWhileHostOwnsDisplay();
                yield return WaitCoopUi(remote, () => Presentation.Ready || !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Remote releases singleton or Host already owns gameplay");
                Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation.Ready, "Host gameplay owner ready");
                Match match = ((HostSessionComponent)Runtime.Authority).Match;
                RestaurantRuntime restaurant = match.Restaurant;
                ((Level)restaurant.CurrentLevel).OperatingTicks = 100; ((GridKitchenComponent)restaurant.Kitchen).Dirty = true;
                yield return ContentUiPump(UnlockUiStartOperating(), remote);
                yield return WaitCoopUi(remote, () => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationChoiceVersion > 0, "Natural actual result before explicit skip", 75);
                yield return ContentUiPump(Command("本关不选装修"), remote);
                yield return WaitCoopUi(remote, () => Entry.ClaimInboxState == ClaimInboxDisplayState.Ready && Entry.AvailableClaimCount == 1 &&
                    OtherEntry().ClaimInboxState == ClaimInboxDisplayState.Ready && OtherEntry().AvailableClaimCount == 1,
                    "Only independently persisted tickets become both badge counts");
                Assert.That(repository.Browse(), Is.Empty); Assert.That(other.Browse(), Is.Empty);
                Assert.That(Button("合作待领：1 · 查看"), Is.Not.Null);
                yield return ReturnToRoomThroughConfirmation();
                yield return WaitCoopUi(remote, () => (RestaurantRuntime)match.Restaurant == null && ((UIGridKitchen)Entry.Hud)?.Available == true,
                    "Return to original room without claiming or closing Match");
                OtherEntry().CloseProductEntry();
                Entry.OpenProductEntry();
                yield return WaitCoopUi(remote, () => ((UIGridKitchen)Entry.Hud)?.Available == true, "Host room badge owns current form");
                Assert.That(Entry.AvailableClaimCount, Is.EqualTo(1));
                Pointer(EntryProgressButton("合作待领：1 · 查看"));
                yield return WaitCoopUi(remote, () => Entry.ClaimsVisible && !Entry.Busy && Entry.ClaimRows.Length == 1, "Durable result survives room return");
                string ticket = Entry.ClaimRows.Single().TicketId;
                string result = Entry.ClaimRows.Single().SaveResultId;
                Pointer(EntryProgressButton("查看结果 ", true));
                Assert.That(EntryHud.SettlementText.text, Does.Contain(result).And.Contain("尚未新建个人记录"));
                Assert.That(repository.Browse(), Is.Empty);
                var retiredClaim = CaptureButtonCallback(EntryProgressButton("领取此结果"));
                long serial = Entry.ProgressSerial;
                Pointer(EntryProgressButton("领取此结果")); retiredClaim();
                Assert.That(Entry.ProgressSerial, Is.EqualTo(serial + 1));
                yield return WaitCoopUi(remote, () => !Entry.Busy && Entry.ClaimRows.Single().State == ProgressReceiptState.Claimed,
                    "Host explicitly publishes one independent record");
                var saved = repository.Browse().Single();
                Assert.That(Entry.AvailableClaimCount, Is.Zero);
                Pointer(EntryProgressButton("查看已领记录"));
                yield return WaitCoopUi(remote, () => !Entry.Busy && Entry.ProgressHeaders.Any(header => header.SlotId == saved.SlotId), "Original selector reads claimed slot and revision");
                Assert.That(EntryHud.SettlementText.text, Does.Contain(saved.SlotId).And.Contain("当前修订：1"));
                Assert.That((RestaurantRuntime)match.Restaurant, Is.Null, "Viewing destination never starts a restaurant");
                Pointer(EntryProgressButton("返回领取列表"));
                Pointer(EntryProgressButton("返回经营或房间"));
                yield return WaitCoopUi(remote, () => !Entry.ClaimsVisible && ((UIGridKitchen)Entry.Hud)?.Available == true, "Host room reopens with count zero");
                Assert.That(EntryProgressButton("合作待领：0 · 查看"), Is.Not.Null);
                Entry.CloseProductEntry();
                yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases room HUD for remote");
                OtherEntry().OpenProductEntry();
                yield return WaitCoopUi(remote, () => ((UIGridKitchen)OtherEntry().Hud)?.Available == true, "Remote room badge visible");
                Pointer(ClaimUiButton(OtherHud(), "合作待领：1 · 查看"));
                yield return WaitCoopUi(remote, () => OtherEntry().ClaimsVisible && !OtherEntry().Busy && OtherEntry().ClaimRows.Length == 1, "Remote independently opens durable unclaimed result");
                Pointer(ClaimUiButton(OtherHud(), "查看结果 ", true));
                Assert.That(OtherHud().SettlementText.text, Does.Contain(result));
                Pointer(ClaimUiButton(OtherHud(), "领取此结果"));
                yield return WaitCoopUi(remote, () => !OtherEntry().Busy && OtherEntry().ClaimRows.Single().State == ProgressReceiptState.Claimed,
                    "Remote explicitly claims original shared result");
                var theirs = other.Browse().Single();
                Assert.That(theirs.ResultId, Is.EqualTo(saved.ResultId)); Assert.That(theirs.SlotId, Is.Not.EqualTo(saved.SlotId));
                Assert.That(OtherEntry().AvailableClaimCount, Is.Zero);
                Pointer(ClaimUiButton(OtherHud(), "查看已领记录"));
                yield return WaitCoopUi(remote, () => !OtherEntry().Busy && OtherEntry().ProgressHeaders.Any(header => header.SlotId == theirs.SlotId),
                    "Client may locate its actual claimed record without restaurant control");
                Assert.That(OtherHud().SettlementText.text, Does.Contain(theirs.SlotId).And.Contain("当前修订：1"));
                Assert.That(OtherHud().GetComponentsInChildren<Button>(false).Any(button =>
                    button.GetComponentInChildren<Text>().text.StartsWith("读取 记录") || button.GetComponentInChildren<Text>().text == "前往单机选档"), Is.False);
                Assert.That((RestaurantRuntime)match.Restaurant, Is.Null);
                Pointer(ClaimUiButton(OtherHud(), "返回领取列表"));
                Pointer(ClaimUiButton(OtherHud(), "返回经营或房间"));
                yield return WaitCoopUi(remote, () => !OtherEntry().ClaimsVisible && ((UIGridKitchen)OtherEntry().Hud)?.Available == true, "Remote room reopens without duplicate claim");
                Assert.That(ClaimUiButton(OtherHud(), "合作待领：0 · 查看"), Is.Not.Null);
                string selfTrust = Path.Combine(other.DirectoryPath, "Identity", "Trusted", otherProfile + ".ccidentity");
                File.Move(selfTrust, selfTrust + ".held");
                OtherEntry().InvalidateClaimInbox();
                yield return WaitCoopUi(remote, () => OtherEntry().ClaimInboxState == ClaimInboxDisplayState.Failed,
                    "Foreign Host ticket does not authorize implicit repair of this Client's self trust");
                Assert.That(File.Exists(selfTrust), Is.False, "Passive query must not trust or recreate identity files");
                File.Move(selfTrust + ".held", selfTrust);
                OtherEntry().InvalidateClaimInbox();
                yield return WaitCoopUi(remote, () => OtherEntry().ClaimInboxState == ClaimInboxDisplayState.Ready && OtherEntry().AvailableClaimCount == 0,
                    "Explicitly restored fixture resumes actual zero count");
                retiredClaim(); Assert.That(repository.Browse().Length, Is.EqualTo(1)); Assert.That(other.Browse().Length, Is.EqualTo(1));
                Assert.That((RestaurantRuntime)match.Restaurant, Is.Null); Assert.That(((ProcessInnerSender)Runtime.Sender).IsStopped, Is.False);
            }
        }
    }
}
#endif
