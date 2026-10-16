using System;
using System.Linq;

namespace ET.Client
{
    public static partial class ProductEntryPresentationSystem
    {
        private static bool ClaimMatchesFilter(ProgressTicketSummary row, ClaimListFilter filter) => filter switch
        {
            ClaimListFilter.Available => row.State == ProgressReceiptState.Available,
            ClaimListFilter.Claimed => row.State == ProgressReceiptState.Claimed,
            ClaimListFilter.Closed => row.State == ProgressReceiptState.Closed,
            _ => true
        };

        private static void AddClaimFilters(this ProductEntryPresentation self, MonoUIGridKitchen view)
        {
            Add("全部结果", ClaimListFilter.All);
            Add("仅可领取", ClaimListFilter.Available);
            Add("仅已领取", ClaimListFilter.Claimed);
            Add("仅已关闭", ClaimListFilter.Closed);
            int count = self.ClaimRows.Count(row => ClaimMatchesFilter(row, self.ClaimFilter));
            view.SettlementText.text += count == 0 ? "\n当前分类没有结果，可切换分类或刷新" : "\n当前分类 " + count + " 项；查看不会领取";
            void Add(string caption, ClaimListFilter filter)
            {
                self.ClaimsButton(view, caption, current =>
                {
                    current.ClaimFilter = filter;
                    current.ClaimDetailTicketId = null;
                    current.RenderProductEntry();
                });
            }
        }

        private static void OpenClaimDetail(this ProductEntryPresentation self, string ticket)
        {
            if (!self.ClaimRows.Any(row => row.TicketId == ticket))
            {
                return;
            }
            self.ClaimDetailTicketId = ticket;
            self.RenderProductEntry();
        }

        private static bool RenderClaimDetail(this ProductEntryPresentation self, MonoUIGridKitchen view)
        {
            var row = self.ClaimRows.SingleOrDefault(value => value.TicketId == self.ClaimDetailTicketId);
            if (row == null)
            {
                self.ClaimDetailTicketId = null;
                self.ProgressFeedback = "结果列表已变化，请重新选择";
                return false;
            }
            var level = Tables.Instance.DTLevelConfig.GetOrDefault(row.CompletedLevelId);
            string name = level?.ChapterId > 0 ? RestaurantChapters.Find(level)?.Name : null;
            string state = row.State switch
            {
                ProgressReceiptState.Available => "可领取：本机凭证已持久化，尚未新建个人记录",
                ProgressReceiptState.Claimed => "已领取：重复查询仍指向同一记录",
                _ => "已关闭：本机已持久观察整个房间正常结束，不再开放领取"
            };
            view.SettlementText.text = "合作结果详情 · 只读\n" + (name == null ? "关卡 " + row.CompletedLevelId : name + " · 第 " + level.ChapterStage + " 关") +
                $"\n{row.Score} 分 · {row.Stars} 星\n" + state + "\n结果：" + row.SaveResultId + "\n房间：" + row.MatchId +
                (row.State == ProgressReceiptState.Claimed ? "\n本机记录：" + row.SlotId : "") + "\n仅代表本设备实际记录，不承诺其他设备的领取状态。";
            if (row.State == ProgressReceiptState.Available)
            {
                if (self.AuthorizationTask == AuthorizationViewTask.ReplaceRecord)
                {
                    self.ClaimsButton(view, "选择此结果替换", current => current.ChooseReplaceTarget(row.TicketId));
                }
                else
                {
                    self.ClaimsButton(view, "领取此结果", current => current.ClaimProgress(row.TicketId));
                }
            }
            if (row.State == ProgressReceiptState.Claimed)
            {
                self.ClaimsButton(view, "查看已领记录", current => current.OpenClaimDestination(row.TicketId));
            }
            self.ClaimsButton(view, "返回领取列表", current =>
            {
                current.ClaimDetailTicketId = null;
                current.RenderProductEntry();
            });
            self.ClaimsButton(view, "返回经营或房间", current => current.CloseClaims());
            return true;
        }

        private static void OpenClaimDestination(this ProductEntryPresentation self, string ticket)
        {
            var row = self.ClaimRows.SingleOrDefault(value => value.TicketId == ticket);
            if (row?.State != ProgressReceiptState.Claimed || string.IsNullOrEmpty(row.SlotId))
            {
                return;
            }
            self.ClaimDestinationSlot = row.SlotId;
            self.ProgressVisible = true;
            self.ProgressHeaders = Array.Empty<SoloSaveHeader>();
            self.BrowseProgressRecords();
        }

        private static void NavigateClaimedSoloRecord(this ProductEntryPresentation self, string slot, int revision)
        {
            ProductRuntimeComponent runtime = self.Root().GetComponent<ProductRuntimeComponent>();
            if (self.Busy || self.ClaimDestinationSlot != slot || runtime.Config.Role != Game.RuntimeInstanceRole.Solo ||
                (ClientSessionComponent)runtime.Client != null || (Scene)runtime.HostScene != null ||
                !self.ProgressHeaders.Any(header => header.SlotId == slot && header.Revision == revision))
            {
                return;
            }
            self.ClaimsVisible = false;
            self.ChoosingSolo = true;
            self.CloseProductEntry();
            EventSystem.Instance.Publish(self.Root(), new SoloSaveSelectionRequested { SlotId = slot, Revision = revision });
        }
    }
}
