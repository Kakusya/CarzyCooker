#if !DOTNET
using System;
using System.IO;
using Cysharp.Threading.Tasks;

namespace ET
{
    [FriendOf(typeof(ProductRuntimeComponent))]
    public static partial class SoloSaveRepositorySystem
    {
        public const string PreferencesFileName = "preferences.ccprefs";
        private const int LegacyPreferenceMemberCount = 3;

        public static ProductPreferencesValue CopyPreferences(ProductPreferencesValue value) => new()
        {
            Version = value.Version,
            FontSize = value.FontSize,
            DragSpeed = value.DragSpeed,
            Notices = value.Notices,
            DeadlineSeconds = value.DeadlineSeconds
        };

        public static void ValidatePreferences(ProductPreferencesValue value)
        {
            if (value == null || value.Version != 1 || !Enum.IsDefined(typeof(ProductFontSize), value.FontSize) ||
                !Enum.IsDefined(typeof(ProductDragSpeed), value.DragSpeed) || (value.Notices & ~ProductNoticeOptions.All) != 0 ||
                value.DeadlineSeconds is not (3 or 5 or 10))
            {
                throw new InvalidOperationException("本机设置格式或选项不可用，原设置文件已保留。");
            }
        }

        public static ProductPreferencesValue DecodePreferences(byte[] bytes)
        {
            if (bytes == null || bytes.Length is <= 0 or > 16384)
            {
                throw new InvalidOperationException("本机设置数据不可用。");
            }
            ProductPreferencesValue value;
            if (bytes[0] == LegacyPreferenceMemberCount)
            {
                var legacy = (ProductPreferencesLegacyValue)MemoryPackHelper.Deserialize(typeof(ProductPreferencesLegacyValue), bytes, 0, bytes.Length);
                value = new ProductPreferencesValue { Version = legacy.Version, FontSize = legacy.FontSize, DragSpeed = legacy.DragSpeed };
            }
            else
            {
                value = (ProductPreferencesValue)MemoryPackHelper.Deserialize(typeof(ProductPreferencesValue), bytes, 0, bytes.Length);
            }
            ValidatePreferences(value);
            return value;
        }

        private static ProductPreferencesValue ReadPreferencesFile(this SoloSaveRepository self)
        {
            string path = Path.Combine(self.DirectoryPath, PreferencesFileName);
            if (!File.Exists(path))
            {
                return new ProductPreferencesValue();
            }
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length is <= 0 or > 16384)
            {
                throw new InvalidOperationException("本机设置文件损坏，原文件已保留。");
            }
            var bytes = new byte[(int)stream.Length];
            int read = 0;
            while (read < bytes.Length)
            {
                int count = stream.Read(bytes, read, bytes.Length - read);
                if (count == 0)
                {
                    throw new EndOfStreamException("本机设置文件不完整。");
                }
                read += count;
            }
            return DecodePreferences(bytes);
        }

        public static void LoadProductPreferences(this SoloSaveRepository self)
        {
            self.Preferences ??= new ProductPreferencesValue();
            try
            {
                var value = self.ReadPreferencesFile();
                self.Preferences = CopyPreferences(value);
                self.PreferencesFailure = null;
                ++self.PreferencesVersion;
                ProductRuntimeComponent runtime = self.Root().GetComponent<ProductRuntimeComponent>();
                ClientSessionComponent client = runtime?.Client ?? default;
                if (client != null)
                {
                    EventSystem.Instance.Publish((Scene)client.IScene, new ProductPreferencesChanged { Version = self.PreferencesVersion });
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or MemoryPack.MemoryPackSerializationException)
            {
                self.PreferencesFailure = "无法读取本机设置：" + exception.Message;
            }
        }

        public static void SaveProductPreferences(this SoloSaveRepository self, ProductPreferencesValue value)
        {
            ValidatePreferences(value);
            // 既有损坏/未知文件不能被一次普通保存静默覆盖；读失败也不发布草稿。
            self.ReadPreferencesFile();
            var committed = CopyPreferences(value);
            string target = Path.Combine(self.DirectoryPath, PreferencesFileName);
            string temporary = Path.Combine(self.DirectoryPath, ".preferences-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                WriteTemporary(temporary, MemoryPackHelper.Serialize(committed));
                if (File.Exists(target))
                {
                    File.Replace(temporary, target, null);
                }
                else
                {
                    File.Move(temporary, target);
                }
                self.Preferences = committed;
                self.PreferencesFailure = null;
                ++self.PreferencesVersion;
                ProductRuntimeComponent runtime = self.Root().GetComponent<ProductRuntimeComponent>();
                ClientSessionComponent client = runtime?.Client ?? default;
                if (client != null)
                {
                    EventSystem.Instance.Publish((Scene)client.IScene, new ProductPreferencesChanged { Version = self.PreferencesVersion });
                }
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        public static float ProductDragMultiplier(ProductDragSpeed speed) => speed switch
        {
            ProductDragSpeed.Slow => .7f,
            ProductDragSpeed.Fast => 1.4f,
            _ => 1f
        };
    }

    [MessageHandler(SceneType.ProductRoot)]
    [FriendOf(typeof(SoloSaveRepository))]
    public sealed class ProductPreferencesReadHandler : ProductActorRequestHandler<SoloSaveRepository, ProductPreferencesReadRequest, ProductPreferencesReadResponse>
    {
        protected override UniTask Run(SoloSaveRepository repository, ProductPreferencesReadRequest request, ProductPreferencesReadResponse response)
        {
            repository.LoadProductPreferences();
            response.Values = MemoryPackHelper.Serialize(repository.Preferences);
            if (repository.PreferencesFailure != null)
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = repository.PreferencesFailure;
            }
            return UniTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.ProductRoot)]
    [FriendOf(typeof(SoloSaveRepository))]
    public sealed class ProductPreferencesWriteHandler : ProductActorRequestHandler<SoloSaveRepository, ProductPreferencesWriteRequest, ProductPreferencesWriteResponse>
    {
        protected override UniTask Run(SoloSaveRepository repository, ProductPreferencesWriteRequest request, ProductPreferencesWriteResponse response)
        {
            try
            {
                if (request.Values == null || request.Values.Length > 16384)
                {
                    throw new InvalidOperationException("本机设置请求不可用。");
                }
                repository.SaveProductPreferences(SoloSaveRepositorySystem.DecodePreferences(request.Values));
                response.Values = MemoryPackHelper.Serialize(repository.Preferences);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or MemoryPack.MemoryPackSerializationException)
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = "保存本机设置失败，原值保留：" + exception.Message;
            }
            return UniTask.CompletedTask;
        }
    }
}
#endif
