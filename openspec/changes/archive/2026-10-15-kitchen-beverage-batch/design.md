# Design

## Context

只有鲜榨橙汁和一次性杯贴单，未实现磨豆/萃取/热奶/奶泡及定份混合，当前饮品完成仍主要依赖单种壶内批次。 动机见 [proposal.md](proposal.md)。本案处于规划阶段，事实来自 2026-10-15 当前工作区静态读取和既有执行记录，不声称重新运行通过。

## Goals / Non-Goals

目标模式：Solo 与已支持的本机合作。业务范围见 proposal；复用原 ET Model/Hotfix 与 ModelView/HotfixView 分层，无新业务架构/消息总线。

前置合同：

- [kitchen-multi-input-assembly](../2026-10-15-kitchen-multi-input-assembly/design.md)

不要求 PC LAN、Android、Player/AOT 或全部其他 P1/P2 完成。涉及同一共享 owner 时依次交接。

持久化首轮验收仅扩展已有 Solo 成功档，合作先覆盖内存世界；合作基线/领取交付后才补对应持久验收，不能把共享代码当远端已保存。

交接责任：本案先于领取案交付时，由领取案集成覆盖已落地持久字段；本案后于领取案交付时，由本案持久化任务补现有合作保存入口的一次往返。同一运行可覆盖多个字段，不重跑未变内容。

## Decisions

### 已落实配置与交接（2026-10-15）

首批资源Cafe=6，包含已交付有限物资；新增52豆、53粉、54咖啡接液壶、55奶缸、56浓缩液、57热奶、58奶泡、59热饮杯、60拿铁、61卡布及62/63原料/杯包装。豆一份研磨40Tick成粉；接液壶萃取80Tick产四份浓缩，奶缸热奶60Tick产四份热奶、打泡60Tick产两份奶泡。模式按原过程RequiredCapability冻结，三类自动模式均锁定容器；研磨沿单输入Process，不另建计时owner。

热饮杯容量3、一次性交付消费，原橙汁杯仍容量1。D15为浓缩1份；D17浓缩1+热奶2，明确混合40Tick成品；D18浓缩1+热奶1后最后奶泡1，明确混合40Tick。源GridManualRecipe末尾LastInputDefinitionId：旧条目/拿铁为0，卡布为58；在杯内有序实物和各配方精确份数上检查前序，过早奶泡/错份/满杯不扣源批次。已贴单杯须先解绑才能继续添加。豆包一份、热饮杯包三只，各五包，牛奶沿原有限13源；无基底成品采购。

磨豆机(4,5)、咖啡机带接液壶(3,9)、蒸汽机带奶缸(5,13)、热饮杯台(3,4)、第二奶缸台(2,7)，全部新旧布局联合通路通过真实配置UTF。热奶完成后可搬原奶缸到空台面，再用第二缸选择打泡；不同设备可独立加工。

文件v12仅保护新内容目录，沿原有序Contents、ManualWork、Batches保存半杯、混合及模式/有限份数，无新的平行状态；旧v11及旧格式读取保持。wire34新增五个明确动作，沿原Grid请求/快照整数和字段，无新协议载荷。内部完成消息在原KitchenWorkTransitionHandler按Beverage种类取得GridAutoProcess，HostSession/厨房再次核对原InstanceId/serial及RequiredCapability后提交；发送器、接收SceneType与释放路径沿原树不变。

普通咖啡菜单1401/2401各三关。新开“进阶小馆”映射Cafe下1501/2501三关：首关沙拉/汉堡、橙汁/浓缩，次关加入汤/早餐/蛋糕及拿铁，末关进一步加吐司和卡布。新开三关的订单前列分别为橙汁/汉堡/芝士沙拉、蛋糕/早餐/拿铁、卡布/浓缩/拿铁；仍由营业时长、到达间隔和有界队列决定实际顾客，无固定三人上限。两个内容章数量/稳定身份保持；旧1201/2201的Growth菜单、Next和旧档物资不改。章节夹具读取实际元数据以验证当前新开路径，另验旧文件原链不改。

### 1. 沿既有事实扩展

设备模式与投入/输出来自明确源定义，不能把蒸汽机加热和打泡当同一动作结果。

### 2. 保留原子效果

咖啡粉、萃取液、热奶和奶泡须各有实际制备路径，不能直接供应熟基底；普通奶和咖啡豆等按本批有限来源供应。

### 3. 共享边界与兼容

调饮核对定义数量与必要顺序，失败保留已有杯内容；配方身份仍与订单绑定分开，成品之后才贴单。

### 4. 分批与模式交付

本批不强依赖热餐/甜品，沿多输入阶段和既有饮品 owner 扩展；其他饮品族待后续批次明确。

### 定份配方与模式

| 成品 | 游戏份数 | 顺序 |
| --- | --- | --- |
| D15 | 浓缩咖啡液×1 | 入热饮杯 |
| D17 | 浓缩咖啡液×1、热牛奶×2 | 基底齐全后完成混合 |
| D18 | 浓缩咖啡液×1、热牛奶×1、奶泡×1 | 咖啡与热奶齐全后最后加奶泡 |

咖啡豆→咖啡粉→萃取液，牛奶分别通过加热/打泡得到热奶或奶泡；不增加温控和拉花。复用 assembly 的精确多输入与必要前序，本案只增加本批设备模式、液体取份和热饮杯能力。现有一次性杯容量1，不能直接放开所有杯容量；新杯的复合内容须同步饮品识别、贴单、清理和保存校验，橙汁仍沿原单份规则。

每次取份先完整预检杯内容/顺序/容量，再扣源批次；拒绝不扣份。各基底批次的剩余量、热奶模式及奶泡收尾状态必须能独立投影与恢复。

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

房间入口已实现，等待房间时可以没有餐厅；基线接口复用其最新实际交付，SaveResult/Claim仍由领取案实现。共享基底、杯内容和剩余份数仍归 GridItem/自动过程；模式与阶段标识保留在原过程或必要阶段字段。当前 Level 只持 DrinkBindings，杯的配方身份不从 BoundOrderId 推导。

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

2026-10-15 已确认 D15 浓缩咖啡、D17 热拿铁、D18 卡布奇诺及最后加奶泡规则；采用目录游戏份数，其他饮品族不纳入。


## Risks / Trade-offs

主要风险为顺序绕过、热奶模式混淆和错贴单。用一条共享基底分支加顺序拒绝/重绑保旧/暂停/末份/保存小集，普通 Editor 实做三款。 任务到最小验收的对应与接受的未验证项见 [execution-notes.md](execution-notes.md)。

## Migration Plan

先修改对应手写源和必要 Product Excel/Proto/CodeBind 源，再经既有生成入口更新消费者；不手改派生 C#/JSON/bytes 或 Unity YAML。旧有效配置/存档明确兼容，新增内容只在选定模式开放。

按实际变更选择相关 UTF 和普通 Editor 入口验收；已有行为未变结果直接引用。不对用户现场清空/回滚，不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDrinkSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDrinkSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSoupSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSoupSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridRecipeGuide.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridRecipeGuide.cs)
- [Docs/Product/catalog.md](../../../../Docs/Product/catalog.md)
- [业务目标与本轮优先级](../../../../Docs/Product/full-version-roadmap.md)。
