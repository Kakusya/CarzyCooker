# Proposal

## Why

创建房间只显示 ListenEndpoint，加入页默认一个地址输入框，连接错误常来自底层文本。已有连接流程缺少能让玩家正确填写和转交地址的说明。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 按创建／加入阶段显示合法地址示例、当前实际监听地址、复制入口和可理解的连接／取消／重试说明；把回环地址的适用范围说明白。
- 只复制公开 endpoint；不展示凭证／密钥，不自动连网测试或扫描网卡，不改变现有显式 LAN 授权与地址校验。错误仅按实际校验／握手结果说明，不能猜测防火墙或声称公网可用。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。本案已获实施授权；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `product-entry`：增加“合作地址与连接帮助”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[ProductEntryPresentationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryPresentationSystem.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [ProductEntryClientUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/ProductEntryClientUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：[product-entry-navigation](../2026-10-16-product-entry-navigation/proposal.md)。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
