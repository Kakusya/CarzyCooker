# Design

## Context

GridKitchenRules 固定 7×18、KitchenBottom=6；寻路缓冲按常量预分配。已有装修只在固定地图增加工位，缺少空间解锁和作为经营选择的餐桌扩建。 动机见 [proposal.md](proposal.md)。本案处于规划阶段，事实来自 2026-10-15 当前工作区静态读取和既有执行记录，不声称重新运行通过。

## Goals / Non-Goals

目标模式：Solo 与已支持的本机合作。业务范围见 proposal；复用原 ET Model/Hotfix 与 ModelView/HotfixView 分层，无新业务架构/消息总线。

前置合同：

- [restaurant-unlock-permissions](../2026-10-15-restaurant-unlock-permissions/design.md)

不要求 PC LAN、Android、Player/AOT 或全部其他 P1/P2 完成。涉及同一共享 owner 时依次交接。

持久化首轮验收仅扩展已有 Solo 成功档，合作先覆盖内存世界；合作基线/领取交付后才补对应持久验收，不能把共享代码当远端已保存。

交接责任：本案先于领取案交付时，由领取案集成覆盖已落地持久字段；本案后于领取案交付时，由本案持久化任务补现有合作保存入口的一次往返。同一运行可覆盖多个字段，不重跑未变内容。

## Decisions

### 1. 沿既有事实扩展

地图尺寸/区域来源归厨房 owner，旧有效档默认原 7×18 几何；新地图有明确边界，寻路缓冲在安装时分配并复用，不每帧重建。

### 2. 保留原子效果

餐桌新增须有真实 Table/座位/顾客路径/餐具池容量检查，不只多画一张桌贴图或复制顾客。

### 3. 共享边界与兼容

扩建选择与安装沿现有装修链，非法占位、入口/操作格不达或无承接位置全部拒绝，不销毁旧物修补。

### 4. 分批与模式交付

HUD 保持竖向查看与同一坐标输入，厨房一屏可见的合同需按有限首批几何验证；不把更大地图变成无法操作的缩小图。

### 控制几何改动范围

只提供一个配置扩建定义及其有限尺寸/区域；优先保留原有格坐标、工位与物品身份，不引入地图编辑器或运行中任意缩放。旧有效档默认原7×18几何。常量替换仅覆盖实际消费者：厨房校验/路径缓冲、玩家/伙伴/顾客、前厅座位、投影、Board点击/镜头及存档。

新增一张单客桌必须同时有真实Table定义、座位、入口/服务/离开路径和足够餐具池容量。候选、所选、已安装三阶段分离；继续时一次预检区域与桌位，失败仍留原选择和旧关。缓冲在几何安装时按有界容量创建并复用，不每帧分配。

本案的 restaurant-renovation 修改块在 unlock 之后累积，保留已裁决的禁新盛装规则；不得用旧规划文本撤回前案合同。合作保存只在领取/基线接口实际交付后针对本案新增几何补一次往返，不把整个存档套件变成扩建前置。

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
        Component: GridKitchen (Width/Height + reusable BFS) -> Child Station (+FrontTable) / Item / Process
        Component: RestaurantRenovation
        Child: Companion (persistent definition identity)
        Child: Level -> Child GridActor / Customer / Order / FrontWork / CompanionRole
          Component: FrontLevelState / FrontWorkCommands / CompanionGrowth / LevelBuff
    Component: NetComponent -> Child Session (Host only)
  Child Scene: ProductClient (SceneType.ProductClient)
    Component: ClientSession + Ordered MailBox -> Component GridClient (independent values)
    Components: GridPresentation / UIComponent / GFEntityComponent
```

房间入口已实现，等待房间时可以没有餐厅；基线接口复用其最新实际交付，SaveResult/Claim仍由领取案实现。几何、可用区域和路径缓冲归现有 GridKitchenComponent；GridStation/FrontTable 保持原身份。保存载荷捕获已提交区域；GridClient 独立复制几何，Board 仅做坐标映射与显示。

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

2026-10-15 已确认首批一块有限扩建区加一张普通单客桌。前置为解锁/许可合同，具体格坐标与尺寸按现有地图可达性选定；不扩大最终规模。


## Risks / Trade-offs

共享几何变更会影响输入与三类角色寻路，需小范围跨消费者验证。选择一次代表性扩建，覆盖占位拒绝、旧物/加工保留、顾客入座和保存恢复，接受其他候选地图 NotRun。 任务到最小验收的对应与接受的未验证项见 [execution-notes.md](execution-notes.md)。

## Migration Plan

先修改对应手写源和必要 Product Excel/Proto/CodeBind 源，再经既有生成入口更新消费者；不手改派生 C#/JSON/bytes 或 Unity YAML。旧有效配置/存档明确兼容，新增内容只在选定模式开放。

按实际变更选择相关 UTF 和普通 Editor 入口验收；已有行为未变结果直接引用。不对用户现场清空/回滚，不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridStationConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridStationConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridFrontLayout.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridFrontLayout.cs)
- [Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoGridKitchenBoard.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoGridKitchenBoard.cs)
- [业务目标与本轮优先级](../../../../Docs/Product/full-version-roadmap.md)。


### 当前实施选择

从09已验收的v8/wire29交接。首批几何7×18→9×18，只增右侧一块有限区域，原坐标与KitchenBottom=6保持；餐桌(7,3)及单人座位(6,3)沿奖励8整体安装。Growth现有资源批次提供有限候选，不另造首发章节。Table3无初始布局身份，仅有奖励来源，旧桌定义和已有物品不变。

尺寸/区域归GridKitchen，玩家与前厅角色共同走其可达性/BFS；路径数组按提交后的尺寸扩容并复用。配置预检、保存校验使用明确几何参数，默认旧尺寸。Board沿原GFEntity view重配地砖/镜头，不另建实体权威、Prefab或池；所有点击仍经原客户端命令。

持久化新增几何纯值，恢复先按合法已保存几何验证和建原工位，再沿原Pending奖励路径只安装一次；未成功的布局不另存。新档/合作成功载荷和旧档只读适配分别由本案真实临时文件验收，不假设09文件通过已证明扩建字段。

### 实际源与版本交接

文件v9在原Payload的第13成员追加SavedKitchenGeometry（宽/高），Header仍28成员；旧v1–v8缺失字段只读采用7×18。已安装几何须对应实际扩建奖励及其唯一桌实例；尚待安装的奖励分别验证原几何、原物坐标及未来桌位/座位，恢复只安装一次。合作成功内容版本保持原独立字段，v8旧签名载荷先适配几何再首次发布当前v9。wire30沿已有Snapshot.Width/Height传已提交值，没有另加网络通道。

Board仍为原GFEntity view，几何变化重用已有地砖并更新边界/镜头，首启外的几何重配不重复注册场景相机；关闭释放仍沿原owner。高频Configure只比较尺寸，稳定尺寸立即返回，不重建全部精灵。路径数组在尺寸提交时扩容并复用，布局预检/前厅可达性同步实际尺寸。
