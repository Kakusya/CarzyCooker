# Spec Delta

## ADDED Requirements

### Requirement: 联机营业自然收口

合作餐厅 MUST 沿用 Preparing、Operating、Draining、Completed 与原零星成功规则。Host 唯一业务时间驱动顾客、伙伴和工作，营业到点只停止来客；既有前厅实际排空才完成。Client 不得自行推进、提前完成或触发下一关。

#### Scenario: 双端等最后一个碗洗完
- **WHEN** 营业到点且最后一个脏碗仍在清洗
- **THEN** 两端保持 Draining 并观察实际进度，洗净及前厅排空后才显示同一结算，不靠远端帧数瞬间完成

#### Scenario: 零分自然完成
- **WHEN** 所有未满足订单按时离开且前厅排空
- **THEN** 两端显示同一零分结果，Host 可以继续，技术退出不能伪造此结果

### Requirement: 联机暂停不阻断控制通信

Host 暂停 MUST 取消各成员尚未执行的动作，冻结已开始加工、前厅工作、顾客及角色当前步；恢复与退出通信仍工作。恢复后接续已开始进度，但不恢复被取消的旧目标。远端无权自行恢复世界。

#### Scenario: 远端服务途中暂停
- **WHEN** Client 正在接近餐桌且已有另一项前厅工作进行，Host 暂停再恢复
- **THEN** 接近的目标被取消，当前步及已开始工作保留；两端恢复后不自动提交旧交付，控制请求仍有答复

### Requirement: 联机换关按当前身份重新就绪

Host 的合法 Continue MUST 只安装一个新关，保留同一厨房实物与批次，清理旧顾客、订单、工作、成长和 Buff；成员的新角色属于新关。客户端在取得匹配关身份、本人角色和状态前禁用输入，旧关高版本与旧结果不能覆盖新关。

#### Scenario: 重复继续及旧消息迟到
- **WHEN** Host 重放已成功 Continue，同时 Client 旧关请求和快照迟到
- **THEN** 只发生一次换关，物资不补满，旧请求被拒绝，旧快照不替代新关，双方从新角色就绪后继续操作

## Sources

- [主规格](../../../../../specs/restaurant-level-lifecycle/spec.md)、[Solo 关卡记录](../../../2026-10-13-solo-restaurant-level-flow/execution-notes.md)。
- 本案仍为内存餐厅；不新增三关终点、联机装修或保存门槛。
