using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(SoloSaveBinding))]
    [FriendOf(typeof(SoloSaveResult))]
    public sealed partial class ProductSessionTests
    {
        private SoloSaveBinding SaveBinding => ((Match)((HostSessionComponent)host.Authority).Match).GetComponent<SoloSaveBinding>();
        private SoloSaveResult SaveResult => SaveBinding.CurrentResult;
        private void BindCurrentSave() => ((Match)((HostSessionComponent)host.Authority).Match).BindSoloSave(SaveProfileId, null, null);

        private long BeginSaveAndContinue()
        {
            ClientSessionComponent client = host.Client; var request = client.GetComponent<GridClientComponent>().CreateLevelCommand(LevelControlAction.SaveAndContinue);
            long command = request.CommandId;
            using (var receipt = LevelReply(client, request)) Assert.That(receipt.Status, Is.EqualTo((int)GridResultStatus.Accepted), receipt.Message);
            AdvanceTicks(1); PumpMessages(8); return command;
        }

        [Test]
        public void SoloSaveCommitZeroStarRequiresDurableFactBeforeSingleNextLevelAndReplayReturnsSameResult()
        {
            StartSolo(); BindCurrentSave(); var repository = TestSaveRepository(out string directory);
            try
            {
                CompleteForSave(); long oldLevel = CurrentLevel.Id;
                LevelDo(LevelControlAction.Continue, "SaveRequired");
                var request = ((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>().CreateLevelCommand(LevelControlAction.SaveAndContinue);
                var replay = CopyReplayRequest(request); var postInstallReplay = CopyReplayRequest(request);
                using (var reply = LevelReply(host.Client, request)) Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Accepted));
                Assert.That(Directory.GetFiles(repository.DirectoryPath, "*.ccsave"), Is.Empty);
                using (var reply = LevelReply(host.Client, replay)) Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Accepted));
                AdvanceTicks(1); PumpMessages(8);
                Assert.That(CurrentLevel.Id, Is.EqualTo(oldLevel), "An accepted request or file receipt cannot install a level before the service step.");
                Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed));
                Assert.That(SaveResult.State, Is.EqualTo(SoloSaveState.Writing));
                Assert.That(repository.Browse().Length, Is.EqualTo(1)); Assert.That(repository.Browse().Single().Stars, Is.Zero);
                AdvanceTicks(1); PumpMessages(8);
                Assert.That(SaveResult.State, Is.EqualTo(SoloSaveState.Committed)); Assert.That(CurrentLevel.Id, Is.Not.EqualTo(oldLevel));
                Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing)); long nextLevel = CurrentLevel.Id;
                Assert.That(FrontPeer.LastCommandStatus, Is.EqualTo(GridResultStatus.Accepted), "Installed level still awaits its preparation result");
                FrontWait(() => Buff.Prepared, 8);
                using (var reply = LevelReply(host.Client, postInstallReplay)) Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Executed));
                AdvanceTicks(2); Assert.That(CurrentLevel.Id, Is.EqualTo(nextLevel)); Assert.That(repository.Browse().Length, Is.EqualTo(1));
                Assert.That(Grid.Stations.Count, Is.EqualTo(17)); Assert.That(SaveBinding.SourceSlotId, Is.EqualTo(repository.Browse().Single().SlotId));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        public void SoloSaveCommitFailureRetainsSameImmutableResultAndRetryPublishesOneFile()
        {
            StartSolo(); BindCurrentSave(); var repository = TestSaveRepository(out string directory);
            string actualPath = repository.DirectoryPath;
            try
            {
                CompleteForSave(); long level = CurrentLevel.Id; int items = Grid.Items.Count;
                long generation = CurrentLevel.Generation;
                string blocker = Path.Combine(actualPath, "blocked"); File.WriteAllText(blocker, "owned test blocker"); repository.DirectoryPath = blocker;
                long failedCommand = BeginSaveAndContinue(); var result = SaveResult; byte[] immutable = (byte[])result.Record.Clone(); string identity = result.ResultId;
                AdvanceTicks(1); PumpMessages(8);
                Assert.That(result.State, Is.EqualTo(SoloSaveState.Failed)); Assert.That(CurrentLevel.Id, Is.EqualTo(level));
                Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed)); Assert.That(Grid.Items.Count, Is.EqualTo(items));
                Assert.That(Grid.Stations.Count, Is.EqualTo(16)); Assert.That(result.Record, Is.EqualTo(immutable));
                var failed = ProductOutcomeTestObserver.Received.Single(value => value.Value.Context.CommandId == failedCommand).Value;
                Assert.That(failed.Status, Is.EqualTo(GridResultStatus.Rejected)); Assert.That(failed.Reason, Is.EqualTo("SaveFailed")); Assert.That(failed.ChangedLevel, Is.False);
                repository.DirectoryPath = actualPath; Assert.That(repository.Browse(), Is.Empty);
                long retryCommand = BeginSaveAndContinue(); AdvanceTicks(1); PumpMessages(8);
                Assert.That(SaveResult.ResultId, Is.EqualTo(identity)); Assert.That(SaveResult.Record, Is.EqualTo(immutable));
                Assert.That(SaveResult.State, Is.EqualTo(SoloSaveState.Committed)); Assert.That(repository.Browse().Length, Is.EqualTo(1));
                Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing)); Assert.That(Grid.Stations.Count, Is.EqualTo(17));
                FrontWait(() => Buff.Prepared, 8);
                var committed = ProductOutcomeTestObserver.Received.Single(value => value.Value.Context.CommandId == retryCommand).Value;
                Assert.That(committed.Status, Is.EqualTo(GridResultStatus.Executed)); Assert.That(committed.ChangedLevel, Is.True);
                Assert.That(committed.Context.LevelId, Is.EqualTo(level)); Assert.That(committed.Context.LevelGeneration, Is.EqualTo(generation));
                Assert.That(CurrentLevel.Id, Is.Not.EqualTo(level)); Assert.That(((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>().LastProductOutcome.Context.CommandId, Is.EqualTo(retryCommand));
            }
            finally { repository.DirectoryPath = actualPath; repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        public void SoloSaveCommitPreparingOperatingExitAndUnsettledRenovationDoNotCreateSuccessFiles()
        {
            StartSolo(); BindCurrentSave(); var repository = TestSaveRepository(out string directory);
            try
            {
                LevelDo(LevelControlAction.SaveAndContinue, "SuccessRequired"); Assert.That(SaveResult, Is.Null);
                LevelDo(LevelControlAction.Start); LevelDo(LevelControlAction.SaveAndContinue, "SuccessRequired");
                Assert.That(repository.Browse(), Is.Empty); host.StopProduct(); Assert.That(repository.Browse(), Is.Empty);
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        public void SoloSaveCommitSettlementChoiceMustBeLockedAndLateCompletionCannotEnterDifferentLevel()
        {
            StartSolo(); BindCurrentSave(); var repository = TestSaveRepository(out string directory);
            try
            {
                RenovationComplete(); LevelDo(LevelControlAction.SaveAndContinue, "RenovationRequired"); Assert.That(SaveResult, Is.Null);
                RenovationChoose(1); BeginSaveAndContinue(); var result = SaveResult;
                var duplicate = ProductMessages.Create<SoloSaveCompletedRequest>(r =>
                {
                    r.ResultEntityId = result.Id; r.ResultInstanceId = result.InstanceId; r.WorldId = result.WorldId;
                    r.WorldGeneration = result.WorldGeneration; r.LevelId = result.LevelId; r.LevelGeneration = result.LevelGeneration;
                    r.Header = MemoryPackHelper.Serialize(repository.Browse().Single());
                });
                AdvanceTicks(1); PumpMessages(8); long level = CurrentLevel.Id;
                var task = host.Root().GetComponent<ProcessInnerSender>().Call(((HostSessionComponent)host.Authority).GetActorId(), duplicate, false);
                PumpMessages(8); using var reply = (SoloSaveCompletedResponse)task.GetAwaiter().GetResult();
                Assert.That(reply.Error, Is.Not.Zero); Assert.That(reply.Message, Is.EqualTo("StaleSaveResult"));
                AdvanceTicks(1); Assert.That(CurrentLevel.Id, Is.EqualTo(level)); Assert.That(repository.Browse().Length, Is.EqualTo(1));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test]
        public void SoloSaveCommitPublishedFileWithLostWriteReceiptRetriesSameResultWithoutSecondRecord()
        {
            StartSolo(); BindCurrentSave(); var repository = TestSaveRepository(out string directory);
            try
            {
                CompleteForSave(); long oldLevel = CurrentLevel.Id;
                var request = ((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>().CreateLevelCommand(LevelControlAction.SaveAndContinue);
                using (var receipt = LevelReply(host.Client, request)) Assert.That(receipt.Status, Is.EqualTo((int)GridResultStatus.Accepted));
                Restaurant.AdvanceRestaurantStep(); var result = SaveResult;
                PumpMessages(1); Assert.That(repository.Browse().Length, Is.EqualTo(1), "The real repository published the file.");
                var sender = host.Root().GetComponent<ProcessInnerSender>();
                var pending = sender.requestCallback.Single(pair => pair.Value.RequestType == typeof(SoloSaveWriteRequest));
                Assert.That(sender.requestCallback.Remove(pending.Key), Is.True);
                // Lose this physical reply before sender consumes it; use the normal timeout result type.
                pending.Value.SetResult(ProductMessages.Create<SoloSaveWriteResponse>(r => { r.RpcId = pending.Key; r.Error = ErrorCore.ERR_Timeout; r.Message = "Lost write receipt"; }));
                PumpMessages(8); AdvanceTicks(1); PumpMessages(8);
                Assert.That(result.State, Is.EqualTo(SoloSaveState.Failed)); Assert.That(CurrentLevel.Id, Is.EqualTo(oldLevel));
                string slot = repository.Browse().Single().SlotId, identity = result.ResultId;
                BeginSaveAndContinue(); AdvanceTicks(1); PumpMessages(8);
                Assert.That(SaveResult.State, Is.EqualTo(SoloSaveState.Committed)); Assert.That(SaveResult.ResultId, Is.EqualTo(identity));
                Assert.That(repository.Browse().Single().SlotId, Is.EqualTo(slot)); Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing));
                Assert.That(Grid.Stations.Count, Is.EqualTo(17));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
