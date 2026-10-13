using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test]
        public void CoopCoreOwnershipHostUsesOneRestaurantLevelKitchenAndOneBusinessTick()
        {
            StartHost(); Match match = ((HostSessionComponent)host.Authority).Match;
            Assert.That((RestaurantRuntime)match.Restaurant, Is.Not.Null); Assert.That(match.Children.Values.OfType<RestaurantRuntime>().Count(), Is.EqualTo(1));
            Assert.That(match.GetComponent<GridKitchenComponent>(), Is.Null);
            Assert.That(Grid.Parent, Is.SameAs(Restaurant)); Assert.That(CurrentLevel.Parent, Is.SameAs(Restaurant));
            Assert.That(Grid.Actors.Values.Select(value => (GridActor)value).All(actor => actor.Parent == CurrentLevel), Is.True);
            Assert.That(Grid.Stations.Values.Select(value => (GridStation)value).All(station => station.Parent == Grid), Is.True);
            Assert.That(((Scene)host.HostScene).GetComponent<NetComponent>(), Is.Not.Null);
            long tick = Grid.Tick; AdvanceTicks(1); Assert.That(Grid.Tick, Is.EqualTo(tick + 1), "HostSession legacy driver must not advance this restaurant.");
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing)); Assert.That(Grid.Items.Count, Is.EqualTo(7));
            Assert.That(Renovation.Candidates, Is.Empty); Assert.That(SaveBinding, Is.Null);
            Assert.That(((HostPeer)((LocalMessageComponent)((ClientSessionComponent)host.Client).Local).Peer).CanControlRestaurant(), Is.True);
        }

        [Test]
        public void CoopCoreOwnershipSoloKeepsNoListenerAndOneClockWithSharedRestaurant()
        {
            StartSolo(); Match match = ((HostSessionComponent)host.Authority).Match;
            Assert.That(match.GetComponent<GridKitchenComponent>(), Is.Null); Assert.That((RestaurantRuntime)match.Restaurant, Is.Not.Null);
            Assert.That(((Scene)host.HostScene).GetComponent<NetComponent>(), Is.Null);
            long tick = Grid.Tick; AdvanceTicks(1); Assert.That(Grid.Tick, Is.EqualTo(tick + 1));
            Assert.That(Grid.Stations.Count, Is.EqualTo(16)); Assert.That(Grid.Items.Count, Is.EqualTo(7));
        }
    }
}
