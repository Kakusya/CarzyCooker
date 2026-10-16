# Proposal

## Why

装修候选目前主要是名称和目标等级，点击即固定选择。玩家在选择前难以分清新增工位、解锁、扩建与原工位升级的实际效果。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 候选先进入只读详情，解释奖励类型、实际目标、升级前后匹配作业基础耗时及生效边界；明确确认才提交原选择，Client 可看同一候选说明。
- 比较只针对配置与当前合法目标，不承诺运行中旧作业缩短、不补物资，也不将预览可安装当作最终保证；原候选固定、单次选择和终关无奖励合同保持。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。已按后续18案实施授权接入并实际验收，证据见execution-notes；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-renovation`：增加“装修奖励与升级对比预览”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[GridKitchen/GridPresentationRenovationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRenovationSystem.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [CooperativeUpgradeUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/CooperativeUpgradeUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：[kitchen-action-navigation](../2026-10-16-kitchen-action-navigation/proposal.md)、[workstation-capability-inspector](../2026-10-16-workstation-capability-inspector/proposal.md)。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
