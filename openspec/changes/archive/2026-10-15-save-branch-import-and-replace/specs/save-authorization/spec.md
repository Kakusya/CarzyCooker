# Spec Delta

## Purpose

补齐已确认的合法分支与有损替换语义，让进度流转具有明确归属和可理解的确认。同平台授权分支导入、谱系与显式有损覆盖。本能力区分已提交世界事实、持久记录和玩家操作，保证拒绝或中断有明确边界，并为后续有限内容扩展提供可观察的合同。

## ADDED Requirements

### Requirement: 授权导入建立独立分支

合法同平台授权导入 MUST 新建归接收 Profile 的独立 SaveSlot，记录直接和根来源及授权身份；不修改源档，不自动合并。

#### Scenario: 原 Owner 离线后继续分支
- **WHEN** 接收者已完成合法授权导入而原 Owner 离线
- **THEN** 接收者可独立运行分支，源档和其他分支不改变

### Requirement: 显式覆盖验证目标及修订

有损覆盖 MUST 经玩家明确确认，并完整核对目标身份、权限范围及预期 revision，保留目标 Owner；谱系关系本身不授予写权。

#### Scenario: 修订冲突或无写权
- **WHEN** 目标已变化或仅知道来源 ID
- **THEN** 拒绝覆盖，旧文件及领取资格保持，不静默改写较新档

#### Scenario: 覆盖确认后目标变化
- **WHEN** 玩家确认的目标revision在实际提交前变化
- **THEN** 拒绝本次替换并重新展示目标；未再次确认不能覆盖新revision

### Requirement: 替换提交与消费一致

覆盖 MUST 将完整目标和领取回执同次原子发布，成功后才向调用者报告消费；发布前失败保护有效旧档，发布后答复丢失仍返回同一结果。后续检查点或替换保留本端已领取事实，成功后不保留用户可恢复旧版本。

#### Scenario: 替换失败再重试
- **WHEN** 真实文件替换在提交前失败，随后重试成功
- **THEN** 失败保留旧记录和资格，成功仅替换一次且保留目标归属

#### Scenario: 成功发布后答复丢失
- **WHEN** 目标及领取回执已发布，答复丢失后玩家重开并请求新建或再次覆盖
- **THEN** 返回原领取Slot/revision，不产生第二次领取；已有领取事实不因后续覆盖丢失

### Requirement: 平台及授权边界拒绝

导入或覆盖 MUST 验证平台、签名、公钥绑定及有效用途范围，未知、过期或本端已拒绝的未使用权限明确拒绝，不因载荷可解码而接纳。首版不承诺离线副本或已导入分支的远程撤销。

#### Scenario: 跨平台授权包
- **WHEN** Android 标记授权包试图导入 PC repository
- **THEN** 明确拒绝且无新档、覆盖或资格消费

#### Scenario: ReadRun 不能替代导入用途
- **WHEN** 玩家使用只授予ReadRun的封套请求ImportBranch
- **THEN** 明确拒绝，不创建分支、不修改源档

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveFile.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveFile.cs)。
