# Design

## Context

问题与范围见 [proposal.md](proposal.md)。本案需要先明确显示与真实操作、异步读取和当前 owner 的交接，因此保留设计。实际入口为 [GridKitchen/GridPresentationRoomSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRoomSystem.cs)，相邻数据在原 Product Model／ModelView，现有 UI 由 `UIGridKitchen` 托管。

## Goals / Non-Goals

**Goals:** 提供按角色区分的离开入口及后果说明：Solo 返回模式、Client 离开本次运行、Host 仅关闭餐厅回房间与结束整个房间分别确认；确认前可取消。

**Non-Goals:** 确认页不自动暂停或保存，需说明经营仍按原状态推进；不将技术离开伪造成功结算。Host 关闭餐厅保留 Match 和现有资格，正常结束整个 Match 沿原回执规则；提交中禁止重复且不把取消等待冒充撤销。 不新增业务权威、协议／文件格式或 UI 结构工程；完整范围外事项见 proposal。

## Decisions

### 实现方案

复用 ReturnToModeMenu、原 CloseRestaurant 房间命令及 LeaveRoomNormally 的 ProgressCloseMatch 流程。选择和确认归当前 Presentation／HUD，捕获世界／关／房间／binding；Client 的正常 Leave 沿已有控制入口收尾后切模式，不直接销毁共享 sender。原存档与资格结果只查询、不临时补写。

保持 `Game.ET.Code.ModelView` 数据与 `Game.ET.Code.HotfixView` 逻辑分层。视图类型仍为1101，沿 `UIComponent.AddUIFormComponentAsync<UIGridKitchen>`；资源仍为 `Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab`。使用原控制／列表／详情／模板绑定，生成的 Bind partial 只读。新增有限页面或动作分类使用具名 enum，数量、ID 和版本保持数值语义。

### 实际 owner 树与寿命

