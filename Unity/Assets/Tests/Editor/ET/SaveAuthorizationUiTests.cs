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
        private IEnumerator ChooseUiReplacement(string ticket, string target)
        {
            if (Entry.ReplaceTicketId == null)
            {
                Pointer(EntryProgressButton("选择有损替换 第 1 关 · 0 分 · " + ticket.Substring(0, 6)));
                yield return Wait(() => !Entry.Busy && Entry.ProgressHeaders.Length > 0, "Target browser reads headers without a world");
            }
            Pointer(EntryProgressButton("替换本机记录 " + target.Substring(0, 8), true));
            yield return Wait(() => !Entry.Busy && Entry.ReplacePreview != null, "Root verifies current target before showing destructive confirmation");
        }

        private static string UiAuthorizedTicket(SoloSaveRepository repository, string sourceSlot)
        {
            var own = OfflineProfiles.OwnIdentity(repository);
            string match = Guid.NewGuid().ToString("N"), id = Guid.NewGuid().ToString("N");
            byte[] authority = OfflineProfiles.Sign(repository, OfflineProfiles.MatchAuthority, MemoryPackHelper.Serialize(new ProgressAuthorityValue
            {
                AuthorityId = Guid.NewGuid().ToString("N"), MatchId = match, WorldId = Guid.NewGuid().ToString("N"),
                HostProfileId = own.ProfileId, HostKeyId = own.KeyId,
                BaselineAuthorization = CooperativeBaselineSystem.Authorize(repository, sourceSlot, match, own)
            }));
            var verified = ProgressClaimCodec.Decode<ProgressAuthorityValue>(OfflineProfiles.Verify(authority, OfflineProfiles.MatchAuthority).Body);
            byte[] success = OfflineProfiles.Sign(repository, OfflineProfiles.SuccessResult, MemoryPackHelper.Serialize(new CooperativeSuccessValue
            {
                SaveResultId = Guid.NewGuid().ToString("N"), MatchId = match, WorldId = verified.WorldId, CompletedLevelId = 1,
                NextLevelId = Tables.Instance.DTLevelConfig.Get(1).NextLevelConfigId, CompletedUtcTicks = DateTime.UtcNow.Ticks,
                Payload = MemoryPackHelper.Serialize(SelectionFixture()), Authority = authority
            }));
            ProgressClaimFiles.PersistTicket(repository, OfflineProfiles.Sign(repository, OfflineProfiles.ClaimTicket, MemoryPackHelper.Serialize(new ProgressTicketValue
            {
                TicketId = id, EligibleProfileId = own.ProfileId, EligibleKeyId = own.KeyId, Success = success
            })), match);
            return id;
        }

        [UnityTest]
        [Category("SaveAuthorizationUi")]
        [Category("EntryAuthorization")]
        [Category("AuthorizationResultViews")]
        public IEnumerator SaveAuthorizationUiImportCancelsWithoutWriteThenRunsIndependentBranchThroughOrdinarySoloEntry()
        {
            yield return StartModeLauncher();
            var repository = CreateSaveUiRepository(Runtime, out string directory, out string profile);
            var ownerScene = EntitySceneFactory.CreateScene(Runtime.Root(), IdGenerater.Instance.GenerateId(), IdGenerater.Instance.GenerateInstanceId(),
                SceneType.ProductRoot, "OwnedAuthorizationSource");
            try
            {
                string ownerProfile = Guid.NewGuid().ToString("N");
                var owner = ownerScene.AddComponent<SoloSaveRepository, string, string>(directory, ownerProfile);
                var source = SelectionHeader(ownerProfile); owner.Commit(source, SelectionFixture());
                string sourceFile = Path.Combine(owner.DirectoryPath, source.SlotId + ".ccsave");
                byte[] sourceBytes = File.ReadAllBytes(sourceFile);
                var signed = SaveAuthorizationFiles.ReadImport(SaveAuthorizationFiles.ExportImport(owner, source.SlotId));
                string input = OfflineProfiles.Export(owner, signed.Grant.GrantId + ".ccbranch", SaveAuthorizationFiles.ExportImport(owner, source.SlotId));
                Pointer(EntryButton("存档与授权"));
                yield return Wait(() => Entry.ProgressVisible && ((UIGridKitchen)Entry.Hud)?.Available == true, "Offline managed authorization page opens");
                string[] beforeNavigation = Directory.GetFiles(repository.DirectoryPath, "*", SearchOption.AllDirectories);
                Pointer(EntryProgressButton("导入独立进度"));
                var retiredInspect = CaptureButtonCallback(EntryProgressButton("查看导入文件"));
                Pointer(EntryProgressButton("返回任务选择"));
                Pointer(EntryProgressButton("导出给朋友"));
                retiredInspect();
                Assert.That(Entry.InspectedIdentity, Is.Null);
                Assert.That(Directory.GetFiles(repository.DirectoryPath, "*", SearchOption.AllDirectories), Is.EquivalentTo(beforeNavigation));
                Pointer(EntryProgressButton("返回任务选择"));
                Pointer(EntryProgressButton("导入独立进度"));
                EntryHud.ShowEntryAddress(input, 2048);
                Pointer(EntryProgressButton("查看导入文件"));
                yield return Wait(() => !Entry.Busy && Entry.InspectedAuthorizationKind == SaveAuthorizationKind.ImportBranch, "Import preview verifies source signature before trusting it");
                Assert.That(Entry.InspectedSummary, Does.Contain(source.SlotId));
                Assert.That(Entry.InspectedSummary, Does.Contain(ownerProfile));
                Pointer(EntryProgressButton("展开身份明细"));
                Assert.That(EntryHud.DetailsText.text, Does.Contain(ownerProfile).And.Not.Contain("<D>").And.Not.Contain("<RSAKeyValue>"));
                Pointer(EntryProgressButton("收起身份明细"));
                EntryHud.ShowEntryAddress(input + ".changed", 2048);
                Pointer(EntryProgressButton("确认信任此身份"));
                Assert.That(Entry.InspectedTrusted, Is.False); Assert.That(Entry.Failure, Does.Contain("路径已变化"));
                EntryHud.ShowEntryAddress(input, 2048);
                Pointer(EntryProgressButton("确认信任此身份"));
                yield return Wait(() => !Entry.Busy && Entry.InspectedTrusted, "Player explicitly pins source key");
                Pointer(EntryProgressButton("导入为独立分支"));
                var oldConfirm = CaptureButtonCallback(EntryProgressButton("确认导入独立分支"));
                Pointer(EntryProgressButton("取消导入"));
                oldConfirm();
                Assert.That(repository.Browse(), Is.Empty);
                Assert.That(File.ReadAllBytes(sourceFile), Is.EqualTo(sourceBytes));
                Pointer(EntryProgressButton("导入为独立分支"));
                Pointer(EntryProgressButton("确认导入独立分支"));
                Assert.That(Entry.ProgressCommitting, Is.True);
                Assert.That(EntryHud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "取消读取"), Is.False);
                yield return Wait(() => !Entry.Busy && Entry.ProgressHeaders.Length == 1, "Actual Root publishes imported branch and receipt");
                var branch = repository.Browse().Single();
                Assert.That(EntryHud.SettlementText.text, Does.Contain("独立分支已导入").And.Contain(branch.SlotId).And.Contain("没有自动开局"));
                Assert.That(branch.ProfileId, Is.EqualTo(profile));
                Assert.That(branch.SlotId, Is.Not.EqualTo(source.SlotId));
                Assert.That(branch.SourceProfileId, Is.EqualTo(ownerProfile));
                Assert.That(branch.RootSlotId, Is.EqualTo(source.SlotId));
                ownerScene.Dispose(); ownerScene = null;
                Pointer(EntryProgressButton("查看导入记录"));
                Assert.That(EntryHud.SettlementText.text, Does.Contain("本机记录选择").And.Contain(branch.SlotId).And.Contain("当前修订：1"));
                Assert.That((Scene)Runtime.HostScene, Is.Null, "Inspecting the new record does not start a world");
                Pointer(EntryProgressButton("返回领取列表"));
                Pointer(EntryProgressButton("身份与授权文件"));
                Pointer(EntryProgressButton("导出给朋友"));
                Pointer(EntryProgressButton("本机成功记录"));
                yield return Wait(() => !Entry.Busy && Entry.ProgressHeaders.Length == 1, "Task export reads actual independently imported record");
                Pointer(EntryProgressButton("导出分支许可 记录 ", true));
                yield return Wait(() => !Entry.Busy && Entry.ProgressFeedback.StartsWith("已导出："), "Imported owner can independently authorize another branch");
                Assert.That(File.Exists(Entry.ProgressPath), Is.True);
                Assert.That(SaveAuthorizationFiles.Inspect(File.ReadAllBytes(Entry.ProgressPath)).Kind, Is.EqualTo(SaveAuthorizationKind.ImportBranch));
                Pointer(EntryProgressButton("导出身份文件"));
                yield return Wait(() => !Entry.Busy && Entry.ProgressPath.EndsWith(".ccidentity"), "Public identity export remains explicit");
                string ownIdentityPath = Entry.ProgressPath;
                Pointer(EntryProgressButton("导出替换许可"));
                EntryHud.ShowEntryAddress(ownIdentityPath, 2048);
                Pointer(EntryProgressButton("查看接收者身份"));
                yield return Wait(() => !Entry.Busy && Entry.InspectedIdentity?.ProfileId == profile, "Chosen replacement recipient public identity inspected");
                Pointer(EntryProgressButton("确认信任此身份"));
                yield return Wait(() => !Entry.Busy && Entry.InspectedTrusted, "Replacement recipient trust separate from export");
                Pointer(EntryProgressButton("导出替换许可 记录 ", true));
                yield return Wait(() => !Entry.Busy && Entry.ProgressPath.EndsWith(".ccreplace"), "Selected replacement export uses original signer and scope");
                Assert.That(SaveAuthorizationFiles.Inspect(File.ReadAllBytes(Entry.ProgressPath)).Kind, Is.EqualTo(SaveAuthorizationKind.Replace));
                Pointer(EntryProgressButton("返回任务选择"));
                Pointer(EntryProgressButton("返回领取列表"));
                yield return Wait(() => !Entry.Busy, "Original receipt list settles before closing");
                Pointer(EntryProgressButton("返回经营或房间"));
                yield return Wait(() => !Entry.ClaimsVisible && ((UIGridKitchen)Entry.Hud)?.Available == true, "Return to ordinary mode menu");
                Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection?.Headers.Length == 1 && ((UIGridKitchen)Selection.Hud)?.Available == true, "Ordinary Solo selector discovers the imported file");
                yield return InspectAndContinueSave(branch.SlotId);
                yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Source owner is offline while independent branch starts actual next-level world");
                Assert.That(Grid.LevelConfigId, Is.EqualTo(branch.NextLevelId));
                yield return Command("拿 碗台 ·", true);
                Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.Not.Zero, "Imported branch accepts real gameplay input");
                oldConfirm();
                Assert.That(repository.Browse().Length, Is.EqualTo(1));
                Assert.That(File.ReadAllBytes(sourceFile), Is.EqualTo(sourceBytes));
            }
            finally { ownerScene?.Dispose(); }
        }

        [UnityTest]
        [Category("SaveAuthorizationUi")]
        [Category("EntryAuthorization")]
        [Category("AuthorizationFlowEdges")]
        [Category("AuthorizationResultViews")]
        public IEnumerator SaveAuthorizationUiReplacementCancelsRejectsStaleTargetRetriesPublicationAndRetiresOldConfirmation()
        {
            yield return StartModeLauncher();
            var repository = CreateSaveUiRepository(Runtime, out _, out string profile);
            var source = SelectionHeader(profile); repository.Commit(source, SelectionFixture());
            var target = SelectionHeader(profile); repository.Commit(target, SelectionFixture());
            string sourceFile = Path.Combine(repository.DirectoryPath, source.SlotId + ".ccsave");
            string targetFile = Path.Combine(repository.DirectoryPath, target.SlotId + ".ccsave");
            byte[] sourceBytes = File.ReadAllBytes(sourceFile), original = File.ReadAllBytes(targetFile);
            string ticket = UiAuthorizedTicket(repository, source.SlotId);
            Pointer(EntryButton("存档与授权"));
            Pointer(EntryProgressButton("替换我的记录"));
            Pointer(EntryProgressButton("选择合作结果"));
            yield return Wait(() => !Entry.Busy && Entry.ClaimRows.Length == 1, "Ordinary offline claim page finds persisted eligibility");
            Pointer(EntryProgressButton("选择有损替换 第 1 关 · 0 分 · " + ticket.Substring(0, 6)));
            var cancelledRead = CaptureButtonCallback(EntryProgressButton("取消读取"));
            Pointer(EntryProgressButton("取消读取"));
            yield return null; yield return null;
            Assert.That(Entry.ReplacePreview, Is.Null); Assert.That(Entry.ProgressHeaders, Is.Empty);
            Assert.That(File.ReadAllBytes(targetFile), Is.EqualTo(original));
            Pointer(EntryProgressButton("取消替换"));
            yield return Wait(() => !Entry.Busy && !Entry.ClaimsNeedsRefresh, "Cancelled readonly target browse returns to current durable results");
            cancelledRead(); Assert.That(File.ReadAllBytes(targetFile), Is.EqualTo(original));
            yield return ChooseUiReplacement(ticket, target.SlotId);
            Assert.That(EntryHud.SettlementText.text, Does.Contain(profile).And.Contain(target.SlotId).And.Contain("预期修订：1").And.Contain("不能恢复"));
            var oldConfirm = CaptureButtonCallback(EntryProgressButton("确认有损替换"));
            Pointer(EntryProgressButton("取消替换"));
            oldConfirm();
            Assert.That(File.ReadAllBytes(targetFile), Is.EqualTo(original));
            yield return Wait(() => !Entry.Busy && !Entry.ClaimsNeedsRefresh, "Canceled target chooser returns to actual receipt query");
            yield return ChooseUiReplacement(ticket, target.SlotId);
            string otherTicket = UiAuthorizedTicket(repository, source.SlotId);
            byte[] grant = SaveAuthorizationFiles.ExportReplace(repository, target.SlotId, OfflineProfiles.OwnIdentity(repository), allowedSource: source);
            var preview = SaveAuthorizationFiles.PreviewReplace(repository, otherTicket, grant);
            SaveAuthorizationFiles.Replace(repository, otherTicket, grant, preview);
            byte[] changed = File.ReadAllBytes(targetFile);
            Pointer(EntryProgressButton("确认有损替换"));
            yield return Wait(() => !Entry.Busy && Entry.Failure != null, "Physical target revision changed since confirmation");
            Assert.That(Entry.Failure, Does.Contain("目标已变化"));
            Assert.That(Entry.ReplacePreview, Is.Null);
            Assert.That(File.ReadAllBytes(targetFile), Is.EqualTo(changed));
            Assert.That(ProgressClaimFiles.Browse(repository).Single(row => row.TicketId == ticket).State, Is.EqualTo(ProgressReceiptState.Available));
            yield return ChooseUiReplacement(ticket, target.SlotId);
            Assert.That(Entry.ReplacePreview.Revision, Is.EqualTo(2));
            using (var locked = new FileStream(targetFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Pointer(EntryProgressButton("确认有损替换"));
                Assert.That(Entry.ProgressCommitting, Is.True);
                Assert.That(EntryHud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "取消替换" ||
                    button.GetComponentInChildren<Text>().text == "取消读取"), Is.False);
                yield return Wait(() => !Entry.Busy && Entry.Failure != null, "Real Windows publication failure keeps target and permits retry");
                Assert.That(File.ReadAllBytes(targetFile), Is.EqualTo(changed));
            }
            yield return ChooseUiReplacement(ticket, target.SlotId);
            var retired = CaptureButtonCallback(EntryProgressButton("确认有损替换"));
            Pointer(EntryProgressButton("确认有损替换"));
            yield return Wait(() => !Entry.Busy && Entry.ReplaceTicketId == null && !Entry.ClaimsNeedsRefresh, "Explicit fresh confirmation atomically publishes replacement and receipt");
            repository.Select(target.SlotId, out var replaced);
            Assert.That(replaced.ProfileId, Is.EqualTo(profile));
            Assert.That(replaced.Revision, Is.EqualTo(3));
            Assert.That(replaced.Receipts.Length, Is.EqualTo(2));
            Assert.That(EntryHud.SettlementText.text, Does.Contain("有损替换已提交").And.Contain(target.SlotId).And.Contain("当前修订：3"));
            Assert.That(File.ReadAllBytes(sourceFile), Is.EqualTo(sourceBytes));
            byte[] committed = File.ReadAllBytes(targetFile);
            retired(); oldConfirm();
            Pointer(EntryProgressButton("返回经营或房间"));
            retired();
            Assert.That(File.ReadAllBytes(targetFile), Is.EqualTo(committed));
            Assert.That(Directory.EnumerateFiles(repository.DirectoryPath, "*.bak"), Is.Empty);
            Assert.That(Directory.EnumerateFiles(repository.DirectoryPath, "*.tmp"), Is.Empty);
            Assert.That(repository.Browse().Length, Is.EqualTo(2));
        }
    }
}
#endif
