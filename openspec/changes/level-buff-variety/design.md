# Design

## Context

BuffConfig 当前仅支持煮汤效果，切配与烘焙有各自作业和 RequiredTicks。沿 product-local-message-boundaries 的作业完成与服务步合同扩展，不再增加直接跨 owner 修改。 动机见 [proposal.md](proposal.md)。本案为待实施规划。

## Goals / Non-Goals

目标模式：Solo 与已交付合作核心。不加多重 Buff 叠加、额外产量、免费材料、全工序或随机效果平台。

依赖：[product-local-message-boundaries](../product-local-message-boundaries/proposal.md)、[cooperative-restaurant-core](../cooperative-restaurant-core/proposal.md)。层次只表达从局部到跨模块的深度；仅上述实际合同构成依赖。

## Decisions

### 1. 有限效果与精确目标

新增手工切配、烘焙效果种类，配置指定对应配方 ID 和正比例；拒绝类别与配方不匹配，候选不足按既有规则，不做全工序反射或多个 Buff 叠加。

### 2. 启动时一次计算

作业接受/启动提交时用当前选中 Buff 算耗时，整数向上取整且至少 1 Tick，结果放作业自身；旧作业、跨关作业不重算。显示读取冻结值，暂停不推进时间；随机种子与候选选择算法不变。

### 3. 模式共享与后续升级对齐

Solo/Host 复用共同控制消息，远端 Client 不得选择 Buff；Host 选择后投影给双方。后续工位升级与 Buff 使用原始基值乘有效比例后一次取整，避免先后顺序影响结果；本案不先造升级状态。

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

Buff 选择/候选归当前 Level；冻结参数归 GridKitchen 下 Manual/Auto/Bake 作业。完成通知携带作业与运行/关代次，经相应 owner 验证，一次提交产物。换关销毁 Level Buff 而保留作业冻结值。

## Risks / Trade-offs

效果可能重复缩时、影响旧作业或由远端越权选择。按新旧作业对照、真实本地与 KCP 权限小集验收；不测全菜目录。 → 具体覆盖与接受的 NotRun 见 [execution-notes.md](execution-notes.md)。不引入额外测试平台、全面压测或理论完备性门槛。

## Migration Plan

实施前以最新主规格、依赖 change 和源码核对差异；保持本案可独立审阅。涉及配置/协议/CodeBind 时修改源并正常生成，不手改生成物。需要格式适配的改动先验证旧有效输入与失败保留，再开放入口。若失败则保留原用户文件和原失败结果，只撤回本案未发布改动，不重置其他人的工作；不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/BuffConfig.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/BuffConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/LevelBuffSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/LevelBuffSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs)
- [openspec/specs/level-buffs/spec.md](../../../openspec/specs/level-buffs/spec.md)
