using Game;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public sealed class ProductEntryPresentation : Entity, IAwake, IUpdate, IDestroy
    {
        public EntityRef<UIGridKitchen> Hud;
        public long Epoch;
        public bool LoadingHud, ChoosingSolo, Busy, Suspended;
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
        public SaveAuthorizationKind InspectedAuthorizationKind;
        public bool ConfirmImport;
        public string ReplaceTicketId;
        public ProgressReplacePreview ReplacePreview;
        public byte[] ReplaceGrant;
    }
}
