# Spec Delta

## MODIFIED Requirements

### Requirement: 有限原工位升级候选
Solo 与已接入升级的合作非终关装修 SHALL 可提供匹配现有加工工位的同占地单级升级，候选明确目标工位和预期等级。已升级、目标不存在或能力不符不得成为合法选择。

#### Scenario: 无可升级目标
- **WHEN** 餐厅没有符合该奖励的未升级工位
- **THEN** 不提供不可兑现的升级候选，其他合法奖励仍按原流程选择

### Requirement: 首版模式范围明确
升级候选 MUST 向 Solo 及已接入本能力的合作模式开放。合作只有 Host 当前本地合法成员可选择和继续，远端只读；尚未接入持久化的合作升级仅属于本轮内存餐厅。

#### Scenario: 合作生成装修候选
- **WHEN** Host 创建本轮联机装修候选
- **THEN** 候选只包含当前可兑现的新增或单级升级，远端直接提交选择须拒绝且无改动

## ADDED Requirements

### Requirement: 合作升级投影与目标一致

合作升级 MUST 向双方提供当前候选、目标工位、预期等级与已提交结果的独立值投影，恢复成员取得当前事实，不重演历史选择。

#### Scenario: 升级后原成员恢复
- **WHEN** Host 已完成升级，原成员随后合法恢复
- **THEN** 恢复端读取同一等级和工位身份，不新增升级或重算作业

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs)。
