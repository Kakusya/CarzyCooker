using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    public static partial class ProductEntryPresentationSystem
    {
        public static void OpenAuthorizationFiles(this ProductEntryPresentation self)
        {
            self.OpenClaims();
            self.NavigateAuthorizationTask(AuthorizationViewTask.Home);
        }

        private static void RenderImportConfirmation(this ProductEntryPresentation self, MonoUIGridKitchen view)
        {
            view.SettlementText.text = ReadableAuthorizationSummary(self.InspectedSummary) + "\n确认后新建归本机身份的独立记录，源档保留；原档主不能远程撤回已导入的分支。";
            self.AddProgressControl(view, "确认导入独立分支", current => current.CommitInspectedImport());
            self.AddProgressControl(view, "取消导入", current =>
            {
                current.ConfirmImport = false;
                current.ProgressFeedback = "已取消，未创建分支";
                current.RenderProductEntry();
            });
        }

        private static void CommitInspectedImport(this ProductEntryPresentation self)
        {
            if (!self.ConfirmImport || !self.InspectedTrusted || self.InspectedAuthorizationKind != SaveAuthorizationKind.ImportBranch)
            {
                return;
            }
            byte[] document = (byte[])self.InspectedAuthorization.Clone();
            self.RunProgress(async (context, current) =>
            {
                var active = current(); active.ProgressCommitting = true; active.RenderProductEntry();
                var receipt = await active.ProgressRepositoryCall(context, ProductMessages.Create<SaveAuthorizationRequest>(request =>
                {
                    request.Action = (int)SaveAuthorizationAction.ImportBranch; request.Document = document;
                }), response => ProgressClaimCodec.Decode<ProgressCommitReceipt>(((SaveAuthorizationResponse)response).Receipt));
                active = current(); active.ConfirmImport = false;
                active.ProgressFeedback = "已导入独立分支：" + receipt.SlotId;
                active.AuthorizationResultSlot = receipt.SlotId; active.AuthorizationResultProfile = receipt.ProfileId; active.AuthorizationResultRevision = receipt.Revision;
                active.ProgressHeaders = await active.ProgressRepositoryCall(context, ProductMessages.Create<SoloSaveBrowseRequest>(_ => { }),
                    response => ((SoloSaveBrowseResponse)response).Headers.Select(ProgressClaimCodec.Decode<SoloSaveHeader>).ToArray());
            });
        }

        private static void ExportProgressGrant(this ProductEntryPresentation self, SaveAuthorizationAction action, string slot)
        {
            byte[] recipient = action == SaveAuthorizationAction.ExportReplace && self.InspectedIdentity != null
                ? OfflineProfiles.EncodeIdentity(self.InspectedIdentity) : null;
            self.RunProgress(async (context, current) =>
            {
                current().ProgressCommitting = true; current().RenderProductEntry();
                string output = await current().ProgressRepositoryCall(context, ProductMessages.Create<SaveAuthorizationRequest>(request =>
                {
                    request.Action = (int)action; request.SlotId = slot; request.RecipientIdentity = recipient;
                }), response => ((SaveAuthorizationResponse)response).OutputPath);
                var active = current(); active.ProgressFeedback = "已导出：" + output; active.ProgressPath = output;
                active.AuthorizationOutputPath = output;
                ((UIGridKitchen)active.Hud).View.ShowEntryAddress(output, 2048);
            });
        }

        private static void RejectInspectedGrant(this ProductEntryPresentation self)
        {
            byte[] document = (byte[])self.InspectedAuthorization.Clone();
            self.RunProgress(async (context, current) =>
            {
                current().ProgressCommitting = true; current().RenderProductEntry();
                await current().ProgressRepositoryCall(context, ProductMessages.Create<SaveAuthorizationRequest>(request =>
                {
                    request.Action = (int)SaveAuthorizationAction.RejectUnusedGrant; request.Document = document;
                }), _ => true);
                current().ProgressFeedback = "本端已拒绝此未使用许可；已导入分支及其他设备不受影响";
            });
        }

        private static void ChooseReplaceTarget(this ProductEntryPresentation self, string ticket)
        {
            self.AuthorizationResultSlot = self.AuthorizationResultProfile = null; self.AuthorizationResultRevision = 0;
            self.ReplaceTicketId = ticket; self.ReplacePreview = null; self.ReplaceGrant = null;
            self.ProgressHeaders = Array.Empty<SoloSaveHeader>();
            self.RunReplacementOperation(async (context, current) =>
            {
                var headers = await current().ProgressRepositoryCall(context, ProductMessages.Create<SoloSaveBrowseRequest>(_ => { }),
                    response => ((SoloSaveBrowseResponse)response).Headers.Select(ProgressClaimCodec.Decode<SoloSaveHeader>).ToArray());
                current().ProgressHeaders = headers;
            });
        }

        private static void RenderReplacementEntry(this ProductEntryPresentation self, MonoUIGridKitchen view)
        {
            view.PhaseText.text = "有损替换确认";
            view.ClockText.text = "目标Owner保持不变";
            view.HintText.text = "这会替换目标完整进度，不合并，不保留可恢复旧版本";
            view.StatusText.text = self.Failure ?? self.ProgressFeedback ?? "先核对目标，再确认本次有损操作";
            view.SettlementText.text = "请选择本机拥有的目标，或已核对的外部替换许可。";
            if (self.Busy)
            {
                view.StatusText.text = self.ProgressCommitting ? "正在提交，请等待实际结果…" : "正在验证，可以取消等待";
                if (!self.ProgressCommitting)
                {
                    self.ClaimsButton(view, "取消读取", current =>
                    {
                        ++current.ProgressSerial; current.Busy = false;
                        current.ReplacePreview = null; current.ReplaceGrant = null;
                        current.ProgressFeedback = "读取已取消，请重新选择当前目标";
                        current.RenderProductEntry();
                    });
                }
                return;
            }
            if (self.ReplacePreview != null)
            {
                var preview = self.ReplacePreview;
                view.SettlementText.text = $"目标Owner：{preview.ProfileId}\nSlot：{preview.SlotId}\n预期修订：{preview.Revision}\n来源结果：{preview.ResultId}\n此次替换会丢弃目标旧进度，不能恢复旧版本。";
                self.ClaimsButton(view, "确认有损替换", current => current.CommitReplacement());
            }
            else
            {
                foreach (var header in self.ProgressHeaders)
                {
                    string slot = header.SlotId;
                    self.ClaimsButton(view, "替换本机记录 " + slot.Substring(0, 8) + " · 修订 " + header.Revision,
                        current => current.PrepareReplacement(SaveAuthorizationAction.PrepareOwnReplace, slot), true);
                }
                if (self.InspectedAuthorizationKind == SaveAuthorizationKind.Replace && self.InspectedTrusted && self.InspectedAuthorization != null)
                {
                    self.ClaimsButton(view, "查看外部许可目标", current => current.PrepareReplacement(SaveAuthorizationAction.PreviewReplace));
                }
            }
            self.ClaimsButton(view, "取消替换", current =>
            {
                current.ReplacePreview = null; current.ReplaceGrant = null; current.ReplaceTicketId = null;
                current.ProgressFeedback = "已取消替换，目标未改变";
                current.ClaimsNeedsRefresh = true;
                current.RenderProductEntry();
            });
            if (self.AuthorizationTask == AuthorizationViewTask.ReplaceRecord)
            {
                self.ClaimsButton(view, "返回任务选择", current => current.NavigateAuthorizationTask(AuthorizationViewTask.Home));
            }
        }

        private static void PrepareReplacement(this ProductEntryPresentation self, SaveAuthorizationAction action, string slot = null)
        {
            string ticket = self.ReplaceTicketId;
            byte[] document = action == SaveAuthorizationAction.PreviewReplace ? (byte[])self.InspectedAuthorization.Clone() : null;
            self.RunReplacementOperation(async (context, current) =>
            {
                var value = await current().ProgressRepositoryCall(context, ProductMessages.Create<SaveAuthorizationRequest>(request =>
                {
                    request.Action = (int)action; request.SlotId = slot; request.TicketId = ticket; request.Document = document;
                }), response =>
                {
                    var reply = (SaveAuthorizationResponse)response;
                    return (Preview: ProgressClaimCodec.Decode<ProgressReplacePreview>(reply.Preview), Grant: reply.Document == null ? document : (byte[])reply.Document.Clone());
                });
                var active = current(); active.ReplacePreview = value.Preview; active.ReplaceGrant = value.Grant;
            });
        }

        private static void CommitReplacement(this ProductEntryPresentation self)
        {
            var confirmed = self.ReplacePreview;
            if (confirmed == null || self.ReplaceGrant == null)
            {
                return;
            }
            byte[] document = (byte[])self.ReplaceGrant.Clone();
            byte[] confirmation = MemoryPackHelper.Serialize(confirmed);
            self.RunReplacementOperation(async (context, current) =>
            {
                var receipt = await current().ProgressRepositoryCall(context, ProductMessages.Create<SaveAuthorizationRequest>(request =>
                {
                    request.Action = (int)SaveAuthorizationAction.Replace; request.TicketId = confirmed.TicketId;
                    request.Document = document; request.Confirmation = confirmation;
                }), response => ProgressClaimCodec.Decode<ProgressCommitReceipt>(((SaveAuthorizationResponse)response).Receipt));
                var active = current(); active.ReplacePreview = null; active.ReplaceGrant = null; active.ReplaceTicketId = null;
                active.ProgressFeedback = $"已替换：{receipt.ProfileId}/{receipt.SlotId} · 修订 {receipt.Revision}";
                active.AuthorizationResultSlot = receipt.SlotId; active.AuthorizationResultProfile = receipt.ProfileId; active.AuthorizationResultRevision = receipt.Revision;
                active.ClaimsNeedsRefresh = true;
            }, true);
        }

        private static void RunReplacementOperation(this ProductEntryPresentation self, Func<AsyncContext, Func<ProductEntryPresentation>, UniTask> action, bool committing = false)
        {
            if (self.Busy || !self.ClaimsVisible || self.Suspended || self.ReplaceTicketId == null)
            {
                return;
            }
            self.Busy = true; self.Failure = null; self.ProgressCommitting = committing;
            long serial = ++self.ProgressSerial, epoch = self.Epoch;
            EntityRef<ProductEntryPresentation> original = self;
            string ticket = self.ReplaceTicketId;
            self.RenderProductEntry();
            ProductEntryPresentation Current()
            {
                ProductEntryPresentation active = original;
                if (active == null || !active.ClaimsVisible || active.Suspended || active.Epoch != epoch || active.ProgressSerial != serial)
                {
                    throw new OperationCanceledException();
                }
                return active;
            }
            ((UIGridKitchen)self.Hud).GetAsyncOwner().Run(async context =>
            {
                try { await action(context, Current); }
                catch (OperationCanceledException) { }
                catch (Exception exception)
                {
                    try
                    {
                        var active = Current(); active.Failure = exception.Message == "GrantScopeDenied"
                            ? "替换许可的用途、接收身份或来源根不匹配" : ProgressError(exception.Message);
                        // 失败后旧确认失效，必须重新查看当前目标及有效许可。
                        active.ReplacePreview = null; active.ReplaceGrant = null; active.ReplaceTicketId = ticket;
                    }
                    catch (OperationCanceledException) { }
                }
                finally
                {
                    try { var active = Current(); active.Busy = false; active.ProgressCommitting = false; active.RenderProductEntry(); }
                    catch (OperationCanceledException) { }
                }
            });
        }
    }
}
