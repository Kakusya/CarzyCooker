# Design

## Context

规划时 SoloSaveResult 绑定单机 Profile，通过 ResultId 去重新建文件；合作尚缺开局冻结的 Profile 资格、各端领取凭证、持久化 ACK 或 normal-close receipt。动机见 [proposal.md](proposal.md)。以下保留原设计取舍，当前实现交接及实际运行见 Migration Plan 和 execution-notes。

## Goals / Non-Goals

目标模式：PC 同平台持久进度与合作。业务范围见 proposal；复用原 ET Model/Hotfix 与 ModelView/HotfixView 分层，无新业务架构/消息总线。

前置合同：

- [cooperative-chapter-completion](../2026-10-15-cooperative-chapter-completion/design.md)
- [progress-authorization-contracts](../2026-10-15-progress-authorization-contracts/design.md)
- [cooperative-save-baseline](../2026-10-15-cooperative-save-baseline/design.md)

不要求 PC LAN、Android、Player/AOT 或全部其他 P1/P2 完成。涉及同一共享 owner 时依次交接。

## Decisions

### 2026-10-15 已确认协议交接

按用户已确认的[离线合作进度协议](../../../../Docs/Product/offline-progress-protocol.md)实现：Level 开始冻结已证明 Profile；本次 MatchAuthority 委托 Host 签发稳定结果/资格；Client 先核验并持久化 Ticket 再 ACK。消费键 SaveResultId+EligibleProfileId，目标记录与领取回执同次发布，重试返回原结果。normal-close receipt 使用同 Match 授权身份签名，只关闭实际持久化该回执的本地未领取资格。没有中心服务，不承诺跨设备全局一次；ResumeCredential 不替代长期证明。

### 1. 沿既有事实扩展

冻结资格属于 Level 开始提交点，迟加入者不领取本关，恢复原合资格 Profile 不能换人；离线资格不阻塞其他玩家。

### 2. 保留原子效果

SaveResult/Claim 运行对象归 Match；持久凭证归本端 repository，不能保留 Host EntityRef/网络 Session。

### 3. 共享边界与兼容

凭证投递、持久化 ACK、主动领取提交是三件事。ACK 丢失重发相同身份；文件失败保留资格和旧档，结果丢失重查返回同一已提交记录。

### 4. 分批与模式交付

Host 本轮前进按已确认的成功检查点提交政策执行；某远端个人尚未领取不是 Host 卡关条件。确认保存失败时不公布已保存或绕过应有换关门槛。

### 5. 提交责任

normal-close receipt 在拆连接前按协议发送；只关闭实际收到并持久化的本地未领取资格。未收到回执者按异常观察保留合法已存凭证。

### 实际前置和最小提交链

基线案已交付 OfflineProfiles、CooperativeSaveBinding、ProductProgressMessages 和受管进度入口；本案复用其 Profile/公钥绑定、挑战证明和 ReadRun/MatchAuthority，不重做身份系统。开始实施前读取其最新 execution-notes，复用其中与当前代码一致的通过结果，新增领取行为另验。

1. 在合法 Level Start 提交点冻结已验证 Profile；同 Profile 只占一份资格。成功、前厅收口及结算选择确定后冻结一次结果，避免 Ticket 携带尚未确定的装修。
2. 本端 repository 验证并持久化 Ticket 后才发具名 ACK；Host 按有限重发节奏在 Match 存续期间继续重发未确认 Ticket，不新增身份或另一份 RPC 等待表。
3. 玩家主动领取在同一个目标记录内发布载荷与领取回执。回执持久存在才表示已领取；发布后丢答复或附加标记写失败，重开须查到原 Slot/revision，不能再次新建。
4. Host 选择保存并继续/结束时，等待本端合法新建提交成功；失败留在原结算。远端未领不阻塞 Host，Host 也不能凭世界权威覆盖非本人的基线。
5. 关闭整个 Match 才发本 Match 的 NormalCloseReceipt；仅退役餐厅返回同一房间不关闭整个 Match 的资格。回执先持久化则后到 Ticket 仍关闭；已领取记录保留，未持久化回执的端按异常观察处理。

repository 属 ProductRoot，目标 ActorId 从本端当前组件 GetActorId() 取得，处理器匹配 SceneType.ProductRoot 的有序 MailBox。文件响应携稳定结果身份返回原 Client/Match，await 后复验实例；离线领取不经已经释放的 HostPeer。消息/票据所有权与业务快照保持分离。

### 内容案的持久化验收交接

本案的普通入口集成覆盖实施时已落地的新持久字段：用一个同时包含这些字段的代表性成功结果验证领取、重开及继续，不重复逐菜运行整套。后于本案交付的内容案在其持久化任务中补已有合作保存入口的一次往返；同一结果可覆盖多项字段。未接入的未来内容不成为本案前置，执行说明逐项列出实际覆盖字段与原结果。

### 状态与 ET 释放归属

