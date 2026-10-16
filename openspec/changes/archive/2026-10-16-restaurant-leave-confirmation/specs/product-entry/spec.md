# Spec Delta

## ADDED Requirements

### Requirement: 经营中离开与返回导航可访问

系统 SHALL 提供以下入口行为：提供按角色区分的离开入口及后果说明：Solo 返回模式、Client 离开本次运行、Host 仅关闭餐厅回房间与结束整个房间分别确认；确认前可取消。

#### Scenario: 经营中离开与返回导航的正常使用
- **WHEN** Solo 营业中取消离开后继续，Host 确认仅返回房间再开
- **THEN** 取消不影响运行，确认离开不补存档；Host 回原房间保留成员，新开世界独立

### Requirement: 经营中离开与返回导航保持事实与操作边界

系统 MUST 遵守以下边界：确认页不自动暂停或保存，需说明经营仍按原状态推进；不将技术离开伪造成功结算。Host 关闭餐厅保留 Match 和现有资格，正常结束整个 Match 沿原回执规则；提交中禁止重复且不把取消等待冒充撤销。

#### Scenario: 经营中离开与返回导航的取消或状态变化
- **WHEN** Client 持物离开、Host 结束整个 Match 或旧确认在新世界触发
- **THEN** 原成员落物／认领释放保持；关闭按原回执处理资格，旧确认不关新餐厅，失败显示实际状态

#### Scenario: 经营中离开与返回导航关闭重开与旧回调
- **WHEN** 玩家关闭并重开本入口，随后旧页面回调或迟到结果到达
- **THEN** 页面只作用于当前有效上下文，原操作不因重开重复提交，旧结果不覆盖新选择，原合法入口仍可使用

## Sources

- [当前主规格](../../../../../specs/product-entry/spec.md)。
- [实际实现前提](../../../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRoomSystem.cs)。
- [本案设计](../../design.md)、[执行与验证边界](../../execution-notes.md)。

本增量描述本案入口合同；实际交付与运行结果见 execution-notes，不由规格存在推断验收完成。
