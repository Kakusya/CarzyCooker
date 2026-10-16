# Spec Delta

## ADDED Requirements

### Requirement: 章节与新开餐厅预览可访问

系统 SHALL 提供以下入口行为：选择已交付章节或已有餐厅菜单后，先显示章节名称、实际关数、各关菜单、模式及保存语义，再明确开始；取消返回原列表。

#### Scenario: 章节与新开餐厅预览的正常使用
- **WHEN** 在单机或 Host 房间选择第二章，阅读菜单后确认开始
- **THEN** 预览和实际首关一致，确认只开一个餐厅，取消则不创建世界

### Requirement: 章节与新开餐厅预览保持事实与操作边界

系统 MUST 遵守以下边界：预览读取当前有效章／Level／Dish 定义，不创造新章节、难度或承诺平衡。Solo 保存选择与合作 Host 开局权限沿原规则；Client 可查看本端目录，但不暗示已同步为全房间共同选项。

#### Scenario: 章节与新开餐厅预览的取消或状态变化
- **WHEN** 在 Client 查看目录、取消预览，或持旧房间按钮确认
- **THEN** 查看不授予开局权，不修改房间；旧身份确认失效且不静默改开其他菜单

#### Scenario: 章节与新开餐厅预览关闭重开与旧回调
- **WHEN** 玩家关闭并重开本入口，随后旧页面回调或迟到结果到达
- **THEN** 页面只作用于当前有效上下文，原操作不因重开重复提交，旧结果不覆盖新选择，原合法入口仍可使用

## Sources

- [当前主规格](../../../../../specs/product-entry/spec.md)。
- [实际实现前提](../../../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs)。
- [本案设计](../../design.md)、[执行与验证边界](../../execution-notes.md)。

本增量描述本案入口合同；已按对应任务实施并运行，逐项结果见 execution-notes。现有底层能力只复用行为未变部分，不由规格存在推断入口验收。
