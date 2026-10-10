using System;
using Game;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;

namespace CarzyCooker.Tests
{
    public sealed class RuntimeInstanceTests
    {
        private const string ProjectA = "D:/CarzyCooker-Tests/ProjectA";
        private const string ProjectB = "D:/CarzyCooker-Tests/ProjectB";

        [TearDown]
        public void Cleanup()
        {
            RuntimeInstanceRequest.Clear(ProjectA);
            RuntimeInstanceRequest.Clear(ProjectB);
            SceneHelper.Clear(ProjectA);
            SceneHelper.Clear(ProjectB);
            RuntimeInstanceContext.Reset();
        }

        [Test]
        public void ConfigExpressesIntentAndRejectsConflicts()
        {
            RuntimeInstanceConfig host = RuntimeInstanceConfig.Create("run-1", "host.1", "host");
            Assert.That(host.Port, Is.Zero);
            Assert.That(host.Address, Is.EqualTo("127.0.0.1"));
            Assert.That(RuntimeInstanceConfig.Create("run-1", "client", "client", connect: "[::1]:12345").Port, Is.EqualTo(12345));
            Assert.That(RuntimeInstanceConfig.Create("r", "i", "legacy").Address, Is.Null);
            Assert.Throws<ArgumentException>(() => RuntimeInstanceConfig.Create("r", "i", "HOST"));
            Assert.Throws<ArgumentException>(() => RuntimeInstanceConfig.Create("r", "i", "host", connect: "127.0.0.1:1234"));
            Assert.Throws<ArgumentException>(() => RuntimeInstanceConfig.Create("r", "i", "client", connect: "127.0.0.1:0"));
            Assert.Throws<ArgumentException>(() => RuntimeInstanceConfig.Create("r", "i", "client"));
            Assert.Throws<ArgumentException>(() => RuntimeInstanceConfig.Create("r", "i", "host", "192.168.1.1:1"));
            Assert.Throws<ArgumentException>(() => RuntimeInstanceConfig.Create("r", "i", "legacy", "127.0.0.1:0"));
        }

        [TestCase("")]
        [TestCase(".")]
        [TestCase("..")]
        [TestCase("a/b")]
        [TestCase("r\n")]
        [TestCase("含中文")]
        public void InvalidIdsAreRejected(string value)
        {
            Assert.Throws<ArgumentException>(() => RuntimeInstanceConfig.Create(value, "i", "legacy"));
            Assert.Throws<ArgumentException>(() => RuntimeInstanceConfig.Create("r", value, "legacy"));
        }

        [Test]
        public void RealSessionRequestsAreScopedAndConsumedOnce()
        {
            SceneHelper.Submit(ProjectA, "Assets/A.unity");
            SceneHelper.Submit(ProjectB, "Assets/B.unity");
            RuntimeInstanceRequest.Submit(RuntimeInstanceConfig.Create("a", "i", "host"), ProjectA);
            RuntimeInstanceRequest.Submit(RuntimeInstanceConfig.Create("b", "i", "client", connect: "127.0.0.1:1234"), ProjectB);
            Assert.That(RuntimeInstanceRequest.Consume(ProjectA).RunId, Is.EqualTo("a"));
            Assert.That(RuntimeInstanceRequest.Consume(ProjectA), Is.Null);
            SceneHelper.Clear(ProjectA);
            RuntimeInstanceRequest.Clear(ProjectA);
            Assert.That(SceneHelper.Read(ProjectB), Is.EqualTo("Assets/B.unity"));
            Assert.That(RuntimeInstanceRequest.Consume(ProjectB).RunId, Is.EqualTo("b"));
            Assert.That(RuntimeInstanceRequest.Key(ProjectA, "Scene"), Is.EqualTo(RuntimeInstanceRequest.Key("d:\\carzycooker-tests\\projecta\\", "Scene")));
        }

        [Test]
        public void DraftAndLateStageCannotOverwriteFailedOrNextRun()
        {
            RuntimeInstanceContext.Reset();
            var first = RuntimeInstanceConfig.Create("first", "i", "host");
            RuntimeInstanceContext.Prepare(first);
            RuntimeInstanceRequest.Submit(RuntimeInstanceConfig.Create("draft", "i", "legacy"), ProjectA);
            Assert.That(RuntimeInstanceContext.Config.RunId, Is.EqualTo("first"));
            Assert.That(RuntimeInstanceContext.ProductStage, Is.Null);
            RuntimeInstanceContext.Fail("ProductBootstrapUnavailable");
            RuntimeInstanceContext.ReportProductStage(first, "HostReady");
            RuntimeInstanceContext.Stop();
            Assert.That(RuntimeInstanceContext.State, Is.EqualTo(RuntimeInstanceState.Failed));
            RuntimeInstanceContext.Reset();
            RuntimeInstanceContext.Prepare(RuntimeInstanceConfig.Create("second", "i", "legacy"));
            RuntimeInstanceContext.ReportProductStage(first, "HostReady");
            Assert.That(RuntimeInstanceContext.ProductStage, Is.Null);
            Assert.That(RuntimeInstanceContext.Config.RunId, Is.EqualTo("second"));
        }

        [Test]
        public void BusyUtfRejectsLaunchWithoutTouchingRequests()
        {
            Assert.That(InstanceLaunchWindow.CanLaunch(out string reason), Is.False);
            Assert.That(reason, Does.Contain("Busy"));
            Assert.That(RuntimeInstanceRequest.Consume(ProjectA), Is.Null);
        }
    }
}
