using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    [EntitySystemOf(typeof(ProductEntryPresentation))]
    [FriendOf(typeof(ProductEntryPresentation))]
    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(ClientSessionComponent))]
    [FriendOf(typeof(ProductRoomClient))]
    [FriendOf(typeof(GridClientComponent))]
    public static partial class ProductEntryPresentationSystem
    {
        [EntitySystem]
        private static void Awake(this ProductEntryPresentation self)
        {
            self.PreferencesDraft = null;
            self.InitializePreferences();
            self.Hud = default;
            self.Epoch = 0;
            self.LoadingHud = self.Busy = false;
            self.Suspended = false;
            self.Page = ProductEntryPage.Home;
            self.ClearLeaveConfirmation();
            ProductRuntimeComponent runtime = self.Root().GetComponent<ProductRuntimeComponent>();
            self.ChoosingSolo = runtime.Config.Role == RuntimeInstanceRole.Solo && !runtime.Config.ShowModeMenu;
            self.Failure = runtime.Config.EntryFeedback;
            self.JoinAddress = "127.0.0.1:";
            self.DisplayedStage = (ProductStage)(-1);
            self.DisplayedRosterVersion = self.DisplayedRoomVersion = self.DisplayedClient = -1;
            self.DisplayedRuntimeFailure = null;
            self.ProgressVisible = self.ProgressCommitting = false;
            self.ProgressSerial = self.ProgressRenderVersion = 0;
            self.AuthorizationTask = AuthorizationViewTask.Home;
            self.AuthorizationExport = AuthorizationExportPurpose.IndependentBranch;
            self.AuthorizationResultSlot = self.AuthorizationResultProfile = self.AuthorizationOutputPath = null; self.AuthorizationResultRevision = 0;
            self.AuthorizationIdentityExpanded = false; self.AuthorizationInspectedPath = null;
            self.InspectedTrusted = false;
            self.ProgressPath = self.ProgressFeedback = self.InspectedSummary = null;
            self.ProgressHeaders = Array.Empty<SoloSaveHeader>();
            self.InspectedIdentity = null;
            self.InspectedAuthorization = null;
            self.ClaimsVisible = self.ClaimsNeedsRefresh = self.ClosingMatch = false;
            self.ClaimRows = Array.Empty<ProgressTicketSummary>();
            self.ClaimInboxState = ClaimInboxDisplayState.Unknown;
            self.ClaimInboxDirty = true;
            self.ClaimInboxRunning = false;
            self.ClaimInboxSerial = self.ClaimInboxVersion = self.ClaimInboxInvalidation = self.ClaimInboxRepository = 0;
            self.AvailableClaimCount = 0; self.ClaimInboxError = null;
            self.ClaimFilter = ClaimListFilter.All;
            self.ClaimDetailTicketId = self.ClaimDestinationSlot = null;
            self.InspectedAuthorizationKind = SaveAuthorizationKind.None;
            self.ConfirmImport = false; self.ReplaceTicketId = null; self.ReplacePreview = null; self.ReplaceGrant = null;
            self.RefreshProductEntry();
        }

        [EntitySystem]
        private static void Update(this ProductEntryPresentation self) => self.RefreshProductEntry();

        [EntitySystem]
        private static void Destroy(this ProductEntryPresentation self) => self.CloseProductEntry();

        public static void CloseProductEntry(this ProductEntryPresentation self)
        {
            self.PreferencesDraft = null;
            self.ClearLeaveConfirmation();
            self.AuthorizationResultSlot = self.AuthorizationResultProfile = self.AuthorizationOutputPath = null; self.AuthorizationResultRevision = 0;
            self.AuthorizationTask = AuthorizationViewTask.Home;
            self.AuthorizationIdentityExpanded = false; self.AuthorizationInspectedPath = null;
            self.ClaimDetailTicketId = self.ClaimDestinationSlot = null;
            self.Suspended = true;
            self.Page = ProductEntryPage.Home;
            self.ConfirmImport = false; self.ReplaceTicketId = null; self.ReplacePreview = null; self.ReplaceGrant = null;
            self.InspectedAuthorizationKind = SaveAuthorizationKind.None;
            self.InspectedIdentity = null; self.InspectedAuthorization = null;
            if (!self.LoadingHud && (UIGridKitchen)self.Hud == null)
            {
                return;
            }
            ++self.Epoch;
            ++self.ProgressSerial;
            self.ProgressVisible = self.ProgressCommitting = false;
            self.ProgressHeaders = Array.Empty<SoloSaveHeader>();
            self.InspectedIdentity = null;
            self.InspectedAuthorization = null;
            self.LoadingHud = false;
            self.InspectedAuthorizationKind = SaveAuthorizationKind.None;
            self.ConfirmImport = false; self.ReplaceTicketId = null; self.ReplacePreview = null; self.ReplaceGrant = null;
            self.Busy = false;
            ((Scene)self.IScene).GetComponent<UIComponent>()?.RemoveComponent<UIGridKitchen>();
            self.Hud = default;
        }

        public static void RefreshProductEntry(this ProductEntryPresentation self)
        {
            self.RefreshClaimInbox();
            if (self.RefreshLeaveConfirmation())
            {
                return;
            }
            if (self.ClaimsVisible)
            {
                if (((UIGridKitchen)self.Hud)?.Available != true)
                {
                    self.OpenProductEntry();
                }
                else if (self.ClaimsNeedsRefresh && !self.Busy && !self.ProgressVisible && self.ReplaceTicketId == null && self.ReplacePreview == null)
                {
                    self.BrowseClaims();
                }
                return;
            }
            ProductRuntimeComponent runtime = self.Root().GetComponent<ProductRuntimeComponent>();
            ClientSessionComponent client = runtime.Client;
            ProductRoomClient room = client?.GetComponent<ProductRoomClient>();
            GridClientComponent grid = client?.GetComponent<GridClientComponent>();
            if (self.ChoosingSolo || room?.Current.HasRestaurant == true &&
                (grid?.Confirmed == true || ((Scene)client.IScene).GetComponent<GridPresentationComponent>() != null))
            {
                self.CloseProductEntry();
                return;
            }
            bool show = runtime.Config.ShowModeMenu || runtime.Config.StartInRoom || room != null && room.Current.Version > 0 && !room.Current.HasRestaurant;
            if (!show)
            {
                return;
            }
            if (self.Suspended)
            {
                return;
            }
            UIGridKitchen hud = self.Hud;
            if (hud?.Available != true)
            {
                self.OpenProductEntry();
                return;
            }
            if (self.DisplayedStage != runtime.Stage || self.DisplayedClient != (client?.InstanceId ?? 0) ||
                self.DisplayedRosterVersion != (client?.RosterVersion ?? 0) || self.DisplayedRoomVersion != (room?.Current.Version ?? 0) ||
                self.DisplayedClientStage != (client?.Stage ?? default) || self.DisplayedRuntimeFailure != runtime.Failure)
            {
                self.RenderProductEntry();
            }
        }

        public static void OpenProductEntry(this ProductEntryPresentation self)
        {
            self.Suspended = false;
            if (self.LoadingHud || ((UIGridKitchen)self.Hud)?.Available == true || self.ChoosingSolo)
            {
                return;
            }
            self.LoadingHud = true;
            long epoch = ++self.Epoch;
            EntityRef<ProductEntryPresentation> original = self;
            self.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    Scene scene = (Scene)self.IScene;
                    UIComponent ui = scene.GetComponent<UIComponent>() ?? scene.AddComponent<UIComponent>();
                    // 上次加载若被受管关闭或失败，先退役本owner留下的不可用组件。
                    ui.RemoveComponent<UIGridKitchen>();
                    UIGridKitchen hud = await ui.AddUIFormComponentAsync<UIGridKitchen>(UGFUIFormId.UIGridKitchen);
                    context.ThrowIfStopped();
                    self = original;
                    if (self == null || self.Epoch != epoch || self.ChoosingSolo)
                    {
                        hud?.Dispose();
                        return;
                    }
                    self.Hud = hud;
                    hud.View.ConfigureHud();
                    self.RenderProductEntry();
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
                        self.LoadingHud = false;
                    }
                }
            });
        }

        private static void RenderProductEntry(this ProductEntryPresentation self)
        {
            UIGridKitchen hud = self.Hud;
            if (hud?.Available != true || self.ChoosingSolo)
            {
                return;
            }
            self.ApplySavedPreferences(hud.View);
            if (self.RenderPreferences(hud, () => self.RenderProductEntry()))
            {
                return;
            }
            ProductRuntimeComponent runtime = self.Root().GetComponent<ProductRuntimeComponent>();
            ClientSessionComponent client = runtime.Client;
            ProductRoomClient room = client?.GetComponent<ProductRoomClient>();
            bool modes = runtime.Config.ShowModeMenu;
            MonoUIGridKitchen view = hud.View;
            if (modes && !self.ClaimsVisible && self.Page == ProductEntryPage.Cooperation && view.EntryAddressVisible)
            {
                self.JoinAddress = view.EntryAddress;
            }
            if (self.ProgressVisible && view.EntryAddressVisible)
            {
                self.ProgressPath = view.EntryAddress;
            }
            ++self.ProgressRenderVersion;
            view.ClearTargets();
            view.ShowPanels(false, true);
            view.CameraDragAreaRectTransform.gameObject.SetActive(false);
            view.SetInformation("", "", false, false);
            view.ClockText.text = modes ? "选择开始方式" : "先集合，再开餐厅";
            view.ScoreText.text = "";
            view.HandText.text = "";
            view.WorkText.text = "";
            view.PhaseText.text = modes ? "CarzyCooker" : "合作房间";
            view.StatusText.text = self.Failure ?? runtime.Failure ?? (modes ? "选择单机，或和朋友合作" : "等待房间就绪");
            view.HintText.text = modes ? "合作首版支持同电脑地址加入" : "本轮内存餐厅 · 房间成员与餐厅分别保留";
            view.SettlementText.text = modes ? "单机可继续本机记录；合作可以先加入房间再开局。" : "连接中，可以取消并返回选择。";
            EntityRef<ProductEntryPresentation> original = self;
            EntityRef<UIGridKitchen> hudRef = hud;
            long epoch = self.Epoch;
            long clientInstance = client?.InstanceId ?? 0;
            long roomVersion = room?.Current.Version ?? 0;
            long renderVersion = self.ProgressRenderVersion;
            view.SetInputGuard(() =>
            {
                ProductEntryPresentation current = original;
                ProductRuntimeComponent owner = current?.Root().GetComponent<ProductRuntimeComponent>();
                ClientSessionComponent active = owner?.Client ?? default;
                return current != null && current.Epoch == epoch && current.ProgressRenderVersion == renderVersion &&
                    !current.ChoosingSolo && ((UIGridKitchen)hudRef)?.Available == true &&
                    (active?.InstanceId ?? 0) == clientInstance && (active?.GetComponent<ProductRoomClient>()?.Current.Version ?? 0) == roomVersion;
            });
            if (self.Page == ProductEntryPage.Home && !self.ProgressVisible && !self.ClaimsVisible && !self.Busy)
            {
                self.AddPreferencesControl(view, () => self.RenderProductEntry());
            }
            if (self.LeaveAction != ProductLeaveAction.None)
            {
                self.RenderLeaveConfirmation(view);
            }
            else if (self.ClaimsVisible)
            {
                self.RenderClaimsEntry(view);
            }
            else if (self.Page == ProductEntryPage.ConnectionHelp)
            {
                self.RenderConnectionHelp(view, runtime.Config.ShowModeMenu);
            }
            else if (modes)
            {
                self.RenderModeNavigation(view);
            }
            else
            {
                bool joined = client?.Stage == ProductClientStage.Joined && room?.Current.Version > 0;
                if (joined && self.ProgressVisible && !room.Current.HasRestaurant)
                {
                    self.RenderProgressEntry(view, client, room);
                }
                else if (joined)
                {
                    view.StatusText.text = self.Failure ?? (room.Current.HasRestaurant ? "正在装载餐厅…" : "已加入合作房间");
                    view.SettlementText.text = string.Join("\n", client.Roster.Values.OrderBy(member => member.MemberId)
                        .Select(member => $"玩家 {member.MemberId} · {(member.Online ? "在线" : "离线")}"));
                    if (runtime.Config.Role == RuntimeInstanceRole.Host)
                    {
                        view.HintText.text = "加入地址：" + runtime.ListenEndpoint;
                        self.AddEndpointCopy(view, runtime.ListenEndpoint);
                    }
                    self.AddEntryNavigation(view, "连接帮助", ProductEntryPage.ConnectionHelp);
                    if (!room.Current.HasRestaurant && (!self.Busy || self.Page == ProductEntryPage.StartPreview))
                    {
                        self.AddRoomStartNavigation(view, roomVersion, clientInstance);
                        if (self.Page == ProductEntryPage.Home)
                        {
                            self.AddRoomReadiness(view, client, room.Current);
                        }
                    }
                    if (!self.Busy && !room.Current.HasRestaurant)
                    {
                        self.AddProgressControl(view, "合作进度领取", current => current.OpenClaims());
                        self.AddClaimInboxControl(view);
                        self.AddProgressControl(view, "存档与身份", current =>
                        {
                            current.ProgressVisible = true;
                            current.ProgressPath = "";
                            current.NavigateAuthorizationTask(AuthorizationViewTask.Home);
                        });
                    }
                }
                else if (!self.Busy && runtime.Stage == ProductStage.Disconnected)
                {
                    view.StatusText.text = "连接已断开，可以恢复原成员或返回选择";
                    view.AddControl("恢复房间连接", () => { ProductEntryPresentation current = original; current?.ResumeRoomEntry(); });
                }
                else if (client?.Stage == ProductClientStage.Joining)
                {
                    view.StatusText.text = "正在等待目标确认加入";
                    view.SettlementText.text = "目标：" + runtime.Config.Address + ":" + runtime.Config.Port +
                        "\n尚未加入房间，可以取消。\n取消后可以修改地址重试，或选择单机。";
                }
                view.AddControl(client?.Stage == ProductClientStage.Joining ? "取消加入" : "返回模式选择", () =>
                {
                    ProductEntryPresentation current = original;
                    if (current != null)
                    {
                        if (client?.Stage == ProductClientStage.Joining || client == null)
                        {
                            current.LeaveRoomNormally();
                        }
                        else
                        {
                            current.RequestProductLeave(runtime.Config.Role == RuntimeInstanceRole.Host
                                ? ProductLeaveAction.HostEndRoom : ProductLeaveAction.ClientLeave);
                        }
                    }
                });
            }
            self.DisplayedStage = runtime.Stage;
            self.DisplayedClient = clientInstance;
            self.DisplayedRosterVersion = client?.RosterVersion ?? 0;
            self.DisplayedRoomVersion = roomVersion;
            self.DisplayedClientStage = client?.Stage ?? default;
            self.DisplayedRuntimeFailure = runtime.Failure;
        }

        private static void ChooseSoloEntry(this ProductEntryPresentation self)
        {
            if (self.Busy || self.ChoosingSolo)
            {
                return;
            }
            self.ChoosingSolo = true;
            self.CloseProductEntry();
            EventSystem.Instance.Publish(self.Root(), new SoloSaveSelectionRequested());
        }

        private static void ChooseCooperativeEntry(this ProductEntryPresentation self, bool join)
        {
            if (self.Busy || self.ChoosingSolo)
            {
                return;
            }
            try
            {
                ProductRuntimeComponent runtime = self.Root().GetComponent<ProductRuntimeComponent>();
                string endpoint = ((UIGridKitchen)self.Hud).View.EntryAddress;
                var next = RuntimeInstanceConfig.Create(Guid.NewGuid().ToString("N"), "launcher", join ? "client" : "host",
                    connect: join ? endpoint : null, startInRoom: true);
                self.Busy = ProductBootstrapBridge.SwitchInitialization(runtime.Config, next);
            }
            catch (Exception exception)
            {
                self.Failure = exception.Message;
                self.RenderProductEntry();
            }
        }

        public static void SendRoomEntry(this ProductEntryPresentation self, ProductRoomAction action, long expectedVersion, long expectedClient, ProductMenuVariant menuVariant = ProductMenuVariant.Drink, int contentChapterId = 0)
        {
            ProductRuntimeComponent runtime = self.Root().GetComponent<ProductRuntimeComponent>();
            ClientSessionComponent client = runtime.Client;
            ProductRoomClient room = client?.GetComponent<ProductRoomClient>();
            UIGridKitchen hud = self.Hud;
            bool ready = action is ProductRoomAction.Ready or ProductRoomAction.CancelReady;
            if (self.Busy || hud?.Available != true || client?.InstanceId != expectedClient || room?.Current.Version != expectedVersion || !ready && !room.Current.CanControlRoom)
            {
                return;
            }
            self.Busy = true;
            self.Failure = null;
            self.RenderProductEntry();
            EntityRef<ProductEntryPresentation> original = self;
            long epoch = self.Epoch;
            hud.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    string error = await client.CallProductValueAsync(context, client.CreateRoomCommand(action, menuVariant, contentChapterId), response => response.Error == 0 ? null : response.Message);
                    context.ThrowIfStopped();
                    self = original;
                    if (self != null && self.Epoch == epoch)
                    {
                        self.Failure = error switch
                        {
                            "RoomProposalRequired" => "请先由Host发布开局提案。",
                            "RoomProposalChanged" => "提案已经变化，请重新查看并准备。",
                            "MembersNotReady" => "还有当前连接成员未准备，Host不能强开。",
                            "RoomSourceChanged" => "来源记录或授权已经变化，请重新发布提案并准备。",
                            _ => error
                        };
                        if (error == null && action == ProductRoomAction.PublishProposal)
                        {
                            self.Page = ProductEntryPage.Home;
                        }
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
                        self.Busy = false;
                        self.RenderProductEntry();
                        self.RefreshProductEntry();
                    }
                }
            });
        }

        private static void ResumeRoomEntry(this ProductEntryPresentation self)
        {
            ProductRuntimeComponent runtime = self.Root().GetComponent<ProductRuntimeComponent>();
            UIGridKitchen hud = self.Hud;
            self.Busy = true;
            EntityRef<ProductEntryPresentation> original = self;
            long epoch = self.Epoch;
            hud.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    await runtime.ResumeProduct();
                    context.ThrowIfStopped();
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
                        self.Busy = false;
                        self.RenderProductEntry();
                        self.RefreshProductEntry();
                    }
                }
            });
        }
    }

    [Event(SceneType.ProductRoot)]
    public sealed class ProductEntryRequestedView : AEvent<Scene, ProductEntryRequested>
    {
        protected override UniTask Run(Scene scene, ProductEntryRequested args)
        {
            if (scene.GetComponent<ProductEntryPresentation>() == null)
            {
                scene.AddComponent<ProductEntryPresentation>();
            }
            return UniTask.CompletedTask;
        }
    }

    [Event(SceneType.ProductClient)]
    [FriendOf(typeof(ProductRoomClient))]
    [FriendOf(typeof(ProductEntryPresentation))]
    public sealed class ProductRoomAppliedView : AEvent<Scene, ProductRoomApplied>
    {
        protected override UniTask Run(Scene scene, ProductRoomApplied args)
        {
            ClientSessionComponent client = scene.GetComponent<ClientSessionComponent>();
            ProductRoomClient room = client.GetComponent<ProductRoomClient>();
            if (!room.Current.HasRestaurant)
            {
                scene.RemoveComponent<GridPresentationComponent>();
            }
            ProductEntryPresentation entry = scene.Root().GetComponent<ProductEntryPresentation>() ?? scene.Root().AddComponent<ProductEntryPresentation>();
            if (!room.Current.HasRestaurant && (!entry.Suspended || args.RestaurantRetired))
            {
                entry.OpenProductEntry();
            }
            entry.RefreshProductEntry();
            return UniTask.CompletedTask;
        }
    }
}
