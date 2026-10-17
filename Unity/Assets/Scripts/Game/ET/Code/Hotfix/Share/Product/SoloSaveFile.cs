#if !DOTNET
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game;

namespace ET
{
    // 同一个文件内的轻量 Header 与纯值载荷。浏览只读取有界 Header。
    public static class SoloSaveFile
    {
        private const string Magic = "CCSOLO01";
        public const int MaxHeaderBytes = 128 * 1024;
        public const int MaxPayloadBytes = 4 * 1024 * 1024;

        public static byte[] Encode(SoloSaveHeader header, SoloSavePayload payload, ProductStoragePlatform? platform = null)
        {
            SoloSaveValidation.Payload(header, payload, header.ProfileId, platform);
            byte[] head = MemoryPackHelper.Serialize(header), body = MemoryPackHelper.Serialize(payload);
            if (head.Length > MaxHeaderBytes || body.Length > MaxPayloadBytes) throw new InvalidOperationException("SaveTooLarge");
            using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(Encoding.ASCII.GetBytes(Magic)); writer.Write(head.Length); writer.Write(body.Length); writer.Write(head); writer.Write(body);
            return stream.ToArray();
        }

        public static SoloSaveHeader ReadHeader(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            if (Encoding.ASCII.GetString(reader.ReadBytes(Magic.Length)) != Magic) throw new InvalidOperationException("InvalidSaveEnvelope");
            int headLength = reader.ReadInt32(), bodyLength = reader.ReadInt32();
            if (headLength <= 0 || headLength > MaxHeaderBytes || bodyLength <= 0 || bodyLength > MaxPayloadBytes ||
                stream.Length != Magic.Length + 8L + headLength + bodyLength) throw new InvalidOperationException("InvalidSaveLength");
            byte[] bytes = reader.ReadBytes(headLength);
            if (bytes.Length != headLength) throw new InvalidOperationException("TruncatedSaveHeader");
            var header = (SoloSaveHeader)MemoryPackHelper.Deserialize(typeof(SoloSaveHeader), bytes, 0, bytes.Length);
            if (header != null && header.Version is >= 1 and <= 4)
            {
                // MemoryPack对旧成员表补default值，不执行新字段的属性初始化。
                // 只做读取适配，不发布或改写原文件，也不把旧档当成已领取。
                header.SourceProfileId = header.SourceSlotId == "" ? "" : header.ProfileId;
                header.RootProfileId = header.ProfileId; header.RootSlotId = header.SlotId; header.RootRevision = header.Revision;
                header.SourceResultId = header.AuthorizationId = "";
                header.ClaimedResults = Array.Empty<string>(); header.ClaimSlots = Array.Empty<string>(); header.ClaimRevisions = Array.Empty<int>();
            }
            if (header != null && header.Version is >= 1 and <= 5)
            {
                header.Receipts = ProgressReceiptValues.FromLegacy(header);
            }
            return header;
        }

        public static SoloSavePayload ReadPayload(Stream stream, string profile, out SoloSaveHeader header, ProductStoragePlatform? platform = null)
        {
            header = ReadHeader(stream); SoloSaveValidation.Header(header, profile, platform);
            int remaining = checked((int)(stream.Length - stream.Position));
            var bytes = new byte[remaining]; int offset = 0;
            while (offset < remaining)
            {
                int read = stream.Read(bytes, offset, remaining - offset);
                if (read == 0) throw new InvalidOperationException("TruncatedSavePayload");
                offset += read;
            }
            var payload = (SoloSavePayload)MemoryPackHelper.Deserialize(typeof(SoloSavePayload), bytes, 0, bytes.Length);
            RestaurantUnlockValues.AdaptLegacy(header, payload);
            KitchenGeometryConfig.AdaptLegacy(header, payload);
            SoloSaveValidation.Payload(header, payload, profile, platform); return payload;
        }
    }

    [EntitySystemOf(typeof(SoloSaveRepository))]
    [FriendOf(typeof(SoloSaveRepository))]
    public static partial class SoloSaveRepositorySystem
    {
        [EntitySystem]
        private static void Awake(this SoloSaveRepository self, string applicationDirectory, string profile)
            => self.InitializeRepository(applicationDirectory, profile, RuntimeInstanceContext.StoragePlatform);

