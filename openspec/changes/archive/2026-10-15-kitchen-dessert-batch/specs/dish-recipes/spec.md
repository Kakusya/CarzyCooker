# Spec Delta

## ADDED Requirements

### Requirement: 甜品阶段不可跳过

首批 S04原味小蛋糕 MUST 使用真实材料制作面糊，再经装模、合法烘烤及脱模入出餐容器，不能直接供应面糊成品或用模具冒充餐盘。

#### Scenario: 未脱模请求交付
- **WHEN** 蛋糕已烤好但仍在模具中
- **THEN** 不能交付为盘装甜品，合法脱模后才识别最终菜品

### Requirement: 面糊批次与恢复守恒

面糊取用 MUST 每次扣除实际份数，跨关及成功档恢复不补满；未完模具加工保留冻结进度和载具关系。

#### Scenario: 最后一份面糊争用
- **WHEN** 两人请求同批最后一份用于不同模具
- **THEN** 只一个成功装模，另一请求不生成额外面糊或空壳成品

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs)。
