# Spec Delta

## ADDED Requirements

### Requirement: 调饮按份数与必要顺序

首批 D15浓缩/D17热拿铁/D18卡布奇诺的调饮 MUST 消费各实际游戏份数，并完整验证必要前序；最后收尾未满足时不得识别成品，错误顺序拒绝且保留原杯内容。

#### Scenario: 过早加入奶泡
- **WHEN** 杯内前序咖啡/热奶尚未齐全而玩家加奶泡
- **THEN** 拒绝该次加入，原杯及手持物不变，不自动补齐前序

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDrinkSystem.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDrinkSystem.cs)。
