# 响应式投影与网络数据约定

**非规格**；冲突以代码/表、进行中 change、已同步主规格及用户最新意图为准。**阅读 ≠ 授权**文中未做能力。

## 状态与 UI

UI 观察权威领域状态或明确 UI context，不保存网络 response 作为第二事实来源。每项独立表现只有一个更新 owner，首次投影与后续刷新走同一机制；不要叠 event、OnOpen 手动 Refresh、Bind 和 UI cache 更新同一控件。

外部 ReactiveBinding 是 manifest 声明的包；ET 的 IETReactive/ETReactiveSystem/Source/Bind 是仓库自己的生成机制。两套 attribute 不混用。ET owner 实现 IETReactive、声明 partial，System 符合 EntitySystemOf 与生成诊断，Bind 用 nameof 指向源。具体签名查 [响应式规格](../openspec/specs/reactive-ui/spec.md) 的源文件。

最小投影只依赖相关字段/语言/可用状态。Bind 只写表现，不发请求、不写领域状态、不承担生命周期清理。集合变更需按选中机制传播版本；不能假定普通集合修改就可观察。对象池与上下文重绑清理观察缓存，具体 Reset API 以实际包或 ET generator 为准。

首次投影、调用次数节流、多源合并、Reset 及版本源签名详见 [主规格](../openspec/specs/reactive-ui/spec.md)。Reset 不代替业务清理或事件解绑，attribute 不自动安排每帧观察。

## 数据与网络

明确类型身份、单位、更新语义和 owner。全量替换、增量应用与消息事件不同；缺字段不能用默认值覆盖未更新数据。池化对象在 Dispose/OnHide/OnRecycle 清自身状态；UI 不另存服务器缓存。

当前 main 分支的 ET 底座使用 ET message/handler/fiber、Session/RPC 与已有服务端 HttpComponent；GameHot/GF Network 运行时已移除。复用对应 owner，不复制 correlation/channel/请求字典。网络/解析/业务失败、取消、会话切换、stale 不 Apply；成功且当前的必需 payload 落到唯一 owner。传输 ACK 不等于业务结果。

UI 触发领域入口并观察状态，不拼 endpoint、不解析 wire response。没有实际 HTTP 消费者时不凭空建 HTTP gateway；当前服务端 HttpListener 能力不表示客户端产品流程已接入。

当前分支与手册残留差异见 [已知问题](../KnownIssues.md)。Session.Call 按 RpcId 管理等待者，取消移除回调，Session 销毁设置异常并清空；不自动等同于未来做饭重连语义。临时格子地图的固定 Tick 已由 GridKitchenComponent 实施；重连、持久餐厅与存档尚未实施，通用 Session helper 不提供这些业务保证。

取消响应、超时异常、销毁异常和迟到响应的不同处理详见 [网络主规格](../openspec/specs/network/spec.md)；默认 time=0 不创建该 overload 的主动超时。token 取消不保证撤销远端已执行业务。

## 进程内发送器停止

每个 Fiber 根一份 ProcessInnerSender，接收 Actor 挂 MailBox。目标来自 Actor.GetActorId（含当前 InstanceId），Call/Send 入 MessageQueue，sender.Update 找到目标 MailBox，再经 MessageDispatcher 按接收者 SceneType 处理；响应按原 FromAddress 回到 root sender 等待名单。

整体退出应先调用该 sender.Stop，再释放所属树；IDestroy 复用 Stop 兜底。Stop 先标停止、快照并清等待名单、撤下捕获的原队列，再直接结束等待；通知中重建同 Fiber 不会被旧 Destroy 再删队列。停止后新 Call/Send/Reply 抛 ObjectDisposedException，不新增等待。单连接退出不能 Stop 共用 sender。

已提交等待：needException=true 得到 ERR_Cancel 的 RpcException；false 得到原请求对应响应类型，Error=ERR_Cancel，保留 RpcId。响应类型提交时捕获，取消构造不依赖已退出的 OpcodeType/ObjectPool。取消只结束本地等待，不撤销接收端已执行动作。正常成功、业务错误和原 40 秒超时保留。

本地 RpcId 在当前托管域线程安全递增，跨发送器重建不归零，耗尽拒绝不回绕；超时、等待返回和消息批次核对实例身份及停止状态。无跨托管域/进程重启恢复保证，不改变 Session 的外层编号或等待表。真实生命周期 UTF 9/9 Passed，Unity/DotNet.Core 编译通过；范围见 [执行记录](../openspec/changes/archive/2026-10-11-local-message-request-shutdown/execution-notes.md)。

## 本机产品会话

实现位于 Model/Hotfix 的 Share/Product，仅 Unity !DOTNET 原型；既有 DotNet 同源编译继续保留。独立 SceneType 为 ProductRoot/Host/Client，不发布 Main 的示例 EntryEvent。根每 Fiber 一份 Timer、CoroutineLock、ProcessInnerSender；ProductBootstrapBridge 只定位根并交接初始化/关闭，不持成员名单。

