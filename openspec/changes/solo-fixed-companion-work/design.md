# Design

## Context

见[proposal.md](proposal.md)与[伙伴合同](specs/fixed-companions/spec.md)。提案前没有产品伙伴或前厅工作，当前7/7已完成持续身份、本关Role／工作、真实调度和成长，运行见execution-notes。旧Unit／AI示例无业务数据，未回填；使用原格子移动、统一时钟和本地消息，无新AI框架。

## Goals / Non-Goals

**Goals:** 一位固定伙伴真实移动、排他工作、可被玩家接续，本关实际完成才成长。

**Non-Goals:** 不回填旧 Demo 数据，不建立行为树、招聘或伙伴私有工作副本。最终人数、名字和扩展技能另定。

## Decisions

### 1. 身份与角色分别归属

```text
Main Fiber / SceneType.ProductRoot
  Component ProcessInnerSender / Timer / ProductRuntime
  Child SceneType.ProductHost
    Child Match -> Child RestaurantRuntime
      Child Companion (DefId / stable identity / fixed post)
      Component GridKitchenComponent (shared terrain / Item owner)
      Child Level (business clock)
        Child CompanionRole + Ordered MailBox (position / current work / owned scope)
        Child GridActor (player role)
        Child FrontWork (kind / target IDs / progress / worker token)
        Child Customer / Order (customer change)
        Component CompanionGrowth (counts keyed by CompanionId)
        Component FrontWorkCommands (copied envelopes / no message ownership)
    Component HostSession + Ordered MailBox -> Child HostPeer + Ordered MailBox
  Child SceneType.ProductClient
    Component ClientSession + Ordered MailBox -> Component GridClient (copied role/work values)
    Component GridPresentation / UIComponent / GFEntityComponent
```

先配置、餐厅与厨房，再一次创建 Companion 身份、本关 Role／成长、目标与工作，最后本地绑定和视图。Level 释放角色、工作、成长及角色所属等待，Companion 持续；整体先停调度及入口、关闭等待和视图，再 Stop sender 和树释放。角色 MailBox 用原 ProductHost SceneType，核对 All 消费者，不创造隔离 SceneType。

### 2. 一个工作引擎

FrontWorkKind 为 Ask、Clear、Wash，保存真实 CustomerId／StationId／ItemId、关代次、状态、归一化进度、执行者实例及 ClaimSerial。伙伴和玩家共享认领、到位、推进、停手、完成规则。目标检查失败不认领；合法操作格停驻前不增长进度；执行者离开或退出释放认领，保留同一工作。

耗时在每次合法领取时冻结。归一化进度表示已经做完的比例，每业务 Tick 按该次冻结耗时增长，接手保留比例，不把玩家已有进度清零，也不因换成加速伙伴立即重算整份旧耗时。完成目标效果成功后标记完成，只计一次适用成长。第三项实际完成先更新成长，再处理同一步后续合法领取；自动调度新发消息可在下一服务步认领，不能保证每次自动领取都在同 Tick。

暂停冻结已开始工作，取消尚未开始接近的认领与剩余目标。旧 ClaimSerial 不得释放新工作者。伙伴策略只在空闲时优先最早 Ask，其次最早 Wash；不抢断已开始工作，Clear 留给玩家，既有手工输入提供必要入口。

### 3. 两类发送者、一处裁决

玩家 → ClientSession 所属 FrontWorkRequest → LocalMessage 捕获 Peer ActorId → root sender.Call → Peer MailBox／ProductHost FrontWorkPeerHandler → 可信内部 Submit → authority ActorId → HostSession MailBox／FrontWorkSubmitHandler → 统一步裁决。接收答复走原两段等待表，最终结果随真实投影返回。

伙伴 Role 的所属调度只发一项在途 CompanionWorkRequest，经原 root sender.Call(authority.GetActorId()) → HostSession MailBox／ProductHost CompanionWorkHandler。请求捕获角色 EntityRef、ActorId、InstanceId、Level 关系，handler 从当前关解析可信角色并核对；客户端不能冒充伙伴。handler 不直接完成工作，统一步调用同一工作服务裁决。伙伴角色创建时注册地址和 Ordered MailBox，最终认领／工作通知 sender.Send(Role ActorId) → CompanionWorkResultHandler；答复和最终通知分开。

