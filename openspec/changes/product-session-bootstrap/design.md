# Design

用户审核入口见 [提案开头的快速审核](proposal.md)。本文保留实施所需的 ET 归属、消息与生命周期细节；技术一致性由 Agent 核对，不要求用户逐字段审核。影响行为的新取舍需要单独说明，不能以已有范围确认代替新决定。

## Context

目标见 [proposal.md](proposal.md)，契约见 [product-session](specs/product-session/spec.md) 与 [startup](specs/startup/spec.md)。本轮为规划；以下新类型和消息名是实施候选，不代表代码已存在。

已核对的底座事实：

- ET.Entry 注册消息／网络单例、加载配置，再创建 SceneType.Main；FiberInit_Main 发布三个 EntryEvent，示例按 Main 类型启动。产品入口需复用公共注册与装载，避开该示例初始化。
- MailBoxComponent 注册在所属 Fiber，以接收实体 InstanceId 定位；MessageDispatcher 还按实体所属 SceneType 筛选。只有同一 Fiber 的 FromAddress 无法区分多个成员，不能将它作为成员凭证。
- Ordered MailBox 通过根 CoroutineLock 串行处理，锁键为接收实体 InstanceId。每个 Peer 各有锁不等于 HostSession 的名单修改已串行。
- NetComponent 基于 KService；接收事件 Invoke 使用所属 SceneType 精确键。OnAccept 添加 5 秒验证超时与空闲检查，成功加入后必须解除前者。旧 Demo Ping 有自己的协议、时钟与 Gate handler，不能直接挂上当产品心跳。
- ProcessInnerSender.Call 改写请求 RpcId；Session.Call 有另一套编号和等待表。现有 actor RPC Handler 在执行前捕获编号，随后经 root sender 答复；桥接必须用独立请求／响应对象保留外层编号。
- 当前 ProcessInnerSender.Call 仅有 needException 参数，没有按 Peer 或 token 取消的接口，运行中超时为 40 秒。Session.Call 支持 token 或显式 time；两者不能被写成相同的即时取消能力。
- PublishAsync 记录处理器异常后返回，不能承担必须成功的启动裁决；普通 Event 使用标志匹配，Invoke 使用精确键。DynamicEvent 注册表在根，不能假定新增子 SceneType 就自动隔离接收者。
- Init 的 Runner 在 CodeLoader.StartAsync 返回后安装，Runner 才驱动 Fiber 更新；产品启动不能在装载返回前等待依赖队列更新的加入，否则阻塞消息泵。Runner.OnDestroy 先 Invoke OnShutdown，再 World.Dispose；既有 HotfixView 的 OnShutdown handler 为空但已占用该键。
- UDP socket 支持端口 0，但 IKcpTransport 文件中的 UdpTransport 没有公开 LocalEndPoint。报告动态监听需补最小只读接口，不能只回报输入。
- CodeMode 工具可禁用 Server 目录；产品基础数据和逻辑放 Share，协议使用 ET-Client 的 Client／ClientServer 输出，不能把 Unity Host 能力只放被禁用的 Server 示例目录。

## Goals / Non-Goals

**Goals:** 清楚的 ET owner 树与双向消息路径；同进程本地投递、远端直连与共同有序裁决；真实阶段状态和可验证收尾。

**Non-Goals:** 厨房玩法、持久进度、账户认证、正式传输选择、自动重连／更新基线、跨域恢复、公开互联网服务或新的消息框架。成员编号属于本轮 Match，不等于账号或存档 owner；此轮不签发重连凭证，重新建立连接的新 Join 不承诺恢复旧成员。

## Decisions

### 1. 一个 Main 调度 Fiber，独立产品 SceneType

增加互不重叠的 ProductRoot、ProductHost、ProductClient 标志，使用 long 位移并核对现有分配。Main 调度器不等于 SceneType.Main；产品根只用 ProductRoot，不拼入 Main，避免匹配 Demo EntryEvent。

Host 进程的拟定 owner 树：

