# Spec Delta

## MODIFIED Requirements

### Requirement: 盘装是独立物品转移

玩家 MUST 将真实烤面包放入兼容空盘才获得盘装吐司；原材料身份唯一，盘容量和位置遵守普通容器规则。若另一条已交付配方明确把吐司作为组件，可继续加入该配方规定的其他组件和份数，部分组合不能冒充吐司或最终菜品。失败不消费或交换；设备不充当出餐容器。

#### Scenario: 烤面包入盘及满盘拒绝
- **WHEN** 玩家先将烤面包放进合法空盘，再尝试向已满盘加一份
- **THEN** 第一次仅转移同一烤面包，第二次拒绝并保留两端物品，不出现第二份内容

#### Scenario: 早餐中的真实吐司组件
- **WHEN** 盘中已有一份真实吐司并加入早餐配方规定的煎蛋和熟香肠
- **THEN** 仅允许该配方精确份数以内的部分组合，齐料后仍须显式组装；错料或第二份吐司拒绝且不消费

## ADDED Requirements

### Requirement: 热餐由共享熟料组装

首批 F11经典牛肉汉堡/F43香肠鸡蛋早餐 MUST 按实际阶段由生料、熟料及既有备料合流，熟料可交接暂存，最终容器和精确份数齐全才可识别交付。

#### Scenario: 一人煎制另一人装配
- **WHEN** 甲制作熟料并交给乙，乙加入其他合法组件组装
- **THEN** 只消费真实各份输入并形成一个成品，免费熟料和未熟输入均不能替代

### Requirement: 新增热餐全链可达

纳入首批的热餐 SHALL 具有正式源定义、有限供应、设备、容器、订单和菜谱路径，保存恢复保留未完成作业和真实物品，不只登记菜名。

#### Scenario: 保存未完成煎制
- **WHEN** 在合法成功检查点中存在暂停或跨关煎制
- **THEN** 恢复保持输入、冻结参数及剩余进度，不提前完成或重发初始材料

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs)。
