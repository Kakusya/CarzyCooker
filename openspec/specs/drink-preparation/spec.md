# drink-preparation Specification

## Purpose

定义鲜榨橙汁和首批三款咖啡的真实制作、有限原料与杯子供应、明确设备模式、有限基底及按份混合，保持原有加工锁定、暂停、清理与成功档恢复守恒。饮品身份和订单绑定分开，成品须合法贴单后一次性交付消费。

## Requirements

### Requirement: 橙汁使用真实原料和器具
橙汁 MUST 由实际橙子切块、装入果汁壶并在合法榨汁机显式启动加工获得。原料、果汁壶和一次性杯均来自有效初始物资或有限供应，不以点击制作免费生成。

#### Scenario: 缺少橙块或器具
- **WHEN** 玩家在没有所需原料或合法果汁壶时请求榨汁
- **THEN** 拒绝启动，原物品不减少，不生成果汁

### Requirement: 加工锁壶且完成解锁
榨汁 MUST 在启动时冻结配置的耗时与产量，并锁定果汁壶及使用中的设备；加工中禁止搬运、倒掉或转移该批次。完成后解锁，果汁与剩余份数随壶移动，暂停不推进加工。

#### Scenario: 加工中搬壶与完成后搬壶
- **WHEN** 玩家先在加工中、再在完成后尝试搬走果汁壶
- **THEN** 前者被拒绝且状态保持，后者合法搬运并保留全部剩余份数

### Requirement: 盛装每次消费一份
盛装 MUST 将完成果汁的一份转入一个合法干净空杯；空批次、非空杯或非法位置不得部分扣减。最后一份被盛出后不可再次盛装。

#### Scenario: 盛出最后一杯
- **WHEN** 壶中只剩一份，玩家连续提交两次盛装
- **THEN** 只第一项合法动作获得一杯，后续拒绝且没有负份数或多余果汁

### Requirement: 饮品清理与跨关保存守恒
可清理的液体 MUST 经水池处理，固体按垃圾桶规则；未完成锁定作业不能绕过锁定清理。跨关及有效保存恢复保留合法实物、剩余份数和加工冻结进度，当前关订单绑定不带入新关。

#### Scenario: 半壶果汁跨关保存恢复
- **WHEN** 已盛出部分果汁后保存并进入下一关，或从该有效记录恢复
- **THEN** 原合法剩余份数保持，不补满壶，不恢复旧关贴单关系

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

- [实施与运行记录](../../changes/archive/2026-10-14-first-drink-order-binding/execution-notes.md)
- [自动批次实现](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSoupSystem.cs)

- [三款咖啡、模式与顺序运行](../../changes/archive/2026-10-15-kitchen-beverage-batch/execution-notes.md)、[基底添加预检](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBeverageSystem.cs)、[份数与顺序定义](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridAssemblyConfig.cs)。
