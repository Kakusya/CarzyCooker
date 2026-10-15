# Proposal

## Why

当前厨房、食材和人物主要使用同一白色精灵着色，玩家依赖文字辨认设备、手持物和容器状态。按本轮用户授权承担项目美术，建立温暖圆润的餐厅资源并接入实际玩法，使现有制作与服务循环具有统一且可辨的画面。

## What Changes

- 将 Blender 与 Inkscape 官方便携软件下载到 `D:/MySoftware`，确认实际可运行；需要分层绘画时再补 Krita。
- 保存可编辑美术源与固定视角导出脚本，制作 2D 斜俯视环境、现有全部工位、角色四方向动作、食材／容器／成品、状态反馈和界面资源。
- 先检查厨师与汤链样板，再覆盖吐司、橙汁和前厅；样板是制作顺序，不缩减最终交付范围。
- 沿现有 Board／HUD owner 接入资源、方向、容器内容、脏污、加工及订单标记；保留竖屏镜头、占地、互动和业务裁决。
- 用真实 Launcher、最小相关 UTF 和竖屏截图验证功能与表现，并更新源文件制作说明、交付清单和 execution-notes。

范围外：不修改菜谱、地图通路、网络与存档合同，不新增玩法、不更换运行时为 3D；初始实施不执行 Player／真机构建、提交、push 或归档。2026-10-15 用户查看切片后追加授权提交、归档与 push；设备验收范围保持原约定。

## Capabilities

### New Capabilities

- `restaurant-art`：统一的餐厅精灵、角色动作、物品状态与界面视觉，含源文件、生命周期与可读性验收。

### Modified Capabilities

无；既有 grid-kitchen、restaurant-ui 与 resource-management 的业务合同保持。

## Impact

- `Unity/Assets/Res/Entity/Product/` 与 `Unity/Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab`。
- `Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/` 的现有 Mono 表现。
- 项目内美术源目录与制作说明；必要的相关 PlayMode UTF、资源集合和当前进度。
- 外部软件仅落在用户指定目录，游戏不新增软件运行依赖。

主要风险是小屏遮挡、资源回收和状态图误导。先做样板，再覆盖改变的视觉状态与已有 UI 生命周期小集；不扩大为全仓或设备套件。取舍与未验证边界见 [execution-notes.md](execution-notes.md)。用户已明确要求建 change 后下载并完成，规划后直接进入 apply。
