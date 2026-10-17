using System.Collections.Generic;
using System.Linq;

namespace ET.Client
{
    public static partial class ProductEntryPresentationSystem
    {
        private static void AddRoomReadiness(this ProductEntryPresentation self, MonoUIGridKitchen view, ClientSessionComponent client, ProductRoomProjection room)
        {
            if (!room.HasProposal)
            {
                view.SettlementText.text += "\n尚无开局提案。由Host选择章节、菜单或合法来源记录并发布；准备不会创建餐厅或保存。";
                return;
            }
            view.ClockText.text = "合作房间 / 提案 #" + room.ProposalVersion;
            string content = room.ProposalChapter == 0 ? "独立菜单试玩" : Tables.Instance.DTRestaurantChapterConfig.Get(room.ProposalChapter).Name;
            if (!string.IsNullOrEmpty(room.BaselineSlotId))
            {
                content = "来源 " + room.BaselineSlotId.Substring(0, 8) + " · 修订 " + room.BaselineRevision;
            }
            view.SettlementText.text = content + "\n从关 " + room.ProposalFirstLevel + " 开始 · 合作\n" +
                string.Join("\n", room.ReadyMembers.Select(member => "玩家 " + member.MemberId + (member.MemberId == client.MemberId ? "（你）" : "") +
                    (member.Online ? member.Ready ? " · 已准备" : " · 未准备" : " · 离线"))) +
                "\n所有当前连接成员（含Host）准备后才能开局。\n内容或成员变化清空准备，准备不创建餐厅或保存。";
            var visited = new HashSet<int>();
            var level = Tables.Instance.DTLevelConfig.Get(room.ProposalFirstLevel);
            while (level != null && visited.Add(level.Id))
            {
                foreach (int dish in level.AllowedDishIds.Concat(level.AllowedDrinkIds))
                {
                    string caption = "提案关 " + level.Id + " · " + Tables.Instance.DTDishConfig.Get(dish).Name;
                    view.AddTarget(caption, () => view.StatusText.text = "只读共享菜单 · " + caption);
                }
                if (level.IsChapterTerminal || level.NextLevelConfigId == 0)
                {
                    break;
                }
                level = Tables.Instance.DTLevelConfig.Get(level.NextLevelConfigId);
            }
            if (self.Busy)
            {
                return;
            }
            bool ownReady = room.ReadyMembers.Any(member => member.MemberId == client.MemberId && member.Binding == client.BindingGeneration && member.Online && member.Ready);
            EntityRef<ProductEntryPresentation> original = self;
            long instance = client.InstanceId;
            view.AddControl(ownReady ? "取消准备" : "准备本提案", () =>
            {
                ProductEntryPresentation current = original;
                current?.SendRoomEntry(ownReady ? ProductRoomAction.CancelReady : ProductRoomAction.Ready, room.Version, instance, room.ProposalMenu, room.ProposalChapter);
            });
            if (room.CanControlRoom)
            {
                if (room.AllReady)
                {
                    view.AddControl("开始本轮餐厅", () =>
                    {
                        ProductEntryPresentation current = original;
                        current?.SendRoomEntry(ProductRoomAction.StartRestaurant, room.Version, instance, room.ProposalMenu, room.ProposalChapter);
                    });
                }
                else
                {
                    view.AddControl("开局（等待全员准备）", () => view.StatusText.text = "尚有当前连接成员未准备；Host不能强开。");
                }
            }
        }
    }
}
