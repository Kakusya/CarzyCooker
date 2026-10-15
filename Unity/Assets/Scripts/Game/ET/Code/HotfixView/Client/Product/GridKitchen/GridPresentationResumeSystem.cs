using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [FriendOf(typeof(ProductResumeIntent))]
    [FriendOf(typeof(ProductRuntimeComponent))]
    public static partial class GridPresentationSystem
    {
        private static bool RefreshConnectionRecovery(this GridPresentationComponent self)
        {
            Scene scene = (Scene)self.IScene;
            ClientSessionComponent client = scene.GetComponent<ClientSessionComponent>();
            ProductRuntimeComponent runtime = client?.Runtime ?? default;
            ProductResumeIntent intent = runtime?.GetComponent<ProductResumeIntent>();
            if (intent == null || runtime.Stage is not (ProductStage.Disconnected or ProductStage.Resuming) &&
                !(client.Stage == ProductClientStage.Joined && !client.GetComponent<GridClientComponent>().Confirmed))
            {
                if(self.RecoveryDisplayed)
                {
                    self.RecoveryDisplayed=false;self.RecoveryFeedback=null;
                    self.RecoveryRuntime=default;self.RecoveryClient=default;self.RecoveryHud=default;
                    self.HudTargetsValid=false;self.HudInformationValid=false;self.HudVersion=long.MinValue;
                }
                return false;
            }
            self.Ready = false; self.PointerHeld = false; self.Highlight = null; self.HudTargetsValid=false;self.HudInformationValid=false;
            UIGridKitchen hud = self.Hud;
            if (hud?.View == null) { return true; }
            bool hasCredential=intent.Credential!=null;
            if(self.RecoveryDisplayed && self.RecoveryStage==runtime.Stage && self.RecoveryAttempt==intent.AttemptId &&
                self.RecoveryFeedback==intent.Feedback && self.RecoveryCredentialAvailable==hasCredential &&
                (ProductRuntimeComponent)self.RecoveryRuntime==runtime && (ClientSessionComponent)self.RecoveryClient==client &&
                (UIGridKitchen)self.RecoveryHud==hud && self.RecoveryHudEpoch==self.HudLoadEpoch)
            {
                return true;
            }
            self.RecoveryDisplayed=true;self.RecoveryStage=runtime.Stage;self.RecoveryAttempt=intent.AttemptId;
            self.RecoveryFeedback=intent.Feedback;self.RecoveryCredentialAvailable=hasCredential;
            self.RecoveryRuntime=runtime;self.RecoveryClient=client;self.RecoveryHud=hud;self.RecoveryHudEpoch=self.HudLoadEpoch;
            MonoUIGridKitchen view = hud.View;
            view.StatusText.text = runtime.Stage == ProductStage.Disconnected ? "连接失联" : "恢复中";
            view.HintText.text = intent.Feedback;
            view.HandText.text = "恢复后空手；断线实物由 Host 保留";
            view.SetHandItemIcon(0);
            view.SettlementText.text = ""; view.ClearTargets();
            EntityRef<ProductRuntimeComponent> runtimeRef = runtime; EntityRef<ClientSessionComponent> clientRef = client;
            EntityRef<UIGridKitchen> hudRef = hud;
            long attempt = intent.AttemptId;
            view.SetInputGuard(() =>
            {
                ProductRuntimeComponent current = runtimeRef; ClientSessionComponent originalClient = clientRef;
                ProductResumeIntent currentIntent = current?.GetComponent<ProductResumeIntent>(); UIGridKitchen originalHud = hudRef;
                return originalHud?.Available == true && originalClient != null && (ClientSessionComponent)current.Client == originalClient &&
                    currentIntent != null && currentIntent.AttemptId == attempt && current.Stage is ProductStage.Disconnected or ProductStage.Resuming;
            });
            if (runtime.Stage == ProductStage.Disconnected && intent.Credential != null)
            {
                view.AddControl("手动恢复", () => { ProductRuntimeComponent current = runtimeRef; current?.ResumeProduct().Forget(); });
            }
            if (intent.Credential != null)
            {
                view.AddControl("结束本地恢复", () => { ProductRuntimeComponent current = runtimeRef; current?.CancelProductResume(); });
            }
            return true;
        }
    }

    [Event(SceneType.ProductClient)]
    public sealed class ProductConnectionLostView : AEvent<Scene, ProductConnectionLost>
    {
        protected override UniTask Run(Scene scene, ProductConnectionLost args)
        {
            scene.GetComponent<GridPresentationComponent>()?.RefreshGridHud();
            return UniTask.CompletedTask;
        }
    }
}
