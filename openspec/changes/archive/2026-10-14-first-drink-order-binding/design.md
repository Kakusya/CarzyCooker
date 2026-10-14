# Design

## Context

产品目录已确认鲜榨橙汁和独立贴单，当前实现只有汤/吐司。用户确认加工中锁壶、完成可搬运逐杯分装、产量和耗时配表。本案完整接入 Solo 饮品链。 动机见 [proposal.md](proposal.md)。本案已实施，任务与逐点运行结果见 execution-notes.md。

## Goals / Non-Goals

目标模式：Solo。不加冰热糖度、封口小游戏、其他饮品、联机动作或联机存档。

依赖：[product-local-message-boundaries](../2026-10-13-product-local-message-boundaries/proposal.md)、[solo-first-chapter](../2026-10-14-solo-first-chapter/proposal.md)、[solo-supply-delivery](../../solo-supply-delivery/proposal.md)。层次只表达从局部到跨模块的深度；仅上述实际合同构成依赖。

## Decisions

### 实施衔接（2026-10-14）

旧 `1→2→2` 与已验收的两菜 `101→102→103→0` 保留原布局、库存和供应。新增 Solo 菜单变体 `MenuVariant=1`，使用 `201→202→203→0`，仍属第一大关的三阶段，两道饭菜保持不变并追加鲜榨橙汁；普通选档的新游戏／内存试玩选择此变体，旧有效档按 Header 的下一关定义恢复，不免费追加设备或物资。Restaurant 持有创建时的菜单变体，Level 的允许菜单来自配置的饭菜与饮品合集；Host／远端 Client 不开放饮品动作。

协议在原工位升级的 20 上递增到 21；显式 `ExpectedOrderId` 贯穿 GridIntent、Peer／Host 转交与重放比较。自动批次冻结 `RequiredCapability` 和 `LockContainer`，不借汤锅的移动许可放行加工中的果汁壶。杯→订单字典只由当前 Level 持有，投影只复制 `BoundOrderId`，换关新 Level 清空，不将贴单写入检查点。

保存格式从升级案的 v3 递增为 v4，批次末尾追加能力与锁定字段；v1–v3 批次以既有煮汤能力、无移动锁读取，禁止旧格式声明新增饮品菜单。有限杯供应用允许装空一次性杯的补给袋承载，普通装料仍禁止嵌套容器，文件验证只允许该袋装合法干净空杯。一次性杯清桌工作保持原玩家领取／实际耗时规则，ItemId 为 0，不产生洗杯后继。

### 1. 复用实物模型，明确加工差异

橙子经手工切配成为橙块，装果汁壶放榨汁机显式开工；验证后冻结耗时/产量、占用壶与机，加工中拒绝搬壶/机、转移内容和倒掉，失败保物。完成解除占用，果汁和剩余份数随壶移动，向干净空杯一次分一份。汤锅既有可搬加工行为不变。

### 2. 贴单独立于制作

成品杯放贴单台并到位操作，绑定当前有效顾客订单；绑定不改变配方。首版一个杯只绑定一单，一单同时最多绑定一杯；合法换绑先完整验证新订单和占用，再解除旧关系并提交。失败保留旧绑定；显式解绑可重用杯。

### 3. 订单与杯共同校验

交付仍到餐桌，核对当前 Level、顾客、订单、配方与绑定；未贴单/错单拒绝保物。订单完成/失效/顾客离席、杯内容清理或旧 Level 退出使绑定失效。汤和吐司继续免贴单。一次性杯交付随内容消费，不进脏池/干净池；用餐/清桌仍真实耗时并释放桌位。

### 4. 供应、清理与存档接齐

橙子和杯经有限供应而非免费补物；配置检查菜单依赖闭包、设备和容器入口。液体排水池，固体倒垃圾桶，未完成锁定加工先拒绝清理；份数按现有守恒规则减少。保存保留可承接实物/批次/加工，当前关贴单关系不写入下一关。

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

果汁批次、锁定引用、剩余份数归 GridKitchen 下物品/自动作业；双向贴单关系归当前 Level 的订单与杯 ID 关联（杯投影复制引用，不保存第二份权威）。贴单请求由 ProductHost handler 校验真实角色并进入服务步；内部加工完成消息不得从 wire 调用。Level 先撤销绑定/结束角色，再换关，持续杯/壶保留。保存沿现有链：ProductClient 的受管入口 → 本地请求 → HostSession/Match 下 SoloSaveResult 捕获纯值 → ProductRoot 的 SoloSaveRepository Ordered MailBox 写文件 → 结果回原 Match/Client。Repository 比 Match 长寿，晚到 I/O 不写新 Match；文件完成后才允许离开原结算。沿用临时文件、Flush(true)、不覆盖发布和 Result/Profile 去重；重试同一结果不重复奖励。Header 不保存 ET ActorId/EntityId。

文件格式在实施时基于届时最新版本递增，不能多个 change 各自假定“都用版本 2”。旧有效格式通过显式只读适配生成内存值，不重写原文件；未知/损坏文件保持拒绝。不支持迁移的新增字段不得静默丢弃。

## Risks / Trade-offs

风险集中在守恒、加工锁定、旧订单绑定和一次性杯收尾。复用现有物品、服务与供应，仅增加新链相应 UTF 加一条普通经营链；保存增加实际往返。 → 具体覆盖与接受的 NotRun 见 [execution-notes.md](execution-notes.md)。不引入额外测试平台、全面压测或理论完备性门槛。

## Migration Plan

实施前以最新主规格、依赖 change 和源码核对差异；保持本案可独立审阅。涉及配置/协议/CodeBind 时修改源并正常生成，不手改生成物。需要格式适配的改动先验证旧有效输入与失败保留，再开放入口。若失败则保留原用户文件和原失败结果，只撤回本案未发布改动，不重置其他人的工作；不自动提交、归档或同步第二工作区。

## Sources

- [Docs/Product/catalog.md](../../../../Docs/Product/catalog.md)
- [Docs/Product/specs/kitchen-item-processing/spec.md](../../../../Docs/Product/specs/kitchen-item-processing/spec.md)
- [Docs/Product/specs/orders-front-service/spec.md](../../../../Docs/Product/specs/orders-front-service/spec.md)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSoupSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSoupSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridServeSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridServeSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridCleanupSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridCleanupSystem.cs)
- [openspec/specs/orders-front-service/spec.md](../../../specs/orders-front-service/spec.md)
