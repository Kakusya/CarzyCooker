# Design

## Context

现有断线保留 Participant 并移除角色/落物，新的 Join 不能接管原成员；尚无恢复凭证。首版只支持 Host 和 Client 进程、本轮 Match 都还在的手动恢复，不承诺跨进程或跨设备身份。 动机见 [proposal.md](proposal.md)。本案为待实施规划。

## Goals / Non-Goals

目标模式：Host/Client；Host 与 Client 进程均保留。不自动重连、持久凭证、跨进程重启、Host 迁移、联机存档、账号鉴权或公网安全产品化。

依赖：[cooperative-restaurant-loopback](../cooperative-restaurant-loopback/proposal.md)、[product-local-message-boundaries](../product-local-message-boundaries/proposal.md)。层次只表达从局部到跨模块的深度；仅上述实际合同构成依赖。

## Decisions

### 1. 凭证归明确生命周期

合法 Join 时 Host 为成员创建不可猜测随机恢复凭证，权威记录归 Match/Participant；Client 将凭证保存在 ProductRuntime 下新增恢复意图组件（不归会被释放的 Session），只在内存保存。Match、协议和平台一起校验，日志不打印凭证；不做账户、签名服务或持久化。

### 2. 同凭证重试解决回执丢失

凭证在本 Match 内保持有效直到 Leave/整体退出，不在每次 Resume 时轮换，避免成功答复丢失锁死恢复。Resume 携带一次尝试编号；同尝试同载荷返回已绑定事实，异载荷拒绝。多个新连接竞争由 HostSession 串行，最新合法绑定替换旧绑定并递增代次，绝不新增成员；有限保存最新尝试结果。

### 3. 恢复只恢复成员和当下世界

连接失联撤旧绑定、取消旧意图并按既有落物规则移除角色。手动 Resume 为同一成员在当前关生成新角色且空手，不能取回已经落下或被别人拿走的物品；换关后恢复以当前关为准。

### 4. 就绪和取消明确

恢复回执只确认本人/新代次，必须再取得匹配完整投影和视图才能输入，通知不能先自认身份。超时显示未知/可手动重试，不自动重放旧业务命令。显式 Leave 成功、停止本轮或 Host 退出撤销凭证；本地取消恢复只清本地意图，无法在线提交 Leave 时不谎报 Host 已移除成员。

### 状态归属与交接路径

```text
Product Fiber / RootScene (SceneType.ProductRoot)
├─ Component: ProductRuntime、ProcessInnerSender、Timer、CoroutineLock
├─ ChildScene: ProductHost (SceneType.ProductHost)
│  ├─ Component: HostSession + Ordered MailBox
│  │  └─ Child: HostPeer + Ordered MailBox（绑定 Participant，非其 owner）
│  └─ Child: Match
│     ├─ Child: Participant（成员事实）
│     └─ Child: RestaurantRuntime
│        ├─ Component: GridKitchen → Child 工位／物品／加工
│        ├─ Component: RestaurantRenovation（跨关奖励）
│        └─ Child: 当前 Level → Child 角色／顾客／订单／前厅工作
└─ ChildScene: ProductClient (SceneType.ProductClient)
   └─ Component: ClientSession + MailBox → Component: GridClient（独立值投影）
      同 Scene 受管 GridPresentation、UIComponent、GFEntityComponent
```
Host/Solo 先建立权威及餐厅，再建立接入、客户端和视图；远端 Client 只有客户端一侧。不同运行使用独立 Fiber/世界身份，UI 不持有权威实体。

本地 UI 从当前 LocalMessage.Peer 取得本地 HostPeer ActorId，并由 ClientSession 提供本轮命令上下文，经 ProductRoot 的 ProcessInnerSender/本地队列投递；HostPeer 的 ProductHost handler 校验真实发送者与绑定，再发往同 Scene 的 HostSession Ordered MailBox。远端经 Session 和 ProductWire 显式白名单复制为同一内部请求，再进入该 Peer；Host 不向自己发 UDP。

入口快速答复接受／拒绝，服务步由持有相关状态的 owner 完成验证后提交；接收、启动、完成分别返回，状态先提交再发布投影或观察 Event。通知经 LocalDelivery／RemoteDelivery 回到 ProductClient handler，按运行、Level、绑定代次安装独立值；网络 RPC 号不代替业务编号。框架 Invoke 仅承担既有启动/生命周期，Event 不充当请求答复，不增加 SceneType.All 写状态消费者。

请求池对象只在本次处理内使用，跨 await／队列复制值；每一跳答复对象由该跳调用者收尾，领域快照不引用消息池。视图关闭取消业务等待但原调用继续按答复／有限超时收尾，await 后核对 owner InstanceId 和绑定。成员退出只撤自身接入、取消旧动作并执行既有落物；整轮停止才结束公共发送器、剩余等待和子树。

新增 owner：Match → Participant 保存恢复凭证/最新尝试；ProductRoot → ProductRuntime 下恢复意图组件保存 Client 凭证/所选 endpoint。ProductClient/Session 可重建而此意图保留，整轮 Stop 才清除。远端 Resume：新 Session → ProductWire 的专用接入 handler → HostSession Ordered MailBox 校验凭证 → 原 Participant 重绑定新 HostPeer → 新 Session 回执/全量投影。不能把 Resume 当已绑定玩法请求，自报 MemberId 或 ActorId 永不授权。所有 await 结果同时核对尝试号、运行和绑定；旧 Session 回执只收尾，不能解除新绑定。

## Risks / Trade-offs

恢复入口涉及冒认成员、重复绑定和迟到回复。用随机能力凭证、Host 单写提交及代表性真实 KCP 负例和失联恢复覆盖；不扩成跨设备身份系统。 → 具体覆盖与接受的 NotRun 见 [execution-notes.md](execution-notes.md)。不引入额外测试平台、全面压测或理论完备性门槛。

## Migration Plan

实施前以最新主规格、依赖 change 和源码核对差异；保持本案可独立审阅。涉及配置/协议/CodeBind 时修改源并正常生成，不手改生成物。需要格式适配的改动先验证旧有效输入与失败保留，再开放入口。若失败则保留原用户文件和原失败结果，只撤回本案未发布改动，不重置其他人的工作；不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/ProductSession.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/ProductSession.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductClientSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductClientSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductPeerSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductPeerSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductAuthority.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductAuthority.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs)
- [openspec/specs/product-session/spec.md](../../../openspec/specs/product-session/spec.md)
