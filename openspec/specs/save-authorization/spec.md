# 同平台授权分支与显式替换

## Purpose

同平台玩家可主动转交独立分支，并在领取合作结果时明确选择有损替换。区分运行读取、导入、谱系及文件写权，保护目标归属和已提交消费事实；失败可重试，重复操作不产生第二份进度，也不承诺恢复已被替换的旧内容。

## Requirements

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


### Requirement: 存档授权的任务式入口可访问

系统 SHALL 提供以下入口行为：按我想继续他人餐厅、导入独立进度、导出给朋友和替换我的记录组织流程，逐步说明来源／目标／用途／结果，并提供返回和可取消步骤。

#### Scenario: 存档授权的任务式入口的正常使用
- **WHEN** 从导入独立进度任务检查并信任有效文件，取消后重新确认导入
- **THEN** 各步骤用途清楚，取消不建档，原明确确认后只创建一次合法独立分支

### Requirement: 存档授权的任务式入口保持事实与操作边界

系统 MUST 遵守以下边界：ReadRun、ImportBranch、Replace 三种权限不能互换；信任公钥、导入和有损替换保留既有独立明确确认。查看、返回与切换任务不写档、不自动信任；提交阶段不伪称可取消已落盘结果。

#### Scenario: 存档授权的任务式入口的取消或状态变化
- **WHEN** 把只读许可用于导入、替换目标修订变化或写入失败
- **THEN** 沿原规则拒绝，保留文件和资格；新任务页面不绕过用途／修订校验，也不误报成功

#### Scenario: 存档授权的任务式入口关闭重开与旧回调
- **WHEN** 玩家关闭并重开本入口，随后旧页面回调或迟到结果到达
- **THEN** 页面只作用于当前有效上下文，原操作不因重开重复提交，旧结果不覆盖新选择，原合法入口仍可使用

## Sources

- [协议](../../../Docs/Product/offline-progress-protocol.md)、[逐项验收](../../changes/archive/2026-10-15-save-branch-import-and-replace/execution-notes.md)。
- [文件授权](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SaveAuthorizationFiles.cs)、[有序入口](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SaveAuthorizationMessages.cs)、[目标发布](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveFile.cs)。
- [确认界面](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryAuthorizationSystem.cs)、[用法](../../../Book/快速开始.md)。
- [save-authorization-task-flow 逐项验收](../../changes/archive/2026-10-16-save-authorization-task-flow/execution-notes.md)?
