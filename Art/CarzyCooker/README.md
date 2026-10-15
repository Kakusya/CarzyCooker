# CarzyCooker 美术制作源

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

环境按原格子显示；工位按原 Footprint、方向与定义映射，所有物品 ID 1–21 均有独立图形。容器内容、脏污、订单票、蒸汽等由实际投影组合，不把内容画死在容器或工位上。

本轮制作／游戏验收结果见 `openspec/changes/archive/2026-10-15-restaurant-art-production/execution-notes.md`，截图为实机 Editor 输出。手机和 Player 验收属于后续设备 change。
