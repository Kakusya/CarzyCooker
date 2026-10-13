# Design

## Context

动机见 [proposal.md](proposal.md)。本案按用户确认采用产品功能边界，不把每个 C# 方法变成消息。当前代码已有两段 Ordered MailBox 请求、唯一 Restaurant 服务步和独立客户端投影，具备迁移基础：

- `GridMessages.cs` 的 `GridBroadcast.SendCommitted` 已先发快照，再发 `GridCommandResult`；它证明部分终点已经消息化，不能重复加一条执行路径。
- `Level/StationLayout/Buff/Supply/RenovationMessages` 已有请求，但一些最终结果只更新 Peer 最近结果并进入快照。
- `GridBakeSystem.AdvanceBakeWork` 直接生成成品；`GridCustomerFlow` 直接创建询问／清桌；`GridFrontTargets` 直接开单和生成洗碗；`RestaurantRuntimeSystem` 直接切 Draining／Completed。这些是本案的主要衔接迁移点。
- `CompanionWorkResult` 已走角色邮箱；`SoloSaveRepositoryMessages` 已使用 ProductRoot 的本地 RPC。保留和补齐这些入口，避免重新包装一套系统。
- `ProcessInnerSenderSystem.HandleMessage` 找不到 MailBox、`MessageQueue.RemoveQueue` 移除待投递队列、Ordered MailBox 等锁后实例失效等路径尚缺显式输入回收。新通知依赖这些路径，需最小修正。

2026-10-13 后续规划核对时源码协议为 17，工作树有其他任务的未提交改动；实施取届时最新基线。存档与合作核心已有各自原案验收，具体范围以原 execution-notes 为准，不作为本案迁移后通过的证据。Model 与 Hotfix 位于现有 `Game.ET.Code.Model/Hotfix`，均按 asmdef 保持 `noEngineReferences`；Unity 表现仍在 ModelView/HotfixView。

## Goals / Non-Goals

**Goals:** 任何纳入清单的产品功能均可回答“谁发起、哪个邮箱接收、谁提交、谁收到终态”；所有跨功能写入经消息接入同一裁决；逐功能运行验收。

**Non-Goals:** 每帧／每属性消息化、替换 ET Event/Invoke 底座、搬迁领域 owner、创建消息总线或第二时钟、跨进程可靠投递、事件溯源、自动重试、全历史结果库。框架根建树和无消息泵时的强制清理保留直接路径；UI 打开关闭、资源加载、镜头、纯查询和计算不属于产品动作。未来玩法仍由原 change 授权。

## Decisions

### 1. 以业务效果划分功能，保留一次提交的完整性

“起点”是请求执行一项产品行为；“终点”是这项行为已提交的成功、拒绝或取消。长作业另外具有开始与结束事实，不能把开始命令的 Executed 当作作业 Completed。终态消息在效果提交后发送；若要启动另一功能，由接收者再发该功能请求。

同一功能里的校验、寻路、进度计算、物品转移及必须一起完成的状态修改仍直接调用。例如交付一次提交物品内容、订单完成和得分；不把每个字段修改拆成消息，否则会失去已有原子性。真正的后继功能，如“顾客离开后开始清桌”“清桌完成后生成洗碗”，必须经过消息。

已有明确类型的 GridAction 等动作族继续使用；新内部请求按领域族定义，不使用字符串路由或任意 object payload。以下名称是拟议消息族，实施时可按现有生成风格命名，但不能省略发送／接收端。

