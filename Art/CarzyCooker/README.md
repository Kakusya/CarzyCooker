# CarzyCooker 美术制作源

手作小馆动画源、离线预览及独立Unity图集导出见[HandmadeBistro](HandmadeBistro/README.md)。原生产图集继续保留其余现有定义；两套切片均由原Board持有与释放。

风格：温暖圆润的街角小馆。模型、材质、角色姿势和图形均在本项目制作，无外部素材包。软件只用于制作，游戏使用 2D 图片。

## 源文件与导出

- `export_art.py`：完整可编辑模型源，定义软边几何、固定光向、45° 正交相机和角色动作。
- `RestaurantModels.blend`：导出后保存的可编辑模型工作台；脚本是全部方向和姿势的统一源。
- `assets.csv`：资产索引，记录种类、定义编号、朝向与姿势；由脚本生成，不手改。
- `hud.svg`：奶油面板、蓝绿按钮、金色星级图形，可在 Inkscape 编辑。
- `sample-sheet.png`：样板联系表，用于比对角色、设备、器具和汤链。

Blender 命令（仓库根执行）：

```powershell
& 'D:/MySoftware/Blender/blender-4.5.9-windows-x64/blender.exe' --background --python 'Art/CarzyCooker/export_art.py' -- --sample
& 'D:/MySoftware/Blender/blender-4.5.9-windows-x64/blender.exe' --background --python 'Art/CarzyCooker/export_art.py'
```

Inkscape：分别导出 `panel`、`button`、`badge`，保留透明背景与边框。正式输出由原 HUD Prefab 引用。

```powershell
./Art/CarzyCooker/export_hud.ps1
```

图集每格 192×192，16 列，索引从图片下方开始。生成的 `MonoGridKitchenBoard.ArtLayout.cs` 只由导出脚本更新，不能手改。方向 0／1／2／3 对应北／东／南／西；角色姿势 0 待机，1／2 行走两帧，3 持物，4／5 作业两帧，6／7 持物行走两帧。角色四种外观为玩家、伙伴、两种顾客。

环境按原格子显示；工位按原 Footprint、方向与定义映射，所有物品 ID 1–94 均有独立图形（沙拉22–31、热餐32–42、甜品43–51、咖啡52–63、蒸食64–75、茶饮76–94）。容器内容、脏污、订单票、蒸汽等由实际投影组合，不把内容画死在容器或工位上。

2026-10-17 茶饮模型由原脚本生成317格、20行图集，ArtItemCount=94；增加热水档、泡茶、煮珍珠和调饮设备四方向，以及原料、冷热杯罐、基底、冰与成品。35个新增方向/物件接触表已查看，原资源集合刷新成功；普通Launcher Solo与真实KCP三杯制作交付均通过，见[茶饮运行记录](../../openspec/changes/archive/2026-10-17-kitchen-tea-beverage-batch/execution-notes.md)。

2026-10-17 蒸食模型由原脚本生成282格、18行图集，ArtItemCount=75；新增蒸锅四方向与洋葱、碎葱、牛肉末、饺子皮、混馅、未熟/熟蒸饺、蒸食盘、拌馅盆及三种供应袋。未熟饺用深色褶线，熟饺用白色与蒸汽形状区分；原HandmadeBistro图集与动画源未改。具体运行范围见[kitchen-steamed-food-batch](../../openspec/changes/archive/2026-10-17-kitchen-steamed-food-batch/execution-notes.md)。

本轮制作／游戏验收结果见 `openspec/changes/archive/2026-10-15-restaurant-art-production/execution-notes.md`，截图为实机 Editor 输出。手机和 Player 验收属于后续设备 change。

2026-10-15 沙拉批次模型已扩展：生菜/生菜片、黄瓜/黄瓜片、芝士片、田园沙拉/芝士蔬菜沙拉及三种补给袋。原脚本生成214切片、14行图集，ArtItemCount及后续人物/效果起点随源生成；Board和HUD图标按同一生成范围取切片，无新增Prefab或材质实例。新增玩法/界面验收查[kitchen-multi-input-assembly](../../openspec/changes/archive/2026-10-15-kitchen-multi-input-assembly/execution-notes.md)，此处不替代其运行结果。

2026-10-15 热餐模型增量：新增煎台四向、汉堡面包、生/熟牛肉饼、生/熟香肠、煎蛋、双层面包夹单层肉饼的经典汉堡、可见香肠/煎蛋/吐司的三组件早餐和三种原料包装。原脚本生成229切片、15行图集，ArtItemCount=42；图集与.blend均由原导出入口更新，42种物品和48个工位方向的受管渲染UTF已通过。玩法运行范围见[kitchen-hot-food-batch](../../openspec/changes/archive/2026-10-15-kitchen-hot-food-batch/execution-notes.md)。

2026-10-15 甜品模型增量：搅拌机四向、预拌粉、牛奶原料、独立搅拌盆/面糊/金属模具/原味小蛋糕/甜品盘及两种有限包装；盆、模具和盘不画死内容，实际投影叠加。原脚本生成242切片、16行图集、ArtItemCount=51。烤制只改内部食物，原模具图片跟随同一物品；玩法和受管渲染实际结果见[kitchen-dessert-batch](../../openspec/changes/archive/2026-10-15-kitchen-dessert-batch/execution-notes.md)。

2026-10-15 咖啡模型增量：磨豆机/咖啡机/蒸汽机三设备四向、豆与粉、接液壶/奶缸/带袖套纸热饮杯、浓缩液/热奶/奶泡/拿铁/卡布及两包装。咖啡深色和可见奶泡顶区分三款完成状态，无拉花或封盖模型。原脚本同次生成266切片、17行、ArtItemCount=63，人物/效果起点随布局生成；玩法和受管渲染实际范围见[kitchen-beverage-batch](../../openspec/changes/archive/2026-10-15-kitchen-beverage-batch/execution-notes.md)。
