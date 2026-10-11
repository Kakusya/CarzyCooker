# Spec Delta

## ADDED Requirements

### Requirement: Entity 所属异步与实例隔离

公共视图及迁移后的 ET 资源桥 MUST 为异步工作关联实际 owner、视图轮次及原 ET 实例身份。视图 Hide SHALL 结束该轮视图工作，ET owner 移除或 Dispose SHALL 取消其所属工作；较短命业务对象不能默认把所有工作绑定到 Scene 寿命。该接入保留原 Component/Child 树和逻辑状态所有权。

#### Scenario: 资源加载中 ET 对象销毁

- **WHEN** ET 业务对象经资源桥加载视图资源，完成之前被移除或 Dispose
- **THEN** 所属业务等待取消，晚到资源由原请求收尾，不写入已销毁对象或同 Id 的新实例

#### Scenario: GF 视图仅隐藏

- **WHEN** GF Hide 结束一轮视图，但 ET 逻辑 owner 仍存活
- **THEN** 该轮视图工作失效，不将 Hide 自动当成销毁 ET owner；再次 Show 取得新视图轮次

#### Scenario: 子业务对象先于 Scene 退出

- **WHEN** Scene 仍运行，但执行资源加载的较短命子对象退出
- **THEN** 按实际子对象 owner 取消，不等待 Scene 退出才能停止该对象工作

#### Scenario: 运行中的创建视图等待

- **WHEN** 当前 Unit/LSUnit 的迁移路径在 await 后准备绑定 Mono 或 Transform
- **THEN** 按原 owner/实例及视图轮次检查后写回，原实例失效则拒绝绑定并收尾

## References

本新增要求不修改第一项 change 的 UIEntity 路由 Requirement；当前入口见 [UGFComponent.cs](../../../../../Unity/Assets/Scripts/Game/ET/Loader/UGF/UGFComponent.cs)、[AExEntity.cs](../../../../../Unity/Assets/Scripts/Game/Entity/EntityLogic/AExEntity.cs) 和 [Entity 手册](../../../../../Book/Entity开发.md)。
