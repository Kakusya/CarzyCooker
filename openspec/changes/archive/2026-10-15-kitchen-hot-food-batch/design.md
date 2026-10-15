# Design

## Context

实际工位能力只有切配、煮制、烘烤、榨汁等；烘烤为单物件替换输入，尚无煎制熟料再与其他分支合流的热餐。 动机见 [proposal.md](proposal.md)。本案处于规划阶段，事实来自 2026-10-15 当前工作区静态读取和既有执行记录，不声称重新运行通过。

## Goals / Non-Goals

目标模式：Solo 与已支持的本机合作。业务范围见 proposal；复用原 ET Model/Hotfix 与 ModelView/HotfixView 分层，无新业务架构/消息总线。

前置合同：

- [kitchen-multi-input-assembly](../2026-10-15-kitchen-multi-input-assembly/design.md)

不要求 PC LAN、Android、Player/AOT 或全部其他 P1/P2 完成。涉及同一共享 owner 时依次交接。

持久化首轮验收仅扩展已有 Solo 成功档，合作先覆盖内存世界；合作基线/领取交付后才补对应持久验收，不能把共享代码当远端已保存。

交接责任：本案先于领取案交付时，由领取案集成覆盖已落地持久字段；本案后于领取案交付时，由本案持久化任务补现有合作保存入口的一次往返。同一运行可覆盖多个字段，不重跑未变内容。

## Decisions

### 1. 沿既有事实扩展

新能力沿现有厨房 Process 所有权和固定 Tick 扩展，显式启动，完成才产生新熟料，单设备单作业。

### 2. 保留原子效果

本批需要可留设备自动加工的煎制，不因名称引入手动连续翻面；设备/载具锁定政策在该批源定义明确。

### 3. 共享边界与兼容

早餐复用真实吐司链，汉堡复用组装链；供应生料而非直接提供熟肉。

实际交接采用Hot=4资源组合与Solo901→902→903/合作1001→1002→1003新菜单链，保留原Level/Next及两内容章元数据。这是原Chapter1/2的热餐菜单变体，不另算内容大关。共享餐盘10允许真实组装配方的部分投入绕过吐司9的单品独占限制：9+煎蛋37可继续等熟香肠36，但9+番茄2、9+9及成品汉堡/沙拉混装仍拒绝，文件校验使用同一判定。

煎制沿原GridBakeProcess单输入自动加工存储，DishStageKind.Fry与StationCapability.Fry明确区分菜谱和设备，StartFry显式加入本地动作边界及远端wire白名单；完成通知仍按原Process身份和冻结输入/输出/耗时收尾，不新建消息owner或等待表。初始煎台(2,16)、牛肉饼台(5,7)，原工位不移动。新的格式v10/wire32；旧v1–v9继续只读，新增Hot内容要求v10，payload既有Baking字段足够，不加冗余状态。

### 4. 分批与模式交付

新增动作逐项 wire 白名单并复制必要字段；不能根据 enum 连续区间默认远端全部可用。

### 首批热餐与前置交接

F11 使用汉堡面包×1、熟牛肉饼×1；F43 使用熟香肠×1、煎蛋×1、烤吐司×1。分别从生肉饼、香肠、鸡蛋及既有面包链制备，不能把两菜写成共用同一种熟料。共享的是煎台、备料交接和前置案的精确组装能力。

前置 assembly 交付多输入、份数和容器规则后，本案只增加煎制能力和上述两条菜链。每台煎台同刻一项自动作业，输入锁定、完成解锁、暂停及新身份输出沿现有自动加工合同。旧烤箱不改成通用设备，早餐直接复用吐司。

### 状态与 ET 释放归属

```text
Product Fiber / Root Scene (SceneType.ProductRoot)
  Component: ProductRuntime / Timer / CoroutineLock / ProcessInnerSender
  Child Scene: ProductHost (SceneType.ProductHost; Solo/Host)
    Component: HostSession + Ordered MailBox
      Child: HostPeer + Ordered MailBox
        Component: LocalDelivery or RemoteDelivery
    Child: Match
      Child: Participant (connection-independent)
      Child: RestaurantRuntime (absent while waiting in room)
        Component: GridKitchen -> Child Station / Item / Process
        Component: RestaurantRenovation
        Child: Companion (persistent definition identity)
        Child: Level -> Child GridActor / Customer / Order / FrontWork / CompanionRole
          Component: FrontLevelState / FrontWorkCommands / CompanionGrowth / LevelBuff
    Component: NetComponent -> Child Session (Host only)
  Child Scene: ProductClient (SceneType.ProductClient)
    Component: ClientSession + Ordered MailBox -> Component GridClient (independent values)
    Components: GridPresentation / UIComponent / GFEntityComponent
```

