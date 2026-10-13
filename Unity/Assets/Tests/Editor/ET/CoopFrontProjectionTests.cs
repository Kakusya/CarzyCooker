using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test]
        public void CoopFrontProjectionRealOrderPoolWorkAndCompanionStayIndependentAfterDtoDisposal()
        {
            var remote = CoopFrontFixture();
            FrontWork clear = InitialDirtyTable(); long bowl = clear.ItemId;
            CoopFrontDo(remote, FrontWorkAction.Claim, clear); FrontWait(() => clear.State == FrontWorkState.Completed);
            Customer customer = InitialSeatedCustomer(); FrontWork ask = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId);
            Assert.That(FrontNpcReply(FrontRole, new FrontWorkIntent(FrontWorkAction.Claim, ask.Id, ask.ClaimSerial)).Error, Is.Zero);
            AdvanceTicks(1); FrontWait(() => ask.State == FrontWorkState.Completed);
            Order order = CurrentLevel.GetChild<Order>(customer.OrderId);
            FrontWork wash = CurrentLevel.Children.Values.OfType<FrontWork>().Single(value => value.Kind == FrontWorkKind.Wash);
            Assert.That(FrontNpcReply(FrontRole, new FrontWorkIntent(FrontWorkAction.Claim, wash.Id, wash.ClaimSerial)).Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            var peer = (HostPeer)((Match)Grid.Match).GetChild<Participant>(remote.MemberId).BoundPeer;
            var view = remote.GetComponent<GridClientComponent>();
            var snapshot = GridBroadcast.CreateSnapshot(Grid, peer); snapshot.Version = view.Version + 1;
            long pool = Grid.Item(bowl).HolderId, role = FrontRole.Id;
            view.ApplyGridSnapshot(snapshot);
            int pathCount = view.Companions[role].Path.Count; double progress = view.FrontWorks[wash.Id].Progress;
            snapshot.Companions.Single(value => value.RoleId == role).CompletedCount = 999;
            snapshot.Companions.Single(value => value.RoleId == role).Path.Clear();
            snapshot.Customers.Single(value => value.CustomerId == customer.Id).Phase = (int)CustomerPhase.Gone;
            snapshot.Orders.Single(value => value.OrderId == order.Id).Score = 999;
            snapshot.FrontWorks.Single(value => value.WorkId == wash.Id).Progress = 99;
            snapshot.Stations.Single(value => value.StationId == pool).PoolItemIds.Clear();
            snapshot.Tables.Single(value => value.StationId == customer.TableStationId).CustomerId = 0;
            snapshot.WaitingCustomers.Add(999); snapshot.Dispose();
            Assert.That(view.Confirmed, Is.True); Assert.That(view.CanControlRestaurant, Is.False);
            Assert.That(view.Companions[role].CompletedCount, Is.EqualTo(1)); Assert.That(view.Companions[role].Path.Count, Is.EqualTo(pathCount));
            Assert.That(view.Customers[customer.Id].Phase, Is.EqualTo(CustomerPhase.Ordered));
            Assert.That(view.Orders[order.Id].Score, Is.EqualTo(order.Score)); Assert.That(view.FrontWorks[wash.Id].Progress, Is.EqualTo(progress));
            Assert.That(view.Stations[pool].PoolItemIds, Is.EqualTo(new[] { bowl }));
            Assert.That(view.Tables[customer.TableStationId].CustomerId, Is.EqualTo(customer.Id)); Assert.That(view.WaitingCustomers, Is.Empty);
            Assert.That(view.Items[bowl].Hygiene, Is.EqualTo(ItemHygiene.Dirty));
        }
    }
}
