# Spec Delta

## ADDED Requirements

### Requirement: 开店准备总览可访问

系统 SHALL 提供以下入口行为：增加准备总览，集中显示本关菜单、必需加成选择／准备中状态、有效开店控制、现有厨房与供应摘要，并链接原菜谱／补货／布局入口。

#### Scenario: 开店准备总览的正常使用
- **WHEN** 必选加成未选时打开准备总览，再完成原选择
- **THEN** 明确显示开店所缺真实条件，选择提交后状态和开店入口更新

### Requirement: 开店准备总览保持事实与操作边界

系统 MUST 遵守以下边界：区分真正阻止开店的原门槛与可选备料建议；物资少、没有预制菜不能新增为强制条件。Client 显示等待 Host，开店仍只用原控制请求。

#### Scenario: 开店准备总览的取消或状态变化
- **WHEN** 厨房没有预制成品，或远端 Client 查看总览
- **THEN** 不会把建议变成开店新门槛，Client 不获得 Host 控制；旧关清单不会留到新关

#### Scenario: 开店准备总览关闭重开与旧回调
- **WHEN** 玩家关闭并重开本入口，随后旧页面回调或迟到结果到达
- **THEN** 页面只作用于当前有效上下文，原操作不因重开重复提交，旧结果不覆盖新选择，原合法入口仍可使用

## Sources

- [当前主规格](../../../../../specs/restaurant-ui/spec.md)。
- [实际实现前提](../../../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)。
- [本案设计](../../design.md)、[执行与验证边界](../../execution-notes.md)。

本增量描述本案入口合同；已按对应任务实施并运行，逐项结果见 execution-notes。现有底层能力只复用行为未变部分，不由规格存在推断入口验收。
