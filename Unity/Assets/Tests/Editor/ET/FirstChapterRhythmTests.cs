using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test, Category("FirstChapterRhythm")]
        public void FirstChapterRhythmActualTablesLimitTimingToSoloAndKeepProductionClosure()
        {
            foreach (var row in new[] { (Id: 1101, Operating: 1800, Interval: 600, Wait: 1200), (Id: 1102, Operating: 2100, Interval: 440, Wait: 1000), (Id: 1103, Operating: 2400, Interval: 360, Wait: 900) })
            {
                var level = Tables.Instance.DTLevelConfig.Get(row.Id);
                var flow = Tables.Instance.DTCustomerFlowConfig.DataList.Single(value => value.LevelConfigId == row.Id);
                Assert.That(level.OperatingTicks, Is.EqualTo(row.Operating));
                Assert.That(flow.FirstArrivalTicks, Is.EqualTo(120));
                Assert.That(flow.ArrivalIntervalTicks, Is.EqualTo(row.Interval));
                Assert.That(flow.OrderWaitTicks, Is.EqualTo(row.Wait));
                Assert.That(flow.QueueCapacity, Is.EqualTo(2));
                Assert.That(flow.EatTicks, Is.EqualTo(60));
                Assert.That(level.NextLevelConfigId, Is.EqualTo(row.Id == 1103 ? 0 : row.Id + 1));
            }
            Assert.That(Tables.Instance.DTLevelConfig.Get(201).OperatingTicks, Is.EqualTo(400));
            Assert.That(Tables.Instance.DTLevelConfig.Get(2101).OperatingTicks, Is.EqualTo(500));
            Assert.That(Tables.Instance.DTLevelConfig.Get(2102).OperatingTicks, Is.EqualTo(650));
            Assert.That(Tables.Instance.DTLevelConfig.Get(2103).OperatingTicks, Is.EqualTo(800));
            GridDishConfig.ValidateAllMenus();
            RestaurantChapters.ValidateAll();
            FrontConfig.Validate(Tables.Instance.DTCustomerFlowConfig.DataList, Tables.Instance.DTOrderConfig.DataList, Tables.Instance.DTTableConfig.DataList);
        }
    }
}
