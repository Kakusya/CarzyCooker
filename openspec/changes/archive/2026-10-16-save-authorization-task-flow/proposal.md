# Proposal

## Why

现有页面混合身份导出、信任、只读运行、导入、替换等动作及路径输入。合同完整，玩家却需要先理解工程术语才能选正确用途。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 按我想继续他人餐厅、导入独立进度、导出给朋友和替换我的记录组织流程，逐步说明来源／目标／用途／结果，并提供返回和可取消步骤。
- ReadRun、ImportBranch、Replace 三种权限不能互换；信任公钥、导入和有损替换保留既有独立明确确认。查看、返回与切换任务不写档、不自动信任；提交阶段不伪称可取消已落盘结果。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。实施按原18案授权推进；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `save-authorization`：增加“存档授权的任务式入口”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[ProductEntryProgressSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryProgressSystem.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [SaveAuthorizationUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SaveAuthorizationUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：[product-entry-navigation](../2026-10-16-product-entry-navigation/proposal.md)、[solo-save-inspection](../2026-10-16-solo-save-inspection/proposal.md)、[cooperative-progress-inbox](../2026-10-16-cooperative-progress-inbox/proposal.md)。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
