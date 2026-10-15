# Design

## Context

已有持续 Companion 和关内 CompanionRole/Growth，创建时唯一配置名为“伙伴”；没有稳定可识别称呼、准备态聊天或完成工作后的适度正面反馈。 动机见 [proposal.md](proposal.md)。以下Context为创建时规划记录，事实来自 2026-10-15 当前工作区静态读取和既有执行记录，不声称重新运行通过。

## Goals / Non-Goals

目标模式：Solo 与已支持的本机合作。业务范围见 proposal；复用原 ET Model/Hotfix 与 ModelView/HotfixView 分层，无新业务架构/消息总线。

前置合同：

- 无新案硬前置，复用已交付主规格和相应原执行证据。

不要求 PC LAN、Android、Player/AOT 或全部其他 P1/P2 完成。涉及同一共享 owner 时依次交接。

持久化首轮验收仅扩展已有 Solo 成功档，合作先覆盖内存世界；合作基线/领取交付后才补对应持久验收，不能把共享代码当远端已保存。

交接责任：本案先于领取案交付时，由领取案集成覆盖已落地持久字段；本案后于领取案交付时，由本案持久化任务补现有合作保存入口的一次往返。同一运行可覆盖多个字段，不重跑未变内容。

## Decisions

### 本次有限表现方案（2026-10-15）

CompanionConfig继续同一Id1/Post1/出生(2,5)/Ask30/Clear20/Wash61；称呼改“小麦”，同一表追加三类聊天及工作/成长两条短文案。沿原中文字符串字段约定，不为本次建人物剧情表、远端聊天服务或新存档字段。

聊天选择只归ProductClient的GridPresentationComponent，使用具名UI枚举和现有信息区/受管按钮；原Board伙伴外观复用，HUD显示实际定义称呼。回调捕获当前World、Level/Generation和HudLoadEpoch，打开/关闭不发业务动作，不改物资、成长或营业状态；退出Preparing自动收起。HUD重开/换关清本地选择，旧按钮不能在新epoch或新关显示聊天。

工作结算先由原FrontWork提交并增长CompanionGrowth，再经原独立快照发布当前伙伴CompletedCount。通用FrontWork完成值没有执行者字段，且完成后会清认领，不能凭最后一条提示猜玩家/伙伴。表现因此观察当前World/Level/Role的已提交次数增量，显示“这边忙好啦！”；第三次跨越沿已有洗碗成长规则显示短反馈。只持单伙伴的显示基线和反馈文本/修订，不增长经验，不保留世界副本或新业务owner。首次显示/重开建立当前基线、不重播旧完成；重复消息/投影无新增次数则不再反馈。沿已有GridProjectionApplied/受管刷新，无新增网络消息或订阅总线；file12/wire34均不因纯表现增加版本。

本案原32项工作规则没有改变，复用其原证据；新增小集聚焦配置/实际成功档身份、普通UI三类聊天/完成与成长、旧回调/重开/跨关，以及真实KCP同一称呼和已提交事实。主规格旧Purpose称名字未定与用户已确认“小麦”不符，交付时同步该范围说明，其他原岗位/成长合同保留。

### 1. 沿既有事实扩展

身份与岗位仍归 RestaurantRuntime 下 Companion；当前工作与成长仍归 Level 的 Role/Growth，不再建人物存档权威。

### 2. 保留原子效果

称呼/台词从经确认的有限配置读取，聊天为只读展示，不发送改数值请求，也不能在准备态启动营业工作。

### 3. 共享边界与兼容

完成反馈消费已提交 FrontWork/Kitchen 消息及当前投影，不由动画触发工作完成/重复增长；重开视图不能重播旧关奖励。

### 4. 分批与模式交付

本案先丰富现有一位伙伴；增加人数、特殊岗位或人物事件必须另有具体决定。

### 表现改动保持轻量

沿现有Companion定义把显示称呼配置为“小麦”，有限聊天只在Preparing可读，不增加对话树、任务、奖励或人物存档字段。优先复用现有伙伴表现资源配清楚称呼，避免把新美术制作变成本案硬前置。

工作反馈观察已安装的FrontWork终态及关内成长值，使用当前World/Level/伙伴身份去重；不为表现新增业务请求、计时器或每帧拼文案。只持当前展示需要的有限状态，关闭/换关清理，重开不重播历史庆祝。

已有工作/成长用例结果仅在行为未变时复用；本案新增验证只覆盖只读聊天、反馈与受管生命周期。保存恢复仅验证稳定定义仍映射“小麦”，不复制完整存档验收。

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

房间入口已实现，等待房间时可以没有餐厅；基线接口复用其最新实际交付，SaveResult/Claim仍由领取案实现。持续 Companion 的 DefinitionId/Name/Post 负责身份；Level Child CompanionRole 和 Component CompanionGrowth 继续负责当前工作/成长。聊天仅本端受管展示，不新增人物数值、任务队列或存档 owner。

### 发送、接收、裁决与返回

聊天只从当前受管视图读取确认的静态文案和有效 Preparing 投影，不发送业务动作。成长/完成反馈沿现有 FrontWorkOutcomeApplied 等独立值事件观察；Event 不增长经验，不完成任务。

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

2026-10-15 已确认保留一位伙伴“小麦”，准备态聊天围绕备菜/合作/收店，真实工作后简短正面反馈。文案与识别表现可在该范围内编写，最终伙伴人数仍未确定。


## Risks / Trade-offs

低影响表现改动，主要是误触动作或旧回调重复反馈。使用受管视图/只读聊天和原成长行为小集，一次 Editor 跨关检查即可，不扩大伙伴规则测试。 任务到最小验收的对应与接受的未验证项见 [execution-notes.md](execution-notes.md)。

## Migration Plan

先修改对应手写源和必要 Product Excel/Proto/CodeBind 源，再经既有生成入口更新消费者；不手改派生 C#/JSON/bytes 或 Unity YAML。旧有效配置/存档明确兼容，新增内容只在选定模式开放。

按实际变更选择相关 UTF 和普通 Editor 入口验收；已有行为未变结果直接引用。不对用户现场清空/回滚，不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/FrontService.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/FrontService.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/CompanionSystems.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/CompanionSystems.cs)
- [Design/Excel/ET/Datas/Product/CompanionConfig.xlsx](../../../../Design/Excel/ET/Datas/Product/CompanionConfig.xlsx)
- [Docs/Product/design.md](../../../../Docs/Product/design.md)
- [业务目标与本轮优先级](../../../../Docs/Product/full-version-roadmap.md)。
