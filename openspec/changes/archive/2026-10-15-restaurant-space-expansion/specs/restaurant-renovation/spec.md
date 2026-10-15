# Spec Delta

## MODIFIED Requirements

### Requirement: 永久装修与本关加成分离

装修选择和已获得定义 MUST 属于持续餐厅，下一关保留，而本关 buff 和成长按既有规则重置。准备态布局变化不独立写盘；启用检查点时，已确定装修随成功结果持久化。解锁奖励记录稳定菜谱/设备定义，不直接生成实物；当前新生产许可依已解锁与本关允许集求交。许可缩小保留旧物、在途货物及已开始作业，禁止受限菜品新采购、投料、启动和新盛装，已完成批次也不例外；共用原料按仍合法菜品的闭包判断。已接入空间扩建的有限内容可沿相同奖励链整体安装区域和餐桌；不增加采购价格或装修评分门槛。

#### Scenario: 已装修后继续及重开界面

- **WHEN** 玩家安装装修并进入下一关，再关闭重开界面
- **THEN** 工位和已获得状态保持，本关加成重新按新关选择，界面不再次安装装修

## ADDED Requirements

### Requirement: 空间餐桌扩建整体安装

扩建奖励 MUST 完整预检区域、工位、桌位、入口和必要通路后一次安装；失败保持旧关、选择和全部实物，加桌不复制顾客或餐具。

#### Scenario: 新增餐桌堵住通路
- **WHEN** 所选扩建安装计划使必要服务路径不可达
- **THEN** 拒绝整次安装，无部分区域/餐桌变化，原加工和物品保留

### Requirement: 扩建随成功状态恢复

已提交扩建 SHALL 随成功检查点保存并准确恢复；恢复不重复安装奖励或附加初始物资，Client 只观察 Host 几何。

#### Scenario: 扩建后重入
- **WHEN** 玩家从包含扩建的合法成功记录恢复
- **THEN** 恢复同一可用区域和工位数量，原份数/供应余额不回满

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)。
