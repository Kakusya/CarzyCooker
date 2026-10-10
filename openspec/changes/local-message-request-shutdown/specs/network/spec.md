# Spec Delta

## ADDED Requirements

### Requirement: 进程内请求停止与销毁收尾

本地消息发送器 MUST 提供停止行为：停止接收新请求，并直接结束其全部未完成等待，再完成资源收尾；销毁须复用同一处理。完成等待不得依赖对端答复、消息队列继续运转或超时计时到期。

#### Scenario: 等待期间主动停止

- **WHEN** 本地请求尚未收到答复，所属发送器开始停止
- **THEN** 全部待答复请求获得退出终态，等待名单清空，不等待既有超时到期

#### Scenario: 直接销毁

- **WHEN** 未先执行正常停止便销毁发送器或其所属 Fiber
- **THEN** 销毁仍结清全部未完成等待，即使计时器或其他运行时资源已不可用也不遗留请求

#### Scenario: 停止后再次调用

- **WHEN** 向已经停止或销毁的发送器提交新的请求或消息
- **THEN** 调用明确拒绝，不新增等待记录，不将消息加入队列

### Requirement: 进程内请求退出终态

已提交本地请求的退出终态 MUST 与成功、业务失败和超时区分：needException 为 true 时以携带 ERR_Cancel 的 RpcException 结束，为 false 时返回原请求对应类型且 Error 为 ERR_Cancel 的响应。结果只表示本地等待已结束，不保证接收端未执行或撤销操作。

#### Scenario: 退出时两种消费模式

- **WHEN** 同一发送器有两种 needException 模式的未完成请求并发生停止
- **THEN** 两者分别获得规定的异常或取消响应，都不被报告成成功

#### Scenario: 正常结果保持

- **WHEN** 请求在停止前已正常完成，或发送器保持运行并发生既有业务失败或超时
- **THEN** 继续采用原有结果与消费方式，停止不会改写已经完成的结果，运行中超时不会改成退出取消

### Requirement: 进程内请求单次完成与重复收尾

本地请求 MUST 在答复、超时、停止中只完成一次；结束记录须先从等待名单移除，再通知消费者。重复停止、重复销毁或通知期间重新发起操作不得重复完成等待、破坏收尾或释放重建实例的资源。

#### Scenario: 退出先于答复

- **WHEN** 停止已经结束请求，随后旧答复或旧超时到达
- **THEN** 请求保持退出终态，不再次成功、不再次失败

#### Scenario: 答复先于退出

- **WHEN** 正常答复先完成请求，随后执行停止或重复停止
- **THEN** 保留原结果，每个等待者最多收到一次完成

#### Scenario: 完成通知发生重入

- **WHEN** 请求结束通知中的调用者再次停止发送器或建立后续运行
- **THEN** 旧收尾不重复通知、不删除新实例的队列，也不因正在处理的消息批次被清理而产生迭代异常

### Requirement: 进程内请求跨实例隔离

本地消息请求 MUST 核对等待任务所属的实例生命周期，并使旧答复无法凭重复请求编号完成重建实例的请求。隔离覆盖同一托管域内的停止／重建及对象复用，不提供跨进程重启或托管域重载的请求恢复。

#### Scenario: 同一 Fiber 标识重建

- **WHEN** 旧发送器停止后在同一 Fiber 标识下建立新发送器，旧答复随后到达
- **THEN** 旧答复不匹配新请求，新请求仍等待自己的答复

#### Scenario: 旧计时在对象复用后继续

- **WHEN** 旧请求的超时任务恢复时，其发送器已经销毁或进入另一实例生命周期
- **THEN** 旧任务不读取或移除新实例等待记录，也不访问已退出的计时／耗时统计资源

## Sources

- [本地发送器](../../../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Actor/ProcessInnerSender.cs)、[当前收尾与请求实现](../../../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Actor/ProcessInnerSenderSystem.cs)
- [已有等待者](../../../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Actor/MessageSenderStruct.cs)、[消息队列](../../../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/World/Module/Actor/MessageQueue.cs)
- [错误码](../../../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/ErrorCore.cs)、[现有网络主规格](../../../../specs/network/spec.md)
- [实体生命周期主规格](../../../../specs/runtime-foundation/spec.md)
