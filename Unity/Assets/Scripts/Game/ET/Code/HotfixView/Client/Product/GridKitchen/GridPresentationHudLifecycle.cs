using System;
using Game;
namespace ET.Client
{
    [FriendOf(typeof(GridPresentationComponent))]
    public static partial class GridPresentationSystem
    {
        public static void OpenGridHud(this GridPresentationComponent self)
        {
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
                    self.Hud=hud;self.HudTargetsKey=null;self.HudVersion=0;hud.View.ConfigureHud();self.TryGridReady();self.RefreshGridHud();
                }
                catch(OperationCanceledException){}
                catch(Exception error){self=original;if(self!=null && self.HudLoadEpoch==epoch){self.Failure=error.Message;self.RefreshGridHud();}}
                finally{self=original;if(self!=null && self.HudLoadEpoch==epoch)self.HudLoading=false;}
            });
        }
        public static void CloseGridHud(this GridPresentationComponent self)
        {
            self.Ready=false;self.HudLoading=false;++self.HudLoadEpoch;self.PointerHeld=false;self.Highlight=null;self.HudTargetsKey=null;
            ((Scene)self.IScene).GetComponent<UIComponent>()?.RemoveComponent<UIGridKitchen>();self.Hud=default;
        }
        public static void GridHudClosed(this GridPresentationComponent self,UIGridKitchen hud)
        {
            if((UIGridKitchen)self.Hud!=hud)return;
            self.Ready=false;self.PointerHeld=false;self.Highlight=null;self.HudTargetsKey=null;self.Hud=default;
        }
        private static AsyncOwnerComponent GridCommandOwner(this GridPresentationComponent self)
        {
            UIGridKitchen hud=self.Hud;
            if(!self.Ready || hud?.Available!=true)return null;
            AsyncOwnerComponent owner=hud.GetAsyncOwner();return owner.AsyncScope.IsActive?owner:null;
        }
    }
}
