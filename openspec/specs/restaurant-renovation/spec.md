# restaurant-renovation Specification

## Purpose

为连续经营提供成功后可见的永久装修选择，使一次结算选择在下一关准备态成为合法的新工位，并随同一家餐厅延续。装修不依赖星级奖励，不改变本关 buff 归属，也不通过删除原有实物或加工修补非法布局。

## Requirements

### Requirement: 成功后展示合法有限候选

系统 SHALL 在自然成功且前厅收口后，展示当前关配置中尚未获得且可合法安装的装修候选，允许玩家选择一个。零星同样适用；候选不合法或已用尽时明确显示无可选项并允许继续，不制造必选空集。首版候选为配置样机，不代表全目录解锁。

#### Scenario: 零星及候选用尽

- **WHEN** 当前关零星完成，或所有配置候选已经获得
- **THEN** 前者正常展示合法候选，后者可继续且不重复授予既有装修

### Requirement: 装修选择固定且单次安装

系统 MUST 固定本次成功的候选与选择，只接受其中一个当前结果的合法选择。重开界面不重抽，重复请求返回原事实，选择后另一方向拒绝。继续时最多安装一次；旧关选择不能改变新关或再次生成工位。

#### Scenario: 选择后重复继续

- **WHEN** 已选择的装修请求与继续请求被重复提交
- **THEN** 选择保持，下一准备态只新增一组配置规定的工位，不重复叠加

### Requirement: 装修整体预检保留旧厨房

装修 MUST 根据可信定义核对占地、朝向、操作面、出生位、路径及现有物品/加工关系，先完整预检再一次安装。无合法空间时不删除旧物或强制挪到任意格。原工位移动、原物品身份及加工进度继续遵守现有布局合同。

#### Scenario: 安装位置冲突

- **WHEN** 继续时新增工位的位置被占用或堵住必要通路
- **THEN** 整次安装和换关拒绝，选择、旧关、原物品及加工保持不变

### Requirement: 永久装修与本关加成分离

装修选择和已获得定义 MUST 属于持续餐厅，下一关保留，而本关 buff 和成长按既有规则重置。准备态布局变化不独立写盘；启用检查点时，已确定装修随成功结果持久化。解锁奖励记录稳定菜谱/设备定义，不直接生成实物；当前新生产许可依已解锁与本关允许集求交。许可缩小保留旧物、在途货物及已开始作业，禁止受限菜品新采购、投料、启动和新盛装，已完成批次也不例外；共用原料按仍合法菜品的闭包判断。已接入空间扩建的有限内容可沿相同奖励链整体安装区域和餐桌；不增加采购价格或装修评分门槛。

#### Scenario: 已装修后继续及重开界面

- **WHEN** 玩家安装装修并进入下一关，再关闭重开界面
- **THEN** 工位和已获得状态保持，本关加成重新按新关选择，界面不再次安装装修

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

- [产品装修目标](../../../Docs/Product/design.md)、[工作台前置](../../changes/archive/2026-10-13-solo-workstation-layout/design.md)。
- [关卡前置](../../changes/archive/2026-10-13-solo-restaurant-level-flow/specs/restaurant-level-lifecycle/spec.md)、[本案设计](../../changes/archive/2026-10-14-solo-interlevel-renovation/design.md)。

- [solo-interlevel-renovation 逐功能运行与验收](../../changes/archive/2026-10-14-solo-interlevel-renovation/execution-notes.md)。

- [持久解锁与当前许可运行记录](../../changes/archive/2026-10-15-restaurant-unlock-permissions/execution-notes.md)。

- [有限区域/单客桌及成功载荷恢复](../../changes/archive/2026-10-15-restaurant-space-expansion/execution-notes.md)。
