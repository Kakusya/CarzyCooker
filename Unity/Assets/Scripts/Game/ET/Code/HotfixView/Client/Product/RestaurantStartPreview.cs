using System;
using System.Collections.Generic;
using System.Linq;
using Game;

namespace ET.Client
{
    // 从实际启动所用定义生成一次性目录文本，不保存业务状态。
    public static class RestaurantStartPreview
    {
        private static List<DRLevelConfig> Route(int chapterId, ProductMenuVariant menu, RuntimeInstanceRole role)
        {
            DRLevelConfig first = RestaurantChapters.First(chapterId, role, menu);
            var rows = new List<DRLevelConfig>();
            var visited = new HashSet<int>();
            DRLevelConfig level = first;
            while (level != null && visited.Add(level.Id))
            {
                rows.Add(level);
                if (level.IsChapterTerminal || level.NextLevelConfigId == 0)
                {
                    break;
                }
                level = Tables.Instance.DTLevelConfig.Get(level.NextLevelConfigId);
            }
            return rows;
        }

        public static string Describe(int chapterId, ProductMenuVariant menu, RuntimeInstanceRole role)
        {
            string name = chapterId == 0 ? "独立菜单试玩" : Tables.Instance.DTRestaurantChapterConfig.Get(chapterId).Name;
            return name + "\n共 " + Route(chapterId, menu, role).Count + " 关 · " +
                (role == RuntimeInstanceRole.Solo ? "单机" : "合作") + "\n各关菜单见下方列表，可滚动查看。\n";
        }

        public static void AddMenus(MonoUIGridKitchen view, int chapterId, ProductMenuVariant menu, RuntimeInstanceRole role)
        {
            foreach (DRLevelConfig row in Route(chapterId, menu, role))
            {
                foreach (int id in row.AllowedDishIds.Concat(row.AllowedDrinkIds))
                {
                    string label = "第 " + row.ChapterStage + " 关 · " + Tables.Instance.DTDishConfig.Get(id).Name;
                    view.AddTarget(label, () => view.StatusText.text = "只读菜单 · " + label);
                }
            }
        }
    }

    public static partial class SoloSavePresentationSystem
    {
        private static void PreviewSoloStart(this SoloSavePresentation self, bool persistent, ProductMenuVariant menu, int chapterId = 0)
        {
            if (self.Started || self.LoadingSelection)
            {
                return;
            }
            self.PreviewPersistent = persistent;
            self.PreviewMenuVariant = menu;
            self.PreviewChapterId = chapterId;
            self.StartPreviewVisible = true;
            self.RenderSoloSelection();
        }

        private static void RenderSoloStartPreview(this SoloSavePresentation self, MonoUIGridKitchen view)
        {
            bool persistent = self.PreviewPersistent;
            ProductMenuVariant menu = self.PreviewMenuVariant;
            int chapterId = self.PreviewChapterId;
            view.ClockText.text = "单机 / 开局预览";
            if (self.Page == SoloEntryPage.Chapters)
            {
                view.ShowEntryLayout(ProductEntryLayout.ChapterSelection);
                view.ClockText.text = "单机 / 选择章节";
                foreach (var chapter in Tables.Instance.DTRestaurantChapterConfig.DataList.OrderBy(row => row.Id))
                {
                    int id = chapter.Id;
                    ProductMenuVariant variant = chapter.MenuVariant;
                    EntityRef<SoloSavePresentation> selection = self;
                    view.AddEntryPrimary((id == chapterId ? "已选 · " : "选择 · ") + chapter.Name, () =>
                    {
                        SoloSavePresentation current = selection;
                        current?.PreviewSoloStart(persistent, variant, id);
                    });
                }
                self.AddSoloPage(view, "菜单试玩", SoloEntryPage.TrialMenus);
            }
            view.ScoreText.text = persistent ? "成功后保存" : "内存试玩";
            view.HintText.text = persistent ? "本轮成功后新增记录" : "本轮退出后不保留";
            view.StatusText.text = "阅读后确认开始，也可以返回重新选择";
            view.SettlementText.text = RestaurantStartPreview.Describe(chapterId, menu, RuntimeInstanceRole.Solo) +
                (persistent ? "成功结算后新建本机记录，不覆盖原记录。" : "内存试玩：退出后不保留记录。") +
                "\n当前尚未创建餐厅。";
            RestaurantStartPreview.AddMenus(view, chapterId, menu, RuntimeInstanceRole.Solo);
            EntityRef<SoloSavePresentation> original = self;
            view.AddControl("确认开始", () =>
            {
                SoloSavePresentation current = original;
                if (current?.StartPreviewVisible != true)
                {
                    return;
                }
                current.NewMenuVariant = menu;
                current.ChooseSoloNewGame(persistent, chapterId);
            });
            view.AddControl("返回开始方式", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    if (current.Page == SoloEntryPage.Chapters)
                    {
                        ProductBootstrapBridge.ReturnToModeMenu(current.Root().GetComponent<ProductRuntimeComponent>().Config);
                        return;
                    }
                    current.StartPreviewVisible = false;
                    current.RenderSoloSelection();
                }
            });
        }
    }

    public static partial class ProductEntryPresentationSystem
    {
        private static void PreviewRoomStart(this ProductEntryPresentation self, long version, long clientId,
            ProductMenuVariant menu, int chapterId = 0)
        {
            self.PreviewParent = self.Page;
            self.PreviewRoomVersion = version;
            self.PreviewClient = clientId;
            self.PreviewMenuVariant = menu;
            self.PreviewChapterId = chapterId;
            self.Page = ProductEntryPage.StartPreview;
            self.RenderProductEntry();
        }

        private static void RenderRoomStartPreview(this ProductEntryPresentation self, MonoUIGridKitchen view, bool canStart)
        {
            ProductMenuVariant menu = self.PreviewMenuVariant;
            int chapterId = self.PreviewChapterId;
            long version = self.PreviewRoomVersion, client = self.PreviewClient;
            view.ClockText.text = "合作房间 / 开局预览";
            view.SettlementText.text = RestaurantStartPreview.Describe(chapterId, menu, RuntimeInstanceRole.Host) +
                "本端目录预览；发布后成为全房间提案，仍须所有连接成员明确准备。\n合作运行的检查点由原领取／授权流程保存，不自动写入本机。";
            RestaurantStartPreview.AddMenus(view, chapterId, menu, RuntimeInstanceRole.Host);
            EntityRef<ProductEntryPresentation> original = self;
            if (canStart && !self.Busy)
            {
                view.AddControl("发布房间提案", () =>
                {
                    ProductEntryPresentation current = original;
                    current?.SendRoomEntry(ProductRoomAction.PublishProposal, version, client, menu, chapterId);
                });
            }
            else
            {
                view.StatusText.text = self.Busy ? "正在发布房间提案…" : "只读目录 · 不改变已发布提案";
            }
            if (!self.Busy)
            {
                self.AddEntryNavigation(view, "返回内容列表", self.PreviewParent);
            }
        }
    }
}
