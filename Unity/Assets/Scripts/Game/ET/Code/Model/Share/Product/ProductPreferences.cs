#if !DOTNET
using MemoryPack;
using System;

namespace ET
{
    public enum ProductFontSize { Standard = 0, Larger = 1, Largest = 2 }
    public enum ProductDragSpeed { Slow = 0, Standard = 1, Fast = 2 }

    [Flags]
    public enum ProductNoticeOptions { None = 0, Arrivals = 1, Processing = 2, Deadlines = 4, All = Arrivals | Processing | Deadlines }

    [MemoryPackable]
    public sealed partial class ProductPreferencesValue : Object
    {
        public int Version { get; set; } = 1;
        public ProductFontSize FontSize { get; set; } = ProductFontSize.Standard;
        public ProductDragSpeed DragSpeed { get; set; } = ProductDragSpeed.Standard;
        public ProductNoticeOptions Notices { get; set; } = ProductNoticeOptions.All;
        public int DeadlineSeconds { get; set; } = 5;
    }

    // 只有字号/拖动的旧布局，读取后在当前值中补提醒默认，不改写文件。
    [MemoryPackable]
    public sealed partial class ProductPreferencesLegacyValue : Object
    {
        public int Version { get; set; } = 1;
        public ProductFontSize FontSize { get; set; }
        public ProductDragSpeed DragSpeed { get; set; }
    }
}
#endif
