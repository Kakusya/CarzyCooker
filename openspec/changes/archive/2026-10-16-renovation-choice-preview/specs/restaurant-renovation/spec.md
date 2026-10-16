# Spec Delta

## ADDED Requirements

### Requirement: 装修奖励与升级对比预览可访问

系统 SHALL 提供以下入口行为：候选先进入只读详情，解释奖励类型、实际目标、升级前后匹配作业基础耗时及生效边界；明确确认才提交原选择，Client 可看同一候选说明。

#### Scenario: 装修奖励与升级对比预览的正常使用
- **WHEN** 查看一项升级和另一项扩建奖励，取消后再确认升级
- **THEN** 比较说明准确，取消不固定选择，明确确认才提交一次，旧作业耗时不变

### Requirement: 装修奖励与升级对比预览保持事实与操作边界

系统 MUST 遵守以下边界：比较只针对配置与当前合法目标，不承诺运行中旧作业缩短、不补物资，也不将预览可安装当作最终保证；原候选固定、单次选择和终关无奖励合同保持。

#### Scenario: 装修奖励与升级对比预览的取消或状态变化
- **WHEN** 预览后目标等级／候选版本变化，或 Client 点击确认
- **THEN** 旧预览不选择新目标，远端不获控制权，仍可查看合法现状

#### Scenario: 装修奖励与升级对比预览关闭重开与旧回调
- **WHEN** 玩家关闭并重开本入口，随后旧页面回调或迟到结果到达
- **THEN** 页面只作用于当前有效上下文，原操作不因重开重复提交，旧结果不覆盖新选择，原合法入口仍可使用

## Sources

- [当前主规格](../../../../../specs/restaurant-renovation/spec.md)。
- [实际实现前提](../../../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRenovationSystem.cs)。
- [本案设计](../../design.md)、[执行与验证边界](../../execution-notes.md)。

本增量描述本案入口合同；已按对应任务实施并运行，逐项结果见 execution-notes。现有底层能力只复用行为未变部分，不由规格存在推断入口验收。