角色所属值等待负责读取独立响应值，内部物理观察者 finally 回收响应；handler 负责输入，过期 Role 通知回收后忽略。角色退出取消自己的等待，不能 Stop 公共 sender。客户端每 Peer 单独工作快照经 LocalDelivery → ClientSession／ProductClient handler，复制值再回收、发布观察。沿 [消息生命周期](../solo-local-message-contracts/design.md)，不建伙伴请求表。

### 4. 顾客接口的交接顺序

本 change 先提交 FrontWork／Companion 基础模型和排他引擎；顾客 change 随后提供 Customer／Order／Table 目标模型与明确完成接口：Ask 开单、Clear 转移餐具并释放桌、Wash 改卫生并转入干净池。再接伙伴真实调度与成长、顾客剩余流程，最后双方各自验收。不是两个完整 change 互相等待。

完成接口只在 HostSession 统一步内调用，写目标效果成功后才完成工作与加成长；Event 不能触发第二次开单或洗碗。单元场景可设置合法初始目标，但完整伙伴验收必须使用顾客模块生成的真实任务，缺模块不能报告实际服务完成。

### 5. 源配置与可见角色

CompanionConfig 注册 ET Product 源表 c,s,e，含样机身份、岗位、出生位置与基础工作耗时；第三次解锁和 ceil 洗碗公式按已确认合同。沿 active ET Check／生成，修改协议源末尾后正常生成。Model 持身份／角色／工作，Hotfix 持调度。现有受管 GridKitchenBoard 显示伙伴与路线投影，不新增每伙伴 UGF Unit 或 Entity 表隐含前置；完整 HUD 由 UI change 收口。

### 6. 当前模型交接（2026-10-12）

[FrontService.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/FrontService.cs) 已提供 Companion、CompanionRole、CompanionGrowth、FrontWork 和 FrontWorkDefinition；[CompanionSystems.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/CompanionSystems.cs) 在首次安装 Level 时创建固定身份，换关只重新创建本关角色和成长组件。Role 自己持 Ordered MailBox 与 AsyncOwnerComponent，原 Level 释放时取消角色所属等待；公共 ProcessInnerSender 保留到整个运行退出。

顾客模块后续使用 `level.AddChild<FrontWork, FrontWorkDefinition, long>(target, level.Generation)` 创建真实目标；target 含 Kind、CustomerId、StationId、ItemId、BaseTicks。身份引用指向顾客／工位／厨房的唯一物品，不复制其领域状态。FrontWork 保存归一化 Progress、单执行者实例／代次、ClaimSerial 和 FrozenTicks；只有 Completed／Cancelled 不阻塞自然收尾。准备态不自动创建工作。目标验证与完成效果已由顾客目标接口接入，实际认领与进度见下方工作核心接入记录；自动调度和完整服务仍待验收。

CompanionConfig 已在 Product 源表注册 c,s,e，样机一位、岗位 1、出生 `(2,5)`、Ask 30／Clear 20／Wash 61 Tick；奇数洗碗耗时用于后续 ceil 行为验收。产品初始化在创建 Host 前检查身份、岗位、正耗时和可走出生格；继续前按真实地形预检伙伴出生格，失败保留当前关。布局候选也保护实际角色的当前格／本步目标格，并保留伙伴出生格到前厅入口的通路。顾客座位与实际目标路径待顾客模型接入后补齐，工位 change 的对应任务仍未完成。

### 7. 工作核心实际接入（2026-10-12，协议 9）

[FrontWorkMessages](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/FrontWorkMessages.cs) 已实现玩家的 FrontWorkPeerHandler → FrontWorkSubmitHandler、伙伴的 CompanionRequests.RequestCompanionWork → CompanionWorkHandler，以及 CompanionWorkResultHandler。玩家仍使用原 Peer ActorId／两段物理等待；内部 Submit 从 Peer 捕获可信实例和绑定，不能从客户端 Peer 直接进入 authority／NPC handler。伙伴创建请求时捕获当前 Role 实例／关身份和 HostSession ActorId，使用自己的 AsyncOwnerComponent、原 root sender 和至多一项在途标记；后者由唯一物理观察者 finally 收尾，不因取消显示等待提前释放。

