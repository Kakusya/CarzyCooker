# Proposal

## Why

订单有前往餐桌与交付按钮，菜谱则独立列出；自动首单选择也可能取另一订单。玩家缺少围绕自己选定订单查看制作依赖的入口。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 从当前订单直接查看对应菜谱、精确制作缺口和关联顾客／餐桌；同菜多单可明确选择目标，并链接可用物品／工位的查看入口。
- 订单选择只是本轮阅读上下文，不认领订单，不抢占菜品，不自动贴单／移动／交付。饮品已有合法绑定继续优先，不因选了另一订单就暗中换绑。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。本案已获实施授权；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `kitchen-recipe-guide`：增加“订单到制作的导航”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[GridKitchen/GridPresentationRecipeSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRecipeSystem.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [KitchenRecipeGuideUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/KitchenRecipeGuideUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：[kitchen-action-navigation](../2026-10-16-kitchen-action-navigation/proposal.md)。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
