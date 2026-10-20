using System;
using System.Collections.Generic;

namespace ET.Client
{
    [FriendOf(typeof(ProductEntryPresentation))]
    public static partial class GridPresentationSystem
    {
        // 逐项比较原结构键的字段，列表长度也参与比较；只保留显示依赖，不序列化世界状态。
        private static void DisplayField(List<(long Number,string Text)> fields,ref int index,ref bool changed,long number,string text=null)
        {
            var field=(number,text);
            if(index==fields.Count)
            {
                fields.Add(field);changed=true;
            }
            else if(!fields[index].Equals(field))
            {
                fields[index]=field;changed=true;
            }
            ++index;
        }
        private static bool EndDisplayFields(List<(long Number,string Text)> fields,int index,bool changed)
        {
            if(index<fields.Count)
            {
                fields.RemoveRange(index,fields.Count-index);changed=true;
            }
            return changed;
        }

        private static bool GridHudActionsChanged(this GridPresentationComponent self,GridClientComponent grid,GridActorProjection own,UIGridKitchen hud)
        {
            int index=0;bool changed=!self.HudTargetsValid;
            void Field(long number,string text=null)=>DisplayField(self.HudActionFields,ref index,ref changed,number,text);
            void Flag(bool flag)=>Field(flag?1:0);
            Field(hud.InstanceId);Field(self.HudLoadEpoch);Field(grid.InstanceId);
            Field(grid.WorldGeneration,grid.WorldId);Field(grid.BindingGeneration);Field(grid.MemberId);Flag(grid.Confirmed);
            Field(grid.LevelId);Field(grid.LevelGeneration);Field(grid.LevelConfigId);Field((int)grid.LevelPhase);Flag(grid.LevelPaused);
            Field(grid.LayoutVersion);Field(self.LayoutStationId);Field(self.SelectedStationId);Field(self.SelectedLevelId);Field(self.SelectedLevelGeneration);
            Field(grid.PermissionVersion);
            Field(own.HandItemId);Field(own.WorkId);Field(own.FrontWorkId);Field(own.FrontClaimSerial);
            Flag(self.DetailsVisible);Flag(self.OrdersVisible);Field(self.RecipeDishId);Flag(self.RecipeVisible);Flag(self.FirstOrderGuideVisible);
            Field(self.FirstGuideOrderId);Flag(self.FirstGuideNeedsOrderChoice);
            Field((int)self.CompanionChat);
            Field((int)self.OverviewPage);
            Field((int)grid.CompanionPreference);
            var notices = ((Scene)self.IScene).GetComponent<RestaurantNoticesComponent>();
            Field(notices?.InstanceId ?? 0);Field(notices?.Version ?? 0);Field(0,self.NoticeFeedback);
            Field((int)self.ServiceSection); Field(self.ServiceDishId); Flag(self.ServiceReturnVisible);
            Field((int)self.HelpTopic); Field(self.HelpSerial);
            Field(self.ActionAdviceSerial);
            // 说明入口及其回调绑定当前反馈；终态变化即使未改变实物也需更新。
            Field(grid.LastCommandId, grid.LastReason); Field((int)grid.LastStatus);
            Flag(self.StationInspectionGeometryVisible); Flag(self.StationInspectionDetailsVisible);
            Field(self.RenovationPreviewId); Field(self.RenovationPreviewStationId); Field(self.RenovationPreviewExpectedLevel);
            Field(self.RenovationPreviewVersion); Field(self.RenovationPreviewLevelId); Field(self.RenovationPreviewGeneration);
            Field(self.RecipeOrderId, self.OrderNavigationFeedback); Field(self.RecipeItemId); Flag(self.OrderRecipeDetailsVisible);
            Field(0,self.ItemLocatorQuery); Flag(self.ItemLocatorKind.HasValue);
            Flag(self.ItemLocatorDetailsVisible);
            if (self.ItemLocatorKind.HasValue)
            {
                Field((int)self.ItemLocatorKind.Value);
            }
            Field(self.SupplyOverviewSourceId); Field(self.SupplyOverviewShipmentId);
            Flag(self.RoomControlPending);
            Field(self.Root().GetComponent<ProductEntryPresentation>()?.ClaimInboxVersion ?? 0);
            Flag(grid.CanControlRestaurant);Field(grid.BuffDrawVersion);Field(grid.SelectedBuffId);Flag(grid.RequireBuffChoice);Flag(self.BuffCandidatesVisible);
            Field(grid.BuffCandidates.Count);for(int i=0;i<grid.BuffCandidates.Count;i++)Field(grid.BuffCandidates[i]);
            Field(grid.AllowedDishIds.Count);for(int i=0;i<grid.AllowedDishIds.Count;i++)Field(grid.AllowedDishIds[i]);
            // 具体投影字典保持安装顺序；动作创建仍保留原排序。无排序／枚举闭包的比较不分配字符串。
            Field(grid.Stations.Count);
            foreach(var station in grid.Stations.Values)
            {
                Field(station.Id,station.Name);Field(station.ItemId);Field(station.Cell.X);Field(station.Cell.Y);Field((int)station.Kind);
                Field((int)station.Orientation);Field(station.AutoProcessId);Field(station.BakeProcessId);Field((int)station.Capabilities);
                Field(station.UpgradeDefinitionId);Field(station.UpgradeLevel);Field(station.WaterDefinitionId);Field(station.WaterUnits);
                Field(station.PoolItemIds.Count);for(int i=0;i<station.PoolItemIds.Count;i++)Field(station.PoolItemIds[i]);
            }
            int drops=0;
            foreach(var item in grid.Items.Values)
            {
                if(item.Location==GridItemLocation.Ground && item.Cell==own.Cell)
                {
                    ++drops;
                }
            }
            Field(drops);
            foreach(var item in grid.Items.Values)
            {
                if(item.Location==GridItemLocation.Ground && item.Cell==own.Cell)
                {
                    Field(item.Id);
                }
            }
            Field(grid.Items.Count);
            foreach(var item in grid.Items.Values)
            {
                Field(item.Id,item.Name);Field(item.DefinitionId);Field((int)item.Location);Field(item.HolderId);Field((int)item.Hygiene);
                Field((int)item.Form);Flag(item.Discardable);Field(item.AutoProcessId);Field(item.BakeProcessId);Field(item.BoundOrderId);
                Field(item.Contents.Count);for(int i=0;i<item.Contents.Count;i++)Field(item.Contents[i]);
            }
            Field(grid.Processes.Count);
            foreach(var work in grid.Processes.Values)
            {
                Field(work.Id);Field((int)work.State);Field(work.WorkerMemberId);Field(work.StationId);Field(work.InputItemId);Field(work.TargetItemId);Field(work.RecipeId);
            }
            Field(grid.AutoProcesses.Count);
            foreach(var work in grid.AutoProcesses.Values)
            {
                Field(work.Id);Field((int)work.State);Field(work.RemainingPortions);Field(work.PotItemId);Flag(work.LockContainer);Field((int)work.RequiredCapability);
            }
            Field(grid.BakeProcesses.Count);
            foreach(var work in grid.BakeProcesses.Values){Field(work.Id);Field((int)work.State);Field(work.StationId);Field(work.InputItemId);}
            Field(grid.FrontWorks.Count);
            foreach(var work in grid.FrontWorks.Values)
            {
                Field(work.Id);Field((int)work.State);Field(work.ClaimSerial);Field(work.WorkerId);Field((int)work.Kind);Field(work.StationId);Field(work.ItemId);
            }
            Field(grid.Orders.Count);
            foreach(var order in grid.Orders.Values){Field(order.Id);Field((int)order.State);Field(order.CustomerId);Field(order.TableStationId);Field(order.DishId);}
            Field(grid.OrderIntents.Count);
            Field(grid.SupplyRequests.Count);
            foreach (var request in grid.SupplyRequests)
            {
                Field(request.MemberId); Field(request.Id); Field(request.Revision); Field(request.SourceId);
                Field((int)request.State); Field(request.ShipmentId);
            }
            Field(grid.TargetPings.Count);
            foreach (var ping in grid.TargetPings)
            {
                Field(ping.MemberId); Field(ping.Serial); Field((int)ping.Kind); Field(ping.TargetId);
            }
            foreach (var intent in grid.OrderIntents)
            {
                Field(intent.MemberId); Field(intent.BindingGeneration); Field(intent.OrderId); Field(intent.CustomerId);
            }
            Field(grid.SupplySources.Count);
            foreach(var source in grid.SupplySources.Values){Field(source.Id);Field(source.RemainingPackages);Field(source.StationId);}
            Field(grid.SupplyShipments.Count);
            foreach(var shipment in grid.SupplyShipments.Values)
            {
                Field(shipment.Id);Field((int)shipment.Stage);Field(shipment.PackageItemId);Field(shipment.SourceId);Field(shipment.ReceivingStationId);
            }
            Field(grid.RenovationChoiceVersion);Field(grid.RenovationLevelId);Field(grid.RenovationLevelGeneration);Field(grid.SelectedRenovationId);
            Field(grid.SelectedUpgradeStationId);Field(grid.PendingUpgradeExpectedLevel);
            Field(grid.RenovationCandidates.Count);for(int i=0;i<grid.RenovationCandidates.Count;i++)Field(grid.RenovationCandidates[i]);
            Field(grid.RenovationApplied.Count);foreach(int id in grid.RenovationApplied.Keys)Field(id);
            Field(grid.UpgradeTargets.Count);
            for(int i=0;i<grid.UpgradeTargets.Count;i++)
            {
                var target=grid.UpgradeTargets[i];Field(target.DefinitionId);Field(target.StationId);Field(target.ExpectedLevel);
            }
            Flag(grid.SaveEnabled);Field((int)grid.SaveState,grid.SaveResultId);
            Flag(grid.CooperativeSave);Flag(grid.RenovationSelectionFinalized);
            Field(grid.NextLevelConfigId);
            return EndDisplayFields(self.HudActionFields,index,changed);
        }

