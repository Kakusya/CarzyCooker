# Design

## Context

动机见 [proposal.md](proposal.md)。本案新增同一营业页中的跨对象派生显示，必须明确队列计数、参考星级和 UI 导航寿命，因此创建设计。已核对主规格 [restaurant-ui](../../../specs/restaurant-ui/spec.md)、[orders-front-service](../../../specs/orders-front-service/spec.md)及[关卡生命周期](../../../specs/restaurant-level-lifecycle/spec.md)。

已确认的实现：

- `GridPresentationPanels.RefreshRestaurantPanels` 逐单列出 Waiting 订单，HUD 直接拼接 `LevelScore/LevelStars`；`LevelTransitions.AdvanceLevelTransitions` 只在合法 Complete 时计算最终星级。因此本案修正的是显示口径，不将尚未结算判为计分失效。
- `GridFrontBroadcast.CopyFrontState` 已复制 Orders、Customers、Tables、WaitingCustomers、SkippedArrivals；`GridClientComponent` 已安装营业时间、得分和计数。`GridFrontProjection` 的独立值足够本案使用。
- `FrontConfig.Validate` 保证每个 LevelConfig 有唯一客流定义；客流配置通过 `LevelConfigId` 匹配，不能假设客流行 Id 等于关卡 Id。
- `GridCustomerFlow` 在顾客进场时即将身份入 Waiting，尚在走向等位点的顾客也占队列；入座后出队。桌位 Free 但工位上有物品时不会分配座位。总览须解释这两项真实规则。
- `GridPresentationFrontWorkboard` 已有前厅工作数和粗略桌位摘要；本案补需求分组、真实队列和计分门槛，详细工作仍交给该看板。

## Goals / Non-Goals

**Goals:** 将当前已有事实组成一个低成本营业总览，选择具体订单后复用原阅读目标；过程评价与最终结算明确分开。Solo、Host 本地和远端 Client 观察同一口径。

**Non-Goals:** 不创建经营调度 owner、统计历史仓库或新的倒计时；不自动为订单分配厨房实物，不估计未来订单或完成速度。无配置源、协议、存档格式、Prefab 结构和 CodeBind 改动；无需以结构重构案为前置。

## Decisions

### 1. 同页三块信息，保留原操作入口

在原 `GridOverviewPage` 增加具名总览状态，选择仍归 ModelView 的 `GridPresentationComponent`。HotfixView 的现有分部 System 读取当前投影，在原 `MonoUIGridKitchen.SetInformation/AddTarget` 模板内提供“需求 / 客流 / 得分”三个可切换分区，默认需求区。使用原滚动正文和关闭/返回入口，关闭后恢复原动作分组；不同时打开第二个1101。

菜品行先展开当前具体订单，再由玩家显式选择原 OrderId/CustomerId/TableStationId。复用原订单导航的选择入口；若现有逻辑内联在按钮回调，只提取必要的原选择方法供两个入口复用。返回总览只恢复仍有效的分区和菜品选择，不保留订单对象。前厅区链接已交付的原工作看板，不重新提供工作认领按钮。

选择三块同页而不拆三个 change/查询服务，原因是它们服务同一次营业决策、读取同一投影、共享页面寿命；没有新增彼此独立的领域写入规则。尚未实施的顾客详情/加工看板不作依赖，也不制作无法使用的链接。

### 2. 明确每项统计的输入和分母

| 显示 | 当前事实与计算 | 明确的解释 |
| --- | --- | --- |
| 各菜待交付 | 当前 `Orders` 中 `State == Waiting` 按 DishId 分组 | 一单一次；已贴单未交付仍算待交付，未询问顾客不算订单 |
| 最早剩余期限 | 分组最小 DeadlineTick 减同版 grid.Tick，钳制到零，乘 TickMilliseconds | 归零但 Host 尚未提交超时时仍列 Waiting，不由 UI 擅自改为 Expired |
| 队列占用/上限 | `WaitingCustomers` 身份列表及当前 LevelConfigId 对应的唯一客流 QueueCapacity | 占用包含进场和已等位，分别按 CustomerPhase 解释；不把全部在店顾客都算队列 |
| 桌位状态 | `Tables` 的 Free/Reserved/Occupied/Dirty；关联 Station.ItemId | Free 且有物品标明占用待腾空；不据名义空桌保证寻路或自动入座 |
| 跳过来客 | `SkippedArrivals` | 不是未满足订单，不加入超时计数，不扣分 |
| 分数/订单结果 | `LevelScore/ServedOrders/UnfulfilledOrders` | 使用原提交值，不预加 Waiting 分数，不用跳过来客补算损失 |
| 参考星级/下一分差 | 当前关可信 StarThresholds 中已达到的数量；下一阈值减当前分数 | 仅未结算参考，最高门槛已达到后不显示第四星；完成态使用 LevelStars |

不引入成品扣减需求、预计产量或材料缺口算法：原菜谱与成品检查规划分别处理实物说明，本案无须再建一份“可交付库存”。配置缺失依原配置错误明确显示不可用，不猜默认阈值、队列容量或免费数据。

准备态显示尚未开店及真实零需求，不给原料充足性增加开店门槛。Operating 显示真实营业剩余时间；Draining 明确停止来客且等待原前厅自然排空；Completed 显示本关最终结果并链接原结算页。原 HUD 的 `ScoreText` 在未结算时改为带“参考/未结算”标识的过程说明，Completed 仍显示实际结算星级。

### 3. 实际 owner 树与寿命