```text
World / FiberManager
  Fiber（SchedulerType.Main，沿用适用主 Fiber 标识）
    Root Scene: ProductRoot
      Component: TimerComponent
      Component: CoroutineLockComponent
      Component: ProcessInnerSender（本 Fiber 唯一）
      Component: ProductRuntimeComponent（本轮生命周期与阶段结果）
      Child Scene: ProductHost
        Child: Match（唯一逻辑名单、MatchId、名单版本）
          Child: Participant（成员身份；可处于离线状态）
        Component: HostSessionComponent（引用 Match；唯一有序控制入口）
          Component: Ordered MailBox
          Child: HostPeer（可替换连接入口；引用 ParticipantId／绑定代次）
            Component: Ordered MailBox
            Component: LocalDelivery 或 RemoteDelivery
        Component: NetComponent（只服务远端）
          Child: Session
            Component: ProductPeerBinding（对应 HostPeer 的 EntityRef）
            Component: 既有接入超时／空闲检查
      Child Scene: ProductClient
        Component: ClientSessionComponent（本人成员与独立名单投影）
          Component: MailBox
        Component: LocalMessageComponent（HostPeer 与通知目标引用）
```

远端 Client 进程：

```text
World / FiberManager
  Fiber（SchedulerType.Main）
    Root Scene: ProductRoot
      Component: Timer / CoroutineLock / ProcessInnerSender
      Component: ProductRuntimeComponent
      Child Scene: ProductClient
        Component: ClientSessionComponent + MailBox
        Component: NetComponent
          Child: Session
            Component: ProductClientBinding
            Component: 产品心跳／既有空闲检查
```

这里 HostSessionComponent 和 ClientSessionComponent 是组件实体，可持有子实体／Mailbox；前者持连接索引和 Match 引用，后者持客户端投影，逻辑成员状态仅归 Match，不另建 MonoBehaviour 副本。ActorId 用实际接收实体 GetActorId，包含当前 InstanceId，不能只用 FiberId 的默认地址指向子实体。引用跨 await 使用 EntityRef，客户端与 Host 的 Scene 所属关系保持准确。

Match 的唯一成员状态由 HostSession 有序入口裁决，HostSession 不再维护第二份可写名单。Participant 的释放父对象是 Match；HostPeer／Session 只持绑定引用，关闭连接只使成员离线。本项不创建 LoadedSave、RestaurantRuntime 或 Level；未来这些业务的树与固定 Tick 见 [业务目标设计](../../../Docs/Product/architecture.md)，Mailbox 有序控制不等于玩法时钟。

拟定数据归属如下。字段名可在实施时对齐现有 ET 命名，但归属与含义不得交换：

| owner | 最小数据 | 唯一写入入口与释放边界 |
| --- | --- | --- |
| ProductRuntimeComponent | 本轮不可变配置、阶段、停止标记及角色子场景引用 | 根编排报告阶段；整体停止负责释放本轮，不能由单 Session 触发 |
| Match | MatchId、RosterVersion、Participant 子实体 | 仅 HostSession 的有序控制处理创建／移除成员或修改在线状态；Host 结束解散 |
| Participant | Match 内 MemberId、在线状态、当前有效绑定引用 | 同一有序入口修改；不存账号认证、Profile 或存档 Owner；断线保留 |
| HostSessionComponent | Match 的 EntityRef、Peer 索引 | 索引只是路由关系，不复制成员数据；持有 Peer 生命周期 |
| HostPeer | 绑定代次、入口开放标记、ParticipantId、投递引用、在途计数 | 传输入口可立即关闭入口标记；逻辑成员变更仍经共同裁决；计数只跟踪接入与释放，不另建等待表 |
| ProductPeerBinding | HostPeer EntityRef、原 Session 实例身份及捕获代次 | Session 释放时通知原入口失效；不能将其关联 Participant 作为子对象释放 |
| ClientSessionComponent | 本轮阶段、MatchId、MemberId、绑定代次、RosterVersion、独立名单 DTO | 共同客户端接收处理应用数据；不持 Host 实体或池化消息引用 |

MemberId 在本 Match 内不复用；绑定代次只用于隔离旧入口，不是重连凭证或跨进程身份。初始未加入 Peer 没有成员号，实际 Join 提交后才建立关联。关闭标记一旦设置不恢复，后续新连接使用新 Peer 与新代次；原型不将新连接视为原玩家恢复。

