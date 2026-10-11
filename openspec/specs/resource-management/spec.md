# 资源、事件、实体及 Widget 容器的所有权

## Purpose

记录当前 CarzyCooker 底座的资源、事件、实体及 Widget 容器的所有权。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；本次不运行产品构建、玩法或编辑器验证，不描述未来做饭业务已实现。

## Requirements

### Requirement: 生命周期容器

AExUIForm/AExEntity SHALL 复用已有事件、实体、资源和池容器；派生层只清自己直接拥有的资源，不能重复全量清理。

#### Scenario: 生命周期容器的适用行为

- **WHEN** 托管对象关闭或隐藏
- **THEN** 由 base/容器清本轮所有权，保留对象池与重复进入契约

### Requirement: 配置资源路由

资源加载 MUST 复用 AssetUtility、GameEntry.Resource、ResourceContainer 和既有 AssetSet 流程；路径源于配置与对应资源根，不另建同义缓存/loader。

#### Scenario: 配置资源路由的适用行为

- **WHEN** 为 Image/RawImage 或业务 view 装载资源
- **THEN** 先选现有资源设置/容器，取消和回收归明确 owner

### Requirement: 资源回调归属与卸载

资源容器 SHALL 跟踪本轮成功资源，并在正常卸载、关闭、Clear 和复用时使旧加载轮次失效。成功、失败、进度及依赖回调 MUST 按原归属检查；旧成功归还本请求引用而不通知业务。有效轮次类型不匹配时先卸载再失败。资源等待在失效时立即取消；Clear 仍只是重置，不能替代正常卸载。

#### Scenario: 旧容器轮次的成功结果

- **WHEN** 回调式加载完成，但容器已经关闭或重新 Create 改变轮次
- **THEN** 归还迟到结果，不将其加入当前资源列表或投递成功回调，也不向旧业务投递类型错误

#### Scenario: 正常卸载与关闭

- **WHEN** 非 shutdown 路径调用 UnloadAllAssets
- **THEN** 卸载本轮已取得资源并立即取消本轮资源等待；阻止旧回调写入，不取消该 owner 无关的延时或请求

#### Scenario: 旧轮次失败或进度

- **WHEN** 关闭、Clear 或复用之后到达旧请求的失败、进度或依赖通知
- **THEN** 不调用旧业务回调，不改当前轮次状态

#### Scenario: 有效轮次加载正常完成

- **WHEN** 有效轮次加载返回正确类型资源
- **THEN** 本请求引用只登记一次，并由原容器的正常卸载处理，不依赖调用者读取结果

### Requirement: 事件容器的订阅清理

EventContainer MUST 记录经自身添加的订阅；严格取消未登记项时失败，TryUnsubscribe 对不存在项不失败。非 shutdown 的全量取消解除实际事件订阅并清记录；shutdown 路径仅清本地记录，不能把引用池 Clear 当成实际事件解绑。

#### Scenario: 正常 owner 退出

- **WHEN** 正常退出路径通过容器取消全部订阅
- **THEN** 解除该容器登记的全局事件关系，不再保留本轮订阅记录

#### Scenario: 可选与必需取消

- **WHEN** 取消一个没有在此容器登记的 handler
- **THEN** 严格入口抛出异常，Try 入口允许无动作返回；调用者按自身接口契约选择

### Requirement: 资源更新成功与失败

集中资源更新 MUST 仅在完成回调报告结果为 true 时设置完成标志并推进资源完成流程。单项失败未达重试上限时从当前进度统计移除；达到上限时记录错误并立即返回，不执行该移除分支。进度变化不能作为更新成功依据。离开流程时解除四类更新事件订阅并销毁更新界面。

#### Scenario: 更新完成成功

- **WHEN** 整体更新回调报告 true
- **THEN** 后续帧进入资源完成流程

#### Scenario: 更新完成失败

- **WHEN** 整体更新回调报告 false
- **THEN** 记录错误且不设置成功完成标志；现有流程不保证另有失败 UI、自动回退或再次发起整轮更新

### Requirement: 进行中加载的独立收尾

业务取消与原生加载结束 MUST 分开处理。原加载责任 SHALL 保留到请求终止或安全交接，晚到结果按原 backend 和本次引用释放，不能访问已拆卸模块、当前新运行的管理器或提前复用的记录。整体退出不等待原生完成；不能以清空请求引用或业务登记清零证明原生已释放。

#### Scenario: 关闭后加载仍继续

- **WHEN** 业务等待已经取消，原加载随后成功
- **THEN** 原请求负责归还其引用，不访问新 owner，不重复卸载其他调用者共享的资源

#### Scenario: 加载系统拆卸

- **WHEN** 加载代理被 Reset/Shutdown 且不再投递业务完成，而原生请求尚未完成
- **THEN** 保留或交接原请求的独立收尾责任；若运行继续，晚到完成只处理原生尾部，不调用已销毁的业务或模块

#### Scenario: 正常进程结束

- **WHEN** 程序退出导致进程完整结束，原生尾部没有机会再次执行 C# 回调
- **THEN** 业务等待仍已提前取消，不能要求再次回调才准许退出；报告区分退出前业务终态与已观察到的原生释放

## Sources

- [AExUIForm.cs](../../../Unity/Assets/Scripts/Game/UI/Common/AExUIForm.cs)
- [AExEntity.cs](../../../Unity/Assets/Scripts/Game/Entity/EntityLogic/AExEntity.cs)
- [ResourceContainer.cs](../../../Unity/Assets/Scripts/Game/Container/ResourceContainer.cs)
- [EntityContainer.cs](../../../Unity/Assets/Scripts/Game/Container/EntityContainer.cs)
- [EventContainer.cs](../../../Unity/Assets/Scripts/Game/Container/EventContainer.cs)
- [UIWidgetContainer.cs](../../../Unity/Assets/Scripts/Game/Container/UIWidgetContainer.cs)
- [AssetSet.md](../../../Book/AssetSet.md)
- [集中资源更新](../../../Unity/Assets/Scripts/Game/Procedure/ProcedureUpdateResources.cs)
- [限制与取舍](../../../KnownIssues.md)
