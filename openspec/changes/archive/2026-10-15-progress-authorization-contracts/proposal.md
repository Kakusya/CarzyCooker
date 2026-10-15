# Proposal

## Why

目标文档已确认 Host≠Save Owner、授权分支和主动领取，但签发/验证/撤销、Profile 证明、关闭回执认证和文件故障边界明确未设计；当前只有本机 Solo repository。

把后续合作存档真正依赖的少数未决项写成可实施协议，先澄清会导致越权写档或结果丢失的选择。

## What Changes

- 形成同平台 Profile/SaveSlot 授权、凭证签发验证撤销及 normal-close receipt 的最小协议。
- 确认本地文件提交/失败重试的保证边界与跨设备 UX，不承诺无中心服务的全局一次领取。
- 逐项记录用户决定后同步产品目标及下游提案，不编写运行代码、不迁入安全或服务基础设施。

优先级：**P1**；建议顺序：**04**。前置：无新增 change 硬前置，复用已交付业务。优先级表示用户价值和依赖，不要求所有案串行。

范围外：不实现密码学、账号中心、云存档、联机保存或任意断电证明；不把文档评审当功能通过。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

无。

本案纯产品协议文档定稿，`.openspec.yaml` 显式 `skip_specs: true`，不创建运行行为 delta。

## Impact

- [Docs/Product/specs/save-progress-claims/spec.md](../../../../Docs/Product/specs/save-progress-claims/spec.md)
- [Docs/Product/design.md](../../../../Docs/Product/design.md)
- [Docs/Product/backlog.md](../../../../Docs/Product/backlog.md)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveFile.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveFile.cs)

实施门槛：2026-10-15 用户已确认离线文件授权首版，按[协议正文](../../../../Docs/Product/offline-progress-protocol.md)交接下游实现；远程撤销、中心服务、云同步和任意断电不在首版保证内。

风险是把未定义认证当成可用授权或扩大断电保证。使用少量成功/拒绝/重试/关闭序列逐项审阅和链接检查即可；本案纯文档，不添加 UTF。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

用户随后授权完成全部 16 个 change，并单独确认离线授权边界。本案只交付协议文档；下游按各自范围运行 Editor/UTF，不包含真机、物理 LAN、Player 或 AOT 实测。没有提交、push 或归档授权。总体证据与优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
