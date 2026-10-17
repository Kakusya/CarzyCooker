#if !DOTNET
using System;
using System.IO;

namespace ET
{
    public static class CooperativeBaselineValues
    {
        public static CooperativeBaselineValue ReadValue(OfflineVerifiedDocument signed, out SoloSaveHeader header, out SoloSavePayload payload)
        {
            var value = (CooperativeBaselineValue)MemoryPackHelper.Deserialize(typeof(CooperativeBaselineValue), signed.Body, 0, signed.Body.Length);
            if (value == null || !SoloSaveValidation.IsIdentifier(value.GrantId) || !SoloSaveValidation.IsIdentifier(value.MatchId) ||
                !SoloSaveValidation.IsIdentifier(value.HostProfileId) || !SoloSaveValidation.IsIdentifier(value.HostKeyId) ||
                !value.DelegateMatchResults || !SoloSaveValidation.IsIdentifier(value.RootProfileId) ||
                !SoloSaveValidation.IsIdentifier(value.RootSlotId) || value.RootRevision <= 0 ||
                value.Record == null || value.Record.Length > SoloSaveFile.MaxPayloadBytes + SoloSaveFile.MaxHeaderBytes + 16)
            {
                throw new InvalidOperationException("GrantScopeDenied");
            }
            using var stream = new MemoryStream(value.Record, false);
            payload = SoloSaveFile.ReadPayload(stream, signed.Signer.ProfileId, out header);
            if (value.RootProfileId != (header.Version >= 5 ? header.RootProfileId : header.ProfileId) ||
                value.RootSlotId != (header.Version >= 5 ? header.RootSlotId : header.SlotId) ||
                value.RootRevision != (header.Version >= 5 ? header.RootRevision : header.Revision))
            {
                throw new InvalidOperationException("InvalidSource");
            }
            if (header.RecordKind != SoloSaveRecordKind.Continue)
            {
                throw new InvalidOperationException("ChapterCompleted");
            }
            return value;
        }
    }
}
#endif
