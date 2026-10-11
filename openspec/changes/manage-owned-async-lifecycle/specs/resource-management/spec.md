# Spec Delta

## MODIFIED Requirements

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

## ADDED Requirements

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

## References

这是拟实施契约，当前入口见 [ResourceContainer.cs](../../../../../Unity/Assets/Scripts/Game/Container/ResourceContainer.cs)、[资源等待](../../../../../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Runtime/Awaitable/Awaitable.ResourceComponent.cs) 和 [原生请求 helper](../../../../../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Resource/DefaultLoadResourceAgentHelper.cs)。真实原生验证范围见 [test-conditions.md](../../test-conditions.md)。
