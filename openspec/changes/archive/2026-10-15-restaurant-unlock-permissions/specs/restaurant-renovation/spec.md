# Spec Delta

## MODIFIED Requirements

### Requirement: 永久装修与本关加成分离

装修选择和已获得定义 MUST 属于持续餐厅，下一关保留，而本关 buff 和成长按既有规则重置。准备态布局变化不独立写盘；启用检查点时，已确定装修随成功结果持久化。解锁奖励记录稳定菜谱/设备定义，不直接生成实物；当前新生产许可依已解锁与本关允许集求交。许可缩小保留旧物、在途货物及已开始作业，禁止受限菜品新采购、投料、启动和新盛装，已完成批次也不例外；共用原料按仍合法菜品的闭包判断。本阶段不扩建地图，不增加采购价格或装修评分门槛。

#### Scenario: 已装修后继续及重开界面

- **WHEN** 玩家安装装修并进入下一关，再关闭重开界面
- **THEN** 工位和已获得状态保持，本关加成重新按新关选择，界面不再次安装装修

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/RestaurantRenovation.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/RestaurantRenovation.cs)。
