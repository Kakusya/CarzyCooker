# Design

## Context

没有甜品定义、搅拌面糊、烘焙载具或脱模阶段；GridBakeSystem 当前直接把单个面包替换为吐司，不能表达模具身份保留。 动机见 [proposal.md](proposal.md)。本案处于规划阶段，事实来自 2026-10-15 当前工作区静态读取和既有执行记录，不声称重新运行通过。

## Goals / Non-Goals

目标模式：Solo 与已支持的本机合作。业务范围见 proposal；复用原 ET Model/Hotfix 与 ModelView/HotfixView 分层，无新业务架构/消息总线。

前置合同：

- [kitchen-multi-input-assembly](../2026-10-15-kitchen-multi-input-assembly/design.md)

不要求 PC LAN、Android、Player/AOT 或全部其他 P1/P2 完成。涉及同一共享 owner 时依次交接。

持久化首轮验收仅扩展已有 Solo 成功档，合作先覆盖内存世界；合作基线/领取交付后才补对应持久验收，不能把共享代码当远端已保存。

交接责任：本案先于领取案交付时，由领取案集成覆盖已落地持久字段；本案后于领取案交付时，由本案持久化任务补现有合作保存入口的一次往返。同一运行可覆盖多个字段，不重跑未变内容。

## Decisions

### 已落实的首批参数（2026-10-15）

P38 为预拌粉43、鸡蛋3、瓶装牛奶原料44各一份，放入搅拌盆45，在搅拌机13显式启动80 Tick搅拌，完成有限两份面糊46；一Tick为50ms。模具47一次承接一份，原烤箱显式100 Tick烘烤，内部面糊替换为蛋糕48，外模具身份不变。手持熟模具向台面上干净空甜品盘49显式脱模，转移原蛋糕，原模具留手且可再次装模。盆、模具、盘均不互相嵌套。

资源菜单Dessert=5仅包含原基础物资及本批，Solo1301→1303、本机合作2301→2303，各关菜品8；不增加内容大关数量，也不改旧档Next链。预拌粉/牛奶供应各五包、每包一份，缺鸡蛋沿原供应补充；没有面糊或蛋糕成品供应。搅拌机(1,7)、模具台(5,9)、甜品盘台(2,11)、第二模具台(4,6)通过实际全布局通路验证。

GridAutoProcess沿原持续厨房持有限批和锁盆，Mix完成仍经原内部消息。GridBakeProcess增加CarrierItemId，直接烘烤/煎制为0，装模链同时绑定工位、原模具和唯一内部面糊；完成只替换内部物品。显式Unmold完整预检菜单许可、源内容和空盘后一次转移。新文件v11仅在SavedBake末尾追加CarrierKey，第13项根载荷Geometry与28项头部不变；wire33在原GridBakeProcessState追加字段11。旧v10七成员烘焙载荷默认CarrierKey=0，读取不补本批物资。源配置/协议均经原生成器导出。

### 1. 沿既有事实扩展

复用多输入准备及自动烘焙阶段，载具与食物内容分开；脱模是经合法动作把成品转入出餐容器，不销毁模具。

### 2. 保留原子效果

普通容器不整体开放任意嵌套。若载具承接需要特殊关系，定义明确允许的单层能力并保持无环与唯一位置。

### 3. 共享边界与兼容

面糊剩余份数和模具加工冻结值随持续厨房保存，不能通过装模或取出补满批次。

### 4. 分批与模式交付

本批不依赖煎台，避免仅为未来松饼把 hot-food-batch 串为硬前置。

### 有限载具链

甜面糊使用目录 P38：甜点预拌粉×1、鸡蛋×1、牛奶×1，在搅拌机/搅拌盆完成有限批次；S04 每个模具装入一份面糊，烤熟后脱模到合法甜品盘。面糊阶段复用 assembly 的投入数量表达，自动加工复用原 Tick 和锁定规则。

模具持有食物内容，不把搅拌盆、模具或甜品盘彼此嵌套。烤制改变内容而保留模具，脱模同时预检源成品、目标空盘和唯一位置，失败两端不动。先覆盖这一条真实关系，不引入任意容器树或通用烘焙框架。

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

房间入口已实现，等待房间时可以没有餐厅；基线接口复用其最新实际交付，SaveResult/Claim仍由领取案实现。面糊有限批次归持续厨房，模具为原 GridItem 的限定载具能力，烘焙 Process 引用模具和内容。脱模先核对源/目标/锁定再移交真实成果，载具身份保留。

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

2026-10-15 已确认 S04 原味小蛋糕，按目录的甜面糊、装模、烘烤、脱模到甜品盘实施。耗时和批次产量按有限配置落地，不新增其他甜品。


## Risks / Trade-offs

新载具关系可能造成循环包含、提前出品和模具丢失。聚焦有限批次、脱模失败保物、模具复用及实际保存，避免通用容器重构。 任务到最小验收的对应与接受的未验证项见 [execution-notes.md](execution-notes.md)。

## Migration Plan

先修改对应手写源和必要 Product Excel/Proto/CodeBind 源，再经既有生成入口更新消费者；不手改派生 C#/JSON/bytes 或 Unity YAML。旧有效配置/存档明确兼容，新增内容只在选定模式开放。

按实际变更选择相关 UTF 和普通 Editor 入口验收；已有行为未变结果直接引用。不对用户现场清空/回滚，不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs)
- [Docs/Product/catalog.md](../../../../Docs/Product/catalog.md)
- [业务目标与本轮优先级](../../../../Docs/Product/full-version-roadmap.md)。