| 功能族 | 起点及发送者 | 接收与终点 | 当前迁移位置 |
| --- | --- | --- | --- |
| 会话与成员 | Client 的 Join／Probe／Leave；可信入口的 Close | 原 Peer→HostSession；原回执、名单及关闭通知 | ProductClientSystem、ProductAuthority、ProductPeerSystem；保持已消息化路径 |
| 移动与实物 | UI／Board 发 Grid 请求；含拿、放、掉落拾取、装容器、倒入、分汤 | 原 Peer→HostSession→厨房；逐命令结果，移动到达或替换取消 | GridKitchenSystem、GridMessages |
| 手工、煮汤、烘烤 | 玩家开始／停手请求；进度 owner 到点发 KitchenWorkTransition 请求 | HostSession 接入；厨房提交 Started、Paused／Resumed、Finished 或取消事实 | GridPrepSystem、GridSoupSystem、GridBakeSystem |
| 顾客与订单 | 前厅时钟发 CustomerFlow 请求；到座、订单到期、用餐结束／离场到达发转换请求 | HostSession 接入；Level 下原 Customer／Order 提交结果，完成通知再请求后继工作 | GridCustomerFlow、GridFrontTargets |
| 玩家／伙伴工作 | 原 FrontWork／CompanionWork 请求；到位、停手、作业到点发工作转换请求 | 同一工作引擎；认领、释放、完成结果；伙伴成长随该次完成原子提交 | FrontWorkMessages、GridFrontWorkSystem、GridCompanionSchedule |
| 出餐、清桌、洗碗 | Serve 请求；顾客离场通知→清桌请求；清桌完成通知→洗碗请求 | Level／厨房原 owner；出餐一次计分，原餐具移转，洗净后完成通知 | GridServeSystem、GridFrontTargets、GridFrontWorkSystem |
| 水与垃圾清理 | FillWater／DrainLiquid／DiscardSolid 原 Grid 请求 | 厨房提交一次内容变化和明确结果 | GridCleanupSystem |
| 供应全链 | Purchase／Receive／TakePortion 原请求；货物到点发 SupplyTransition 请求 | KitchenSupply／Shipment；采购、到货、接收、拆份各有结果 | SupplyMessages、GridSupplySystem |
| 开店、暂停、换关 | 原 Level 控制；营业时钟到点与自然收尾条件满足发 LevelTransition 请求 | Restaurant 服务控制步；阶段切换及换关结果 | LevelMessages、RestaurantRuntimeSystem、GridLevelLifecycle |
| 布局与 Buff | 原 Layout／ChooseBuff 请求；新关已建立通知触发本关候选准备请求 | 原厨房／LevelBuff；布局或候选／选择提交结果 | StationLayoutMessages、BuffMessages、LevelBuffSystem |
| 装修 | 结算完成通知→准备候选请求；原 ChooseRenovation 请求；Continue 内消费已锁定计划 | 原 RestaurantRenovation；候选、选择和换关安装结果 | RestaurantRenovationSystem、GridRenovationLayout |
| 已有单机存档 | 选档 UI 的 Browse／Select；SaveAndContinue→结果 owner 的 Write；文件完成请求 | ProductRoot repository 答复；HostSession 接入→Restaurant 提交保存门槛及换关结果 | SoloSaveRepositoryMessages、SoloSaveCapture 及原 change 的最终入口 |

加入、同步查询、选档等单次请求的 Response 可同时是其终点，无须额外复制等价通知；耗时的游戏工作必须有独立结束消息。加载结果导致合法新运行时，原建树 API 仍是受控基础设施，建树完成后业务从消息开始。

### 2. 沿用实际 ET 树和稳定邮箱

```text
Product Fiber (existing; root SceneType.ProductRoot)
  Components: Timer / CoroutineLock / ProcessInnerSender / ProductRuntime
  Component: SoloSaveRepository + Ordered MailBox [checkpoint change]
  Child Scene: ProductHost (SceneType.ProductHost; same Fiber)
    Child: Match
      Children: Participant
      Component: GridKitchenComponent [仅历史夹具兼容/防双驱动检查，不是当前 Host 主路径]
      Components/Children: SoloSaveBinding / SelectedSaveBaseline / SaveResult [checkpoint]
      Child: RestaurantRuntime
        Children: Companion (persistent identity)
        Component: GridKitchenComponent
          Children: Station / Item / ManualProcess / AutoProcess / BakeProcess
          Component: KitchenSupply -> Children: SupplyShipment
        Component: RestaurantRenovation
        Child: current Level
          Children: GridActor / CompanionRole (+ Ordered MailBox) / Customer / Order / FrontWork
          Components: FrontLevelState / FrontWorkCommands / CompanionGrowth / LevelBuff
    Component: HostSessionComponent + Ordered MailBox
      Children: HostPeer + Ordered MailBox
        Components: LocalDelivery or RemoteDelivery
    Component: NetComponent -> Children: Session [existing Host only]
  Child Scene: ProductClient (SceneType.ProductClient; same Fiber)
    Component: ClientSessionComponent + Ordered MailBox
      Component: GridClientComponent (independent values)
    Components: LocalMessage / GridPresentation / UIComponent / GFEntityComponent
```

Solo 与当前 Host 均使用 Restaurant；`cooperative-restaurant-core` 已迁移 Host，原 Match.GridKitchen 残余只按实际夹具/兼容检查处理，不恢复为产品主路径。上图树关系以对应 Model attribute／创建语句为准；不把引用画成 owner。

