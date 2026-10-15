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
    [FriendOf(typeof(ProductEntryPresentation))]
    [FriendOf(typeof(ProductRoomClient))]
    public sealed partial class SoloRestaurantUiTests
    {
        private Button EntryProgressButton(string caption, bool prefix = false) => EntryHud.GetComponentsInChildren<Button>(false)
            .Single(button => prefix ? button.GetComponentInChildren<Text>().text.StartsWith(caption) : button.GetComponentInChildren<Text>().text == caption);

        private IEnumerator StartProgressHostRoom()
        {
            yield return StartModeLauncher();
            Pointer(EntryButton("创建合作房间"));
            yield return Wait(() => Runtime?.Config.Role == RuntimeInstanceRole.Host && Client?.Stage == ProductClientStage.Joined &&
                Entry != null && ((UIGridKitchen)Entry.Hud)?.Available == true, "Ordinary room before selecting a baseline");
            ownedInit = Init.Instance;
        }

        private IEnumerator VerifyProgressHostIdentity(string profile)
        {
            Pointer(EntryProgressButton("存档与身份"));
            Pointer(EntryProgressButton("验证本机身份"));
            yield return Wait(() => !Entry.Busy && Client.GetComponent<ProductRoomClient>().Current.VerifiedProfileId == profile,
                "Ordinary UI proves repository private-key possession", 30);
            Assert.That(Entry.Failure, Is.Null);
        }

        [UnityTest]
        [Category("CooperativeBaselineUi")]
        public IEnumerator CooperativeBaselineUiOrdinaryBrowseCancelLoadExportAndKcpWorkKeepOriginalFile()
        {
            yield return StartProgressHostRoom();
            var runtime = Runtime;
            var repository = CreateSaveUiRepository(runtime, out string directory, out string profile);
            var header = SelectionHeader(profile);
            repository.Commit(header, SelectionFixture());
            string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
            byte[] original = File.ReadAllBytes(path);
            yield return VerifyProgressHostIdentity(profile);
            var match = (Match)((HostSessionComponent)Runtime.Authority).Match;
            Pointer(EntryProgressButton("导出身份文件"));
            yield return Wait(() => !Entry.Busy && Entry.ProgressFeedback?.StartsWith("已导出：") == true, "Public identity export through repository mailbox");
            string identityPath = EntryHud.EntryAddress;
            Assert.That(File.Exists(identityPath), Is.True);
            Assert.That(OfflineProfiles.DecodeIdentity(File.ReadAllBytes(identityPath)).PublicKeyXml, Does.Not.Contain("<D>"));
            var browse = EntryProgressButton("本机成功记录");
            var retiredBrowse = CaptureButtonCallback(browse);
            Pointer(browse);
            Pointer(EntryProgressButton("取消读取"));
            for (int i = 0; i < 12; ++i)
            {
                yield return null;
            }
            Assert.That(Entry.ProgressHeaders, Is.Empty);
            Assert.That((RestaurantRuntime)match.Restaurant, Is.Null);
            long serial = Entry.ProgressSerial;
            retiredBrowse();
            Assert.That(Entry.ProgressSerial, Is.EqualTo(serial));
            Pointer(EntryProgressButton("本机成功记录"));
            yield return Wait(() => !Entry.Busy && Entry.ProgressHeaders.Length == 1, "Metadata browse creates no kitchen");
            Assert.That((RestaurantRuntime)match.Restaurant, Is.Null);
            Pointer(EntryProgressButton("授权导出 记录 ", true));
            yield return Wait(() => !Entry.Busy && EntryHud.EntryAddress.EndsWith(".ccrun"), "Explicit read/run and match-result delegation export");
            string grantPath = EntryHud.EntryAddress;
            var signed = OfflineProfiles.Verify(File.ReadAllBytes(grantPath), OfflineProfiles.BaselineReadRun, repository);
            Assert.That(CooperativeBaselineSystem.ReadValue(signed, out _, out _).MatchId, Is.EqualTo(Client.MatchId));
            var read = EntryProgressButton("读取 记录 ", true);
            var retiredRead = CaptureButtonCallback(read);
            Pointer(read);
            Pointer(EntryProgressButton("取消读取"));
            for (int i = 0; i < 12; ++i)
            {
                yield return null;
            }
            Assert.That((RestaurantRuntime)match.Restaurant, Is.Null, "Late file response cannot start a world after cancellation");
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            Pointer(EntryProgressButton("读取 记录 ", true));
            yield return Wait(() => Presentation?.Ready == true, "Ordinary selected baseline starts exactly one real kitchen", 30);
            Assert.That(Grid.LevelConfigId, Is.EqualTo(header.NextLevelId));
            Assert.That(Grid.Items.Count, Is.EqualTo(SelectionFixture().Items.Length));
            Assert.That(Hud.HintText.text, Does.Contain(profile.Substring(0, 8)).And.Contain("只读运行"));
            long command = Client.NextCommandId;
            string world = Grid.WorldId;
            retiredRead();
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.WorldId, Is.EqualTo(world));
            yield return Command("拿 碗台 ·", true);
            Presentation.CloseGridHud();
            yield return null;
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "KCP Client continues restored inventory through its managed view");
                Assert.That(remote.Grid.WorldId, Is.EqualTo(world));
                Assert.That(remote.Client.GetComponent<ProductRoomClient>().Current.BaselineOwnerId, Is.EqualTo(profile));
                Assert.That(remote.Hud.HintText.text, Does.Contain("只读运行"));
                Button RemoteButton(string caption) => remote.Hud.GetComponentsInChildren<Button>(false)
                    .Single(button => button.GetComponentInChildren<Text>().text.StartsWith(caption));
                long remoteCommand = remote.Client.NextCommandId + 1;
                Pointer(RemoteButton("拿 备料台 ·"));
                yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == remoteCommand && remote.Grid.LastStatus == GridResultStatus.Executed,
                    "Remote picks a different original item");
                long tomato = remote.Grid.Actors[remote.Client.MemberId].HandItemId;
                remoteCommand = remote.Client.NextCommandId + 1;
                Pointer(RemoteButton("放 切配台 ·"));
                yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == remoteCommand && remote.Grid.LastStatus == GridResultStatus.Executed,
                    "Remote places the restored tomato");
                remoteCommand = remote.Client.NextCommandId + 1;
                Pointer(RemoteButton("切配 切配台 ·"));
                yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == remoteCommand && remote.Grid.LastStatus == GridResultStatus.Executed,
                    "Restored station starts real work");
                long chopping = remote.Grid.Stations.Values.Single(station => station.Name == "切配台").Id;
                yield return WaitCoopUi(remote, () => remote.Grid.Items.TryGetValue(remote.Grid.Stations[chopping].ItemId, out GridItemProjection item) && item.DefinitionId == GridKitchenRules.TomatoChunks,
                    "Original restored item completes preparation");
                Assert.That(remote.Grid.Items.ContainsKey(tomato), Is.False);
                Assert.That(Grid.Items.Count, Is.EqualTo(SelectionFixture().Items.Length));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
                Assert.That(repository.Browse().Length, Is.EqualTo(1));
            }
        }

        [UnityTest]
        [Category("CooperativeBaselineUi")]
        public IEnumerator CooperativeBaselineUiForeignFileTrustRejectsWrongMatchThenLoadsWithoutTakingOwnership()
        {
            yield return StartProgressHostRoom();
            var repository = CreateSaveUiRepository(Runtime, out string directory, out string profile);
            yield return VerifyProgressHostIdentity(profile);
            var hostIdentity = OfflineProfiles.OwnIdentity(repository);
            var scene = EntitySceneFactory.CreateScene(Runtime.Root(), IdGenerater.Instance.GenerateId(), IdGenerater.Instance.GenerateInstanceId(),
                SceneType.ProductRoot, "OwnedForeignFileFixture");
            string ownerProfile = Guid.NewGuid().ToString("N");
            var owner = scene.AddComponent<SoloSaveRepository, string, string>(directory, ownerProfile);
            try
            {
                var header = SelectionHeader(ownerProfile);
                owner.Commit(header, SelectionFixture());
                OfflineProfiles.OwnIdentity(owner);
                OfflineProfiles.TrustIdentity(owner, hostIdentity);
                byte[] wrong = CooperativeBaselineSystem.Authorize(owner, header.SlotId, Guid.NewGuid().ToString("N"), hostIdentity);
                byte[] valid = CooperativeBaselineSystem.Authorize(owner, header.SlotId, Client.MatchId, hostIdentity);
                string wrongPath = OfflineProfiles.Export(owner, "wrong-owned-test.ccrun", wrong);
                string validPath = OfflineProfiles.Export(owner, "valid-owned-test.ccrun", valid);
                string source = Path.Combine(owner.DirectoryPath, header.SlotId + ".ccsave");
                byte[] original = File.ReadAllBytes(source);
                var match = (Match)((HostSessionComponent)Runtime.Authority).Match;
                foreach (var input in new[] { wrongPath, validPath })
                {
                    EntryHud.GetComponentInChildren<InputField>(false).text = input;
                    Pointer(EntryProgressButton("查看导入文件"));
                    yield return Wait(() => !Entry.Busy && Entry.InspectedIdentity?.ProfileId == ownerProfile, "Inspect actual external authorization without trusting it yet");
                    Assert.That(EntryHud.SettlementText.text, Does.Contain(ownerProfile).And.Contain(input).And.Contain("目标房间"));
                    Assert.That((RestaurantRuntime)match.Restaurant, Is.Null);
                    Pointer(EntryProgressButton("确认信任此身份"));
                    yield return Wait(() => !Entry.Busy && Entry.InspectedTrusted, "Explicit identity pinning preserves existing keys");
                    Pointer(EntryProgressButton("载入此授权餐厅"));
                    if (input == wrongPath)
                    {
                        yield return Wait(() => !Entry.Busy && Entry.Failure != null, "Valid signature with wrong match is rejected");
                        Assert.That(Entry.Failure, Does.Contain("不适用于当前房主或房间"));
                        Assert.That((RestaurantRuntime)match.Restaurant, Is.Null);
                    }
                }
                yield return Wait(() => Presentation?.Ready == true, "Trusted foreign file creates a new running world");
                Assert.That(Client.GetComponent<ProductRoomClient>().Current.BaselineOwnerId, Is.EqualTo(ownerProfile));
                Assert.That(Client.GetComponent<ProductRoomClient>().Current.BaselineOwnerId, Is.Not.EqualTo(profile));
                Assert.That(Hud.HintText.text, Does.Contain(ownerProfile.Substring(0, 8)).And.Contain("只读运行"));
                Assert.That(File.ReadAllBytes(source), Is.EqualTo(original));
                Assert.That(repository.Browse(), Is.Empty);
                Assert.That(owner.Browse().Single().Revision, Is.EqualTo(1));
            }
            finally { owner.Dispose(); scene.Dispose(); }
        }
    }
}
#endif
