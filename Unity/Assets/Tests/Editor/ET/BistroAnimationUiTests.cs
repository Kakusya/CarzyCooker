#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ET.Client;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest]
        [Category("BistroAnimationRegression")]
        [Category("ProductEnumDirectionAndLegacy")]
        public IEnumerator BistroFramesSocketsWorkAndSingleCommittedServeFollowProjection()
        {
            yield return StartLauncher();
            var people = new Dictionary<int, GridActorProjection>();
            var counters = new Dictionary<long, GridStationProjection>();
            var objects = new Dictionary<long, GridItemProjection>();
            var automatic = new Dictionary<long, GridAutoProcessProjection>();
            var baking = new Dictionary<long, GridBakeProjection>();
            var companions = new Dictionary<long, GridCompanionProjection>();
            var customers = new Dictionary<long, GridCustomerProjection>();
            var fronts = new Dictionary<long, GridFrontWorkProjection>();
            var manual = new Dictionary<long, GridProcessProjection>();
            var cell = new GridCell(3, 12);
            const long level = 71001, generation = 71002, binding = 71003;
            long serial = 0;
            ProductCommandOutcomeValue outcome = default;
            void Actor(int direction, GridMotion motion = GridMotion.Standing, long hand = 0, long work = 0, long identity = 7)
            {
                people[1] = new GridActorProjection(1, identity, cell, cell, 0, motion, (GridFacing)direction, hand, cell,
                    Array.Empty<GridCell>(), 0, default, null, work);
            }
            void Render(float tick, long actualGeneration = generation, long actualBinding = binding, float displayTick = float.NaN)
            {
                Board.Render(people,counters,objects,automatic,baking,companions,customers,fronts,1,tick,5,null,manual,null,
                    outcome,serial,level,actualGeneration,actualBinding,displayTick);
            }
            Sprite SpriteNow() => ArtMap<int>("actors")[1].sprite;
            Sprite Expected(string clip, int facing, int frame)
            {
                int start = (int)typeof(MonoGridKitchenBoard).GetField(clip,BindingFlags.NonPublic|BindingFlags.Static).GetRawConstantValue();
                var slices = (Sprite[])typeof(MonoGridKitchenBoard).GetField("bistroSprites",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Board);
                return slices[start+facing*8+frame];
            }
            Actor(2);
            Render(0);
            Assert.That(SpriteNow().texture.name,Is.EqualTo("BistroAtlas"));
            Assert.That(SpriteNow(),Is.SameAs(Expected("BistroIdle",2,0)));
            var poses = new HashSet<Sprite>();
            for (int direction = 0; direction < 4; ++direction)
            {
                Actor(direction,GridMotion.Transit,8001);
                objects[8001] = new GridItemProjection(8001,"碗",GridItemLocation.Hand,1,cell,Array.Empty<long>(),GridKitchenRules.Bowl);
                for (int frame = 0; frame < 8; ++frame)
                {
                    Render(frame*2);
                    Assert.That(poses.Add(SpriteNow()),Is.True,"All four carry directions have eight separate frames");
                    var sockets = (Dictionary<int,Vector3>)typeof(MonoGridKitchenBoard).GetField("bistroHands",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Board);
                    Assert.That(ArtMap<long>("items")[8001].transform.localPosition,
                        Is.EqualTo(ArtMap<int>("actors")[1].transform.localPosition+sockets[1]));
                    Assert.That(ArtMap<long>("items")[8001].sortingOrder,
                        direction==0?Is.LessThan(ArtMap<int>("actors")[1].sortingOrder):Is.GreaterThan(ArtMap<int>("actors")[1].sortingOrder));
                }
            }
            Sprite frozen = SpriteNow();
            Vector3 frozenHand = ArtMap<long>("items")[8001].transform.localPosition;
            Render(14);
            Assert.That(SpriteNow(),Is.SameAs(frozen));
            Assert.That(ArtMap<long>("items")[8001].transform.localPosition,Is.EqualTo(frozenHand));
            Assert.That(people[1].HandItemId,Is.EqualTo(8001),"Rendering does not change domain projection");
            Actor(2,GridMotion.Transit);
            Render(2);
            Assert.That(SpriteNow(),Is.SameAs(Expected("BistroWalk",2,1)));
            counters[9001] = new GridStationProjection(9001,cell,"切配台",0,GridStationKind.Chopping);
            manual[9101] = new GridProcessProjection(9101,1,0,0,9001,60,10,GridWorkState.Working,1,7,0);
            Actor(2,work:9101);
            Render(4);
            Assert.That(SpriteNow(),Is.SameAs(Expected("BistroChop",2,2)));
            counters[9001] = new GridStationProjection(9001,cell,"水槽",0,GridStationKind.Wash);
            Render(4);
            Assert.That(SpriteNow(),Is.SameAs(Expected("BistroWork",2,2)),"Non-cutting work uses knife-free frames");
            Actor(2);
            ProductCommandContext context = new("match","world",1,level,generation,binding,1);
            outcome = new ProductCommandOutcomeValue(context,ProductCommandKind.Serve,GridResultStatus.Accepted,null);
            serial = 1; Render(40);
            Assert.That(SpriteNow(),Is.SameAs(Expected("BistroIdle",2,4)),"Accepted request is not a delivered meal");
            outcome = new ProductCommandOutcomeValue(context,ProductCommandKind.Serve,GridResultStatus.Executed,null);
            serial = 2; Render(40);
            Assert.That(SpriteNow(),Is.SameAs(Expected("BistroServe",2,0)));
            Render(42);
            Assert.That(SpriteNow(),Is.SameAs(Expected("BistroServe",2,1)),"Same result continues rather than restarting");
            Render(58);
            Assert.That(SpriteNow(),Is.SameAs(Expected("BistroIdle",2,5)),"Single delivery gesture ends");
            Render(60);
            Assert.That(SpriteNow(),Is.SameAs(Expected("BistroIdle",2,6)),"Replayed executed result does not retrigger");
            outcome = new ProductCommandOutcomeValue(context,ProductCommandKind.Serve,GridResultStatus.Rejected,"Rejected");
            serial = 3; Render(60);
            Assert.That(SpriteNow(),Is.SameAs(Expected("BistroIdle",2,6)));
            outcome = new ProductCommandOutcomeValue(context,ProductCommandKind.Serve,GridResultStatus.Executed,null);
            serial = 4; Render(60,actualGeneration:generation+1);
            serial = 5; Render(62,actualGeneration:generation+1);
            Assert.That(SpriteNow(),Is.SameAs(Expected("BistroIdle",2,7)),"Old level outcome is ignored");
            serial = 6; Render(64,actualBinding:binding+1);
            serial = 7; Render(66,actualBinding:binding+1);
            Assert.That(SpriteNow(),Is.SameAs(Expected("BistroIdle",2,1)),"Old binding outcome is ignored");
            Render(66,actualBinding:binding+1,displayTick:68);
            Assert.That(SpriteNow(),Is.SameAs(Expected("BistroIdle",2,2)),"Sparse snapshots do not stop the visual phase");
            Assert.That(people[1].Cell,Is.EqualTo(cell),"Animation extrapolation does not move authoritative projection");
            Render(66,actualBinding:binding+1,displayTick:68);
            Assert.That(SpriteNow(),Is.SameAs(Expected("BistroIdle",2,2)),"Frozen animation tick stays frozen independently of movement interpolation");
            yield return null;
        }
    }
}
#endif
