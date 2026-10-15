# Design

## Context

Solo与合作各自已有首章三关，当前配置分别使用ChapterId=1/2；尚无两个内容大关的选择与逐关递进菜单。现有动态总关数可复用，不能把模式配置链算作两个可选章节。 动机见 [proposal.md](proposal.md)。本案处于规划阶段，事实来自 2026-10-15 当前工作区静态读取和既有执行记录，不声称重新运行通过。

## Goals / Non-Goals

目标模式：Solo 与已支持的本机合作。业务范围见 proposal；复用原 ET Model/Hotfix 与 ModelView/HotfixView 分层，无新业务架构/消息总线。

前置合同：

- [cooperative-chapter-completion](../2026-10-15-cooperative-chapter-completion/design.md)
- [restaurant-unlock-permissions](../2026-10-15-restaurant-unlock-permissions/design.md)

不要求 PC LAN、Android、Player/AOT 或全部其他 P1/P2 完成。涉及同一共享 owner 时依次交接。

持久化首轮验收仅扩展已有 Solo 成功档，合作先覆盖内存世界；合作基线/领取交付后才补对应持久验收，不能把共享代码当远端已保存。

交接责任：本案先于领取案交付时，由领取案集成覆盖已落地持久字段；本案后于领取案交付时，由本案持久化任务补现有合作保存入口的一次往返。同一运行可覆盖多个字段，不重跑未变内容。

## Decisions

### 1. 沿既有事实扩展

复用 LevelConfig、CustomerFlow、Order、Renovation 及现有检查点；加入最小章节元数据即可，不新建剧本引擎。

### 2. 保留原子效果

首包两个大关各三小关已经确认；菜单/来客/产量和评分按已交付能力配置并复玩，400 Tick 等 smoke 参数不是正式平衡。

### 3. 共享边界与兼容

默认只依赖已交付三款产品即可开始编排；assembly/hot/dessert/beverage 实际交付后按选定批次增量纳入，不硬串所有内容案。

创建时仅有MenuVariant=0/1的调查现已由08–10交付替代。本案使用现有Growth=3资源组合，新增内容ChapterId与MenuVariant分开；初始化、供应、保存仍沿原读口，旧ID/Next与库存不变。具体12行关序、参数与真实结果见execution-notes，不新建目录框架。

### 4. 分批与模式交付

新开与旧有效档分开：旧档按所保存定义/版本读取，不因为新菜单而补货或改关序。若配置将移除引用，先提供显式只读兼容。

### 5. 提交责任

Editor 工程复玩用于验证首单/补货/收尾是否可执行，不推断真人体验；本轮不强制安排真人观察。

### 内容身份与模式分开

当前源码已有 Solo ChapterId=1 与合作 ChapterId=2，以及合作301→302→303/401→402→403；它们是首章在模式/菜单下的配置链，不是已经存在两个可选内容大关。ChapterLevelCount 已能读取实际总关数，复用它，不再安排重做“动态总关数”。

在现有配置增加最小章节选择映射：稳定内容章身份对应各支持模式/菜单的首关定义。保留既有ChapterId/LevelId及旧Next引用，不把旧合作ChapterId=2改作新第二章。UI、房间开局、成功记录与重开都携带选定章身份；每种支持模式都能选择两个各三关的内容章，不能拿Solo一章+合作一章凑数。

首包从现有汤/吐司/橙汁编排介绍、组合与分工；逐关菜单许可由unlock统一裁决。本案消费该读口并配客流/供应/设备，不再实现第二套解锁。具体时长、来客间隔和阈值属于可调整首包参数，必须经过一次代表性经营复玩，不宣称正式平衡或真人理解通过。

旧有效档按原关链恢复，不用新开配置回填旧载荷。新关序与配置兼容检查覆盖全部六小关及两种模式；普通入口仅覆盖不同新机制，不重复完整遍历未变制作链。

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

房间入口已实现，等待房间时可以没有餐厅；基线接口复用其最新实际交付，SaveResult/Claim仍由领取案实现。章节元数据来自源配置，运行当前关仍为 RestaurantRuntime Child Level；完成事实归现有持久记录。新增内容不建立并列的关间 owner，不把章节列表做成另一份可写经营进度。

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

2026-10-15 已确认两个可选大关、每个三小关。复用合作终关和解锁许可；首包可只编排已交付菜品，新增菜实际交付后再纳入，不依赖全部内容批次。


## Risks / Trade-offs

内容常见风险是生产链缺口与高压到无法完成既有操作，用菜单闭包加每种新关机制一次 Editor 复玩覆盖，不做全目录穷举或统计留存研究。 任务到最小验收的对应与接受的未验证项见 [execution-notes.md](execution-notes.md)。

## Migration Plan

先修改对应手写源和必要 Product Excel/Proto/CodeBind 源，再经既有生成入口更新消费者；不手改派生 C#/JSON/bytes 或 Unity YAML。旧有效配置/存档明确兼容，新增内容只在选定模式开放。

按实际变更选择相关 UTF 和普通 Editor 入口验收；已有行为未变结果直接引用。不对用户现场清空/回滚，不自动提交、归档或同步第二工作区。

## Sources

- [Design/Excel/ET/Datas/Product/LevelConfig.xlsx](../../../../Design/Excel/ET/Datas/Product/LevelConfig.xlsx)
- [Design/Excel/ET/Datas/Product/CustomerFlowConfig.xlsx](../../../../Design/Excel/ET/Datas/Product/CustomerFlowConfig.xlsx)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)
- [业务目标与本轮优先级](../../../../Docs/Product/full-version-roadmap.md)。


### 当前实施身份与边界

从10的v9/wire30交接；已支持0/1/2/3资源组合，首包沿Growth3。内容章1家常开张/2进阶小馆映射Solo Chapter11/12、合作21/22；新Level1101–1103/1201–1203/2101–2103/2201–2203。旧Chapter1/2及全部原Level/Next不动，旧文件保持原链与物资。元数据只读描述模式首关与标题，当前状态仍由Match→Restaurant→Level持有，保存沿已有Header/Level身份。

Solo选择或Host房间开局携带内容章定义ID，经原Room公开请求→可信Peer→HostSession有序邮箱裁决；客户端不能改Host资格。0定义ID保留原菜单入口，非零必须匹配真实章节与资源组合。记录/提示从稳定Level/Chapter映射解释标题，读取不创建物品；完成后仍返回选档/同房间，另选章节创建独立厨房，整个运行/单成员退出边界不变。

经营参数采用有界递进来客/营业窗口，第一章依次引入汤、吐司、F01，第二章组合F01/F02/橙汁后逐步加入旧菜；初始解锁集合与当关菜单分开，沿09共同闭包。后续内容批次只能在实际具备生产链后纳入，保留旧链用于旧记录，不偷偷改档补库存。

2026-10-15 咖啡批次集成：新开的第二内容章采用1501/2501、Cafe6，三关逐步加入热餐/甜品/咖啡；两个大关、稳定章身份及每章三关保持。原1201/2201和旧Next链保留，旧成功档不补新库存。受变核心5项、普通Solo/KCP三关UI2项与实际旧文件兼容已验收，详见[咖啡记录](../2026-10-15-kitchen-beverage-batch/execution-notes.md)。
