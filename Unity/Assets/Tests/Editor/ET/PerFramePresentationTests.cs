#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using ET.Client;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest]
        public IEnumerator PerFrameHudSameVersionEventsFeedbackPanelsAndBindingUseCurrentContext()
        {
            yield return StartLauncher();
            var presentation=Presentation;long version=Grid.Version;
            var target=Button("拿 碗台 ·",true);string status=Hud.StatusText.text;
            presentation.HudVersion=0;
            EventSystem.Instance.Publish((Scene)Client.IScene,new GridProjectionApplied{Version=version});
            Assert.That(presentation.HudVersion,Is.EqualTo(version),"Event refresh records the displayed version before polling");
            for(int frame=0;frame<5;frame++)
            {
                presentation.RefreshGridHud();yield return null;
                Assert.That(Button("拿 碗台 ·",true),Is.SameAs(target));
                Assert.That(Hud.StatusText.text,Is.SameAs(status),"Unchanged fields do not format identical status again");
            }
            Grid.LastStatus=GridResultStatus.Rejected;Grid.LastReason="HandFull";presentation.RefreshGridHud();
            Assert.That(Grid.Version,Is.EqualTo(version));Assert.That(Hud.StatusText.text,Does.Contain("手中已有物品"));
            Assert.That(Button("拿 碗台 ·",true),Is.SameAs(target),"Feedback-only refresh preserves actions");
            Pointer(Button("隐藏候选加成"));yield return null;
            Assert.That(Hud.BuffActionsRectTransform.GetComponentsInChildren<Button>(false),Is.Empty);
            Pointer(Button("查看候选加成"));yield return null;
            Assert.That(Hud.BuffActionsRectTransform.GetComponentsInChildren<Button>(false).Length,Is.GreaterThan(0));
            Pointer(Button("菜谱 番茄蛋花汤"));yield return null;
            Assert.That(Hud.DetailsPanelRectTransform.gameObject.activeSelf,Is.True);Assert.That(Hud.DetailsText.text,Does.Contain("番茄"));
            Pointer(Button("关闭菜谱"));yield return null;
            Assert.That(Grid.Version,Is.EqualTo(version),"Local panels update without a projection revision");
            var retired=CaptureButtonCallback(Button("拿 碗台 ·",true));long before=Client.NextCommandId;
            long binding=Grid.BindingGeneration;
            try
            {
                ++Grid.BindingGeneration;presentation.RefreshGridHud();retired();
                Assert.That(Client.NextCommandId,Is.EqualTo(before),"Old binding callback cannot submit to replacement context");
            }
            finally
            {
                Grid.BindingGeneration=binding;
            }
            presentation.CloseGridHud();yield return null;presentation.OpenGridHud();
            yield return Wait(()=>presentation.Ready,"HUD reopens with invalidated display fields");
            Assert.That(Button("拿 碗台 ·",true),Is.Not.Null);
            Assert.That(presentation.HudVersion,Is.EqualTo(Grid.Version));
        }
        [UnityTest]
        [Category("RestaurantArtRegression")]
        public IEnumerator PerFrameBoardStableProjectionInterpolationChangesAndRemovals()
        {
            yield return StartLauncher();
            var people=new Dictionary<int,GridActorProjection>();
            var counters=new Dictionary<long,GridStationProjection>();
            var objects=new Dictionary<long,GridItemProjection>();
            var automatic=new Dictionary<long,GridAutoProcessProjection>();
            var baking=new Dictionary<long,GridBakeProjection>();
            var companions=new Dictionary<long,GridCompanionProjection>();
            var customers=new Dictionary<long,GridCustomerProjection>();
            var works=new Dictionary<long,GridFrontWorkProjection>();
            var path=new[]{new GridCell(2,13)};var origin=new GridCell(2,12);var next=new GridCell(3,12);
            people[1]=new GridActorProjection(1,1,origin,next,100,GridMotion.Transit,GridFacing.Right,8003,next,path,0,default,null);
            counters[7001]=new GridStationProjection(7001,new GridCell(4,12),"测试灶台",8004);
            objects[8002]=new GridItemProjection(8002,"后落物",GridItemLocation.Ground,0,origin,Array.Empty<long>());
            objects[8001]=new GridItemProjection(8001,"先落物",GridItemLocation.Ground,0,origin,Array.Empty<long>());
            objects[8003]=new GridItemProjection(8003,"手持",GridItemLocation.Hand,1,origin,Array.Empty<long>());
            objects[8004]=new GridItemProjection(8004,"锅",GridItemLocation.Counter,7001,new GridCell(4,12),Array.Empty<long>(),autoProcess:9001);
            objects[8005]=new GridItemProjection(8005,"锅内",GridItemLocation.Container,8004,origin,Array.Empty<long>());
            automatic[9001]=new GridAutoProcessProjection(9001,1,8004,7001,0,100,20,1,2,2,0,GridAutoState.Cooking,Array.Empty<long>());
            using(var customer=GridCustomerState.Create())
            {
                customer.CustomerId=6001;customer.ArrivalSequence=3;customer.Phase=(int)CustomerPhase.Ordered;
                customer.X=2;customer.Y=5;customer.NextX=2;customer.NextY=5;
                customers[6001]=new GridCustomerProjection(customer);
            }
            Dictionary<TKey,TValue> ViewMap<TKey,TValue>(string name)=>(Dictionary<TKey,TValue>)typeof(MonoGridKitchenBoard).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Board);
            void Render(float tick,GridCell? highlight)=>Board.Render(people,counters,objects,automatic,baking,companions,customers,works,1,tick,4,highlight);
            Render(101,path[0]);
            var actor=ViewMap<int,SpriteRenderer>("actors")[1];var itemViews=ViewMap<long,SpriteRenderer>("items");
            var labels=ViewMap<long,Text>("labels");var tiles=ViewMap<GridCell,SpriteRenderer>("tiles");
            Assert.That(actor.transform.localPosition.x,Is.EqualTo(2.25f).Within(.001f));
            Assert.That(itemViews[8001].transform.localPosition.x,Is.LessThan(itemViews[8002].transform.localPosition.x),"Ground order follows ID, not insertion");
            Assert.That(itemViews.ContainsKey(8005),Is.False,"Container contents remain hidden");
            Assert.That(labels[7001].text,Does.Contain("20%"));Assert.That(labels[6001].text,Does.Contain("顾客 3\n等餐"));
            string caption=labels[7001].text;var stableActor=actor;var stableLabel=labels[7001];
            for(int i=0;i<30;i++)Render(101,path[0]);
            Assert.That(labels[7001],Is.SameAs(stableLabel));Assert.That(labels[7001].text,Is.SameAs(caption));
            Assert.That(ViewMap<int,SpriteRenderer>("actors")[1],Is.SameAs(stableActor));
            Render(102,new GridCell(4,12));
            Assert.That(actor.transform.localPosition.x,Is.EqualTo(2.5f).Within(.001f));
            var socket=ViewMap<int,Vector3>("bistroHands")[1];
            Assert.That(itemViews[8003].transform.localPosition,Is.EqualTo(actor.transform.localPosition+socket),"Interpolated holder and current animation socket place the held item");
            Assert.That(tiles[path[0]].color,Is.EqualTo(new Color(.62f,.81f,.82f)),"Old target becomes path colour");
            Assert.That(tiles[new GridCell(4,12)].color,Is.EqualTo(new Color(.95f,.82f,.3f)));
            automatic[9001]=new GridAutoProcessProjection(9001,1,8004,7001,0,100,35,1,2,2,0,GridAutoState.Cooking,Array.Empty<long>());
            Render(102,null);Assert.That(labels[7001].text,Does.Contain("35%"));Assert.That(labels[7001].fontSize,Is.EqualTo(16));
            counters[7001]=new GridStationProjection(7001,new GridCell(4,13),"测试灶台",8004,footprint:new[]{new GridCell(4,13),new GridCell(5,13)});
            Render(102,null);
            var stationView=ViewMap<long,SpriteRenderer>("stations")[7001];
            Assert.That(stationView.transform.localPosition.x,Is.EqualTo(4.5f).Within(.001f));
            Assert.That(stationView.transform.localScale.x,Is.EqualTo(1.88f).Within(.001f));
            people.Clear();customers.Clear();objects.Remove(8002);Render(102,null);
            Assert.That(ViewMap<int,SpriteRenderer>("actors"),Is.Empty);Assert.That(ViewMap<long,SpriteRenderer>("frontPeople"),Is.Empty);
            Assert.That(labels.ContainsKey(6001),Is.False);Assert.That(itemViews.ContainsKey(8002),Is.False);
            objects.Remove(8001);objects[8002]=new GridItemProjection(8002,"移入落物",GridItemLocation.Ground,0,new GridCell(3,12),Array.Empty<long>());
            Render(102,null);
            Assert.That(itemViews[8002].transform.localPosition.x,Is.EqualTo(2.8f).Within(.001f),"Changed ground membership and cell recompute ordinal");
            Assert.That(tiles[path[0]].color,Is.EqualTo(Color.white),"Retired route restores the authored floor without tint");
            counters.Clear();Render(102,null);Assert.That(labels.ContainsKey(7001),Is.False);
            yield return null;
        }
        [UnityTest]
        public IEnumerator PerFrameViewportContinuousQueriesObserveActualLayoutChanges()
        {
            yield return StartLauncher();
            Rect ExpectedViewport()
            {
                Canvas canvas=Hud.GetComponent<Canvas>().rootCanvas;
                Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                var corners=new Vector3[4];Hud.CameraDragAreaRectTransform.GetWorldCorners(corners);
                Vector2 lower=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);
                Vector2 upper=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
                return Rect.MinMaxRect(lower.x/Screen.width,lower.y/Screen.height,upper.x/Screen.width,upper.y/Screen.height);
            }
            Rect first=Hud.MapViewport();
            for(int i=0;i<60;i++)Assert.That(Hud.MapViewport(),Is.EqualTo(first));
            Vector2 oldMin=Hud.CameraDragAreaRectTransform.offsetMin,oldMax=Hud.CameraDragAreaRectTransform.offsetMax;
            try
            {
                Hud.CameraDragAreaRectTransform.offsetMin+=new Vector2(13,31);
                Hud.CameraDragAreaRectTransform.offsetMax-=new Vector2(17,29);
                Canvas.ForceUpdateCanvases();yield return null;
                Rect changed=Hud.MapViewport();Assert.That(changed,Is.Not.EqualTo(first));
                Rect expected=ExpectedViewport();
                Assert.That(changed.x,Is.EqualTo(expected.x).Within(.00001f));
                Assert.That(changed.y,Is.EqualTo(expected.y).Within(.00001f));
                Assert.That(changed.width,Is.EqualTo(expected.width).Within(.00001f));
                Assert.That(changed.height,Is.EqualTo(expected.height).Within(.00001f));
                Board.SetHudViewport(changed);
                Assert.That(Board.InputScreenRect.width,Is.EqualTo(changed.width*Screen.width).Within(.01f));
                Assert.That(Board.InputScreenRect.height,Is.EqualTo(changed.height*Screen.height).Within(.01f));
            }
            finally
            {
                Hud.CameraDragAreaRectTransform.offsetMin=oldMin;Hud.CameraDragAreaRectTransform.offsetMax=oldMax;
                Canvas.ForceUpdateCanvases();
            }
            yield return null;
            Assert.That(Hud.MapViewport(),Is.EqualTo(first));
        }
    }
}
#endif
