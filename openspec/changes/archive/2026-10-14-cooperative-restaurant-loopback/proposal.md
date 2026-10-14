# Proposal

## Why

三个前置案将分别交付核心制作、前厅和物流，但同 Editor 的局部 KCP 结果仍不足以证明普通双 Editor 完整餐厅可玩。本案保留原名称，收口同一版本在两种工作区角色排列下的完整合作体验。

## What Changes

- 原开发范围迁入 [核心](../2026-10-14-cooperative-restaurant-core/proposal.md)、[前厅](../2026-10-14-cooperative-front-service/proposal.md)与 [清理供应](../2026-10-14-cooperative-cleanup-supply/proposal.md)，本案不重复开发或重复勾选这些任务。
- 三个前置实际交付后，核对当前入口、协议、完整 owner／消息与逐功能证据，再执行普通 Host／Client 双 Editor 合作。
- 两种工作区角色排列均完成两菜分工、真实交付、补货／清理、暂停与自然换关，记录断线接续和再次启动边界。
- 集成中发现缺口时在对应前置归属处修复，复用未变结果，仅补受影响项。
- 仍只交付本机内存餐厅，不包含装修、成功档、原成员恢复、LAN、公网、Player 或正式传输选型。

## Capabilities

### New Capabilities

- `cooperative-restaurant`：普通双进程入口下完整合作餐厅的组合行为与明确交付范围。

### Modified Capabilities

无；原 product-session 增量迁入 cooperative-restaurant-core，避免重复覆盖主规格。

## Impact

强依赖三案的实际实现与运行证据；规划 done 不满足前置。复用当前 loopback KCP、Unity CLI 与现有第二工作区协作入口，不新增测试／端口／双包框架。

本轮仅重排规划，未标记任何联机功能完成。原任务迁移表、验收取舍与当前 0/3 状态见 [execution-notes.md](execution-notes.md)。第二工作区按 multiplayer-baseline 查找，只接收已授权 main 提交与执行指令；不得要求第二工作区开发或处理源码冲突。
