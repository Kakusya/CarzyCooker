# ET Session、RPC 与消息分发

## Purpose

记录当前 ET 消息管线与 owner/取消边界；GameHot/GF Network 运行时在本分支已移除。未来做饭传输选择仍待裁决。 当前基线为 main 分支的 ET 底座；静态格式/源码核对不代表产品构建、Editor、玩法或联网已验证。

## Requirements

### Requirement: ET 消息管线

ET 消息 SHALL 使用当前生成的请求/响应接口与 handler/fiber 分发；UI 不把 wire response 作为第二业务事实来源。

#### Scenario: ET 消息管线的适用行为

- **WHEN** 处理 ET 请求或响应
- **THEN** 进入相应 handler/owner，不恢复已删除的 GF packet 运行时

### Requirement: Session RPC 等待者

Session.Call MUST 分配 RpcId 并登记 requestCallbacks；OnResponse 移除对应回调后设置结果，取消移除回调并构造 ERR_Cancel 响应，Session 销毁设置异常并清空等待者。

#### Scenario: Session RPC 等待者的适用行为

- **WHEN** RPC 取消或 Session 销毁
- **THEN** 沿当前 SessionSystem 的明确清理路径处理，不把机制扩大为产品重连/存档保证

### Requirement: 未来网络范围

协作文档 MUST 区分底座的 ET/KCP/WebSocket 等传输能力与未来做饭产品选型；不把被移除的示例 endpoint 当作当前生产服务。

#### Scenario: 未来网络范围的适用行为

- **WHEN** 提出做饭联网需求
- **THEN** 保持传输选择待裁决，按具体行为/环境另行授权，不自动启动服务或更换传输

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

- [Session 与 SessionSystem](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Message/Session.cs)
- [NetComponent](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Message/NetComponent.cs)
- [请求示例](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Demo/Map/C2M_TestRobotCaseHandler.cs)
- [数据与投影](../../../references/reactive-and-network.md)
