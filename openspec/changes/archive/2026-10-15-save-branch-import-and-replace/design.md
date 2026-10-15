# Design

## Context

Solo repository 只允许 File.Move 不覆盖新建，header 仅有直接来源字段；没有授权包导入、根谱系/授权范围或预期修订覆盖实现。 动机见 [proposal.md](proposal.md)。此段为创建时现状；实际从已交付v5/wire26前置接入并完成v6/wire27，运行结果见execution-notes。

## Goals / Non-Goals

目标模式：PC 同平台持久进度与合作。业务范围见 proposal；复用原 ET Model/Hotfix 与 ModelView/HotfixView 分层，无新业务架构/消息总线。

前置合同：

- [progress-authorization-contracts](../2026-10-15-progress-authorization-contracts/design.md)
- [cooperative-save-baseline](../2026-10-15-cooperative-save-baseline/design.md)
- [cooperative-result-claims](../2026-10-15-cooperative-result-claims/design.md)

不要求 PC LAN、Android、Player/AOT 或全部其他 P1/P2 完成。涉及同一共享 owner 时依次交接。

## Decisions

### 2026-10-15 已确认协议交接

用户明确接受[离线合作进度协议](../../../../Docs/Product/offline-progress-protocol.md)。ImportBranch 由有效 Owner 签发文件授权，导入立刻新建接收者独立分支，保留直接/根来源及 GrantId；原 Owner 不可远程撤回该分支。覆盖另验 ReplaceGrant 的目标 Owner/Slot/预期 revision/来源根/接收 Profile，并呈现本次有损确认。发布前失败保旧、保资格；成功目标与领取回执同次原子发布，不保留用户可恢复备份。无中心服务或任意断电保证。

### 1. 沿既有事实扩展

覆盖入口必须呈现目标、Owner、修订和有损含义，明确确认由本次用户动作获得，禁止把领取点击等同覆盖许可。

### 2. 保留原子效果

用已确认最小文件提交策略扩展原 repository，临时数据不发布为可选档；领取回执与完整目标同次发布，发布成功后才向调用者报告领取成功。

### 3. 共享边界与兼容

知道谱系 ID 不代表写权限；导入后不等待原 Owner 在线，源档不被导入过程改写。

### 4. 分批与模式交付

格式升级保留旧有效档只读适配；一次提交的故障恢复范围由前置协议确定，不声称任意断电事务。

### 先交接共用提交点，再分两种用途

复用基线案的本地身份、可信公钥与源记录验证，复用领取案的结果/资格回执；本案不另建凭证仓库。ImportBranch 只要求有效导入用途和本端接收身份，立即新建分支；ReadRun 封套不隐含 ImportBranch，不能因两者含相同源记录就互换。

ReplaceGrant 核验目标 Owner/Slot/预期revision、接收Profile和允许来源根；确认页绑定同一组值。目标变化后必须重新显示并确认，不能用旧确认覆盖新revision。repository 从稳定Slot解析本端路径，远端不能指定任意文件路径。

暂存完整目标及领取回执，原子发布一次；回执不能等文件替换后再独立写入作为唯一消费证据。后续替换/检查点保留既有本端领取事实。发布前失败保旧保资格；发布后答复丢失重查返回同一已提交结果。未知格式、错用途或签名拒绝不触碰源和目标。

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
      Child: SaveResult / Claim (proposed by result-claims only)
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

房间、基线和SaveResult/Claim均复用00–06实际交付；等待房间时可以没有餐厅。授权、来源谱系和预期修订属于纯值文件记录；运行许可归当前已验证基线，文件访问由本端 repository 校验。授权记录不作为可写世界，也不把远端 Session 当文件 Owner。

### 发送、接收、裁决与返回

导入/覆盖属于本设备Root repository请求，不经过HostPeer或远端Session。模式页、房间或地图的ProductEntry从Root取得实际SoloSaveRepository.GetActorId()，其受管UI AsyncOwner经Root.ProcessInnerSender.Call→MessageQueue→SceneType.ProductRoot的SaveAuthorizationHandler/有序MailBox；handler验签、目标与本端资格，并在同一目标发布中写入回执。池答复中的Preview/Document/Receipt复制为独立值后释放；UI核对实例、视图Epoch、RenderVersion、ProgressSerial和当前连接身份后呈现。关闭只取消观察，已发送物理请求按原答复/超时收尾；取消确认页则根本不发送写请求。

导入确认前仅展示签名摘要并要求主动公钥绑定；同Profile冲突公钥拒绝。覆盖预览绑定Ticket/Grant、目标Owner/Slot/revision/原Result及新结果/领取Profile。提交重读目标；变化即清旧确认，必须重新查看及取得有效当前修订许可。仅Root本地请求能够触达文件Handler，不进入ProductWire白名单。文件操作不创建世界；选档继续由既有SoloSavePresentation或基线读取入口创建。

下述运行世界消息路径沿用05/06，本案不增加世界写权：

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

离线协议已确认。ImportBranch 与 ReplaceGrant 是独立用途；依赖基线案的身份/验签和领取案的原子回执合同。覆盖测试只使用本案隔离目录，玩家操作每次明确确认目标。


## Risks / Trade-offs

这是会覆盖文件的改动，验证比内容案严格。用独立临时目录真实文件核对成功、权限/修订冲突、写入失败、重复与跨平台拒绝，避免动用户现有档。 任务到最小验收的对应与接受的未验证项见 [execution-notes.md](execution-notes.md)。

## Migration Plan

### 实施中的共用提交交接

实际前置为已交付的文件v5/wire26，已有根谱系与领取回执。此处按同一任务合同演进为文件v6/wire27：消费回执记录消费Profile、目标Owner/Slot/revision及结果或Grant身份，原v5平行字段仅作读取适配。后续覆盖保留回执；重复请求返回当时的Slot/revision元数据，不要求或保留旧载荷。新建导入沿原同目录发布，目标替换使用系统原子替换且不产生备份。

导入不拷贝源设备的消费台账，只合并本端已持久化事实。源Owner离线后，接收者Own Profile的新Slot可独立继续。ReplaceGrant绑定本端已存在目标的Owner/Slot/预期revision、接收Profile/公钥与允许根；默认签发同根范围，目标有变化即拒绝并要求重新确认及有效授权。Root从PC库目录和稳定ID解析目标，不接受任意写路径；另一Owner私钥从不进入接收者或新档。

ET树不新增权威：本案仅扩展ProductRoot repository的私有有序请求及原ProductEntry/领取UI；复用06的Match结果、逐Profile票与关闭回执。签名封套、公钥主动绑定及原实例/视图代次检查继续使用05/06交付，文件答复为独立元数据，离线导入/领取无需旧Session存活。

协议已经定稿；先复用已交付身份/授权接口并以隔离测试文件验证格式、权限和失败保旧，再开放新入口。格式从届时实际版本递增，保留旧有效输入只读适配，未知/损坏记录拒绝且不改原文件。

按实际变更选择相关 UTF 和普通 Editor 入口验收；已有行为未变结果直接引用。不对用户现场清空/回滚，不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveFile.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveFile.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs)
- [Docs/Product/specs/save-progress-claims/spec.md](../../../../Docs/Product/specs/save-progress-claims/spec.md)
- [业务目标与本轮优先级](../../../../Docs/Product/full-version-roadmap.md)。
