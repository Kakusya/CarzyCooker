# Spec Delta

## ADDED Requirements

### Requirement: 骨架生成的失败与交付范围

ET UI/UGFEntity 代码骨架生成 MUST 在目标代码已存在或模板缺失时拒绝该项写入，不覆盖已有代码。多个文件按顺序创建，没有整组预检和自动回滚保证。UI 的 Mono 挂载等待脚本重载；生成完成不代表 Excel 注册、资源收集、绑定和实际打开验收已完成。

#### Scenario: 后续目标文件已存在

- **WHEN** 同一组代码前面的文件已生成，后面的目标已存在而抛出异常
- **THEN** 前面的新文件可能保留；先核对实际部分输出，不能把失败当作没有任何改动

#### Scenario: UI Prefab 与脚本重载

- **WHEN** UI 骨架创建新 Prefab 并记录待挂 Mono 的名称
- **THEN** 重载后的回调再尝试挂载；类型尚不可用或资源找不到时不构成 UI 已可运行

#### Scenario: UGFEntity 骨架生成

- **WHEN** 三份 UGFEntity/Mono/System 文件成功生成
- **THEN** Prefab、配置表、CodeBind 与资源导出仍按实际流程接入，工具不代办这些步骤

## Sources

- [UI 代码与重载挂载](../../../../../../Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/UIFormCodeCreator.cs)
- [Entity 骨架](../../../../../../Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/UGFEntityCodeCreator.cs)
- [UI 接入](../../../../../../Book/UI开发.md)
- [Entity 接入](../../../../../../Book/Entity开发.md)