### 2. 启动及关闭入口

ProcedureET 的可用能力检查由本项接入后解除真实产品路径的拒绝，仍使用 ET.Init／CodeLoader；缺适用代码继续 ProductBootstrapUnavailable。稳定层持不可变配置，Loader 只桥接配置和阶段报告；Model／Hotfix 通过原有方向引用，不让 Game 引用 Hotfix。必要时显式引用既有程序集，不创建新业务程序集。

Entry 共用必需单例／CodeTypes／配置装载后按本轮配置创建 Main 或 ProductRoot。产品根由 `[Invoke(ProductRoot)]` 的 FiberInit 创建公共组件，再显式 StartHost／StartClient 子场景；不等待子场景自动收到 FiberInit，也不复用 Main 的三个示例事件。

装载完成后允许 Runner 开始驱动 Fiber，再由本轮产品异步编排报告初始化、HostListening、本地 Joined 或远端 Joined。HostReady 要同时具备监听和本地 Joined；启动配置的 Prepared 保留其原含义。编排捕获失败并向本轮 owner／稳定上下文报告，不能让 Forget 的日志替代最终结果。

关闭沿已有 OnShutdown handler 分流到产品停止 Invoke，不再注册同参数同键 handler；该同步停止在 World.Dispose 前执行，不等待任何网络确认。核对 Init 在 Runner 尚未建立时失败／被关闭的路径，已创建 World／产品资源也须清理，旧异步初始化不得创建新世界或新一轮。

启动顺序为：取得配置 → 完成既有装载与注册 → 创建产品根和公共组件 → 安装／允许 Runner 泵消息 → Host 创建 Match／监听并发起本地 Join，或 Client 创建连接并发起远端 Join → 校验本人确认和名单 → 报告 Ready。Listen 与 Join 可分别报告；不使用对象已创建、Session.Create 返回或分支名称证明连接与业务已就绪。任一必需阶段失败先记录真实原因，再进入同一停止路径；已经 Stopping 的本轮不会被后续回调改成 Ready。

### 3. 完整发送、接收与答复路径

| 情形 | 发送与接收路径 | 身份／结果归属 |
| --- | --- | --- |
| Host 本地 Join／Probe／Leave | ClientSession → LocalMessageComponent → root ProcessInnerSender.Call(HostPeer.GetActorId) → MessageQueue → sender.Update → Peer MailBox／ProductHost handler | LocalMessageComponent 由本轮 owner 持有可信 Peer 引用；从请求自报成员号不取得权限 |
| 远端同类请求 | ClientSession → Session.Call → KCP → Host ProductHost 的 NetComponentOnRead Invoke → 产品 wire 入口 → sender.Call(绑定 Peer 的 ActorId) → 相同 Peer MailBox／handler | 外层 Session 和内层 Actor 分别使用原有等待表；接收端捕获原 wire RpcId |
| 共用权威裁决 | Peer handler 从绑定提取当前身份，复制为内部命令 → sender.Call(HostSession.GetActorId) → HostSession Ordered MailBox → 共用 Join／Probe／Leave handler | 所有名单更改在这一入口提交；内部身份由本地可信代码填入，网络不接受内部命令 |
| actor 答复 | HostSession／Peer RPC handler → root sender.Reply(FromAddress) → 队列 → sender 匹配内部等待 → 原调用继续 | 各层 request／response 对象独立；Peer handler 不拿网络编号当内部编号 |
| 远端答复 | 产品 wire 入口取得内部结果 → 构造独立 wire response 并恢复原 wire RpcId → 原 Session.Send → Client OnRead → Session.OnResponse | 只回原仍有效 Session；失效绑定不得把结果交给重连后的 Session |
| 名单通知 | HostSession 提交后生成快照 → 逐 Peer 投递；本地 sender.Send(ClientSession ActorId)，远端原 Session.Send | 每个接收者持独立消息；通知不等待客户端回信 |
| 远端通知落地 | ProductClient 的 OnRead 核对连接与允许类型 → sender.Send(本轮 ClientSession ActorId) → Client MailBox／共同接收 handler | 与本地通知使用同一客户端状态应用逻辑 |

