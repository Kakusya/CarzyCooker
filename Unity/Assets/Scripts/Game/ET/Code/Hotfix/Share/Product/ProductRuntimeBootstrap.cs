#if !DOTNET
using System;
using System.Net;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game;

namespace ET
{
    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(ClientSessionComponent))]
    [FriendOf(typeof(Match))]
    public static class ProductRuntimeBootstrap
    {
        public static void InitializeProduct(this ProductRuntimeComponent self, bool joinLocal = true, ProductMenuVariant menuVariant = ProductMenuVariant.Basic, int contentChapterId = 0) =>
            self.InitializeProductCore(joinLocal, null, null, null, menuVariant, contentChapterId: contentChapterId);

        public static void InitializeProductRoom(this ProductRuntimeComponent self, bool joinLocal = true, ProductMenuVariant menuVariant = ProductMenuVariant.Drink)
        {
            if (self.Config.Role != RuntimeInstanceRole.Host)
            {
                throw new InvalidOperationException("HostRoomOnly");
            }
            self.InitializeProductCore(joinLocal, null, null, null, menuVariant, roomOnly: true);
        }

        public static void InitializeSavedProduct(this ProductRuntimeComponent self, string profile, SoloSaveHeader header = null, SoloSavePayload payload = null, bool joinLocal = true, ProductMenuVariant menuVariant = ProductMenuVariant.Basic, int contentChapterId = 0)
        {
            if (self.Config.Role != RuntimeInstanceRole.Solo || !SoloSaveValidation.IsIdentifier(profile)) throw new InvalidOperationException("SoloSaveOnly");
            if (header != null) SoloSaveValidation.Payload(header, payload, profile);
            else if (payload != null) throw new InvalidOperationException("MissingSaveHeader");
            if (header?.RecordKind == SoloSaveRecordKind.ChapterCompleted) throw new InvalidOperationException("ChapterCompleted");
            if (header != null && contentChapterId != 0)
            {
                throw new InvalidOperationException("SavedChapterAlreadySelected");
            }
            self.InitializeProductCore(joinLocal, profile, header, payload, menuVariant, contentChapterId: contentChapterId);
        }

        public static void RegisterProductEntry(this ProductRuntimeComponent self)
        {
            if (ProductBootstrapBridge.Root == self) return;
            EntityRef<ProductRuntimeComponent> selfRef = self;
            ProductBootstrapBridge.Register(self.Config, self, () =>
            {
                ProductRuntimeComponent current = selfRef;
                current?.StopProduct();
            });
        }

        private static void InitializeProductCore(this ProductRuntimeComponent self, bool joinLocal, string profile, SoloSaveHeader header, SoloSavePayload payload, ProductMenuVariant menuVariant, bool roomOnly = false, int contentChapterId = 0)
        {
            if ((Scene)self.HostScene != null || (Scene)self.ClientScene != null || self.Stage is ProductStage.Stopping or ProductStage.Stopped)
                throw new InvalidOperationException("ProductAlreadyStarted");
            self.RegisterProductEntry();
            Scene root = self.Root();
            try
            {
                if (self.Config.Role is RuntimeInstanceRole.Host or RuntimeInstanceRole.Solo)
                {
                    GridKitchenSystem.ValidateGridDefinitions(Tables.Instance.DTGridItemConfig.DataList, Tables.Instance.DTGridManualRecipeConfig.DataList);
                    GridKitchenSystem.ValidateGridSoupDefinitions(Tables.Instance.DTGridItemConfig.DataList, Tables.Instance.DTGridAutoRecipeConfig.DataList);
                    GridKitchenSystem.ValidateCleanupDefinitions(Tables.Instance.DTGridItemConfig.DataList, Tables.Instance.DTStationConfig.DataList);
                    GridKitchenSystem.ValidateStationConfiguration();
                    GridDishConfig.ValidateAllMenus();
                    if (self.Config.Role is RuntimeInstanceRole.Solo or RuntimeInstanceRole.Host)
                    {
                        RestaurantConfig.ValidateLevelDefinitions(Tables.Instance.DTLevelConfig.DataList, GridKitchenSystem.IsInitialGridWalkable);
                        CompanionConfig.Validate(Tables.Instance.DTCompanionConfig.DataList, GridKitchenSystem.IsInitialGridWalkable);
                        FrontConfig.Validate(Tables.Instance.DTCustomerFlowConfig.DataList, Tables.Instance.DTOrderConfig.DataList, Tables.Instance.DTTableConfig.DataList);
                        BuffConfig.ValidateAll();
                        RenovationConfig.ValidateAll();
                        RestaurantChapters.ValidateAll();
                    }
                    var firstDefinition = header != null ? Tables.Instance.DTLevelConfig.Get(header.NextLevelId)
                        : RestaurantChapters.First(contentChapterId, self.Config.Role, menuVariant);
                    Scene host = EntitySceneFactory.CreateScene(root, IdGenerater.Instance.GenerateId(), IdGenerater.Instance.GenerateInstanceId(), SceneType.ProductHost, "ProductHost");
                    self.HostScene = host;
                    Match match = host.AddChild<Match>();
                    if (profile != null) match.BindSoloSave(profile, header, payload);
                    match.RoomMenuVariant = menuVariant;
                    if (!roomOnly && self.Config.Role == RuntimeInstanceRole.Host)
                    {
                        match.PublishRoomProposal(menuVariant, contentChapterId, firstDefinition.Id);
                    }
                    else if (!roomOnly)
                    {
                        match.Restaurant = match.AddChild<RestaurantRuntime, DRLevelConfig>(firstDefinition);
                    }
                    HostSessionComponent authority = host.AddComponent<HostSessionComponent, Match, ProductRuntimeComponent>(match, self);
                    authority.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
                    self.Authority = authority;
                    if (self.Config.Role == RuntimeInstanceRole.Host)
                    {
                    var service = new KService(new IPEndPoint(IPAddress.Parse(self.Config.Address), self.Config.Port), NetworkProtocol.UDP, ServiceType.Outer);
                    try
                    {
                        host.AddComponent<NetComponent, AService, bool>(service, true);
                    }
                    catch
                    {
                        service.Dispose();
                        throw;
                    }
                    self.ListenEndpoint = ((UdpTransport)service.Transport).LocalEndPoint.ToString();
                    self.Report(ProductStage.HostListening);
                    Log.Info($"Product listening: {self.ListenEndpoint} match={match.MatchId}");
                    }
                    if (!joinLocal)
                    {
                        return;
                    }
                    ClientSessionComponent client = self.CreateClientScene();
                    HostPeer peer = ProductAuthority.CreatePeer(authority);
                    peer.AddComponent<LocalDelivery, ClientSessionComponent>(client);
                    LocalMessageComponent local = ((Scene)client.IScene).AddComponent<LocalMessageComponent, ClientSessionComponent, HostPeer>(client, peer);
                    client.Local = local;
                    client.JoinProduct().Forget(); // 首次 Call 入队后立即返回，让 Init 安装 Runner。
                }
                else
                {
                    ClientSessionComponent client = self.CreateClientScene();
                    IPAddress address = IPAddress.Parse(self.Config.Address);
                    var service = new KService(address.AddressFamily, NetworkProtocol.UDP, ServiceType.Outer);
                    try
                    {
                        NetComponent net = ((Scene)client.IScene).AddComponent<NetComponent, AService, bool>(service, false);
                        Session session = net.Create(new IPEndPoint(address, self.Config.Port));
                        session.AddComponent<ProductClientBinding, ClientSessionComponent>(client);
                        client.Session = session;
                    }
                    catch
                    {
                        service.Dispose();
                        throw;
                    }
                    client.JoinProduct().Forget();
                }
            }
            catch (Exception e)
            {
                self.Failure = $"ProductInitializeFailed: {e.Message}";
                RuntimeInstanceContext.Fail(self.Config, self.Failure);
                self.StopProduct();
                throw;
            }
        }

        private static ClientSessionComponent CreateClientScene(this ProductRuntimeComponent self)
        {
            Scene scene = EntitySceneFactory.CreateScene(self.Root(), IdGenerater.Instance.GenerateId(), IdGenerater.Instance.GenerateInstanceId(), SceneType.ProductClient, "ProductClient");
            self.ClientScene = scene;
            ClientSessionComponent client = scene.AddComponent<ClientSessionComponent, ProductRuntimeComponent>(self);
            client.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
            self.Client = client;
            return client;
        }

    }
}
#endif
