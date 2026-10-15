# Design

## Context

RestaurantRenovation 记录已安装工位，但尚无独立菜谱/设备解锁事实；当前校验强制同章节菜单相同。许可缩小已于2026-10-15裁决，尚需实现共同许可读口。 动机见 [proposal.md](proposal.md)。此段为创建时事实；实际从00–08交付的v7/wire28前置实施，运行结果另记execution-notes。

## Goals / Non-Goals

目标模式：Solo 与已支持的本机合作。业务范围见 proposal；复用原 ET Model/Hotfix 与 ModelView/HotfixView 分层，无新业务架构/消息总线。

前置合同：

- 无新案硬前置，复用已交付主规格和相应原执行证据。

不要求 PC LAN、Android、Player/AOT 或全部其他 P1/P2 完成。涉及同一共享 owner 时依次交接。

持久化首轮验收仅扩展已有 Solo 成功档，合作先覆盖内存世界；合作基线/领取交付后才补对应持久验收，不能把共享代码当远端已保存。

交接责任：本案先于领取案交付时，由领取案集成覆盖已落地持久字段；本案后于领取案交付时，由本案持久化任务补现有合作保存入口的一次往返。同一运行可覆盖多个字段，不重跑未变内容。

## Decisions

### 2026-10-15 许可缩小已裁决

用户明确选择“沿用原规则，禁止新盛装”。保留已有原料/半成品/成品、在途与待收货、已开始（含合法暂停）的作业；允许搬运、接收旧货及同一已受理作业接续。被本关限制的菜品禁止新采购、向生产容器投料、新启动及新盛装，即使源批次已经完成也不例外。旧物可正常搬运和按原设施规则清理，不清空厨房，不补回份数。

许可按实际动作及有效配方闭包裁决：共用原料若仍服务于本关另一合法菜品则不能按物品名字一概禁用；同一暂停作业的接续与新起一项作业区分。HUD 与 Host 使用同一许可结果，受限盛装明确提示“本关未开放该菜品，不能新盛装”。此处记录决定，实际功能仍待本案 UTF/Editor 验收。

### 1. 沿既有事实扩展

在现有装修来源增加 DefinitionId 解锁奖励；设备安装由明确布局或奖励执行一次，换关重算许可不再生设备，解锁不凭空增加食材。

### 2. 保留原子效果

实际状态归 RestaurantRuntime 的最小解锁组件（拟新增），Level 只持当前生效集合，Client 仅独立投影。

### 3. 共享边界与兼容

新增菜单必须同时通过供应/设备/容器/阶段闭包校验，缺少能力不能用免费中间物补齐。

### 4. 分批与模式交付

许可缩小按上文已确认操作表实施，保留旧物和已受理工作，拒绝受限菜品新生产及新盛装；不因历史许可缓存或 UI 旧按钮放行。

### 本案交付边界

先用现有汤、吐司、橙汁构造一组解锁和许可缩小，不依赖新菜批次。当前 RestaurantConfig 强制同一章节菜单完全相同；本案将其改为逐关验证生效菜单及生产闭包，章节案再负责新增可选内容。MenuVariant=0/1 继续表示旧配置能力集合，不能当作解锁身份。

| 动作 | 许可缩小后的处理 |
| --- | --- |
| 已有物品搬运、接收在途/待收货、设施清理 | 沿原合法条件允许，不补库存 |
| 同一已开始/暂停作业继续 | 保留原输入与冻结参数；禁止以继续名义启动新作业 |
| 新采购、生产容器投料、新启动 | 按仍开放菜品的配方闭包核对；共用原料仍有合法用途时允许 |
| 从受限完成批次新盛装 | 拒绝，源剩余份数和目标容器不变 |
| 旧成品交付 | 仍须匹配当前真实有效订单，不为旧物新造订单 |

解锁只记录 DefinitionId。新增设备的实际安装仍由明确布局/奖励控制，不因每关重算许可而再生设备。旧档适配从其原菜单/能力恢复初始解锁集合，不给它补发新奖励。

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
        Component: RestaurantRenovation / RestaurantUnlocks (persistent definition sets)
        Child: Companion (persistent definition identity)
        Child: Level (effective ProductionPermit) -> Child GridActor / Customer / Order / FrontWork / CompanionRole
          Component: FrontLevelState / FrontWorkCommands / CompanionGrowth / LevelBuff
    Component: NetComponent -> Child Session (Host only)
  Child Scene: ProductClient (SceneType.ProductClient)
    Component: ClientSession + Ordered MailBox -> Component GridClient (independent values)
    Components: GridPresentation / UIComponent / GFEntityComponent