        private static bool GridHudInformationChanged(this GridPresentationComponent self,GridClientComponent grid,bool actionsChanged)
        {
            int index=0;bool changed=actionsChanged || !self.HudInformationValid;
            void Field(long number,string text=null)=>DisplayField(self.HudInformationFields,ref index,ref changed,number,text);
            Field((int)grid.LastStatus,grid.LastReason);Field(0,self.Failure);Field(0,grid.SaveFailure);Field(0,grid.RenovationFailure);
            Field(self.ItemLocatorSelectedId);
            Field(self.FrontWorkboardSelectedId);
            if (self.OverviewPage == GridOverviewPage.Service)
            {
                Field(grid.SkippedArrivals);
                Field(grid.WaitingCustomers.Count);
                foreach (long customerId in grid.WaitingCustomers)
                {
                    Field(customerId);
                }
                foreach (var customer in grid.Customers.Values)
                {
                    Field(customer.Id); Field((int)customer.Phase); Field(customer.ArrivalSequence);
                }
                foreach (var table in grid.Tables.Values)
                {
                    Field(table.Id); Field((int)table.State); Field(table.CustomerId);
                }
            }
            if (self.OverviewPage == GridOverviewPage.FrontWork)
            {
                Field(grid.Tables.Count);
                foreach (var table in grid.Tables.Values)
                {
                    Field(table.Id); Field((int)table.State); Field(table.CustomerId);
                }
                Field(grid.Customers.Count);
                foreach (var customer in grid.Customers.Values)
                {
                    Field(customer.Id); Field((int)customer.Phase); Field(customer.ArrivalSequence);
                }
                foreach (var work in grid.FrontWorks.Values)
                {
                    Field(work.FrozenTicks); Field((int)work.WorkerKind);
                }
            }
            if (self.OverviewPage == GridOverviewPage.Items)
            {
                foreach (var actor in grid.Actors.Values)
                {
                    Field(actor.MemberId); Field(actor.Cell.X); Field(actor.Cell.Y);
                }
            }
            Field(grid.LevelScore);Field(grid.LevelStars);Field(grid.ServedOrders);Field(grid.UnfulfilledOrders);
            if (self.OverviewPage == GridOverviewPage.Recap)
            {
                foreach (var order in grid.Orders.Values)
                {
                    Field(order.Id);Field(order.Score);
                }
            }
            Field(grid.LevelOperatingTicks);Field(grid.TickMilliseconds);
            Field((long)Math.Round(Math.Max(0,grid.LevelOperatingTicks-grid.LevelElapsedTicks)*grid.TickMilliseconds/100d,MidpointRounding.AwayFromZero));
            foreach(var work in grid.Processes.Values){Field(work.Id);Field(work.ProgressTicks);Field(work.RequiredTicks);}
            foreach(var work in grid.AutoProcesses.Values){Field(work.Id);Field(work.ProgressTicks);Field(work.RequiredTicks);}
            foreach(var work in grid.BakeProcesses.Values){Field(work.Id);Field(work.ProgressTicks);Field(work.RequiredTicks);}
            foreach(var work in grid.FrontWorks.Values){Field(work.Id);Field((long)Math.Round(work.Progress*100));}
            Field(self.CompanionFeedbackRevision);
            Field(grid.Companions.Count);
            foreach(var companion in grid.Companions.Values)
            {
                Field(companion.Id,companion.Name);Field(companion.WorkId);Field(companion.CompletedCount);
            }
            foreach(var order in grid.Orders.Values)
            {
                if(order.State!=OrderState.Waiting)
                {
                    continue;
                }
                Field(order.Id);Field((long)Math.Round(Math.Max(0,order.DeadlineTick-grid.Tick)*grid.TickMilliseconds/100d,MidpointRounding.AwayFromZero));
                Field(grid.Customers.TryGetValue(order.CustomerId,out var customer)?customer.ArrivalSequence:0);
            }
            foreach(var shipment in grid.SupplyShipments.Values){Field(shipment.Id);Field(shipment.ProgressTicks);Field(shipment.RequiredTicks);}
            Field(grid.LastKitchenOutcome.WorkId);Field((int)grid.LastKitchenOutcome.Kind);Field((int)grid.LastKitchenOutcome.Stage);
            Field(grid.LastFrontWorkOutcome.WorkId);Field((int)grid.LastFrontWorkOutcome.Kind);Field((int)grid.LastFrontWorkOutcome.Stage);
            return EndDisplayFields(self.HudInformationFields,index,changed);
        }
    }
}
