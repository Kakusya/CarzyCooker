# Design

## Context

见[proposal.md](proposal.md)和[关卡增量](specs/restaurant-level-lifecycle/spec.md)。迁移前Match.Children只有Participant，厨房是Match Component且独立IUpdate；相关固定父层查询和全Child成员枚举已随本次迁移处理。当前正式Solo采用下述实际树，旧Host／Client临时样机保留原厨房owner并由HostSession驱动。

长期目标要求 Kitchen 与 Level 并列。本轮用户明确暂停取消未执行动作、已开始工作停钟，以及前厅按真实进度自然收尾。先落内存单机餐厅，不创建 LoadedSave 或空持久化实现。

## Goals / Non-Goals

**Goals:** 一份持续厨房、一份当前关、一个统一步驱动和可操作的暂停／换关。

**Non-Goals:** 不建立第二份世界、独立关间准备 owner、存档／领取凭证、菜单缩小裁决或联机餐厅。UI 完整结构由 UI change 负责，流程只在原 HUD 提供必要控制。

## Decisions

### 1. 目标 owner 树与迁移

```text
Main Fiber / SceneType.ProductRoot
  Component ProductRuntime / Timer / CoroutineLock / ProcessInnerSender
  Child SceneType.ProductHost (Solo)
    Child Match
      Child Participant... (member identity)
      Child RestaurantRuntime (persistent run / single step driver)
        Component GridKitchenComponent (persistent Kitchen authority)
          Child GridStation / GridItem / GridManualProcess / GridAutoProcess...
        Child Level (phase / generation / business clock / score)
          Child GridActor... (PlayerRole / hand relation)
          Child CompanionRole + Ordered MailBox + AsyncOwner
          Child Customer / Order / FrontWork (current front activity)
          Component FrontLevelState / FrontWorkCommands / CompanionGrowth / LevelBuff
        Child Companion (stable identity / fixed post)
    Component HostSession + Ordered MailBox
      Child HostPeer + Ordered MailBox
        Component LocalDelivery
  Child SceneType.ProductClient
    Component ClientSession + Ordered MailBox
      Component GridClient (copied Kitchen + Level values)
    Component LocalMessage / GridPresentation / UIComponent / GFEntityComponent
```

RestaurantRuntime 归 Match；厨房继续用当前 GridKitchenComponent，不新写另一套 Item／Process。GridActor 移到 Level，Kitchen.Actors 只保留 EntityRef 索引，不再声称只索引自己 Children；Item/Station/Process 坚持厨房 Child。Kitchen 捕获 Restaurant/Match EntityRef，用明确关系取 Participant，不按固定父层级推测身份。Match.Children 的 Participant 枚举改为按类型／明确注册获取，不能 Cast 所有 Child 后把 Restaurant 当成员。

创建顺序：实际 Tables → 根 Sender → Match → Restaurant＋一次初始厨房 → Level Preparing／角色 → HostSession／Peer／Join → 同版投影 → 原受管 Board/HUD。不复制一次标准物资到每个 Level。整个运行退出先停止入口与驱动、关闭视图和所属等待，再 sender.Stop 与原树释放；单关结束只释放本关对象，单成员退出仍不 Stop 公共 Sender。

### 2. 只有一个业务驱动

将 GridKitchen 的 IUpdate 计时提升到 RestaurantRuntime。每个 50ms 服务步先处理成员与类型化控制，再决定是否推进业务 Tick。暂停时服务步、Mailbox 和恢复／退出控制继续，BusinessTick 不增加；Kitchen 旧 IUpdate 必须撤下，防止双时钟。Preparing 推进合法备料加工，顾客生成与营业计时不启动；Operating／Draining 推进业务；Completed 停止业务，只接收继续／退出。

实际迁移补充：仅正式 Solo 使用 RestaurantRuntime 树，旧 Host/Client 临时样机继续使用 Match 的厨房；旧样机驱动提升到既有 HostSession IUpdate，且发现 Restaurant 时明确不驱动厨房。因此所有厨房均撤下 IUpdate，Solo 不出现第二份时钟，也不扩大到联机餐厅。换关提交所在服务步只安装新 Level 和投影，下一个服务步再接续业务，保留切换边界的原加工进度。

实际50ms服务步依次处理成员／关卡控制、buff选择、布局，再在未暂停且非Completed时推进厨房Tick（角色移动互动／加工）、顾客、交付、前厅工作和伙伴调度，最后营业elapsed／排空／结算及同版快照。第三次实际工作完成先计成长，同一步后续领取可享加速；换关所在步不推进新关业务。

### 3. 暂停矩阵与自然结束

暂停控制提交时：取消 Incoming、尚未提交的目标动作及当前一步之后的路线，发 Cancelled；已经在走的当前一步保存时间，恢复后完成该步并停驻。已开始的手工／自动／前厅作业冻结进度及合法认领；尚在接近但工作未开始的认领释放，不遗留旧目标。暂停期间新业务意图明确 Paused 拒绝；恢复不重放已取消请求。

