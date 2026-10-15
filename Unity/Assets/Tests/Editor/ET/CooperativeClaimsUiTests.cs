#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using ET.Client;
using Game;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    [FriendOf(typeof(CooperativeSaveResult))]
    [FriendOf(typeof(CooperativeClaim))]
    [FriendOf(typeof(CooperativeClaimState))]
    public sealed partial class SoloRestaurantUiTests
    {
        private static Button ClaimUiButton(MonoUIGridKitchen hud, string caption, bool prefix = false) => hud.GetComponentsInChildren<Button>(false)
            .Single(button => prefix ? button.GetComponentInChildren<Text>().text.StartsWith(caption) : button.GetComponentInChildren<Text>().text == caption);

        [UnityTest]
        [Category("CooperativeClaimsUiStates")]
        public IEnumerator CooperativeClaimsUiOfflineMenuShowsClaimedAndClosedFactsFromActualFiles()
        {
            yield return StartModeLauncher();
            var repository = CreateSaveUiRepository(Runtime, out _, out string profile);
            var own = OfflineProfiles.OwnIdentity(repository);
            string match = Guid.NewGuid().ToString("N"), world = Guid.NewGuid().ToString("N");
            byte[] authority = OfflineProfiles.Sign(repository, OfflineProfiles.MatchAuthority, MemoryPackHelper.Serialize(new ProgressAuthorityValue
            {
                AuthorityId = Guid.NewGuid().ToString("N"), MatchId = match, WorldId = world, HostProfileId = profile, HostKeyId = own.KeyId
            }));
            string MakeTicket()
            {
                string id = Guid.NewGuid().ToString("N");
                byte[] success = OfflineProfiles.Sign(repository, OfflineProfiles.SuccessResult, MemoryPackHelper.Serialize(new CooperativeSuccessValue
                {
                    SaveResultId = Guid.NewGuid().ToString("N"), MatchId = match, WorldId = world, CompletedLevelId = 1,
                    NextLevelId = Tables.Instance.DTLevelConfig.Get(1).NextLevelConfigId, CompletedUtcTicks = DateTime.UtcNow.Ticks,
                    Payload = MemoryPackHelper.Serialize(SelectionFixture()), Authority = authority
                }));
                ProgressClaimFiles.PersistTicket(repository, OfflineProfiles.Sign(repository, OfflineProfiles.ClaimTicket, MemoryPackHelper.Serialize(new ProgressTicketValue
                {
                    TicketId = id, EligibleProfileId = profile, EligibleKeyId = own.KeyId, Success = success
                })), match);
                return id;
            }
            string first = MakeTicket();
            ProgressClaimFiles.Claim(repository, first);
            ProgressClaimFiles.PersistClose(repository, OfflineProfiles.Sign(repository, OfflineProfiles.NormalClose, MemoryPackHelper.Serialize(new ProgressCloseValue
            {
                ReceiptId = Guid.NewGuid().ToString("N"), MatchId = match, ProfileId = profile, Authority = authority
            })), match);
            MakeTicket();
            Pointer(EntryButton("合作进度领取"));
            yield return Wait(() => Entry.ClaimsVisible && !Entry.Busy && Entry.ClaimRows.Length == 2, "Offline query uses files without a Client Session");
            Assert.That(Entry.ClaimRows.Select(row => row.State), Is.EquivalentTo(new[] { ProgressReceiptState.Claimed, ProgressReceiptState.Closed }));
            Pointer(EntryProgressButton("已关闭 ", true));
            Assert.That(Entry.ProgressFeedback, Does.Contain("资格已关闭"));
            Pointer(EntryProgressButton("已领取 ", true));
            Assert.That(Entry.ProgressFeedback, Does.Contain("已领取至"));
            Assert.That(repository.Browse().Length, Is.EqualTo(1));
            Assert.That((ClientSessionComponent)Runtime.Client, Is.Null);
            Pointer(EntryProgressButton("返回经营或房间"));
            yield return Wait(() => !Entry.ClaimsVisible && ((UIGridKitchen)Entry.Hud)?.Available == true, "Offline menu resumes its managed form");
            Assert.That(EntryHud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "创建合作房间"), Is.True);
        }

        [UnityTest]
        [Category("CooperativeClaimsUi")]
        public IEnumerator CooperativeClaimsUiOrdinaryLauncherBothClaimCancelRetryReloadAndNormalMatchEnd()
        {
            yield return StartProgressHostRoom();
            var runtime = Runtime;
            var repository = CreateSaveUiRepository(runtime, out string directory, out string profile);
            yield return VerifyProgressHostIdentity(profile);
            Pointer(EntryProgressButton("返回房间"));
            Entry.CloseProductEntry();
            using (var remote = new CoopUiRemote(runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                ProductEntryPresentation RemoteEntry() => remote.Client.Root().GetComponent<ProductEntryPresentation>();
                MonoUIGridKitchen RemoteEntryHud() => ((UIGridKitchen)RemoteEntry().Hud).View;
                yield return WaitCoopUi(remote, () => remote.Client.Stage == ProductClientStage.Joined && ((UIGridKitchen)RemoteEntry()?.Hud)?.Available == true,
                    "Real KCP Client enters ordinary empty room");
                string otherProfile = Guid.NewGuid().ToString("N");
                var other = remote.Client.Root().AddComponent<SoloSaveRepository, string, string>(directory, otherProfile);
                OfflineProfiles.TrustIdentity(repository, OfflineProfiles.OwnIdentity(other));
                OfflineProfiles.TrustIdentity(other, OfflineProfiles.OwnIdentity(repository));
                Pointer(ClaimUiButton(RemoteEntryHud(), "存档与身份"));
                Pointer(ClaimUiButton(RemoteEntryHud(), "验证本机身份"));
                yield return WaitCoopUi(remote, () => !RemoteEntry().Busy && remote.Client.GetComponent<ProductRoomClient>().Current.VerifiedProfileId == otherProfile,
                    "Remote actively proves its own private key through managed UI");
                RemoteEntry().CloseProductEntry();
                Entry.CloseProductEntry();
                yield return null;
                Entry.OpenProductEntry();
                yield return WaitCoopUi(remote, () => ((UIGridKitchen)Entry.Hud)?.Available == true, "Host room form resumes");
                Pointer(EntryButton("开启内存餐厅"));
                yield return WaitCoopUi(remote, () => Presentation != null && remote.Presentation != null && Grid.Confirmed && remote.Grid.Confirmed,
                    "Both independent world projections exist");
                remote.Presentation.CloseGridHud();
                Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation.Ready, "Host owns current managed gameplay form");
                string buff = Hud.GetComponentsInChildren<Button>(false).First(button => button.GetComponentInChildren<Text>().text.StartsWith("选择 "))
                    .GetComponentInChildren<Text>().text;
                yield return ChapterUiCommand(remote, buff);
                yield return ChapterUiCommand(remote, "开始营业");
                yield return WaitCoopUi(remote, () => Grid.LevelPhase == LevelPhase.Completed && remote.Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationChoiceVersion > 0,
                    "Ordinary cooperative level drains naturally", 90);
                string renovation = Hud.GetComponentsInChildren<Button>(false).First(button => button.GetComponentInChildren<Text>().text.StartsWith("装修 ") &&
                    button.GetComponentInChildren<Text>().text.Contains("等级 0→1"))
                    .GetComponentInChildren<Text>().text;
                yield return ChapterUiCommand(remote, renovation);
                var match = (Match)((HostSessionComponent)runtime.Authority).Match;
                var level = (Level)((RestaurantRuntime)match.Restaurant).CurrentLevel;
                yield return WaitCoopUi(remote, () => ((CooperativeSaveResult)level.ClaimResult)?.SignedSuccess != null &&
                    ProgressClaimFiles.Browse(other).Length == 1 && ProgressClaimFiles.Browse(repository).Length == 1, "Both actual repositories durably receive the shared result");
                CooperativeSaveResult result = level.ClaimResult;
                CooperativeClaim hostClaim = result.Claims[profile];
                string blocked = Path.Combine(repository.DirectoryPath, hostClaim.TicketId + ".ccsave");
                Directory.CreateDirectory(blocked);
                Pointer(Button("合作进度领取"));
                yield return WaitCoopUi(remote, () => Entry.ClaimsVisible && !Entry.Busy && Entry.ClaimRows.Length == 1, "Managed claim list shows available receipt");
                var claimButton = EntryProgressButton("领取新档 ", true);
                var retired = CaptureButtonCallback(claimButton);
                long serial = Entry.ProgressSerial;
                Pointer(claimButton);
                retired(); // 首次点击已退役旧按钮；迟到点击回调不能重复提交。
                Assert.That(Entry.ProgressSerial, Is.EqualTo(serial + 1), "One player click owns one physical request");
                yield return WaitCoopUi(remote, () => !Entry.Busy && Entry.Failure != null, "Actual publication failure is shown and can retry");
                Assert.That(repository.Browse(), Is.Empty);
                Assert.That(Entry.ClaimRows.Single().State, Is.EqualTo(ProgressReceiptState.Available));
                Directory.Delete(blocked);
                Pointer(EntryProgressButton("领取新档 ", true));
                Pointer(EntryProgressButton("取消等待"));
                Pointer(EntryProgressButton("刷新领取状态"));
                yield return WaitCoopUi(remote, () => !Entry.Busy && Entry.ClaimRows.Single().State == ProgressReceiptState.Claimed,
                    "Cancel only the observer; querying actual durable commit finds the original record");
                var saved = repository.Browse().Single();
                Assert.That(saved.ResultId, Is.EqualTo(result.ResultId));
                Pointer(EntryProgressButton("返回经营或房间"));
                yield return WaitCoopUi(remote, () => Presentation.Ready, "Map resumes through its original UI owner");
                Presentation.CloseGridHud();
                ((Scene)remote.Client.IScene).RemoveComponent<GridPresentationComponent>();
                yield return null;
                ((Scene)remote.Client.IScene).AddComponent<GridPresentationComponent>();
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready, "Remote reopens its managed form after shared Editor UI handoff");
                Pointer(ClaimUiButton(remote.Hud, "合作进度领取"));
                yield return WaitCoopUi(remote, () => RemoteEntry().ClaimsVisible && !RemoteEntry().Busy && RemoteEntry().ClaimRows.Length == 1,
                    "Remote independently sees its own unclaimed ticket");
                Pointer(ClaimUiButton(RemoteEntryHud(), "领取新档 ", true));
                yield return WaitCoopUi(remote, () => !RemoteEntry().Busy && RemoteEntry().ClaimRows.Single().State == ProgressReceiptState.Claimed,
                    "Remote explicitly creates its independent local record");
                Assert.That(other.Browse().Single().ResultId, Is.EqualTo(saved.ResultId));
                Assert.That(other.Browse().Single().ProfileId, Is.EqualTo(otherProfile));
                Assert.That(other.Browse().Single().SlotId, Is.Not.EqualTo(saved.SlotId));
                Pointer(ClaimUiButton(RemoteEntryHud(), "返回经营或房间"));
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready, "Remote exits claim panel");
                remote.Presentation.CloseGridHud();
                Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation.Ready, "Host resumes settlement");
                Pointer(Button("返回房间"));
                yield return WaitCoopUi(remote, () => (RestaurantRuntime)match.Restaurant == null && Entry != null && ((UIGridKitchen)Entry.Hud)?.Available == true,
                    "Retiring restaurant preserves the same Match");
                RemoteEntry().CloseProductEntry();
                yield return null;
                Entry.OpenProductEntry();
                Assert.That(match.GetComponent<CooperativeClaimState>().Closing, Is.False);
                Pointer(EntryProgressButton("存档与身份"));
                Pointer(EntryProgressButton("本机成功记录"));
                yield return WaitCoopUi(remote, () => !Entry.Busy && Entry.ProgressHeaders.Length == 1, "Claimed file appears in the original progress browser");
                Pointer(EntryProgressButton("读取 记录 ", true));
                yield return WaitCoopUi(remote, () => Presentation != null && Grid.Confirmed && remote.Grid.Confirmed, "Claimed file creates a real next-level restaurant", 30);
                remote.Presentation.CloseGridHud(); Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation.Ready, "Host view of reloaded record ready");
                Assert.That(Grid.LevelConfigId, Is.EqualTo(saved.NextLevelId));
                Assert.That(Grid.RenovationApplied.Count, Is.GreaterThan(0));
                Assert.That(Grid.Stations.Values.Any(station => station.UpgradeLevel == 1), Is.True, "Cooperative upgrade survives claim and real reload");
                long commands = Client.NextCommandId;
                retired();
                Assert.That(Client.NextCommandId, Is.EqualTo(commands));
                Assert.That(repository.Browse().Length, Is.EqualTo(1));
                Pointer(Button("返回房间"));
                yield return WaitCoopUi(remote, () => (RestaurantRuntime)match.Restaurant == null && ((UIGridKitchen)Entry.Hud)?.Available == true, "Return from restored restaurant");
                RemoteEntry().CloseProductEntry();
                yield return null;
                Entry.OpenProductEntry();
                string savedFile = Path.Combine(repository.DirectoryPath, saved.SlotId + ".ccsave");
                Pointer(EntryButton("返回模式选择"));
                yield return WaitCoopUi(remote, () => Runtime != runtime && Runtime?.Config.ShowModeMenu == true && Entry != null && ((UIGridKitchen)Entry.Hud)?.Available == true,
                    "Explicit normal whole-Match end waits for receipts then returns to ordinary mode menu", 30);
                ownedInit = Init.Instance;
                retired();
                Assert.That(Runtime.Config.ShowModeMenu, Is.True);
                Assert.That(File.Exists(savedFile), Is.True);
            }
        }
    }
}
#endif