房间入口已实现，等待房间时可以没有餐厅；基线接口复用其最新实际交付，SaveResult/Claim仍由领取案实现。以原 GridBakeProcess/自动过程的可适用形态承接煎制，源定义明确能力与输入输出；只有实际新增语义才增加最小字段。熟料是 GridItem，终菜使用组装阶段，不再建热餐库存。

### 发送、接收、裁决与返回



本地操作由 ProductClient 的当前 ClientSession/LocalMessage 取得真实 HostPeer EntityRef，并使用 peer.GetActorId()；ProductRoot 的 ProcessInnerSender.Call 入 MessageQueue，ProductHost SceneType 对应 Peer handler 接收。handler 校验绑定，再转到 authority.GetActorId() 的 HostSession Ordered MailBox；领域 owner 在允许业务步复验上下文和实物后提交。自动后继仍经现有内部消息 handler 接入原 owner，不能由观察事件直接提交。

远端由 Session.Call 发公开协议，ProductHost NetComponentOnRead/ProductWire 只在显式白名单内复制值、捕获原 wire RpcId、取得可信 HostPeer 再进入同一 Peer/HostSession 路径。本地 Host 不向自己发 UDP；远端不能自报内部 ActorId 直达。协议版本从实施时 ProductSessionRules.ProtocolVersion 的真实值演进；本次读取为25，仅作定位，不预占下一版。

入口答复只表示 Accepted/Rejected；实际动作或阶段的成功/拒绝/取消沿 ProductCommandOutcome/GridCommandResult 或明确阶段通知返回。Host 提交同版快照后，各接收者独立 LocalDelivery/RemoteDelivery 通知 ProductClient handler，复制独立值后释放 DTO，再发布只读观察 Event；暂停期间控制邮箱仍工作。

### 创建、退出和消息所有权

创建时先准备 Root sender 与权限上下文，再创建权威/可选餐厅、可信 Peer 和客户端，最后加载原受管 HUD/Board；没有匹配世界、本人确认和视图时不开放地图输入。关卡变化只重建关内角色/工作，持续厨房沿原 owner 留存。

请求/答复每跳由其原调用/handler 收尾，跨 await/队列只留独立值和 EntityRef/InstanceId；同时核对 Match/World/Level/绑定代次。UI 关闭取消对外等待，原物理 Call 仍按答复/有限超时收尾，不提前重复回收池消息或另建等待字典。

成员失联仅撤旧绑定/未执行输入，按既有最后提交格落物和工作接续规则收尾，不停止 Root sender。关闭餐厅回房间只退役餐厅所属对象；整个运行解散才拒绝新入口、处理适用业务关闭、结束原等待并释放 sender/Scene。Root 释放不临时补成功/存档。

### 方案取舍

采用现有 owner 上的最小能力扩展，避免把有限首批内容变成独立框架。替代方案是为每菜/每模式复制流程，或一次实现完整候选目录；两者都会增加维护或推迟玩家可见交付。沿当前真实第二消费者增加必要复用字段，避免仅为潜在未来搭平台。

### 实施门槛与决策身份

2026-10-15 已确认 F11 经典牛肉汉堡、F43 香肠鸡蛋早餐，按目录配方制作。煎台显式启动后自动加工，耗时/供给量为可调首包参数。


## Risks / Trade-offs

风险为旧过程类型被新设备混用或跨关重复产物。用共用煎制能力及两菜各自熟料的拒绝/暂停/交接/保存小集，不遍历所有候选热餐。 任务到最小验收的对应与接受的未验证项见 [execution-notes.md](execution-notes.md)。

## Migration Plan

先修改对应手写源和必要 Product Excel/Proto/CodeBind 源，再经既有生成入口更新消费者；不手改派生 C#/JSON/bytes 或 Unity YAML。旧有效配置/存档明确兼容，新增内容只在选定模式开放。

按实际变更选择相关 UTF 和普通 Editor 入口验收；已有行为未变结果直接引用。不对用户现场清空/回滚，不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSoupSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSoupSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridWorkTransitions.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridWorkTransitions.cs)
- [Docs/Product/catalog.md](../../../../Docs/Product/catalog.md)
- [业务目标与本轮优先级](../../../../Docs/Product/full-version-roadmap.md)。