根 sender、目标实体和 MailBox 先同步建好，再发首个 Join／业务初始化请求；FiberInit 不等待依赖尚未安装 Runner 的队列答复。恢复载入先校验，再建新树；运行身份仍重新生成。

玩家通过当前 `LocalMessage.Peer` 取得 Peer ActorId；Peer 从真实 Authority 引用取得 HostSession ActorId。自动流程从本轮 Restaurant/Match 对应 authority 取得稳定地址，使用原源实体身份，不接受外部传入任意 ActorId。内部请求只在 ProductHost 的 HostSession 接收，Peer 和 wire 均拒绝；repository 只在 ProductRoot 接收，客户端结果只在 ProductClient 接收。

不为每个 Item／Process 增邮箱。自动源实体可能退役，稳定 HostSession 接收后检查原源实体／业务阶段；给角色的观察通知使用原 CompanionRole 邮箱。SceneType 保持 ProductRoot／ProductHost／ProductClient；核对现有 SceneType.All 消费者，不以类型旗标假定完全隔离。

### 3. 四条消息路径

**玩家命令：** `UI owner → ClientSession.CallProductValueAsync → root sender.Call(Peer ActorId) → Peer Ordered MailBox／对应 PeerHandler → root sender.Call(Authority ActorId) → HostSession Ordered MailBox／SubmitHandler → 原领域待处理队列 → 快速 Response`。下一允许服务步复验并提交，生成最终结果。保留两段 RpcId 及各自答复；业务编号由 ClientSession 统一分配。

**自动转换：** `Process/Customer/Shipment/Level 的 Tick 检测器 → sender.Send(Authority ActorId, 具名 TransitionIntent) → HostSession Ordered MailBox／领域 TransitionHandler → 原 owner 待处理值 → 下一允许服务步校验和提交 → 具名 Outcome`。检测只安排一次待处理转换，不提前创建成品或完成订单；待处理身份归原 owner，不建立全局调度表。接收 handler 不等待整段作业，也不向自身 Ordered MailBox 发 Call 并 await。

**功能衔接：** `原功能提交 → sender.Send(Authority ActorId, 具名完成通知) → HostSession／对应完成 Handler → sender.Send(Authority ActorId, 后继功能请求) → 后继请求 Handler 入队 → 后续允许服务步提交`。例如顾客离场→清桌、清桌→洗碗、Level 完成→装修候选。一个阶段转换标识只启动一次后继；完成通知 handler 不直接完成另一功能，也不重做原功能效果。

**客户端与保存：** `领域提交 → 每 Peer 独立快照 → ProductDelivery → LocalDelivery sender.Send(ClientSession ActorId)／原 Session 传输 → ProductClient SnapshotHandler 安装 → 具名结果 Handler → 本地观察事件 → UI`。存档 `SaveResult.AsyncOwner → sender.Call(repository ActorId) → ProductRoot repository Handler → 原 Reply → 值复制与响应 Dispose → 原 SaveCompleted 请求至 HostSession → Restaurant 服务步`，不能由文件 continuation 直接切关。

### 4. 终点身份与观察

- 保留 GridCommandResult 的原有单一发送点；其他已接收的动作族补明确终态消息，可共用具名 `ProductCommandOutcome` 值合同（Kind、CommandId、Status、Reason），不能只靠最近命令快照字段猜结果。
- 长作业使用领域消息族，如 KitchenWorkStarted／KitchenWorkFinished、FrontWorkFinished、ShipmentReady、LevelPhaseChanged。结果含明确领域身份和本次转换标识；Paused 不是 Completed，正常暂停保留作业身份和进度。
- 玩家命令携原 Match／World／适用 Level／绑定代次和 CommandId。自动作业携 World、源业务 Id／InstanceId、阶段序号；关内对象再带 Level。持续厨房 Process／Shipment 跨关仍存活，不因创建它的旧 Level 结束而丢弃合法当前完成；旧 UI 命令结果则不能更新新关。
- 完成通知仅携独立值，不引用活 Entity 或回收后的 DTO。源已销毁时可由稳定 owner 保存本次待发送值直到交接；收件端按当前运行与终态阶段防重，不保存无限历史。
- 对仍有效的接收者，每次实际终态只产生一次业务通知；重放原请求返回既有事实，不再次生成物品、加分或启动后继。取消 UI 等待不等于取消已启动自动加工。
- 正常同端交付先安装对应 Version 快照，再消费同提交结果；结果与快照身份不符时不触发表现成功。快照仍供首次加入／重开界面还原当前状态，不能用“只收到最新快照”证明所有阶段通知均已发出。

