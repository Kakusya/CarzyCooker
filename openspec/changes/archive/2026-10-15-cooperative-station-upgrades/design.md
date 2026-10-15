# Design

## Context

Solo 已有单级原位升级与冻结耗时；CompleteRenovationPreparation 对 RewardKind=2 在非 Solo 模式直接跳过，合作候选只有新增工位。 动机见 [proposal.md](proposal.md)。本案处于规划阶段，事实来自 2026-10-15 当前工作区静态读取和既有执行记录，不声称重新运行通过。

## Goals / Non-Goals

目标模式：合作 Host/Client。业务范围见 proposal；复用原 ET Model/Hotfix 与 ModelView/HotfixView 分层，无新业务架构/消息总线。

前置合同：

- 无新案硬前置，复用已交付主规格和相应原执行证据。

不要求 PC LAN、Android、Player/AOT 或全部其他 P1/P2 完成。涉及同一共享 owner 时依次交接。

## Decisions

### 1. 沿既有事实扩展

复用 StationUpgradeConfig、RenovationChoiceIntent 和原控制资格，不新增远端升级旁路。

### 2. 保留原子效果

远端 ChooseRenovation 即使直接构造请求也须拒绝；升级候选与当前目标同版快照复制。

### 3. 共享边界与兼容

先检查实际协议/字段消费者，只有确有新载荷才递增协议；不预定统一的下一版数字。

### 4. 分批与模式交付

保存功能未接入时界面明确本轮内存升级，后续基线/结果保存包含已提交等级。

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

原类型来自当前源码；optional 房间、泛化基线和 SaveResult/Claim 为所标前置案的计划变化，不声称已实现。等级和 UpgradeDefinitionId 仍归 GridStation，目标及预期等级仍归 RestaurantRenovation。旧 GridAutoProcess/GridBakeProcess/GridManualProcess 的冻结参数不重算，没有第二份升级账本。

### 发送、接收、裁决与返回



本地操作由 ProductClient 的当前 ClientSession/LocalMessage 取得真实 HostPeer EntityRef，并使用 peer.GetActorId()；ProductRoot 的 ProcessInnerSender.Call 入 MessageQueue，ProductHost SceneType 对应 Peer handler 接收。handler 校验绑定，再转到 authority.GetActorId() 的 HostSession Ordered MailBox；领域 owner 在允许业务步复验上下文和实物后提交。自动后继仍经现有内部消息 handler 接入原 owner，不能由观察事件直接提交。

远端由 Session.Call 发公开协议，ProductHost NetComponentOnRead/ProductWire 只在显式白名单内复制值、捕获原 wire RpcId、取得可信 HostPeer 再进入同一 Peer/HostSession 路径。本地 Host 不向自己发 UDP；远端不能自报内部 ActorId 直达。协议版本按实施时当前版本演进，当前扫描值为 22，不预定多个案的统一下一版。

入口答复只表示 Accepted/Rejected；实际动作或阶段的成功/拒绝/取消沿 ProductCommandOutcome/GridCommandResult 或明确阶段通知返回。Host 提交同版快照后，各接收者独立 LocalDelivery/RemoteDelivery 通知 ProductClient handler，复制独立值后释放 DTO，再发布只读观察 Event；暂停期间控制邮箱仍工作。

### 创建、退出和消息所有权

创建时先准备 Root sender 与权限上下文，再创建权威/可选餐厅、可信 Peer 和客户端，最后加载原受管 HUD/Board；没有匹配世界、本人确认和视图时不开放地图输入。关卡变化只重建关内角色/工作，持续厨房沿原 owner 留存。

请求/答复每跳由其原调用/handler 收尾，跨 await/队列只留独立值和 EntityRef/InstanceId；同时核对 Match/World/Level/绑定代次。UI 关闭取消对外等待，原物理 Call 仍按答复/有限超时收尾，不提前重复回收池消息或另建等待字典。

成员失联仅撤旧绑定/未执行输入，按既有最后提交格落物和工作接续规则收尾，不停止 Root sender。关闭餐厅回房间只退役餐厅所属对象；整个运行解散才拒绝新入口、处理适用业务关闭、结束原等待并释放 sender/Scene。Root 释放不临时补成功/存档。

### 方案取舍

采用现有 owner 上的最小能力扩展，避免把有限首批内容变成独立框架。替代方案是为每菜/每模式复制流程，或一次实现完整候选目录；两者都会增加维护或推迟玩家可见交付。沿当前真实第二消费者增加必要复用字段，避免仅为潜在未来搭平台。

### 实施门槛与决策身份

无新增必要产品裁决，沿用已有单级规则。 用户本轮仅确认“按已确认目标、菜品分批扩展”，未决产品规则或推荐菜单未因此变成逐项批准。任务从该门槛开始，未解决部分不能标为可直接实施。

## Risks / Trade-offs

风险是双装升级或重算旧加工。相关 KCP/受管 HUD 小集覆盖重复、换关、目标变化和作业前后耗时；不扩大原 Solo 整套回归。 任务到最小验收的对应与接受的未验证项见 [execution-notes.md](execution-notes.md)。

## Migration Plan

先修改对应手写源和必要 Product Excel/Proto/CodeBind 源，再经既有生成入口更新消费者；不手改派生 C#/JSON/bytes 或 Unity YAML。旧有效配置/存档明确兼容，新增内容只在选定模式开放。

按实际变更选择相关 UTF 和普通 Editor 入口验收；已有行为未变结果直接引用。不对用户现场清空/回滚，不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/StationUpgradeConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/StationUpgradeConfig.cs)
- [Unity/Assets/Tests/Editor/ET/StationUpgradeTests.cs](../../../../Unity/Assets/Tests/Editor/ET/StationUpgradeTests.cs)
- [Unity/Assets/Tests/Editor/ET/CoopRenovationTests.cs](../../../../Unity/Assets/Tests/Editor/ET/CoopRenovationTests.cs)
- [业务目标与本轮优先级](../../../../Docs/Product/full-version-roadmap.md)。
