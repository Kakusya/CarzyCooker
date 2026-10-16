#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest, Category("ProductEnumDirectionAndLegacy")]
        public IEnumerator ProductEnumsOrdinaryLauncherRotationButtonUsesTypedLayoutAndProjection()
        {
            yield return StartLauncher();
            var oven = Grid.Stations.Values.Single(value => value.Kind == GridStationKind.Oven);
            long version = Grid.LayoutVersion;
            Pointer(Button("调整 " + oven.Name));
            yield return Command("旋转 " + oven.Name);
            Assert.That(Grid.LayoutVersion, Is.EqualTo(version + 1));
            var rotated = Grid.Stations[oven.Id];
            Assert.That(rotated.Orientation, Is.EqualTo(GridOrientation.CounterClockwise90));
            Assert.That(rotated.Footprint, Has.Member(new GridCell(oven.Cell.X - 1, oven.Cell.Y)));
            Assert.That(rotated.Footprint, Has.No.Member(new GridCell(oven.Cell.X, oven.Cell.Y + 1)));
            Pointer(Button("结束调整"));
        }
    }
}
#endif