```text
Fiber / Root Scene(ProductRoot)
  Components: Timer / CoroutineLock / ProcessInnerSender / ProductRuntime
  Child Scene(ProductHost，Host 或 Solo)
    Child Match → Children Participant（唯一成员权威）
      Component GridKitchen → Children GridActor / GridStation / GridItem（唯一地图权威）
    Component HostSession + Ordered MailBox
      Child HostPeer + Ordered MailBox + LocalDelivery 或 RemoteDelivery
    Component NetComponent → Child Session → ProductPeerBinding(HostPeer EntityRef，仅联网 Host)
  Child Scene(ProductClient)
    Component ClientSession + Ordered MailBox（独立值名单）
      Component GridClient（复制的地图值投影与最新命令状态）
    Host / Solo 本地: Component LocalMessage(HostPeer EntityRef)
    远端: Component NetComponent → Child Session → ProductClientBinding
    Component GridPresentation / UI / GFEntity（输入及托管 Board / HUD）
```

本地 Client Call 在入队前增加 Peer InFlight，root sender.Call(Peer.GetActorId)→队列→ProductHost Peer handler→独立 ProductControlRequest→sender.Call(HostSession.GetActorId)→共同 Ordered MailBox。身份由 Peer 的 InstanceId/Generation 填入，共同提交前再次核对入口。HostSession 只引用 Match，不持第二份名单，不 await 客户端通知。

远端 Session.Call→UDP-KCP→ProductHost 精确 OnRead Invoke；wire 仅 Join/Probe/Leave/Heartbeat/GridCommand 白名单。入口捕获 wire RpcId、Session EntityRef 和绑定代次，复制请求走同一个 Peer/HostSession 路径，再复制响应恢复 wire RpcId、只回原 Session。两份既有等待表互不代替，没有第三份私有 RPC 字典。内部 ProductControlRequest 和 GridSubmitRequest 不接受网络直达。

玩法 Peer handler 注入可信身份，Sender.Call(HostSession.GetActorId)→ProductHost 的 GridSubmitHandler 快速返回 Accepted/Rejected；GridKitchen 固定 50ms Tick 再校验并提交位置或物品。Move 和自动接近在行走／满格等待时保持 PendingApproach，到达才发最终结果；改目标取消旧动作。Solo、Host 本地和远端走相同规则，UI／插值不写权威世界。

Tick 提交后为每端生成独立 GridWorldSnapshot 与本人 GridCommandResult，经 LocalDelivery/RemoteDelivery→ProductClient OnRead（远端）→ClientSession MailBox→ProductClient 的 GridSnapshotHandler/GridResultHandler。接收者复制值后回收消息，按世界／版本／绑定拒绝旧数据，再发布 GridProjectionApplied 观察事件。Join 确认、本人地图投影和 Board/HUD 就绪共同形成 GridReady；成功 Join 本身不等于地图可操作。

名单变更先提交 Match/RosterVersion，再为每个 Peer 生成独立 DTO。本地 sender.Send(ClientSession.GetActorId)、远端 Session.Send；远端 ProductClient 精确 OnRead 将通知再次投递到同一个客户端 MailBox。Joining 候选不能确定 Match/Joined；有效本人确认后安装匹配的较新版本。消息回收前复制为 ProductMemberSnapshot 值，旧版本、旧绑定及已关闭本地入口拒绝。

产品 actor handler 消费后释放输入；actor 响应由调用者消费/释放。wire 输入/复制响应在桥接 finally 释放；Session.Send 完成序列化但不回收输入。每个远端通知发送后释放，本地通知由接收 handler 释放，不共享可写消息对象。Event 仅观察提交后事实，ProductHost/Client 明确匹配；All 标志仍会匹配新类型，不承诺 DynamicEvent 隔离。

协议版本2，Probe最大1024 UTF-8 bytes，网络请求4000ms，串行心跳间隔2000ms/预算2000ms；成功加入移除原5秒接入超时，保留2秒检查/10秒接收空闲阈值。本地不发网络心跳，Solo 不创建 NetComponent 或对自身 UDP 连接。KCP 响应 Send 后留100ms关闭宽限以允许 Update 发出；该段仍计入 InFlight，不保证送达或撤销已提交事实。

单 Peer Closing 立即关闭资格，独立可信 Close 控制请求经 HostSession 标成员离线；原 Participant 由 Match 保留。CloseFinished且入口在途归零才释放 Peer/MailBox。Leave 提交删除成员一次，允许回送成功结果后关闭自身连接；Host 本地 Leave 不停止监听/sender，也不自动重入。异常仍回退现有本地40秒超时，消息泵永久停止通过整体 Stop 结清，未增加单请求取消接口。

