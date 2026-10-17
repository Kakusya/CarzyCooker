using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;

namespace ET.Client
{
    public static partial class ProductEntryPresentationSystem
    {
        private static SoloSaveRepository ProgressRepository(this ProductEntryPresentation self)
        {
            var repository = self.Root().GetComponent<SoloSaveRepository>();
            if (repository != null)
            {
                return repository;
            }
            if (Application.isMobilePlatform)
            {
                throw new InvalidOperationException("当前平台只支持内存合作");
            }
            string directory = Application.persistentDataPath;
            return self.Root().AddComponent<SoloSaveRepository, string, string>(directory, SoloSaveRepositorySystem.LocalProfile(directory));
        }

        private static void AddProgressControl(this ProductEntryPresentation self, MonoUIGridKitchen view, string caption,
            Action<ProductEntryPresentation> action, bool target = false)
        {
            EntityRef<ProductEntryPresentation> original = self;
            ClientSessionComponent client = self.Root().GetComponent<ProductRuntimeComponent>().Client;
            EntityRef<ClientSessionComponent> initialClient = client;
            long epoch = self.Epoch, render = self.ProgressRenderVersion, binding = client?.BindingGeneration ?? 0;
            ProductClientStage stage = client?.Stage ?? default;
            long clientInstance = client?.InstanceId ?? 0;
            long version = client?.GetComponent<ProductRoomClient>()?.Current.Version ?? 0;
            void Execute()
            {
                ProductEntryPresentation current = original;
                ClientSessionComponent active = initialClient;
                if (current == null || current.Suspended || current.Epoch != epoch || current.ProgressRenderVersion != render ||
                    (active?.InstanceId ?? 0) != clientInstance ||
                    (ClientSessionComponent)current.Root().GetComponent<ProductRuntimeComponent>().Client != active ||
                    active != null && (active.BindingGeneration != binding || active.Stage != stage ||
                        (active.GetComponent<ProductRoomClient>()?.Current.Version ?? 0) != version))
                {
                    return;
                }
                action(current);
            }
            if (target)
            {
                view.AddTarget(caption, Execute);
            }
            else
            {
                view.AddControl(caption, Execute);
            }
        }

