using System;
using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    public partial class MonoGridKitchenBoard
    {
        [SerializeField] private Texture2D m_ArtAtlas;
        // 本轮 view 拥有切片；图集纹理依旧由原 Prefab / GF 资源链持有。
        private Sprite[] artSprites;
        private readonly Dictionary<long, SpriteRenderer> contents = new(32);
        private readonly Dictionary<long, SpriteRenderer> secondaryContents = new(16);
        private readonly Dictionary<long, SpriteRenderer> dirt = new(16);
        private readonly Dictionary<long, SpriteRenderer> tickets = new(16);
        private readonly Dictionary<long, SpriteRenderer> steam = new(16);
        private readonly Dictionary<long, SpriteRenderer> upgrades = new(16);
        private readonly Dictionary<long, SpriteRenderer> chairs = new(8);

        private void RenderChairs(Dictionary<long,GridTableProjection> tables)
        {
            if (tables == null)
            {
                return;
            }
            foreach (GridTableProjection table in tables.Values)
            {
                if (!chairs.TryGetValue(table.Id,out SpriteRenderer chair))
                {
                    chair=CreateSprite("Dining chair",CellToWorld(table.Seat),new Vector3(.8f,.8f,1),Color.white,0);
                    chair.sprite=artSprites[5];
                    chairs.Add(table.Id,chair);
                }
                chair.transform.localPosition=CellToWorld(table.Seat);
                chair.sortingOrder=999-Mathf.RoundToInt(table.Seat.Y*GroundY*100);
            }
        }

        private void InitializeArt()
        {
            if (m_ArtAtlas == null || m_ArtAtlas.width != ArtColumns * ArtCell || m_ArtAtlas.height != ArtRows * ArtCell)
            {
                throw new InvalidOperationException("Restaurant art atlas is missing or has an invalid layout.");
            }
            artSprites = new Sprite[ArtCount];
            for (int index = 0; index < ArtCount; ++index)
            {
                float span = index < ArtStationStart ? 1.65f : index < ArtItemStart ? 1.7f :
                    index < ArtCharacterStart ? 1.15f : index < ArtEffectStart ? 1.85f : 1.15f;
                Vector2 pivot = index >= ArtCharacterStart && index < ArtEffectStart ? new Vector2(.5f, .17f) : new Vector2(.5f, .5f);
                artSprites[index] = Sprite.Create(m_ArtAtlas,
                    new Rect(index % ArtColumns * ArtCell, index / ArtColumns * ArtCell, ArtCell, ArtCell),
                    pivot, ArtCell / span, 0, SpriteMeshType.FullRect);
                artSprites[index].name = "RestaurantArt " + index;
            }
        }

        private void ApplyCharacterArt(SpriteRenderer renderer, int skin, int facing, GridMotion motion,
            bool carrying, bool working, float tick)
        {
            int frame = Mathf.FloorToInt(tick / 3) & 1;
            int pose = working ? 4 + frame : carrying ? motion == GridMotion.Transit ? 6 + frame : 3 : motion == GridMotion.Transit ? 1 + frame : 0;
            renderer.sprite = artSprites[ArtCharacterStart + skin * 4 * ArtPoseCount + (facing & 3) * ArtPoseCount + pose];
            renderer.color = Color.white;
            renderer.transform.localScale = new Vector3(.82f, .82f, 1);
        }

        private static int FacingFromMotion(GridCell cell, GridCell next)
        {
            if (next.X > cell.X)
            {
                return 1;
            }
            if (next.X < cell.X)
            {
                return 3;
            }
            return next.Y > cell.Y ? 0 : 2;
        }

        private Sprite ItemArt(int definition)
        {
            // Projection 的旧测试构造器允许 definition=0；正式物品由已校验配置提供 1–21。
            return definition > 0 && definition <= 21 ? artSprites[ArtItemStart + definition - 1] : PaletteSpriteRenderer.sprite;
        }

        private void ApplyStationArt(GridStationProjection station, SpriteRenderer renderer,
            GridAutoProcessProjection batch, GridBakeProjection bake, float tick)
        {
            renderer.sprite = artSprites[ArtStationStart + (int)station.Kind * 4 + (station.Orientation & 3)];
            renderer.color = Color.white;
            int minY = station.Cell.Y, maxY = minY;
            for (int i = 0; i < station.Footprint.Count; ++i)
            {
                minY = Math.Min(minY, station.Footprint[i].Y);
                maxY = Math.Max(maxY, station.Footprint[i].Y);
            }
            Vector3 scale = renderer.transform.localScale;
            scale.y = (maxY - minY) * GroundY + .95f;
            renderer.transform.localScale = scale;
            // 前沿作为家具遮挡基准，与角色脚底使用同一排序刻度。
            renderer.sortingOrder = 1000 - Mathf.RoundToInt((minY - .38f) * GroundY * 100);
            bool active = batch.Id != 0 && batch.State == GridAutoState.Cooking || bake.Id != 0 && bake.State == GridBakeState.Baking;
            SetDecoration(steam, station.Id, renderer, ArtEffectStart, active,
                new Vector3(0, .67f + ((Mathf.FloorToInt(tick / 4) & 1) == 0 ? 0 : .03f), 0), .55f, 3);
            SetDecoration(upgrades, station.Id, renderer, ArtEffectStart + 3, station.UpgradeLevel > 0,
                new Vector3(.31f, -.22f, 0), .3f, 2);
        }

        private void ApplyItemArt(GridItemProjection item, SpriteRenderer renderer,
            Dictionary<long, GridItemProjection> objects, Dictionary<long, GridAutoProcessProjection> automatic)
        {
            renderer.sprite = ItemArt(item.DefinitionId);
            renderer.transform.localScale = new Vector3(.60f, .60f, 1);
            renderer.color = Color.white;
            int contentDefinition = 0;
            int secondDefinition = 0;
            for (int i = 0; i < item.Contents.Count; ++i)
            {
                if (objects.TryGetValue(item.Contents[i], out GridItemProjection inner))
                {
                    if (contentDefinition == 0)
                    {
                        contentDefinition = inner.DefinitionId;
                    }
                    else
                    {
                        secondDefinition = inner.DefinitionId;
                        break;
                    }
                }
            }
            automatic.TryGetValue(item.AutoProcessId, out GridAutoProcessProjection batch);
            if (batch.Id != 0 && batch.State == GridAutoState.Ready && batch.RemainingPortions > 0)
            {
                contentDefinition = batch.OutputDefinitionId;
                secondDefinition = 0;
            }
            bool hasContents = contentDefinition > 0;
            int contentIndex = hasContents && contentDefinition <= 21 ? ArtItemStart + contentDefinition - 1 : ArtItemStart;
            float contentY = item.DefinitionId == GridKitchenRules.Plate ? .045f : .18f;
            SetDecoration(contents, item.Id, renderer, contentIndex, hasContents, new Vector3(0, contentY, 0), .73f, 1);
            SetDecoration(secondaryContents, item.Id, renderer, secondDefinition > 0 ? ArtItemStart + secondDefinition - 1 : ArtItemStart,
                secondDefinition > 0, new Vector3(.10f,contentY+.035f,0), .50f, 2);
            SetDecoration(dirt, item.Id, renderer, ArtEffectStart + 1, item.Hygiene == ItemHygiene.Dirty,
                new Vector3(0, .12f, 0), .75f, 2);
            SetDecoration(tickets, item.Id, renderer, ArtEffectStart + 2, item.BoundOrderId != 0,
                new Vector3(.29f, -.02f, 0), .65f, 3);
            if (batch.Id != 0 && batch.State == GridAutoState.Ready)
            {
                TextArtCount(item.Id, renderer.transform.localPosition, batch.RemainingPortions);
            }
            else if (labels.ContainsKey(item.Id))
            {
                RemoveLabel(item.Id);
            }
        }

        private void TextArtCount(long id, Vector3 position, int portions)
        {
            var caption = Label(id, position + new Vector3(.23f, -.03f, 0));
            caption.transform.parent.localPosition = position + new Vector3(.23f, -.03f, 0);
            caption.fontSize = 14;
            caption.color = new Color(.12f, .26f, .23f);
            if (LabelChanged(id, (null, 0, portions, 0, 0)))
            {
                caption.text = portions + "份";
            }
        }

        private void SetDecoration(Dictionary<long, SpriteRenderer> map, long id, SpriteRenderer parent,
            int spriteIndex, bool visible, Vector3 position, float scale, int order)
        {
            if (!map.TryGetValue(id, out SpriteRenderer child))
            {
                if (!visible)
                {
                    return;
                }
                child = CreateSprite("Art detail", Vector3.zero, Vector3.one, Color.white, 0);
                child.transform.SetParent(parent.transform, false);
                map.Add(id, child);
            }
            child.enabled = visible;
            if (!visible)
            {
                return;
            }
            child.sprite = artSprites[spriteIndex];
            child.transform.localPosition = position;
            child.transform.localScale = new Vector3(scale, scale, 1);
            child.sortingOrder = parent.sortingOrder + order;
        }

        private void RemoveItemArt(long id)
        {
            contents.Remove(id);
            secondaryContents.Remove(id);
            dirt.Remove(id);
            tickets.Remove(id);
            RemoveLabel(id);
        }

        private void ReleaseArt()
        {
            if (artSprites != null)
            {
                foreach (Sprite sprite in artSprites) Destroy(sprite);
                artSprites = null;
            }
            contents.Clear(); secondaryContents.Clear(); dirt.Clear(); tickets.Clear(); steam.Clear(); upgrades.Clear(); chairs.Clear();
        }

        public static Sprite CreateItemArtSlice(Texture2D atlas, int definition)
        {
            if (atlas == null || definition < 1 || definition > 21)
            {
                return null;
            }
            int index = ArtItemStart + definition - 1;
            return Sprite.Create(atlas, new Rect(index % ArtColumns * ArtCell, index / ArtColumns * ArtCell, ArtCell, ArtCell),
                new Vector2(.5f,.5f), ArtCell, 0, SpriteMeshType.FullRect);
        }
    }
}