营业到点停止生成客人，保持现有顾客／队列／行走／用餐与清桌洗碗自然推进。Level 读取实际 Customer／FrontWork 集合的排空条件；尚未接入这些模块时空集合确实为空，不制造假完成任务。完成时厨房未完加工可以留下，手工执行认领释放保留进度，自动作业到 Completed 停钟，下一 Preparing 再接续。顾客和伙伴完整链的真实排空验收由顾客 change 接入后补齐，不拿空前厅结果代替。

### 4. 换关与内存边界

两关样机使用相同菜单和许可，避开尚未裁决的许可缩小 UX。Completed 的 Continue 命令固定原 Level 及配置目标：完整预检下一配置和出生位，先把角色手持 Item 放到最后提交格、暂停手工并清认领，再移除旧关对象，安装新 Level 和角色；原 Kitchen、物品与 Process 不重建。新关得分、成长、buff 和权限清零，旧命令结果不能进入新关。成败边界只在预检成功后提交，无“先销毁再发现配置缺失”。

得分来源是订单成功提交，不是 UI 或完成事件反复加分；阈值来自关卡配置。0 星正常完成。此模式退出丢弃内存餐厅，UI 应说明未提供进度保存；正式产品接入存档时再遵守目标检查点阻止过早换关，本轮不调用假 SaveSuccess。

### 5. 消息收发与投影

Control输入（Start／Pause／Resume／Continue）从受管HUD当前UIGridKitchen.AsyncOwner等待→ClientSession typed请求→LocalMessage.Peer.GetActorId→root sender.Call→Peer Ordered MailBox→ProductHost LevelPeerHandler→捕获可信Peer／代次的LevelSubmitRequest→authority.GetActorId→HostSession Ordered MailBox→LevelSubmitHandler。入口快速接受，Restaurant服务步唯一提交阶段变化；恢复不等待停止的BusinessTick。

答复沿两段原sender等待表返回。阶段变化／最终结果由每个Peer独立DTO，经LocalDelivery sender.Send(ClientSession ActorId)→ProductClient MailBox／原GridSnapshotHandler→一次安装同版Kitchen／Level／前厅／buff值→Dispose DTO→观察事件。每个视图等待绑定当次UI owner，await后检查原EntityRef、World／Level／绑定。完整消息所有权和有限重放沿[本地消息设计](../2026-10-13-solo-local-message-contracts/design.md)，没有新增LevelSnapshotHandler、SceneType或私有等待表。

实际通知使用扩展后的原 GridWorldSnapshot／GridSnapshotHandler，关卡字段与厨房在同一 Version 完整复制，包含 Peer 的最新命令类型及结果事实；没有新增一份关卡投影。GridCommandResult 保留产生时的 Level 身份，不把旧结果重标为新关。Continue 最新完整请求重放可返回已执行事实，其它旧关输入和旧关高版本快照均拒绝。

最终身份检查包含另一真实本地Peer已换关、原Peer最新编号仍属旧关的交错：旧Start／Pause／布局／工作／交付／buff重放也须WrongLevel。仅原Peer最新完整、已Executed的Continue可以返回自己此前换关事实；不同载荷、更早编号或其它旧关控制不能利用该例外，不建立历史结果库。

### 6. 配置、程序集与交接

在 ET Product 源表新增 LevelConfig：序号、下一关定义、营业 Tick、星阈值、允许菜单、初始布局／物资引用与后续客流/buff引用；后续字段只在对应 change 接入，不预填不存在定义。c,s,e 注册，仍走 active ET 五目标 Check／生成，产品读取实际 Tables。Model 持 Restaurant/Level 状态，Hotfix 驱动，ModelView/HotfixView 只展示。

优先在本流程实施前交接汤 change；若它尚未落地，本流程只迁移当前 ManualProcess，AutoProcess 合同保留给后续，不能自行补写煮汤。后续都按实际树接入；不让多个 change 并行覆盖同一 Product／表／协议源。

## Risks / Trade-offs

- [移动 owner 导致旧查询失效或成员误枚举] → 同次迁移列明消费者，复验相关 Grid／成员身份与生命周期。
- [暂停把恢复入口也停掉] → 服务控制步继续，业务 Tick 单独冻结；UTF 泵真实 Resume。
- [假成功或复制跨关物资] → 前厅实际集合排空、手持唯一位置、一次初始创建与旧关代次检查。

## Migration Plan

后续 apply 先完成本地消息和适用汤交接，再修改源表／owner／查询及唯一驱动；协议身份按实际版本正常生成。各组配套小集与原 HUD 控制验收；顾客真实排空在顾客 change 完成时验证。只做内存单机，不修改历史归档或以准备结果写盘。

## Sources

- [当前厨房源码](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen)、[产品建树](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs)。
- [目标 owner 树](../../../../Docs/Product/architecture.md)、[跨关规则](../../../../Docs/Product/specs/restaurant-level-lifecycle/spec.md)。
