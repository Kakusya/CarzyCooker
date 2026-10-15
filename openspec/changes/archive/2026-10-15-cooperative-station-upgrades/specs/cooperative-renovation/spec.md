# Spec Delta

## MODIFIED Requirements

### Requirement: 新增工位一次安装
合法 Continue MUST 在布局及奖励预检通过后一次安装新增工位或合法原工位单级升级；失败零改动，重复或旧请求不能多造工位或升级。升级保留原身份、占地、内容和已开始作业参数；候选只含可兑现的目标及预期等级。

#### Scenario: 安装失败后重复请求
- **WHEN** 布局不满足安装条件，随后相同请求再次到达
- **THEN** 不创建部分工位或新 Level，双方能看到失败原因，原物品保持

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs)。
