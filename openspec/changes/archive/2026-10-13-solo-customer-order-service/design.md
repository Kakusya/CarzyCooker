# Design

## Context

见 [proposal.md](proposal.md)、[前厅合同](specs/orders-front-service/spec.md)。本轮仅有汤与吐司餐食，不实施目标规格中的饮品贴单。物品由厨房唯一保管，餐桌、顾客和工作只持真实身份引用。

## Goals / Non-Goals

**Goals:** 从可见来客、询问开单、真实出餐到同一餐具洗净复用的一条单机流程。

**Non-Goals:** 不建货币经济、特殊客群、免费餐具或客户端订单权威；客流和分数样机值不算正式平衡。

## Decisions

### 1. Owner 树与顺序

```text
Main Fiber / SceneType.ProductRoot
  Component ProcessInnerSender / Timer / ProductRuntime
  Child SceneType.ProductHost
    Child Match -> Child RestaurantRuntime
      Component GridKitchenComponent
        Child GridStation (table / wash / dirty pool / clean pool + kitchen)
        Child GridItem (unique reusable bowl / plate and Hygiene)
      Child Companion (stable identity)
      Child Level (arrival schedule / bounded waiting IDs / score)
        Child Customer (position / seated relation / phase / remaining ticks)
        Child Order (CustomerId / DishId / Level generation / state)
        Child FrontWork / CompanionRole / GridActor
        Component CompanionGrowth / LevelBuff (when available)
    Component HostSession + Ordered MailBox -> Child HostPeer + Ordered MailBox
  Child SceneType.ProductClient
    Component ClientSession + Ordered MailBox -> Component GridClient (independent values)
    Component GridPresentation / UIComponent / GFEntityComponent
```

源配置闭包 → 厨房有限餐具与餐桌 → Level 客流时序／角色 → 真实顾客／任务 → 投影。单顾客离开释放关联状态并留下适用脏餐具；单关必须真实排空顾客与必要前厅工作后释放旧 Level。餐桌和餐具随厨房持续，不在换关补库存。整体先停入口／来客／驱动和所有所属等待，再视图、sender 与原树释放。

### 2. 客流与订单

CustomerFlowConfig 规定来客间隔、队列容量、订单等待和用餐 Tick；OrderConfig 规定有效 DishId、得分与样机选择序列，TableConfig 关联真实餐桌工位及座位／操作格。字段只引用实际存在定义，源表 c,s,e 注册并正常 Check／生成。样机两菜可配置轮换，不能把历史 100／50 分写成正式平衡。

Customer 状态为 Entering、Queued、WalkingToSeat、WaitingAsk、Ordered、Eating、Leaving。只在 Operating 按节奏来客；满队列跳过本次，不创建隐藏客户或扣分。已到店队列在 Draining 继续 FIFO 入座。所有走位读取工位 change 的同一地形／路径，不占玩家“两人停驻”名额，认领保护操作排他；完整目标的不可达配置在开局拒绝。

入座生成 Ask 工作，完成时再次核对仍在本关、有效同一顾客和桌位，才创建独立 Order。等待 Ask 只占桌；玩家可以参与询问。订单超时计零并正常离开，无额外惩罚。得分只由合法交付提交一次，不由 UI、Event 或伙伴重复通知增加。

### 3. 真实出餐与有限餐具

Serve 固定 OrderId、CustomerId、TableStationId、ItemId 和关代次，要求玩家在真实桌的合法四邻操作格。统一步完整验证仍有效订单、手持物位置、Dish 内容和兼容干净容器后，一次消费食物内容、清手槽、把原容器移到该桌、完成订单并计分。容器内容不另生成一个可重复交付物件；普通台面不是出餐口。

GridItem 新增明确位置 Table／DirtyPool／CleanPool 与 Hygiene。吃饭期间原容器留桌且占桌；离席将其置脏并生成 Clear，清桌转移到脏池后释放桌位、生成 Wash，洗完原 Item 变干净并移入干净池。每次转移验证原位置和唯一持有关系；脏碗不能再盛汤或装菜。有限池容量覆盖初始可复用餐具总数，不靠超容销毁或生成新碗；完成目标不可提交时保持原任务与物品，不能先计成长。洗碗工位同刻单工作，队列保存 ItemId。未消费食物的离席不制造不存在的脏容器。

