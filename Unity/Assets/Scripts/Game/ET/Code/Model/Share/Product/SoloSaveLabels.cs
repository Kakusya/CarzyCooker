#if !DOTNET
using System;
using MemoryPack;

namespace ET
{
    [MemoryPackable]
    public sealed partial class SoloSaveLabel : Object
    {
        public string SlotId { get; set; }
        public string Name { get; set; } = "";
        public bool Favorite { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SoloSaveLabels : Object
    {
        public int Version { get; set; } = 1;
        public string ProfileId { get; set; }
        public SoloSaveLabel[] Entries { get; set; } = Array.Empty<SoloSaveLabel>();
    }
}
#endif