本地与远端只在入口／投递组件不同，Peer 和 HostSession handler 是相同代码。HostSession 处理在有序入口内完成核对和提交，不等待下级 Peer／Client 的 RPC；不能持有名单锁又等待通知接收者，造成回环死锁。

远端未加入时只接受 Join／必要控制请求；加入后只接受已声明的 Probe、Leave、心跳。严格白名单拒绝内部命令、任意 ActorId 和未绑定成员请求。格式／解码失败走本 Session 错误路径，不遗留验证成功标记。

所有转交请求携带由入口捕获的 Peer EntityRef／绑定代次。Peer 接收时核对一次，共同裁决在修改 Match 前再核对一次，不能以入队时尚在线为由批准已关闭入口的晚到请求。关闭入口的资源清理通知使用独立可信内部消息，不经过已被关闭的 Peer 请求资格；重复通知不重复修改名单。Session Destroy 不直接写 Match，也不等待 RPC。

### 4. 最小协议与消息所有权

候选消息包括外层 Join／Probe／Leave 请求响应、RosterSnapshot 与 Heartbeat，以及 Peer 向 HostSession 转交的内部请求／响应；均用既有生成入口和请求接口。Probe 为无玩法回显，答复含实际裁决成员号及请求载荷，用于验证两端经过同一入口，不新增厨房状态。

修改启用 Proto 源并生成 Client／ClientServer 输出，内部消息不列入网络入口白名单；先核对文件排序和 Opcode 编号，避免重排既有消息。稳定端按明确维护流程纳入必要版本，记录可读协议版本；不能用分支名、ProductName 或提交身份替代 wire 兼容版本。

| 消息语义 | 必需数据 | 接收与结果 |
| --- | --- | --- |
| Join 请求／响应 | 请求：协议版本；成功响应：MatchId、MemberId、绑定代次、完整 Roster 与版本 | 可信 Peer → HostSession 有序裁决；同绑定重复加入不另建成员，响应 RpcId 仅作该段关联 |
| Probe 请求／响应 | 请求：当前 MatchId、客户端探测序号、有限载荷；响应：原序号、载荷、可信裁决成员号 | 与 Join 相同共同路径；核对当前有效绑定，探测不引入厨房状态或世界 Tick |
| Leave 请求／响应 | 当前 MatchId；结果明确已提交、拒绝或失败 | 明确退出协作；答复构造后入口进入关闭流程，资源清理不改变已经提交的事实 |
| RosterSnapshot 通知 | MatchId、RosterVersion、每成员 MemberId／在线状态 | 从 Host 提交事实生成；每接收者独立 DTO，客户端以同一处理安装 |
| Heartbeat 请求／响应 | 产品控制协议版本、当前连接关联 | 在连接适配层维护存活，不新增逻辑成员，不改世界时钟；每连接最多一个在途请求 |
| MatchClosed 通知 | 当前 MatchId、正常停止原因 | 尽力通知；客户端先核对来源再结束本轮，不把普通网络失联解释成业务失败 |
| 内部转交／入口关闭消息 | 可信 Peer 实例、代次、必要载荷或关闭原因 | 只接受本地可信生成，关闭通知可在入口失效后做资源收尾；不得出现在 wire 白名单 |

wire 的合法版本、类型、MatchId、必填字段和载荷长度必须在入口验证，载荷限制与时间预算属于本原型配置并在实施时记录，不将它们写成最终人数、玩法容量或 Tick 频率。来自请求的 MemberId、ActorId、Peer 引用不能成为身份来源。失败分别使用现有 Error／Message 及明确产品错误语义，不将 ERR_Cancel、网络超时或收到了 IResponse 解释为成功。

