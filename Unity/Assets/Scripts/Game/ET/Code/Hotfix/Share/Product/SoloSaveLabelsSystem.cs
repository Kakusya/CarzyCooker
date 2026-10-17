#if !DOTNET
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace ET
{
    public static partial class SoloSaveRepositorySystem
    {
        public const string LabelsFileName = "labels.cclabels";
        public const int MaximumLabelElements = 32;
        private const int MaximumLabelsBytes = 256 * 1024;

        public static void ValidateLabelName(string name)
        {
            if (name == null || name.Length > 4096)
            {
                throw new InvalidOperationException("名称不可用；最多32个Unicode文本元素。");
            }
            foreach (char character in name)
            {
                if (char.IsControl(character) || character is '\u2028' or '\u2029')
                {
                    throw new InvalidOperationException("名称不能包含换行或控制字符。");
                }
            }
            if (new StringInfo(name).LengthInTextElements > MaximumLabelElements)
            {
                throw new InvalidOperationException("名称最多32个Unicode文本元素。");
            }
            // Reject malformed UTF-16 rather than persisting a replacement-character spelling.
            for (int index = 0; index < name.Length; ++index)
            {
                if (char.IsSurrogate(name[index]))
                {
                    if (!char.IsHighSurrogate(name[index]) || index + 1 == name.Length || !char.IsLowSurrogate(name[++index]))
                    {
                        throw new InvalidOperationException("名称包含不完整的Unicode字符。");
                    }
                }
            }
        }

        public static SoloSaveLabels DecodeSaveLabels(byte[] bytes, string profile)
        {
            if (bytes == null || bytes.Length is <= 0 or > MaximumLabelsBytes)
            {
                throw new InvalidOperationException("本机名称与收藏文件不可用，原文件已保留。");
            }
            var value = (SoloSaveLabels)MemoryPackHelper.Deserialize(typeof(SoloSaveLabels), bytes, 0, bytes.Length);
            if (value?.Version != 1 || value.ProfileId != profile || value.Entries == null)
            {
                throw new InvalidOperationException("本机名称与收藏格式或玩家不匹配，原文件已保留。");
            }
            var slots = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in value.Entries)
            {
                if (entry == null || !SoloSaveValidation.IsIdentifier(entry.SlotId) || !slots.Add(entry.SlotId))
                {
                    throw new InvalidOperationException("本机名称与收藏记录身份不可用。");
                }
                ValidateLabelName(entry.Name);
            }
            return value;
        }

        public static SoloSaveLabels ReadSaveLabels(this SoloSaveRepository self)
        {
            string path = Path.Combine(self.DirectoryPath, LabelsFileName);
            if (!File.Exists(path))
            {
                return new SoloSaveLabels { ProfileId = self.ProfileId };
            }
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length is <= 0 or > MaximumLabelsBytes)
            {
                throw new InvalidOperationException("本机名称与收藏文件损坏，原文件已保留。");
            }
            var bytes = new byte[(int)stream.Length];
            int offset = 0;
            while (offset < bytes.Length)
            {
                int count = stream.Read(bytes, offset, bytes.Length - offset);
                if (count == 0)
                {
                    throw new EndOfStreamException("本机名称与收藏文件不完整。");
                }
                offset += count;
            }
            return DecodeSaveLabels(bytes, self.ProfileId);
        }

        public static SoloSaveLabels WriteSaveLabel(this SoloSaveRepository self, string slot, int revision, string result, string name, bool favorite)
        {
            ValidateLabelName(name);
            self.Select(slot, out var header);
            if (header.Revision != revision || header.ResultId != result)
            {
                throw new InvalidOperationException("记录已经变化，请重新选择并核对当前记录。");
            }
            var before = self.ReadSaveLabels();
            var entries = before.Entries.Where(entry => entry.SlotId != slot).ToList();
            if (name != "" || favorite)
            {
                entries.Add(new SoloSaveLabel { SlotId = slot, Name = name, Favorite = favorite });
            }
            var committed = new SoloSaveLabels { ProfileId = self.ProfileId, Entries = entries.ToArray() };
            byte[] bytes = MemoryPackHelper.Serialize(committed);
            DecodeSaveLabels(bytes, self.ProfileId);
            string target = Path.Combine(self.DirectoryPath, LabelsFileName);
            string temporary = Path.Combine(self.DirectoryPath, ".labels-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                WriteTemporary(temporary, bytes);
                if (File.Exists(target))
                {
                    File.Replace(temporary, target, null);
                }
                else
                {
                    File.Move(temporary, target);
                }
                return committed;
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

    [MessageHandler(SceneType.ProductRoot)]
    [FriendOf(typeof(SoloSaveRepository))]
    public sealed class SoloSaveLabelsReadHandler : ProductActorRequestHandler<SoloSaveRepository, SoloSaveLabelsReadRequest, SoloSaveLabelsReadResponse>
    {
        protected override UniTask Run(SoloSaveRepository repository, SoloSaveLabelsReadRequest request, SoloSaveLabelsReadResponse response)
        {
            try
            {
                response.Values = MemoryPackHelper.Serialize(repository.ReadSaveLabels());
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or MemoryPack.MemoryPackSerializationException)
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = "无法读取本机名称与收藏，进度记录仍可查看：" + error.Message;
            }
            return UniTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.ProductRoot)]
    public sealed class SoloSaveLabelsWriteHandler : ProductActorRequestHandler<SoloSaveRepository, SoloSaveLabelsWriteRequest, SoloSaveLabelsWriteResponse>
    {
        protected override UniTask Run(SoloSaveRepository repository, SoloSaveLabelsWriteRequest request, SoloSaveLabelsWriteResponse response)
        {
            try
            {
                response.Values = MemoryPackHelper.Serialize(repository.WriteSaveLabel(request.SlotId, request.Revision, request.ResultId, request.Name, request.Favorite));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or MemoryPack.MemoryPackSerializationException)
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = "保存名称与收藏失败，原值保留：" + error.Message;
            }
            return UniTask.CompletedTask;
        }
    }
}
#endif
