# Spec Delta

## ADDED Requirements

### Requirement: 本地消息输入的交接与丢弃收尾

本地请求和通知 MUST 有明确输入所有者。尚未交接的失败由发起边界释放；交接后由队列、分发层或接收 handler 中实际持有者恰好收尾一次。目标不存在、等待邮箱期间目标退出及停止撤下队列不能遗留无人收尾的消息，也不能回收已交给 handler 的输入。既有成功、错误、取消和超时合同保持，不新增私有等待表。

#### Scenario: 发起时目标队列不存在
- **WHEN** 本地请求或通知无法交给目标队列
- **THEN** 输入按明确交接规则只释放一次，请求取得适用失败结果，通知不得报告已完成业务

#### Scenario: 入队后目标邮箱消失
- **WHEN** 已入队输入被取出时目标邮箱已经销毁
- **THEN** 分发方释放尚未交付的输入；可回复的请求返回目标失效，通知不触发任何新实例业务

#### Scenario: 有序等待期间实例被替换
- **WHEN** 输入等待有序执行机会期间，原邮箱所属实例退出或复用
- **THEN** 不调用新实例 handler，原输入只回收一次，失败答复或等待取消沿原发送器身份收尾

#### Scenario: 停止时仍有未取出的消息
- **WHEN** 所属发送器停止并撤下尚含消息的队列
- **THEN** 未交付输入被收尾，待答复请求按原停止合同结束，已经交付的输入仍由原 handler 负责

#### Scenario: 单调用者退出但共用发送器继续
- **WHEN** 一个 owner 取消显示等待，原请求仍由有效 handler 处理
- **THEN** 不提前回收 handler 持有的输入，也不停止共用发送器；原物理答复结束后释放对应响应

### Requirement: 技术退出不依赖终态消息消费

整个运行的技术退出 MUST 及时使所属等待失效并拆卸原资源，不依赖终态通知在未来帧被消费。停止前已提交的业务事实不因通知未送达而回滚；未交付结果不得伪装成功。单成员退出仍不得停止其他成员的公共消息资源。

#### Scenario: 终态通知尚在队列就整体退出
- **WHEN** 本轮已提交业务终态，但通知尚未被消费，随后消息泵停止
- **THEN** 原等待与消息责任可直接收尾，不等待下一帧，不向新运行交付旧结果，也不补造未提交的成功

## Sources

以上为本案补充合同；当前缺口来自 [本地发送器](../../../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Actor/ProcessInnerSenderSystem.cs)、[消息队列](../../../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/World/Module/Actor/MessageQueue.cs)、[Ordered 分发](../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Module/Actor/MailBoxType_OrderedMessageHandler.cs)。原等待与取消合同见 [network 主规格](../../../../specs/network/spec.md)。