### 4. 本地发送、接收和通知

玩家 UI／场景 → ClientSession 所属 ServeOrderRequest → LocalMessage 捕获 HostPeer ActorId → root sender.Call → Peer Ordered MailBox／ProductHost 拟 ServePeerHandler → 可信 ServeSubmitRequest → authority ActorId → HostSession MailBox／拟 ServeSubmitHandler。快速答复返回原两段等待者；业务 Tick 重新验证并提交。

伙伴的 Ask／Wash 及玩家 Clear／Wash 沿 [工作请求路径](../2026-10-13-solo-fixed-companion-work/design.md)，最终效果调用本 change 明确的 Host 内接口。不经过 Event 改订单或餐具。提交后每 Peer 独立 Customer／Order／Table／Hygiene DTO 与 Kitchen／Level 共用提交版本，经 LocalDelivery sender.Send(ClientSession ActorId) → ProductClient 拟 FrontSnapshotHandler → 复制并一次安装普通值 → Dispose DTO → 观察事件。过期世界／关／绑定数据丢弃，仍回收消息。

原 handler 收尾输入，所属物理观察者收尾两段响应；关闭 UI 不撤销已提交出餐。编号、有限重放、await 后实例校验和整体退出依 [消息合同](../2026-10-13-solo-local-message-contracts/design.md)，不新增消息平台或网络监听。

### 5. 收尾与交接

业务步采用流程约定：控制／厨房、顾客时序、前厅工作完成、再排空判定与快照。Draining 停新来客；队列、在店顾客、已生成订单、用餐、离席和 Clear／Wash 都按真实进度走完。前厅工作只查未终结项，完成记录不是永不排空的待办。Completed 不因锅内未完汤阻塞，也不提前把最后脏碗洗完。

先用伙伴 change 的基础模型接入本 change 目标模型和完成效果接口，再交接伙伴推进与成长，之后补完整客流／交付／池／收尾。配置和协议源串行修改，追加原协议末尾并按实际版本生成，不把单机前厅消息自动开放到传输白名单。原 HUD 提供最少询问／交付／清桌入口，完整 UI 后做。

### 6. 当前目标接口交接（2026-10-12）

[CustomerService.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/CustomerService.cs) 已提供 Level 的 FrontLevelState／Customer／Order、工位的 FrontTable 和物品 Hygiene 值；[目标接口](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridFrontTargets.cs) 提供 `InstallFrontTables`、`CheckFrontTarget(level, work)`、`CompleteFrontTarget(level, work)`。餐桌组件归厨房持有的原工位，不随关重建；客流时序、队列、顾客与订单归当前 Level，只有 Gone 顾客不阻塞收尾。

伙伴／玩家工作引擎领取和推进前核对目标；进度达到 1 后调用完成接口。接口重新检查当前关、目标关系、工作状态、进度及暂停／阶段：Ask 只给同一桌同一已入座顾客开独立 Order，Clear 只把原脏餐具移入有空位的脏池并生成一个 Wash，Wash 只把原 ItemId 变干净并移入有限干净池。接口成功后由工作引擎终结 FrontWork、释放执行者、再计算适用成长；接口本身不加分或计成长。池满返回 `PoolFull`，原关系、物品、进度和工作保留；不提前终结或生成免费餐具。Event 不调用这些提交方法。

Source InitialLayout 现在 15 个工位、16 格占地：两桌 `(1,3)`／`(5,3)`，座位相对偏移 `(1,0)`／`(-1,0)`；洗碗台 `(3,5)`、脏池 `(1,5)`、干净池 `(5,5)`，两池各 4 槽。原初始一锅、两碗、一盘和材料保持。TableConfig 通过 InitialLayoutId 关联原运行 Station；移动／旋转时以候选位置重新计算座位并预检可达性，失败不提交。

CustomerFlowConfig 按 LevelConfigId 关联两条关卡，包含首客延迟 20、间隔 60、队列 2、订单等待 200、用餐 60 Tick、入口 `(3,1)`、队列格 `(3,2)`、OrderConfigIds `1,2`；OrderConfig 分别关联真实汤和吐司，每单 10 分。这些仍是样机值。启动在创建 Host 前验证所有引用、菜单、座位和有限池容量。当前尚未驱动自动来客、玩家 Serve 或工作认领；目标接口 UTF 的已入座／已离席初始条件只证明接口，不代替真实服务链。

