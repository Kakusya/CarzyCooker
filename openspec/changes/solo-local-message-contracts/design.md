# Design

## Context

动机见[proposal.md](proposal.md)，增量见[product-session](specs/product-session/spec.md)。提案基线协议3，实施基线4，当前九案协议12；Solo不创建NetComponent，本地依次Call HostPeer／HostSession，后者快速接收，唯一业务Tick提交世界。本change6/6的真实运行见[执行记录](execution-notes.md)。

原CallProductValueAsync外层finally在所属等待返回时EndEntry的风险已由真实UTF复现并修复：EndEntry现在交给CallLocalValueAsync内部物理观察者，取消对外等待不会提前释放Peer，响应／超时／Stop收尾后才结束原在途。ET.Core发送器未重写，原停止／跨实例证据复用。

## Goals / Non-Goals

**Goals:** 让各单机功能经可识别、单次执行、可取消和可验证的原消息路径提交。

**Non-Goals:** 不建通用 CommandBus、第三份请求等待表、可靠消息服务或测试平台；不承诺任意历史结果查询、自动重试、撤销已提交事实或物理等待即时取消。

## Decisions

### 1. 原树与入口寿命（本change实施基线）

```text
Main Fiber / SceneType.ProductRoot
  Component ProductRuntime / Timer / CoroutineLock / ProcessInnerSender
  Child SceneType.ProductHost (Solo authority, no network listener)
    Child Match
      Child Participant...
      Component GridKitchenComponent
        Child GridActor / GridStation / GridItem / GridManualProcess...
    Component HostSessionComponent + Ordered MailBox
      Child HostPeer + Ordered MailBox
        Component LocalDelivery
  Child SceneType.ProductClient
    Component ClientSessionComponent + Ordered MailBox
      Component GridClientComponent (independent values)
    Component LocalMessageComponent (HostPeer EntityRef)
    Component GridPresentation / UIComponent / GFEntityComponent
```

创建顺序为配置、根 Sender、Host／Match／厨房／权威入口、Client／Peer／本地绑定、Join、Tick 角色和快照、受管视图。单客户端退出撤销资格并结清所属等待，Peer Closing 且物理 InFlight 为零才移除；整体先拒绝、StopGrid、关视图，再 sender.Stop 和原树 Dispose。后续正式餐厅树只由 [流程 change](../solo-restaurant-level-flow/design.md) 迁移，不在这里提前创建 Level。

九案最终Solo已由流程change迁移到RestaurantRuntime.Component Kitchen和Child Level；最终完整树以该设计为准。消息发送、接收和物理在途责任沿本节保留，UI操作等待现归当次UIGridKitchen.AsyncOwner，加载等待归GridPresentation，见[UI设计](../solo-restaurant-ui/design.md)。

### 2. 类型化上下文，保留有限重放规则

Model/Share/Product 增加普通只读上下文值：MatchId、WorldId／Generation、可选 LevelId／Generation、BindingGeneration、CommandId。收到的 PeerId／InstanceId 由可信入口补入内部请求。各具体请求仍有独立类型及字段，禁止任意字符串 action、object payload 或由 UI 选择内部 ActorId。

ClientSession 统一分配新增动作族的业务编号；HostPeer 持最新上下文、完整动作值和接收／最终结果元数据，属于入口记账而非第二份厨房事实。共同入口比较完整值，最新相同值返回当前事实；冲突或较旧编号拒绝。已有 GridActor 的状态显示可以引用对应结果，但不另裁决编号。不为每种功能建无限去重缓存。编号随绑定隔离，关卡变更再核对代次；控制换关的结果包含旧关完成及新关身份，旧请求不会安装第二个新关。

### 3. 完整发送、接收与返回

玩家的 View/System → `ClientSession.CallProductValueAsync(context, typedRequest)` → `LocalMessage.Peer` 捕获原 EntityRef → root `ProcessInnerSender.Call(peer.GetActorId())` → MessageQueue → Peer Ordered MailBox → ProductHost 的对应 Peer handler。Grid 沿用 GridPeerHandler；后续 Level／Layout／Serve／Buff 各增自己的类型化 handler，不增通用路由器。

