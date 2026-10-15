# Design

## Context

Host 默认菜单使用 3→4→4，关卡 4 自循环且 ChapterId 为 0；FinishChapter 在 RestaurantRuntimeSystem 中明确只接受 Solo。 动机见 [proposal.md](proposal.md)。用户已授权实施，四项核心与一项普通入口整轮用例的最新有效结果均Passed。

## Goals / Non-Goals

目标模式：合作 Host/Client。业务范围见 proposal；复用原 ET Model/Hotfix 与 ModelView/HotfixView 分层，无新业务架构/消息总线。

前置合同：

- [product-mode-room-flow](../2026-10-15-product-mode-room-flow/design.md)

不要求 PC LAN、Android、Player/AOT 或全部其他 P1/P2 完成。涉及同一共享 owner 时依次交接。

## Decisions

### 1. 沿既有事实扩展

用户已明确批准合作首个大关三关；新增301→302→303和401→402→403，ChapterId=2，旧1/2、3/4循环配置只保留旧引用兼容。

### 2. 保留原子效果

关序、终关和菜单从合作配置读取，旧样机循环配置仅保留兼容入口；HUD 不把总关数硬编码为 3。

### 3. 共享边界与兼容

终关 Completed 先发布一致统计，Host 请求返回房间时退役餐厅，Client 不自行保存、建第四关或删除 Match。

### 4. 分批与模式交付

cooperative-result-claims 后续接入有资格的完成结果；本案的内存结果不得显示已保存。

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
      Child: RestaurantRuntime (optional after room-flow)
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

房间已由前案交付；泛化基线与SaveResult/Claim仍属后续案，本案不实现个人持久记录。直接复用 Level.IsChapterTerminal、Score/Stars、ServedOrders/UnfulfilledOrders 和明确关序；完成记录在本案仅为本轮只读结果。Host 确认后由 Root 的 FinishingRestaurant 交接退役该餐厅，Match 保留。ProductRoomClient 已由前案交付，独立Current/Candidate投影归ClientSession。

### 新关序和终关提交

保留旧1→2→2、3→4→4及Solo101/201三关源行；新增ChapterId=2的301→302→303（基础）和401→402→403（饮品），第三关Next=0且无装修。新开Host与空房间开局均从FirstCooperativeLevel读取，同一MenuVariant配置共用；CustomerFlowConfig同步追加对应6行。关序校验按连续Stage、唯一末端及相邻引用验证，HUD按实际ChapterId/MenuVariant计数，未把3写死到显示或通用校验。

终关仍由真实营业→Draining→前厅活动排空→Completed提交，结果先投影可见。Continue/SaveAndContinue在终关拒绝，非法引用不是完成。Host的FinishChapter先经过原Peer与HostSession两级有序邮箱，业务步复验当前世界、关、成员绑定和Completed后记录一次结果并设置FinishingRestaurant。Root下一次Update只处理同一Restaurant引用，递增RoomVersion，Stop/Dispose餐厅并广播无世界房间；Match/Participant/Session/Root sender存活。Solo继续原保存后返回选档路径。远端不能确认；重复确认不创建第二个退出任务，旧世界请求不能关闭新餐厅。普通终关按钮改为“完成大关并返回房间”，队友只读等待Host，且不显示个人保存成功。

### 发送、接收、裁决与返回



本地操作由 ProductClient 的当前 ClientSession/LocalMessage 取得真实 HostPeer EntityRef，并使用 peer.GetActorId()；ProductRoot 的 ProcessInnerSender.Call 入 MessageQueue，ProductHost SceneType 对应 Peer handler 接收。handler 校验绑定，再转到 authority.GetActorId() 的 HostSession Ordered MailBox；领域 owner 在允许业务步复验上下文和实物后提交。自动后继仍经现有内部消息 handler 接入原 owner，不能由观察事件直接提交。

远端由 Session.Call 发公开协议，ProductHost NetComponentOnRead/ProductWire 只在显式白名单内复制值、捕获原 wire RpcId、取得可信 HostPeer 再进入同一 Peer/HostSession 路径。本地 Host 不向自己发 UDP；远端不能自报内部 ActorId 直达。协议版本按实施时当前版本演进，当前实际协议为24；本案复用已有LevelControl/结果字段和RoomSnapshot，没有新增wire字段，不递增协议。

入口答复只表示 Accepted/Rejected；实际动作或阶段的成功/拒绝/取消沿 ProductCommandOutcome/GridCommandResult 或明确阶段通知返回。Host 提交同版快照后，各接收者独立 LocalDelivery/RemoteDelivery 通知 ProductClient handler，复制独立值后释放 DTO，再发布只读观察 Event；暂停期间控制邮箱仍工作。

### 创建、退出和消息所有权

创建时先准备 Root sender 与权限上下文，再创建权威/可选餐厅、可信 Peer 和客户端，最后加载原受管 HUD/Board；没有匹配世界、本人确认和视图时不开放地图输入。关卡变化只重建关内角色/工作，持续厨房沿原 owner 留存。

请求/答复每跳由其原调用/handler 收尾，跨 await/队列只留独立值和 EntityRef/InstanceId；同时核对 Match/World/Level/绑定代次。UI 关闭取消对外等待，原物理 Call 仍按答复/有限超时收尾，不提前重复回收池消息或另建等待字典。

成员失联仅撤旧绑定/未执行输入，按既有最后提交格落物和工作接续规则收尾，不停止 Root sender。关闭餐厅回房间只退役餐厅所属对象；整个运行解散才拒绝新入口、处理适用业务关闭、结束原等待并释放 sender/Scene。Root 释放不临时补成功/存档。

### 方案取舍

采用现有 owner 上的最小能力扩展，避免把有限首批内容变成独立框架。替代方案是为每菜/每模式复制流程，或一次实现完整候选目录；两者都会增加维护或推迟玩家可见交付。沿当前真实第二消费者增加必要复用字段，避免仅为潜在未来搭平台。

### 实施门槛与决策身份

用户已明确确认合作首个大关三关。 用户另已确认全部16案实施，合作三关及内容范围；本案不扩大为持久领取或新增菜品。

## Risks / Trade-offs

终关判断错误会误建第四关或过早释放结果。用终关/非法引用、重复结束、Client 越权及真实前厅排空 UTF，加一次普通 Editor 合作终点流程；复用原制作证据。 任务到最小验收的对应与接受的未验证项见 [execution-notes.md](execution-notes.md)。

## Migration Plan

先修改对应手写源和必要 Product Excel/Proto/CodeBind 源，再经既有生成入口更新消费者；不手改派生 C#/JSON/bytes 或 Unity YAML。旧有效配置/存档明确兼容，新增内容只在选定模式开放。

按实际变更选择相关 UTF 和普通 Editor 入口验收；已有行为未变结果直接引用。不对用户现场清空/回滚，不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)
- [Design/Excel/ET/Datas/Product/LevelConfig.xlsx](../../../../Design/Excel/ET/Datas/Product/LevelConfig.xlsx)
- [业务目标与本轮优先级](../../../../Docs/Product/full-version-roadmap.md)。