### 5. 统一服务步与衔接间隙

保留 `AdvanceRestaurantStep` 与 `AdvanceGridTick`，不发送逐帧 Tick 消息。handler 只接入，领域效果只在原业务服务步执行；会话控制和文件 repository 各保留原有有序提交职责。一个服务步提交完毕后统一发同版快照和终态消息。

转换到点时发消息，效果在消息到达后的允许服务步发生；功能串接因此允许经过后续消息轮次，不保证原直调链在同一个 Tick 全部完成。RequiredTicks、已用进度与规则不改，到点等待转换时不反复累加、发同一请求或重新开始计时。接受这项有限调度变化，不引入手动重入消息泵以追求同 Tick。

控制泵在暂停和 Completed 仍运行，恢复／退出／保存完成可处理。自动业务转换暂停时保留，不提交效果，恢复后继续；尚未执行的玩家意图仍按既有暂停合同取消。清理、取消及控制优先于本步业务转换，旧轮转换不复活已取消动作。

自然收尾检查真实前厅活动以及其待衔接阶段。顾客离场到清桌、清桌到洗碗之间，原领域对象保留“待后继接入”阶段，直到后继成功接入或明确取消；不因临时没有 FrontWork 就提前 Completed。该阶段是原对象生命周期的一部分，不另建 pending 总数来代表世界。伙伴第三次有效完成和成长仍一次提交；随后收到的洗碗认领读取已提交成长并冻结耗时。

新关建树可直接初始化字段；依赖消息生成的 Buff 等业务准备未结束时，开店门槛保持未就绪。Continue 的终点是新关所需准备已提交，不能只因新 Level 指针赋值就报完全就绪。装修实物安装仍与预检后的换关一起提交，保持原“失败零安装”。

### 6. 所有权、退出与公共缺口

原 `AsyncOwner`／`CallLocalValueAsync` 持有调用等待。未交接的同步失败由发起边界释放输入；正常入队后由 handler finally 释放输入，响应由原物理观察者转值后释放。单 owner 退出只取消显示等待；Peer.InFlight 到物理答复／超时才减少，公共 sender 不停止。

新增通知复用生成消息和原 sender.Send。每个收件人独立消息；发送阶段未接收、找不到 mailbox、Ordered 等待后实例失效、整队列被停止撤下时，必须由尚持有输入的层恰好释放一次，交给 handler 后不重复释放。必要修正限定现有 sender／queue／ordered invoker，覆盖输入及关联失败响应；不改 RPC 成功／取消／超时语义，不新增重试或跨 Fiber 协议。失效 invoker 使用捕获的 sender EntityRef 收尾，不从已销毁 MailBox.Root 找新对象。

业务正常取消先提交取消结果并发送。整个运行技术退出仍先拒绝新工作、使 owner 等待无效，然后 sender.Stop、拆树；不能为等待每个终态消息被读到而依赖未来帧。停止后不承诺消息交付；明确本地 RuntimeStopped／取消与结果未知，不伪造游戏成功。单成员退出不停止公共 sender；持续作业按原规则保留。

ET Event 仅作已提交状态的本地表现观察；Bind 只写表现。Invoke 保留 Fiber 初始化、MailBox 分发和 GF 桥；不要求修改 ET 内部赖以工作的方法。正常业务不得用 Event/Invoke 绕过功能请求。

### 7. 源生成、兼容与现有 change

协议只改 `Design/Proto/ET-Client` 源，按当前排序在末尾追加新类型，经既有 Proto2CS 生成 Client／ClientServer；旧 Opcode 不重排。内部类型即使有 Opcode 也不自动成为网络允许消息。协议版本按实施时真实基线升级；不将本次观察到的 17 写死为永久起点。

本案不需要 Excel 新字段、CodeBind 或 Prefab 结构变化。HotfixView 只对接原受管入口和已安装投影；若实施发现确需结构或配置行为改变，应单列原因与范围，不混进本案。

`solo-success-checkpoint` 原案功能与验收已交付，本案可在核对实际最终入口后迁移其消息边界；其他不冲突功能也可分组实施。不得借本案补做未授权存档能力。合作餐厅四案继续持有其远端功能与验收归属；实施前核对双方触及文件，使用最新实际入口，不恢复旧直调。本文不修改它们的 tasks 或冒领验收。

