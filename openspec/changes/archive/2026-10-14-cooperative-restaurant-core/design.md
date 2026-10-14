# Design

## Context

见 [proposal.md](proposal.md)。实施前 ProductRuntimeBootstrap 仅 Solo 创建 RestaurantRuntime，ProductWire 拒绝 StopWork 之后动作；当前已接正式 Host 的同一 Restaurant/Level，显式开放核心两菜动作及 Level/Layout/Buff。ProductEntitySystems 保留“无 Restaurant 才推进 legacy clock”的原 guard，单步运行证据确认没有双推进。前厅／物流远端入口仍归后继案。

本案跨启动、请求、投影和生命周期，需此设计。沿用 Model／Hotfix／ModelView／HotfixView；无新程序集、SceneType 或消息框架。

## Goals / Non-Goals

**Goals:** 正式 Host 餐厅与可验证的双端两菜链；每项状态保留一个 owner，控制资格从真实绑定取得。

**Non-Goals:** 不复制 Solo 餐厅，不在远端建模拟权威，不恢复旧 Demo；远端前厅／物流由后继案开放。没有联机存档或新增平衡。

## Decisions

### 1. 复用餐厅树并保留驱动排他

```text
Host Main Fiber / SceneType.ProductRoot
  Component ProductRuntime / Timer / ProcessInnerSender
  Child Scene(ProductHost)
    Child Match
      Child Participant...              [member authority]
      Child RestaurantRuntime           [single business clock]
        Component GridKitchenComponent
          Child GridStation / GridItem / Manual / Auto / Bake
          SupplyComponent / Shipment    [only if supplied by Solo baseline]
        Child Level
          GridActor / Customer / Order / FrontWork / Growth / LevelBuff
        Companion                       [existing Solo ownership]
    Component HostSession + Ordered MailBox
      Child HostPeer(local) + Ordered MailBox + LocalDelivery
      Child HostPeer(remote) + Ordered MailBox + RemoteDelivery
    Component NetComponent -> Session + ProductPeerBinding
  Child Scene(ProductClient)
    Component ClientSession + Ordered MailBox -> GridClient
    LocalMessage / UIComponent / GFEntityComponent / GridPresentation

Remote Main Fiber / SceneType.ProductRoot
  Component ProductRuntime / Timer / ProcessInnerSender
  Child Scene(ProductClient)
    Component ClientSession + Ordered MailBox -> GridClient
    NetComponent -> Session + ProductClientBinding
    UIComponent / GFEntityComponent / GridPresentation
```

树为当前实际接入关系；下层 ET Child／Component 细节沿已有 Solo 类型，不为拆案改变父对象。Solo 采用 Host 权威树但无 NetComponent。根初始化先验证实际定义，再创建 Match／Restaurant／Level、authority 和 Peer，最后监听、Join、投影与视图就绪。通过 ProductKitchen／ProductLevel 访问实际 owner，修受影响 fixture 的旧直接查询，保留其行为断言。

替代方案是继续给临时厨房追加餐厅状态；它会形成第二套关卡与时钟，故不采用。新增 Restaurant 后，保留 HostSession 的旧驱动排他分支；网络／消息泵不随业务暂停停止。

### 2. 白名单按动作枚举，不按数值上限放开

本案开放现有备料以及 PourContents、StartAutoWork、PortionSoup、StartBake 和装盘实际使用的合法基础动作；追加 LevelCommand、StationLayout、ChooseBuff 外部请求。FillWater／DrainLiquid／DiscardSolid／ReceiveSupply／TakeSupplyPortion、SupplyCommand、ServeOrder、FrontWork 仍留后继案。不能简单删掉 SoloCommandOnly 后允许所有整数。

现有 GridWorldSnapshot 携带的字段均完整复制，不为了分段删前厅字段或用默认值覆盖。核心案负责通用通知／结果路径、身份版本安装、厨房与控制 HUD；后继案负责领域动作、具体字段消费者及相应行为验收。对未接通的远端操作入口显示不可用，不把缺少能力误报为成功。

协议如需新增字段，改 Design/Proto/ET-Client 源并按既有生成器生成两模式；版本以实施时实际基线递增，不硬编码规划时的 14。保留旧版本明确拒绝，无跨版本兼容层。

### 3. 请求、答复与通知两端