| 对象 | 所有权规则 |
| --- | --- |
| wire 解码请求 | 入口复制转交载荷；现有发送／Session handler 约定下由明确入口 finally 释放一次 |
| 内部请求 | 与外层对象独立；接收 handler 在消费后按生成／现有 Call 约定释放，不在接收前回池 |
| actor 响应 | 调用者消费、复制需要的数据后释放；桥接不得把同一个响应直接改编号再交给别的等待者 |
| 多人通知 | 每个 Peer 一份消息和独立快照；本地接收者消费后释放，远端发送按序列化完成后的既有 Send 约定释放 |
| 客户端状态 | 深复制成员数据与容器，不持有池化响应、通知或 Host 实体引用 |

实际释放行为按现有 Session.Send／actor handler 和生成物核对，不能仅按上述位置猜测双重释放。测试要回收并复用消息后再次读取投影，证明所有权正确。

### 5. 加入、快照和事件

入口建立本轮临时 HostPeer 及绑定，Join 携协议版本进入共用处理；HostSession 验证仍存活、协议一致及未停止后，在本轮 Match 下分配 Participant／MemberId，生成 MatchId／RosterVersion／完整成员 DTO，其中包含在线／离线状态。协议原 SessionId 字段若保留必须明确表示 MatchId，不用物理连接身份代替。重复绑定 Join 不新增成员；Probe 与 Leave 只接受当前有效绑定。断线再连接在本原型只能作为新加入，不能凭请求自报 MemberId 接管离线成员；原身份恢复协议另案设计。

客户端阶段为 Connecting、Joining、Joined、Failed、Stopping、Stopped；所属会话标识、成员号和名单属于 ClientSession。Join 成功答复与通知都调用同一快照应用逻辑：会话匹配，版本不回退，深复制后再通知。通知先到可暂存，但 Joined 必须由有效的本人 Join 成功答复触发。

Join 之前若尚不知道 MatchId，只在当前入口实例下暂存一份独立候选快照；成功答复确认 MatchId 和本人成员后才安装匹配且较新的数据。其他 Match 的暂存数据丢弃，不允许未确认通知先锁定当前 Match。进入 Joined 必须已安装有效本人确认和名单，客户端停用旧实例后不能靠迟到回复恢复。

Join 等待中退出先失效绑定和临时 Peer，使未执行命令不能再登记；若登记已经提交，由 HostSession 有序入口将成员离线，不能因答复未收到推断加入未执行并删除身份。拒绝和提交前失败不留登记；提交后的离线 Participant 由 Match 明确持有，不是孤儿，未来身份恢复另案处理。

普通 Event 只发布在明确的 ProductHost／ProductClient 场景，例如 MemberChanged、ClientJoined／Left；发布前状态已提交，事件消费者只观察，不形成第二份名单。审查相同事件类型的 All handler。Invoke 仅用于初始化、网络读及关闭等既有要求的精确入口；不把可直接调用的共用 System 方法全面改成 Invoke。本项不改 DynamicEvent 的根级广播机制。

| 接收边界 | 适用 SceneType | 语义与约束 |
| --- | --- | --- |
| Fiber 初始化 Invoke | 精确 ProductRoot | 创建根公共组件，子场景显式初始化，不发送旧 Main EntryEvent |
| Peer 与共同控制消息 Handler | ProductHost | Peer／HostSession MailBox 处理；HostSession 是唯一 Match 名单提交入口 |
| 客户端快照消息 Handler | ProductClient | 本地 Send 和远端桥接 Send 到同一 ClientSession ActorId，不写 Host 状态 |
| MemberChanged Event（拟定） | ProductHost | 提交成员或在线状态后发布 MatchId／RosterVersion，观察者不增加版本 |
| ClientJoined／RosterApplied／ClientLeft Event（拟定） | ProductClient | 本端确认与投影提交后发布，不将通知到达作为已加入 |
| OnShutdown 既有 Invoke | 沿用已有精确键，再分流产品根 | 不重复注册 handler；先调用同步停止，再释放 World |

事件名仅是拟定标识；实际属性签名沿用现有 ET。观察者不进入名单提交，也不发布同类事件回写名单；配置新增 SceneType 后仍要检查 All 匹配和根 DynamicEvent 广播，不能承诺自动事件隔离。

### 6. 端口、心跳与分层退出

