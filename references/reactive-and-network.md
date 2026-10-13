# 响应式投影与网络数据约定

本页为同步来源实现约定与历史证据；main历史Passed不代表第二工作区本轮重跑，实际验收另行记录。

**非规格**；冲突以代码/表、进行中 change、已同步主规格及用户最新意图为准。**阅读 ≠ 授权**文中未做能力。

## 状态与 UI

单机餐厅当前协议14，已在协议12关卡／前厅／buff基线上接入设施清理和有限供应。玩家操作等待归当次UIGridKitchen.AsyncOwner，HUD加载等待归GridPresentation.AsyncOwner；关闭HUD取消显示等待、解绑所属按钮，原物理请求及Peer在途仍由原观察者收尾。按钮捕获原UI／GridClient EntityRef和World／Level／绑定代次；当前上下文校验先于旧关最新重放，仅已Executed且完整相同的Continue可回报自己换关事实。实际树和请求路径见[关卡设计](../openspec/changes/archive/2026-10-13-solo-restaurant-level-flow/design.md)，旧关四参数与当前关重放证据见[执行记录](../openspec/changes/archive/2026-10-13-solo-restaurant-level-flow/execution-notes.md)。后文协议3至12保留阶段边界，当前设施和供应结果见[清理记录](../openspec/changes/solo-kitchen-clear-and-discard/execution-notes.md)及[供应记录](../openspec/changes/solo-supply-delivery/execution-notes.md)。

设施请求沿原 Grid Peer／Submit／固定业务步，不新建入口；完整预期内容列表、剩余份数和布局版本从独立投影冻结，原请求回收前后均不共享可变列表。实际到位再重验和提交，结果及水／固液形态／设施字段属于同一 Version；wire 继续只接受原动作白名单，协议13不自动开放联机清理。

供应使用同一业务编号的SupplyCommandRequest/Response和可信SupplySubmitRequest/Response（10103～10106），三种SupplyAction沿原Peer和authority有序MailBox，采购在Restaurant服务步一次预留，接收／拆份复用原Grid到位互动。KitchenSupply归持续厨房，SupplyShipment归该组件；配额是外部包数，实际材料由GridItem.Contents表达。Snapshot47/48的来源配额／货物状态与实物同版独立复制，DTO回收后只读值仍有效；暂停及Completed停运输，换关保留同一货物，旧关请求／HUD按钮拒绝。协议14仍没有开放供应wire或增加监听。

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

本地 RpcId 在当前托管域线程安全递增，跨发送器重建不归零，耗尽拒绝不回绕；超时、等待返回和消息批次核对实例身份及停止状态。无跨托管域/进程重启恢复保证，不改变 Session 的外层编号或等待表。真实生命周期 UTF 9/9 Passed，Unity/DotNet.Core 编译通过；范围见 [执行记录](../openspec/changes/local-message-request-shutdown/execution-notes.md)。

## 本机产品会话

实现位于 Model/Hotfix 的 Share/Product，仅 Unity !DOTNET 原型；既有 DotNet 同源编译继续保留。独立 SceneType 为 ProductRoot/Host/Client，不发布 Main 的示例 EntryEvent。根每 Fiber 一份 Timer、CoroutineLock、ProcessInnerSender；ProductBootstrapBridge 只定位根并交接初始化/关闭，不持成员名单。