[GridFrontWorkSystem](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridFrontWorkSystem.cs) 在 Restaurant 的统一业务步中先处理 Stop、推进角色／合法到位后的工作及目标效果，再处理新 Claim。角色使用厨房同一 Blocked／FindGridPath，玩家复用原 GridActor 路径和格移动；内部移动编号 0 不覆盖客户端业务编号。工作同刻只有一个执行者，同工位／操作格不能双占；目标、关、绑定、执行者实例和 ClaimSerial 重新校验。认领后冻结耗时，到位前不增长进度，目标效果成功后才 Completed 并计适用成长。

停止、合法新移动及成员退出释放适用认领并保留比例；角色退出／同 ID 新实例也不能继承旧认领。暂停取消已接受但未执行请求和接近中的认领，已开始的认领／比例／冻结耗时停钟保留。Role 最终通知经 sender.Send(Role.GetActorId()) 到自身有序 MailBox，校验后只更新观察元数据；工作状态和成长仍由业务步写入。角色 owner 停止后的原物理响应仍回收，迟到请求不能命中同 ID 新实例；整个餐厅先停队列／角色 scope，再按原顺序停公共 sender 和释放树。

第三项实际 Ask／Wash 完成后，同 Tick 后续洗碗领取冻结 `max(1, baseWashTicks / 2 + baseWashTicks % 2)`，与已确认 ceil 公式一致。玩家、Clear、取消、重复请求和重复通知不加成长；旧关结果不进入新关。阶段 UTF 使用合法已入座／脏餐具初始条件，实际认领、路线、Tick 和目标效果均真实执行。自动询问优先／洗碗 FIFO、每 Peer 工作／角色值投影、Board 及顾客整链仍属于未完成任务 2.1／3.1／3.2，不由这些后台结果替代。

### 8. 自动调度与显示实际接入（2026-10-12，协议 10）

顾客从真实源节奏、有界队列和同一厨房路径到座位产生工作，伙伴空闲时按 Ask 优先／同类创建顺序自行发所属本地请求。Restaurant 业务步按顾客时序、前厅停手／进度／认领、下一次伙伴调度、排空判定、一次快照提交执行；新增模块不拥有第二套地图或时钟。

原 GridWorldSnapshot 同版携带角色、工作、顾客、订单、桌、队列、池与 Hygiene。每 Peer 独立建立消息与列表，客户端全部复制后才安装／发布观察，Board 和 HUD 只读值；对象与 UI callback 沿原受管 owner 收尾。没有新增 SceneType 或网络入口。下一次调度／来客时点可作为隔离 fixture 的合法初始条件，产品默认和 liveFront 验收不推迟这些时点。

真实自动来客／询问开单、手动参与与超时离店已验收；完整 Serve、用餐后的原餐具回收和最后洗碗自然关尾尚待对应任务。具体用例、相机渲染、屏幕截图黑帧限制与 NotRun 见 [执行说明](execution-notes.md)。

## Risks / Trade-offs

- [伙伴测试远处直接完成] → 真实消息、路线和业务 Tick，断言到位前无进度与完成目标后的事实。
- [接手改变速度而重置进度] → 保留完成比例，测试玩家接续及奇数基础耗时的 ceil。
- [顾客接口依赖循环] → 按模型／目标效果／真实工作／完整客流分段交接，最终验收仍覆盖完整链。

## Migration Plan

按上述交接顺序串行改共享源，保留普通 Solo 入口。未接目标模块时只验证工作引擎，不勾选真实询问／洗碗任务。验证成本与 NotRun 见 [执行说明](execution-notes.md)。

## Sources

- [伙伴目标](../../../Docs/Product/specs/fixed-companions/spec.md)、[现有格子移动](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridKitchenSystem.cs)。
- [本地 fixture](../../../Unity/Assets/Tests/Editor/ET/ProductSessionTests.cs)、[顾客设计](../solo-customer-order-service/design.md)。
