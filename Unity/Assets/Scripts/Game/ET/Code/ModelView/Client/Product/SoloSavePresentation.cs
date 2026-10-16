namespace ET.Client
{
    public enum SoloSaveListFilter { All = 0, Continue = 1, Completed = 2 }

    [ComponentOf(typeof(Scene))]
    public sealed class SoloSavePresentation : Entity, IAwake, IUpdate, IDestroy
    {
        public EntityRef<UIGridKitchen> Hud;
        public SoloSaveHeader[] Headers;
        public string Failure;
        public SoloSaveHeader ViewedResult;
        public ProductMenuVariant NewMenuVariant;
        public long Epoch;
        // 只保存本轮新开选项；不持有另一份存档或运行状态。
        public long RenderVersion;
        public bool StartPreviewVisible, PreviewPersistent;
        public ProductMenuVariant PreviewMenuVariant;
        public int PreviewChapterId;
        // 校验后的轻量头和显示文字；完整载荷只存在于所属读取请求内。
        public SoloSaveHeader InspectedSave;
        public string[] SaveInspectionRows;
        public SoloSaveListFilter SaveFilter;
        public long BrowseSerial;
        // 原领取入口定位的Slot/修订；只在本轮浏览中消费，完整载荷仍走原Select检查。
        public string RequestedSlotId;
        public int RequestedRevision;
        public bool LoadingHud, LoadingSelection, Started;
    }
}
