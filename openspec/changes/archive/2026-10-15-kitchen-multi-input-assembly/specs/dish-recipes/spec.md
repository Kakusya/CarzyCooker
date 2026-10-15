# Spec Delta

## ADDED Requirements

### Requirement: 多材料组装依据精确实物

首批 F01田园沙拉/F02芝士蔬菜沙拉的多材料组装 MUST 按有效阶段核对实际输入定义和数量，完整通过后只生成一次结果；缺料、多料或错料不部分消费，显示名不参与裁决。

#### Scenario: 两份同类投入不足
- **WHEN** 某确认配方需要同类材料两份而仅有一份
- **THEN** 拒绝开始且保留全部输入，不按总重量或一个定义存在视作满足数量

### Requirement: 共享备料与合流

独立准备分支 SHALL 可以分工并交接真实半成品，在合流阶段完整匹配后组装；同一输入不能同时消费于两份菜。

#### Scenario: 两人争用同一份切配料
- **WHEN** 两名成员将同一材料身份用于不同组装
- **THEN** 仅首个合法提交消费，另一提交拒绝，不产生重复成品

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs)。
