#if !DOTNET
using System.Linq;

namespace ET
{
    [FriendOf(typeof(FrontLevelState))]
    [FriendOf(typeof(FrontTable))]
    [FriendOf(typeof(CompanionRole))]
    [FriendOf(typeof(Companion))]
    [FriendOf(typeof(CompanionGrowth))]
    [FriendOf(typeof(FrontWork))]
    [FriendOf(typeof(Customer))]
    [FriendOf(typeof(Order))]
    [FriendOf(typeof(LevelBuff))]
    [FriendOf(typeof(CompanionPreferenceState))]
    [FriendOf(typeof(CooperativeOrderIntents))]
    [FriendOf(typeof(CooperativeTargetPings))]
    [FriendOf(typeof(CooperativeSupplyRequests))]
    public static partial class GridBroadcast
    {
        private static void CopyFrontState(GridKitchenComponent world, GridWorldSnapshot snapshot)
        {
            foreach (GridStationState state in snapshot.Stations)
            {
                GridStation station = world.Stations[state.StationId]; state.PoolItemIds.AddRange(station.PoolItemIds);
                FrontTable table = station.GetComponent<FrontTable>();
                if (table == null) continue;
                GridCell seat = station.FrontSeat();
                snapshot.Tables.Add(ProductMessages.Create<GridTableState>(value =>
                { value.StationId=station.Id;value.CustomerId=table.CustomerId;value.State=(int)table.State;value.SeatX=seat.X;value.SeatY=seat.Y; }));
            }
            foreach (GridItemState state in snapshot.Items) state.Hygiene = (int)((GridItem)world.Items[state.ItemId]).Hygiene;
            Level level = world.ProductLevel(); if (level == null) return;
            snapshot.CompanionPreference = (int)level.GetParent<RestaurantRuntime>().GetComponent<CompanionPreferenceState>().Value;
            foreach (var intent in level.GetComponent<CooperativeOrderIntents>().Members.Values)
            {
                snapshot.OrderIntents.Add(ProductMessages.Create<GridOrderIntentState>(value =>
                {
                    value.MemberId = intent.MemberId; value.BindingGeneration = intent.BindingGeneration;
                    value.OrderId = intent.OrderId; value.CustomerId = intent.CustomerId;
                }));
            }
            foreach (var ping in level.GetComponent<CooperativeTargetPings>().Members.Values)
            {
                snapshot.TargetPings.Add(ProductMessages.Create<GridTargetPingState>(value =>
                {
                    value.MemberId = ping.MemberId; value.BindingGeneration = ping.BindingGeneration;
                    value.TargetKind = (int)ping.Kind; value.TargetId = ping.TargetId;
                    value.Serial = ping.Serial; value.ExpiresTick = ping.ExpiresTick;
                }));
            }
            foreach (var request in level.GetComponent<CooperativeSupplyRequests>().Members.Values)
            {
                snapshot.SupplyRequests.Add(ProductMessages.Create<GridSupplyRequestState>(value =>
                {
                    value.MemberId = request.MemberId; value.BindingGeneration = request.BindingGeneration;
                    value.RequestId = request.Id; value.Revision = request.Revision; value.SourceId = request.SourceId;
                    value.State = (int)request.State; value.ShipmentId = request.ShipmentId;
                }));
            }
            LevelBuff buff=level.GetComponent<LevelBuff>();
            snapshot.BuffSeed=buff.Seed;snapshot.BuffDrawVersion=buff.DrawVersion;snapshot.SelectedBuffId=buff.SelectedId;snapshot.RequireBuffChoice=buff.RequireChoice;
            snapshot.BuffCandidates.AddRange(buff.Candidates);
            FrontLevelState front = level.GetComponent<FrontLevelState>();
            snapshot.WaitingCustomers.AddRange(front.Waiting); snapshot.SkippedArrivals = front.SkippedArrivals;
            CompanionGrowth growth = level.GetComponent<CompanionGrowth>();
            foreach (CompanionRole role in level.Children.Values.OfType<CompanionRole>())
            {
                Companion identity = role.Companion; growth.CompletedByCompanion.TryGetValue(identity.Id, out int count);
                var state = ProductMessages.Create<GridCompanionState>(value =>
                { value.RoleId=role.Id;value.CompanionId=identity.Id;value.DefinitionId=identity.DefinitionId;value.Name=identity.Name;
                    value.X=role.Cell.X;value.Y=role.Cell.Y;value.NextX=role.NextCell.X;value.NextY=role.NextCell.Y;
                    value.StepStartTick=role.StepStartTick;value.Motion=(int)role.Motion;value.WorkId=role.WorkId;value.ClaimSerial=role.ClaimSerial;value.CompletedCount=count; });
                foreach (GridCell cell in role.Path.Skip(role.PathIndex)) state.Path.Add(ProductMessages.Create<GridPathCell>(value=>{value.X=cell.X;value.Y=cell.Y;}));
                snapshot.Companions.Add(state);
            }
            foreach (FrontWork work in level.Children.Values.OfType<FrontWork>())
                snapshot.FrontWorks.Add(ProductMessages.Create<GridFrontWorkState>(value =>
                { value.WorkId=work.Id;value.Kind=(int)work.Kind;value.CustomerId=work.CustomerId;value.StationId=work.StationId;value.ItemId=work.ItemId;
                    value.State=(int)work.State;value.Progress=work.Progress;value.WorkerKind=(int)work.WorkerKind;value.WorkerId=work.WorkerId;
                    value.ClaimSerial=work.ClaimSerial;value.FrozenTicks=work.FrozenTicks;value.OperationX=work.OperationCell.X;value.OperationY=work.OperationCell.Y; }));
            foreach (Customer customer in level.Children.Values.OfType<Customer>().Where(value => value.Phase != CustomerPhase.Gone))
            {
                var state = ProductMessages.Create<GridCustomerState>(value =>
                { value.CustomerId=customer.Id;value.ArrivalSequence=customer.ArrivalSequence;value.Phase=(int)customer.Phase;
                    value.TableStationId=customer.TableStationId;value.OrderId=customer.OrderId;value.X=customer.Cell.X;value.Y=customer.Cell.Y;
                    value.NextX=customer.NextCell.X;value.NextY=customer.NextCell.Y;value.Motion=(int)customer.Motion;value.StepStartTick=customer.StepStartTick;value.RemainingEatTicks=customer.RemainingEatTicks; });
                foreach (GridCell cell in customer.Path.Skip(customer.PathIndex)) state.Path.Add(ProductMessages.Create<GridPathCell>(value=>{value.X=cell.X;value.Y=cell.Y;}));
                snapshot.Customers.Add(state);
            }
            foreach (Order order in level.Children.Values.OfType<Order>())
                snapshot.Orders.Add(ProductMessages.Create<GridOrderState>(value =>
                { value.OrderId=order.Id;value.CustomerId=order.CustomerId;value.TableStationId=order.TableStationId;value.DishId=order.DishId;
                    value.Score=order.Score;value.State=(int)order.State;value.DeadlineTick=order.DeadlineTick;value.ServedItemId=order.ServedItemId; }));
        }
    }
}
#endif