Host 创建 NetComponent 后从 KService.Transport 的 UDP 绑定读取实际 endpoint。必要框架改动限定为 IKcpTransport.cs 内 UdpTransport 的只读绑定地址访问；不扩展所有 transport 接口、不试探扫描端口、不新增租约服务。端口冲突失败，端口 0 由 OS 分配后回报；Client 使用显式非零目标，Pipeline 连接端口不参与业务配置。

远端加入成功后移除该 Session 的验证超时，按既有空闲阈值安排心跳；每连接最多一个在途心跳，明确超时／取消，不复用 Demo 的 Gate 协议和全球时钟写入。空闲检查保留，本地通道无网络心跳。绑定退出后的计时／答复都检查实例身份。

显式 Leave 表示退出逻辑协作，走共同裁决移除该 Participant 并关闭命令资格，重复清理不重复变更版本；仅关闭本端连接与发出 Leave 不等价，Leave 未提交时只能将成员离线。本原型没有未领取结果或角色资产，后续玩法 Leave 另行补齐业务收尾。单连接关闭立即撤销 Peer 入口权限，但保留处理在途本地请求所需的 Peer／Mailbox；主动 Leave 尽力答复后才关闭 Session，意外断线的 Session 则已由原错误路径销毁，不能直接删除 Peer 并承诺本地等待即时取消。

整体 Stop 先进入 Stopping，拒绝新的 Join／Probe；尽力发会话关闭通知，不 await 远端。随后调用本轮 sender.Stop 结清本地等待，销毁各 Session／NetComponent 结清网络等待，再释放本轮子树与 Fiber；此时 Timer 尚在，最终树 Dispose 仍有 sender 的兜底。应用正常关闭通过 OnShutdown 在 World.Dispose 前进入该顺序，失败／直接销毁也不遗留资源。

意外断开立即撤销可信绑定并结清该 Session 网络等待，通过共同有序入口将 Participant 离线，逻辑身份和名单条目保留；在线状态实际改变才增加 RosterVersion。尚未提交 Join 的临时 Peer 关闭而不创建成员。Host 整体 Stop 解散本轮 Match 后释放所有成员，不将正常连接 Dispose 当成逻辑 Leave。

单连接关闭时，接入层在调用本地 Call 前增加 Peer 在途计数，等待在 finally 中结束后减少；不复制 sender 的请求字典或 UniTask 等待表。Closing Peer 拒绝新业务、让已接入请求走明确失败答复；共同处理不 await Client／Peer，能在消息泵继续运行时排空。计数为零且共同关闭处理完成后才释放 Peer／Mailbox。已提交成功保留，旧答复不得交给新 Session。

该过程不保证对单个本地 Call 的即时取消；异常未答复仍依赖既有 40 秒超时，期间入口始终无命令权限。若消息泵永久停止，进入整体运行退出，由前置框架 Stop 结清本轮等待，不引入另一套墙钟取消器。Session 的网络等待依既有 Dispose／显式有限 time；本项不扩展核心按请求取消接口。正常 Peer 处理应无需等待 40 秒才退出，回归要证明关闭后请求实际返回拒绝，并验证计数不是零就提前释放的错误。

| 状态对象 | 转换 | 提交和释放结果 |
| --- | --- | --- |
| Host 根 | Initializing → HostListening → HostReady；任一步失败／退出 → Stopping → Stopped | HostReady 需本地 Joined；停止后不被异步回调复活 |
| Client | Connecting → Joining → Joined；失败 → Failed 后清理；退出 → Stopping → Stopped | 不自动重试；退出后清空本端活动绑定和投影 |
| Peer | Accepted → Joining → Bound；失败／断开／Leave → Closing → Closed | Closing 先失效权限，待已接入请求结束再释放；不会回到 Bound |
| Participant | Join 提交时创建在线；断线 → 离线；显式 Leave 提交／Match 解散 → 释放 | 断线不会自行删除；本原型不实现从离线凭证恢复 |

成功 Leave 的响应要在 Session 仍能发送时先尝试发送，然后关闭；发送失败不能撤销已经移除的成员。离线通知和 Leave 可能交错：未提交 Leave 时连接先失效，拒绝旧请求并保留离线成员；Leave 已提交后关闭通知不重新创建成员或重复增加版本。root 停止先使所有入口关闭，之后共用 Stop 才结清本地等待，计数回调此时检查根身份并不得创建资源。

