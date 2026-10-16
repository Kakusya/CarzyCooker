# Spec Delta

## ADDED Requirements

### Requirement: 工位能力与操作位置说明可访问

系统 SHALL 提供以下入口行为：在选中工位详情展示可信能力、当前许可中的匹配工序、必要容器、当前实际占地与操作格；提供只读高亮帮助理解位置和旋转。

#### Scenario: 工位能力与操作位置说明的正常使用
- **WHEN** 选择蒸汽机或旋转后的加工工位查看详情
- **THEN** 显示对应合法热奶／打泡等能力、实际容器与当前操作格，而非按名称推断

### Requirement: 工位能力与操作位置说明保持事实与操作边界

系统 MUST 遵守以下边界：显示当前已提交几何和有效定义，不能保证动态路线一定可达；不把预览当布局提交，不新增设备、加工规则或移动确认流程。Client 可查看而无布局控制权。

#### Scenario: 工位能力与操作位置说明的取消或状态变化
- **WHEN** 工位移位／扩建／菜单缩小或远端只读查看
- **THEN** 说明和高亮随当前布局更新，不用旧操作格发动作，查看不改变位置或权限

#### Scenario: 工位能力与操作位置说明关闭重开与旧回调
- **WHEN** 玩家关闭并重开本入口，随后旧页面回调或迟到结果到达
- **THEN** 页面只作用于当前有效上下文，原操作不因重开重复提交，旧结果不覆盖新选择，原合法入口仍可使用

## Sources

- [当前主规格](../../../../../specs/kitchen-workstations/spec.md)。
- [实际实现前提](../../../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationInformation.cs)。
- [本案设计](../../design.md)、[执行与验证边界](../../execution-notes.md)。

本增量描述本案入口合同；已按对应任务实施并运行，逐项结果见 execution-notes。现有底层能力只复用行为未变部分，不由规格存在推断入口验收。