        [EntitySystem]
        private static void Awake(this SoloSaveRepository self, string applicationDirectory, string profile, ProductStoragePlatform platform)
            => self.InitializeRepository(applicationDirectory, profile, platform);

        private static void InitializeRepository(this SoloSaveRepository self, string applicationDirectory, string profile, ProductStoragePlatform platform)
        {
            if (!SoloSaveValidation.IsIdentifier(profile)) throw new InvalidOperationException("InvalidProfile");
            string platformName = RuntimeInstanceContext.StoragePlatformName(platform);
            self.ProfileId = profile;
            self.Platform = platform;
            self.Preferences = new ProductPreferencesValue();
            self.PreferencesFailure = null;
            self.PreferencesVersion = 0;
            self.DirectoryPath = Path.GetFullPath(Path.Combine(applicationDirectory, "Solo", platformName, profile));
            Directory.CreateDirectory(self.DirectoryPath);
            self.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
            self.LoadProductPreferences();
        }

        public static string LocalProfile(string applicationDirectory, ProductStoragePlatform? platform = null)
        {
            string directory = Path.GetFullPath(Path.Combine(applicationDirectory, "Solo",
                RuntimeInstanceContext.StoragePlatformName(platform ?? RuntimeInstanceContext.StoragePlatform)));
            Directory.CreateDirectory(directory); string path = Path.Combine(directory, "profile.id");
            if (!File.Exists(path))
            {
                string temp = Path.Combine(directory, ".profile-" + Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    WriteTemporary(temp, Encoding.ASCII.GetBytes(Guid.NewGuid().ToString("N")));
                    try { File.Move(temp, path); }
                    catch (IOException) when (File.Exists(path)) { /* Another local opener published the profile. */ }
                }
                finally { if (File.Exists(temp)) File.Delete(temp); }
            }
            using var stream = File.OpenRead(path);
            if (stream.Length != 32) throw new InvalidOperationException("InvalidLocalProfile");
            var bytes = new byte[32]; if (stream.Read(bytes, 0, bytes.Length) != bytes.Length) throw new IOException("TruncatedLocalProfile");
            string profile = Encoding.ASCII.GetString(bytes);
            if (!SoloSaveValidation.IsIdentifier(profile)) throw new InvalidOperationException("InvalidLocalProfile");
            return profile;
        }

        public static SoloSaveHeader[] Browse(this SoloSaveRepository self)
        {
            var headers = new List<SoloSaveHeader>();
            foreach (string path in Directory.EnumerateFiles(self.DirectoryPath, "*.ccsave").OrderBy(value => value, StringComparer.Ordinal))
            {
                string slot = Path.GetFileNameWithoutExtension(path);
                if (!SoloSaveValidation.IsIdentifier(slot)) continue;
                try
                {
                    using var stream = File.OpenRead(path); var header = SoloSaveFile.ReadHeader(stream);
                    if (header != null && header.SlotId == slot && header.ProfileId == self.ProfileId) headers.Add(header);
                }
                catch (Exception e) when (e is IOException or InvalidOperationException or MemoryPack.MemoryPackSerializationException) { }
            }
            return headers.OrderByDescending(header => header.SavedUtcTicks).ToArray();
        }

        public static SoloSavePayload Select(this SoloSaveRepository self, string slot, out SoloSaveHeader header)
        {
            using var stream = File.OpenRead(self.SlotPath(slot));
            var payload = SoloSaveFile.ReadPayload(stream, self.ProfileId, out header, self.Platform);
            if (header.SlotId != slot) throw new InvalidOperationException("SlotIdentityMismatch");
            return payload;
        }

        public static SoloSaveHeader Commit(this SoloSaveRepository self, SoloSaveHeader header, SoloSavePayload payload)
        {
            SoloSaveValidation.Payload(header, payload, self.ProfileId, self.Platform);
            // Rebuild from committed headers, so response loss and reopening share the same fact.
            var existing = self.Browse().FirstOrDefault(h => h.ResultId == header.ResultId && h.ProfileId == header.ProfileId);
            if (existing != null) { self.Select(existing.SlotId, out var committed); return committed; }
            return self.WriteNewRecord(header, payload);
        }

        public static SoloSaveHeader CommitWithReceipt(this SoloSaveRepository self, SoloSaveHeader header, SoloSavePayload payload)
        {
            SoloSaveValidation.Payload(header, payload, self.ProfileId, self.Platform);
            RequireReceiptForRecord(header);
            return self.WriteNewRecord(header, payload);
        }