提交后的 await 返回检查只决定结果是否还能交付给原调用方，不能把已提交成功改成未执行。成功 Leave 是 Closing 状态下允许回送既有结果的收尾路径，不因自己刚撤销资格而改成失败。主动 Client 等待 Leave 答复只用有限预算，到期可关闭自身连接；Host 是否已移除成员仍按真实提交事实判断。

Host 本地 Client 单独 Leave 与 root Stop 同样分开：前者只结束本地成员及其接入，Host 监听和远端成员继续存在；HostReady 是此前启动阶段的成功事实，不意味着本地成员以后永不退出。原型不自动重新加入本地玩家，应用／Procedure 整体退出才结束 Host 根。

### 7. 文件与验证

| 位置 | 本项范围 |
| --- | --- |
| Game/ET/Code/Model/Share/Product 与 Hotfix/Share/Product（拟新增） | owner 数据／System、产品 FiberInit、公共 Peer 与会话 handler、入口与投递、测试探测 |
| Model/Share/Entry.cs、Game/Procedure/ProcedureET.cs、ET/Loader | 同一配置交接、适用能力检查、根分流与阶段／失败报告，避免阻塞消息泵 |
| ET.Core/Runtime/Entity/SceneType.cs | 产品独立类型，核对标志匹配 |
| ET.Core/Runtime/Network/IKcpTransport.cs | 仅 UDP 实际监听 endpoint 的只读暴露，核对 DotNet 同源编译 |
| HotfixView/Client/Module/Unity/UnityEventHandler.cs | 延用现有关闭 Invoke 键进入本轮同步停止；不新增重复 handler |
| Design/Proto/ET-Client 启用源、现有 Proto2CS 输出 | 产品 wire／内部消息的源修改与生成，不手改输出，不新增生成器 |
| 既有 UTF／Tools/AutoTesting | 接现有测试程序集或必要 asmdef，无新 runner／编排服务 |
| references、Book、Docs/AI 既有相关页面 | 实施后记录树、消息、运行用法、定位和实测范围 |

UTF 验证真实树、mailbox 路由、共同裁决、名单版本／对象回收、加入失败、重复退出及下一轮隔离；网络测试实际监听回环，不用 mock 通过宣称 KCP 验收。双 Editor 在明确授权的稳定端环境可用后，使用真实角色配置和完整 projectPath，两种角色排列都核对名单／Probe／空闲保活／退出，保存原日志原位。未运行记 NotRun，不创建 JSON 证据包或执行版本 hash 比对。

| 验收范围 | 必须观察到的结果 | 能证明的边界 |
| --- | --- | --- |
| Host 单端、本地通道 | 真实树中 Match／Participant、Peer 与 Client 独立；本地 Join／Probe／Leave 经过真实 Call／Mailbox，未向自身 UDP 发请求 | 本地产品会话，不证明远端网络 |
| 两个接入并发 | 本地及远端请求进入同一 HostSession；编号不串、相同版本名单一致、通知对象独立 | 共同控制裁决与投影，不证明固定玩法 Tick |
| 单连接断开与 Leave | 断线成员离线、其他成员 Probe 正常；在途请求被拒绝；Leave 只移除一次，Peer 排空后释放 | 连接和逻辑成员寿命分离，不证明自动重连 |
| root 退出与连续两轮 | 全部等待结束、监听释放、旧结果不修改新轮；重复 Stop 安全 | 本轮退出及实例隔离，不证明跨域恢复 |
| 真 KCP loopback | 报告实际非零 endpoint；兼容／不兼容 Join、心跳空闲、退出各有明确终态 | 同机真实传输，不证明两台物理电脑或公网 |
| 双 Editor 两种角色排列 | 各自配置／数据目录和日志有效、MatchId／名单一致、Probe 请求答复可追踪 | 稳定对端联调；缺版本或环境记 NotRun |

本轮仅静态核对。上述动态场景均为未来验收，不因设计表或任务存在而算通过。

