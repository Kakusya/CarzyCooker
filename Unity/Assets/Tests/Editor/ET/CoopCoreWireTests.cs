using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [TestCase(0)]
        [TestCase(99)]
        public void CoopCoreWireExplicitGridActionWhitelistRejectsUnknownActions(int action)
        {
            StartHost(); var remote = CoopRemote(); var view = remote.GetComponent<GridClientComponent>(); long[] items = Grid.Items.Keys.ToArray();
            var request = view.CreateGridCommand((GridAction)action); long command = request.CommandId;
            using (var response = (GridCommandResponse)Finish(remote.CallProduct(request), 500))
            {
                Assert.That(response.Error, Is.EqualTo(ProductError.InvalidRequest)); Assert.That(response.CommandId, Is.EqualTo(command));
                Assert.That(response.Message, Is.EqualTo("UnsupportedGridAction")); Assert.That(response.Status, Is.EqualTo((int)GridResultStatus.Rejected));
            }
            Assert.That(Grid.Items.Keys, Is.EqualTo(items)); Assert.That(Grid.Actors.Count, Is.EqualTo(2));
            using var probe = Finish(remote.ProbeProduct("after-whitelist-reject"), 500); Assert.That(probe.Error, Is.Zero);
        }

        [Test]
        public void CoopCoreWireOldProtocolAndInternalSubmitRejectAndDoNotChangeOtherMember()
        {
            StartHost(); var remote = CoopRemote(); var view = remote.GetComponent<GridClientComponent>();
            var old = view.CreateGridCommand(GridAction.StartAutoWork); old.ProtocolVersion = ProductSessionRules.ProtocolVersion - 1;
            using (var response = (GridCommandResponse)Finish(remote.CallProduct(old), 500)) Assert.That(response.Message, Is.EqualTo("IncompatibleProtocol"));
            using (var forged = LevelSubmitRequest.Create()) ((Session)remote.Session).Send(forged);
            Pump(900); Assert.That(Grid.Actor(remote.MemberId), Is.Null); Assert.That(Grid.Actor(((ClientSessionComponent)host.Client).MemberId), Is.Not.Null);
            Assert.That(Grid.Items.Count, Is.EqualTo(7)); Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing));
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
        }

        [Test]
        public void CoopCoreWireRejectedControlResponsesKeepPhysicalRpcAndBusinessCommandCorrelation()
        {
            StartHost(); var remote = CoopRemote(); var view = remote.GetComponent<GridClientComponent>(); var station = PrepStation("备料台");
            var level = view.CreateLevelCommand(LevelControlAction.Pause); long levelCommand = level.CommandId;
            using (var response = (LevelCommandResponse)Finish(remote.CallProduct(level), 500))
            { Assert.That(response.CommandId, Is.EqualTo(levelCommand)); Assert.That(response.RpcId, Is.GreaterThan(0)); Assert.That(response.Message, Is.EqualTo("HostControlOnly")); }
            var layout = view.CreateStationLayout(station.Id, new GridCell(2, 12), 0); long layoutCommand = layout.CommandId;
            using (var response = (StationLayoutResponse)Finish(remote.CallProduct(layout), 500))
            { Assert.That(response.CommandId, Is.EqualTo(layoutCommand)); Assert.That(response.RpcId, Is.GreaterThan(0)); Assert.That(response.Message, Is.EqualTo("HostControlOnly")); }
            var buff = view.CreateBuffChoice(view.BuffCandidates.First(), view.BuffDrawVersion); long buffCommand = buff.CommandId;
            using (var response = (ChooseBuffResponse)Finish(remote.CallProduct(buff), 500))
            { Assert.That(response.CommandId, Is.EqualTo(buffCommand)); Assert.That(response.RpcId, Is.GreaterThan(0)); Assert.That(response.Message, Is.EqualTo("HostControlOnly")); }
            Assert.That(view.WorldId, Is.EqualTo(Grid.WorldId)); Assert.That(CurrentLevel.Paused, Is.False);
        }
    }
}