        private static void RequireReceiptForRecord(SoloSaveHeader header)
        {
            if (header.Version < 6 || !header.Receipts.Any(receipt => receipt.ResultId == header.ResultId && receipt.ProfileId == header.ProfileId &&
                receipt.SlotId == header.SlotId && receipt.Revision == header.Revision))
            {
                throw new InvalidOperationException("CommitReceiptRequired");
            }
        }

        private static SoloSaveHeader WriteNewRecord(this SoloSaveRepository self, SoloSaveHeader header, SoloSavePayload payload)
        {
            string target = self.SlotPath(header.SlotId);
            if (File.Exists(target)) throw new IOException("SaveSlotConflict");
            byte[] bytes = SoloSaveFile.Encode(header, payload, self.Platform);
            string temp = Path.Combine(self.DirectoryPath, ".save-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                WriteTemporary(temp, bytes);
                try { File.Move(temp, target); }
                catch (IOException) when (File.Exists(target))
                {
                    self.Select(header.SlotId, out var committed);
                    if (committed.ResultId == header.ResultId && committed.ProfileId == header.ProfileId) return committed;
                    throw new IOException("SaveSlotConflict");
                }
                return header;
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        public static string OwnerSlotPath(this SoloSaveRepository self, string owner, string slot)
        {
            if (!SoloSaveValidation.IsIdentifier(owner) || !SoloSaveValidation.IsIdentifier(slot))
            {
                throw new InvalidOperationException("InvalidSlotId");
            }
            string pcDirectory = Path.GetDirectoryName(self.DirectoryPath);
            string ownerDirectory = Path.GetFullPath(Path.Combine(pcDirectory, owner));
            string path = Path.GetFullPath(Path.Combine(ownerDirectory, slot + ".ccsave"));
            if (!string.Equals(Path.GetDirectoryName(path), ownerDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("SavePathOutsideRepository");
            }
            return path;
        }

        public static SoloSavePayload SelectOwnerRecord(this SoloSaveRepository self, string owner, string slot, out SoloSaveHeader header)
        {
            string path = self.OwnerSlotPath(owner, slot);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("TargetMissing");
            }
            using var stream = File.OpenRead(path);
            var payload = SoloSaveFile.ReadPayload(stream, owner, out header, self.Platform);
            if (header.SlotId != slot)
            {
                throw new InvalidOperationException("SlotIdentityMismatch");
            }
            return payload;
        }

        public static void ReplaceWithReceipt(this SoloSaveRepository self, SoloSaveHeader header, SoloSavePayload payload, int expectedRevision)
        {
            SoloSaveValidation.Payload(header, payload, header.ProfileId, self.Platform);
            RequireReceiptForRecord(header);
            self.SelectOwnerRecord(header.ProfileId, header.SlotId, out var current);
            if (current.Revision != expectedRevision || header.Revision != checked(expectedRevision + 1))
            {
                throw new InvalidOperationException("RevisionConflict");
            }
            foreach (var prior in current.Receipts)
            {
                string key = ProgressReceiptValues.Key(prior);
                var retained = header.Receipts.FirstOrDefault(receipt => ProgressReceiptValues.Key(receipt) == key);
                if (retained == null || retained.ProfileId != prior.ProfileId || retained.SlotId != prior.SlotId || retained.Revision != prior.Revision)
                {
                    throw new InvalidOperationException("CommitReceiptMissing");
                }
            }
            string target = self.OwnerSlotPath(header.ProfileId, header.SlotId);
            string temporary = Path.Combine(Path.GetDirectoryName(target), ".replace-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                WriteTemporary(temporary, SoloSaveFile.Encode(header, payload, self.Platform));
                // 同目录替换为唯一发布点；null备份参数，不生成可恢复旧载荷。
                File.Replace(temporary, target, null);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        private static void WriteTemporary(string path, byte[] bytes)
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
        }

        private static string SlotPath(this SoloSaveRepository self, string slot)
        {
            if (!SoloSaveValidation.IsIdentifier(slot)) throw new InvalidOperationException("InvalidSlotId");
            string path = Path.GetFullPath(Path.Combine(self.DirectoryPath, slot + ".ccsave"));
            if (!string.Equals(Path.GetDirectoryName(path), self.DirectoryPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("SavePathOutsideRepository");
            return path;
        }
    }
}
#endif
