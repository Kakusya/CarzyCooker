# Proposal

## Why

RefreshGridHud 为菜谱、采购、前厅和每个工位持续追加动作，虽有主要动作区，全部动作仍混排。内容增加后寻找目标与保持按钮稳定的成本升高。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 在现有 HUD 提供明确的功能分组与当前目标动作，其他合法动作通过全部动作入口抵达；保持当前选中目标、滚动和必要反馈。
- 分组不改变合法动作集合或原单击执行语义，查看分类不移动角色。已接受／等待到位／完成反馈保持，状态变化只刷新受影响区域。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。本案已获实施授权；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-ui`：增加“厨房动作分组与目标导航”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[GridKitchen/GridPresentationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [SoloRestaurantUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SoloRestaurantUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：可独立开始。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