        private static void RenderProgressEntry(this ProductEntryPresentation self, MonoUIGridKitchen view, ClientSessionComponent client, ProductRoomClient room)
        {
            bool joined = client?.Stage == ProductClientStage.Joined && room?.Current.Version > 0;
            view.PhaseText.text = "存档、身份与授权";
            view.ClockText.text = joined ? room.Current.VerifiedProfileId == null ? "先验证本机身份" : "本机身份已验证" : "离线本机记录";
            view.HintText.text = "导入新建独立分支；只读运行和有损替换分别授权";
            view.StatusText.text = self.Failure ?? self.ProgressFeedback ?? "导入前请核对签发者、来源和目标房间";
            view.SettlementText.text = !joined ? "离线可以导入授权、查询与导出本机记录。" :
                room.Current.VerifiedProfileId == null ? "身份通过后，房主可载入成功记录。" : "当前身份：" + room.Current.VerifiedProfileId;
            if (self.RenderAuthorizationTask(view, client, room))
            {
                return;
            }
            view.ShowEntryAddress(self.ProgressPath ?? "", 2048);
            if (self.Busy)
            {
                view.StatusText.text = self.ProgressCommitting ? "正在提交，请等待结果…" : "正在读取或验证…";
                if (!self.ProgressCommitting)
                {
                    self.AddProgressControl(view, "取消读取", current =>
                    {
                        ++current.ProgressSerial;
                        current.Busy = false;
                        current.ProgressFeedback = "读取已取消";
                        current.RenderProductEntry();
                    });
                }
                return;
            }
            if (self.ConfirmImport)
            {
                self.RenderImportConfirmation(view);
                return;
            }
            if (self.ClaimDestinationSlot == null)
            {
                self.AddProgressControl(view, "返回任务选择", current => current.NavigateAuthorizationTask(AuthorizationViewTask.Home));
            }
            if (joined)
            {
                self.AddProgressControl(view, "验证本机身份", current => current.VerifyProgressProfile());
            }
            self.AddProgressControl(view, "本机成功记录", current => current.BrowseProgressRecords());
            self.AddProgressControl(view, "导出身份文件", current => current.ProgressFileOperation(ProgressFileAction.ExportIdentity));
            self.AddProgressControl(view, "查看导入文件", current => current.ProgressFileOperation(ProgressFileAction.Inspect));
            if (self.InspectedIdentity != null)
            {
                var identity = self.InspectedIdentity;
                view.SettlementText.text = $"签发者：{identity.DisplayName}\nProfile：{identity.ProfileId}\n密钥：{identity.KeyId}\n来源：{self.ProgressPath}\n{self.InspectedSummary}";
                if (!self.InspectedTrusted)
                {
                    self.AddProgressControl(view, "确认信任此身份", current => current.ProgressFileOperation(ProgressFileAction.TrustIdentity));
                }
                else if (self.InspectedAuthorizationKind == SaveAuthorizationKind.ImportBranch)
                {
                    self.AddProgressControl(view, "导入为独立分支", current => { current.ConfirmImport = true; current.RenderProductEntry(); });
                }
                else if (self.InspectedAuthorizationKind == SaveAuthorizationKind.Replace)
                {
                    self.AddProgressControl(view, "用于领取替换", current =>
                    {
                        current.ProgressVisible = false;
                        current.ProgressFeedback = "替换许可已核对，请选择未领取结果和目标";
                        current.RenderProductEntry();
                    });
                }
                else if (self.InspectedAuthorization != null && joined && !room.Current.HasRestaurant && room.Current.CanControlRoom)
                {
                    self.AddProgressControl(view, "发布此授权提案", current => current.LoadProgressAuthorization(current.InspectedAuthorization));
                }
                if (self.InspectedAuthorizationKind != SaveAuthorizationKind.None && self.InspectedAuthorization != null)
                {
                    self.AddProgressControl(view, "本端拒绝未使用许可", current => current.RejectInspectedGrant());
                }
            }
            if (self.ClaimDestinationSlot != null)
            {
                var destination = self.ProgressHeaders.SingleOrDefault(header => header.SlotId == self.ClaimDestinationSlot);
                view.SettlementText.text = destination == null ? "本机记录选择\n当前未找到记录，请刷新确认；原提交事实不会因此撤回" :
                    "本机记录选择 · 尚未加载\n本机记录：" + destination.SlotId + "\n当前修订：" + destination.Revision +
                    "\n归属：" + destination.ProfileId + "\n" + (destination.RecordKind == SoloSaveRecordKind.Continue
                        ? "可继续：关卡 " + destination.NextLevelId : "大关完成记录，仅供查看") + "\n请选择原读取入口继续核对；合作来源先发布提案，全员准备后由Host开局。";
                var runtime = self.Root().GetComponent<ProductRuntimeComponent>();
                if (destination != null && runtime.Config.Role == RuntimeInstanceRole.Solo &&
                    (ClientSessionComponent)runtime.Client == null && (Scene)runtime.HostScene == null)
                {
                    self.AddProgressControl(view, "前往单机选档", current => current.NavigateClaimedSoloRecord(destination.SlotId, destination.Revision));
                }
            }
            foreach (var header in self.ProgressHeaders)
            {
                if (self.ClaimDestinationSlot != null && header.SlotId != self.ClaimDestinationSlot)
                {
                    continue;
                }
                string slot = header.SlotId;
                string title = "记录 " + slot.Substring(0, 8) + " · 修订 " + header.Revision;
                self.AddProgressControl(view, "导出分支许可 " + title, current => current.ExportProgressGrant(SaveAuthorizationAction.ExportImport, slot), true);
                if (self.InspectedIdentity != null && self.InspectedTrusted)
                {
                    self.AddProgressControl(view, "向已核对身份签发替换许可 " + title, current => current.ExportProgressGrant(SaveAuthorizationAction.ExportReplace, slot), true);
                }
                if (header.RecordKind != SoloSaveRecordKind.Continue)
                {
                    self.AddProgressControl(view, "已完成 " + title, current =>
                    {
                        current.ProgressFeedback = "此记录已完成大关，请选择可继续记录";
                        current.RenderProductEntry();
                    }, target: true);
                    continue;
                }
                if (joined && !room.Current.HasRestaurant && room.Current.CanControlRoom)
                {
                    self.AddProgressControl(view, "读取 " + title, current => current.ReadProgressRecord(slot), target: true);
                }
                if (joined)
                {
                    self.AddProgressControl(view, "授权导出 " + title, current => current.ProgressFileOperation(ProgressFileAction.ExportReadRun, slot), target: true);
                }
            }
            self.AddProgressControl(view, self.ClaimsVisible ? "返回领取列表" : "返回房间", current =>
            {
                current.ProgressVisible = false;
                current.ClaimDestinationSlot = null;
                current.ClaimDetailTicketId = null;
                current.RenderProductEntry();
            });
        }

