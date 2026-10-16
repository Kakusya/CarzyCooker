using System;
using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    public static partial class ProductEntryPresentationSystem
    {
        private static void ClearLeaveConfirmation(this ProductEntryPresentation self)
        {
            ++self.LeaveSerial;
            self.LeaveAction = ProductLeaveAction.None;
            self.LeavePending = self.LeaveInvalidated = self.LeaveFromRestaurant = false;
            self.LeaveClient = default;
            self.LeaveMatch = self.LeaveWorld = null;
        }

        public static void RequestProductLeave(this ProductEntryPresentation self, ProductLeaveAction action, bool fromRestaurant = false)
        {
            if (self.LeavePending || self.ClosingMatch || self.Busy || action == ProductLeaveAction.None)
            {
                return;
            }
            var runtime = self.Root().GetComponent<ProductRuntimeComponent>();
            ClientSessionComponent client = runtime.Client;
            if (action == ProductLeaveAction.SoloReturnModes && runtime.Config.Role != RuntimeInstanceRole.Solo ||
                action == ProductLeaveAction.ClientLeave && runtime.Config.Role != RuntimeInstanceRole.Client ||
                action is ProductLeaveAction.HostReturnRoom or ProductLeaveAction.HostEndRoom &&
                (runtime.Config.Role != RuntimeInstanceRole.Host || client?.GetComponent<ProductRoomClient>()?.Current.CanControlRoom != true))
            {
                return;
            }
            self.ChoosingSolo = false;
            self.OpenClaims(); // Existing overlay handoff closes only the map HUD; its runtime and sender keep running.
            self.ClaimsNeedsRefresh = self.ProgressVisible = false;
            self.LeaveAction = action;
            self.LeaveFromRestaurant = fromRestaurant;
            self.LeaveClient = client;
            self.LeaveMatch = client?.MatchId;
            self.LeaveBinding = client?.BindingGeneration ?? 0;
            self.LeaveRoomVersion = client?.GetComponent<ProductRoomClient>()?.Current.Version ?? 0;
            var grid = client?.GetComponent<GridClientComponent>();
            self.LeaveWorld = grid?.WorldId;
            self.LeaveWorldGeneration = grid?.WorldGeneration ?? 0;
            self.LeaveLevelId = grid?.LevelId ?? 0;
            self.LeaveLevelGeneration = grid?.LevelGeneration ?? 0;
            ++self.LeaveSerial;
            self.LeaveInvalidated = false;
            self.Failure = self.ProgressFeedback = null;
            if (((UIGridKitchen)self.Hud)?.Available == true)
            {
                self.RenderProductEntry();
            }
        }

        private static bool LeaveContextMatches(this ProductEntryPresentation self)
        {
            ClientSessionComponent captured = self.LeaveClient;
            var runtime = self.Root().GetComponent<ProductRuntimeComponent>();
            ClientSessionComponent client = runtime.Client;
            if (client != captured || client?.MatchId != self.LeaveMatch || (client?.BindingGeneration ?? 0) != self.LeaveBinding ||
                (client?.GetComponent<ProductRoomClient>()?.Current.Version ?? 0) != self.LeaveRoomVersion)
            {
                return false;
            }
            if (self.LeaveFromRestaurant)
            {
                var grid = client?.GetComponent<GridClientComponent>();
                return client?.Stage == ProductClientStage.Joined && grid?.Confirmed == true && grid.WorldId == self.LeaveWorld &&
                    grid.WorldGeneration == self.LeaveWorldGeneration && grid.LevelId == self.LeaveLevelId && grid.LevelGeneration == self.LeaveLevelGeneration;
            }
            return self.LeaveAction == ProductLeaveAction.ClientLeave || client?.Stage == ProductClientStage.Joined;
        }

        private static bool RefreshLeaveConfirmation(this ProductEntryPresentation self)
        {
            if (self.LeaveAction == ProductLeaveAction.None)
            {
                return false;
            }
            bool changed = !self.LeavePending && !self.LeaveInvalidated && !self.LeaveContextMatches();
            if (changed)
            {
                self.LeaveInvalidated = true;
                self.Failure = "房间、关卡或连接已变化，请返回后重新选择";
            }
            if (((UIGridKitchen)self.Hud)?.Available != true)
            {
                self.OpenProductEntry();
            }
            else if (changed)
            {
                self.RenderProductEntry();
            }
            return true;
        }

        private static void RenderLeaveConfirmation(this ProductEntryPresentation self, MonoUIGridKitchen view)
        {
            view.PhaseText.text = "离开与返回";
            view.ClockText.text = "确认前不执行";
            view.HintText.text = "不会自动暂停或保存；营业中的餐厅继续推进";
            view.StatusText.text = self.Failure ?? "先核对离开范围，取消可返回当前运行";
            string effect = self.LeaveAction switch
            {
                ProductLeaveAction.SoloReturnModes => "返回模式选择\n结束本轮单机运行。\n尚未保存的进度不会补存，已发布记录保留。",
                ProductLeaveAction.ClientLeave => "离开房间\n只退出本人；Host和其他成员继续。\n手持物与工作按原Leave规则收尾。\n已收到的凭证保留，不在此代领。",
                ProductLeaveAction.HostReturnRoom => "仅关闭餐厅并返回房间\n当前运行不补存；不是成功结算。\n保留同一房间、成员和既有领取资格。\n再次开局使用独立的新世界。",
                _ => "结束整个房间\n所有成员结束本次运行，尚未保存的运行不补存。\n按原关闭回执处理未领取资格；已发布记录保留。\n提交后不能撤销，等待原关闭流程完成。"
            };
            view.SettlementText.text = effect + "\n\n已暂停的世界保持原暂停状态。";
            if (self.LeavePending || self.ClosingMatch)
            {
                view.StatusText.text = "正在执行原退出流程，请等待实际结果…";
                view.ClockText.text = "提交中，不能撤销";
                return;
            }
            if (!self.LeaveInvalidated)
            {
                self.ClaimsButton(view, "确认" + LeaveCaption(self.LeaveAction), current => current.ConfirmProductLeave());
            }
            self.ClaimsButton(view, "取消离开", current => current.CloseClaims());
        }

        private static string LeaveCaption(ProductLeaveAction action) => action switch
        {
            ProductLeaveAction.SoloReturnModes => "返回模式选择",
            ProductLeaveAction.ClientLeave => "离开房间",
            ProductLeaveAction.HostReturnRoom => "仅返回房间",
            _ => "结束整个房间"
        };

        private static void ConfirmProductLeave(this ProductEntryPresentation self)
        {
            if (self.LeavePending || self.ClosingMatch || self.LeaveInvalidated || !self.LeaveContextMatches())
            {
                return;
            }
            self.LeavePending = true;
            self.RenderProductEntry();
            var runtime = self.Root().GetComponent<ProductRuntimeComponent>();
            if (self.LeaveAction == ProductLeaveAction.SoloReturnModes)
            {
                if (!ProductBootstrapBridge.ReturnToModeMenu(runtime.Config))
                {
                    self.LeavePending = false;
                    self.Failure = "尚未切换模式，请核对当前运行后重试";
                    self.RenderProductEntry();
                }
                return;
            }
            if (self.LeaveAction == ProductLeaveAction.HostEndRoom)
            {
                self.LeaveRoomNormally();
                return;
            }
            EntityRef<ProductEntryPresentation> original = self;
            EntityRef<ProductRuntimeComponent> runtimeRef = runtime;
            EntityRef<ClientSessionComponent> clientRef = runtime.Client;
            long serial = self.LeaveSerial, epoch = self.Epoch;
            ProductLeaveAction action = self.LeaveAction;
            self.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    ClientSessionComponent client = clientRef;
                    if (action == ProductLeaveAction.HostReturnRoom)
                    {
                        string error = await client.CallProductValueAsync(context,
                            client.CreateRoomCommand(ProductRoomAction.CloseRestaurant, client.GetComponent<ProductRoomClient>().Current.MenuVariant),
                            response => response.Error == 0 ? null : response.Message);
                        if (error != null)
                        {
                            throw new InvalidOperationException(error);
                        }
                        var current = Current();
                        current?.CloseClaims();
                    }
                    else
                    {
                        bool committed = false;
                        string failure = null;
                        try { committed = client != null && await client.LeaveProduct(); }
                        catch (Exception exception) { failure = exception.Message; }
                        context.ThrowIfStopped();
                        var current = Current();
                        runtime = runtimeRef;
                        if (current != null && runtime != null &&
                            !ProductBootstrapBridge.ReturnToModeMenu(runtime.Config, committed ? "已获Host确认，已离开房间" :
                                "本地连接已结束，未获Host确认Leave" + (failure == null ? "" : "：" + failure)))
                        {
                            current.Failure = committed ? "Host已确认离开；本地模式切换尚未完成" : "本地连接已结束，未获Host确认Leave";
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception exception)
                {
                    var current = Current();
                    if (current != null)
                    {
                        current.Failure = exception.Message;
                    }
                }
                finally
                {
                    var current = Current();
                    if (current != null)
                    {
                        current.LeavePending = false;
                        current.RenderProductEntry();
                    }
                }
            });
            ProductEntryPresentation Current()
            {
                ProductEntryPresentation current = original;
                return current != null && current.Epoch == epoch && current.LeaveSerial == serial &&
                    current.LeaveAction == action && (ClientSessionComponent)current.Root().GetComponent<ProductRuntimeComponent>().Client == (ClientSessionComponent)clientRef
                    ? current : null;
            }
        }
    }
}