### 8. 验收安排

复用 `CarzyCooker.ET.Tests` 中 `ProductSessionTests` 的 StartSolo／PumpMessages／领域步与现有 Solo* 行为用例。每项迁移必须观察真实队列的入口和终点，并断言物品、阶段、得分等效果；仅测试 DTO 字段或直接调用 handler 不算通过。优先扩展现有用例；新增的 `ProductMessageBoundary*`／`LocalMessageOwnership*` 为拟议命名，不声称已发现用例。

执行阶段先通过 Tools/AutoTesting 发现真实 PlayMode FullName，再按 [execution-notes.md](execution-notes.md) 的功能对应选择最小集。消息收尾增加缺 mailbox、锁等待退出、撤队列及单 owner 取消几个代表性测试，不建立故障平台。已有本地／真实 KCP Join／Leave 与内部消息拒绝用例各选必要小集；不扩展网络玩法或双 Editor。

普通 Launcher 从新轮准备开始，经过两菜加工、前厅询问出餐、原餐具清洗、供应清理、暂停恢复、自然结算、装修／保存和下一关，记录每类功能真实操作及消息结果。一个完整流程可以覆盖多项；UI 关闭重开验证单击只发一次和旧回调失效。已有与本案改动无关的配置验证可复用；玩法输入、消息路径或提交时序改变的证据不能复用旧通过。

## Risks / Trade-offs

- [完成通知重复导致两份成品或二次计分] → 原 owner 按实例和阶段提交一次，观察者只读，后继请求按原阶段防重。
- [消息衔接间隙提前结束关卡] → 原对象保留待后继阶段，专测离场／清桌／洗碗三个衔接及自然收尾。
- [自动作业跨关后被当作旧命令拒绝] → 区分持续世界作业与关内角色／命令身份，检验同一锅和货物继续完成。
- [修改公共丢弃路径引入二次回收] → 明确交接点，代表性计数断言覆盖已交 handler 和未交 handler 两侧。
- [调度延后改变体验] → 接受后续消息轮次，保持配置与逻辑时长；普通入口确认明显停顿或卡住时再定位，不预设性能百分比。

## Migration Plan

先落源协议／输入收尾与公共结果合同，再迁移动作族、长作业、前厅衔接及关卡，最后接存档和现有 UI 消费。每个动作族在自己的唯一提交点切换，迁移中不同时广播两种等价终态。

当前仅创建 change。实施使用原工作树最终基线；回退只回退本案已明确的源码／协议源和相应生成结果，不重置其他任务修改，不删除用户存档。最终同步相关网络／响应式约定和进度说明；归档／提交另按授权执行。

## Sources

- [现有产品主规格](../../specs/product-session/spec.md)、[网络](../../specs/network/spec.md)、[关卡](../../specs/restaurant-level-lifecycle/spec.md)、[伙伴](../../specs/fixed-companions/spec.md)。
- [产品源码](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product)、[Model](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product)、[UI 接入](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product)。
- [原本地消息验收](../archive/2026-10-13-solo-local-message-contracts/execution-notes.md)、[存档设计](../solo-success-checkpoint/design.md)。
- [发送器](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Actor/ProcessInnerSenderSystem.cs)、[队列](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/World/Module/Actor/MessageQueue.cs)、[有序邮箱](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Module/Actor/MailBoxType_OrderedMessageHandler.cs)。

## 本轮已实施的厨房、供应和前厅消息名称

厨房到点使用 KitchenWorkTransition，提交后使用 KitchenWorkOutcome 的 Started／Paused／Resumed／Finished。供应到点的最终名称为 ShipmentTransition（取代规划表中的 SupplyTransition），采购／Ready／Received 使用 ShipmentOutcome；Receive／TakePortion复用原Grid寻路并保存OriginKind=Supply，终态仍是ProductCommandOutcome。

顾客阶段使用 CustomerFlowTransition 的 Arrive／ReachedQueue／AdmitSeat／ReachedSeat／OrderExpired／FinishedEating／ReachedExit。源为原FrontLevelState、Customer或Order，关内身份含当前Level和原Source InstanceId／TransitionSerial。到座与离场提交后发送CustomerFlowFinished；原owner接收完成值后发送FrontWorkCreateRequest(Ask/Clear)，原FrontWorkCommands接入并在后续业务步创建唯一工作。询问完成与订单创建是该询问功能的一次原子效果，不另建订单状态权威。

