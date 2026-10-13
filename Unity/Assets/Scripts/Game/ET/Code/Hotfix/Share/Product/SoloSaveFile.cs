#if !DOTNET
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ET
{
    // 同一个文件内的轻量 Header 与纯值载荷。浏览只读取有界 Header。
    public static class SoloSaveFile
    {
        private const string Magic = "CCSOLO01";
        public const int MaxHeaderBytes = 4096;
        public const int MaxPayloadBytes = 4 * 1024 * 1024;

        public static byte[] Encode(SoloSaveHeader header, SoloSavePayload payload)
        {
            SoloSaveValidation.Payload(header, payload, header.ProfileId);
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
            return (SoloSaveHeader)MemoryPackHelper.Deserialize(typeof(SoloSaveHeader), bytes, 0, bytes.Length);
        }

        public static SoloSavePayload ReadPayload(Stream stream, string profile, out SoloSaveHeader header)
        {
            header = ReadHeader(stream); SoloSaveValidation.Header(header, profile);
            int remaining = checked((int)(stream.Length - stream.Position));
            var bytes = new byte[remaining]; int offset = 0;
            while (offset < remaining)
            {
                int read = stream.Read(bytes, offset, remaining - offset);
                if (read == 0) throw new InvalidOperationException("TruncatedSavePayload");
                offset += read;
            }
            var payload = (SoloSavePayload)MemoryPackHelper.Deserialize(typeof(SoloSavePayload), bytes, 0, bytes.Length);
            SoloSaveValidation.Payload(header, payload, profile); return payload;
        }
    }

    [EntitySystemOf(typeof(SoloSaveRepository))]
    [FriendOf(typeof(SoloSaveRepository))]
    public static partial class SoloSaveRepositorySystem
    {
        [EntitySystem]
        private static void Awake(this SoloSaveRepository self, string applicationDirectory, string profile)
        {
            if (!SoloSaveValidation.IsIdentifier(profile)) throw new InvalidOperationException("InvalidProfile");
            self.ProfileId = profile;
            self.DirectoryPath = Path.GetFullPath(Path.Combine(applicationDirectory, "Solo", "PC", profile));
            Directory.CreateDirectory(self.DirectoryPath);
            self.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
        }

        public static string LocalProfile(string applicationDirectory)
        {
            string directory = Path.GetFullPath(Path.Combine(applicationDirectory, "Solo", "PC"));
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
            var payload = SoloSaveFile.ReadPayload(stream, self.ProfileId, out header);
            if (header.SlotId != slot) throw new InvalidOperationException("SlotIdentityMismatch");
            return payload;
        }

        public static SoloSaveHeader Commit(this SoloSaveRepository self, SoloSaveHeader header, SoloSavePayload payload)
        {
            SoloSaveValidation.Payload(header, payload, self.ProfileId);
            // Rebuild from committed headers, so response loss and reopening share the same fact.
            var existing = self.Browse().FirstOrDefault(h => h.ResultId == header.ResultId && h.ProfileId == header.ProfileId);
            if (existing != null) { self.Select(existing.SlotId, out var committed); return committed; }
            string target = self.SlotPath(header.SlotId);
            if (File.Exists(target)) throw new IOException("SaveSlotConflict");
            byte[] bytes = SoloSaveFile.Encode(header, payload);
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
