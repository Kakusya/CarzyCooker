# Spec Delta

## ADDED Requirements

### Requirement: 订单到制作的导航可访问

系统 SHALL 提供以下入口行为：从当前订单直接查看对应菜谱、精确制作缺口和关联顾客／餐桌；同菜多单可明确选择目标，并链接可用物品／工位的查看入口。

#### Scenario: 订单到制作的导航的正常使用
- **WHEN** 选择两张同菜订单中的一张并查看制作说明
- **THEN** 显示选定顾客和餐桌，菜谱缺口按真实物件解释，阅读不创建订单占用

### Requirement: 订单到制作的导航保持事实与操作边界

系统 MUST 遵守以下边界：订单选择只是本轮阅读上下文，不认领订单，不抢占菜品，不自动贴单／移动／交付。饮品已有合法绑定继续优先，不因选了另一订单就暗中换绑。

#### Scenario: 订单到制作的导航的取消或状态变化
- **WHEN** 所选订单过期或饮品已绑定另一有效订单
- **THEN** 旧订单导航失效，不自动交付新顾客或改杯绑定，玩家可重新选择

#### Scenario: 订单到制作的导航关闭重开与旧回调
- **WHEN** 玩家关闭并重开本入口，随后旧页面回调或迟到结果到达
- **THEN** 页面只作用于当前有效上下文，原操作不因重开重复提交，旧结果不覆盖新选择，原合法入口仍可使用

## Sources

- [当前主规格](../../../../../specs/kitchen-recipe-guide/spec.md)。
- [实际实现前提](../../../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRecipeSystem.cs)。
- [本案设计](../../design.md)、[执行与验证边界](../../execution-notes.md)。

本增量描述本案入口合同；已按对应任务实施并运行，逐项结果见 execution-notes。现有底层能力只复用行为未变部分，不由规格存在推断入口验收。