## Risks / Trade-offs

- [启动 await 等待未安装的 Runner] → 装载先允许消息泵运转，后续阶段由原 owner 异步报告。
- [子 Scene 没有自动隔离旧事件] → 独立标志与显式启动，核对 All／精确 Invoke，不扩展 DynamicEvent。
- [本地对象引用掩盖联网所有权问题] → 每次投递独立消息，DTO 深复制，回收复用回归。
- [内部 RpcId 覆盖网络编号] → 桥接持有原编号、独立请求响应，两份既有等待表分别工作。
- [验证超时或空闲检查误断开] → 加入后解除验证超时、有界心跳，做持续真实连接验证。
- [稳定基线缺新协议] → 明确必要维护版本，不自动 pull，不用旧 Demo 代替产品连接。
- [连接 Dispose 误删逻辑成员] → Match 独立拥有 Participant，断线只结束绑定并标离线，显式 Leave 与解散分别处理。
- [误把单连接关闭写成即时取消所有本地等待] → Peer 先关闭资格、保留 Mailbox 排空在途；异常回退既有有限超时，root 退出才 Stop 共用 sender。

## Migration Plan

1. 按业务交接校准成员和连接生命周期；后续明确 apply 授权才实施产品会话。当前不修改代码或主规格。
2. 启动项及本地请求 Stop 能力实际可用后，先实现公共树和本地加入，再接 wire 桥与真实 loopback。
3. 更新源协议与生成输出、相关用法和模块索引，验证适用程序集与本机 UTF；不启动示例服务或打包。
4. 稳定第二端按必要维护纳入获准版本，再做双 Editor 实测；缺条件记 NotRun，相应任务未完成。
5. 回退本项时移除产品接入、保留原不可用拒绝和普通路线，不删用户数据、不停止别的工程。实现与验证完成后再按流程同步 delta／归档，不自动提交或推送。

## 业务交接校准与后续边界

[交接 A3／G3／G5](../../../Docs/handoff/Cooking-设计交接-2026-10-09.md) 和现有产品设计已经明确断线保留逻辑成员，原草案的删除／保留问题撤回。本项增加最小 Match／Participant，不实现自动重连或凭证协议，也不把新连接按新成员加入当成完整产品恢复能力。真正待裁决的暂停队列、断线手持物、正式传输和安全协议仍见 [目标规格设计](../../../Docs/Product/architecture.md)，不会因本项完成而视为已解决。

## Sources

- [AGENTS 设计重点](../../../AGENTS.md)、[启动配置设计](../multiplayer-instance-bootstrap/design.md)、[本地请求 Stop 设计](../local-message-request-shutdown/design.md)、[稳定第二端](../multiplayer-peer-workspace/design.md)
- [ET Entry](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Entry.cs)、[FiberInit_Main](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/FiberInit_Main.cs)、[Main 公共组件初始化](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Demo/EntryEvent1_InitShare.cs)
- [SceneType](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Entity/SceneType.cs)、[MailBox](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/MailBoxComponent.cs)、[有序接收](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Module/Actor/MailBoxType_OrderedMessageHandler.cs)
- [本地发送器](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Actor/ProcessInnerSenderSystem.cs)、[actor Handler](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Module/Actor/MessageHandler.cs)、[EventSystem](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/World/Module/EventSystem/EventSystem.cs)
- [网络组件](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Module/Message/NetComponentSystem.cs)、[Session](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Message/Session.cs)、[UDP 绑定](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/IKcpTransport.cs)
- [Init 和 Runner](../../../Unity/Assets/Scripts/Game/ET/Loader/Init.cs)、[既有关闭 handler](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Module/Unity/UnityEventHandler.cs)、[CodeMode 分流](../../../Unity/Assets/Scripts/Game/ET/Editor/DefineSymbol/CodeModeDefineSymbolTool.cs)
- [协议配置](../../../Design/Proto/ET-Client/proto.conf)、[Proto 工具用法](../../../Book/Proto生成工具.md)、[网络约定](../../../references/reactive-and-network.md)、[端口 SOP](../../../references/port-management.md)
