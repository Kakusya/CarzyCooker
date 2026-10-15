# Proposal

## Why

Solo repository 只允许 File.Move 不覆盖新建，header 仅有直接来源字段；没有授权包导入、根谱系/授权范围或预期修订覆盖实现。

补齐已确认的合法分支与有损替换语义，让进度流转具有明确归属和可理解的确认。

## What Changes

- 同平台有效 bearer 授权导入立即创建接收 Profile 独立新分支，记录直接/根来源及授权身份。
- 领取时可显式选择权限范围内目标有损覆盖，校验身份、权限及预期 revision，保留目标 Owner。
- 保护提交前有效旧档，目标与领取回执同次发布，成功后才报告消费；冲突/磁盘失败可重试，成功后不保留用户可恢复上一版。

优先级：**P1**；建议顺序：**07**。前置：[progress-authorization-contracts](../2026-10-15-progress-authorization-contracts/proposal.md)、[cooperative-save-baseline](../2026-10-15-cooperative-save-baseline/proposal.md)、[cooperative-result-claims](../2026-10-15-cooperative-result-claims/proposal.md)。优先级表示用户价值和依赖，不要求所有案串行。

范围外：不合并分支、不跨 PC/Android、不静默覆盖、不自动撤销别设备副本，不建立云服务或版本备份库。

## Capabilities

### New Capabilities

- `save-authorization`：同平台授权分支导入、谱系与显式有损覆盖。

### Modified Capabilities

无。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveFile.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveFile.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs)
- [Docs/Product/specs/save-progress-claims/spec.md](../../../../Docs/Product/specs/save-progress-claims/spec.md)

实施交接：离线协议已确认。ImportBranch 与 ReplaceGrant 是独立用途；依赖基线案的身份/验签和领取案的原子回执合同。覆盖测试只使用本案隔离目录，玩家操作每次明确确认目标。

这是会覆盖文件的改动，验证比内容案严格。用独立临时目录真实文件核对成功、权限/修订冲突、写入失败、重复与跨平台拒绝，避免动用户现有档。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

用户已授权完成全部 16 个 change，并确认离线授权首版。本案运行任务已逐项实施及验收，实际7新增及2受变回归通过，详见execution-notes；只运行相关 Editor/UTF，不包含真机、物理 LAN、Player 或 AOT 实测；未获提交、push 或归档授权。总体证据与优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
