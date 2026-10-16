using System;
using System.Linq;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void ResetSupplyOverview(this GridPresentationComponent self)
        {
            self.SupplyOverviewSourceId = 0;
            self.SupplyOverviewShipmentId = 0;
        }

        private static string SupplyStageDescription(ShipmentStage stage) => stage switch
        {
            ShipmentStage.InTransit => "运输中",
            ShipmentStage.Ready => "待接收",
            ShipmentStage.Received => "已接收",
            _ => "状态已变化"
        };

        private static void RefreshSupplyOverview(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OverviewPage != GridOverviewPage.Supply)
            {
                return;
            }
            string phase = grid.LevelPaused ? "已暂停：运输冻结，请先恢复" : grid.LevelPhase switch
            {
                LevelPhase.Completed => "已结算：只读，运输不推进",
                LevelPhase.Draining => "正在收尾：不能新采购，已受理货物继续到货",
                _ => "采购、接收与拆份仍使用原单次按钮"
            };
            if (!grid.SupplySources.TryGetValue(self.SupplyOverviewSourceId, out var source))
            {
                view.SetInformation("补货总览", "请选择来源，分别查看外部配额、运输和实物。\n" + phase +
                    "\n配额和未到货不属于可取库存。", true, self.OrdersVisible);
                return;
            }
            var definition = Tables.Instance.DTSupplyConfig.Get(source.Id);
            int transit = 0, ready = 0, received = 0, packages = 0, portions = 0;
            foreach (var shipment in grid.SupplyShipments.Values)
            {
                if (shipment.SourceId != source.Id)
                {
                    continue;
                }
                if (shipment.Stage == ShipmentStage.InTransit)
                {
                    ++transit;
                }
                else if (shipment.Stage == ShipmentStage.Ready)
                {
                    ++ready;
                }
                else if (shipment.Stage == ShipmentStage.Received)
                {
                    ++received;
                }
            }
            // 从实际物品数包装；货物记录只用于关联，不与包装或内容再求和。
            foreach (var item in grid.Items.Values)
            {
                if (item.DefinitionId == definition.PackageDefinitionId && grid.SupplyShipments.Values.Any(shipment =>
                    shipment.SourceId == source.Id && shipment.Stage == ShipmentStage.Received && shipment.PackageItemId == item.Id))
                {
                    ++packages; portions += item.Contents.Count;
                }
            }
            string details = $"外部配额：{source.RemainingPackages}/{definition.AvailablePackages} 包" +
                $"\n运输中 {transit} · 待接收 {ready}" + $"\n已接收记录 {received} 条（不是额外实物）" +
                $"\n现存关联包装 {packages} 个 · 包内材料 {portions} 份\n" + phase;
            if (source.RemainingPackages == 0)
            {
                details += "\n配额已用尽，原采购会拒绝";
            }
            if (!grid.ProductionPermit.ItemDefinitions.Contains(definition.MaterialDefinitionId))
            {
                details += "\n本关生产许可未开放该原料";
            }
            if (self.SupplyOverviewShipmentId != 0)
            {
                if (grid.SupplyShipments.TryGetValue(self.SupplyOverviewShipmentId, out var shipment))
                {
                    details += "\n所选货物：" + SupplyStageDescription(shipment.Stage);
                    if (shipment.Stage == ShipmentStage.InTransit)
                    {
                        details += $" · 剩余 {Math.Max(0, shipment.RequiredTicks - shipment.ProgressTicks) * grid.TickMilliseconds / 1000f:0.0} 秒";
                    }
                    if (shipment.Stage == ShipmentStage.Ready && grid.Stations.TryGetValue(shipment.ReceivingStationId, out var receiving) && receiving.ItemId != 0)
                    {
                        details += "\n收货台已满，请先腾空；同一货物仍等待接收";
                    }
                }
                else
                {
                    details += "\n所选货物已离开当前上下文，请重新选择";
                }
            }
            view.SetInformation("补货总览 · " + definition.Name, details, true, self.OrdersVisible);
        }

        private static void AddSupplyOverview(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            EntityRef<GridPresentationComponent> original = self;
            bool shown = self.OverviewPage == GridOverviewPage.Supply;
            view.AddTarget(shown ? "关闭补货总览" : "补货总览", () =>
            {
                GridPresentationComponent current = original;
                if (current == null)
                {
                    return;
                }
                current.CloseItemLocator();
                current.ClearOrderNavigation();
                current.ResetSupplyOverview();
                current.OverviewPage = shown ? GridOverviewPage.None : GridOverviewPage.Supply;
                current.RecipeVisible = current.DetailsVisible = false;
                current.CompanionChat = CompanionChatTopic.None;
                current.ActionGroup = GridActionGroup.Supply;
                current.RefreshGridHud();
            });
            if (!shown)
            {
                return;
            }
            foreach (var source in grid.SupplySources.Values.OrderBy(value => value.Id))
            {
                view.AddTarget("来源 · " + Tables.Instance.DTSupplyConfig.Get(source.Id).Name, () =>
                {
                    GridPresentationComponent current = original;
                    if (current != null && grid.SupplySources.ContainsKey(source.Id))
                    {
                        current.SupplyOverviewSourceId = source.Id;
                        current.SupplyOverviewShipmentId = 0;
                        current.RefreshGridHud();
                    }
                });
            }
            int index = 0;
            foreach (var shipment in grid.SupplyShipments.Values.Where(value => self.SupplyOverviewSourceId == 0 || value.SourceId == self.SupplyOverviewSourceId).OrderBy(value => value.Id))
            {
                view.AddTarget($"货物 {++index} · {Tables.Instance.DTSupplyConfig.Get(shipment.SourceId).Name} · {SupplyStageDescription(shipment.Stage)}", () =>
                {
                    GridPresentationComponent current = original;
                    if (current != null && grid.SupplyShipments.ContainsKey(shipment.Id))
                    {
                        current.SupplyOverviewSourceId = shipment.SourceId;
                        current.SupplyOverviewShipmentId = shipment.Id;
                        current.RefreshGridHud();
                    }
                });
                if (shipment.Stage == ShipmentStage.Received && grid.Items.TryGetValue(shipment.PackageItemId, out var package))
                {
                    view.AddTarget("实物包装 · " + package.Name + " · 余 " + package.Contents.Count + " 份", () =>
                    {
                        GridPresentationComponent current = original;
                        if (current != null && grid.Items.ContainsKey(package.Id))
                        {
                            current.OpenItemLocator(grid, package.Id);
                        }
                    });
                }
            }
        }
    }
}
