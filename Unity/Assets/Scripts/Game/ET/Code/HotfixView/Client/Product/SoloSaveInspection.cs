using System;
using System.Collections.Generic;
using System.Linq;

namespace ET.Client
{
    public static partial class SoloSavePresentationSystem
    {
        private static bool SameSelectedSave(SoloSaveHeader shown, SoloSaveHeader current)
        {
            return shown != null && current != null && shown.SlotId == current.SlotId && shown.Revision == current.Revision &&
                shown.ProfileId == current.ProfileId && shown.ResultId == current.ResultId && shown.Version == current.Version &&
                shown.Platform == current.Platform && shown.RecordKind == current.RecordKind && shown.SavedUtcTicks == current.SavedUtcTicks &&
                shown.CompletedLevelId == current.CompletedLevelId && shown.NextLevelId == current.NextLevelId &&
                shown.Score == current.Score && shown.Stars == current.Stars && shown.SourceSlotId == current.SourceSlotId &&
                shown.SourceRevision == current.SourceRevision && shown.SourceProfileId == current.SourceProfileId &&
                shown.RootProfileId == current.RootProfileId && shown.RootSlotId == current.RootSlotId && shown.RootRevision == current.RootRevision &&
                shown.SourceResultId == current.SourceResultId && shown.AuthorizationId == current.AuthorizationId;
        }

        private static void ClearSaveInspection(this SoloSavePresentation self)
        {
            self.InspectedSave = null;
            self.SaveInspectionRows = Array.Empty<string>();
            self.ViewedResult = null;
            self.LabelDraft = null;
        }

        private static string[] SaveInspectionRows(SoloSaveHeader header, SoloSavePayload payload)
        {
            var rows = new List<string>
            {
                "记录 " + header.SlotId.Substring(0, 8) + " · 修订 " + header.Revision,
                "格式 v" + header.Version + " · 平台 " + header.Platform,
                "成功 " + header.ServedOrders + " 单 · 未满足 " + header.UnfulfilledOrders + " 单",
                header.SourceSlotId == "" ? "来源：本机新建" : "来源记录 " + header.SourceSlotId.Substring(0, 8) + " · 修订 " + header.SourceRevision,
                "工位 " + payload.Stations.Length + " · 物品 " + payload.Items.Length,
                "脏餐具／物品 " + payload.Items.Count(item => item.Hygiene == (int)ItemHygiene.Dirty),
                "手工进度 " + payload.ManualWork.Length + " · 烘烤进度 " + payload.Baking.Length
            };
            foreach (var batch in payload.Batches)
            {
                var recipe = Tables.Instance.DTGridAutoRecipeConfig.Get(batch.RecipeId);
                string name = Tables.Instance.DTGridItemConfig.Get(recipe.OutputDefinitionId).Name;
                rows.Add(name + (batch.State == (int)GridAutoState.Ready
                    ? $" · 剩余 {batch.RemainingPortions}/{batch.TotalPortions} 份"
                    : $" · 加工 {batch.ProgressTicks}/{batch.RequiredTicks} tick"));
            }
            foreach (var shipment in payload.Shipments)
            {
                string state = (ShipmentStage)shipment.Stage switch
                {
                    ShipmentStage.InTransit => "运输中",
                    ShipmentStage.Ready => "待接收",
                    ShipmentStage.Received => "已接收",
                    _ => "未知"
                };
                rows.Add(Tables.Instance.DTGridItemConfig.Get(shipment.MaterialDefinitionId).Name + " · " + state);
                if (shipment.Stage == (int)ShipmentStage.InTransit)
                {
                    rows.Add("剩余运输 " + (shipment.RequiredTicks - shipment.ProgressTicks) + " tick");
                }
            }
            foreach (var supply in payload.Supply)
            {
                var source = Tables.Instance.DTSupplyConfig.Get(supply.SourceId);
                rows.Add(Tables.Instance.DTGridItemConfig.Get(source.MaterialDefinitionId).Name + " · 供应余量 " + supply.RemainingPackages + " 包");
            }
            return rows.ToArray();
        }

        private static void RenderSaveInspection(this SoloSavePresentation self, MonoUIGridKitchen view)
        {
            SoloSaveHeader header = self.InspectedSave;
            view.ClockText.text = "单机 / 记录详情";
            view.HintText.text = "只读检查 · 确认前重新读取";
            view.StatusText.text = self.Failure == "RecordChanged" ? "记录已经变化，请重新核对再确认" : "完整载荷已校验，尚未创建餐厅";
            view.SettlementText.text = RestaurantChapters.Label(header.RecordKind == SoloSaveRecordKind.Continue ? header.NextLevelId : header.CompletedLevelId) +
                (header.RecordKind == SoloSaveRecordKind.Continue ? "\n可继续记录" : "\n大关已完成 · 仅供查看") +
                $" · {header.Score}分 · {header.Stars}星\n成功 {header.ServedOrders} 单 · 未满足 {header.UnfulfilledOrders} 单\n{new DateTime(header.SavedUtcTicks).ToLocalTime():yyyy-MM-dd HH:mm}\n实物、剩余份数和在途货见下方列表";
            EntityRef<SoloSavePresentation> original = self;
            self.AddSaveLabelEditor(view, header);
            if (header.RecordKind == SoloSaveRecordKind.Continue)
            {
                view.AddControl("确认继续此记录", () =>
                {
                    SoloSavePresentation current = original;
                    current?.ReadSoloSave(header.SlotId, true);
                });
            }
            view.AddControl("刷新记录", () => { SoloSavePresentation current = original; current?.BrowseSoloSelection(); });
            view.AddControl("返回记录列表", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    current.ClearSaveInspection();
                    current.Failure = null;
                    current.RenderSoloSelection();
                }
            });
            foreach (string row in self.SaveInspectionRows)
            {
                view.AddTarget(row, () => view.StatusText.text = "已校验 · " + row);
            }
        }

        private static void AddSaveFilters(this SoloSavePresentation self, MonoUIGridKitchen view)
        {
            Add("全部记录", SoloSaveListFilter.All);
            Add("可继续记录", SoloSaveListFilter.Continue);
            Add("已完成记录", SoloSaveListFilter.Completed);
            Add("收藏记录", SoloSaveListFilter.Favorites);
            void Add(string label, SoloSaveListFilter filter)
            {
                EntityRef<SoloSavePresentation> original = self;
                view.AddTarget(label, () =>
                {
                    SoloSavePresentation current = original;
                    if (current != null)
                    {
                        current.SaveFilter = filter;
                        current.RenderSoloSelection();
                    }
                });
            }
        }
    }
}