```text
Product Fiber / Root Scene (SceneType.ProductRoot)
  Component: ProductRuntime / Timer / CoroutineLock / ProcessInnerSender
  Component: SoloSaveRepository + Ordered MailBox (SceneType.ProductRoot)
  Child Scene: ProductHost (SceneType.ProductHost; Solo/Host)
    Component: HostSession + Ordered MailBox
      Child: HostPeer + Ordered MailBox
        Component: LocalDelivery or RemoteDelivery
    Child: Match
      Child: Participant (connection-independent)
      Child: SelectedSaveBaseline (read-only; from save-baseline)
      Component: CooperativeClaimState (results / per-binding ACK / normal-close deadline)
      Child: CooperativeSaveResult -> Child CooperativeClaim
      Child: CooperativeCloseReceipt (whole Match normal close only)
      Child: RestaurantRuntime (absent while waiting in room)
        Component: GridKitchen -> Child Station / Item / Process
        Component: RestaurantRenovation
        Child: Companion (persistent definition identity)
        Child: Level -> Child GridActor / Customer / Order / FrontWork / CompanionRole
          Component: FrontLevelState / FrontWorkCommands / CompanionGrowth / LevelBuff
    Component: NetComponent -> Child Session (Host only)
  Child Scene: ProductClient (SceneType.ProductClient)
    Component: ClientSession + Ordered MailBox -> Component GridClient (independent values)
    Components: ClientProgressDelivery / GridPresentation / UIComponent / GFEntityComponent
```

房间入口已实现，等待房间时可以没有餐厅；基线接口复用其最新实际交付。本案现已在 Match 创建共同结果、逐Profile票及正常关闭回执，Level 只持开局冻结的 Profile 资格和当前成功关联。Client 本地 repository 持久凭证/领取提交，不保存 ET 引用。文件事务字段和认证按已定稿离线协议执行。

### 发送、接收、裁决与返回

成功结果在原 HostSession/Restaurant 提交后由 Match 持有并逐人发送。ProductClient 收到有效凭证后，先 Call 本端 repository 持久化，返回成功后再经原 Peer→HostSession 发具名 ACK；ACK 不等于领取。主动领取与 normal-close receipt 的持久处理也走本端 repository，完成再回所属 Match/Client；离线领取无需旧 Actor/Session 存活。

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

离线首版协议已经确认；资格证明和基线接口复用 cooperative-save-baseline 的实际交付，不能把该案文件存在当作验收通过。合作终关与房间已交付，继续沿其原控制合同接入。


## Risks / Trade-offs

主要是误授资格、重复领取或失败吞资格，采用真实本地文件和 KCP 小集覆盖资格/ACK/提交/关闭四条路径，保留失败输出；不建立跨设备资产平台。 任务到最小验收的对应与接受的未验证项见 [execution-notes.md](execution-notes.md)。

## Migration Plan

### 当前实现交接（2026-10-15）

`Match -> Component CooperativeClaimState / Child CooperativeSaveResult -> Child CooperativeClaim` 持有本 Match 的稳定结果/逐 Profile 票与逐 binding ACK；`Match -> Child CooperativeCloseReceipt` 只用于整个 Match 的显式正常关闭。Level 的 EligibleProfiles 在合法 Start 冻结，ClaimResult 在自然 Completed 且装修选定/明确跳过后关联一次；终关不等待装修。以上均为原 ProductHost Fiber 内的对象，不新建 SceneType 或发送器。

结果 owner 通过 Root repository 的有序邮箱私有 `ProgressRepositoryRequest` 签发 MatchAuthority、成功结果及票；签发完成以 `ProgressPreparedRequest` 返回原 HostSession，核对 Match 和结果 InstanceId。`ProgressPulse` 每秒送一次原 HostSession 邮箱，未 ACK 的同一票按当前已证明 Peer/绑定重投。wire 仅新增公开的 Delivery、Ack 和 CloseMatch，内部提交、签发、文件请求及回调不开放远端。

ProductClient 将 Delivery 复制成独立值再 Dispose DTO，`ClientProgressDelivery` 顺序请求本端 Root 验链与原子持久化；成功响应且票身份吻合后经 Peer -> HostSession ACK。失败没有 ACK，由 Match 原票重试。主动领取走 Root repository，不调用旧 Host/Session；新 `ccsave` 同时包含载荷及消费键到原 Slot/revision 的映射，后续 Solo 检查点继承该事实。格式为 v5，旧 v1–v4 保留适配，wire 为26。

Host 保存后继续/完成仍走原 Level 控制邮箱：Result 拥有写盘观察者，`ProgressHostSavedRequest` 只在原结果实例收口；成功后把原控制放回 Restaurant.Controls，失败停留原结算。远端的个人领取与此门槛无依赖。仅退役餐厅回房间不创建关闭回执。

显式结束 Match 先停止该餐厅新输入，用已有签名授权为有结果的 Profile 准备 NormalCloseReceipt，接收端持久化后 ACK；关闭状态每秒观察，最多等待4秒，离线者不假定收到。入口查询已完成关闭阶段后才调用既有安全时点模式切换；Dispose 不补签或制造结果。签发结果正在准备时先返回 ProgressPreparing，避免正常关闭后迟到结果重新开放资格。

协议已经定稿；先复用已交付身份/授权接口并以隔离测试文件验证格式、权限和失败保旧，再开放新入口。格式从届时实际版本递增，保留旧有效输入只读适配，未知/损坏记录拒绝且不改原文件。

按实际变更选择相关 UTF 和普通 Editor 入口验收；已有行为未变结果直接引用。不对用户现场清空/回滚，不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveCommitSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveCommitSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveRepositoryMessages.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveRepositoryMessages.cs)
- [Docs/Product/specs/save-progress-claims/spec.md](../../../../Docs/Product/specs/save-progress-claims/spec.md)
- [业务目标与本轮优先级](../../../../Docs/Product/full-version-roadmap.md)。