```

房间入口已实现，等待房间时可以没有餐厅；基线接口复用其最新实际交付，SaveResult/Claim仍由领取案实现。拟新增最小 RestaurantUnlocks Component 归 RestaurantRuntime，记录已解锁 DefinitionId；Level 持当前生效许可。原 RestaurantRenovation 提交奖励，持久化只捕获成功事实，不把 HUD 可见列表当授权。

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

2026-10-15 已确认许可缩小规则：保留旧物、在途货物和已开始作业；拒绝受限菜品新采购、投料、启动及新盛装，已完成批次也不例外。按实际配方闭包区分共用原料与受限产物，不再等待产品裁决。


## Risks / Trade-offs

风险是 UI 与裁决许可不一致或换关吞旧物。以一种新增解锁和一组已裁决缩小场景验证真实入口及原物保留，不做全目录许可穷举。 任务到最小验收的对应与接受的未验证项见 [execution-notes.md](execution-notes.md)。

## Migration Plan

先修改对应手写源和必要 Product Excel/Proto/CodeBind 源，再经既有生成入口更新消费者；不手改派生 C#/JSON/bytes 或 Unity YAML。旧有效配置/存档明确兼容，新增内容只在选定模式开放。

按实际变更选择相关 UTF 和普通 Editor 入口验收；已有行为未变结果直接引用。不对用户现场清空/回滚，不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/RestaurantRenovation.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/RestaurantRenovation.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RenovationConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RenovationConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs)
- [Docs/Product/backlog.md](../../../../Docs/Product/backlog.md)
- [业务目标与本轮优先级](../../../../Docs/Product/full-version-roadmap.md)。


### 实际交接参数

本案不回溯锁掉既有0/1/2菜单；增加具名Growth资源批次，组合已有饮品/沙拉资源，保留旧批次语义。普通入口选择成长餐厅，初始未解锁橙汁/榨汁设备，一次装修定义解锁后第二关可制作，第三关缩小。初始解锁用显式开关及定义集合区分“未配置沿旧基线”与空集；不以MenuVariant作为解锁身份。具体ID/布局和有限营业节奏为配置参数，后续章节案统一首发两大关节奏。

解锁与当前许可分开：RestaurantUnlocks保存稳定Dish/Station定义及修订；Level的ProductionPermit是可重建索引，含有效菜品、原料/载具、手工/自动/烘焙配方及按生产容器的合法投入。当前生效菜品取解锁∩关卡菜单并要求已解锁设备具备闭包能力。Host动作及Client提示复用纯闭包规则；高频路径不临时遍历图或新建集合。

同一已受理ManualProcess按实际过程/输入/载具身份识别接续，不能仅凭非零processId放行。普通拿放/抽取已有实物、接收旧在途、设施清理不作为新生产；新采购/投料/启动和完成批次新盛装需当前许可。共用番茄切配仍可服务F01，但不能因此向受限汤锅投料。订单只从当前有效菜单生成，不向未解锁菜派单。

v8成功载荷记录独立纯值解锁集合；旧档只读适配、旧签名结果内容版本分别处理，导入/领取不得补库存或重新发奖励。一次当前装修选择后记录集合事实，后续继续/检查点按原单次安装记录收尾，解锁不增加实体。新字段沿原Snapshot/生成链复制，无私有网络通道。

### 实际格式与原协议交接

本案wire29在原GridWorldSnapshot追加许可修订、已解锁菜谱/工位定义；本地和KCP仍经过同一HostPeer/HostSession/Owner，没有额外消息通道。Client独立复制定义并按修订重建同一纯闭包，HUD只消费投影。

成功文件v8的Payload在原11成员后追加Unlocks，Header维持原28成员布局。旧v1–v7读取缺字段时按原菜单和已保存工位能力只读适配，不补物资；共同SuccessResult在原12成员后追加ContentVersion，缺失为0时按旧v7内容校验、适配解锁值，首次合法领取写v8。签名原票和源文件保持原字节；重复领取仍沿原消费记录返回原Slot/revision。

实际动作表的到位复验、本地/KCP保物以及临时文件结果逐项见execution-notes，不把源代码已接入或编译通过当验收。
