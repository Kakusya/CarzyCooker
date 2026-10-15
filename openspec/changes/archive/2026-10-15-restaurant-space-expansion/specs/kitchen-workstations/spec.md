# Spec Delta

## ADDED Requirements

### Requirement: 地图几何由同一配置持有

厨房 MUST 以有界配置表达地图尺寸和可用区域，玩家、伙伴、顾客、工位验证、镜头及点击使用同一已提交几何；旧有效档保留原地图语义。

#### Scenario: 扩建后寻路与点击
- **WHEN** 扩建合法提交且玩家点击新增可用格
- **THEN** 各端选择同一有效格，三类角色不会穿过未解锁区域，原布局关系不改变

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)。