### 7. 实际客流与同版投影（2026-10-12，协议 10）

顾客从真实源节奏、有界队列和同一厨房路径到座位产生工作，伙伴空闲时按 Ask 优先／同类创建顺序自行发所属本地请求。Restaurant 业务步按顾客时序、前厅停手／进度／认领、下一次伙伴调度、排空判定、一次快照提交执行；新增模块不拥有第二套地图或时钟。

原 GridWorldSnapshot 同版携带角色、工作、顾客、订单、桌、队列、池与 Hygiene。每 Peer 独立建立消息与列表，客户端全部复制后才安装／发布观察，Board 和 HUD 只读值；对象与 UI callback 沿原受管 owner 收尾。没有新增 SceneType 或网络入口。下一次调度／来客时点可作为隔离 fixture 的合法初始条件，产品默认和 liveFront 验收不推迟这些时点。

真实自动来客／询问开单、手动参与与超时离店已验收；完整 Serve、用餐后的原餐具回收和最后洗碗自然关尾尚待对应任务。具体用例、相机渲染、屏幕截图黑帧限制与 NotRun 见 [执行说明](execution-notes.md)。

### 8. 当前真实出餐提交（2026-10-12，协议 11）

新增 ServeOrderMessages／GridServeSystem 沿现有 owner 树，不新增 Fiber 或 SceneType。客户端原 UI 所属 AsyncOwner 发送 ServeOrderRequest；LocalMessage 取 HostPeer.GetActorId，ProcessInnerSender.Call 交 ProductHost 的 ServePeerHandler／Peer Ordered MailBox，再把实际 Peer Id／InstanceId／Generation 写入 ServeOrderSubmitRequest，发 authority.GetActorId 到 HostSession Ordered MailBox／ServeSubmitHandler。Submit 直接命中 Peer 时返回 InternalOnly，内部入口重新核对可信 Peer 和原共用业务编号／完整载荷。两段原 handler 回收输入，物理等待者 using 回收响应；UI await 后校验原实例和世界／绑定／关身份。

快答只入 FrontLevelState 的普通值 Serve 队列。Restaurant 原控制／厨房后先推进顾客时序，再 ApplyServe、前厅工作、伙伴调度与排空；到期订单先结束。ApplyServe 完整核对订单／顾客／桌／关、手持物／卫生／真实操作格、状态和菜品／菜单，全部成功才消费食物内容、原容器移桌、订单和分数一次提交。随后原 GridWorldSnapshot 以同版独立 DTO 返回客户端，观察事件仅呈现提交事实。暂停取消排队 Serve，已提交的用餐与餐具过程停钟；整体退出先取消队列再关闭所属等待和 sender，不由单玩家停共用 sender。

原 TakeCounter 的干净池分支固定 ExpectedItemId 并验证唯一成员、原 Holder 和 Clean；其他特殊前厅工位拒绝普通物品互动。装料、手工制作、倒料、煮汤和盛汤统一拒绝脏容器。清桌／洗碗仍调用原目标完成接口，厨房唯一持有餐具；移动池时重新检查成员并更新同一 Item 的位置。行为及接受的未验证范围见 [执行记录](execution-notes.md)。

## Risks / Trade-offs

- [旧桌号请求命中新顾客] → 顾客、订单、工位与代次全部固定，UTF 重用同桌再发旧请求。
- [重复交付或洗碗多计分／多产碗] → 一次提交位置、内容与订单，完整流程检查同一 ItemId 和总量。
- [提前结算掩盖未做工作] → 断言最后 Wash 未完成仍 Draining，并从真实入口完成后结算。

## Migration Plan

上述五部分配套最小行为 UTF，最后真实 Launcher Solo 走“备料做汤→伙伴询问→交付→用餐离席→玩家清桌→伙伴洗碗→同碗取回”。未跑完整服务链不能报告产品完成。细目见 [执行说明](execution-notes.md)。

## Sources

- [目标前厅](../../../../Docs/Product/specs/orders-front-service/spec.md)、[产品设计](../../../../Docs/Product/design.md)。
- [现有物品与消息](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen)、[实际 UTF fixture](../../../../Unity/Assets/Tests/Editor/ET/ProductSessionTests.cs)。
