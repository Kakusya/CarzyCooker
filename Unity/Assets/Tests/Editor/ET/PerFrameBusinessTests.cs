using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test]
        [Category("PerFrameTraversal")]
        public void PerFrameTraversalGoneCustomerAndCancelledWorkKeepFollowingObjectsAdvancing()
        {
            StartSolo();LevelDo(LevelControlAction.Start);
            Customer departed=InitialSeatedCustomer();
            FrontWork cancelled=CurrentLevel.GetChild<FrontWork>(departed.AskWorkId);
            departed.Phase=CustomerPhase.Gone;cancelled.State=FrontWorkState.Working;
            Customer eating=InitialSeatedCustomer(2,12);
            eating.Phase=CustomerPhase.Eating;eating.RemainingEatTicks=2;eating.PhaseEnteredTick=Grid.Tick-1;
            Grid.AdvanceCustomers(CurrentLevel);
            Assert.That(departed.Phase,Is.EqualTo(CustomerPhase.Gone));
            Assert.That(eating.RemainingEatTicks,Is.EqualTo(1),"An exited customer must not suppress the next customer's step");
            FrontWork washing=InitialWashTarget();InitialTargetReady(washing);
            washing.Progress=.25;washing.FrozenTicks=100;washing.StartedTick=Grid.Tick-1;
            Grid.AdvanceFrontWork(CurrentLevel);
            Assert.That(cancelled.State,Is.EqualTo(FrontWorkState.Cancelled));
            Assert.That(washing.State,Is.EqualTo(FrontWorkState.Working));
            Assert.That(washing.Progress,Is.GreaterThan(.25),"Cancelling an earlier work must not skip the remaining work");
            Assert.That(FrontRole.WorkId,Is.EqualTo(washing.Id));
            Assert.That(CurrentLevel.GetComponent<FrontLevelState>().CustomerStepBuffer,Is.Empty);
            Assert.That(CurrentLevel.GetComponent<FrontWorkCommands>().WorkStepBuffer,Is.Empty);
        }

        [Test]
        [Category("PerFramePath")]
        public void PerFramePathConsecutiveSearchesKeepIndependentRoutesAndResetUnreachableWorkspace()
        {
            StartHost();
            GridCell from=(from y in Enumerable.Range(1,GridKitchenRules.Height-3)
                           from x in Enumerable.Range(1,GridKitchenRules.Width-3)
                           let cell=new GridCell(x,y)
                           where Grid.IsWalkable(cell) && Grid.IsWalkable(new GridCell(x,y+1)) &&
                                 Grid.IsWalkable(new GridCell(x+1,y)) && Grid.IsWalkable(new GridCell(x+1,y+1))
                           select cell).First();
            GridCell to=new(from.X+1,from.Y+1);
            var first=Grid.FindGridPath(from,to);var saved=first.ToArray();
            Assert.That(first,Is.EqualTo(new[]{from,new GridCell(from.X,from.Y+1),to}),"BFS preserves up before right for equal routes");
            for(int attempt=0;attempt<8;attempt++)
            {
                var reverse=Grid.FindGridPath(to,from);
                Assert.That(reverse.First(),Is.EqualTo(to));Assert.That(reverse.Last(),Is.EqualTo(from));
                reverse.Clear();Assert.That(first,Is.EqualTo(saved),"Later searches and external result edits cannot overwrite an earlier route");
            }
            var added=new System.Collections.Generic.List<GridCell>();
            try
            {
                foreach(GridCell cell in GridKitchenSystem.Neighbours(to))
                {
                    if(Grid.Blocked.Add(cell))
                    {
                        added.Add(cell);
                    }
                }
                Assert.That(Grid.IsWalkable(to),Is.True);
                Assert.That(Grid.FindGridPath(from,to),Is.Null,"Reachable goal validity does not imply a route through blocked neighbours");
            }
            finally
            {
                foreach(GridCell cell in added)Grid.Blocked.Remove(cell);
            }
            Assert.That(Grid.FindGridPath(from,to),Is.EqualTo(saved),"Unreachable search leaves no visited state in the next search");
            Assert.That(Grid.FindGridPath(from,from),Is.EqualTo(new[]{from}));
            Assert.That(Grid.FindGridPath(new GridCell(-1,0),to),Is.Null);
            Assert.That(first,Is.EqualTo(saved));
        }
    }
}