前厅认领及停手仍走已有 FrontWorkRequest／CompanionWorkRequest 两端，原Approaching到位只发FrontWorkTransition(Start)，进度到点只发同族Complete。停止或旧worker退役提高原工作serial；收到旧ClaimSerial／serial不完成新认领。完整目标效果和伙伴成长仍在同一完成提交内，第三次成长先于该业务步后续认领冻结洗碗时长。提交后另发FrontWorkOutcome的Claimed／Started／Released／Finished／Cancelled，同时保留CompanionWorkResult到原角色邮箱。

清桌只提交原餐具从桌面到DirtyPool，不同步创建Wash；完成后FrontWorkFinished到稳定HostSession，原队列接收完成值后发送FrontWorkCreateRequest(Wash)，后续业务步创建唯一Wash。Customer从WaitingAsk及Gone分别持有本阶段SuccessorPending；Clear完成后持有SuccessorPending，直到合法后继请求创建成功再清除。ILevelFrontActivity把该pending计入未收尾活动，因此每个投递间隙仍不能自然Completed。新阶段重置SuccessorQueued，serial负责旧阶段隔离，领域实体负责pending所有权；没有新增通用调度表、事件业务入口或消息时钟。

本节仅记录已接入的消息路径，全部任务验收以tasks与execution-notes为准。当前各具名类型均已有发送和接收路径；尚未完成的功能与真实验收仍以tasks为准。

Buff准备使用原LevelBuff的Frozen allowedIds／count／seed和阶段serial；Awake只校验，DrawVersion0表示未准备。当前稳定HostSession建立后，由原Restaurant准备态服务步发BuffPrepare，原owner收到值后下一个允许步抽候选一次；DrawVersion1与候选同版安装后接收BuffPrepared独立值。Start门槛先检查Prepared，再检查原RequireChoice／SelectedId；暂停不抽取，旧Level/Instance/serial拒绝，选择锁定与布局原子规则保留。没有新增UI结构；准备期显示“本关加成准备中”，旧按钮仍冻结原关和draw身份。

装修结算后继新增LevelFinished（只在内部HostSession接收），Source为原Completed Level，CompletionSerial独立于控制命令。原Restaurant.FinishedTransitions消费后发送RenovationPrepare，原RestaurantRenovation.Preparations在下一未暂停的Completed服务步创建候选一次；生成ResultLevel身份、ChoiceVersion并标Prepared，再经同版快照／RenovationPrepared报告。通知或请求尚待投递时不能Continue或捕获存档，空候选也需实际准备完毕。新关准备未完成时UI显示准备信息并暂不提供下一关；Host/Client保留原不参与Solo装修的控制范围。原选择、完整预检、Continue内部一次安装以及持久AppliedStations不另建新状态权威；HUD关闭不停止准备，旧Level／Instance／serial拒绝，准备重放不刷新ChoiceVersion。

关卡到点使用LevelTransition(Drain/Complete)：Level保存TransitionSerial与单项Queued，Elapsed到点夹止；原稳定HostSession接收值，Restaurant在原控制步之后复验世界／Level实例／serial，暂停不消费转换。Drain提交后仍走原前厅活动；Complete复验所有ILevelFrontActivity的真实活动与后继pending，一次取消未执行动作、冻结手工、计星并发送LevelFinished。收到转换才切阶段，不为保持旧同Tick重入消息泵。过营业截止等待Drain时不新增顾客，既有前厅和世界作业保持原规则。

Start／Pause／Resume及实际Preparing／Draining／Completed由原Restaurant收集LevelPhaseOutcomeValue，GridBroadcast先同版快照再逐收件人独立LevelPhaseChanged；ClientSession仅复制阶段观察值并防重。控制命令继续ProductCommandOutcome，Continue成功保留OriginLevel并适用于新Level；旧关转换和结果不能更新新轮。Completed的Pause仍按原契约拒绝LevelCompleted，拒绝仍有明确终态，不新增产品暂停规则。所有新增结果类型只加入Client通知白名单，内部转换／完成／创建请求仍不开放Host wire或Peer直达。

最终Continue就绪结果：新Level.PreparationControl保存原LevelControlEnvelope（原Context与Peer EntityRef），建树后状态仍Accepted；原BuffPrepare提交Prepared后，Restaurant控制步发唯一ChangedLevel Executed，先同版快照再终态。准备期间精确重放回Accepted不重建Level，保存完成后的原Continue也共用此路径；原Peer失效不作用新绑定，Stop不等待后续投递。
