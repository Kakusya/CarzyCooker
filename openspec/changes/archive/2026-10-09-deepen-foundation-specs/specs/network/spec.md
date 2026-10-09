# Spec Delta

## ADDED Requirements

### Requirement: RPC 超时与迟到响应

带 time 参数的 Session.Call SHALL 仅在 time 大于零时启动超时等待；到期后仍存在的 RpcId 等待者被移除并以超时异常结束。已经响应或已移除的等待者不再次完成；没有匹配等待者的响应被忽略。默认 time 为零不提供本入口主动超时保证。

#### Scenario: 响应先于超时

- **WHEN** 对应响应已移除等待者并完成任务，之后超时计时到期
- **THEN** 超时分支不再改变已完成任务

#### Scenario: 超时先于响应

- **WHEN** RpcId 等待者仍存在且超时已到
- **THEN** 移除等待者并返回异常；随后同 RpcId 响应不会重新写入任务结果

#### Scenario: 默认无超时调用

- **WHEN** 使用 time 等于零的调用
- **THEN** 该 overload 不创建超时任务；不能把默认等待当作已配置有界超时

### Requirement: RPC 不同终态的消费

RPC 消费者 MUST 区分成功收到响应、token 取消构造的 ERR_Cancel 响应、time 超时异常及 Session 销毁的 RpcException。传输层收到 IResponse 不自动代表业务成功；现有取消路径不保证远端执行已撤销，也不提供自动重连、重试或幂等业务效果。

#### Scenario: token 取消仍在等待的 RPC

- **WHEN** token 取消且等待者仍可移除
- **THEN** 根据请求类型构造相应响应并设为 ERR_Cancel；消费者检查 Error，而不是仅以 await 返回判断业务成功

#### Scenario: Session 销毁

- **WHEN** Session 退出且尚有多个等待者
- **THEN** 解除底层连接，将等待者置为 RpcException 并清空记录；不自动迁移到新 Session

## Sources

- [Session、RpcInfo 与 SessionSystem](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Message/Session.cs)
- [消费与投影约定](../../../../../../references/reactive-and-network.md)
