namespace ET.Client
{
    public static partial class ProductEntryPresentationSystem
    {
        private static void RenderConnectionHelp(this ProductEntryPresentation self, MonoUIGridKitchen view, bool modes)
        {
            view.ClockText.text = "合作 / 连接帮助";
            view.StatusText.text = "先核对地址适用范围，再确认加入";
            view.SettlementText.text = "127.0.0.1只指正在使用的这台电脑。\n同电脑的另一实例可粘贴房主复制的地址。\n跨电脑需要显式LAN启动和实际私网地址。\n连接未完成时可取消，返回后重新填写。\n本页不会测试网络，也不能判断防火墙。";
            self.AddEntryNavigation(view, modes ? "返回合作入口" : "返回房间首页",
                modes ? ProductEntryPage.Cooperation : ProductEntryPage.Home);
        }

        private static void AddEndpointCopy(this ProductEntryPresentation self, MonoUIGridKitchen view, string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                return;
            }
            EntityRef<ProductEntryPresentation> original = self;
            view.AddControl("复制加入地址", () =>
            {
                ProductEntryPresentation current = original;
                if (current != null)
                {
                    view.CopyPublicEndpoint(endpoint);
                    view.StatusText.text = "已复制当前房间地址";
                }
            });
        }
    }
}
