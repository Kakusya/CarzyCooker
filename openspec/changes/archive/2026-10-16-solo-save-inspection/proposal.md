# Proposal

## Why

选档列表有星级和时间，但继续记录点击后直接创建世界。现有完整载荷已能解释实物、供应和布局，玩家缺少加载前确认入口。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 提供记录详情与显式继续，展示章节／关序、结果类型、时间／修订／来源和已校验厨房摘要；按继续／完成分类及时间排序，刷新保留有效选择。
- 详情通过原只读 Select 路径完整校验后派生，不安装世界、不写记录；完成记录只查看。确认继续前重新读取并核对 Slot／revision 等本次选择事实；文件变化时更新预览并要求重新确认。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。本案已获实施授权；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `solo-save-progress`：增加“选档详情与只读检查”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[SoloSavePresentationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [SoloSaveSelectionUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SoloSaveSelectionUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：[product-entry-navigation](../2026-10-16-product-entry-navigation/proposal.md)。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
