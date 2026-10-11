# Spec Delta

## ADDED Requirements

### Requirement: owner 所属请求等待

迁移后的请求等待 MUST 关联实际调用者，退出时立即取消其业务等待，保留原 Session、本地发送器、ActorId、MailBox 和裁决路径。取消 SHALL 与成功响应区分，不撤销接收端已经提交的动作，不以单 owner 退出停止共用传输；复用现有 RPC 等待表，不建立第二套 RpcId 映射。

#### Scenario: Session 请求等待中调用者退出

- **WHEN** 所属对象等待 Session 请求响应时退出
- **THEN** 该业务等待取消，原请求取消结果不当作成功；其他 owner 仍能使用同一 Session

#### Scenario: 本地发送器没有逐请求取消入口

- **WHEN** 所属对象等待本地 Call 时退出，而原发送器仍运行
- **THEN** 先结束该调用者等待，原物理等待由答复或既有有界超时收尾；不要求全表立即清空或调用 sender.Stop

#### Scenario: 接收端动作已提交

- **WHEN** 接收端提交动作后，发送侧调用者退出或取消等待
- **THEN** 保留接收端事实，取消不自动构成回滚命令

### Requirement: 所属请求的返回对象收尾

请求适配层 MUST 明确处理自己取得的响应对象：在安全有效期内转成独立值或显式移交所有权；取消、无人消费和晚到响应均不得造成漏收尾、重复 Dispose 或旧 owner 写回。原收件端与传输入口的消息责任 SHALL 保持，不抢占别处持有的池化消息。

#### Scenario: 取消后适配器取得晚到响应

- **WHEN** 对外等待已取消，内部观察者仍从原请求取得响应
- **THEN** 收尾自己取得的响应，不完成旧业务等待为成功，不向新 owner 写回

#### Scenario: 正常响应无人读取

- **WHEN** 受管请求完成后调用者未读取对外结果
- **THEN** 适配器仍按约定完成原池化响应的收尾；保留给业务的值不依赖被回收的消息对象

## References

新增适配契约的实际底座见 [Session.cs](../../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Message/Session.cs)、[ProductClientSystem.cs](../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductClientSystem.cs) 和 [本地/远端桥](../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs)。完整路径与 owner 树见 [design.md](../../design.md)。
