# Proposal

## Why

工位详情有位置、内容、加工与等级，但对能做哪些工序、需要什么容器、从哪侧操作缺少统一说明。图形相近设备也容易混淆。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 在选中工位详情展示可信能力、当前许可中的匹配工序、必要容器、当前实际占地与操作格；提供只读高亮帮助理解位置和旋转。
- 显示当前已提交几何和有效定义，不能保证动态路线一定可达；不把预览当布局提交，不新增设备、加工规则或移动确认流程。Client 可查看而无布局控制权。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。已按后续18案实施授权接入并验收，证据见execution-notes；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `kitchen-workstations`：增加“工位能力与操作位置说明”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[GridKitchen/GridPresentationInformation.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationInformation.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [SoloRestaurantUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SoloRestaurantUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：[kitchen-action-navigation](../2026-10-16-kitchen-action-navigation/proposal.md)。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
