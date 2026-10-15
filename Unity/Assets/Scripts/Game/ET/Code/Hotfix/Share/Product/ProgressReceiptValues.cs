#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public static class ProgressReceiptValues
    {
        public const int MaxReceipts = 512;

        public static bool Identifier(string value) => value != null && value.Length == 32 &&
            Guid.TryParseExact(value, "N", out _) && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

        public static string Key(ProgressCommitReceipt receipt) => Key(receipt.Kind,
            receipt.Kind == ProgressCommitKind.Claim ? receipt.ResultId : receipt.GrantId, receipt.ConsumerProfileId);

        public static string Key(ProgressCommitKind kind, string source, string consumer) => (int)kind + ":" + source + ":" + consumer;

        public static ProgressCommitReceipt Copy(ProgressCommitReceipt receipt) => new()
        {
            Kind = receipt.Kind, ResultId = receipt.ResultId, GrantId = receipt.GrantId, ConsumerProfileId = receipt.ConsumerProfileId,
            ProfileId = receipt.ProfileId, SlotId = receipt.SlotId, Revision = receipt.Revision
        };

        public static void Validate(ProgressCommitReceipt[] receipts)
        {
            if (receipts == null || receipts.Length > MaxReceipts)
            {
                throw new InvalidOperationException("InvalidCommitReceipt");
            }
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var receipt in receipts)
            {
                if (receipt == null || receipt.Kind is not (ProgressCommitKind.Claim or ProgressCommitKind.ImportBranch) ||
                    !Identifier(receipt.ResultId) || !Identifier(receipt.ConsumerProfileId) ||
                    !Identifier(receipt.ProfileId) || !Identifier(receipt.SlotId) || receipt.Revision <= 0 ||
                    (receipt.Kind == ProgressCommitKind.ImportBranch ? !Identifier(receipt.GrantId)
                        : receipt.GrantId != "" && !Identifier(receipt.GrantId)) || !keys.Add(Key(receipt)))
                {
                    throw new InvalidOperationException("InvalidCommitReceipt");
                }
            }
        }

        public static ProgressCommitReceipt[] FromLegacy(SoloSaveHeader header)
        {
            if (header.ClaimedResults == null || header.ClaimSlots == null || header.ClaimRevisions == null ||
                header.ClaimedResults.Length != header.ClaimSlots.Length || header.ClaimedResults.Length != header.ClaimRevisions.Length)
            {
                throw new InvalidOperationException("InvalidClaimReceipt");
            }
            var receipts = new ProgressCommitReceipt[header.ClaimedResults.Length];
            for (int i = 0; i < receipts.Length; ++i)
            {
                string key = header.ClaimedResults[i];
                if (key?.Length != 65 || key[32] != ':')
                {
                    throw new InvalidOperationException("InvalidClaimReceipt");
                }
                receipts[i] = new ProgressCommitReceipt
                {
                    Kind = ProgressCommitKind.Claim, ResultId = key.Substring(0, 32), ConsumerProfileId = key.Substring(33),
                    ProfileId = header.ProfileId, SlotId = header.ClaimSlots[i], Revision = header.ClaimRevisions[i]
                };
            }
            Validate(receipts);
            return receipts;
        }

    }
}
#endif
