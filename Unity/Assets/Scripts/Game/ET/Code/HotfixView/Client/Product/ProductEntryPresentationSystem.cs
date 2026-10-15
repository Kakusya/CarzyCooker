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
            self.Hud = default;
            self.Epoch = 0;
            self.LoadingHud = self.Busy = false;
            self.Suspended = false;
            ProductRuntimeComponent runtime = self.Root().GetComponent<ProductRuntimeComponent>();
            self.ChoosingSolo = runtime.Config.Role == RuntimeInstanceRole.Solo && !runtime.Config.ShowModeMenu;
            self.Failure = runtime.Config.EntryFeedback;
            self.JoinAddress = "127.0.0.1:";
            self.DisplayedStage = (ProductStage)(-1);
            self.DisplayedRosterVersion = self.DisplayedRoomVersion = self.DisplayedClient = -1;
            self.DisplayedRuntimeFailure = null;
            self.ProgressVisible = self.ProgressCommitting = false;
            self.ProgressSerial = self.ProgressRenderVersion = 0;
            self.InspectedTrusted = false;
            self.ProgressPath = self.ProgressFeedback = self.InspectedSummary = null;
            self.ProgressHeaders = Array.Empty<SoloSaveHeader>();
            self.InspectedIdentity = null;
            self.InspectedAuthorization = null;
            self.ClaimsVisible = self.ClaimsNeedsRefresh = self.ClosingMatch = false;
            self.ClaimRows = Array.Empty<ProgressTicketSummary>();
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
            self.Suspended = true;
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
            ProductRuntimeComponent runtime = self.Root().GetComponent<ProductRuntimeComponent>();
            ClientSessionComponent client = runtime.Client;
            ProductRoomClient room = client?.GetComponent<ProductRoomClient>();
            bool modes = runtime.Config.ShowModeMenu;
            MonoUIGridKitchen view = hud.View;
            if (modes && !self.ClaimsVisible && view.EntryAddress.Length > 0)
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
            view.SetInputGuard(() =>
            {
                ProductEntryPresentation current = original;
                ProductRuntimeComponent owner = current?.Root().GetComponent<ProductRuntimeComponent>();
                ClientSessionComponent active = owner?.Client ?? default;
                return current != null && current.Epoch == epoch && !current.ChoosingSolo && ((UIGridKitchen)hudRef)?.Available == true &&
                    (active?.InstanceId ?? 0) == clientInstance && (active?.GetComponent<ProductRoomClient>()?.Current.Version ?? 0) == roomVersion;
            });
            if (self.ClaimsVisible)
            {
                self.RenderClaimsEntry(view);
            }
            else if (modes)
            {
                view.ShowEntryAddress(self.JoinAddress);
                view.AddControl("单机餐厅", () => { ProductEntryPresentation current = original; current?.ChooseSoloEntry(); });
                view.AddControl("创建合作房间", () => { ProductEntryPresentation current = original; current?.ChooseCooperativeEntry(false); });
                view.AddControl("加入合作房间", () => { ProductEntryPresentation current = original; current?.ChooseCooperativeEntry(true); });
                view.AddControl("合作进度领取", () => { ProductEntryPresentation current = original; current?.OpenClaims(); });
                view.AddControl("存档与授权", () => { ProductEntryPresentation current = original; current?.OpenAuthorizationFiles(); });
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
                    }
                    if (!self.Busy && !room.Current.HasRestaurant && room.Current.CanControlRoom)
                    {
                        view.AddControl("开启内存餐厅", () => { ProductEntryPresentation current = original; current?.SendRoomEntry(ProductRoomAction.StartRestaurant, roomVersion, clientInstance); });
                        view.AddControl("开启沙拉餐厅", () =>
                        {
                            ProductEntryPresentation current = original;
                            current?.SendRoomEntry(ProductRoomAction.StartRestaurant, roomVersion, clientInstance, ProductMenuVariant.Salad);
                        });
                        view.AddControl("开启经营成长餐厅", () =>
                        {
                            ProductEntryPresentation current = original;
                            current?.SendRoomEntry(ProductRoomAction.StartRestaurant, roomVersion, clientInstance, ProductMenuVariant.Growth);
                        });
                        view.AddControl("开启热餐餐厅", () =>
                        {
                            ProductEntryPresentation current = original;
                            current?.SendRoomEntry(ProductRoomAction.StartRestaurant, roomVersion, clientInstance, ProductMenuVariant.Hot);
                        });
                        view.AddControl("开启甜品餐厅", () =>
                        {
                            ProductEntryPresentation current = original;
                            current?.SendRoomEntry(ProductRoomAction.StartRestaurant, roomVersion, clientInstance, ProductMenuVariant.Dessert);
                        });
                        view.AddControl("开启咖啡餐厅", () =>
                        {
                            ProductEntryPresentation current = original;
                            current?.SendRoomEntry(ProductRoomAction.StartRestaurant, roomVersion, clientInstance, ProductMenuVariant.Cafe);
                        });
                        foreach (var chapter in Tables.Instance.DTRestaurantChapterConfig.DataList.OrderBy(row => row.Id))
                        {
                            int id = chapter.Id; ProductMenuVariant menu = chapter.MenuVariant;
                            view.AddControl($"开启大关 {id} · {chapter.Name}", () =>
                            {
                                ProductEntryPresentation current = original;
                                current?.SendRoomEntry(ProductRoomAction.StartRestaurant, roomVersion, clientInstance, menu, id);
                            });
                        }
                    }
                    if (!self.Busy && !room.Current.HasRestaurant)
                    {
                        self.AddProgressControl(view, "合作进度领取", current => current.OpenClaims());
                        self.AddProgressControl(view, "存档与身份", current =>
                        {
                            current.ProgressVisible = true;
                            current.ProgressPath = "";
                            current.RenderProductEntry();
                        });
                    }
                }
                else if (!self.Busy && runtime.Stage == ProductStage.Disconnected)
                {
                    view.StatusText.text = "连接已断开，可以恢复原成员或返回选择";
                    view.AddControl("恢复房间连接", () => { ProductEntryPresentation current = original; current?.ResumeRoomEntry(); });
                }
                view.AddControl(client?.Stage == ProductClientStage.Joining ? "取消加入" : "返回模式选择", () =>
                {
                    ProductEntryPresentation current = original;
                    if (current != null)
                    {
                        current.LeaveRoomNormally();
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
            if (self.Busy || hud?.Available != true || client?.InstanceId != expectedClient || room?.Current.Version != expectedVersion || !room.Current.CanControlRoom)
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
                    string error = await client.CallProductValueAsync(context, client.CreateRoomCommand(action, (int)menuVariant, contentChapterId), response => response.Error == 0 ? null : response.Message);
                    context.ThrowIfStopped();
                    self = original;
                    if (self != null && self.Epoch == epoch)
                    {
                        self.Failure = error;
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
            if (!room.Current.HasRestaurant)
            {
                entry.OpenProductEntry();
            }
            entry.RefreshProductEntry();
            return UniTask.CompletedTask;
        }
    }
}
