# Spec Delta

## ADDED Requirements

### Requirement: 共享基底真实制备

本批咖啡、热奶和奶泡 MUST 通过有效输入及明确设备模式制备，按统一业务时间产出有限份数；模式不匹配不得产生另一基底。

#### Scenario: 加热模式不能产奶泡
- **WHEN** 玩家在热奶模式完成牛奶加工
- **THEN** 获得定义的热奶而非奶泡，未执行打泡阶段不能满足卡布奇诺最后条件

### Requirement: 新饮品沿用独立贴单

新成品饮品 MUST 继续使用原有效订单绑定、原子换绑和一次性杯消费规则；暂存不计分，旧关绑定不恢复，基底剩余份数可保存。

#### Scenario: 饮品完成后错单换绑
- **WHEN** 完成的新饮品尝试从有效绑定换到不匹配订单
- **THEN** 拒绝且保留原绑定及内容，之后合法交付仍只计一次

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDrinkSystem.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDrinkSystem.cs)。
