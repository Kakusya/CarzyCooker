namespace ET
{
    public enum DevelopmentTestGroup { Chapter, Feature }

    // 从当前表派生的短目录值；不保存运行世界或正式记录载荷。
    [EnableClass]
    public sealed class DevelopmentTestLevel
    {
        public int LevelId;
        public string Name;
        public DevelopmentTestGroup Group;
        public string Failure;
    }
}
