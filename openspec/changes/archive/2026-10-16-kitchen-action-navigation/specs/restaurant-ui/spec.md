# Spec Delta

## ADDED Requirements

### Requirement: 厨房动作分组与目标导航可访问

系统 SHALL 提供以下入口行为：在现有 HUD 提供明确的功能分组与当前目标动作，其他合法动作通过全部动作入口抵达；保持当前选中目标、滚动和必要反馈。

#### Scenario: 厨房动作分组与目标导航的正常使用
- **WHEN** 选中灶台并切换补货／前厅分类后返回当前目标
- **THEN** 可访问同一组原合法动作，查看不发命令，当前真实动作一次点击只发一次

### Requirement: 厨房动作分组与目标导航保持事实与操作边界

系统 MUST 遵守以下边界：分组不改变合法动作集合或原单击执行语义，查看分类不移动角色。已接受／等待到位／完成反馈保持，状态变化只刷新受影响区域。

#### Scenario: 厨房动作分组与目标导航的取消或状态变化
- **WHEN** 物品变化、拖动、暂停或 HUD 池化重开
- **THEN** 不因分组丢失合法入口，不把拖动变点击，旧按钮不能作用新目标

#### Scenario: 厨房动作分组与目标导航关闭重开与旧回调
- **WHEN** 玩家关闭并重开本入口，随后旧页面回调或迟到结果到达
- **THEN** 页面只作用于当前有效上下文，原操作不因重开重复提交，旧结果不覆盖新选择，原合法入口仍可使用

## Sources

- [当前主规格](../../../../../specs/restaurant-ui/spec.md)。
- [实际实现前提](../../../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)。
- [本案设计](../../design.md)、[执行与验证边界](../../execution-notes.md)。

本增量描述本案入口合同；已按对应任务实施并运行，逐项结果见 execution-notes。现有底层能力只复用行为未变部分，不由规格存在推断入口验收。
