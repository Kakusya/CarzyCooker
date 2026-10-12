#if !DOTNET
using System.Linq;

namespace ET
{
    [FriendOf(typeof(FrontLevelState))]
    [FriendOf(typeof(Customer))]
    [FriendOf(typeof(Order))]
    [FriendOf(typeof(FrontTable))]
    public static partial class GridKitchenSystem
    {
        public static string QueueServe(this GridKitchenComponent self, HostPeer peer, ProductCommandContext context, ServeOrderIntent intent)
        {
            Level level = self.ProductLevel(); Match match = self.Match;
            if (level == null || self.Stopped || peer == null || !self.HasBinding(peer.MemberId, peer.Generation)) return "EntryClosed";
            string error = context.Validate(match.MatchId,self.WorldId,self.WorldGeneration,level.Id,level.Generation,peer.Generation);
            if (error != null) return error;
            string prior = peer.CheckProductCommand(context, ProductCommandKind.Serve, intent.Equals(peer.LastServeCommand), out bool replay);
            if (prior != null || replay) return prior;
            if (level.Paused) return "Paused";
            if (level.Phase != LevelPhase.Operating && level.Phase != LevelPhase.Draining) return "WrongPhase";
            if (intent.OrderId<=0 || intent.CustomerId<=0 || intent.TableStationId<=0 || intent.ItemId<=0) return "InvalidCommand";
            peer.RecordProductCommand(context,ProductCommandKind.Serve);peer.LastServeCommand=intent;
            level.GetComponent<FrontLevelState>().Serves.Enqueue(new ServeOrderEnvelope(context,intent,peer));
            return null;
        }

        public static void AdvanceServes(this GridKitchenComponent self, Level level)
        {
            var queue=level.GetComponent<FrontLevelState>().Serves;
            while(queue.Count>0)
            {
                ServeOrderEnvelope command=queue.Dequeue();HostPeer peer=command.Peer;
                string error=peer==null || !peer.EntryOpen || !self.HasBinding(peer.MemberId,command.Context.BindingGeneration) ? "EntryClosed"
                    : command.Context.Validate(((Match)self.Match).MatchId,self.WorldId,self.WorldGeneration,level.Id,level.Generation,peer.Generation);
                if(error==null)error=self.ApplyServe(level,self.Actor(peer.MemberId),command.Intent);
                peer?.RecordProductResult(command.Context.CommandId,error==null?GridResultStatus.Executed:GridResultStatus.Rejected,error);self.Dirty=true;
            }
        }

        private static string ApplyServe(this GridKitchenComponent self, Level level, GridActor actor, ServeOrderIntent intent)
        {
            if(level.Paused)return "Paused";
            if(level.Phase!=LevelPhase.Operating && level.Phase!=LevelPhase.Draining)return "WrongPhase";
            Order order=level.GetChild<Order>(intent.OrderId);Customer customer=level.GetChild<Customer>(intent.CustomerId);
            GridStation station=self.Station(intent.TableStationId);FrontTable table=station?.GetComponent<FrontTable>();
            if(order==null || customer==null || table==null || order.LevelGeneration!=level.Generation || customer.LevelGeneration!=level.Generation ||
                order.CustomerId!=customer.Id || order.TableStationId!=station.Id || customer.TableStationId!=station.Id ||
                customer.OrderId!=order.Id || table.CustomerId!=customer.Id)return "OrderChanged";
            if(order.State!=OrderState.Waiting || customer.Phase!=CustomerPhase.Ordered || self.Tick>=order.DeadlineTick)return "OrderEnded";
            if(actor==null || actor.Motion!=GridMotion.Standing || !self.AtStation(actor,station))return "RequiresTablePosition";
            if(actor.WorkId!=0 || actor.FrontWorkId!=0 || actor.HasIntent)return "WorkBusy";
            GridItem item=self.Item(intent.ItemId);
            if(actor.HandItemId!=intent.ItemId || item==null || item.Location!=GridItemLocation.Hand || item.HolderId!=actor.MemberId)return "ItemChanged";
            if(item.Hygiene!=ItemHygiene.Clean)return "DirtyUtensil";
            if(station.ItemId!=0 || table.State!=FrontTableState.Occupied)return "TableBusy";
            if(self.IdentifyDish(item.Id)!=order.DishId)return "WrongDish";
            if(!level.AllowedDishIds.Contains(order.DishId))return "OrderUnavailable";
            GridItem food=self.Item(item.Contents.Single());
            item.Contents.Clear();self.Items.Remove(food.Id);food.Dispose();
            actor.HandItemId=0;item.Location=GridItemLocation.Table;item.HolderId=station.Id;item.Cell=station.Cell;station.ItemId=item.Id;
            order.State=OrderState.Served;order.ServedItemId=item.Id;order.ServedTick=self.Tick;level.Score+=order.Score;
            customer.Phase=CustomerPhase.Eating;customer.PhaseEnteredTick=self.Tick;
            customer.RemainingEatTicks=Tables.Instance.DTCustomerFlowConfig.Get(customer.FlowDefinitionId).EatTicks;
            return null;
        }

        public static void CancelPendingServes(this GridKitchenComponent self,Level level,string reason)
        {
            var queue=level.GetComponent<FrontLevelState>().Serves;
            while(queue.Count>0){ServeOrderEnvelope command=queue.Dequeue();HostPeer peer=command.Peer;peer?.RecordProductResult(command.Context.CommandId,GridResultStatus.Cancelled,reason);}
            self.Dirty=true;
        }

        private static string TakeCleanUtensil(this GridKitchenComponent self,GridActor actor,GridStation pool,GridIntent intent)
        {
            GridItem item=self.Item(intent.ExpectedItemId);
            if(actor.HandItemId!=0)return "HandFull";
            if(item==null || item.Location!=GridItemLocation.CleanPool || item.HolderId!=pool.Id || item.Hygiene!=ItemHygiene.Clean ||
                pool.PoolItemIds.Count(id=>id==item.Id)!=1)return "ItemChanged";
            self.PauseWork(actor);pool.PoolItemIds.Remove(item.Id);actor.HandItemId=item.Id;item.Location=GridItemLocation.Hand;item.HolderId=actor.MemberId;
            return null;
        }
    }
}
#endif
