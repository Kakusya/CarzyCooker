using CodeBind;
using System.Collections.Generic;
using System.Linq;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [MonoBehaviourBinding]
    public partial class MonoGridKitchenBoard : AETMonoUGFEntity
    {
        // XY 精灵平面统一压缩地面纵轴，相当于正对地图边俯视 45°；角色保持竖直。
        public const float GroundY = .70710678f;
        public const int RenderLayer = 30;
        private Camera boardCamera, originalCamera;
        private bool originalEnabled, configured;
        private int mapWidth, mapHeight, kitchenBottom;
        private readonly Dictionary<GridCell, SpriteRenderer> tiles = new();
        private readonly Dictionary<long, SpriteRenderer> stations = new();
        private readonly Dictionary<int, SpriteRenderer> actors = new();
        private readonly Dictionary<int, SpriteRenderer> directions = new();
        private readonly Dictionary<long, SpriteRenderer> items = new();
        private readonly Dictionary<long, Text> labels = new();
        private Font labelFont;

        private Text Label(long id, Vector3 position)
        {
            if (labels.TryGetValue(id, out Text existing)) return existing;
            if (labelFont == null) labelFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 36);
            var go = new GameObject("KitchenLabel", typeof(RectTransform), typeof(Canvas)); go.layer = RenderLayer;
            go.transform.SetParent(RenderRootTransform, false); go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * .018f;
            Canvas canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = boardCamera;
            canvas.sortingOrder = 600;
            var caption = new GameObject("Caption", typeof(RectTransform), typeof(Text)); caption.layer = RenderLayer;
            caption.transform.SetParent(go.transform, false);
            Text text = caption.GetComponent<Text>(); text.font = labelFont; text.fontSize = 24;
            text.color = new Color(.08f,.12f,.14f); text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(100, 65); labels.Add(id, text); return text;
        }
        public Camera BoardCamera => boardCamera;

        public Vector3 CellToWorld(GridCell cell) => new(cell.X, cell.Y * GroundY, 0);
        public GridCell ScreenToCell(Vector2 point)
        {
            Vector3 world = boardCamera.ScreenToWorldPoint(new Vector3(point.x, point.y, 10));
            return new GridCell(Mathf.FloorToInt(world.x + .5f), Mathf.FloorToInt(world.y / GroundY + .5f));
        }

        private SpriteRenderer CreateSprite(string name, Vector3 position, Vector3 scale, Color color, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer)); go.layer = RenderLayer;
            go.transform.SetParent(RenderRootTransform, false); go.transform.localPosition = position; go.transform.localScale = scale;
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = PaletteSpriteRenderer.sprite;
            renderer.sharedMaterial = PaletteSpriteRenderer.sharedMaterial; renderer.color = color; renderer.sortingOrder = order;
            return renderer;
        }

        public void Configure(int width, int height, int bottom)
        {
            if (configured) return;
            configured = true; mapWidth = width; mapHeight = height; kitchenBottom = bottom;
            boardCamera = ViewportTransform.GetComponent<Camera>();
            originalCamera = GameEntry.Camera.CurrentSceneCamera;
            originalEnabled = originalCamera != null && originalCamera.enabled;
            for (int y = 0; y < height; ++y)
            for (int x = 0; x < width; ++x)
            {
                GridCell cell = new(x, y);
                bool wall = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                Color color = wall ? new Color(.21f,.28f,.31f) : y >= bottom ? new Color(.7f,.77f,.71f) : new Color(.68f,.59f,.46f);
                // 小间距形成常驻淡格线，图形和点击使用相同投影。
                tiles.Add(cell, CreateSprite($"Cell {x},{y}", CellToWorld(cell), new Vector3(.97f, .97f * GroundY, 1), color, -1000));
            }
            // 保留完整相机 viewport，让既有 URP UI overlay 正常绘制；上下 HUD 遮住对应区域。
            boardCamera.rect = new Rect(0, 0, 1, 1);
            boardCamera.orthographicSize = Mathf.Max((width + .4f) / (2 * boardCamera.aspect), (height - bottom + .7f) * GroundY / 1.4f);
            boardCamera.transform.position = new Vector3((width - 1) / 2f, (height + bottom - 1) * GroundY / 2 - boardCamera.orthographicSize * .1f, -10);
            boardCamera.transform.rotation = Quaternion.identity;
            boardCamera.enabled = true;
            GameEntry.Event.Fire(this, SceneCameraEnableEventArgs.Create(boardCamera));
            Pan(0);
        }

        public void Pan(float screenDeltaY)
        {
            if (!configured) return;
            float half = boardCamera.orthographicSize;
            float lower = -.5f * GroundY + half * .6f;
            float upper = (mapHeight - .5f) * GroundY - half * .8f;
            Vector3 position = boardCamera.transform.position;
            position.y = lower <= upper ? Mathf.Clamp(position.y - screenDeltaY * 2 * half / boardCamera.pixelHeight, lower, upper) : (mapHeight - 1) * GroundY / 2;
            boardCamera.transform.position = position;
        }

        public void Render(IEnumerable<GridActorProjection> people, IEnumerable<GridStationProjection> counters,
            IEnumerable<GridItemProjection> objects, int ownMember, float tick, int stepTicks, GridCell? highlight)
        {
            GridItemProjection[] objectValues = objects.ToArray();
            foreach (SpriteRenderer tile in tiles.Values)
            {
                int y = Mathf.RoundToInt(tile.transform.localPosition.y / GroundY), x = Mathf.RoundToInt(tile.transform.localPosition.x);
                tile.color = x == 0 || x == mapWidth-1 || y == 0 || y == mapHeight-1 ? new Color(.21f,.28f,.31f) : y >= kitchenBottom ? new Color(.7f,.77f,.71f) : new Color(.68f,.59f,.46f);
            }
            GridActorProjection[] actorValues = people.ToArray();
            foreach (GridActorProjection person in actorValues)
            {
                if (person.MemberId == ownMember)
                    foreach (GridCell cell in person.Path) if (tiles.TryGetValue(cell, out SpriteRenderer tile)) tile.color = new Color(.62f,.81f,.82f);
                if (!actors.TryGetValue(person.MemberId, out SpriteRenderer renderer))
                {
                    renderer = CreateSprite($"Player {person.MemberId}", Vector3.zero, new Vector3(.46f,.62f,1), person.MemberId == ownMember ? new Color(.24f,.65f,.93f) : new Color(.93f,.45f,.33f), 0);
                    actors.Add(person.MemberId,renderer);
                    directions.Add(person.MemberId,CreateSprite($"Facing {person.MemberId}",Vector3.zero,new Vector3(.12f,.12f,1),Color.white,1));
                }
                float progress = person.Motion == GridMotion.Transit ? Mathf.Clamp01((tick - person.StepStartTick) / stepTicks) : 0;
                Vector3 foot = Vector3.Lerp(CellToWorld(person.Cell),CellToWorld(person.NextCell),progress);
                // 同格两名角色小幅横向错开，落脚点仍使用投影格。
                if (actorValues.Count(value => value.Cell == person.Cell) > 1) foot.x += person.MemberId % 2 == 0 ? .13f : -.13f;
                renderer.transform.localPosition = foot + new Vector3(0,.25f,0); renderer.sortingOrder = 200 - Mathf.RoundToInt(foot.y * 10);
                Vector3 facing = person.Facing == 0 ? Vector3.up : person.Facing == 1 ? Vector3.right : person.Facing == 2 ? Vector3.down : Vector3.left;
                directions[person.MemberId].transform.localPosition = renderer.transform.localPosition + facing * .2f;
                directions[person.MemberId].sortingOrder = renderer.sortingOrder + 1;
            }
            foreach (int member in actors.Keys.Except(actorValues.Select(value=>value.MemberId)).ToArray())
            { Destroy(actors[member].gameObject);Destroy(directions[member].gameObject);actors.Remove(member);directions.Remove(member); }
            foreach (GridStationProjection counter in counters)
            {
                if (!stations.TryGetValue(counter.Id,out SpriteRenderer renderer))
                { renderer = CreateSprite(counter.Name,CellToWorld(counter.Cell)+new Vector3(0,.12f,0),new Vector3(.88f,.65f,1),new Color(.38f,.49f,.54f),100-Mathf.RoundToInt(counter.Cell.Y*GroundY*10));stations.Add(counter.Id,renderer); }
                GridItemProjection item = objectValues.FirstOrDefault(v=>v.Id==counter.ItemId);
                string contents = item.DefinitionId==GridKitchenRules.Bowl ? "（"+(item.Contents.Count==0?"空":string.Join("、",item.Contents.Select(id=>objectValues.First(v=>v.Id==id).Name)))+"）" : "";
                Label(counter.Id,CellToWorld(counter.Cell)+new Vector3(0,-.32f,0)).text=counter.Name+"\n"+(item.Id==0?"空":item.Name+contents);
            }
            foreach (long id in items.Keys.Except(objectValues.Where(v=>v.Location!=GridItemLocation.Container).Select(v=>v.Id)).ToArray())
            { Destroy(items[id].gameObject); items.Remove(id); }
            foreach (GridItemProjection item in objectValues)
            {
                if(item.Location==GridItemLocation.Container)continue;
                if (!items.TryGetValue(item.Id,out SpriteRenderer renderer))
                {renderer=CreateSprite($"{item.Name} #{item.Id}",Vector3.zero,new Vector3(.22f,.22f,1), item.Contents.Count > 0 ? new Color(.94f,.91f,.78f) : new Color(.94f,.73f,.22f),300);items.Add(item.Id,renderer);}
                Vector3 position=CellToWorld(item.Cell);
                if (item.Location==GridItemLocation.Hand && actors.TryGetValue((int)item.HolderId,out SpriteRenderer holder))position=holder.transform.localPosition+new Vector3(.22f,.12f,0);
                else if(item.Location==GridItemLocation.Counter)position+=new Vector3(0,.26f,0);
                else { int ordinal=objects.Where(value=>value.Location==GridItemLocation.Ground && value.Cell==item.Cell).OrderBy(value=>value.Id).TakeWhile(value=>value.Id!=item.Id).Count();position.x+=(ordinal%3-1)*.2f;position.y+=(ordinal/3)*.12f; }
                renderer.transform.localPosition=position;renderer.sortingOrder=400-Mathf.RoundToInt(position.y*10);
            }
            if(highlight.HasValue && tiles.TryGetValue(highlight.Value,out SpriteRenderer target))target.color=new Color(.95f,.82f,.3f);
        }

        public void ReleaseBoard()
        {
            if (!configured) return;
            configured=false;
            if(boardCamera!=null)
            {
                boardCamera.enabled=false;
                if(GameEntry.Camera != null && GameEntry.Camera.CurrentSceneCamera==boardCamera)
                {
                    GameEntry.Event.Fire(this,SceneCameraDisableEventArgs.Create(boardCamera));
                    if(originalCamera!=null)
                    {GameEntry.Event.Fire(this,SceneCameraEnableEventArgs.Create(originalCamera));originalCamera.enabled=originalEnabled;}
                }
            }
            for(int i=RenderRootTransform.childCount-1;i>=0;--i)Destroy(RenderRootTransform.GetChild(i).gameObject);
            tiles.Clear();stations.Clear();actors.Clear();directions.Clear();items.Clear();labels.Clear();
            if(labelFont!=null)Destroy(labelFont);labelFont=null;
            originalCamera=null;
        }
    }
}
