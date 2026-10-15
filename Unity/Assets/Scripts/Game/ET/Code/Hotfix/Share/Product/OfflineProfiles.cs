#if !DOTNET
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ET
{
    public readonly struct OfflineVerifiedDocument
    {
        public readonly OfflineProfileIdentity Signer;
        public readonly byte[] Body, Signature;
        public OfflineVerifiedDocument(OfflineProfileIdentity signer, byte[] body, byte[] signature)
        {
            Signer = signer;
            Body = body;
            Signature = signature;
        }
    }

    [FriendOf(typeof(SoloSaveRepository))]
    public static class OfflineProfiles
    {
        public const string ProfileProof = "ProfileProof/v1";
        public const string BaselineReadRun = "BaselineReadRun+MatchAuthority/v1";
        public const string MatchAuthority = "MatchAuthority/v1";
        public const string SuccessResult = "CooperativeSuccess/v1";
        public const string ClaimTicket = "ClaimTicket/v1";
        public const string NormalClose = "NormalClose/v1";
        public const string ImportBranch = "ImportBranch/v1";
        public const string ReplaceSave = "ReplaceGrant/v1";
        public const int MaxIdentityBytes = 8192;
        public const int MaxDocumentBytes = SoloSaveFile.MaxPayloadBytes + SoloSaveFile.MaxHeaderBytes + 32768;
        private const string Magic = "CCAUTH01";

        public static byte[] EncodeIdentity(OfflineProfileIdentity identity)
        {
            ValidateIdentity(identity);
            return MemoryPackHelper.Serialize(identity);
        }

        public static OfflineProfileIdentity DecodeIdentity(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > MaxIdentityBytes)
            {
                throw new InvalidOperationException("InvalidProfileIdentity");
            }
            var identity = (OfflineProfileIdentity)MemoryPackHelper.Deserialize(typeof(OfflineProfileIdentity), bytes, 0, bytes.Length);
            ValidateIdentity(identity);
            return identity;
        }

        private static void ValidateIdentity(OfflineProfileIdentity identity)
        {
            if (identity == null || identity.Version != 1)
            {
                throw new InvalidOperationException("UnsupportedVersion");
            }
            if (identity.Platform != "PC")
            {
                throw new InvalidOperationException("WrongPlatform");
            }
            if (!SoloSaveValidation.IsIdentifier(identity.ProfileId) || !SoloSaveValidation.IsIdentifier(identity.KeyId) ||
                string.IsNullOrWhiteSpace(identity.DisplayName) || identity.DisplayName.Length > 32 ||
                string.IsNullOrEmpty(identity.PublicKeyXml) || identity.PublicKeyXml.Length > 2048)
            {
                throw new InvalidOperationException("InvalidProfileIdentity");
            }
            using RSA rsa = RSA.Create();
            rsa.FromXmlString(identity.PublicKeyXml);
            if (rsa.ExportParameters(false).Modulus?.Length != 256 || rsa.ToXmlString(false) != identity.PublicKeyXml)
            {
                throw new InvalidOperationException("InvalidPublicKey");
            }
        }

        public static OfflineProfileIdentity OwnIdentity(SoloSaveRepository repository)
        {
            string directory = Path.Combine(repository.DirectoryPath, "Identity");
            string privatePath = Path.Combine(directory, "own.cckey");
            string publicPath = Path.Combine(directory, "own.ccidentity");
            Directory.CreateDirectory(directory);
            if (!File.Exists(privatePath))
            {
                if (File.Exists(publicPath))
                {
                    throw new InvalidOperationException("ProfilePrivateKeyMissing");
                }
                // 当前Unity Mono的RSA.Create().KeySize赋值仍生成1024位；构造参数已实际验证为2048位。
                using var rsa = new RSACryptoServiceProvider(2048) { PersistKeyInCsp = false };
                var identity = new OfflineProfileIdentity
                {
                    ProfileId = repository.ProfileId,
                    KeyId = Guid.NewGuid().ToString("N"),
                    PublicKeyXml = rsa.ToXmlString(false)
                };
                using var buffer = new MemoryStream();
                using (var writer = new BinaryWriter(buffer, Encoding.UTF8, true))
                {
                    writer.Write("CCKEY01");
                    writer.Write(identity.ProfileId);
                    writer.Write(identity.KeyId);
                    writer.Write(identity.DisplayName);
                    writer.Write(rsa.ToXmlString(true));
                }
                PublishNew(privatePath, buffer.ToArray(), acceptExisting: true);
            }
            var own = ReadPrivate(repository, out _);
            PublishIdentical(publicPath, EncodeIdentity(own));
            TrustIdentity(repository, own);
            return own;
        }

        public static byte[] AvailablePublicIdentity(SoloSaveRepository repository)
        {
            if (repository == null)
            {
                return null;
            }
            try
            {
                string path = Path.Combine(repository.DirectoryPath, "Identity", "own.ccidentity");
                if (!File.Exists(path))
                {
                    return null;
                }
                byte[] bytes = ReadLimited(path, MaxIdentityBytes);
                return DecodeIdentity(bytes).ProfileId == repository.ProfileId ? bytes : null;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or CryptographicException or MemoryPack.MemoryPackSerializationException)
            {
                // 房间仍可进行内存游戏；显式身份操作会报告实际密钥/存储错误。
                return null;
            }
        }

        private static OfflineProfileIdentity ReadPrivate(SoloSaveRepository repository, out string privateXml)
        {
            byte[] data = ReadLimited(Path.Combine(repository.DirectoryPath, "Identity", "own.cckey"), 16384);
            using var memory = new MemoryStream(data, false);
            using var reader = new BinaryReader(memory, Encoding.UTF8, true);
            if (reader.ReadString() != "CCKEY01" || reader.ReadString() != repository.ProfileId)
            {
                throw new InvalidOperationException("WrongProfile");
            }
            string key = reader.ReadString(), name = reader.ReadString();
            privateXml = reader.ReadString();
            if (memory.Position != memory.Length)
            {
                throw new InvalidOperationException("InvalidPrivateKey");
            }
            using RSA rsa = RSA.Create();
            rsa.FromXmlString(privateXml);
            var identity = new OfflineProfileIdentity { ProfileId = repository.ProfileId, KeyId = key, DisplayName = name, PublicKeyXml = rsa.ToXmlString(false) };
            ValidateIdentity(identity);
            return identity;
        }

        // 仅由本端明确的身份导入/信任动作调用；验证签名不会自动建立信任。
        public static void TrustIdentity(SoloSaveRepository repository, OfflineProfileIdentity identity)
        {
            byte[] bytes = EncodeIdentity(identity);
            string directory = Path.Combine(repository.DirectoryPath, "Identity", "Trusted");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, identity.ProfileId + ".ccidentity");
            if (File.Exists(path))
            {
                var previous = DecodeIdentity(ReadLimited(path, MaxIdentityBytes));
                if (previous.KeyId != identity.KeyId || previous.PublicKeyXml != identity.PublicKeyXml)
                {
                    throw new InvalidOperationException("ProfileKeyConflict");
                }
                return;
            }
            PublishNew(path, bytes);
        }

        public static void RequireTrusted(SoloSaveRepository repository, OfflineProfileIdentity identity)
        {
            ValidateIdentity(identity);
            string path = Path.Combine(repository.DirectoryPath, "Identity", "Trusted", identity.ProfileId + ".ccidentity");
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("ProfileNotTrusted");
            }
            var known = DecodeIdentity(ReadLimited(path, MaxIdentityBytes));
            if (known.KeyId != identity.KeyId || known.PublicKeyXml != identity.PublicKeyXml)
            {
                throw new InvalidOperationException("ProfileKeyConflict");
            }
        }

        public static byte[] Sign(SoloSaveRepository repository, string purpose, byte[] body)
        {
            OwnIdentity(repository);
            var identity = ReadPrivate(repository, out string privateXml);
            byte[] identityBytes = EncodeIdentity(identity);
            byte[] content = SigningContent(purpose, identityBytes, body);
            using RSA rsa = RSA.Create();
            rsa.FromXmlString(privateXml);
            byte[] signature = rsa.SignData(content, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory, Encoding.UTF8, true);
            writer.Write(Encoding.ASCII.GetBytes(Magic));
            writer.Write(1);
            writer.Write("PC");
            writer.Write(purpose);
            WriteBytes(writer, identityBytes);
            WriteBytes(writer, body);
            WriteBytes(writer, signature);
            return memory.ToArray();
        }

        public static OfflineVerifiedDocument Verify(byte[] document, string purpose, SoloSaveRepository trustedBy = null)
        {
            if (document == null || document.Length == 0 || document.Length > MaxDocumentBytes)
            {
                throw new InvalidOperationException("InvalidAuthorizationLength");
            }
            using var memory = new MemoryStream(document, false);
            using var reader = new BinaryReader(memory, Encoding.UTF8, true);
            if (Encoding.ASCII.GetString(reader.ReadBytes(Magic.Length)) != Magic || reader.ReadInt32() != 1)
            {
                throw new InvalidOperationException("UnsupportedVersion");
            }
            if (reader.ReadString() != "PC")
            {
                throw new InvalidOperationException("WrongPlatform");
            }
            if (reader.ReadString() != purpose)
            {
                throw new InvalidOperationException("GrantScopeDenied");
            }
            byte[] identityBytes = ReadBytes(reader, MaxIdentityBytes);
            byte[] body = ReadBytes(reader, MaxDocumentBytes - 16384);
            byte[] signature = ReadBytes(reader, 256);
            if (signature.Length != 256 || memory.Position != memory.Length)
            {
                throw new InvalidOperationException("InvalidSignature");
            }
            var identity = DecodeIdentity(identityBytes);
            using RSA rsa = RSA.Create();
            rsa.FromXmlString(identity.PublicKeyXml);
            if (!rsa.VerifyData(SigningContent(purpose, identityBytes, body), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            {
                throw new InvalidOperationException("InvalidSignature");
            }
            if (trustedBy != null)
            {
                RequireTrusted(trustedBy, identity);
            }
            return new OfflineVerifiedDocument(identity, body, signature);
        }

        private static byte[] SigningContent(string purpose, byte[] identity, byte[] body)
        {
            if (purpose is not (ProfileProof or BaselineReadRun or MatchAuthority or SuccessResult or ClaimTicket or NormalClose or ImportBranch or ReplaceSave) ||
                body == null || body.Length == 0 || body.Length > MaxDocumentBytes - 16384)
            {
                throw new InvalidOperationException("GrantScopeDenied");
            }
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory, Encoding.UTF8, true);
            writer.Write(1);
            writer.Write("PC");
            writer.Write(purpose);
            WriteBytes(writer, identity);
            WriteBytes(writer, body);
            return memory.ToArray();
        }

        private static void WriteBytes(BinaryWriter writer, byte[] bytes)
        {
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static byte[] ReadBytes(BinaryReader reader, int maximum)
        {
            int length = reader.ReadInt32();
            if (length <= 0 || length > maximum || length > reader.BaseStream.Length - reader.BaseStream.Position)
            {
                throw new InvalidOperationException("InvalidAuthorizationLength");
            }
            return reader.ReadBytes(length);
        }

        public static byte[] ReadLimited(string path, int maximum)
        {
            using var stream = File.OpenRead(path);
            if (stream.Length <= 0 || stream.Length > maximum)
            {
                throw new InvalidOperationException("InvalidAuthorizationLength");
            }
            var bytes = new byte[checked((int)stream.Length)];
            int position = 0;
            while (position < bytes.Length)
            {
                int count = stream.Read(bytes, position, bytes.Length - position);
                if (count == 0)
                {
                    throw new EndOfStreamException();
                }
                position += count;
            }
            return bytes;
        }

        public static string Export(SoloSaveRepository repository, string name, byte[] bytes)
        {
            if (string.IsNullOrEmpty(name) || Path.GetFileName(name) != name || bytes == null || bytes.Length > MaxDocumentBytes)
            {
                throw new InvalidOperationException("InvalidExport");
            }
            string directory = Path.Combine(repository.DirectoryPath, "Exports");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, name);
            PublishIdentical(path, bytes);
            return path;
        }

        private static void PublishIdentical(string path, byte[] bytes)
        {
            if (File.Exists(path))
            {
                if (!ReadLimited(path, MaxDocumentBytes).SequenceEqual(bytes))
                {
                    throw new InvalidOperationException("ProfileKeyConflict");
                }
                return;
            }
            PublishNew(path, bytes);
        }

        private static void PublishNew(string path, byte[] bytes, bool acceptExisting = false)
        {
            string temporary = Path.Combine(Path.GetDirectoryName(path), ".identity-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                try { File.Move(temporary, path); }
                catch (IOException) when (acceptExisting && File.Exists(path)) { }
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }
    }
}
#endif
