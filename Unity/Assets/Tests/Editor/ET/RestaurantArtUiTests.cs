#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
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
        private Dictionary<TKey,SpriteRenderer> ArtMap<TKey>(string field)
            => (Dictionary<TKey,SpriteRenderer>)typeof(MonoGridKitchenBoard).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Board);

        [UnityTest]
        [Category("RestaurantArtRegression")]
        [Category("RestaurantArtFinal")]
        public IEnumerator RestaurantArtAllDefinitionsDirectionsAndLayeredContentsFollowProjection()
        {
            yield return StartLauncher(menuVariant:ProductMenuVariant.Drink);
            yield return null;
            Assert.That(ArtMap<long>("chairs").Count,Is.EqualTo(Grid.Tables.Count));
            foreach(GridTableProjection table in Grid.Tables.Values)
            {
                Assert.That(ArtMap<long>("chairs")[table.Id].transform.localPosition,Is.EqualTo(Board.CellToWorld(table.Seat)),"Chair follows the actual table seat projection");
            }
            var people=new Dictionary<int,GridActorProjection>();
            var counters=new Dictionary<long,GridStationProjection>();
            var objects=new Dictionary<long,GridItemProjection>();
            var automatic=new Dictionary<long,GridAutoProcessProjection>();
            var baking=new Dictionary<long,GridBakeProjection>();
            var companions=new Dictionary<long,GridCompanionProjection>();
            var customers=new Dictionary<long,GridCustomerProjection>();
            var works=new Dictionary<long,GridFrontWorkProjection>();
            var cell=new GridCell(3,12);
            for(int definition=1;definition<=Tables.Instance.DTGridItemConfig.DataList.Count;++definition)
            {
                objects[8100+definition]=new GridItemProjection(8100+definition,Tables.Instance.DTGridItemConfig.Get(definition).Name,
                    GridItemLocation.Ground,0,cell,Array.Empty<long>(),definition);
            }
            void Render(float tick)=>Board.Render(people,counters,objects,automatic,baking,companions,customers,works,1,tick,5,null);
            Render(100);
            var views=ArtMap<long>("items");
            var itemRects=new HashSet<Rect>();
            Texture2D atlas=views[8101].sprite.texture;
            Assert.That(atlas.name,Is.EqualTo("RestaurantAtlas"));
            foreach(var view in views.Values)
            {
                Assert.That(view.sprite.texture,Is.SameAs(atlas));
                Assert.That(itemRects.Add(view.sprite.rect),Is.True,"Every configured item has a distinct authored silhouette");
            }
            Assert.That(itemRects.Count,Is.EqualTo(Tables.Instance.DTGridItemConfig.DataList.Count));
            var stationRects=new HashSet<Rect>();
            foreach(GridStationKind kind in Enum.GetValues(typeof(GridStationKind)))
            {
                for(int facing=0;facing<4;++facing)
                {
                    counters[7001]=new GridStationProjection(7001,cell,"美术工位",0,kind,orientation:(GridOrientation)facing);
                    Render(100);
                    Assert.That(stationRects.Add(ArtMap<long>("stations")[7001].sprite.rect),Is.True,"Each station direction uses its own view");
                }
            }
            Assert.That(stationRects.Count,Is.EqualTo(64));
            counters[7001]=new GridStationProjection(7001,cell,"美术灶台",8106,GridStationKind.Stove,orientation:GridOrientation.CounterClockwise90,
                footprint:new[]{cell,new GridCell(4,12)},upgradeLevel:1);
            objects[8105]=new GridItemProjection(8105,"碗",GridItemLocation.Hand,1,cell,new long[]{8104},5);
            objects[8106]=new GridItemProjection(8106,"煮锅",GridItemLocation.Counter,7001,cell,new long[]{8102,8204},6,autoProcess:9001);
            objects[8104]=new GridItemProjection(8104,"蛋液",GridItemLocation.Container,8105,cell,Array.Empty<long>(),4);
            objects[8102]=new GridItemProjection(8102,"番茄块",GridItemLocation.Container,8106,cell,Array.Empty<long>(),2);
            objects[8204]=new GridItemProjection(8204,"第二份蛋液",GridItemLocation.Container,8106,cell,Array.Empty<long>(),4);
            automatic[9001]=new GridAutoProcessProjection(9001,1,8106,7001,0,120,60,7,2,2,5,GridAutoState.Cooking,new long[]{8102,8204});
            people[1]=new GridActorProjection(1,1,cell,new GridCell(4,12),100,GridMotion.Transit,GridFacing.Right,8105,new GridCell(4,12),Array.Empty<GridCell>(),0,default,null);
            Render(101);
            var bowl=views[8105];var pot=views[8106];var actor=ArtMap<int>("actors")[1];var station=ArtMap<long>("stations")[7001];
            Assert.That(ArtMap<long>("contents")[8105].enabled,Is.True);
            Assert.That(ArtMap<long>("secondaryContents")[8106].enabled,Is.True,"Both raw pot ingredients are visible before cooking completes");
            Assert.That(ArtMap<long>("steam")[7001].enabled,Is.True);
            Assert.That(ArtMap<long>("upgrades")[7001].enabled,Is.True);
            Assert.That(station.transform.localScale.x,Is.EqualTo(1.88f).Within(.001f));
            Assert.That(pot.sortingOrder,Is.GreaterThan(station.sortingOrder));
            Assert.That(bowl.sortingOrder,Is.GreaterThan(actor.sortingOrder));
            Assert.That(bowl.transform.localPosition.x,Is.EqualTo(actor.transform.localPosition.x+.22f).Within(.001f));
            Sprite walking=actor.sprite;Render(104);Assert.That(actor.sprite,Is.Not.SameAs(walking),"Held walking has two distinct foot poses");
            Sprite frozen=actor.sprite;Render(104);Assert.That(actor.sprite,Is.SameAs(frozen),"A frozen projected tick keeps the pose unchanged");
            Sprite raw=ArtMap<long>("contents")[8106].sprite;
            automatic[9001]=new GridAutoProcessProjection(9001,1,8106,7001,0,120,120,7,2,1,5,GridAutoState.Ready,Array.Empty<long>());
            Render(104);
            Assert.That(ArtMap<long>("contents")[8106].sprite,Is.Not.SameAs(raw));
            Assert.That(ArtMap<long>("secondaryContents")[8106].enabled,Is.False);
            Assert.That(ArtMap<long>("steam")[7001].enabled,Is.False,"Completion stops working feedback");
            objects[8119]=new GridItemProjection(8119,"饮品杯",GridItemLocation.Ground,0,cell,new long[]{8118},19,boundOrder:9201);
            objects[8118]=new GridItemProjection(8118,"橙汁",GridItemLocation.Container,8119,cell,Array.Empty<long>(),18);
            objects[8105]=new GridItemProjection(8105,"脏碗",GridItemLocation.Ground,0,cell,Array.Empty<long>(),5,hygiene:ItemHygiene.Dirty);
            Render(104);
            Assert.That(ArtMap<long>("dirt")[8105].enabled,Is.True);
            Assert.That(ArtMap<long>("tickets")[8119].enabled,Is.True);
            objects[8119]=new GridItemProjection(8119,"空杯",GridItemLocation.Ground,0,cell,Array.Empty<long>(),19);
            objects[8105]=new GridItemProjection(8105,"净碗",GridItemLocation.Ground,0,cell,Array.Empty<long>(),5);
            Render(104);
            Assert.That(ArtMap<long>("contents")[8119].enabled,Is.False);
            Assert.That(ArtMap<long>("dirt")[8105].enabled,Is.False);
            Assert.That(ArtMap<long>("tickets")[8119].enabled,Is.False);
            objects.Remove(8119);Render(104);
            Assert.That(ArtMap<long>("tickets").ContainsKey(8119),Is.False,"Retired detail references leave the current view maps");
            yield return null;
        }

        [UnityTest]
        [Category("RestaurantArtRegression")]
        [Category("RestaurantArtFinal")]
        public IEnumerator RestaurantArtRealHudReopenAndWholeRunExitReleaseOwnedSlices()
        {
            yield return StartLauncher();
            yield return Command("拿 碗台 ·",true);
            var field=typeof(MonoUIGridKitchen).GetField("handArt",BindingFlags.Instance|BindingFlags.NonPublic);
            var icon=(Image)typeof(MonoUIGridKitchen).GetField("m_HandIcon",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Hud);
            Assert.That(icon.enabled,Is.True);Assert.That(icon.sprite.texture.name,Is.EqualTo("RestaurantAtlas"));
            Assert.That(icon.raycastTarget,Is.False);
            var retired=(Sprite[])field.GetValue(Hud);
            var boardSlices=(Sprite[])typeof(MonoGridKitchenBoard).GetField("artSprites",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Board);
            Presentation.CloseGridHud();yield return null;
            foreach(Sprite slice in retired)Assert.That(slice==null,Is.True,"Closing HUD releases native slices owned by that opening");
            Assert.That(boardSlices[0]!=null,Is.True,"Closing HUD does not release the retained Board owner");
            Presentation.OpenGridHud();yield return Wait(()=>Presentation.Ready,"Art HUD reopened");
            Assert.That(field.GetValue(Hud),Is.Not.SameAs(retired));
            Assert.That(((Image)typeof(MonoUIGridKitchen).GetField("m_HandIcon",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Hud)).enabled,Is.True);
            Runtime.StopProduct();yield return null;
            foreach(Sprite slice in boardSlices)Assert.That(slice==null,Is.True,"Whole runtime exit releases Board slices");
        }
    }
}
#endif
