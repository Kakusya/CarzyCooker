# Spec Delta

## ADDED Requirements

### Requirement: 烘焙载具保持唯一身份

模具 MUST 为真实可复用载具，内容使用受限定的无环关系；装模、搬运、烘焙和脱模不复制或销毁模具，失败不部分转移。

#### Scenario: 脱模目标已满
- **WHEN** 玩家向非空或不兼容出餐容器脱模
- **THEN** 拒绝并保留模具、成品内容和目标，之后合法目标仍可脱模

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs)。
