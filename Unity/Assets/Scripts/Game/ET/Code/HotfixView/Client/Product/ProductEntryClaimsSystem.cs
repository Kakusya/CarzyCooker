using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    public static partial class ProductEntryPresentationSystem
    {
        public static bool ClaimsAreVisible(Scene scene) => scene.Root().GetComponent<ProductEntryPresentation>()?.ClaimsVisible == true;

        public static void OpenClaims(this ProductEntryPresentation self)
        {
            if (self.ClaimsVisible)
            {
                return;
            }
            self.ClaimsVisible = true; self.ClaimsNeedsRefresh = true;
            self.ProgressVisible = false; self.Busy = false; self.Suspended = false;
            self.ProgressFeedback = self.Failure = null;
            self.ClaimRows = Array.Empty<ProgressTicketSummary>();
            ClientSessionComponent client = self.Root().GetComponent<ProductRuntimeComponent>().Client;
            if (client != null)
            {
                EventSystem.Instance.Publish((Scene)client.IScene, new ProgressClaimsOverlayChanged { Visible = true });
            }
            if (((UIGridKitchen)self.Hud)?.Available == true)
            {
                self.RenderProductEntry();
            }
            else self.OpenProductEntry();
        }

        private static void ClaimsButton(this ProductEntryPresentation self, MonoUIGridKitchen view, string caption,
            Action<ProductEntryPresentation> action, bool target = false)
        {
            EntityRef<ProductEntryPresentation> original = self;
            long epoch = self.Epoch, render = self.ProgressRenderVersion;
            void Execute()
            {
                ProductEntryPresentation current = original;
                if (current == null || !current.ClaimsVisible || current.Suspended || current.Epoch != epoch || current.ProgressRenderVersion != render)
                {
                    return;
                }
                action(current);
            }
            if (target)
            {
                view.AddTarget(caption, Execute);
            }
            else view.AddControl(caption, Execute);
        }

        private static void RenderClaimsEntry(this ProductEntryPresentation self, MonoUIGridKitchen view)
        {
            ClientSessionComponent client = self.Root().GetComponent<ProductRuntimeComponent>().Client;
            if (self.ProgressVisible)
            {
                self.RenderProgressEntry(view, client, client?.GetComponent<ProductRoomClient>());
                return;
            }
            if (self.ReplacePreview != null || self.ReplaceTicketId != null)
            {
                self.RenderReplacementEntry(view);
                return;
            }
            view.PhaseText.text = "合作进度领取";
            view.ClockText.text = "本机独立分支";
            view.HintText.text = "主动领取会新建记录；每个结果在本机只领取一次";
            view.StatusText.text = self.Failure ?? self.ProgressFeedback ?? "收到凭证后可领取；正常结束整个房间会关闭未领取资格";
            view.SettlementText.text = self.ClaimRows.Length == 0 ? "暂无已持久化的合作凭证" : "已领取记录可从单机记录或房间的存档入口继续";
            if (self.Busy)
            {
                view.StatusText.text = "正在查询或保存…";
                self.ClaimsButton(view, "取消等待", current =>
                {
                    ++current.ProgressSerial;
                    current.Busy = false;
                    current.ProgressFeedback = "等待已取消；重新查询可确认实际提交结果";
                    current.RenderProductEntry();
                });
                return;
            }
            self.ClaimsButton(view, "刷新领取状态", current => current.BrowseClaims());
            self.ClaimsButton(view, "身份与授权文件", current => { current.ProgressVisible = true; current.RenderProductEntry(); });
            foreach (var row in self.ClaimRows)
            {
                string ticketId = row.TicketId;
                string title = "第 " + row.CompletedLevelId + " 关 · " + row.Score + " 分 · " + row.TicketId.Substring(0, 6);
                switch (row.State)
                {
                    case ProgressReceiptState.Available:
                        self.ClaimsButton(view, "领取新档 " + title, current => current.ClaimProgress(ticketId), true);
                        self.ClaimsButton(view, "选择有损替换 " + title, current => current.ChooseReplaceTarget(ticketId), true);
                        break;
                    case ProgressReceiptState.Claimed:
                        self.ClaimsButton(view, "已领取 " + title, current =>
                        {
                            current.ProgressFeedback = "已领取至 " + row.SlotId;
                            current.RenderProductEntry();
                        }, true);
                        break;
                    case ProgressReceiptState.Closed:
                        self.ClaimsButton(view, "已关闭 " + title, current =>
                        {
                            current.ProgressFeedback = "本机已记录整个房间正常结束，此资格已关闭";
                            current.RenderProductEntry();
                        }, true);
                        break;
                }
            }
            self.ClaimsButton(view, "返回经营或房间", current => current.CloseClaims());
        }

        private static void RunClaimOperation(this ProductEntryPresentation self, ProgressRepositoryOperation operation, string ticket = null)
        {
            if (self.Busy || !self.ClaimsVisible || self.Suspended)
            {
                return;
            }
            self.Busy = true; self.ClaimsNeedsRefresh = false; self.Failure = null;
            long serial = ++self.ProgressSerial, epoch = self.Epoch;
            EntityRef<ProductEntryPresentation> original = self;
            self.RenderProductEntry();
            var hud = (UIGridKitchen)self.Hud;
            ProductEntryPresentation Current()
            {
                ProductEntryPresentation current = original;
                if (current == null || !current.ClaimsVisible || current.Suspended || current.Epoch != epoch || current.ProgressSerial != serial)
                {
                    throw new OperationCanceledException();
                }
                return current;
            }
            hud.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    if (operation == ProgressRepositoryOperation.Claim)
                    {
                        var header = await self.ProgressRepositoryCall(context, ProductMessages.Create<ProgressRepositoryRequest>(value =>
                            { value.Operation = (int)ProgressRepositoryOperation.Claim; value.Id = ticket; }), response => ProgressClaimCodec.Decode<ProgressCommitReceipt>(((ProgressRepositoryResponse)response).Receipt));
                        Current().ProgressFeedback = "已领取至 " + header.SlotId;
                    }
                    var rows = await Current().ProgressRepositoryCall(context, ProductMessages.Create<ProgressRepositoryRequest>(value => value.Operation = (int)ProgressRepositoryOperation.Browse),
                        response => ((ProgressRepositoryResponse)response).Documents.Select(ProgressClaimCodec.Decode<ProgressTicketSummary>).ToArray());
                    Current().ClaimRows = rows;
                    Current().ClaimsNeedsRefresh = false;
                }
                catch (OperationCanceledException) { }
                catch (Exception exception)
                {
                    try { Current().Failure = ProgressError(exception.Message); }
                    catch (OperationCanceledException) { }
                }
                finally
                {
                    try { var current = Current(); current.Busy = false; current.RenderProductEntry(); }
                    catch (OperationCanceledException) { }
                }
            });
        }

        public static void BrowseClaims(this ProductEntryPresentation self) => self.RunClaimOperation(ProgressRepositoryOperation.Browse);
        private static void ClaimProgress(this ProductEntryPresentation self, string ticket) => self.RunClaimOperation(ProgressRepositoryOperation.Claim, ticket);

        public static void CloseClaims(this ProductEntryPresentation self)
        {
            self.ClaimsVisible = self.ClaimsNeedsRefresh = false;
            self.CloseProductEntry();
            var client = (ClientSessionComponent)self.Root().GetComponent<ProductRuntimeComponent>().Client;
            if (client != null)
            {
                EventSystem.Instance.Publish((Scene)client.IScene, new ProgressClaimsOverlayChanged { Visible = false });
            }
            if (client?.GetComponent<ProductRoomClient>()?.Current.HasRestaurant != true)
            {
                self.OpenProductEntry();
            }
        }

        private static void LeaveRoomNormally(this ProductEntryPresentation self)
        {
            if (self.ClosingMatch)
            {
                return;
            }
            var runtime = self.Root().GetComponent<ProductRuntimeComponent>();
            ClientSessionComponent client = runtime.Client;
            if (runtime.Config.Role != RuntimeInstanceRole.Host || client?.Stage != ProductClientStage.Joined)
            {
                ProductBootstrapBridge.ReturnToModeMenu(runtime.Config);
                return;
            }
            self.ClosingMatch = self.Busy = true;
            long epoch = self.Epoch;
            EntityRef<ProductEntryPresentation> original = self;
            EntityRef<ClientSessionComponent> clientRef = client;
            EntityRef<ProductRuntimeComponent> runtimeRef = runtime;
            self.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    while (true)
                    {
                        client = clientRef; runtime = runtimeRef; self = original;
                        if (client == null || runtime == null || self == null || self.Epoch != epoch || (ClientSessionComponent)runtime.Client != client)
                        {
                            return;
                        }
                        var response = await client.ProgressClientCall(context, ProductMessages.Create<ProgressCloseMatchRequest>(value =>
                        {
                            value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = client.MatchId;
                            value.BindingGeneration = client.BindingGeneration; value.ExpectedRoomVersion = client.GetComponent<ProductRoomClient>().Current.Version;
                        }), reply => ((ProgressCloseMatchResponse)reply).Status);
                        context.ThrowIfStopped();
                        if (response == (int)GridResultStatus.Executed)
                        {
                            runtime = runtimeRef;
                            if (runtime != null)
                            {
                                ProductBootstrapBridge.ReturnToModeMenu(runtime.Config);
                            }
                            return;
                        }
                        await UniTask.Delay(200, cancellationToken: context.CancellationToken);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception exception)
                {
                    self = original;
                    if (self != null && self.Epoch == epoch)
                    {
                        self.Failure = exception.Message;
                    }
                }
                finally
                {
                    self = original;
                    if (self != null && self.Epoch == epoch)
                    {
                        self.Busy = self.ClosingMatch = false;
                        self.RenderProductEntry();
                    }
                }
            });
        }
    }

    [Event(SceneType.ProductRoot)]
    [FriendOf(typeof(ProductEntryPresentation))]
    public sealed class ProgressInboxChangedView : AEvent<Scene, ProgressInboxChanged>
    {
        protected override UniTask Run(Scene scene, ProgressInboxChanged args)
        {
            var entry = scene.GetComponent<ProductEntryPresentation>();
            if (entry?.ClaimsVisible == true)
            {
                entry.ClaimsNeedsRefresh = true;
            }
            return UniTask.CompletedTask;
        }
    }

    [Event(SceneType.ProductClient)]
    public sealed class ProgressClaimsOverlayView : AEvent<Scene, ProgressClaimsOverlayChanged>
    {
        protected override UniTask Run(Scene scene, ProgressClaimsOverlayChanged args)
        {
            var presentation = scene.GetComponent<GridPresentationComponent>();
            if (args.Visible)
            {
                presentation?.CloseGridHud();
            }
            else presentation?.OpenGridHud();
            return UniTask.CompletedTask;
        }
    }
}
