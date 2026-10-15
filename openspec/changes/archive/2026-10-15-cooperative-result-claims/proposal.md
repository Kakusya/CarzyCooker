# Proposal

## Why

现有 SoloSaveResult 绑定单机 Profile，通过 ResultId 去重新建文件；合作没有开局冻结的 Profile 资格、各端领取凭证、持久化 ACK 或 normal-close receipt。

合作每关成功后形成共享结果，合资格玩家能主动保存自己的独立进度，其他成员离线或不领取不阻止经营。

## What Changes

- 开局冻结合资格 Profile 名单，成功收口后形成不可变 SaveResult 和稳定逐人凭证。
- 客户端先持久化凭证再 ACK，Host 在本 Match 内重发同一未确认凭证。
- 首版主动领取只新建本地独立档，按 Result/Profile 本机最多一次；处理每端正常关闭和异常终止。

优先级：**P1**；建议顺序：**06**。前置：[cooperative-chapter-completion](../2026-10-15-cooperative-chapter-completion/proposal.md)、[progress-authorization-contracts](../2026-10-15-progress-authorization-contracts/proposal.md)、[cooperative-save-baseline](../2026-10-15-cooperative-save-baseline/proposal.md)。优先级表示用户价值和依赖，不要求所有案串行。

范围外：显式有损覆盖由 save-branch-import-and-replace 接入；本案不自动写全部玩家文件、不承诺跨设备全局一次、主机迁移或云存档。

## Capabilities

### New Capabilities

- `progress-claims`：合作不可变成功结果、逐人资格投递及主动新建领取。

### Modified Capabilities

无。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveCommitSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveCommitSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveRepositoryMessages.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveRepositoryMessages.cs)
- [Docs/Product/specs/save-progress-claims/spec.md](../../../../Docs/Product/specs/save-progress-claims/spec.md)

实施交接：离线首版协议已经确认；资格证明和基线接口复用 cooperative-save-baseline 的实际交付，不能把该案文件存在当作验收通过。合作终关与房间已交付，继续沿其原控制合同接入。

主要是误授资格、重复领取或失败吞资格，采用真实本地文件和 KCP 小集覆盖资格/ACK/提交/关闭四条路径，保留失败输出；不建立跨设备资产平台。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

用户已授权完成全部 16 个 change，并确认离线授权首版。本案6项任务已按执行说明逐项验收，只运行相关 Editor/UTF，不包含真机、物理 LAN、Player 或 AOT 实测；未获提交、push 或归档授权。总体证据与优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