```text
Fiber / Root Scene(ProductRoot)
  Components: Timer / CoroutineLock / ProcessInnerSender / ProductRuntime
  Child Scene(ProductHost，Host 或 Solo)
    Child Match → Children Participant（唯一成员权威）
      Child RestaurantRuntime（唯一业务时钟）
        Component GridKitchen → Children GridStation / GridItem / 加工作业（持续厨房权威）
        Child Level → GridActor / Customer / Order / FrontWork / Growth / Buff（当前关状态）
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

远端Session.Call→UDP-KCP→ProductHost精确OnRead Invoke；协议17显式开放Join/Probe/Leave/Heartbeat、核心Grid动作1–17、Level/Layout/Buff、ServeOrder/FrontWork及SupplyCommand请求。入口捕获wire RpcId、Session EntityRef和绑定代次，复制请求走同一个Peer/HostSession路径，再复制响应恢复wire RpcId、只回原Session。两份既有等待表互不代替，没有第三份私有RPC字典。所有内部Submit／伙伴消息均不接受网络直达，未知动作仍显式拒绝。

玩法 Peer handler 注入可信身份，Sender.Call(HostSession.GetActorId)→ProductHost 的 GridSubmitHandler 快速返回 Accepted/Rejected；GridKitchen 固定 50ms Tick 再校验并提交位置或物品。Move 和自动接近在行走／满格等待时保持 PendingApproach，到达才发最终结果；改目标取消旧动作。Solo、Host 本地和远端走相同规则，UI／插值不写权威世界。

Tick 提交后为每端生成独立 GridWorldSnapshot 与本人 GridCommandResult，经 LocalDelivery/RemoteDelivery→ProductClient OnRead（远端）→ClientSession MailBox→ProductClient 的 GridSnapshotHandler/GridResultHandler。接收者复制值后回收消息，按世界／版本／绑定拒绝旧数据，再发布 GridProjectionApplied 观察事件。Join 确认、本人地图投影和 Board/HUD 就绪共同形成 GridReady；成功 Join 本身不等于地图可操作。

名单变更先提交 Match/RosterVersion，再为每个 Peer 生成独立 DTO。本地 sender.Send(ClientSession.GetActorId)、远端 Session.Send；远端 ProductClient 精确 OnRead 将通知再次投递到同一个客户端 MailBox。Joining 候选不能确定 Match/Joined；有效本人确认后安装匹配的较新版本。消息回收前复制为 ProductMemberSnapshot 值，旧版本、旧绑定及已关闭本地入口拒绝。

产品 actor handler 消费后释放输入；actor 响应由调用者消费/释放。wire 输入/复制响应在桥接 finally 释放；Session.Send 完成序列化但不回收输入。每个远端通知发送后释放，本地通知由接收 handler 释放，不共享可写消息对象。Event 仅观察提交后事实，ProductHost/Client 明确匹配；All 标志仍会匹配新类型，不承诺 DynamicEvent 隔离。

当前协议版本17（历史版本3为备料、4为本地命令、5为汤制作、6为关卡核心、7为工位布局；17追加Host可信控制资格），Probe 最大 1024 UTF-8 bytes，网络请求 4000ms，串行心跳间隔 2000ms／预算 2000ms；成功加入移除原 5 秒接入超时，保留 2 秒检查／10 秒接收空闲阈值。本地不发网络心跳，Solo 不创建 NetComponent 或对自身 UDP 连接。KCP 响应 Send 后留 100ms 关闭宽限以允许 Update 发出；该段仍计入 InFlight，不保证送达或撤销已提交事实。核心Grid1–12及Level／Layout／Buff已开放wire；共享控制须为当前Host本地可信绑定，前厅ServeOrder／FrontWork和物流SupplyCommand／Grid13–17均为合法成员个人操作，不因共享控制而加HostOnly。

单 Peer Closing 立即关闭资格，独立可信 Close 控制请求经 HostSession 标成员离线；原 Participant 由 Match 保留。CloseFinished且入口在途归零才释放 Peer/MailBox。Leave 提交删除成员一次，允许回送成功结果后关闭自身连接；Host 本地 Leave 不停止监听/sender，也不自动重入。异常仍回退现有本地40秒超时，消息泵永久停止通过整体 Stop 结清，未增加单请求取消接口。

成员变化交给地图 Tick：原绑定角色移除，手持物保持身份与容器内容，落在 Host 最后提交格；离线 Participant 不等于角色仍留场。未提交的旧绑定动作失效，队友按同格空手规则选择具体落物，重复收尾不复制物品。

整体先 Stopping/停止地图 Tick 与意图，拒绝新工作，尽力 Send MatchClosed；本端停止展示后 sender.Stop 结束本地等待、Dispose Net/Session 结束网络等待、释放角色树。正常 OnShutdown/Init.Shutdown 再释放捕获 World/Fiber。直接树销毁有 sender/绑定兜底。异步失败先保留 Failure、在原 Fiber 排队收尾，再由 Loader 在 Runner 遍历后的 LastPostLateUpdate 停止原初始化；旧 owner/EntityRef 恢复不能写新轮。

当前本地、真实 KCP 小集及 Editor 单端结果见 [产品执行记录](../openspec/changes/product-session-bootstrap/execution-notes.md)。双 Editor 两种排列和未触发的资源消费者分别列明，不将 KCP loopback 当物理两机、正式传输或厨房验收。

Host／Solo当前都持有持续Restaurant／厨房和当前Level；共享控制只来自权威在线Participant／Peer绑定及runtime.Client.Local的当前真实Peer引用。首个快照可以早于Join答复，复制的CanControlRestaurant不替代客户端Join／当前角色／受管视图就绪条件。核心7/7、阶段28个核心UTF及普通Host＋独立Fiber真实KCP受管两菜已验收，见[核心记录](../openspec/changes/cooperative-restaurant-core/execution-notes.md)；前厅5/5、19项UTF及普通入口两菜交付／原餐具循环／自然两关已验收，见[前厅记录](../openspec/changes/cooperative-front-service/execution-notes.md)；物流5/5已有15项真实KCP／受管UI及普通补货制作交付／剩液空壳清理／同一在途货物跨关证据，见[物流记录](../openspec/changes/cooperative-cleanup-supply/execution-notes.md)。当前未知动作0／99两项与受影响核心UI已新运行通过；普通双Editor完整合作3/3已通过，见[集成记录](../openspec/changes/cooperative-restaurant-loopback/execution-notes.md)。

早期格子样机的核心／共同入口UTF、竖屏操作、真实KCP交接与断线拾物见[地图执行说明](../openspec/changes/archive/2026-10-11-portrait-grid-kitchen-prototype/execution-notes.md)。当前普通双Editor完整餐厅两种角色排列已验收3/3，前厅真实交付／原餐具循环、有限供应／清理、自然换关、真实Client失联后Host收货与退出新世界隔离通过，见[集成记录](../openspec/changes/cooperative-restaurant-loopback/execution-notes.md)。真实手机Player/AOT、LAN／公网、联机装修／持久餐厅与原成员恢复仍未交付。

## owner 所属等待

临时格子备料使用产品协议 3：物品投影包含 DefinitionId 与内容 ItemId，Container 位置的 HolderId 是外层容器，内容不另画成地面物品。GridManualProcess、物品及角色在同一 World／Generation／Version 快照安装，进度由 Host Tick 推进，视图不计业务时间。暂停保留进度和输入身份；输出为新物品身份，开始命令 Executed 仅表示开始成功。

输入从 GridPresentation → GridClient.CreateGridCommand → ClientSession 的所属异步值请求。本地经 LocalMessage 的可信 Peer → 根 ProcessInnerSender.Call(peer ActorId) → MessageQueue／Ordered MailBox → ProductHost GridPeerHandler；KCP 经 Session／ProductWire 复制载荷并保留外层 RpcId 后进入同一 Peer。Peer 携可信实例／代次再 Call HostSession ActorId → Ordered GridSubmitHandler → 快速答复，Tick 再核对源／目标 ItemId、ProcessId 和绑定并提交。拒绝不交换／半扣，内部 GridSubmit 不进入 wire 白名单，版本 2 明确拒绝。

提交后每端独立 Snapshot／Result，经 LocalDelivery sender.Send 或 RemoteDelivery Session.Send → ProductClient 的 ClientSession MailBox → GridSnapshotHandler／GridResultHandler；接收者复制独立值集合后释放 DTO，再发观察事件。GridWorldCommitted／GridProjectionApplied 不承担传输或请求答复。材料与 Process 归地图 Child，Match Children 保持 Participant；单成员释放工作与落物不停止共用 Sender。详见 [实际 owner 树和完整路径](../openspec/changes/archive/2026-10-11-grid-kitchen-prep-processing/design.md)。

`OwnedRequestExtensions` 位于既有 Hotfix 程序集。Session 值适配使用原 Session.Call(token)、原等待表及 Timer 超时；ERR_Cancel 转为取消，超时单独报告。适配器在响应有效期内读取独立值，finally 释放自己拿到的池化响应及 Session 输出请求；返回值不保留池化消息。Session RpcInfo 改用不复用的记录，移除等待/取消注册后再通知续体；迟到未匹配响应由 Session 收尾。

本地值适配沿原 sender.Call → ActorId → MessageQueue → Ordered MailBox → SceneType/Handler → 原答复路径。单 owner Stop 只结束其业务等待，内部唯一观察者继续等原答复／40 秒超时并释放响应；不 Stop 共用 sender，也不宣称已清空物理等待。`CallLocalValueAsync` 的可选 `physicalFinished` 只在原调用观察者 finally 执行，产品 Peer 在该处 EndEntry，不能在被取消的外层等待 finally 提前减计数。入队前 owner 已停止或 sender 已停止的同步失败由适配边界释放未交接输入和责任；入队后输入仍由 handler 收尾。整个运行退出保留原 sender.Stop，取消不会回滚已提交动作。

产品业务编号由 ClientSession 的 `CreateProductCommandContext` 统一分配；上下文固定 Match／World／可选 Level／绑定与编号。HostPeer 只保留最新命令的类型、完整动作及结果元数据；GridActor 的编号仅作原投影兼容值。相同最新载荷重放返回接收或已提交事实，不再排队；同编号冲突为 CommandConflict，更早编号为 ExpiredCommand。旧 Host/Client 样机无 Level，正式 Solo 校验当前关身份及代次；不建立字符串 CommandBus 或历史结果库。基线实证见 [本地消息 change](../openspec/changes/archive/2026-10-13-solo-local-message-contracts/execution-notes.md)，迁移回归见 [关卡流程](../openspec/changes/archive/2026-10-13-solo-restaurant-level-flow/execution-notes.md)。

普通 ET 延时走 `context.DelayAsync(timer, time)`，维持原 Fiber 执行上下文；本轮 Ping 消费者已迁移，其他产品 Join/Probe/Heartbeat、wire 和未迁移旧业务保持原契约。原 AutoResetUniTaskCompletionSourcePlus 在同步续体读取结果时，延后池回收直到原完成通知/取消动作全部返回，避免丢取消注册或误触下一任务。实际四项 ET 适配测试与必要计时回归见 [执行说明](../openspec/changes/archive/2026-10-11-manage-owned-async-lifecycle/execution-notes.md)。

### 单机汤批次（协议 5）

UI／Board 共用 `CounterIntent`，冻结碗、锅和批次身份后经原 ClientSession→LocalMessage.Peer ActorId→根 ProcessInnerSender→Peer Ordered MailBox／GridPeerHandler→HostSession Ordered MailBox／GridSubmitHandler。入口回执后由 Grid Tick 再验并提交；开始成功不等待煮完。`GridAutoProcess` 归厨房，锅和原灶台只保存引用，搬锅或单成员离开不结束作业。

提交后 `GridBroadcast` 为每个 Peer 独立建立内容图、`GridAutoProcessState` 与本人结果，经 LocalDelivery→ClientSession MailBox→GridSnapshotHandler／GridResultHandler；Client 复制输入列表、进度及份数后回收 DTO，再发布本端投影事件。BatchId 使用 ProcessId，最后一份同时生成汤、消费原输入并复位同一锅；旧批次不能跨到新批次扣份。新增三个动作只走本地，wire 明确 `SoloCommandOnly` 拒绝。最新本地运行证据见 [煮汤执行说明](../openspec/changes/archive/2026-10-13-grid-kitchen-soup-batch-cooking/execution-notes.md)。

### 正式 Solo 关卡（交付基线协议 6）

厨房由 Match 的 RestaurantRuntime Child 以 Component 持有，角色归餐厅当前 Level Child，Match 的成员枚举按 Participant 类型过滤。LevelCommandRequest 经 Peer Ordered MailBox／LevelPeerHandler，可信 LevelSubmitRequest 再到 HostSession Ordered MailBox／LevelSubmitHandler；服务步独占阶段写入，暂停时仍处理控制／成员清理，业务钟冻结。关卡与厨房字段、Peer 最新命令事实通过原独立 GridWorldSnapshot 同版安装，DTO 回收后仅保留值；旧关高版本快照及结果仍拒绝。实际树、生命周期与运行范围见 [关卡执行说明](../openspec/changes/archive/2026-10-13-solo-restaurant-level-flow/execution-notes.md)。

### 准备态布局（交付基线协议 7）

StationLayoutRequest 经原 ClientSession 所属值等待、LocalMessage 的 HostPeer ActorId、Root ProcessInnerSender.Call，进入 Peer Ordered MailBox／LayoutPeerHandler；带可信实例的 StationLayoutSubmitRequest 进入 HostSession ActorId／Ordered MailBox／LayoutSubmitHandler，再由餐厅服务步调用厨房布局队列提交。当前关代次、绑定、共享业务编号和布局版本均核对，最新完整请求重放只返回既有事实。恢复入口不依赖停止的 BusinessTick。

布局、设备占地／操作格及最终结果仍由原 GridWorldSnapshot 以同版独立值安装，DTO 与等待沿原 handler／所属观察者收尾；新增四个类型只追加协议末尾（10078～10081），不进入 wire 白名单。UI 选择只保留工位 ID；真实位置、内容和进度由投影读取。运行及未验证的前厅路径见 [工位执行说明](../openspec/changes/archive/2026-10-13-solo-workstation-layout/execution-notes.md)。

### 两菜制作与菜单（协议 8）

Grid 的 StartBake 经原 Peer／HostSession 两段 Ordered MailBox handler 接收，在唯一厨房 Tick 启动作业；GridBakeProcess 为持续厨房 Child，原 Station／输入 Item 只存身份关系，RequiredTicks 冻结，完成一次消费输入并创建新身份。汤仍是批次，烤面包是整物，盘装沿原 AddToContainer。菜品按实际内容和指定容器识别，不因完成加工或普通台面暂存开单／加分。

同版独立 GridWorldSnapshot 增菜单与 BakeProcess 值，原 client handler 复制值后回收 DTO；世界停止由原 owner 清理，Completed 停钟而下一关 Preparing 接续同一加工。新增状态类型在 ZZToastBake.proto 排序末尾（10082），原编号不改；协议 8 仍保持新动作本地边界。实际 19 项 UTF、受影响的45项回归、普通两菜入口和 NotRun 见 [菜品执行说明](../openspec/changes/archive/2026-10-13-solo-dish-recipe-chain/execution-notes.md)。


### 前厅工作请求与 Tick（协议 9）

FrontWorkRequest 由 ClientSession 使用共享业务编号创建，经 LocalMessage 捕获原 Peer ActorId、Root ProcessInnerSender.Call、Peer Ordered MailBox／FrontWorkPeerHandler，转换为带可信 PeerId／InstanceId／Generation 的 FrontWorkSubmitRequest，再到 HostSession Ordered MailBox／FrontWorkSubmitHandler。handler 只核对并入队，快速回执后由 Restaurant 唯一业务步裁决；ClaimSerial 与目标身份保护旧停手不能释放新工作者。

CompanionRole 通过自身 AsyncOwnerComponent 的 RequestCompanionWork 发送一项在途物理 Call；创建请求时捕获当前 RoleId／InstanceId、Level、世界和 authority ActorId。HostSession 的 CompanionWorkHandler 解析当前关真实角色；客户端 Peer 入口收到这些内部请求只返回 InternalOnly，不转交为 NPC。角色退出取消所属值等待，物理观察者仍等原答复并 Dispose 响应，finally 只通过原 EntityRef 收尾；不 Stop 公共 sender 或建立新的等待表。

业务步先处理停手，再推进到位的已有工作和完成目标效果，最后处理新认领；完成第三项先计成长，后续同 Tick 领取洗碗可冻结 ceil 加速。CompanionWorkResult 经 root sender.Send(Role ActorId) 到同一 ProductHost SceneType 的 Role Ordered MailBox／CompanionWorkResultHandler，核对角色实例、关代次和请求；通知只写观察元数据，不再次推进工作或成长。旧通知同样 finally Dispose。

本阶段追加 10083–10089，保留旧 Opcode；没有 wire 白名单扩展或网络监听。玩家最终命令事实沿既有独立 GridWorldSnapshot 返回，Role／FrontWork 的客户端值投影与 Board 将在伙伴调度任务接入；不能由后台 UTF 代替实际自动服务与自然收尾。当前证据见 [伙伴执行说明](../openspec/changes/archive/2026-10-13-solo-fixed-companion-work/execution-notes.md)。


### 自动前厅与同版投影（协议 10）

餐厅同一服务步推动顾客、工作和空闲伙伴调度，所有路线使用 Kitchen 的同一 Blocked／FindGridPath。自动伙伴仍经自身 owner、原 root sender、HostSession 有序 mailbox 到同一工作引擎；不直接提交完成效果。默认首客 20 Tick、间隔 60，满队列跳过，独立 Order 只在询问完成后开出。

原 GridWorldSnapshot 扩展前厅值，新增 DTO 仅追加 10090–10094。GridFrontBroadcast 每 Peer 独立创建角色／工作／顾客／订单／桌／队列及 Hygiene／池列表；GridClientSystem 在发布 GridProjectionApplied 前完整复制安装，Board 标签、路径和基础前厅按钮读同一版本。Scope、mailbox、输入／答复和 DTO 收尾不变；未增加 wire 白名单或监听。运行边界与相机渲染见 [前厅记录](../openspec/changes/archive/2026-10-13-solo-customer-order-service/execution-notes.md)。

### 真实出餐与原餐具（协议 11）

ZZZZServe.proto 追加 10095–10098，经正常 Proto2CS 生成。ServeOrderRequest 从 ClientSession 所属等待到 Peer Ordered MailBox／ServePeerHandler，再由可信 ServeOrderSubmitRequest 到 authority ActorId／HostSession MailBox／ServeSubmitHandler；内部消息不能直接作用 Peer。快答 Accepted 只保存当前 Level 的普通值队列，统一 Tick 完整核对身份、位置、卫生和菜品后一次消费内容／计分，原容器留桌。暂停取消尚未执行 Serve，恢复不重放。两段输入和响应仍由原 handler／物理观察者收尾；最终事实沿原同版 Snapshot，Event 只观察。CleanPool 取回固定原 ItemId，不创建新餐具；没有新增网络入口或白名单。行为和真实入口证据见 [服务记录](../openspec/changes/archive/2026-10-13-solo-customer-order-service/execution-notes.md)。

### 本关一次加成选择（协议12）

ChooseBuffRequest／Response／SubmitRequest／SubmitResponse 追加10099–10102，经原 Peer／authority 有序 MailBox 和所属 Call 接受，统一 Restaurant 步提交。先校验当前世界／关／绑定，再返回最新重放事实，另一 Peer 换关后旧成功命令也明确拒绝。Snapshot42–46复制本关Seed／DrawVersion／候选／已选／门槛；每Peer独立列表，回收 DTO 不改已安装值。LevelBuff 是本关 Component，换关重建，厨房旧作业保持冻结耗时；暂停取消尚未执行选择，没有新监听／白名单。实际入口与有限验证见 [加成记录](../openspec/changes/archive/2026-10-13-solo-random-level-buffs/execution-notes.md)。
