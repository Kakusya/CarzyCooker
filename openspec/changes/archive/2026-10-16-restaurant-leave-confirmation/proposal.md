# Proposal

## Why

Solo 营业中缺少清楚的返回模式入口，合作有 Host 返回房间及房间返回模式，但不同关闭范围对未保存运行和待领资格影响不同。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 提供按角色区分的离开入口及后果说明：Solo 返回模式、Client 离开本次运行、Host 仅关闭餐厅回房间与结束整个房间分别确认；确认前可取消。
- 确认页不自动暂停或保存，需说明经营仍按原状态推进；不将技术离开伪造成功结算。Host 关闭餐厅保留 Match 和现有资格，正常结束整个 Match 沿原回执规则；提交中禁止重复且不把取消等待冒充撤销。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。实施沿原18案授权推进；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `product-entry`：增加“经营中离开与返回导航”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[GridKitchen/GridPresentationRoomSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRoomSystem.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [ProductEntryClientUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/ProductEntryClientUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：[product-entry-navigation](../2026-10-16-product-entry-navigation/proposal.md)、[cooperative-progress-inbox](../2026-10-16-cooperative-progress-inbox/proposal.md)。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
