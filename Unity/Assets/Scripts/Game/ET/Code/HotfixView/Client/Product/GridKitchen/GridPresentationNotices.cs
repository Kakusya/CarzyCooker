using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [FriendOf(typeof(RestaurantNoticesComponent))]
    public static partial class GridPresentationSystem
    {
        private static void ResetNoticeView(this GridPresentationComponent self)
        {
            self.NoticeFeedback = null;
            self.NoticeText.Clear();
            if (self.OverviewPage == GridOverviewPage.Notices)
            {
                self.OverviewPage = GridOverviewPage.None;
            }
        }

        private static string NoticeKindCaption(RestaurantNoticeKind kind) => kind switch
        {
            RestaurantNoticeKind.Arrival => "到货",
            RestaurantNoticeKind.Processing => "完工",
            _ => "临期"
        };

        private static void RefreshNoticeView(this GridPresentationComponent self, MonoUIGridKitchen view)
        {
            if (self.OverviewPage != GridOverviewPage.Notices)
            {
                return;
            }
            var notices = ((Scene)self.IScene).GetComponent<RestaurantNoticesComponent>();
            var text = self.NoticeText;
            text.Clear();
            text.Append("仅列最近8条，不是完整历史。\n首次进入/恢复只建立基线，不重放过去事件。\n在本机设置中分别开关；查看不自动操作或暂停。\n");
            if (notices?.BaselineReady != true)
            {
                text.Append("等待当前提醒基线。");
            }
            else if (notices.Entries.Count == 0)
            {
                text.Append("当前没有提醒；已关闭类别不显示。");
            }
            else
            {
                for (int index = notices.Entries.Count - 1; index >= 0; --index)
                {
                    var notice = notices.Entries[index];
                    text.Append('#').Append(notice.Serial).Append(" · ").Append(notice.Caption).Append('\n');
                }
            }
            if (self.NoticeFeedback != null)
            {
                text.Append('\n').Append(self.NoticeFeedback);
            }
            view.SetInformation("营业提醒 · 只读", text.ToString(), true, self.OrdersVisible);
        }

        private static void ReadNotice(this GridPresentationComponent self, long serial)
        {
            var scene = (Scene)self.IScene;
            var grid = scene.GetComponent<ClientSessionComponent>()?.GetComponent<GridClientComponent>();
            var notices = scene.GetComponent<RestaurantNoticesComponent>();
            if (!self.Ready || grid?.Confirmed != true || notices == null)
            {
                return;
            }
            foreach (var notice in notices.Entries)
            {
                if (notice.Serial != serial)
                {
                    continue;
                }
                if (notice.Kind == RestaurantNoticeKind.Deadline && notice.LevelId == grid.LevelId &&
                    grid.Orders.TryGetValue(notice.TargetId, out var order) && order.CustomerId == notice.CustomerId &&
                    self.SelectOrderForReading(grid, order.Id, notice.CustomerId, order.DishId))
                {
                    self.RefreshGridHud();
                    return;
                }
                if (notice.Kind == RestaurantNoticeKind.Processing && notice.ItemId != 0 && grid.Items.ContainsKey(notice.ItemId))
                {
                    self.OpenItemLocator(grid, notice.ItemId);
                    return;
                }
                if (notice.Kind == RestaurantNoticeKind.Arrival && grid.SupplyShipments.TryGetValue(notice.TargetId, out var shipment))
                {
                    self.CloseItemLocator();
                    self.ClearOrderNavigation();
                    self.RecipeVisible = self.DetailsVisible = false;
                    self.CompanionChat = CompanionChatTopic.None;
                    self.OverviewPage = GridOverviewPage.Supply;
                    self.ActionGroup = GridActionGroup.Supply;
                    self.SupplyOverviewSourceId = shipment.SourceId;
                    self.SupplyOverviewShipmentId = shipment.Id;
                    self.RefreshGridHud();
                    return;
                }
                break;
            }
            self.NoticeFeedback = "原目标已变化或离开当前列表，请从当前事实重新选择；不会自动改选替代物。";
            self.RefreshGridHud();
        }

        private static void AddNoticeView(this GridPresentationComponent self, MonoUIGridKitchen view)
        {
            var notices = ((Scene)self.IScene).GetComponent<RestaurantNoticesComponent>();
            bool shown = self.OverviewPage == GridOverviewPage.Notices;
            string caption = "营业提醒（0）";
            if (notices?.BaselineReady != true)
            {
                caption = "营业提醒：同步中";
            }
            else if (notices.Entries.Count != 0)
            {
                var latest = notices.Entries[notices.Entries.Count - 1];
                caption = "提醒 #" + latest.Serial + " · " + NoticeKindCaption(latest.Kind);
            }
            EntityRef<GridPresentationComponent> original = self;
            view.AddControl(shown ? "关闭营业提醒" : caption, () =>
            {
                GridPresentationComponent current = original;
                if (current == null || !current.Ready)
                {
                    return;
                }
                current.ClearOperationHelp();
                current.ResetActionAdvice();
                current.ClearOrderNavigation();
                current.CloseItemLocator();
                current.RecipeVisible = current.DetailsVisible = false;
                current.CompanionChat = CompanionChatTopic.None;
                current.NoticeFeedback = null;
                current.OverviewPage = shown ? GridOverviewPage.None : GridOverviewPage.Notices;
                current.ActionGroup = GridActionGroup.FrontService;
                current.RefreshGridHud();
            });
            if (!shown || notices == null)
            {
                return;
            }
            for (int index = notices.Entries.Count - 1; index >= 0; --index)
            {
                var notice = notices.Entries[index];
                view.AddTarget("查看提醒 #" + notice.Serial + " · " + NoticeKindCaption(notice.Kind), () =>
                {
                    GridPresentationComponent current = original;
                    current?.ReadNotice(notice.Serial);
                }, group: GridActionGroup.FrontService);
            }
        }
    }

    [Event(SceneType.ProductClient)]
    public sealed class RestaurantNoticeViewChanged : AEvent<Scene, RestaurantNoticesChanged>
    {
        protected override UniTask Run(Scene scene, RestaurantNoticesChanged args)
        {
            scene.GetComponent<GridPresentationComponent>()?.RefreshGridHud();
            return UniTask.CompletedTask;
        }
    }
}