Peer handler 捕获自己的可信 InstanceId／Generation，并创建独立内部 Submit 请求 → 同一 root sender.Call(authority.GetActorId()) → HostSession Ordered MailBox → ProductHost Submit handler：验世界／关／绑定／编号，记录意图并返回接收结果。RPC 答复经原两段 sender 等待表返回，不等待动作到达或加工完成。Tick 再验当前依赖，提交后产生最终值结果。

通知：每个有效 Peer 各建独立 snapshot/result → LocalDelivery → sender.Send(client.GetActorId()) → ProductClient 的 ClientSession MailBox → 原 GridSnapshotHandler／GridResultHandler 或对应新增投影 handler → 复制普通值 → 回收 DTO → 发布本端观察事件。Host 提交事件只供本端观察；Event 不送消息或替代答复，Invoke 只做精确入口。沿用 ProductHost／ProductClient SceneType 并核对 All 消费者。

### 4. 取消与物理调用只由一个观察者收尾

BeginEntry 后的原 Call 在途责任交给执行 `sender.Call` 的内部观察者；它的 finally 在响应、异常、原超时或整体 Stop 后 EndEntry。外层业务等待取消时不提前 EndEntry。可在现有本地值适配增加明确的完成回调参数，或把同段 finally 内聚到产品所属适配；实施选择以不复制原等待机制为准。原请求未实际交给 sender 的同步失败由发起边界收尾，交接后不抢 handler 的输入责任。

内部观察者取得响应后，先检查原 owner／EntityRef，再读为独立值；finally Dispose 自己取得的响应并释放捕获 Peer 的在途责任。取消对外业务等待不撤销已提交动作；公共 sender 仍服务其他 owner。缺少答复沿原 40 秒物理预算结束，不把这段等待误称即时取消。整个运行退出沿已验证的 sender.Stop，无需等 40 秒。

### 5. 真实 fixture，消息与业务时间分别控制

复用 `Unity/Assets/Tests/Editor/ET/ProductSessionTests.cs` 的 World、真实导出 Tables、CreateFiber、AddLocalClient 与清理。增加 Solo 创建方式，不能沿 StartHost 建 KService 后宣称单机无监听。

现有 Pump 每次加 15ms；抽出不推进测试 clock 的 PumpMessages，保持原 Fiber EntitySystem／线程队列更新，使真实 MailBox 续体运行而厨房没有新的时间步。AdvanceTicks 每次加 50ms 并使用同一更新路径，不再额外调用 AdvanceGridTick 导致双推进。所有操作用真实 request/Call，先拿回执，再读提交和投影；可搭建局部初始场景，但不直接安装期望最终状态。fixture 拒绝替换活跃 ET World，测试退出 Stop 自己 sender／Fiber／World。

具体最小测试和复用证据见 [execution-notes.md](execution-notes.md)。测试名称为拟新增，执行前必须实际发现。

## Risks / Trade-offs

- [取消让 Peer 提前退出] → 对外等待与原 Call 寿命分开，测试先取消再投递／答复，并断言其他 owner 正常。
- [新编号与旧 Grid 元数据重复裁决] → 新共享记账只在可信入口；改动到的 Grid 重放用例重验。
- [测试悄悄推进时间或伪造成功] → 固定测试 clock、真实消息泵、读取独立投影，保留普通 Solo 点击验收。

## Migration Plan

后续 apply 先以当前两段路径复现取消风险，再修原适配的交接／finally；接入有限上下文与重放元数据，追加必要协议源类型并正常生成，按实际落地版本递增。所有新消息追加现有协议源末尾，不重排 opcode；单机新增请求不自动进入 wire 白名单。只迁移实际消费者并执行最小本地 UTF 与普通 Solo；没有运行授权时不做这些步骤。回撤仅处理本 change 的具体源改动，不 reset 用户现场。

## Sources

- [产品入口源码](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/)、[所属请求适配](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Module/Message/OwnedRequestExtensions.cs)。
- [实际 fixture](../../../Unity/Assets/Tests/Editor/ET/ProductSessionTests.cs)、[网络约定](../../../references/reactive-and-network.md)。
- [发送器既有验收](../archive/2026-10-11-local-message-request-shutdown/execution-notes.md)、[所属异步既有验收](../archive/2026-10-11-manage-owned-async-lifecycle/execution-notes.md)。
