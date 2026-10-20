using Game;

namespace ET.Client
{
    public enum ClaimInboxDisplayState { Unknown = 0, Loading = 1, Ready = 2, Failed = 3 }
    public enum ClaimListFilter { All = 0, Available = 1, Claimed = 2, Closed = 3 }
    public enum AuthorizationViewTask { Home = 0, ContinueRestaurant = 1, ImportBranch = 2, ExportToFriend = 3, ReplaceRecord = 4, Advanced = 5 }
    public enum AuthorizationExportPurpose { IndependentBranch = 0, RoomReadRun = 1, Replacement = 2 }
    public enum ProductLeaveAction { None = 0, SoloReturnModes = 1, ClientLeave = 2, HostReturnRoom = 3, HostEndRoom = 4 }
    public enum ProductEntryPage
    {
        Home = 0,
        Cooperation = 1,
        Chapters = 2,
        TrialMenus = 3,
        StartPreview = 4,
        ConnectionHelp = 5
    }

    [ComponentOf(typeof(Scene))]
    public sealed class ProductEntryPresentation : Entity, IAwake, IUpdate, IDestroy
    {
        public EntityRef<UIGridKitchen> Hud;
        public ProductPreferencesDraft PreferencesDraft;
        public ProductAudioPage AudioPage;
        public long Epoch;
        public bool LoadingHud, ChoosingSolo, Busy, Suspended;
        // 本轮页面导航；只影响可见入口，不代表房间、世界或文件状态。
        public ProductEntryPage Page;
        // 本端目录选择，不同步到房间；预览绑定打开时的身份和房间版本。
        public ProductEntryPage PreviewParent;
        public ProductMenuVariant PreviewMenuVariant;
        public int PreviewChapterId;
        public long PreviewRoomVersion, PreviewClient;
        public string Failure, JoinAddress;
        public ProductStage DisplayedStage;
        public long DisplayedRosterVersion, DisplayedRoomVersion, DisplayedClient;
        public ProductClientStage DisplayedClientStage;
        public string DisplayedRuntimeFailure;
        public bool ProgressVisible, ProgressCommitting, InspectedTrusted;
        public long ProgressSerial, ProgressRenderVersion;
        public string ProgressPath, ProgressFeedback, InspectedSummary;
        public SoloSaveHeader[] ProgressHeaders;
        public OfflineProfileIdentity InspectedIdentity;
        public byte[] InspectedAuthorization;
        public bool ClaimsVisible, ClaimsNeedsRefresh, ClosingMatch;
        public ProgressTicketSummary[] ClaimRows;
        // 本端已有repository的只读派生计数；序号隔离迟到答复，不复制持久inbox。
        public ClaimInboxDisplayState ClaimInboxState;
        public int AvailableClaimCount;
        public long ClaimInboxSerial, ClaimInboxVersion, ClaimInboxInvalidation, ClaimInboxRepository;
        public bool ClaimInboxDirty, ClaimInboxRunning;
        public string ClaimInboxError;
        // 本轮列表/详情选择；去向修订从原记录浏览答复读取，不从凭证猜测。
        public ClaimListFilter ClaimFilter;
        public string ClaimDetailTicketId, ClaimDestinationSlot;
        public SaveAuthorizationKind InspectedAuthorizationKind;
        public bool ConfirmImport;
        public string ReplaceTicketId;
        public ProgressReplacePreview ReplacePreview;
        public byte[] ReplaceGrant;
        // 只导航原文件操作，不表示已信任/授权；已检查路径锚定当前预览来源。
        public AuthorizationViewTask AuthorizationTask;
        public AuthorizationExportPurpose AuthorizationExport;
        public bool AuthorizationIdentityExpanded;
        public string AuthorizationInspectedPath;
        // 最近一次原提交答复的显示事实；不裁决提交，换任务/关闭清理。
        public string AuthorizationResultSlot, AuthorizationResultProfile, AuthorizationOutputPath;
        public int AuthorizationResultRevision;
        // 本轮离开确认；只是原控制请求的预期上下文，不持有暂停/保存/资格状态。
        public ProductLeaveAction LeaveAction;
        public bool LeavePending, LeaveInvalidated, LeaveFromRestaurant;
        public EntityRef<ClientSessionComponent> LeaveClient;
        public long LeaveSerial, LeaveBinding, LeaveRoomVersion, LeaveWorldGeneration, LeaveLevelId, LeaveLevelGeneration;
        public string LeaveMatch, LeaveWorld;
    }
}
