using System;
using Game;
namespace ET.Client
{
    [FriendOf(typeof(GridPresentationComponent))]
    public static partial class GridPresentationSystem
    {
        public static void OpenGridHud(this GridPresentationComponent self)
        {
            if (ProductEntryPresentationSystem.ClaimsAreVisible((Scene)self.IScene))
            {
                return;
            }
            UIGridKitchen existing=self.Hud;
            if(self.HudLoading || existing?.Available==true)return;
            self.HudLoading=true;long epoch=++self.HudLoadEpoch;EntityRef<GridPresentationComponent> original=self;
            self.GetAsyncOwner().Run(async context=>
            {
                try
                {
                    Scene scene=(Scene)self.IScene;UIComponent ui=scene.GetComponent<UIComponent>()??scene.AddComponent<UIComponent>();
                    ui.RemoveComponent<UIGridKitchen>();
                    UIGridKitchen hud=await ui.AddUIFormComponentAsync<UIGridKitchen>(UGFUIFormId.UIGridKitchen);
                    context.ThrowIfStopped();self=original;
                    if(self==null || self.HudLoadEpoch!=epoch){hud?.Dispose();return;}
                    self.Hud=hud;self.RecoveryDisplayed=false;self.HudTargetsValid=false;self.HudInformationValid=false;self.HudVersion=0;hud.View.ConfigureHud();self.TryGridReady();self.RefreshGridHud();
                }
                catch(OperationCanceledException){}
                catch(Exception error){self=original;if(self!=null && self.HudLoadEpoch==epoch){self.Failure=error.Message;self.RefreshGridHud();}}
                finally{self=original;if(self!=null && self.HudLoadEpoch==epoch)self.HudLoading=false;}
            });
        }
        public static void CloseGridHud(this GridPresentationComponent self)
        {
            self.AudioPage = null;
            self.PreferencesDraft = null;
            self.ClearOperationHelp();
            self.ResetActionAdvice();
            self.OverviewPage = GridOverviewPage.None;
            self.ResetServiceOverview();
            self.ResetServiceRecap();
            self.ResetNoticeView();
            self.ClearOrderNavigation();
            self.ResetItemLocator();
            self.ResetSupplyOverview();
            self.ResetFrontWorkboard();
            self.ClearStationInspection();
            self.ClearRenovationPreview();
            self.ResetCompanionDisplay();
            self.RoomControlPending=false;
            self.Ready=false;self.HudLoading=false;++self.HudLoadEpoch;self.PointerHeld=false;self.Highlight=null;self.HudTargetsValid=false;self.HudInformationValid=false;
            self.RecoveryDisplayed=false;self.RecoveryFeedback=null;self.RecoveryRuntime=default;self.RecoveryClient=default;self.RecoveryHud=default;
            self.HudActionFields.Clear();self.HudInformationFields.Clear();
            ((Scene)self.IScene).GetComponent<UIComponent>()?.RemoveComponent<UIGridKitchen>();self.Hud=default;
        }
        public static void GridHudClosed(this GridPresentationComponent self,UIGridKitchen hud)
        {
            if((UIGridKitchen)self.Hud!=hud)return;
            self.AudioPage = null;
            self.PreferencesDraft = null;
            self.ClearOperationHelp();
            self.ResetActionAdvice();
            self.ResetServiceOverview();
            self.ResetServiceRecap();
            self.ResetNoticeView();
            if (self.OverviewPage == GridOverviewPage.Service)
            {
                self.OverviewPage = GridOverviewPage.None;
            }
            self.ClearRenovationPreview();
            self.ClearStationInspection();
            if (self.OverviewPage is GridOverviewPage.Stations or GridOverviewPage.Settlement)
            {
                self.OverviewPage = GridOverviewPage.None;
            }
            self.ResetFrontWorkboard();
            if (self.OverviewPage == GridOverviewPage.FrontWork)
            {
                self.OverviewPage = GridOverviewPage.None;
            }
            self.ResetCompanionDisplay();
            self.RecoveryDisplayed=false;self.RecoveryFeedback=null;self.RecoveryRuntime=default;self.RecoveryClient=default;self.RecoveryHud=default;
            self.HudActionFields.Clear();self.HudInformationFields.Clear();
            self.Ready=false;self.PointerHeld=false;self.Highlight=null;self.HudTargetsValid=false;self.HudInformationValid=false;self.Hud=default;
        }
        private static AsyncOwnerComponent GridCommandOwner(this GridPresentationComponent self)
        {
            UIGridKitchen hud=self.Hud;
            if(!self.Ready || hud?.Available!=true)return null;
            AsyncOwnerComponent owner=hud.GetAsyncOwner();return owner.AsyncScope.IsActive?owner:null;
        }
    }
}