        private static void RunProgress(this ProductEntryPresentation self, Func<AsyncContext, Func<ProductEntryPresentation>, UniTask> action, bool returnToRoomAfterSuccess = false)
        {
            UIGridKitchen hud = self.Hud;
            if (self.Busy || self.Suspended || !self.ProgressVisible || hud?.Available != true)
            {
                return;
            }
            ClientSessionComponent client = self.Root().GetComponent<ProductRuntimeComponent>().Client;
            EntityRef<ProductEntryPresentation> original = self;
            EntityRef<ClientSessionComponent> initialClient = client;
            long binding = client?.BindingGeneration ?? 0, epoch = self.Epoch, serial = ++self.ProgressSerial;
            ProductClientStage stage = client?.Stage ?? default;
            long clientInstance = client?.InstanceId ?? 0;
            self.Busy = true;
            self.Failure = null;
            self.ProgressCommitting = false;
            self.RenderProductEntry();
            ProductEntryPresentation Current()
            {
                ProductEntryPresentation current = original;
                ClientSessionComponent active = initialClient;
                if (current == null || current.Suspended || current.Epoch != epoch || current.ProgressSerial != serial ||
                    (active?.InstanceId ?? 0) != clientInstance ||
                    !current.ProgressVisible || active != null && (active.Stage != stage || active.BindingGeneration != binding) ||
                    (ClientSessionComponent)current.Root().GetComponent<ProductRuntimeComponent>().Client != active)
                {
                    throw new OperationCanceledException();
                }
                return current;
            }
            hud.GetAsyncOwner().Run(async context =>
            {
                bool completed = false;
                try
                {
                    await action(context, Current);
                    completed = true;
                }
                catch (OperationCanceledException) { }
                catch (Exception exception)
                {
                    try { Current().Failure = ProgressError(exception.Message); }
                    catch (OperationCanceledException) { }
                }
                finally
                {
                    try
                    {
                        ProductEntryPresentation current = Current();
                        current.Busy = current.ProgressCommitting = false;
                        if (completed && returnToRoomAfterSuccess)
                        {
                            current.ProgressVisible = false;
                            current.Page = ProductEntryPage.Home;
                        }
                        current.RenderProductEntry();
                    }
                    catch (OperationCanceledException) { }
                }
            });
        }

        private static string ProgressError(string error) => error switch
        {
            "ProfileNotTrusted" => "请先导入并确认对方身份文件",
            "ProfileProofRequired" => "请先验证本机身份",
            "ProfileKeyConflict" => "该身份已绑定另一公钥，未替换原绑定",
            "ProfilePrivateKeyMissing" => "本机身份私钥缺失，无法代替原身份签名",
            "ProfileIdentityMissing" => "本机身份资料不完整，请先核对身份文件",
            "InvalidSignature" => "文件签名无效，未载入餐厅",
            "GrantScopeDenied" => "此授权不适用于当前房主或房间",
            "WrongPlatform" => "此文件属于另一平台",
            "ChapterCompleted" => "大关完成记录不能继续",
            "GrantExpired" => "授权已过期，需重新取得有效许可",
            "GrantRejected" => "此未使用许可已被本端拒绝",
            "TargetMissing" => "本机没有这份授权目标记录",
            "RevisionConflict" or "TargetChanged" => "目标已变化，请重新查看并取得当前修订的许可后确认",
            "AlreadyClaimed" => "该结果已领取，请刷新查看原记录",
            _ => error
        };

