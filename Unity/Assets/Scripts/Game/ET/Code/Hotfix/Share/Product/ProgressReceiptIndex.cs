#if !DOTNET
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ET
{
    // 只索引已发布Header中的轻量事实；不保存第二份载荷或独立消费文件。
    [FriendOf(typeof(SoloSaveRepository))]
    public static class ProgressReceiptIndex
    {
        public static Dictionary<string, ProgressCommitReceipt> Read(SoloSaveRepository repository)
        {
            var receipts = new Dictionary<string, ProgressCommitReceipt>(StringComparer.Ordinal);
            string pcDirectory = Path.GetDirectoryName(repository.DirectoryPath);
            foreach (string directory in Directory.EnumerateDirectories(pcDirectory).OrderBy(path => path, StringComparer.Ordinal))
            {
                string owner = Path.GetFileName(directory);
                if (!SoloSaveValidation.IsIdentifier(owner))
                {
                    continue;
                }
                foreach (string path in Directory.EnumerateFiles(directory, "*.ccsave"))
                {
                    string slot = Path.GetFileNameWithoutExtension(path);
                    if (!SoloSaveValidation.IsIdentifier(slot))
                    {
                        continue;
                    }
                    using var stream = File.OpenRead(path);
                    var header = SoloSaveFile.ReadHeader(stream);
                    SoloSaveValidation.Header(header, owner);
                    if (header.SlotId != slot)
                    {
                        throw new InvalidOperationException("SlotIdentityMismatch");
                    }
                    foreach (var receipt in header.Receipts)
                    {
                        if (receipt.ConsumerProfileId == repository.ProfileId)
                        {
                            Add(receipts, receipt);
                        }
                    }
                }
            }
            return receipts;
        }

        public static void Add(Dictionary<string, ProgressCommitReceipt> receipts, ProgressCommitReceipt value)
        {
            ProgressReceiptValues.Validate(new[] { value });
            string key = ProgressReceiptValues.Key(value);
            if (receipts.TryGetValue(key, out var prior) &&
                (prior.ProfileId != value.ProfileId || prior.SlotId != value.SlotId || prior.Revision != value.Revision || prior.GrantId != value.GrantId))
            {
                throw new InvalidOperationException("CommitReceiptConflict");
            }
            receipts[key] = ProgressReceiptValues.Copy(value);
        }

        public static ProgressCommitReceipt Find(SoloSaveRepository repository, ProgressCommitKind kind, string source, string consumer)
        {
            return Read(repository).TryGetValue(ProgressReceiptValues.Key(kind, source, consumer), out var value) ? ProgressReceiptValues.Copy(value) : null;
        }

        public static ProgressCommitReceipt[] Values(Dictionary<string, ProgressCommitReceipt> receipts)
        {
            var values = receipts.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => ProgressReceiptValues.Copy(pair.Value)).ToArray();
            ProgressReceiptValues.Validate(values);
            return values;
        }
    }
}
#endif
