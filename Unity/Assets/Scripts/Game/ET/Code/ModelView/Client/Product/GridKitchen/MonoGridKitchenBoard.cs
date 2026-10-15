using CodeBind;
using System.Collections.Generic;
using System;
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
        private Rect inputViewport=new(0,0,1,1);
        private bool hudViewportConfigured;
        private float viewportAspect;
        private readonly Dictionary<GridCell, SpriteRenderer> tiles = new();
        private readonly Dictionary<long, SpriteRenderer> stations = new();
        private readonly Dictionary<int, SpriteRenderer> actors = new();
        private readonly Dictionary<int, SpriteRenderer> directions = new();
        private readonly Dictionary<long, SpriteRenderer> items = new();
        private readonly Dictionary<long, Text> labels = new();
        private readonly Dictionary<long, SpriteRenderer> frontPeople = new();
        private Font labelFont;
        // 只缓存派生显示与同步待删 ID；不持有权威实体或跨帧消息。ReleaseBoard 清有效内容。
        private readonly List<long> removedIds = new(32);
        private readonly List<int> removedMembers = new(8);
        private readonly Dictionary<long, (string Name, int Kind, int Progress, int Count, int Extra)> labelStates = new(32);
        private Dictionary<GridCell, Color> previousTints = new(128), nextTints = new(128);
        private readonly Dictionary<long,(GridCell Cell,IReadOnlyList<GridCell> Footprint)> stationGeometry = new(24);
        private readonly Dictionary<long,(GridCell Cell,int Ordinal)> groundPositions = new(32);

        private Text Label(long id, Vector3 position)
        {
            if (labels.TryGetValue(id, out Text existing)) return existing;
            if (labelFont == null) labelFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 36);
            var go = new GameObject("KitchenLabel", typeof(RectTransform), typeof(Canvas)); go.layer = RenderLayer;
            go.transform.SetParent(RenderRootTransform, false); go.transform.localPosition = position;
            go.transform.localScale = Vector3.one * .018f;
            Canvas canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = boardCamera;
            canvas.sortingOrder = 6000;
            var caption = new GameObject("Caption", typeof(RectTransform), typeof(Text)); caption.layer = RenderLayer;
            caption.transform.SetParent(go.transform, false);
            Text text = caption.GetComponent<Text>(); text.font = labelFont; text.fontSize = 18;
            text.color = new Color(.08f,.12f,.14f); text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(110, 120); labels.Add(id, text); return text;
        }
        public Camera BoardCamera => boardCamera;
        public Rect InputScreenRect=>new(inputViewport.x*Screen.width,inputViewport.y*Screen.height,inputViewport.width*Screen.width,inputViewport.height*Screen.height);

        public void SetHudViewport(Rect viewport)
        {
            if(!configured || viewport.width<=0 || viewport.height<=0)return;
            if(hudViewportConfigured && inputViewport==viewport && Mathf.Approximately(viewportAspect,boardCamera.aspect))return;
            float previousCenter=boardCamera.transform.position.y+(inputViewport.center.y-.5f)*2*boardCamera.orthographicSize;
            inputViewport=viewport;viewportAspect=boardCamera.aspect;
            float half=Mathf.Max((mapWidth+.4f)/(2*boardCamera.aspect*viewport.width),(mapHeight-kitchenBottom+.7f)*GroundY/(2*viewport.height));
            // URP 的 UI overlay 继续使用完整 viewport；按中间地图区拟合厨房和限制输入。
            boardCamera.orthographicSize=half;
            float center=hudViewportConfigured?previousCenter:(mapHeight+kitchenBottom-1)*GroundY/2;
            Vector3 position=boardCamera.transform.position;position.y=center-(viewport.center.y-.5f)*2*half;boardCamera.transform.position=position;
            hudViewportConfigured=true;Pan(0);
        }

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
            InitializeArt();
            configured = true; mapWidth = width; mapHeight = height; kitchenBottom = bottom;
            inputViewport=new Rect(0,0,1,1);hudViewportConfigured=false;viewportAspect=0;
            boardCamera = ViewportTransform.GetComponent<Camera>();
            originalCamera = GameEntry.Camera.CurrentSceneCamera;
            originalEnabled = originalCamera != null && originalCamera.enabled;
            for (int y = 0; y < height; ++y)
            for (int x = 0; x < width; ++x)
            {
                GridCell cell = new(x, y);
                bool wall = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                Color color = Color.white;
                // 小间距形成常驻淡格线，图形和点击使用相同投影。
                SpriteRenderer tile = CreateSprite($"Cell {x},{y}", CellToWorld(cell), new Vector3(.645f, .645f, 1), color, -1000);
                tile.sprite = artSprites[wall ? 2 : y >= bottom ? 0 : 1];
                tiles.Add(cell, tile);
            }
            // 边缘饰物不参与占地或点击；只在地图配置时创建。
            SpriteRenderer plant = CreateSprite("Restaurant plant", CellToWorld(new GridCell(0, bottom)), new Vector3(.65f,.65f,1), Color.white, 2000);
            plant.sprite = artSprites[3];
            SpriteRenderer window = CreateSprite("Restaurant window", CellToWorld(new GridCell(width/2,height-1))+new Vector3(0,.2f,0), new Vector3(.8f,.8f,1), Color.white, 2000);
            window.sprite = artSprites[4];
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
            float visibleHalf=half*inputViewport.height,offset=(inputViewport.center.y-.5f)*2*half;
            float lower = -GroundY+visibleHalf-offset;
            float upper = (mapHeight+.5f)*GroundY-visibleHalf-offset;
            Vector3 position = boardCamera.transform.position;
            position.y = lower <= upper ? Mathf.Clamp(position.y - screenDeltaY * 2 * half / boardCamera.pixelHeight, lower, upper) : (mapHeight - 1) * GroundY / 2;
            boardCamera.transform.position = position;
        }

        public void Render(Dictionary<int,GridActorProjection> people, Dictionary<long,GridStationProjection> counters,
            Dictionary<long,GridItemProjection> objects, Dictionary<long,GridAutoProcessProjection> automatic,
            Dictionary<long,GridBakeProjection> baking, Dictionary<long,GridCompanionProjection> companions,
            Dictionary<long,GridCustomerProjection> customers, Dictionary<long,GridFrontWorkProjection> frontWorks,
            int ownMember, float tick, int stepTicks, GridCell? highlight, Dictionary<long,GridProcessProjection> manual = null,
            Dictionary<long,GridTableProjection> tables = null)
        {
            // 客户端投影在这段同步渲染中不变；直接遍历具体字典，不经 IEnumerable 装箱或复制。
            nextTints.Clear();
            foreach (GridActorProjection person in people.Values)
            {
                if (person.MemberId == ownMember)
                {
                    for(int i=0;i<person.Path.Count;i++)nextTints[person.Path[i]]=new Color(.62f,.81f,.82f);
                }
                if (!actors.TryGetValue(person.MemberId, out SpriteRenderer renderer))
                {
                    renderer = CreateSprite($"Player {person.MemberId}", Vector3.zero, new Vector3(.46f,.62f,1), person.MemberId == ownMember ? new Color(.24f,.65f,.93f) : new Color(.93f,.45f,.33f), 0);
                    actors.Add(person.MemberId,renderer);
                    directions.Add(person.MemberId,CreateSprite($"Facing {person.MemberId}",Vector3.zero,new Vector3(.12f,.12f,1),Color.white,1));
                }
                float progress = person.Motion == GridMotion.Transit ? Mathf.Clamp01((tick - person.StepStartTick) / stepTicks) : 0;
                Vector3 foot = Vector3.Lerp(CellToWorld(person.Cell),CellToWorld(person.NextCell),progress);
                // 同格两名角色小幅横向错开，落脚点仍使用投影格。
                int sameCell=0;
                foreach(GridActorProjection other in people.Values)
                {
                    if(other.Cell==person.Cell)
                    {
                        ++sameCell;
                    }
                }
                if (sameCell > 1)
                {
                    foot.x += person.MemberId % 2 == 0 ? .13f : -.13f;
                }
                renderer.transform.localPosition = foot; renderer.sortingOrder = 1000 - Mathf.RoundToInt(foot.y * 100);
                bool working = person.WorkId != 0 && manual != null && manual.TryGetValue(person.WorkId, out GridProcessProjection process) && process.State == GridWorkState.Working;
                working |= person.FrontWorkId != 0 && frontWorks.TryGetValue(person.FrontWorkId, out GridFrontWorkProjection frontWork) && frontWork.State == FrontWorkState.Working;
                ApplyCharacterArt(renderer, person.MemberId == ownMember ? 0 : 1, person.Facing, person.Motion, person.HandItemId != 0, working, tick);
                Vector3 facing = person.Facing == 0 ? Vector3.up : person.Facing == 1 ? Vector3.right : person.Facing == 2 ? Vector3.down : Vector3.left;
                directions[person.MemberId].transform.localPosition = renderer.transform.localPosition + facing * .2f;
                directions[person.MemberId].sortingOrder = renderer.sortingOrder + 1;
                directions[person.MemberId].enabled = person.MemberId == ownMember;
            }
            removedMembers.Clear();
            foreach(int member in actors.Keys)
            {
                if(!people.ContainsKey(member))
                {
                    removedMembers.Add(member);
                }
            }
            foreach (int member in removedMembers)
            { Destroy(actors[member].gameObject);Destroy(directions[member].gameObject);actors.Remove(member);directions.Remove(member); }
            foreach (GridCompanionProjection companion in companions.Values)
            {
                for(int i=0;i<companion.Path.Count;i++)nextTints[companion.Path[i]]=new Color(.79f,.75f,.92f);
                frontWorks.TryGetValue(companion.WorkId,out GridFrontWorkProjection work);
                RenderFrontPerson(companion.Id, companion.Cell, companion.NextCell, companion.Motion, companion.StepStartTick,
                    tick, stepTicks, new Color(.69f,.43f,.89f));
                ApplyCharacterArt(frontPeople[companion.Id], 1, FacingFromMotion(companion.Cell,companion.NextCell), companion.Motion, false, work.State == FrontWorkState.Working, tick);
                var state=(companion.Name,work.Id==0?-1:(int)work.Kind,(int)Math.Round(work.Progress*100),companion.CompletedCount,0);
                if(LabelChanged(companion.Id,state))
                {
                    string action=work.Id==0?"待命":work.Kind switch{FrontWorkKind.Ask=>"询问",FrontWorkKind.Wash=>"洗碗",_=>"清桌"};
                    string progress=work.Id==0?"":$" {work.Progress:P0}";
                    labels[companion.Id].text=companion.Name+"\n"+action+progress+$" · {companion.CompletedCount}/3";
                }
            }
            foreach (GridCustomerProjection customer in customers.Values)
            {
                RenderFrontPerson(customer.Id, customer.Cell, customer.NextCell, customer.Motion, customer.StepStartTick,
                    tick, stepTicks, new Color(.93f,.67f,.33f));
                ApplyCharacterArt(frontPeople[customer.Id], 2 + (customer.ArrivalSequence & 1), FacingFromMotion(customer.Cell,customer.NextCell), customer.Motion, false, false, tick);
                if(LabelChanged(customer.Id,(null,(int)customer.Phase,0,customer.ArrivalSequence,1)))
                {
                    string phase=customer.Phase switch{CustomerPhase.WaitingAsk=>"等询问",CustomerPhase.Ordered=>"等餐",CustomerPhase.Eating=>"用餐",CustomerPhase.Leaving=>"离店",CustomerPhase.Queued=>"排队",_=>"走位"};
                    labels[customer.Id].text=$"顾客 {customer.ArrivalSequence}\n{phase}";
                }
            }
            removedIds.Clear();
            foreach(long id in frontPeople.Keys)
            {
                if(!companions.ContainsKey(id) && !customers.ContainsKey(id))
                {
                    removedIds.Add(id);
                }
            }
            foreach(long id in removedIds){Destroy(frontPeople[id].gameObject);frontPeople.Remove(id);RemoveLabel(id);}
            removedIds.Clear();
            foreach(long id in stations.Keys)
            {
                if(!counters.ContainsKey(id))
                {
                    removedIds.Add(id);
                }
            }
            foreach(long id in removedIds)
            {
                Destroy(stations[id].gameObject);stations.Remove(id);stationGeometry.Remove(id);steam.Remove(id);upgrades.Remove(id);RemoveLabel(id);
                if(chairs.TryGetValue(id,out SpriteRenderer chair))
                {
                    Destroy(chair.gameObject);chairs.Remove(id);
                }
            }
            foreach (GridStationProjection counter in counters.Values)
            {
                if (!stations.TryGetValue(counter.Id,out SpriteRenderer renderer))
                { renderer = CreateSprite(counter.Name,CellToWorld(counter.Cell)+new Vector3(0,.12f,0),new Vector3(.88f,.65f,1),new Color(.38f,.49f,.54f),100-Mathf.RoundToInt(counter.Cell.Y*GroundY*10));stations.Add(counter.Id,renderer); }
                bool selected=false;
                if(highlight.HasValue)
                {
                    for(int i=0;i<counter.Footprint.Count;i++)selected|=counter.Footprint[i]==highlight.Value;
                }
                if(!stationGeometry.TryGetValue(counter.Id,out var geometry) || geometry.Cell!=counter.Cell || !SameCells(geometry.Footprint,counter.Footprint))
                {
                    int minX=counter.Footprint[0].X,maxX=minX,minY=counter.Footprint[0].Y,maxY=minY;
                    for(int i=1;i<counter.Footprint.Count;i++)
                    {
                        GridCell cell=counter.Footprint[i];minX=Math.Min(minX,cell.X);maxX=Math.Max(maxX,cell.X);minY=Math.Min(minY,cell.Y);maxY=Math.Max(maxY,cell.Y);
                    }
                    renderer.transform.localPosition=new Vector3((minX+maxX)*.5f,(minY+maxY)*.5f*GroundY+.12f,0);
                    renderer.transform.localScale=new Vector3(maxX-minX+.88f,(maxY-minY)*GroundY+.65f,1);
                    renderer.sortingOrder=100-Mathf.RoundToInt(counter.Cell.Y*GroundY*10);
                    // Footprint 是客户端独立只读值；仅保留上一份几何输入，不保存消息或实体。
                    stationGeometry[counter.Id]=(counter.Cell,counter.Footprint);
                }
                objects.TryGetValue(counter.ItemId,out GridItemProjection item);
                automatic.TryGetValue(item.AutoProcessId,out GridAutoProcessProjection batch);
                baking.TryGetValue(counter.BakeProcessId,out GridBakeProjection bake);
                ApplyStationArt(counter, renderer, batch, bake, tick);
                int batchProgress=batch.Id==0?0:batch.State==GridAutoState.Cooking?batch.ProgressTicks*100/batch.RequiredTicks:batch.RemainingPortions;
                int bakeProgress=bake.Id==0?0:bake.State==GridBakeState.Baking?bake.ProgressTicks*100/bake.RequiredTicks:100;
                Text caption = Label(counter.Id,CellToWorld(counter.Cell)+new Vector3(0,-.32f,0));
                Vector3 labelPosition=CellToWorld(counter.Cell)+new Vector3(0,-.32f,0);
                if(caption.transform.parent.localPosition!=labelPosition)
                {
                    caption.transform.parent.localPosition=labelPosition;
                }
                // 地图保留短名称与进度；容器内容在选中目标详情中展开。
                if(LabelChanged(counter.Id,(counter.Name,batch.Id==0?-1:(int)batch.State,batchProgress,bake.Id==0?-1:(int)bake.State,bakeProgress)))
                {
                    string progress=batch.Id==0?"":batch.State==GridAutoState.Cooking?$"\n煮制 {batchProgress}%":$"\n已熟 · {batch.RemainingPortions} 份";
                    if(bake.Id!=0)
                    {
                        progress+=bake.State==GridBakeState.Baking?$"\n烘烤 {bakeProgress}%":"\n烘烤完成";
                    }
                    caption.text=progress.Length>0?counter.Name+progress:"";
                }
                caption.enabled = selected || batch.Id != 0 || bake.Id != 0;
                if(selected && caption.text.Length==0)
                {
                    caption.text=counter.Name;
                }
                int fontSize=selected?18:16;
                if(caption.fontSize!=fontSize)
                {
                    caption.fontSize=fontSize;
                }
            }
            RenderChairs(tables);
            removedIds.Clear();
            foreach(long id in items.Keys)
            {
                if(!objects.TryGetValue(id,out GridItemProjection value) || value.Location==GridItemLocation.Container)
                {
                    removedIds.Add(id);
                }
            }
            foreach (long id in removedIds)
            { Destroy(items[id].gameObject); items.Remove(id); RemoveItemArt(id); }
            bool groundChanged=false;int groundCount=0;
            foreach(GridItemProjection item in objects.Values)
            {
                if(item.Location!=GridItemLocation.Ground)
                {
                    continue;
                }
                ++groundCount;
                if(!groundPositions.TryGetValue(item.Id,out var previous) || previous.Cell!=item.Cell)
                {
                    groundChanged=true;
                }
            }
            if(groundChanged || groundCount!=groundPositions.Count)
            {
                groundPositions.Clear();
                foreach(GridItemProjection item in objects.Values)
                {
                    if(item.Location!=GridItemLocation.Ground)
                    {
                        continue;
                    }
                    int ordinal=0;
                    foreach(GridItemProjection other in objects.Values)
                    {
                        if(other.Location==GridItemLocation.Ground && other.Cell==item.Cell && other.Id<item.Id)
                        {
                            ++ordinal;
                        }
                    }
                    groundPositions.Add(item.Id,(item.Cell,ordinal));
                }
            }
            foreach (GridItemProjection item in objects.Values)
            {
                if(item.Location==GridItemLocation.Container)
                {
                    continue;
                }
                if (!items.TryGetValue(item.Id,out SpriteRenderer renderer))
                {renderer=CreateSprite($"{item.Name} #{item.Id}",Vector3.zero,new Vector3(.22f,.22f,1), item.Contents.Count > 0 ? new Color(.94f,.91f,.78f) : new Color(.94f,.73f,.22f),300);items.Add(item.Id,renderer);}
                Vector3 position=CellToWorld(item.Cell);
                if (item.Location==GridItemLocation.Hand && actors.TryGetValue((int)item.HolderId,out SpriteRenderer holder))
                {
                    position=holder.transform.localPosition+new Vector3(.22f,.12f,0);
                }
                else if(item.Location==GridItemLocation.Counter)
                {
                    position+=new Vector3(0,.26f,0);
                }
                else if(item.Location==GridItemLocation.Table)
                {
                    position+=new Vector3(0,.30f,0);
                }
                else if(item.Location is GridItemLocation.DirtyPool or GridItemLocation.CleanPool && counters.TryGetValue(item.HolderId,out GridStationProjection pool))
                {
                    int ordinal=0;
                    for(int i=0;i<pool.PoolItemIds.Count;++i)
                    {
                        if(pool.PoolItemIds[i]==item.Id)
                        {
                            ordinal=i;
                            break;
                        }
                    }
                    position=CellToWorld(pool.Cell)+new Vector3((ordinal%2-.5f)*.20f,.26f+ordinal/2*.07f,0);
                }
                else
                {
                    int ordinal=groundPositions.TryGetValue(item.Id,out var ground)?ground.Ordinal:0;
                    position.x+=(ordinal%3-1)*.2f;position.y+=(ordinal/3)*.12f;
                }
                if(renderer.transform.localPosition!=position)
                {
                    renderer.transform.localPosition=position;
                }
                int order = item.Location == GridItemLocation.Hand && actors.TryGetValue((int)item.HolderId,out SpriteRenderer carrier)
                    ? carrier.sortingOrder + 5 : item.Location is GridItemLocation.Counter or GridItemLocation.Table or GridItemLocation.DirtyPool or GridItemLocation.CleanPool && stations.TryGetValue(item.HolderId,out SpriteRenderer surface)
                    ? surface.sortingOrder + 5 : 1000 - Mathf.RoundToInt((position.y-.05f)*100);
                if(renderer.sortingOrder!=order)
                {
                    renderer.sortingOrder=order;
                }
                ApplyItemArt(item, renderer, objects, automatic);
            }
            if(highlight.HasValue)
            {
                nextTints[highlight.Value]=new Color(.95f,.82f,.3f);
            }
            foreach(GridCell cell in previousTints.Keys)
            {
                if(!nextTints.ContainsKey(cell) && tiles.TryGetValue(cell,out SpriteRenderer tile))
                {
                    tile.color=Color.white;
                }
            }
            foreach(var tint in nextTints)
            {
                if(tiles.TryGetValue(tint.Key,out SpriteRenderer tile) && tile.color!=tint.Value)
                {
                    tile.color=tint.Value;
                }
            }
            (previousTints,nextTints)=(nextTints,previousTints);
        }

        private bool LabelChanged(long id,(string Name,int Kind,int Progress,int Count,int Extra) state)
        {
            if(labelStates.TryGetValue(id,out var previous) && previous.Equals(state))
            {
                return false;
            }
            labelStates[id]=state;return true;
        }
        private static bool SameCells(IReadOnlyList<GridCell> previous,IReadOnlyList<GridCell> current)
        {
            if(previous.Count!=current.Count)
            {
                return false;
            }
            for(int i=0;i<current.Count;i++)
            {
                if(previous[i]!=current[i])
                {
                    return false;
                }
            }
            return true;
        }
        private void RemoveLabel(long id)
        {
            if(labels.TryGetValue(id,out Text text))
            {
                Destroy(text.transform.parent.gameObject);labels.Remove(id);
            }
            labelStates.Remove(id);
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
            tiles.Clear();stations.Clear();actors.Clear();directions.Clear();items.Clear();labels.Clear();frontPeople.Clear();
            labelStates.Clear();removedIds.Clear();removedMembers.Clear();previousTints.Clear();nextTints.Clear();
            stationGeometry.Clear();groundPositions.Clear();
            ReleaseArt();
            if(labelFont!=null)Destroy(labelFont);labelFont=null;
            originalCamera=null;
        }

        private void RenderFrontPerson(long id, GridCell cell, GridCell next, GridMotion motion, long start,
            float tick, int stepTicks, Color color)
        {
            if (!frontPeople.TryGetValue(id, out SpriteRenderer renderer))
            { renderer = CreateSprite($"Front {id}", Vector3.zero, new Vector3(.42f,.6f,1), color, 200); frontPeople.Add(id, renderer); }
            float progress = motion == GridMotion.Transit ? Mathf.Clamp01((tick - start) / stepTicks) : 0;
            Vector3 foot = Vector3.Lerp(CellToWorld(cell), CellToWorld(next), progress);
            renderer.transform.localPosition = foot; renderer.sortingOrder = 1000 - Mathf.RoundToInt(foot.y * 100);
            Text label = Label(id, foot + new Vector3(0,.75f,0)); label.transform.parent.localPosition = foot + new Vector3(0,.75f,0);
        }
    }
}
