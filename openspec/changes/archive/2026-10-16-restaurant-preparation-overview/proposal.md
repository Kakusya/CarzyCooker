# Proposal

## Why

开店按钮只在 Buff 等门槛满足时出现，准备阶段信息散在菜谱、加成、物资和工位动作中，玩家难以判断为什么还不能开店。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 增加准备总览，集中显示本关菜单、必需加成选择／准备中状态、有效开店控制、现有厨房与供应摘要，并链接原菜谱／补货／布局入口。
- 区分真正阻止开店的原门槛与可选备料建议；物资少、没有预制菜不能新增为强制条件。Client 显示等待 Host，开店仍只用原控制请求。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。本案已获实施授权；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-ui`：增加“开店准备总览”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[GridKitchen/GridPresentationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [SoloRestaurantUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SoloRestaurantUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：[kitchen-action-navigation](../2026-10-16-kitchen-action-navigation/proposal.md)。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
