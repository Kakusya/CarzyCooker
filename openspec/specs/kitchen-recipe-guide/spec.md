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

## Sources

- [只读配方与缺口](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridRecipeGuide.cs)。
- [受管视图接入](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRecipeSystem.cs)。
- [实施与验证范围](../../changes/archive/2026-10-14-kitchen-recipe-guide/execution-notes.md)。

- [两个内容章的参数与实际运行记录](../../changes/archive/2026-10-15-restaurant-chapter-content/execution-notes.md)。
