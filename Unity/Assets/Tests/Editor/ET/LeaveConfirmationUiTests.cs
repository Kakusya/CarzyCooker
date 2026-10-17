#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using ET.Client;
using Game;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        private IEnumerator ReturnToRoomThroughConfirmation()
        {
            Pointer(Button("返回房间"));
            yield return Wait(() => Entry?.LeaveAction == ProductLeaveAction.HostReturnRoom && ((UIGridKitchen)Entry.Hud)?.Available == true,
                "Original map hands its UI to the owned leave confirmation");
            Pointer(EntryProgressButton("确认仅返回房间"));
            yield return Wait(() => !Entry.LeavePending && Entry.LeaveAction == ProductLeaveAction.None && !Entry.ClaimsVisible &&
                ((UIGridKitchen)Entry.Hud)?.Available == true, "Original close reply finishes and hands the owned UI back to the room");
        }

        [UnityTest, Category("EntryLeave")]
        public IEnumerator EntryLeaveSoloOperatingCancelKeepsClockAndFilesThenExplicitReturnRetiresOldCallbacks()
        {
            yield return StartLauncher();
            var originalRuntime = Runtime;
            var repository = CreateSaveUiRepository(Runtime, out _, out string profile);
            var header = SelectionHeader(profile); repository.Commit(header, SelectionFixture());
            string savedPath = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
            byte[] savedBytes = File.ReadAllBytes(savedPath);
            yield return UnlockUiStartOperating();
            long command = Client.NextCommandId;
            string world = Grid.WorldId;
            var oldTake = CaptureButtonCallback(Button("拿 碗台 ·", true));
            Pointer(Button("返回模式选择"));
            yield return Wait(() => Entry.LeaveAction == ProductLeaveAction.SoloReturnModes && ((UIGridKitchen)Entry.Hud)?.Available == true, "Solo confirmation opens in original managed UI");
            Assert.That(EntryHud.SettlementText.text, Does.Contain("尚未保存").And.Contain("已发布记录保留"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            long tick = Grid.Tick;
            yield return Wait(() => Grid.Tick > tick, "Confirmation does not pause the actual game clock");
            Assert.That(Grid.LevelPaused, Is.False);
            Assert.That(File.ReadAllBytes(savedPath), Is.EqualTo(savedBytes));
            var oldConfirm = CaptureButtonCallback(EntryProgressButton("确认返回模式选择"));
            Pointer(EntryProgressButton("取消离开"));
            yield return Wait(() => Presentation?.Ready == true, "Cancel returns the pooled HUD to its original map owner");
            oldConfirm(); oldTake();
            Assert.That(Runtime, Is.SameAs(originalRuntime)); Assert.That(Grid.WorldId, Is.EqualTo(world));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return Command("拿 碗台 ·", true);
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.Not.Zero);
            yield return Command("暂停");
            tick = Grid.Tick;
            Pointer(Button("返回模式选择"));
            yield return Wait(() => ((UIGridKitchen)Entry.Hud)?.Available == true && Entry.LeaveAction != ProductLeaveAction.None, "Confirmation reopens");
            for (int frame = 0; frame < 12; ++frame) { yield return null; }
            Assert.That(Grid.LevelPaused, Is.True); Assert.That(Grid.Tick, Is.EqualTo(tick), "Leave preview does not resume an already paused world");
            var confirm = CaptureButtonCallback(EntryProgressButton("确认返回模式选择"));
            Pointer(EntryProgressButton("确认返回模式选择")); confirm();
            Assert.That(Entry.LeavePending, Is.True);
            Assert.That(EntryHud.GetComponentsInChildren<Button>(false).Any(b => b.GetComponentInChildren<Text>().text == "取消离开"), Is.False);
            yield return Wait(() => Runtime != originalRuntime && Runtime?.Config.ShowModeMenu == true && ((UIGridKitchen)Entry?.Hud)?.Available == true, "Confirmed Solo leave completes original initialization switch");
            ownedInit = Init.Instance;
            Assert.That(File.ReadAllBytes(savedPath), Is.EqualTo(savedBytes));
            Pointer(EntryButton("单机餐厅"));
            yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "New original Solo selector");
            yield return StartPreviewAndConfirm(SelectionButton("内存试玩"));
            yield return Wait(() => Presentation?.Ready == true, "New independent world");
            command = Client.NextCommandId; confirm(); oldConfirm();
            Assert.That(Grid.WorldId, Is.Not.EqualTo(world)); Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return Command("拿 碗台 ·", true);
        }

        [UnityTest, Category("EntryLeave"), Timeout(180000)]
        public IEnumerator EntryLeaveHostRoomReturnPreservesUnclaimedFilesThenWholeMatchClosesActualReceipts()
        {
            yield return StartProgressHostRoom();
            var originalRuntime = Runtime;
            var repository = CreateSaveUiRepository(Runtime, out string directory, out string profile);
            var existing = SelectionHeader(profile); repository.Commit(existing, SelectionFixture());
            string savedPath = Path.Combine(repository.DirectoryPath, existing.SlotId + ".ccsave");
            byte[] savedBytes = File.ReadAllBytes(savedPath);
            yield return VerifyProgressHostIdentity(profile);
            Pointer(EntryProgressButton("返回房间")); Entry.CloseProductEntry();
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                ProductEntryPresentation OtherEntry() => remote.Client.Root().GetComponent<ProductEntryPresentation>();
                MonoUIGridKitchen OtherHud() => ((UIGridKitchen)OtherEntry().Hud).View;
                yield return WaitCoopUi(remote, () => remote.Client.Stage == ProductClientStage.Joined && ((UIGridKitchen)OtherEntry()?.Hud)?.Available == true, "Real Client joins original room");
                string otherProfile = Guid.NewGuid().ToString("N");
                var other = remote.Client.Root().AddComponent<SoloSaveRepository, string, string>(directory, otherProfile);
                OfflineProfiles.TrustIdentity(repository, OfflineProfiles.OwnIdentity(other));
                OfflineProfiles.TrustIdentity(other, OfflineProfiles.OwnIdentity(repository));
                Pointer(ClaimUiButton(OtherHud(), "存档与身份"));
                Pointer(ClaimUiButton(OtherHud(), "身份与高级操作"));
                Pointer(ClaimUiButton(OtherHud(), "验证本机身份"));
                yield return WaitCoopUi(remote, () => !OtherEntry().Busy && remote.Client.GetComponent<ProductRoomClient>().Current.VerifiedProfileId == otherProfile, "Remote proves original identity");
                OtherEntry().CloseProductEntry();
                yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Remote releases shared Editor UI");
                Entry.OpenProductEntry();
                yield return WaitCoopUi(remote, () => ((UIGridKitchen)Entry.Hud)?.Available == true, "Host room owner reopens");
                yield return StartPreviewAndConfirm(EntryButton("开启内存餐厅"));
                yield return WaitCoopUi(remote, () => Presentation != null && remote.Presentation != null && Grid.Confirmed && remote.Grid.Confirmed, "Both actual world projections");
                remote.KeepMapHeadlessWhileHostOwnsDisplay();
                Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation.Ready, "Host managed map");
                Match match = ((HostSessionComponent)Runtime.Authority).Match;
                RestaurantRuntime restaurant = match.Restaurant;
                ((Level)restaurant.CurrentLevel).OperatingTicks = 100;
                ((GridKitchenComponent)restaurant.Kitchen).Dirty = true;
                yield return ContentUiPump(UnlockUiStartOperating(), remote);
                yield return WaitCoopUi(remote, () => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationChoiceVersion > 0, "Natural real cooperative result", 75);
                yield return ContentUiPump(Command("本关不选装修"), remote);
                yield return WaitCoopUi(remote, () => ProgressClaimFiles.Browse(repository).Length == 1 && ProgressClaimFiles.Browse(other).Length == 1, "Both unclaimed files are durable");
                Assert.That(ProgressClaimFiles.Browse(repository).Single().State, Is.EqualTo(ProgressReceiptState.Available));
                string world = Grid.WorldId, matchId = match.MatchId;
                long commands = Client.NextCommandId;
                Pointer(Button("返回房间"));
                yield return WaitCoopUi(remote, () => Entry.LeaveAction == ProductLeaveAction.HostReturnRoom && ((UIGridKitchen)Entry.Hud)?.Available == true, "Host reads exact retirement scope");
                Assert.That(EntryHud.SettlementText.text, Does.Contain("保留同一房间").And.Contain("既有领取资格"));
                var retired = CaptureButtonCallback(EntryProgressButton("确认仅返回房间"));
                Pointer(EntryProgressButton("取消离开"));
                yield return WaitCoopUi(remote, () => Presentation.Ready, "Cancel leaves original settlement and tickets");
                Assert.That(Client.NextCommandId, Is.EqualTo(commands)); Assert.That((RestaurantRuntime)match.Restaurant, Is.SameAs(restaurant));
                yield return ContentUiPump(ReturnToRoomThroughConfirmation(), remote);
                yield return WaitCoopUi(remote, () => (RestaurantRuntime)match.Restaurant == null && ((UIGridKitchen)Entry.Hud)?.Available == true, "Explicit retirement returns original room");
                Assert.That(match.MatchId, Is.EqualTo(matchId)); Assert.That(Client.Roster.Count, Is.EqualTo(2));
                Assert.That(((ProcessInnerSender)Runtime.Sender).IsStopped, Is.False);
                Assert.That(ProgressClaimFiles.Browse(repository).Single().State, Is.EqualTo(ProgressReceiptState.Available));
                Assert.That(ProgressClaimFiles.Browse(other).Single().State, Is.EqualTo(ProgressReceiptState.Available));
                OtherEntry().CloseProductEntry(); Entry.CloseProductEntry();
                yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Both room views release before Host reopens");
                Entry.OpenProductEntry();
                yield return WaitCoopUi(remote, () => ((UIGridKitchen)Entry.Hud)?.Available == true, "Host room form is available before a new preview");
                yield return StartPreviewAndConfirm(EntryButton("开启内存餐厅"));
                yield return WaitCoopUi(remote, () => Presentation != null && Grid.Confirmed && remote.Grid.Confirmed, "Same members observe a new world");
                remote.KeepMapHeadlessWhileHostOwnsDisplay(); Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation.Ready, "New world owned display");
                commands = Client.NextCommandId; retired();
                Assert.That(Client.NextCommandId, Is.EqualTo(commands)); Assert.That(Grid.WorldId, Is.Not.EqualTo(world));
                Pointer(Button("结束整个房间"));
                yield return WaitCoopUi(remote, () => Entry.LeaveAction == ProductLeaveAction.HostEndRoom && ((UIGridKitchen)Entry.Hud)?.Available == true, "Whole Match has a separate confirmation");
                Assert.That(EntryHud.SettlementText.text, Does.Contain("关闭回执").And.Contain("不能撤销"));
                var end = CaptureButtonCallback(EntryProgressButton("确认结束整个房间"));
                Pointer(EntryProgressButton("确认结束整个房间")); end();
                Assert.That(Entry.LeavePending, Is.True);
                Assert.That(EntryHud.GetComponentsInChildren<Button>(false).Any(b => b.GetComponentInChildren<Text>().text == "取消离开"), Is.False);
                yield return ContentUiPump(Wait(() => Runtime != originalRuntime && Runtime?.Config.ShowModeMenu == true && ((UIGridKitchen)Entry?.Hud)?.Available == true, "Original close receipts finish before mode switch", 30), remote);
                ownedInit = Init.Instance;
                var reopened = Runtime.Root().AddComponent<SoloSaveRepository, string, string>(directory, profile);
                Assert.That(ProgressClaimFiles.Browse(reopened).Single().State, Is.EqualTo(ProgressReceiptState.Closed));
                Assert.That(ProgressClaimFiles.Browse(other).Single().State, Is.EqualTo(ProgressReceiptState.Closed));
                Assert.That(File.ReadAllBytes(savedPath), Is.EqualTo(savedBytes));
                Assert.That(reopened.Browse().Length, Is.EqualTo(1)); Assert.That(other.Browse(), Is.Empty);
                end(); retired(); Assert.That(Runtime.Config.ShowModeMenu, Is.True);
            }
        }

        private IEnumerator JoinLeaveTestHost(EntryUiHost host)
        {
            EntryHud.ShowEntryAddress(host.Runtime.ListenEndpoint);
            Pointer(EntryButton("加入合作房间"));
            yield return WaitEntryHost(host, () => Runtime?.Config.Role == RuntimeInstanceRole.Client && Client?.Stage == ProductClientStage.Joined &&
                ((UIGridKitchen)Entry?.Hud)?.Available == true && host.Client.Stage == ProductClientStage.Joined, "Ordinary Client completes real KCP Join");
            ownedInit = Init.Instance;
        }

        [UnityTest, Category("EntryLeave")]
        public IEnumerator EntryLeaveOrdinaryKcpClientDropsHeldItemWithoutStoppingHost()
        {
            yield return StartModeLauncher();
            using (var host = new EntryUiHost("127.0.0.1:0"))
            {
                yield return JoinLeaveTestHost(host);
                var start = host.Client.CallProduct(host.Client.CreateRoomCommand(ProductRoomAction.StartRestaurant));
                yield return WaitEntryHost(host, () => start.Status != UniTaskStatus.Pending, "Actual Host starts original restaurant");
                using (var response = (ProductRoomControlResponse)start.GetAwaiter().GetResult()) { Assert.That(response.Error, Is.Zero); }
                yield return WaitEntryHost(host, () => Presentation?.Ready == true, "Client installs actual world");
                long command = Client.NextCommandId + 1; Pointer(Button("拿 碗台 ·", true));
                yield return WaitEntryHost(host, () => Grid.LastCommandId == command && Grid.LastStatus == GridResultStatus.Executed, "Real KCP pickup");
                long bowl = Grid.Actors[Client.MemberId].HandItemId; int member = Client.MemberId;
                GridKitchenComponent kitchen = ((Match)((HostSessionComponent)host.Runtime.Authority).Match).ProductKitchen();
                var originalRuntime = Runtime;
                Pointer(Button("离开房间"));
                yield return WaitEntryHost(host, () => Entry.LeaveAction == ProductLeaveAction.ClientLeave && ((UIGridKitchen)Entry.Hud)?.Available == true, "Client reads personal exit scope");
                Assert.That(EntryHud.SettlementText.text, Does.Contain("只退出本人").And.Contain("不在此代领"));
                Pointer(EntryProgressButton("取消离开"));
                yield return WaitEntryHost(host, () => Presentation.Ready, "Client cancel returns original map");
                Assert.That(Grid.Actors[member].HandItemId, Is.EqualTo(bowl));
                Pointer(Button("离开房间"));
                yield return WaitEntryHost(host, () => ((UIGridKitchen)Entry.Hud)?.Available == true, "Client confirmation reopens");
                Pointer(EntryProgressButton("确认离开房间"));
                yield return WaitEntryHost(host, () => Runtime != originalRuntime && Runtime?.Config.ShowModeMenu == true && ((UIGridKitchen)Entry?.Hud)?.Available == true, "Committed real Leave returns ordinary mode menu");
                ownedInit = Init.Instance;
                yield return WaitEntryHost(host, () => !kitchen.Actors.ContainsKey(member), "Host applies original member removal");
                Assert.That(Entry.Failure, Does.Contain("已获Host确认"));
                Assert.That(kitchen.Item(bowl).Location, Is.EqualTo(GridItemLocation.Ground));
                Assert.That(((ProcessInnerSender)host.Runtime.Sender).IsStopped, Is.False); Assert.That(host.Client.Stage, Is.EqualTo(ProductClientStage.Joined));

            }
        }

        [UnityTest, Category("EntryLeave")]
        public IEnumerator EntryLeaveOrdinaryKcpClientReleasesFrontClaimThroughOriginalLeave()
        {
            yield return StartModeLauncher();
            using (var host = new EntryUiHost("127.0.0.1:0"))
            {
                yield return JoinLeaveTestHost(host);
                var start = host.Client.CallProduct(host.Client.CreateRoomCommand(ProductRoomAction.StartRestaurant));
                yield return WaitEntryHost(host, () => start.Status != UniTaskStatus.Pending, "Actual Host starts a restaurant for the worker leave");
                using (var response = (ProductRoomControlResponse)start.GetAwaiter().GetResult()) { Assert.That(response.Error, Is.Zero); }
                yield return WaitEntryHost(host, () => Presentation?.Ready == true, "Client original map ready for front work");
                GridKitchenComponent kitchen = ((Match)((HostSessionComponent)host.Runtime.Authority).Match).ProductKitchen();
                long command; int member; ProductRuntimeComponent originalRuntime;
                Level ownedLevel = ((RestaurantRuntime)((Match)((HostSessionComponent)host.Runtime.Authority).Match).Restaurant).CurrentLevel;
                // Delay only the owned fixture's companion scheduling; the player still claims real customer work through KCP.
                foreach (CompanionRole role in ownedLevel.Children.Values.OfType<CompanionRole>())
                {
                    role.NextScheduleTick = kitchen.Tick + 1000;
                }
                var hostGrid = host.Client.GetComponent<GridClientComponent>();
                yield return WaitEntryHost(host, () => hostGrid.BuffCandidates.Count > 0, "Host original buff candidates");
                var buff = host.Client.CallProduct(hostGrid.CreateBuffChoice(hostGrid.BuffCandidates[0], hostGrid.BuffDrawVersion));
                yield return WaitEntryHost(host, () => buff.Status != UniTaskStatus.Pending, "Original Host buff request");
                using (var response = (ChooseBuffResponse)buff.GetAwaiter().GetResult()) { Assert.That(response.Error, Is.Zero); }
                var open = host.Client.CallProduct(hostGrid.CreateLevelCommand(LevelControlAction.Start));
                yield return WaitEntryHost(host, () => open.Status != UniTaskStatus.Pending, "Original Host opens the level");
                using (var response = (LevelCommandResponse)open.GetAwaiter().GetResult()) { Assert.That(response.Error, Is.Zero); }
                yield return WaitEntryHost(host, () => Grid.FrontWorks.Values.Any(w => w.Kind == FrontWorkKind.Ask && w.State == FrontWorkState.Available), "Actual available customer work before delayed fixture companion scheduling");
                var work = Grid.FrontWorks.Values.First(w => w.Kind == FrontWorkKind.Ask && w.State == FrontWorkState.Available);
                command = Client.NextCommandId + 1;
                Pointer(Button("询问 " + Grid.Stations[work.StationId].Name));
                yield return WaitEntryHost(host, () => Grid.LastCommandId == command && (Grid.LastStatus == GridResultStatus.Rejected ||
                    Grid.Actors[Client.MemberId].FrontWorkId == work.Id), "Client receives the real front work outcome");
                Assert.That(Grid.Actors[Client.MemberId].FrontWorkId, Is.EqualTo(work.Id), "Front claim result: " + Grid.LastStatus + "/" + Grid.LastReason);
                member = Client.MemberId; originalRuntime = Runtime;
                Pointer(Button("离开房间"));
                yield return WaitEntryHost(host, () => ((UIGridKitchen)Entry.Hud)?.Available == true, "Claim owner confirms leaving");
                Pointer(EntryProgressButton("确认离开房间"));
                EntityRef<ClientSessionComponent> leaving = Client;
                yield return WaitEntryHost(host, () => !kitchen.Actors.ContainsKey(member) &&
                    ((ClientSessionComponent)leaving == null || ((ClientSessionComponent)leaving).Stage == ProductClientStage.Stopped), "Claim owner removal and original Leave response finish");
                Level level = ((RestaurantRuntime)((Match)((HostSessionComponent)host.Runtime.Authority).Match).Restaurant).CurrentLevel;
                var authoritativeWork = level.GetChild<FrontWork>(work.Id);
                Assert.That(authoritativeWork.WorkerKind == FrontWorkerKind.Player && authoritativeWork.WorkerId == member, Is.False, "Original Leave releases the player's claim");
                Assert.That(((ProcessInnerSender)host.Runtime.Sender).IsStopped, Is.False);
                Assert.That(host.Client.Stage, Is.EqualTo(ProductClientStage.Joined));
                // The separate Fiber shares this Editor's Init parent scope; retire the owned fixture before the local reinitialization's next update.
                host.Dispose();
                yield return Wait(() => Runtime != originalRuntime && Runtime?.Config.ShowModeMenu == true && ((UIGridKitchen)Entry?.Hud)?.Available == true, "Committed worker Leave reaches the ordinary mode menu");
                ownedInit = Init.Instance;
                Assert.That(Entry.Failure, Does.Contain("已获Host确认"));
            }
        }

        [UnityTest, Category("EntryLeave")]
        public IEnumerator EntryLeaveRejectedByRealHostReturnsModesWithUnconfirmedFeedback()
        {
            yield return StartModeLauncher();
            using (var host = new EntryUiHost("127.0.0.1:0"))
            {
                yield return JoinLeaveTestHost(host);
                var original = Runtime;
                Pointer(EntryButton("返回模式选择"));
                Assert.That(Entry.LeaveAction, Is.EqualTo(ProductLeaveAction.ClientLeave));
                HostSessionComponent authority = host.Runtime.Authority;
                HostPeer peer = authority.Children.Values.Cast<HostPeer>().Single(p => p.MemberId == Client.MemberId);
                peer.CloseEntry(); // Real owned peer rejects the queued Leave through its original request handler.
                Pointer(EntryProgressButton("确认离开房间"));
                yield return WaitEntryHost(host, () => Runtime != original && Runtime?.Config.ShowModeMenu == true && ((UIGridKitchen)Entry?.Hud)?.Available == true, "Rejected Leave still ends only the local connection", 20);
                ownedInit = Init.Instance;
                Assert.That(Entry.Failure, Does.Contain("未获Host确认Leave"));
                Assert.That(((ProcessInnerSender)host.Runtime.Sender).IsStopped, Is.False);
                Assert.That(host.Client.Stage, Is.EqualTo(ProductClientStage.Joined));
            }
        }
    }
}
#endif
