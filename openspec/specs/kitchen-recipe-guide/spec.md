# kitchen-recipe-guide Specification

## Purpose

以当前有效配方和只读投影解释已开放菜品的原料、份数、设备、容器、工序与缺口，并提供可跳过、可重看的Solo首单说明。菜谱与提示不生成物品或代交动作，新玩家理解观察仍不在自动化验收范围内。

## Requirements

### Requirement: 菜谱对应有效配方

菜谱 SHALL 展示当前已开放菜品实际需要的原料、加工步骤、精确份数、设备及容器，名称和要求与生效定义一致；无效引用不得被显示成可执行配方，不限于旧汤、吐司、橙汁。提示只解释当前实际阶段与缺口，不自动完成动作。

#### Scenario: 查看汤和吐司
- **WHEN** 玩家打开任一已开放菜谱
- **THEN** 可找到完成制作和交付需要的依赖，不出现免费的原料或不存在的设备

#### Scenario: 选择新菜单菜品
- **WHEN** 玩家查看新包中已开放菜谱且已有部分备料
- **THEN** 提示读取当前有效定义和实物，显示剩余步骤，无须重做或免费生成材料

### Requirement: 引导以当前事实更新
Solo 首单提示 MUST 依据当前关和当前物品事实计算阶段与缺口，允许跳过和重新打开；已经完成的合法备料无需重复制作，动作拒绝不能使步骤提前完成。

#### Scenario: 预先完成备料再打开引导
- **WHEN** 玩家已有合法半成品或成品并开启提示
- **THEN** 显示当前仍缺的条件，不强制重做已完成步骤

#### Scenario: 换关和失效目标
- **WHEN** 原订单或目标失效，或当前 Level 已切换
- **THEN** 旧提示失效并按当前事实更新，不指向旧顾客或旧关动作

### Requirement: 提示仅提供解释
菜谱和引导 MUST 不直接改变物品、得分、时间或控制权限，不自动提交玩家动作。联机成员可只读菜谱，但阅读不获得 Host 控制权。

#### Scenario: 暂停或远端阅读
- **WHEN** 暂停中的 Solo 玩家或远端 Client 打开菜谱
- **THEN** 可以阅读说明，世界不因提示推进，原操作权限保持

### Requirement: 引导视图可安全重开
关闭、重开或跳过引导 MUST 保留实际经营进度，且不能增加重复动作回调或恢复上一关的提示。

#### Scenario: 多次开关后提交动作
- **WHEN** 玩家多次关闭重开引导后执行一次真实操作
- **THEN** 只产生一次原操作意图，提示按实际结果更新

### Requirement: 制作提示尊重实物可用性与载具
菜谱提示 MUST 区分可取用物料、锁定输入、批次产物和载具内内容；熟蛋糕仍在模具中时指向脱模，已有同一载具的部分配料按精确缺口继续，不能因其他作业保留输入就跳过备料。提示只读且不创建物品。

#### Scenario: 原料已经锁定在加工批次
- **WHEN** 别的作业保留了该原料但当前制作没有可用份数
- **THEN** 不将锁定输入当作可自由使用材料

#### Scenario: 蛋糕已经烤熟但仍在原模具
- **WHEN** 查看蛋糕下一步
- **THEN** 指向原模具脱模及干净空甜品盘，不要求重新搅拌面糊

### Requirement: 饮品交付提示保持已有合法绑定
已有等待订单的饮品提示 MUST 优先指向其有效绑定订单；不能因为另一同菜订单更早就要求重绑。失效绑定需提示先解绑再选当前有效订单。

#### Scenario: 同菜品更早订单存在
- **WHEN** 查看已绑定较晚订单的现成饮品
- **THEN** 指向该饮品自己的有效订单，不要求绑定更早订单


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

- [实物阶段及绑定提示验收](../../changes/archive/2026-10-16-recipe-guidance-physical-progress/execution-notes.md)。

- [只读配方与缺口](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridRecipeGuide.cs)。
- [受管视图接入](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRecipeSystem.cs)。
- [实施与验证范围](../../changes/archive/2026-10-14-kitchen-recipe-guide/execution-notes.md)。

- [两个内容章的参数与实际运行记录](../../changes/archive/2026-10-15-restaurant-chapter-content/execution-notes.md)。
- [order-production-navigation 逐项验收](../../changes/archive/2026-10-16-order-production-navigation/execution-notes.md)?
