# Spec Delta

## ADDED Requirements

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

- [当前主规格](../../../../../specs/save-authorization/spec.md)。
- [实际实现前提](../../../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryProgressSystem.cs)。
- [本案设计](../../design.md)、[执行与验证边界](../../execution-notes.md)。

本增量描述本案入口合同；实际实施与逐项运行结果见 execution-notes，不由规格存在推断验收完成。
