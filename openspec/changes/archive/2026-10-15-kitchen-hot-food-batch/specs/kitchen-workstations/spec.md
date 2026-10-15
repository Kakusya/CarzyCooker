# Spec Delta

## ADDED Requirements

### Requirement: 煎制设备显式启动

煎制 MUST 在合法设备使用真实生料显式启动，由统一业务 Tick 推进，设备同刻一个作业；未完成不能取得熟料或用显示动画提前完成。

#### Scenario: 开始后暂停
- **WHEN** 煎制启动后暂停再恢复
- **THEN** 暂停不推进，恢复接续同一冻结耗时和输入，最终只产生一次熟料

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs)。
