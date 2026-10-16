# Design

## Context

参见 [proposal.md](proposal.md)。原入口已具备实际输入与终态反馈，本案增加可回看的说明与上下文导航，需要先界定阅读和原业务动作的交接，保留本设计。

## Goals / Non-Goals

**Goals:** 围绕手满、加工锁定、容器不符、不可达、生产许可、收货台满、旧目标及保存失败，提供可理解原因和对应查看入口；未知原因保留可复制诊断信息。

**Non-Goals:** 建议根据当前真实投影提供，不自动重试、丢弃物品、解绑、采购或改布局；查看帮助与实际业务动作清楚分开。旧命令结果不覆盖当前命令反馈，接收成功不冒称执行成功。

## Decisions

复用 LastStatus／LastReason 与原 ProductCommandOutcome 水位，不建无限操作历史；在 HotfixView 集中已有 reason 到说明／相关入口的映射。查看当前目标、菜谱、供应或保存状态只改本轮选择；显式新动作仍由原入口生成新命令。

逻辑归 `Game.ET.Code.HotfixView`，显示选择归 `Game.ET.Code.ModelView` 的原 Presentation，使用具名 enum 表达有限分类；原 Model 的 LastStatus／消息水位与业务状态不迁移。保留 UIGridKitchen（1101）与 Product/UIGridKitchen.prefab 的控制、目标和可滚动详情绑定，不修改 CodeBind 生成文件。

### owner 与创建退出

```text
Host process / Fiber / Scene(ProductRoot)
  +-- Component: ProcessInnerSender / ProductRuntimeComponent
  +-- Child: Scene(ProductHost)
  |     +-- Component: HostSessionComponent + Ordered MailBox
  |     |     +-- Child: HostPeer + Ordered MailBox
  |     +-- Child: Match
  |           +-- Child: RestaurantRuntime
  |                 +-- Component: GridKitchenComponent (items/processes; actor index)
  |                 +-- Child: Level (actors/front works/orders/phase)
  +-- Child: Scene(ProductClient)
        +-- Component: ClientSessionComponent + Ordered MailBox
        |     +-- Component: GridClientComponent (independent projection)
        +-- Component: GridPresentationComponent (view selection only)
        +-- Component: UIComponent -> Component: UIGridKitchen
        +-- Component: GFEntityComponent -> Component: GridKitchenBoard

Remote process / Fiber / Scene(ProductRoot)
  +-- same client owner tree
  +-- Child: Scene(ProductClient)
        +-- Component: NetComponent -> Session + ProductClientBinding
```

Host 的厨房／前厅保持唯一写入；本端独立投影提供页面事实；说明选择和可见分类只属于本轮 Presentation。沿原身份确认、投影安装、Board／HUD 加载后开放入口。页面关闭、换关、返回房间或整个运行退出先清本案选择，原 AsyncOwner 与 HUD epoch／EntityRef 负责在途加载、迟到结果和池化重用。单成员退出不停止共用 sender。

### 完整数据与输入路径

显示数据沿原 GridBroadcast.SendCommitted -> ProductDelivery 到每个 HostPeer：本地从 LocalDelivery 取得 ClientSession.GetActorId()，Root ProcessInnerSender.Send 经该 ClientSession 的 Ordered MailBox 投递；远端从 RemoteDelivery.Session 序列化，经远端 ProductClientBinding／分发进入同一 GridSnapshotHandler（SceneType.ProductClient）。ApplyGridSnapshot 核对身份、完整复制并安装后，原 Presentation.Update 才刷新。本地 DTO 由接收 handler finally 释放，远端输入在同步序列化后释放；UI 不保留消息对象、Host EntityRef 或消息集合。

阅读说明或打开相关详情不发业务消息、没有业务等待者。真实动作仍经 ClientSession 的原 Call：本地经 Root.ProcessInnerSender 到 LocalMessage 绑定的 HostPeer ActorId，远端经 Session 到对应 Peer；GridPeerHandler 再投 HostSession.GetActorId() 的 Ordered MailBox 到 GridSubmitHandler（ProductHost），统一原裁决。准入答复不等于提交，GridCommandResult／ProductCommandOutcome 先按原水位安装，再更新本人反馈。输入、响应与 DTO 仍按原发送器／handler／using 各自收尾，UI 只留独立值。

不增加 SceneType.All 订阅或新的事件权威；原 ProductClient 观察只刷新本轮显示，旧命令结果和旧关不会覆盖当前提示。

### UI 整合

实现位于GridPresentationActionAdvice；ActionAdviceKind集中八类及暂停映射，未知仅显示诊断。最近一次本端请求只记录命令/工位/物品/布局值，不保存消息对象或历史。说明打开后绑定当前命令/状态/原因、保存结果、所看工位及手持物、内容与锁定，变化则撤下旧建议；目标变化类不提供旧请求目标导航。原动作显示依赖加入当前反馈，即使拒绝没有改变实物也能出现入口。关闭/换关清理元数据，帮助与说明显式互斥；正文只随本轮说明上下文变化生成。

详情与菜谱／伙伴／队友说明互斥，选中目标和原动作保持；说明文本按真实模式／阶段变化更新，相关入口以目标当前仍有效为前提。只在相关依赖变化时生成说明，不逐帧拼接或维护无限错误／教学历史。普通 Launcher 实际验证说明与点击／拖动吻合；已有合法动作仍沿原按钮产生一次新请求。

## Risks / Trade-offs

- [建议按钮暗中重试或丢物，及旧失败覆盖当前反馈] → 相关 UTF 断言阅读无命令／状态副作用，真实入口核对实际输入及失效目标。
- [复用详情造成旧回调串页] → 原 HUD 身份、epoch、世界／关／binding guard；关闭重开及换关补运行。
- [脚本无法证明真人理解] → 本案只验解释与实际功能相符，真人理解留给试玩反馈。

## Migration Plan

仅改原视图逻辑与相关测试，无文件或协议迁移。按 execution-notes 逐项运行后同步实际操作说明；撤回时移除入口及本案显示选择，不修改经营状态。不在本案做设备构建或发布。
