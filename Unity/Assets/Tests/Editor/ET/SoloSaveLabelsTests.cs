using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private SoloSaveLabels LabelsWrite(SoloSaveRepository repository, SoloSaveHeader header, string name, bool favorite, bool success = true)
        {
            var request = SoloSaveLabelsWriteRequest.Create();
            request.SlotId = header.SlotId; request.Revision = header.Revision; request.ResultId = header.ResultId;
            request.Name = name; request.Favorite = favorite;
            var task = host.Root().GetComponent<ProcessInnerSender>().Call(repository.GetActorId(), request, false);
            PumpMessages(8);
            Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending));
            using var reply = (SoloSaveLabelsWriteResponse)task.GetAwaiter().GetResult();
            Assert.That(reply.Error == 0, Is.EqualTo(success), reply.Message);
            return reply.Error == 0 ? SoloSaveRepositorySystem.DecodeSaveLabels(reply.Values, repository.SaveProfile()) : null;
        }

        private SoloSaveLabels LabelsRead(SoloSaveRepository repository, bool success = true)
        {
            var task = host.Root().GetComponent<ProcessInnerSender>().Call(repository.GetActorId(), SoloSaveLabelsReadRequest.Create(), false);
            PumpMessages(8);
            using var reply = (SoloSaveLabelsReadResponse)task.GetAwaiter().GetResult();
            Assert.That(reply.Error == 0, Is.EqualTo(success), reply.Message);
            return reply.Error == 0 ? SoloSaveRepositorySystem.DecodeSaveLabels(reply.Values, repository.SaveProfile()) : null;
        }

        [Test, Category("SoloSaveLabels")]
        public void EntrySaveLabelsActualLocalMailboxTwoSlotsUnicodeRevisionAndEmptyNameNeverChangeSuccessBytes()
        {
            StartSolo();
            var payload = CompleteForSave();
            var first = SaveHeader(); var second = SaveHeader();
            var repository = TestSaveRepository(out string directory);
            try
            {
                repository.Commit(first, payload); repository.Commit(second, payload);
                string firstPath = Path.Combine(repository.DirectoryPath, first.SlotId + ".ccsave");
                string secondPath = Path.Combine(repository.DirectoryPath, second.SlotId + ".ccsave");
                byte[] firstBytes = File.ReadAllBytes(firstPath), secondBytes = File.ReadAllBytes(secondPath);
                Assert.That(LabelsRead(repository).Entries, Is.Empty);
                string chinese = new string('馆', 32), unicode = string.Concat(Enumerable.Repeat("e\u0301\U0001F375", 16));
                LabelsWrite(repository, first, chinese, true);
                var labels = LabelsWrite(repository, second, unicode, false);
                Assert.That(labels.Entries.Single(value => value.SlotId == first.SlotId).Name, Is.EqualTo(chinese));
                Assert.That(labels.Entries.Single(value => value.SlotId == second.SlotId).Name, Is.EqualTo(unicode));
                foreach (string bad in new[] { chinese + "馆", unicode + "x", "a\nb", "a\rb", "a\tb", "a\u2028b", "\ud800" })
                {
                    LabelsWrite(repository, first, bad, false, false);
                }
                Assert.That(File.ReadAllBytes(firstPath), Is.EqualTo(firstBytes));
                Assert.That(File.ReadAllBytes(secondPath), Is.EqualTo(secondBytes));
                var revised = CopySaveValue(first);
                revised.Revision = 2; revised.ResultId = Guid.NewGuid().ToString("N"); revised.SavedUtcTicks++;
                byte[] revisedBytes = SoloSaveFile.Encode(revised, payload);
                File.WriteAllBytes(firstPath, revisedBytes);
                Assert.That(LabelsRead(repository).Entries.Single(value => value.SlotId == first.SlotId).Name, Is.EqualTo(chinese), "A same-slot revision retains its local display preference");
                LabelsWrite(repository, first, "旧修订不能保存", false, false);
                labels = LabelsWrite(repository, revised, "", true);
                Assert.That(labels.Entries.Single(value => value.SlotId == first.SlotId).Name, Is.Empty);
                Assert.That(labels.Entries.Single(value => value.SlotId == first.SlotId).Favorite, Is.True);
                labels = LabelsWrite(repository, revised, "", false);
                Assert.That(labels.Entries.Any(value => value.SlotId == first.SlotId), Is.False);
                Assert.That(labels.Entries.Single().SlotId, Is.EqualTo(second.SlotId));
                repository.Select(first.SlotId, out var current);
                Assert.That(current.Revision, Is.EqualTo(2));
                Assert.That(File.ReadAllBytes(firstPath), Is.EqualTo(revisedBytes));
                Assert.That(File.ReadAllBytes(secondPath), Is.EqualTo(secondBytes));
            }
            finally
            {
                repository.Dispose(); RemoveOwnSaveTestDirectory(directory);
            }
        }

        [Test, Category("SoloSaveLabels")]
        public void EntrySaveLabelsActualLockedPublicationRetryReopenCorruptUnknownAndProfileKeepOriginalFiles()
        {
            StartSolo(); var payload = CompleteForSave(); var header = SaveHeader();
            var repository = TestSaveRepository(out string directory);
            try
            {
                repository.Commit(header, payload);
                string record = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                byte[] recordBytes = File.ReadAllBytes(record);
                LabelsWrite(repository, header, "常用小馆", true);
                string path = Path.Combine(repository.DirectoryPath, SoloSaveRepositorySystem.LabelsFileName);
                byte[] before = File.ReadAllBytes(path);
                using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    LabelsWrite(repository, header, "更名重试", false, false);
                    Assert.That(Directory.GetFiles(repository.DirectoryPath, ".labels-*.tmp"), Is.Empty);
                }
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
                LabelsWrite(repository, header, "更名重试", false);
                repository.Dispose();
                repository = host.Root().AddComponent<SoloSaveRepository, string, string>(directory, SaveProfileId);
                Assert.That(LabelsRead(repository).Entries.Single().Name, Is.EqualTo("更名重试"));
                var unknown = new SoloSaveLabels { Version = 99, ProfileId = SaveProfileId };
                var wrongProfile = new SoloSaveLabels { ProfileId = Guid.NewGuid().ToString("N") };
                foreach (byte[] invalid in new[] { new byte[] { 222, 1, 7 }, MemoryPackHelper.Serialize(unknown), MemoryPackHelper.Serialize(wrongProfile) })
                {
                    File.WriteAllBytes(path, invalid);
                    LabelsRead(repository, false);
                    LabelsWrite(repository, header, "不能覆盖未知文件", false, false);
                    Assert.That(File.ReadAllBytes(path), Is.EqualTo(invalid));
                    repository.Select(header.SlotId, out var selected);
                    Assert.That(selected.ResultId, Is.EqualTo(header.ResultId));
                    Assert.That(File.ReadAllBytes(record), Is.EqualTo(recordBytes));
                }
            }
            finally
            {
                repository.Dispose(); RemoveOwnSaveTestDirectory(directory);
            }
        }
    }
}
