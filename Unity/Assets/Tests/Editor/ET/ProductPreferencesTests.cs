using System;
using System.IO;
using Cysharp.Threading.Tasks;
using MemoryPack;
using NUnit.Framework;

namespace ET
{
    [MemoryPackable]
    public sealed partial class LegacyProductPreferences : Object
    {
        public int Version { get; set; } = 1;
        public ProductFontSize FontSize { get; set; }
        public ProductDragSpeed DragSpeed { get; set; }
    }

    public sealed partial class ProductSessionTests
    {
        private void PreferencesWrite(SoloSaveRepository repository, ProductPreferencesValue value, bool success = true)
        {
            var request = ProductPreferencesWriteRequest.Create();
            request.Values = MemoryPackHelper.Serialize(value);
            var task = host.Root().GetComponent<ProcessInnerSender>().Call(repository.GetActorId(), request, false);
            PumpMessages(8);
            Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending));
            using var response = (ProductPreferencesWriteResponse)task.GetAwaiter().GetResult();
            Assert.That(response.Error == 0, Is.EqualTo(success), response.Message);
            if (success)
            {
                Assert.That(SoloSaveRepositorySystem.DecodePreferences(response.Values).FontSize, Is.EqualTo(value.FontSize));
            }
        }

        [Test, Category("ProductPreferences")]
        public void EntryPreferencesActualLocalMailboxFileLockRetryAndReopenPreserveSuccessBytes()
        {
            StartSolo();
            var payload = CompleteForSave();
            var header = SaveHeader();
            var repository = TestSaveRepository(out string directory);
            try
            {
                repository.Commit(header, payload);
                string savePath = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                byte[] saveBytes = File.ReadAllBytes(savePath);
                Assert.That(repository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Standard));
                Assert.That(repository.Preferences.DragSpeed, Is.EqualTo(ProductDragSpeed.Standard));
                Assert.That(repository.Preferences.Notices, Is.EqualTo(ProductNoticeOptions.All));
                Assert.That(repository.Preferences.DeadlineSeconds, Is.EqualTo(5));
                var value = new ProductPreferencesValue
                {
                    FontSize = ProductFontSize.Largest, DragSpeed = ProductDragSpeed.Slow,
                    Notices = ProductNoticeOptions.Processing | ProductNoticeOptions.Deadlines, DeadlineSeconds = 3
                };
                PreferencesWrite(repository, value);
                string preferences = Path.Combine(repository.DirectoryPath, SoloSaveRepositorySystem.PreferencesFileName);
                byte[] previous = File.ReadAllBytes(preferences);
                value.FontSize = ProductFontSize.Standard;
                Assert.That(repository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Largest), "Draft is independent of committed state");
                // Permit reading the old file, but deny delete-sharing so actual publication fails after the temporary write.
                using (var locked = new FileStream(preferences, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    PreferencesWrite(repository, value, false);
                    Assert.That(repository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Largest));
                    Assert.That(Directory.GetFiles(repository.DirectoryPath, ".preferences-*.tmp"), Is.Empty);
                }
                Assert.That(File.ReadAllBytes(preferences), Is.EqualTo(previous));
                PreferencesWrite(repository, value);
                repository.Dispose();
                repository = host.Root().AddComponent<SoloSaveRepository, string, string>(directory, SaveProfileId);
                Assert.That(repository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Standard));
                Assert.That(repository.Preferences.DragSpeed, Is.EqualTo(ProductDragSpeed.Slow));
                Assert.That(repository.Preferences.DeadlineSeconds, Is.EqualTo(3));
                Assert.That(repository.Preferences.Notices.HasFlag(ProductNoticeOptions.Arrivals), Is.False);
                Assert.That(File.ReadAllBytes(savePath), Is.EqualTo(saveBytes));
                Assert.That(Directory.GetFiles(repository.DirectoryPath, ".preferences-*.tmp"), Is.Empty);
                var read = host.Root().GetComponent<ProcessInnerSender>().Call(repository.GetActorId(), ProductPreferencesReadRequest.Create(), false);
                PumpMessages(8);
                using var response = (ProductPreferencesReadResponse)read.GetAwaiter().GetResult();
                Assert.That(response.Error, Is.Zero, response.Message);
                Assert.That(SoloSaveRepositorySystem.DecodePreferences(response.Values).DeadlineSeconds, Is.EqualTo(3));
            }
            finally
            {
                repository.Dispose();
                RemoveOwnSaveTestDirectory(directory);
            }
        }

        [Test, Category("ProductPreferences")]
        public void EntryPreferencesActualLegacyMissingFieldsUnknownCorruptAndPlatformIsolation()
        {
            StartSolo();
            var repository = TestSaveRepository(out string directory);
            try
            {
                string pcPath = repository.DirectoryPath;
                string path = Path.Combine(pcPath, SoloSaveRepositorySystem.PreferencesFileName);
                byte[] legacy = MemoryPackHelper.Serialize(new LegacyProductPreferences { FontSize = ProductFontSize.Larger, DragSpeed = ProductDragSpeed.Fast });
                File.WriteAllBytes(path, legacy);
                repository.LoadProductPreferences();
                Assert.That(repository.PreferencesFailure, Is.Null);
                Assert.That(repository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Larger));
                Assert.That(repository.Preferences.Notices, Is.EqualTo(ProductNoticeOptions.All));
                Assert.That(repository.Preferences.DeadlineSeconds, Is.EqualTo(5));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(legacy), "Reading legacy fields does not rewrite the file");
                var invalid = new ProductPreferencesValue { FontSize = (ProductFontSize)99 };
                Assert.Throws<InvalidOperationException>(() => repository.SaveProductPreferences(invalid));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(legacy));
                byte[] unknown = MemoryPackHelper.Serialize(new ProductPreferencesValue { Version = 99 });
                File.WriteAllBytes(path, unknown);
                repository.LoadProductPreferences();
                Assert.That(repository.PreferencesFailure, Is.Not.Null);
                Assert.That(repository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Larger));
                Assert.Throws<InvalidOperationException>(() => repository.SaveProductPreferences(new ProductPreferencesValue()));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(unknown));
                var corrupt = new byte[] { 255, 17, 3 };
                File.WriteAllBytes(path, corrupt);
                repository.LoadProductPreferences();
                Assert.That(repository.PreferencesFailure, Is.Not.Null);
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(corrupt));
                repository.Dispose();
                repository = host.Root().AddComponent<SoloSaveRepository, string, string>(directory, SaveProfileId);
                Assert.That(repository.PreferencesFailure, Is.Not.Null);
                Assert.That(repository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Standard), "A new owner with an unreadable file cannot inherit another pooled owner's preferences");
                repository.Dispose();
                repository = host.Root().AddComponent<SoloSaveRepository, string, string, Game.ProductStoragePlatform>(directory, SaveProfileId, Game.ProductStoragePlatform.Android);
                Assert.That(repository.DirectoryPath, Is.Not.EqualTo(pcPath));
                Assert.That(repository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Standard));
                repository.SaveProductPreferences(new ProductPreferencesValue { FontSize = ProductFontSize.Largest });
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(corrupt), "Android settings cannot rewrite the PC file");
            }
            finally
            {
                repository.Dispose();
                RemoveOwnSaveTestDirectory(directory);
            }
        }
    }
}
