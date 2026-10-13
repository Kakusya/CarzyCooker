# Design

## Context

现有 UTF 已能驱动真实本地消息及同 Editor KCP。近期失败涉及默认物品位置假设、客户端尚未收到投影、运行重启与等待；应修正重复夹具，保留失败揭示的行为边界。 动机见 [proposal.md](proposal.md)。本案为待实施规划。

## Goals / Non-Goals

目标模式：测试维护。不改生产业务、Tools/AutoTesting 协议或 UTF 框架；不全面重写测试，不新增测试编排器。

依赖：无玩法硬前置。层次只表达从局部到跨模块的深度；仅上述实际合同构成依赖。

## Decisions

### 1. 保留现有测试宿主

只抽取现有测试 partial/fixture 内重复配置查找、创建和清理；不新增运行器、协议或生产后门。目录名 Editor 不推导测试模式，实际发现以 PlayMode 为准。

### 2. 等待条件表达业务阶段

分别等待运行树已创建、Join 确认、完整投影、动作最终结果；消息泵不会隐式推进业务时间，业务 Tick 由测试明确驱动。用条件截止和实际阶段诊断，不无条件睡眠、放大所有超时或重跑到绿。

### 3. 夹具退出仍覆盖失败路径

fixture 只拥有自己创建的 Fiber、Session、UI 与临时文件。部分启动失败也释放自身资源；遇到用户已有产品/测试拒绝启动。代表性等待不满足应明确失败并保留原输出，测试方法原断言不被吞掉。

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

测试仅控制消息泵与业务步进，不能直接修改最后结果冒充请求。共享辅助函数不拥有其他测试的运行或静态最终状态。

## Risks / Trade-offs

测试可信性是实际风险。先保留原行为断言，按变动夹具执行最小 PlayMode 小集，并用代表性条件未满足情况确认等待会失败；不延长所有超时。 → 具体覆盖与接受的 NotRun 见 [execution-notes.md](execution-notes.md)。不引入额外测试平台、全面压测或理论完备性门槛。

## Migration Plan

实施前以最新主规格、依赖 change 和源码核对差异；保持本案可独立审阅。涉及配置/协议/CodeBind 时修改源并正常生成，不手改生成物。需要格式适配的改动先验证旧有效输入与失败保留，再开放入口。若失败则保留原用户文件和原失败结果，只撤回本案未发布改动，不重置其他人的工作；不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Tests/Editor/ET/ProductSessionTests.cs](../../../Unity/Assets/Tests/Editor/ET/ProductSessionTests.cs)
- [Unity/Assets/Tests/Editor/ET/SoloSupplyTests.cs](../../../Unity/Assets/Tests/Editor/ET/SoloSupplyTests.cs)
- [Unity/Assets/Tests/Editor/ET/SoloSaveSelectionUiTests.cs](../../../Unity/Assets/Tests/Editor/ET/SoloSaveSelectionUiTests.cs)
- [Unity/Assets/Tests/Editor/ET/CoopCoreControlTests.cs](../../../Unity/Assets/Tests/Editor/ET/CoopCoreControlTests.cs)
- [Tools/AutoTesting/README.md](../../../Tools/AutoTesting/README.md)
- [openspec/specs/automated-testing/spec.md](../../../openspec/specs/automated-testing/spec.md)