本地：ClientSession 所属值调用 → LocalMessage 捕获当前 Peer.GetActorId → 根 ProcessInnerSender.Call → ProductHost 的 Peer Ordered MailBox。远端：原 Session.Call → ProductHost NetComponentOnRead／ProductWire → 版本／大小／类型／实际 Session 绑定校验 → 复制独立请求、保存外层 RpcId → 同一 Peer ActorId。

GridPeerHandler／LevelPeerHandler／LayoutPeerHandler／BuffPeerHandler 从真实 Peer 复制内部 Submit → authority.GetActorId → HostSession Ordered MailBox 的对应 Submit handler → 快速接收 → Restaurant 服务步核对当前关、绑定、资格和目标后提交。Host 控制资格从 ProductRuntime.Client → ClientSession.Local → LocalMessage.Peer 的当前 EntityRef／InstanceId／代次取得；Peer 入口与最终提交都校验。消息载荷不授予 Host 权限。

首个快照可先于本地 Join 答复，控制资格只由权威已经提交的在线 Participant／Peer 精确绑定与上述本地路径证明，不依赖客户端收到答复的时点。CanControlRestaurant 是复制的资格值；客户端仍须确认 Join 身份、世界／关／绑定和本人角色，并等 Board／HUD 就绪才开放输入。远端缺少真实本地路径，不能借此取得控制。

两段 Call／Reply 用各自原等待表。转交前保存的 wire RpcId 只用于外层回复；await 后核对原 Session／Peer 身份。handler 消费其请求，原调用观察者 finally 回收响应并结束 Peer 在途责任；UI 取消只结束其观察，不提前销毁物理请求或回滚已提交事实。

每 Peer 创建独立 Snapshot／Result。LocalDelivery 用 sender.Send(ClientSession ActorId)，RemoteDelivery 用有效 Session.Send；ProductClient 的网络接收和 ClientSession MailBox handler 核对世界／关／绑定／版本，复制普通值后 Dispose DTO，再发本端观察 Event。Event 不负责答复或跨进程传输，新增消费者不得凭 SceneType.All 监听另一产品树。

### 4. 保留实际退出合同

单成员失联先关入口；服务步使旧意图失效、释放手工执行者、落同一手持物一次并移除角色，Participant 保留离线。显式 Leave 才移除成员，锅内自动批次仍属于厨房，公共 sender 不停。新 Join 仍建立新成员，不实现 Resume。

整轮先停输入与业务驱动、拒绝新请求，关闭受管视图和所属等待，再 Stop 根 sender 并释放本轮网络与树。旧轮异步结果只收尾，不能影响新初始化。Host 本地 Leave 不把共享控制权交给 Client。

### 5. 分段出口与实际入口

核心阶段保留 Host 现有本地前厅；远端两菜在 Preparing 可独立验证，无需等待网络前厅。Host 合法开店、暂停／恢复、布局和 Buff 用原按钮，Continue 的权限及一次安装用最小真实消息测试；完整自然两关在后继案验证。

使用现有 PlayMode fixture 和同 Editor 独立 Fiber／真实 KCP，普通 Host Launcher 建树，临时 Client 沿已有入口加入；通过两端所属受管 HUD／场景输入制作。可按原 owner 依次切换视图，不增双画面工具。此证据明确限同 Editor；双 Editor 全流程归原 loopback 案。

## Risks / Trade-offs

- [切换 owner 破坏旧查询或导致重复推进] → 核对访问器与原驱动 guard，单步／暂停行为断言。
- [按钮隐藏而权威仍越权] → 直接构造真实远端控制请求，断言拒绝与状态不变。
- [DTO 复用或旧绑定覆盖当前视图] → 回收后独立值、Join 前通知及旧关高版本用例。
- [分段扩大交付表述] → 当前进度只更新核心制作范围，完整双端餐厅仍等待后继。

验证成本与可复用结果见 [execution-notes.md](execution-notes.md)。

## Migration Plan

先接 Host 树与控制，再接两菜白名单／客户端调用和投影，最后最小 KCP 与受管入口。失败时保留未完成任务并局部修复，不回退为“HostReady 即可玩”，不重建第二厨房。实施范围不要求修改 Excel；若后续发现确需改表，按实际原因加载 Luban、改源导出并补对应校验。

## Sources

- [启动](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs)、[驱动](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductEntitySystems.cs)、[wire](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs)。
- [消息与投影约定](../../../../references/reactive-and-network.md)、[历史真实 KCP 备料](../2026-10-11-grid-kitchen-prep-processing/execution-notes.md)。