        private static async UniTask<T> ProgressRepositoryCall<T>(this ProductEntryPresentation self, AsyncContext context, IRequest request, Func<IResponse, T> read)
        {
            SoloSaveRepository repository;
            try { repository = self.ProgressRepository(); }
            catch { ((MessageObject)request).Dispose(); throw; }
            var result = await context.CallLocalValueAsync(self.Root().GetComponent<ProcessInnerSender>(), repository.GetActorId(), request,
                response => (Error: response.Error == 0 ? null : response.Message ?? "RequestRejected", Value: response.Error == 0 ? read(response) : default(T)));
            if (result.Error != null)
            {
                throw new InvalidOperationException(result.Error);
            }
            return result.Value;
        }

        private static async UniTask<T> ProgressClientCall<T>(this ClientSessionComponent client, AsyncContext context, IRequest request, Func<IResponse, T> read)
        {
            var result = await client.CallProductValueAsync(context, request,
                response => (Error: response.Error == 0 ? null : response.Message ?? "RequestRejected", Value: response.Error == 0 ? read(response) : default(T)));
            if (result.Error != null)
            {
                throw new InvalidOperationException(result.Error);
            }
            return result.Value;
        }

        private static void RequireProgressResponse(IResponse response)
        {
            if (response.Error != 0)
            {
                throw new InvalidOperationException(response.Message);
            }
        }

        private static void VerifyProgressProfile(this ProductEntryPresentation self)
        {
            self.RunProgress(async (context, current) =>
            {
                ProductEntryPresentation active = current();
                ClientSessionComponent client = active.Root().GetComponent<ProductRuntimeComponent>().Client;
                EntityRef<ClientSessionComponent> clientRef = client;
                string match = client.MatchId;
                long binding = client.BindingGeneration;
                string challenge = await client.ProgressClientCall(context, ProductMessages.Create<ProductProfileRequest>(request =>
                {
                    request.ProtocolVersion = ProductSessionRules.ProtocolVersion;
                    request.MatchId = match;
                    request.BindingGeneration = binding;
                    request.Action = 1;
                }), response => { RequireProgressResponse(response); return ((ProductProfileResponse)response).Challenge; });
                active = current();
                byte[] proof = await active.ProgressRepositoryCall(context, ProductMessages.Create<ProductProfileSignRequest>(request =>
                {
                    request.MatchId = match;
                    request.BindingGeneration = binding;
                    request.Challenge = challenge;
                }), response => { RequireProgressResponse(response); return (byte[])((ProductProfileSignResponse)response).Proof.Clone(); });
                active = current();
                client = clientRef;
                string profile = await client.ProgressClientCall(context, ProductMessages.Create<ProductProfileRequest>(request =>
                {
                    request.ProtocolVersion = ProductSessionRules.ProtocolVersion;
                    request.MatchId = match;
                    request.BindingGeneration = binding;
                    request.Action = 2;
                    request.Proof = proof;
                }), response => { RequireProgressResponse(response); return ((ProductProfileResponse)response).ProfileId; });
                current().ProgressFeedback = "身份已验证：" + profile;
            });
        }

        private static void BrowseProgressRecords(this ProductEntryPresentation self)
        {
            self.RunProgress(async (context, current) =>
            {
                var headers = await current().ProgressRepositoryCall(context, ProductMessages.Create<SoloSaveBrowseRequest>(_ => { }), response =>
                {
                    RequireProgressResponse(response);
                    return ((SoloSaveBrowseResponse)response).Headers.Select(bytes =>
                        (SoloSaveHeader)MemoryPackHelper.Deserialize(typeof(SoloSaveHeader), bytes, 0, bytes.Length)).ToArray();
                });
                current().ProgressHeaders = headers;
                current().ProgressFeedback = headers.Length == 0 ? "没有本机成功记录，可以返回房间开启内存餐厅" : "请选择读取，或为当前房主导出授权";
            });
        }