```text
Solo / Host Fiber
  Scene(ProductRoot)
    +-- Component: ProcessInnerSender
    +-- Component: ProductRuntimeComponent
    +-- Child: Scene(ProductHost)
    |     +-- Component: HostSessionComponent + Ordered MailBox
    |     |     +-- Child: HostPeer + Ordered MailBox
    |     |           +-- Component: LocalDelivery or RemoteDelivery
    |     +-- Child: Match
    |           +-- Child: RestaurantRuntime
    |                 +-- Component: GridKitchenComponent
    |                 |     +-- Children: GridStation / GridItem / processing
    |                 +-- Child: Level
    |                       +-- Component: FrontLevelState
    |                       +-- Children: Customer / Order / FrontWork / actors
    +-- Child: Scene(ProductClient)
          +-- Component: ClientSessionComponent + Ordered MailBox
          |     +-- Component: GridClientComponent (independent projection)
          +-- Component: GridPresentationComponent (view selection)
          +-- Component: UIComponent
          |     +-- Component: UIGridKitchen (1101)
          +-- Component: GFEntityComponent
                +-- Component: GridKitchenBoard

Remote Client Fiber
  Scene(ProductRoot)
    +-- Components: ProductRuntimeComponent / ProcessInnerSender
    +-- Child: Scene(ProductClient)
          +-- Component: NetComponent
          |     +-- Child: Session
          |           +-- Component: ProductClientBinding
          +-- same ClientSession / projection / Presentation / UI owners
```

树沿 [ProductRuntimeBootstrap](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs) 和 [RestaurantRuntimeSystem](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs) 核对。创建沿原 runtime -> Host 房间/Match -> Restaurant/Kitchen/Level -> 本人就绪投影 -> ProductClient 原 HUD；本案不参与创建领域实体。

页面选择只持有分区、DishId 和适用的稳定身份值，有限分类用具名 enum；不持 Host 实体、池化消息或完整世界副本。换关清本关选择；恢复新绑定和重开 HUD 沿原 EntityRef、HudLoadEpoch、world/level/binding generation 保护。顾客离开后原具体选择失效，不能因为桌位复用自动选下一客。

关闭页面先使本轮显示回调失效，HUD 仍由 UIComponent/GF owner 关闭回收；退出整体运行沿原拆树次序。本端看页不持有额外 Call 等待者，单成员退出不停止 Root 共用 ProcessInnerSender。失联期间冻结最后事实并显示停止更新，不能用本机帧时钟继续扣订单期限。

### 4. 显示数据的发送与接收

```text
Host domain commit
  -> GridBroadcast / CopyFrontState
  -> peer LocalDelivery
       -> Root ProcessInnerSender.Send(ClientSession.GetActorId(), snapshot)
       -> ProductClient Ordered MailBox -> GridSnapshotHandler
  -> peer RemoteDelivery
       -> Session / existing KCP serialization
       -> remote ProductClientBinding -> same GridSnapshotHandler
  -> ApplyGridSnapshot copies and validates identity/version
  -> GridProjectionApplied -> existing Presentation -> overview
```

Host 本地不使用 UDP 向自己发送。`GridSnapshotHandler` 在 finally 释放输入；投影安装独立值后原消息回收不影响本案。UI 不保留消息集合，不读 Host 领域对象，参考星级只是原客户端可信定义与已提交分数的派生值。

总览查看和跳转没有新请求/答复。进入订单页也只设置原阅读上下文；玩家随后点击原显式动作才走原请求路径。以交付为例，原 `ServeOrderRequest` 经本地 sender 或远端 Session 到 HostPeer 的 `ServePeerHandler`，再经 authority ActorId 到 ProductHost HostSession 的 `ServeSubmitHandler`；统一服务步复验并提交，原所属等待与 handler 收尾消息。原 Accepted 不更新总览为已交付，最终沿已提交快照刷新。

继续使用已有 `GridProjectionApplied` 的 ProductClient 消费者，不增加 SceneType.All 订阅。Event 只通知观察，Invoke 保持原根启动/运行职责，不承担总览业务裁决或请求答复。

### 5. 按变更刷新而不新增逐帧全量统计

接入原 `GridPresentationDisplayContext` 与动作/信息失效机制。订单集合、状态或分区改变时重建有限行；纯 Tick 改变只更新可见期限/时钟文本，暂停不更新倒计时。队列、桌位和得分在原同版投影安装后刷新；页未打开时不建立第二份统计镜像。不逐帧复制排序完整厨房，不为本案添加通用缓存框架。

## Risks / Trade-offs

- [分组误导为已分配或已完成] -> 只按 Waiting 计需求，用同菜两单和已绑定饮品验证，原制作/出餐不变结果可复用。
- [队列与桌位重复统计] -> 以 WaitingCustomers 为队列分母，验证进场、等位、入座和满队列跳过，区分桌位状态与可入座条件。
- [参考星级被当成最终奖励] -> 过程/完成双文案，同一次真实交付跨门槛和自然完成验证；不修改 LevelStars 或保存载荷。
- [共享 HUD 文件与旧回调] -> 与现有入口案顺序整合，用真实关切换、池化重开和恢复新绑定验证；不扩大为全仓测试。
- [实际体验未知] -> UTF 证明统计与交互，普通540×960入口确认可读可操作；真人理解、平衡、Player和真机保持未验证。

## Migration Plan

本案无领域数据迁移。先接入原显示派生与页面，再复用导航和更新过程评分文案，最后按 execution-notes 对照最小运行验收并同步玩家用法、根进度及对应已实施主规格。撤回本案总览和评分文案即可回到原界面，不写用户记录、不影响原业务规则。初始规划后已获整批实施授权；构建、提交、推送和归档仍不在本轮范围。
