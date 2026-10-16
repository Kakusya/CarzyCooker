# Proposal

## Why

当前只给空手且无工作玩家列出可认领动作，WorkText 主要显示伙伴与最近结果。玩家难以理解哪些桌待询问、谁在清理，以及收尾为什么尚未完成。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 增加前厅任务看板，按询问／清桌／洗碗展示当前工作、关联桌位／餐具、可用／前往／执行状态及真实执行者；收尾时显示仍在进行的顾客和必要工作。
- 按已有生命周期解释收尾，不要求排空后厨；阅读不认领、抢占或修改伙伴调度。对可认领任务提供原显式入口，已被占用任务只说明事实。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。已按后续18案实施授权接入并运行验收，结果见execution-notes；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `orders-front-service`：增加“前厅工作与收尾看板”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[GridKitchen/GridPresentationPanels.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [CoopFrontUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/CoopFrontUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：[kitchen-action-navigation](../2026-10-16-kitchen-action-navigation/proposal.md)。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
