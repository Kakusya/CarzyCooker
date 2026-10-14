# Design

## Context

当前 endpoint 限制 loopback，Editor 同进程/双 Editor 结果不能证明独立 Player 和 LAN。沿既有 BuildHelper/HybridCLR/资源入口补有限启动配置，最后做设备验收。 动机见 [proposal.md](proposal.md)。本案为待实施规划。

## Goals / Non-Goals

目标模式：PC Player Host/Client。不做公网、中继、NAT、大厅、联机存档或更换正式传输；不新建双包编排器／端口注册表。

依赖：[cooperative-restaurant-loopback](../archive/2026-10-14-cooperative-restaurant-loopback/proposal.md)。层次只表达从局部到跨模块的深度；仅上述实际合同构成依赖。

## Decisions

### 1. 运行输入进入稳定入口

独立 Player 从显式命令行角色/endpoint 输入生成现有 RuntimeInstanceRequest，启动时验证并冻结；无参数默认 Solo。Host 显示实际监听结果，Client 使用显式输入；不依赖 EditorPrefs 或制作专用双包编排器。首次从普通 Launcher 场景完成 UGF/ET 启动。

### 2. 显式私网范围

默认仍 loopback；仅显式 LAN 模式允许 RFC1918 私网 IPv4。Host 指定实际本机网卡地址，Client 目标端口非零；拒绝通配、公网、广播、组播和未支持 IPv6，不自动扫描发现或改全局防火墙。工具 Pipeline 端口仍从实例发现，不能当游戏端口。

### 3. 按证据分段推进

先实际 BuildReport/热更资源/AOT，再当轮包在同机独立进程完成流程，再两台物理 PC LAN。每段失败保留日志，只修本段；同 Editor KCP 不替代独立进程，独立进程不替代跨机。

### 4. 先声明选定的产品范围

执行前在本案列清已选 Editor 验收的功能；两菜/前厅/补货/普通换关是基线，若纳入装修、恢复、饮品则先有对应 Editor 结果，再设备补局部链。未选择的候选不成为永远阻塞所有打包的前置。所有本轮选中功能的设备测试放在其 Editor 工作后。

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

不新增业务树。新增交接仅 Player 进程参数 → 稳定 RuntimeInstance 配置 → ProcedureET/ET 初始化 → ProductRuntimeBootstrap。Host NetComponent/Session 属 ProductHost，Client Session 属本轮接入；每个 Player 独立进程拥有自己的 Fiber。端口和进程只清理本轮明确拥有者，另一个工作区仍按实际分支发现与指定提交维护。

## Risks / Trade-offs

包体和监听范围有明确平台风险。分构建、同机独立进程、物理两机三段验证，失败停在对应段；不修改全局防火墙、不扫描网络。 → 具体覆盖与接受的 NotRun 见 [execution-notes.md](execution-notes.md)。不引入额外测试平台、全面压测或理论完备性门槛。

## Migration Plan

实施前以最新主规格、依赖 change 和源码核对差异；保持本案可独立审阅。涉及配置/协议/CodeBind 时修改源并正常生成，不手改生成物。需要格式适配的改动先验证旧有效输入与失败保留，再开放入口。若失败则保留原用户文件和原失败结果，只撤回本案未发布改动，不重置其他人的工作；不自动提交、归档或同步第二工作区。

## Sources

- [Book/打包与包体验收SOP.md](../../../Book/打包与包体验收SOP.md)
- [Unity/Assets/Scripts/Game/Editor/Build/BuildHelper.cs](../../../Unity/Assets/Scripts/Game/Editor/Build/BuildHelper.cs)
- [Unity/Assets/Scripts/Game/Base/RuntimeInstance/RuntimeInstanceConfig.cs](../../../Unity/Assets/Scripts/Game/Base/RuntimeInstance/RuntimeInstanceConfig.cs)
- [Unity/Assets/Scripts/Game/Base/RuntimeInstance/RuntimeInstanceRequest.cs](../../../Unity/Assets/Scripts/Game/Base/RuntimeInstance/RuntimeInstanceRequest.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs)
- [openspec/specs/runtime-instances/spec.md](../../../openspec/specs/runtime-instances/spec.md)
- [openspec/specs/product-session/spec.md](../../../openspec/specs/product-session/spec.md)
