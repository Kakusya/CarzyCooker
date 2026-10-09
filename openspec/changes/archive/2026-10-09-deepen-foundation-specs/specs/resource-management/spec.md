# Spec Delta

## ADDED Requirements

### Requirement: 资源回调归属与卸载

ResourceContainer SHALL 跟踪成功加载的资源；回调结果类型不匹配时先卸载再失败，捕获的容器版本与复用后的版本不一致时卸载结果且不调用成功回调。异步路径复用该容器的取消 token。Clear 只是引用池重置，不能替代正常资源卸载；版本检查不构成所有退出时序的保证。

#### Scenario: 旧容器轮次的成功结果

- **WHEN** 回调式加载完成，但容器已经重新 Create 并改变版本
- **THEN** 卸载迟到结果，不将其加入当前轮次资源列表或投递成功回调

#### Scenario: 正常卸载与关闭

- **WHEN** 非 shutdown 路径调用 UnloadAllAssets
- **THEN** 卸载当前记录资源并取消该容器的异步任务；不能把这等同于回调式请求已取消，相关限制见已知问题

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

## Sources

- [资源容器](../../../../../../Unity/Assets/Scripts/Game/Container/ResourceContainer.cs)
- [事件容器](../../../../../../Unity/Assets/Scripts/Game/Container/EventContainer.cs)
- [集中资源更新](../../../../../../Unity/Assets/Scripts/Game/Procedure/ProcedureUpdateResources.cs)
- [限制与取舍](../../../../../../KnownIssues.md)
