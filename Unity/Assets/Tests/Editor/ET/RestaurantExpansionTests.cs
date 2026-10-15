using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test, Category("RestaurantExpansionCore")]
        public void RestaurantExpansionConfigHasOneBoundedAreaSingleSeatAndNoInitialFreeTable()
        {
            RenovationConfig.ValidateAll();
            FrontConfig.Validate(Tables.Instance.DTCustomerFlowConfig.DataList, Tables.Instance.DTOrderConfig.DataList, Tables.Instance.DTTableConfig.DataList);
            var rewards = Tables.Instance.DTRenovationConfig.DataList.Where(row => row.RewardKind == RenovationRewardKind.ExpandSpace).ToArray();
            Assert.That(rewards.Length, Is.EqualTo(1)); var reward = rewards.Single();
            Assert.That(reward.ExpansionWidth, Is.EqualTo(9)); Assert.That(reward.ExpansionHeight, Is.EqualTo(18));
            Assert.That(reward.Quantity, Is.EqualTo(1));
            Assert.That(KitchenGeometryConfig.ExpansionSeat(reward), Is.EqualTo(new GridCell(6, 3)));
            Assert.That(GridStationGeometry.Inside(new GridCell(7, 3)), Is.False);
            Assert.That(GridStationGeometry.Inside(new GridCell(7, 3), 9, 18), Is.True);
            var invalid = RenovationDefinition(reward, x: 8);
            var rows = Tables.Instance.DTRenovationConfig.DataList.Select(row => row.Id == reward.Id ? invalid : row).ToArray();
            Assert.Throws<InvalidOperationException>(() => RenovationConfig.Validate(rows, Tables.Instance.DTStationConfig.DataList, Tables.Instance.DTLevelConfig.DataList));
            StartGrowth();
            Assert.That(Grid.Width, Is.EqualTo(7)); Assert.That(Grid.Height, Is.EqualTo(18));
            Assert.That(Grid.Stations.Values.Count(value => ((GridStation)value).GetComponent<FrontTable>() != null), Is.EqualTo(2));
            Assert.That(Grid.Stations.Values.Any(value => ((GridStation)value).Cell == new GridCell(7, 3)), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("RestaurantExpansionCore")]
        public void RestaurantExpansionRealContinueRejectsObstructionThenInstallsOnceAndBothActorsUseNewGeometry(bool useRemote)
        {
            StartGrowth(true); var client = useRemote ? CoopRemote() : (ClientSessionComponent)host.Client;
            long bowl = PrepEggBowl(client), egg = Grid.Item(bowl).Contents.Single();
            PrepDo(client, GridAction.StartHeldWork, bowl, egg, bowl); var work = Grid.InputProcess(egg); PrepTick(5);
            PrepDo(client, GridAction.StopWork, input: egg, expectedTarget: bowl, process: work.Id);
            PrepDo(client, GridAction.PlaceCounter, PrepStation("碗台").Id, bowl);
            int progress = work.ProgressTicks;
            var items = Grid.Items.Keys.OrderBy(id => id).ToArray(); var oldStations = Grid.Stations.Keys.OrderBy(id => id).ToArray();
            RenovationComplete(); Assert.That(Renovation.Candidates, Does.Contain(8));
            RenovationChoose(8); Assert.That(Grid.Width, Is.EqualTo(7), "Selection is not installation");
            long level = CurrentLevel.Id;
            var source = Tables.Instance.DTRenovationConfig.Get(8);
            var rows = (List<DRRenovationConfig>)Tables.Instance.DTRenovationConfig.DataList;
            var map = (Dictionary<int, DRRenovationConfig>)Tables.Instance.DTRenovationConfig.DataMap;
            int index = rows.IndexOf(source);
            try
            {
                // 安装源夹具临时指向真实旧工位占地，实际有序继续请求必须完整拒绝。
                rows[index] = map[8] = RenovationDefinition(source, x: 5, y: 11);
                LevelDo(LevelControlAction.Continue, "RenovationBlocked");
                Assert.That(CurrentLevel.Id, Is.EqualTo(level)); Assert.That(Grid.Width, Is.EqualTo(7));
                Assert.That(Grid.Stations.Keys.OrderBy(id => id), Is.EqualTo(oldStations));
                Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
                Assert.That(work.ProgressTicks, Is.EqualTo(progress)); Assert.That(Renovation.SelectedDefinitionId, Is.EqualTo(8));
            }
            finally { rows[index] = map[8] = source; }
            var request = RenovationView.CreateLevelCommand(LevelControlAction.Continue); var replay = CopyReplayRequest(request);
            using (var reply = LevelReply(host.Client, request))
            {
                Assert.That(reply.Error, Is.Zero, reply.Message);
            }
            AdvanceTicks(1); Pump(20);
            using (var reply = LevelReply(host.Client, replay))
            {
                Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Executed));
            }
            var table = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Cell == new GridCell(7, 3));
            Assert.That(table.GetComponent<FrontTable>(), Is.Not.Null);
            Assert.That(table.FrontSeat(), Is.EqualTo(new GridCell(6, 3)));
            Assert.That(Grid.Width, Is.EqualTo(9)); Assert.That(Grid.Height, Is.EqualTo(18));
            Assert.That(Grid.SearchQueue.Length, Is.GreaterThanOrEqualTo(162));
            Assert.That(Grid.SearchPrevious.Length, Is.GreaterThanOrEqualTo(162));
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            Assert.That(Grid.Stations.Count, Is.EqualTo(oldStations.Length + 1));
            Assert.That(Renovation.AppliedStations[8], Is.EqualTo(new[] { table.Id }));
            Assert.That(work.State, Is.EqualTo(GridWorkState.Paused)); Assert.That(work.ProgressTicks, Is.EqualTo(progress));
            var projection = client.GetComponent<GridClientComponent>();
            Assert.That(projection.Width, Is.EqualTo(9)); Assert.That(projection.Height, Is.EqualTo(18));
            Assert.That(projection.Stations.ContainsKey(table.Id), Is.True);
            var queue = Grid.SearchQueue; var previous = Grid.SearchPrevious;
            PrepDo(client, GridAction.Move, cell: new GridCell(6, 3));
            Assert.That(Grid.Actor(client.MemberId).Cell, Is.EqualTo(new GridCell(6, 3)));
            Assert.That(Grid.FindGridPath(new GridCell(6, 3), new GridCell(7, 4)).Last(), Is.EqualTo(new GridCell(7, 4)));
            Assert.That(Grid.FindGridPath(new GridCell(6, 3), new GridCell(7, 4)), Does.Contain(new GridCell(6, 4)));
            LayoutDo(PrepStation("交接台"), new GridCell(6, 4));
            var rerouted = Grid.FindGridPath(new GridCell(6, 3), new GridCell(7, 4));
            Assert.That(rerouted, Is.Not.Null); Assert.That(rerouted.Contains(new GridCell(6, 4)), Is.False, "New queries cannot retain the old path through the installed obstacle");
            Assert.That(Grid.SearchQueue, Is.SameAs(queue)); Assert.That(Grid.SearchPrevious, Is.SameAs(previous));
        }
    }
}
