# Spec Delta

## ADDED Requirements

### Requirement: 结算到下一步的导航可访问

系统 SHALL 提供以下入口行为：按当前模式和真实阶段组织本关结果、待处理装修／保存、下一关菜单变化或大关终点，并给出唯一清楚的主要下一步和原合法次要入口。

#### Scenario: 结算到下一步的导航的正常使用
- **WHEN** 非终关保存失败后重试，或零星完成终关
- **THEN** 页面说明尚未保存／原结果保留，合法成功后只前进一次；终关明确返回选档或房间

### Requirement: 结算到下一步的导航保持事实与操作边界

系统 MUST 遵守以下边界：不新增自动保存、奖励或第四关；零星仍可继续。内存、保存中、失败、已提交和已领取分别说明，Host／Client 控制与个人领取资格保持。

#### Scenario: 结算到下一步的导航的取消或状态变化
- **WHEN** Client 结算只读、旧继续回调或装修尚未准备
- **THEN** 不误显示已保存，不开放 Host 控制，不生成下一关或绕过原门槛

#### Scenario: 结算到下一步的导航关闭重开与旧回调
- **WHEN** 玩家关闭并重开本入口，随后旧页面回调或迟到结果到达
- **THEN** 页面只作用于当前有效上下文，原操作不因重开重复提交，旧结果不覆盖新选择，原合法入口仍可使用

## Sources

- [当前主规格](../../../../../specs/restaurant-ui/spec.md)。
- [实际实现前提](../../../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)。
- [本案设计](../../design.md)、[执行与验证边界](../../execution-notes.md)。

本增量描述本案入口合同；已按对应任务实施并运行，逐项结果见 execution-notes。现有底层能力只复用行为未变部分，不由规格存在推断入口验收。
