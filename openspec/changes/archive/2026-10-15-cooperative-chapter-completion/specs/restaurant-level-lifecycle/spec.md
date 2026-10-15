# Spec Delta

## MODIFIED Requirements

### Requirement: 联机换关按当前身份重新就绪

Host 的非终关合法 Continue MUST 只安装一个新关；明确终关拒绝 Continue，按合作终关结果流程结束餐厅回房间。非终关换关须保留同一厨房实物与批次，清理旧顾客、订单、工作、成长和 Buff；成员的新角色属于新关。客户端在取得匹配关身份、本人角色和状态前禁用输入，旧关高版本与旧结果不能覆盖新关。

#### Scenario: 重复继续及旧消息迟到
- **WHEN** Host 重放已成功 Continue，同时 Client 旧关请求和快照迟到
- **THEN** 只发生一次换关，物资不补满，旧请求被拒绝，旧快照不替代新关，双方从新角色就绪后继续操作

#### Scenario: 合作终关继续被拒绝
- **WHEN** 合作终关已自然完成而 Continue 或旧按钮到达
- **THEN** 不建第四关、不发装修，保留结果等待 Host 合法确认回房间

### Requirement: 联机营业自然收口

合作餐厅 MUST 沿用 Preparing、Operating、Draining、Completed 与原零星成功规则。Host 唯一业务时间驱动顾客、伙伴和工作，营业到点只停止来客；既有前厅实际排空才完成。Client 不得自行推进、提前完成或触发下一关。

#### Scenario: 双端等最后一个碗洗完
- **WHEN** 营业到点且最后一个脏碗仍在清洗
- **THEN** 两端保持 Draining 并观察实际进度，洗净及前厅排空后才显示同一结算，不靠远端帧数瞬间完成

#### Scenario: 零分自然完成
- **WHEN** 所有未满足订单按时离开且前厅排空
- **THEN** 两端显示同一零分结果，非终关Host可以继续，终关按完成流程返回房间；技术退出不能伪造此结果

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs)。