成员变化交给地图 Tick：原绑定角色移除，手持物保持身份与容器内容，落在 Host 最后提交格；离线 Participant 不等于角色仍留场。未提交的旧绑定动作失效，队友按同格空手规则选择具体落物，重复收尾不复制物品。

整体先 Stopping/停止地图 Tick 与意图，拒绝新工作，尽力 Send MatchClosed；本端停止展示后 sender.Stop 结束本地等待、Dispose Net/Session 结束网络等待、释放角色树。正常 OnShutdown/Init.Shutdown 再释放捕获 World/Fiber。直接树销毁有 sender/绑定兜底。异步失败先保留 Failure、在原 Fiber 排队收尾，再由 Loader 在 Runner 遍历后的 LastPostLateUpdate 停止原初始化；旧 owner/EntityRef 恢复不能写新轮。

当前本地、真实 KCP 小集及 Editor 单端结果见 [产品执行记录](../openspec/changes/archive/2026-10-11-product-session-bootstrap/execution-notes.md)。双 Editor 两种排列和未触发的资源消费者分别列明，不将 KCP loopback 当物理两机、正式传输或厨房验收。

格子样机的核心／共同入口 UTF、竖屏操作、真实 KCP 交接与断线拾物见 [地图执行说明](../openspec/changes/archive/2026-10-11-portrait-grid-kitchen-prototype/execution-notes.md)。本轮双 Editor 新玩法、真实手机 Player/AOT、LAN/公网和持久餐厅 NotRun；前厅只是地图占位。

## owner 所属等待

临时格子备料使用产品协议 3：物品投影包含 DefinitionId 与内容 ItemId，Container 位置的 HolderId 是外层容器，内容不另画成地面物品。GridManualProcess、物品及角色在同一 World／Generation／Version 快照安装，进度由 Host Tick 推进，视图不计业务时间。暂停保留进度和输入身份；输出为新物品身份，开始命令 Executed 仅表示开始成功。

输入从 GridPresentation → GridClient.CreateGridCommand → ClientSession 的所属异步值请求。本地经 LocalMessage 的可信 Peer → 根 ProcessInnerSender.Call(peer ActorId) → MessageQueue／Ordered MailBox → ProductHost GridPeerHandler；KCP 经 Session／ProductWire 复制载荷并保留外层 RpcId 后进入同一 Peer。Peer 携可信实例／代次再 Call HostSession ActorId → Ordered GridSubmitHandler → 快速答复，Tick 再核对源／目标 ItemId、ProcessId 和绑定并提交。拒绝不交换／半扣，内部 GridSubmit 不进入 wire 白名单，版本 2 明确拒绝。

提交后每端独立 Snapshot／Result，经 LocalDelivery sender.Send 或 RemoteDelivery Session.Send → ProductClient 的 ClientSession MailBox → GridSnapshotHandler／GridResultHandler；接收者复制独立值集合后释放 DTO，再发观察事件。GridWorldCommitted／GridProjectionApplied 不承担传输或请求答复。材料与 Process 归地图 Child，Match Children 保持 Participant；单成员释放工作与落物不停止共用 Sender。详见 [实际 owner 树和完整路径](../openspec/changes/archive/2026-10-11-grid-kitchen-prep-processing/design.md)。

`OwnedRequestExtensions` 位于既有 Hotfix 程序集。Session 值适配使用原 Session.Call(token)、原等待表及 Timer 超时；ERR_Cancel 转为取消，超时单独报告。适配器在响应有效期内读取独立值，finally 释放自己拿到的池化响应及 Session 输出请求；返回值不保留池化消息。Session RpcInfo 改用不复用的记录，移除等待/取消注册后再通知续体；迟到未匹配响应由 Session 收尾。

本地值适配沿原 sender.Call → ActorId → MessageQueue → Ordered MailBox → SceneType/Handler → 原答复路径。单 owner Stop 只结束其业务等待，内部唯一观察者继续等原答复/40秒超时并释放响应；不 Stop 共用 sender，也不宣称已清空物理等待。整个运行退出保留原 sender.Stop。输入由现有 sender/handler 收尾，适配器不抢收件端所有权；取消不会回滚已经提交的动作。

普通 ET 延时走 `context.DelayAsync(timer, time)`，维持原 Fiber 执行上下文；本轮 Ping 消费者已迁移，其他产品 Join/Probe/Heartbeat、wire 和未迁移旧业务保持原契约。原 AutoResetUniTaskCompletionSourcePlus 在同步续体读取结果时，延后池回收直到原完成通知/取消动作全部返回，避免丢取消注册或误触下一任务。实际四项 ET 适配测试与必要计时回归见 [执行说明](../openspec/changes/archive/2026-10-11-manage-owned-async-lifecycle/execution-notes.md)。
