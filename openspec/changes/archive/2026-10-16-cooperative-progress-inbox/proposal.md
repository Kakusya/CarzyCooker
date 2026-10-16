# Proposal

## Why

合作领取页已有可领取／已领取／已关闭列表，但入口只写合作进度领取，平时不显示本端待领数量，记录标题主要是关号和凭证短码。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 在模式页、房间和结算入口显示已持久化本端可领数量，提供结果详情、状态分类和已领取记录去向；个人领取仍显式操作。
- 只统计 repository 实际可用凭证；网络收到但未持久化不能计为可领取。刷新不发 ACK／领取或关闭资格，状态未知／查询失败不显示零；不跨设备承诺唯一领取。
- 被动Browse要求已有完整本机身份资料，不借查询补建密钥、公钥或自身信任；资料缺失显示查询失败，保留原显式身份入口。已领记录读取当前Slot/修订，离线可显式转原单机选档只读检查，合作房间保留原记录页。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。已按后续18案实施授权接入并完成范围内验收，证据和相称投入见execution-notes；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `progress-claims`：增加“合作进度待办与领取详情”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[ProductEntryClaimsSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryClaimsSystem.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 原[Browse](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProgressClaimFiles.cs)增加已有身份的只读前置校验，由[身份owner](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/OfflineProfiles.cs)验证既有密钥/公钥/信任；明确初始化、签名、领取发布和权限合同保持。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [CooperativeClaimsUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/CooperativeClaimsUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：[product-entry-navigation](../2026-10-16-product-entry-navigation/proposal.md)、[settlement-next-step-guidance](../2026-10-16-settlement-next-step-guidance/proposal.md)、[solo-save-inspection](../2026-10-16-solo-save-inspection/proposal.md)。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
