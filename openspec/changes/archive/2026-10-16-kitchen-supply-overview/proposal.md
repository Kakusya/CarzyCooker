# Proposal

## Why

采购按钮展示余包数，运输信息拼入 HintText，收货与拆包又在动作列表中。玩家很难同时区分配额、在途货、待收货与已经进厨房的材料。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 增加按来源查看的供应页，分区展示剩余外部配额、运输／待接收、实际包装和可用拆份入口，解释满收货台及当前阶段限制。
- 不把配额、未到货、包装内材料合并为可用库存；不自动建议采购数量或批量补货。选择采购／接收／拆份仍使用原逐次显式命令和实时资格。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。已按后续18案实施授权接入并实际验收，见execution-notes；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `kitchen-supply`：增加“补货与收货业务总览”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[GridKitchen/GridPresentationSupplySystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSupplySystem.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [CoopLogisticsUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/CoopLogisticsUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：[kitchen-action-navigation](../2026-10-16-kitchen-action-navigation/proposal.md)、[kitchen-item-locator](../2026-10-16-kitchen-item-locator/proposal.md)。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
