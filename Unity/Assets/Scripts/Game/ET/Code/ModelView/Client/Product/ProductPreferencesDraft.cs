namespace ET.Client
{
    // 当前受管页面的草稿，保存值仍只归Root repository。
    public sealed class ProductPreferencesDraft : Object
    {
        public ProductPreferencesValue Value;
        public bool Busy;
        public bool Dirty = true;
        public string Feedback;
        public long Render;
    }
}
