#if !DOTNET
using System;

namespace ET
{
    [EntitySystemOf(typeof(SelectedSaveBaseline))]
    [FriendOf(typeof(SelectedSaveBaseline))]
    public static partial class SelectedSaveBaselineSystem
    {
        [EntitySystem]
        private static void Awake(this SelectedSaveBaseline self, SoloSaveHeader header, SoloSavePayload payload)
        {
            byte[] h = MemoryPackHelper.Serialize(header), p = MemoryPackHelper.Serialize(payload);
            self.Header = (SoloSaveHeader)MemoryPackHelper.Deserialize(typeof(SoloSaveHeader), h, 0, h.Length);
            self.Payload = (SoloSavePayload)MemoryPackHelper.Deserialize(typeof(SoloSavePayload), p, 0, p.Length);
        }
    }

    [EntitySystemOf(typeof(SoloSaveBinding))]
    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(SoloSaveBinding))]
    [FriendOf(typeof(SelectedSaveBaseline))]
    public static partial class SoloSaveBindingSystem
    {
        [EntitySystem]
        private static void Awake(this SoloSaveBinding self, string profile)
        {
            self.ProfileId = profile; self.Baseline = default; self.CurrentResult = default;
            self.SourceSlotId = ""; self.SourceRevision = 0; self.Completions.Clear();
            self.SourceHeader = null;
        }

        public static void BindSoloSave(this Match match, string profile, SoloSaveHeader header, SoloSavePayload payload)
        {
            if (match.Root().GetComponent<ProductRuntimeComponent>()?.Config.IsDevelopmentTest == true)
                throw new InvalidOperationException("DevelopmentTestCannotSave");
            if (!SoloSaveValidation.IsIdentifier(profile)) throw new InvalidOperationException("InvalidProfile");
            var binding = match.AddComponent<SoloSaveBinding, string>(profile);
            if (header != null)
            {
                binding.Baseline = match.AddChild<SelectedSaveBaseline, SoloSaveHeader, SoloSavePayload>(header, payload);
                binding.SourceSlotId = header.SlotId; binding.SourceRevision = header.Revision;
                binding.SourceHeader = ((SelectedSaveBaseline)binding.Baseline).Header;
            }
        }
    }
}
#endif
