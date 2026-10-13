# Design

## Context

先完成 Solo 橙汁实体链，合作补货/清理与普通双 Editor 链；本案只接公开饮品动作和独立投影，不复制加工或交付规则。 动机见 [proposal.md](proposal.md)。本案为待实施规划。

## Goals / Non-Goals

目标模式：Host/Client 内存餐厅。不增加饮品配方、联机持久化、自动恢复或新传输；重连能力单列依赖时才验收其组合。

依赖：[first-drink-order-binding](../first-drink-order-binding/proposal.md)、[cooperative-cleanup-supply](../cooperative-cleanup-supply/proposal.md)、[cooperative-restaurant-loopback](../cooperative-restaurant-loopback/proposal.md)、[product-local-message-boundaries](../archive/2026-10-13-product-local-message-boundaries/proposal.md)。层次只表达从局部到跨模块的深度；仅上述实际合同构成依赖。

## Decisions

### 1. 白名单逐项开放

将 Solo 实际公开的榨汁、盛装、贴单/解绑/换绑动作接入 ProductWire；动作载荷、上下文、目标、关联答复逐字段复制。内部完成/消费/推进通知保持不可从 wire 伪造，协议在最新基线上正常生成。

### 2. 竞争由一次提交裁决

两端抢最后一份、同杯或同订单时都到共同服务步；只有第一个合法动作改变事实，后来者返回拒绝并保留其物品。重绑完整验证后原子提交，旧关/旧绑定不能解绑新关。

### 3. 投影只观察共同事实

分别复制果汁壶锁定/份数、杯内容、贴单对象和当前订单；客户端不持 Host 实体。断线沿既有落物规则，绑定是否仍有效由当前订单决定；若接入成员恢复，恢复空手，原杯可被其他成员继续操作。

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

业务 owner 与 Solo 饮品一致：持续厨房持批次与杯；当前 Level 持贴单关系。端差异仅是入口/通知运输，Host 本地始终本地消息；不为合作创建第二套订单、批次或杯库存。

## Risks / Trade-offs

远端字段漏拷贝、杯／订单重复消费和旧绑定是主要风险。用真实 KCP 请求加两端真实输入，复用 Solo 配方算法证据。 → 具体覆盖与接受的 NotRun 见 [execution-notes.md](execution-notes.md)。不引入额外测试平台、全面压测或理论完备性门槛。

## Migration Plan

实施前以最新主规格、依赖 change 和源码核对差异；保持本案可独立审阅。涉及配置/协议/CodeBind 时修改源并正常生成，不手改生成物。需要格式适配的改动先验证旧有效输入与失败保留，再开放入口。若失败则保留原用户文件和原失败结果，只撤回本案未发布改动，不重置其他人的工作；不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridClientSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridClientSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridMessages.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridMessages.cs)
- [Unity/Assets/Tests/Editor/ET/CoopCoreCookingTests.cs](../../../Unity/Assets/Tests/Editor/ET/CoopCoreCookingTests.cs)
- [Docs/Product/specs/orders-front-service/spec.md](../../../Docs/Product/specs/orders-front-service/spec.md)
