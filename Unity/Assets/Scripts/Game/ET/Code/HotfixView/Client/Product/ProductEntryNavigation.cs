using System.Linq;
using Game;

namespace ET.Client
{
    public static partial class ProductEntryPresentationSystem
    {
        private static void NavigateEntry(this ProductEntryPresentation self, ProductEntryPage page)
        {
            if (self.Busy || self.ChoosingSolo || self.ClaimsVisible || self.ProgressCommitting)
            {
                return;
            }
            UIGridKitchen hud = self.Hud;
            if (self.Page == ProductEntryPage.Cooperation && hud?.View.EntryAddressVisible == true)
            {
                self.JoinAddress = hud.View.EntryAddress;
            }
            self.Page = page;
            self.Failure = null;
            self.RenderProductEntry();
        }

        private static void AddEntryNavigation(this ProductEntryPresentation self, MonoUIGridKitchen view,
            string caption, ProductEntryPage page)
        {
            EntityRef<ProductEntryPresentation> original = self;
            view.AddControl(caption, () =>
            {
                ProductEntryPresentation current = original;
                current?.NavigateEntry(page);
            });
        }

        private static void RenderModeNavigation(this ProductEntryPresentation self, MonoUIGridKitchen view)
        {
            self.AddClaimInboxControl(view);
            EntityRef<ProductEntryPresentation> original = self;
            if (self.Page == ProductEntryPage.Cooperation)
            {
                view.ClockText.text = "开始 / 合作房间";
                view.SettlementText.text = "创建后先集合，再选择内容。\n加入地址示例：127.0.0.1:实际端口。\n127.0.0.1只指本电脑，不能交给另一台电脑使用。\n当前菜单用于同电脑加入；跨电脑需显式LAN启动。";
                if (self.Failure != null)
                {
                    view.StatusText.text = "地址未通过校验，请修改后重试";
                    view.SettlementText.text += "\n校验结果：" + self.Failure;
                }
                view.ShowEntryAddress(self.JoinAddress);
                view.AddControl("粘贴地址", () =>
                {
                    ProductEntryPresentation current = original;
                    if (current != null)
                    {
                        view.ShowEntryAddress(view.ReadClipboardAddress());
                        current.JoinAddress = view.EntryAddress;
                    }
                });
                view.AddControl("创建合作房间", () =>
                {
                    ProductEntryPresentation current = original;
                    current?.ChooseCooperativeEntry(false);
                });
                view.AddControl("加入合作房间", () =>
                {
                    ProductEntryPresentation current = original;
                    current?.ChooseCooperativeEntry(true);
                });
                self.AddEntryNavigation(view, "返回开始页", ProductEntryPage.Home);
                self.AddEntryNavigation(view, "连接帮助", ProductEntryPage.ConnectionHelp);
                return;
            }

            self.Page = ProductEntryPage.Home;
            view.ClockText.text = "开始 / 主页";
            view.SettlementText.text = "新开或继续本机餐厅，或进入合作房间。\n合作结果和授权文件可单独查看，不会因此开启餐厅。";
            view.AddControl("单机餐厅", () =>
            {
                ProductEntryPresentation current = original;
                current?.ChooseSoloEntry();
            });
            view.AddControl("继续记录", () =>
            {
                ProductEntryPresentation current = original;
                current?.ChooseSoloEntry();
            });
            self.AddEntryNavigation(view, "合作房间", ProductEntryPage.Cooperation);
            view.AddControl("合作进度领取", () =>
            {
                ProductEntryPresentation current = original;
                current?.OpenClaims();
            });
            view.AddControl("存档与授权", () =>
            {
                ProductEntryPresentation current = original;
                current?.OpenAuthorizationFiles();
            });
        }

        private static void AddRoomStartNavigation(this ProductEntryPresentation self, MonoUIGridKitchen view,
            long roomVersion, long clientInstance)
        {
            ProductRoomClient room = ((ClientSessionComponent)self.Root().GetComponent<ProductRuntimeComponent>().Client)?.GetComponent<ProductRoomClient>();
            if (self.Page == ProductEntryPage.StartPreview)
            {
                if (self.PreviewRoomVersion == roomVersion && self.PreviewClient == clientInstance)
                {
                    self.RenderRoomStartPreview(view, room?.Current.CanControlRoom == true);
                    return;
                }
                self.Page = ProductEntryPage.Home;
                view.StatusText.text = "房间已变化，请重新选择内容";
            }
            if (self.Page is not (ProductEntryPage.Chapters or ProductEntryPage.TrialMenus))
            {
                self.Page = ProductEntryPage.Home;
                self.AddEntryNavigation(view, "选择章节", ProductEntryPage.Chapters);
                self.AddEntryNavigation(view, "菜单试玩", ProductEntryPage.TrialMenus);
                return;
            }

            view.ClockText.text = self.Page == ProductEntryPage.Chapters ? "合作房间 / 选择章节" : "合作房间 / 菜单试玩";
            EntityRef<ProductEntryPresentation> original = self;
            if (self.Page == ProductEntryPage.Chapters)
            {
                foreach (var chapter in Tables.Instance.DTRestaurantChapterConfig.DataList.OrderBy(row => row.Id))
                {
                    int id = chapter.Id;
                    ProductMenuVariant menu = chapter.MenuVariant;
                    view.AddControl($"{(room?.Current.CanControlRoom == true ? "开启" : "查看")}大关 {id} · {chapter.Name}", () =>
                    {
                        ProductEntryPresentation current = original;
                        current?.PreviewRoomStart(roomVersion, clientInstance, menu, id);
                    });
                }
            }
            else
            {
                AddMenu("开启内存餐厅", ProductMenuVariant.Drink);
                AddMenu("开启沙拉餐厅", ProductMenuVariant.Salad);
                AddMenu("开启经营成长餐厅", ProductMenuVariant.Growth);
                AddMenu("开启热餐餐厅", ProductMenuVariant.Hot);
                AddMenu("开启甜品餐厅", ProductMenuVariant.Dessert);
                AddMenu("开启咖啡餐厅", ProductMenuVariant.Cafe);
                AddMenu("开启蒸食餐厅", ProductMenuVariant.Steamed);
                AddMenu("开启茶饮餐厅", ProductMenuVariant.Tea);
                view.SettlementText.text += "\n这些入口保留原菜单链，用于单独体验；内容大关请返回选择章节。";
            }
            self.AddEntryNavigation(view, "返回房间首页", ProductEntryPage.Home);

            void AddMenu(string caption, ProductMenuVariant menu)
            {
                view.AddControl(room?.Current.CanControlRoom == true ? caption : caption.Replace("开启", "查看"), () =>
                {
                    ProductEntryPresentation current = original;
                    current?.PreviewRoomStart(roomVersion, clientInstance, menu);
                });
            }
        }
    }
}
