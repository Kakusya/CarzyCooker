namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public sealed class SoloSavePresentation : Entity, IAwake, IUpdate, IDestroy
    {
        public EntityRef<UIGridKitchen> Hud;
        public SoloSaveHeader[] Headers;
        public string Failure;
        public SoloSaveHeader ViewedResult;
        public int NewMenuVariant;
        public long Epoch;
        public bool LoadingHud, LoadingSelection, Started;
    }
}