```text
Host process / Fiber / Scene(ProductRoot)
  +-- Component: ProcessInnerSender / ProductRuntimeComponent
  +-- Component: ProductEntryPresentation (leave intent and original async owner)
  +-- Component: UIComponent -> UIGridKitchen (only while root overlay owns display)
  +-- Component: SoloSaveRepository + Ordered MailBox (when already initialized)
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

树中的 Child／Component 表示释放归属，字段引用不改变 parent。Host 写 Restaurant／Match，repository 写文件，客户端仅持独立投影；本案只增加属于原 Presentation 的选择、摘要及显示依赖。不会为“统一入口”再建可写业务副本。

创建继续走原启动和 UI 加载链，身份及投影未就绪不开放业务输入；Close／Dispose／页面换轮先失效旧选择与回调，晚到加载按原 AsyncOwner／EntityRef／epoch 收尾。新关／新世界不继承旧操作上下文。单成员退出不停止 Root 共享 sender，整轮停止继续由原运行拆树。

离开选择统一归原 Root ProductEntryPresentation。Grid 按角色添加原控制栏入口，仅以同 Fiber 的普通方法交接具名 ProductLeaveAction 和当前 world/关/binding；沿原 ProgressClaimsOverlayChanged（ProductClient）通知关闭地图 HUD，Root 接管同一 UI1101。此事件只交接显示，不是离开或保存请求；确认阶段不查询领取目录，也不初始化 repository。原 ClaimsVisible 仅复用现有互斥显示所有权，实际渲染优先走离开确认，不展示领取按钮或自动 Browse。

意图持有原 Client EntityRef、Match、binding、room version、world/generation 和 Level/generation；换轮或取消使 serial/旧按钮失效。等待确认时上下文改变显示失效并要求返回重选，时间推进不反复重建按钮。取消经原 CloseClaims 归还受管 HUD；提交期间撤下确认/取消，不能把已发出的关闭或 Leave 撤销。Solo 仍可从经营中返回，Host 的仅回房间和结束整个房间为两个入口；房间中的返回模式也先确认，Joining 的取消保留原连接取消路径。

### 发送、接收与结果

显示数据沿原 GridBroadcast.SendCommitted -> ProductDelivery 到每个 HostPeer：本地从 LocalDelivery 取得 ClientSession.GetActorId()，Root ProcessInnerSender.Send 经该 ClientSession 的 Ordered MailBox 投递；远端从 RemoteDelivery.Session 序列化，经远端 ProductClientBinding／分发进入同一 GridSnapshotHandler（SceneType.ProductClient）。ApplyGridSnapshot 核对身份、完整复制并安装后，原 Presentation.Update 才刷新。本地 DTO 由接收 handler finally 释放，远端输入在同步序列化后释放；UI 不保留消息对象、Host EntityRef 或消息集合。

Client 确认后用原 LeaveProduct 的 ProductLeaveRequest，经本地 Peer 或 Session 进入原 ProductHost 控制链完成成员解除；其 committed／失败结果与本端 StopClient 分别说明。Host 仅回房间用 ProductRoomPeerHandler -> authority ActorId -> ProductRoomSubmitHandler 的 CloseRestaurant；结束整个 Match 用 ProgressCloseMatchPeerHandler -> 同一 authority Ordered MailBox -> ProgressCloseMatchSubmitHandler，再走原 repository 关闭回执。Solo 只沿 ReturnToModeMenu 结束本轮，无成功结果写入。

Host 关闭餐厅请求由 Root 确认 owner 的原 AsyncOwner 承担等待，以 CallProductValueAsync 复制 Error/Message；原 Grid 私有关闭方法的 UI 等待移到此处，业务请求与同一裁决不变。正常结束 Match 复用 LeaveRoomNormally 的既有 Accepted→关闭回执/ACK→Executed 轮询，只有原完成答复才切模式。Client 的原 LeaveProduct 内部 using 回收响应、finally StopClient；Root 等待 bool committed 或失败后，把真实确认/未确认反馈带到新模式页，原发送器/消息等待表不增加。整体运行切换仍由 Loader 在原 Fiber 帧后退役捕获 World。

本地玩家请求复用原 LocalMessage／Root sender 到 HostPeer.GetActorId()，远端 Session 进入同一 ProductHost handler；handler 再取 authority.GetActorId()，两端最终进入同一原裁决。 请求适配器仍负责响应值复制与消息回收；查看本身没有成功业务答复可等待。Event／Invoke 只沿原 SceneType 范围刷新观察，不新增 All 订阅，不把观察事件当请求成功。

### UI 协作与更新成本

只保留必要的页面、选择 ID、筛选及版本上下文；状态与权限每次从当前有效输入重验。与原菜谱／伙伴／队友详情共享区域时明确互斥，切回原目标仍可操作。补齐本案依赖字段比较，纯时间／位置更新不反复重建全部按钮，不额外逐帧复制／排序全状态。

Client 的 LeaveProduct 即使未得到 committed 也会 StopClient，故不能把本端页面退出显示为 Host 已确认离开。显示真实结果与后续返回选择，不新建自动重连或静默重试；确认页本身不会发送 Pause，已暂停世界继续保持原暂停事实。

实施协调见 proposal 中的相关案。对共享文件顺序整合，保留各案实际 delta；其他案尚未实施时，使用现有入口完成本案独立验收，不添加虚假占位按钮。

## Risks / Trade-offs

- [混淆关闭餐厅与正常关闭整个 Match，导致不符合原合同的资格或存档处理] → 运行本案正常与变化场景，操作前复验当前身份；读页面不提前提交效果。
- [旧结果与界面池重用] → 复用原 EntityRef、world／Level／binding、epoch／serial guard，取消后只收尾原等待。
- [只用文本断言冒充可玩] → Solo／Client／Host各关闭范围、取消／失败／重复及正常关闭回执；使用真实入口与有行为断言的相关 PlayMode UTF，逐功能结果见 execution-notes。
- [入口更易用尚无真人证据] → 功能结果只证明事实与可操作性，真人体验及设备差异保留明确未验证范围。

## Migration Plan

无需数据迁移。仅整合本案实际视图逻辑与相关测试，更新已有操作说明和当前进度；撤回入口即可恢复原导航，既有文件／世界规则不变。不在本案做提交、推送、归档或设备构建。
