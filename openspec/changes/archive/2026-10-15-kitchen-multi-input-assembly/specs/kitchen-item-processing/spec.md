# Spec Delta

## ADDED Requirements

### Requirement: 组装过程可停手接续

多输入手工组装 MUST 复用单执行者和业务 Tick，停手保留合法阶段及进度；载具内容随同一外层物移动，普通台面仍一外层物。

#### Scenario: 组装一半交接
- **WHEN** 甲停止并交出半组装载具，乙合法接续
- **THEN** 原实物和进度保持，只乙推进；未完成阶段不能交付成品

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs)。