        private static void ProgressFileOperation(this ProductEntryPresentation self, ProgressFileAction action, string slot = null)
        {
            string input = ((UIGridKitchen)self.Hud).View.EntryAddress;
            byte[] identity = action == ProgressFileAction.TrustIdentity ? OfflineProfiles.EncodeIdentity(self.InspectedIdentity) :
                action == ProgressFileAction.ExportReadRun ? ((ClientSessionComponent)self.Root().GetComponent<ProductRuntimeComponent>().Client)?.GetComponent<ProductRoomClient>()?.Current.HostIdentity : null;
            self.RunProgress(async (context, current) =>
            {
                if (action == ProgressFileAction.ExportReadRun && identity == null)
                {
                    throw new InvalidOperationException("请房主先验证本机身份，并导入其身份文件");
                }
                if (action != ProgressFileAction.Inspect)
                {
                    current().ProgressCommitting = true;
                    current().RenderProductEntry();
                }
                ClientSessionComponent client = current().Root().GetComponent<ProductRuntimeComponent>().Client;
                var values = await current().ProgressRepositoryCall(context, ProductMessages.Create<ProgressFileRequest>(request =>
                {
                    request.Action = (int)action;
                    request.InputPath = input;
                    request.Identity = identity == null ? null : (byte[])identity.Clone();
                    request.SlotId = slot;
                    request.MatchId = client?.MatchId ?? "";
                }), response =>
                {
                    RequireProgressResponse(response);
                    var reply = (ProgressFileResponse)response;
                    return (reply.OutputPath, reply.Summary, Kind: (SaveAuthorizationKind)reply.AuthorizationKind, Identity: reply.Identity == null ? null : (byte[])reply.Identity.Clone(),
                        Authorization: reply.Authorization == null ? null : (byte[])reply.Authorization.Clone());
                });
                var active = current();
                if (action == ProgressFileAction.Inspect)
                {
                    active.AuthorizationOutputPath = null;
                    active.InspectedIdentity = OfflineProfiles.DecodeIdentity(values.Identity);
                    active.InspectedAuthorization = values.Authorization;
                    active.InspectedAuthorizationKind = values.Kind;
                    active.ConfirmImport = false;
                    active.InspectedTrusted = false;
                    active.InspectedSummary = values.Summary;
                    active.ProgressPath = input;
                    active.AuthorizationInspectedPath = input;
                    active.ProgressFeedback = "请核对来源与身份，再明确确认信任";
                }
                else if (action == ProgressFileAction.TrustIdentity)
                {
                    active.InspectedTrusted = true;
                    active.ProgressFeedback = "该公钥已绑定，可载入适用于当前房间的授权";
                }
                else
                {
                    active.ProgressFeedback = "已导出：" + values.OutputPath;
                    active.AuthorizationOutputPath = values.OutputPath;
                    active.ProgressPath = values.OutputPath;
                    ((UIGridKitchen)active.Hud).View.ShowEntryAddress(values.OutputPath, 2048);
                }
            });
        }

        private static void ReadProgressRecord(this ProductEntryPresentation self, string slot)
        {
            self.RunProgress(async (context, current) =>
            {
                ClientSessionComponent client = current().Root().GetComponent<ProductRuntimeComponent>().Client;
                var room = client.GetComponent<ProductRoomClient>();
                if (string.IsNullOrEmpty(room.Current.VerifiedProfileId) || room.Current.HostIdentity == null)
                {
                    throw new InvalidOperationException("ProfileProofRequired");
                }
                byte[] authorization = await current().ProgressRepositoryCall(context, ProductMessages.Create<CooperativeBaselineSelectRequest>(request =>
                {
                    request.SlotId = slot;
                    request.MatchId = client?.MatchId ?? "";
                    request.HostIdentity = (byte[])room.Current.HostIdentity.Clone();
                }), response => { RequireProgressResponse(response); return (byte[])((CooperativeBaselineSelectResponse)response).Authorization.Clone(); });
                await CommitProgressAuthorization(context, current, authorization);
            }, returnToRoomAfterSuccess: true);
        }

        private static void LoadProgressAuthorization(this ProductEntryPresentation self, byte[] authorization)
        {
            self.RunProgress((context, current) => CommitProgressAuthorization(context, current, authorization), returnToRoomAfterSuccess: true);
        }

        private static async UniTask CommitProgressAuthorization(AsyncContext context, Func<ProductEntryPresentation> current, byte[] authorization)
        {
            var active = current();
            ClientSessionComponent client = active.Root().GetComponent<ProductRuntimeComponent>().Client;
            active.ProgressCommitting = true;
            active.RenderProductEntry();
            await client.ProgressClientCall(context, client.CreateBaselineCommand(authorization), response =>
            {
                RequireProgressResponse(response);
                return true;
            });
            active = current();
            active.ProgressFeedback = "来源提案已发布，请在房间分别准备后由Host开局";
        }
    }
}
