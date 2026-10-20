using System;
using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    public partial class MonoGridKitchenBoard
    {
        [SerializeField] private Texture2D m_BistroAtlas;
        private Sprite[] bistroSprites;
        private float bistroTick;
        // 仅缓存本轮显示的挂点和短动作起点；不保存业务状态、消息或等待者。
        private readonly Dictionary<int, Vector3> bistroHands = new(8);
        private long bistroOutcomeSerial, bistroLevelId, bistroLevelGeneration, bistroBindingGeneration;
        private int bistroServeMember;
        private long bistroServeGeneration;
        private float bistroServeTick = float.NegativeInfinity;

        private void ValidateBistroAtlas()
        {
            if (m_BistroAtlas == null || m_BistroAtlas.width != BistroColumns * BistroCell || m_BistroAtlas.height != BistroRows * BistroCell)
            {
                throw new InvalidOperationException("Bistro animation atlas is missing or has an invalid layout.");
            }
        }

        private void InitializeBistroArt()
        {
            bistroSprites = new Sprite[BistroCount];
            for (int i = 0; i < BistroCount; ++i)
            {
                Vector2 pivot = i < BistroSoup || i >= BistroWork && i < BistroWork + 32 ? new Vector2(.5f, BistroFootY) : new Vector2(.5f, .5f);
                bistroSprites[i] = Sprite.Create(m_BistroAtlas,
                    new Rect(i % BistroColumns * BistroCell, i / BistroColumns * BistroCell, BistroCell, BistroCell),
                    pivot, BistroCell / BistroSpans[i], 0, SpriteMeshType.FullRect);
                bistroSprites[i].name = "BistroArt " + i;
            }
        }

        private static int BistroFrame(float tick)
        {
            return Mathf.FloorToInt(tick * GridKitchenRules.TickMilliseconds / 100f) & 7;
        }

        private void ObserveBistroOutcome(int member, Dictionary<int, GridActorProjection> people, float tick,
            ProductCommandOutcomeValue outcome, long serial, long levelId, long levelGeneration, long bindingGeneration)
        {
            if (bistroLevelId != levelId || bistroLevelGeneration != levelGeneration || bistroBindingGeneration != bindingGeneration)
            {
                bistroLevelId = levelId;
                bistroLevelGeneration = levelGeneration;
                bistroBindingGeneration = bindingGeneration;
                bistroOutcomeSerial = serial;
                bistroServeTick = float.NegativeInfinity;
                bistroHands.Clear();
                return;
            }
            if (serial <= bistroOutcomeSerial)
            {
                return;
            }
            bistroOutcomeSerial = serial;
            if (outcome.Kind == ProductCommandKind.Serve && outcome.Status == GridResultStatus.Executed &&
                outcome.Context.LevelId == levelId && outcome.Context.LevelGeneration == levelGeneration && outcome.Context.BindingGeneration == bindingGeneration &&
                people.TryGetValue(member, out GridActorProjection actor))
            {
                bistroServeMember = member;
                bistroServeGeneration = actor.Generation;
                bistroServeTick = tick;
            }
        }

        private void ApplyBistroCharacter(SpriteRenderer renderer, GridActorProjection actor, bool chopping, bool working, float tick)
        {
            int clip, frame = BistroFrame(tick);
            if (chopping)
            {
                clip = BistroChop;
            }
            else if (working)
            {
                clip = BistroWork;
            }
            else if (actor.Motion == GridMotion.Transit)
            {
                clip = actor.HandItemId == 0 ? BistroWalk : BistroCarry;
            }
            else if (!working && actor.MemberId == bistroServeMember && actor.Generation == bistroServeGeneration &&
                tick >= bistroServeTick && tick - bistroServeTick < 16)
            {
                clip = BistroServe;
                frame = Mathf.Min(7, Mathf.FloorToInt((tick - bistroServeTick) * .5f));
            }
            else if (actor.HandItemId != 0)
            {
                clip = BistroCarry;
                frame = 0;
            }
            else
            {
                clip = BistroIdle;
            }
            int index = clip + (int)actor.Facing * 8 + frame;
            renderer.sprite = bistroSprites[index];
            renderer.color = Color.white;
            renderer.transform.localScale = new Vector3(.82f, .82f, 1);
            Vector2 socket = BistroHands[index];
            bistroHands[actor.MemberId] = new Vector3((socket.x - .5f) * 1.95f * .82f,
                (socket.y - BistroFootY) * 1.95f * .82f + .055f, 0);
        }

        private Vector3 HandPosition(int member, SpriteRenderer holder)
        {
            return holder.transform.localPosition + (bistroHands.TryGetValue(member, out Vector3 point) ? point : new Vector3(.22f, .12f, 0));
        }

        private static int BistroItemIndex(int definition, float tick)
        {
            return definition switch
            {
                GridKitchenRules.Tomato => BistroTomato,
                GridKitchenRules.TomatoChunks => BistroTomatoChunks,
                GridKitchenRules.Egg => BistroEgg,
                GridKitchenRules.EggLiquid => BistroEggLiquid,
                GridKitchenRules.Bread => BistroBread,
                GridKitchenRules.Bowl => BistroBowl,
                GridKitchenRules.Pot => BistroPot,
                GridKitchenRules.DisposableCup => BistroCup,
                GridKitchenRules.Plate => BistroPlate,
                GridKitchenRules.Toast => BistroToast,
                GridKitchenRules.Soup => BistroSoup + BistroFrame(tick),
                GridKitchenRules.OrangeJuice => BistroJuice + BistroFrame(tick),
                _ => -1
            };
        }

        private Sprite BistroItem(int definition)
        {
            int index = BistroItemIndex(definition, bistroTick);
            return index >= 0 ? bistroSprites[index] : null;
        }

        public static Sprite CreateHudItemArtSlice(Texture2D atlas, Texture2D bistro, int definition)
        {
            int index = BistroItemIndex(definition, 0);
            if (bistro == null || index < 0)
            {
                return CreateItemArtSlice(atlas, definition);
            }
            return Sprite.Create(bistro, new Rect(index % BistroColumns * BistroCell, index / BistroColumns * BistroCell, BistroCell, BistroCell),
                new Vector2(.5f, .5f), BistroCell, 0, SpriteMeshType.FullRect);
        }

        private Sprite BistroStation(GridStationProjection station)
        {
            int index = station.Kind switch
            {
                GridStationKind.Chopping => BistroChopping,
                GridStationKind.Stove => BistroStove,
                GridStationKind.Wash => BistroWash,
                GridStationKind.Oven => BistroOven,
                _ => -1
            };
            return index >= 0 ? bistroSprites[index + ((int)station.Orientation)] : null;
        }

        private void ReleaseBistroArt()
        {
            if (bistroSprites != null)
            {
                foreach (Sprite sprite in bistroSprites)
                {
                    Destroy(sprite);
                }
                bistroSprites = null;
            }
            bistroHands.Clear();
            bistroOutcomeSerial = bistroLevelId = bistroLevelGeneration = bistroBindingGeneration = 0;
            bistroServeTick = float.NegativeInfinity;
        }
    }
}
