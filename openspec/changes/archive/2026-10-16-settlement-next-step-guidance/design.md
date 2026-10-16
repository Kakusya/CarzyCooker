# Design

## Context

问题与范围见 [proposal.md](proposal.md)。本案需要先明确显示与真实操作、异步读取和当前 owner 的交接，因此保留设计。实际入口为 [GridKitchen/GridPresentationPanels.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)，相邻数据在原 Product Model／ModelView，现有 UI 由 `UIGridKitchen` 托管。

## Goals / Non-Goals

**Goals:** 按当前模式和真实阶段组织本关结果、待处理装修／保存、下一关菜单变化或大关终点，并给出唯一清楚的主要下一步和原合法次要入口。

**Non-Goals:** 不新增自动保存、奖励或第四关；零星仍可继续。内存、保存中、失败、已提交和已领取分别说明，Host／Client 控制与个人领取资格保持。 不新增业务权威、协议／文件格式或 UI 结构工程；完整范围外事项见 proposal。

## Decisions

### 实现方案

集中派生展示状态，继续用原 RefreshSaveStatus／RenovationReadyForContinue 和 LevelControlAction；不会为统一按钮名称重写 Continue／SaveAndContinue／FinishChapter 的业务含义。下关菜单从有效定义查，不臆造下关奖励。

保持 `Game.ET.Code.ModelView` 数据与 `Game.ET.Code.HotfixView` 逻辑分层。视图类型仍为1101，沿 `UIComponent.AddUIFormComponentAsync<UIGridKitchen>`；资源仍为 `Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab`。使用原控制／列表／详情／模板绑定，生成的 Bind partial 只读。新增有限页面或动作分类使用具名 enum，数量、ID 和版本保持数值语义。

### 实际 owner 树与寿命

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

树中的 Child／Component 表示释放归属，字段引用不改变 parent。Host 写 Restaurant／Match，repository 写文件，客户端仅持独立投影；本案只增加属于原 Presentation 的选择、摘要及显示依赖。不会为“统一入口”再建可写业务副本。

创建继续走原启动和 UI 加载链，身份及投影未就绪不开放业务输入；Close／Dispose／页面换轮先失效旧选择与回调，晚到加载按原 AsyncOwner／EntityRef／epoch 收尾。新关／新世界不继承旧操作上下文。单成员退出不停止 Root 共享 sender，整轮停止继续由原运行拆树。

### 发送、接收与结果

显示数据沿原 GridBroadcast.SendCommitted -> ProductDelivery 到每个 HostPeer：本地从 LocalDelivery 取得 ClientSession.GetActorId()，Root ProcessInnerSender.Send 经该 ClientSession 的 Ordered MailBox 投递；远端从 RemoteDelivery.Session 序列化，经远端 ProductClientBinding／分发进入同一 GridSnapshotHandler（SceneType.ProductClient）。ApplyGridSnapshot 核对身份、完整复制并安装后，原 Presentation.Update 才刷新。本地 DTO 由接收 handler finally 释放，远端输入在同步序列化后释放；UI 不保留消息对象、Host EntityRef 或消息集合。

下一步仍是 LevelCommandRequest -> LevelPeerHandler -> authority ActorId／Ordered MailBox -> LevelSubmitHandler（ProductHost）-> 原 Restaurant／成功保存流程；文件发布失败不会因为新 UI 绕过，原提交终态和当前关身份决定前进。

本地玩家请求复用原 LocalMessage／Root sender 到 HostPeer.GetActorId()，远端 Session 进入同一 ProductHost handler；handler 再取 authority.GetActorId()，两端最终进入同一原裁决。 请求适配器仍负责响应值复制与消息回收；查看本身没有成功业务答复可等待。Event／Invoke 只沿原 SceneType 范围刷新观察，不新增 All 订阅，不把观察事件当请求成功。

### UI 协作与更新成本

只保留必要的页面、选择 ID、筛选及版本上下文；状态与权限每次从当前有效输入重验。与原菜谱／伙伴／队友详情共享区域时明确互斥，切回原目标仍可操作。补齐本案依赖字段比较，纯时间／位置更新不反复重建全部按钮，不额外逐帧复制／排序全状态。



实施协调见 proposal 中的相关案。对共享文件顺序整合，保留各案实际 delta；其他案尚未实施时，使用现有入口完成本案独立验收，不添加虚假占位按钮。

## Risks / Trade-offs

- [用统一展示抹平保存提交、结果与领取的差异] → 运行本案正常与变化场景，操作前复验当前身份；读页面不提前提交效果。
- [旧结果与界面池重用] → 复用原 EntityRef、world／Level／binding、epoch／serial guard，取消后只收尾原等待。
- [只用文本断言冒充可玩] → Solo持久／试玩、合作Host／Client、零星／终关、失败重试和旧回调；使用真实入口与有行为断言的相关 PlayMode UTF，逐功能结果见 execution-notes。
- [入口更易用尚无真人证据] → 功能结果只证明事实与可操作性，真人体验及设备差异保留明确未验证范围。

## Migration Plan

无需数据迁移。仅整合本案实际视图逻辑与相关测试，更新已有操作说明和当前进度；撤回入口即可恢复原导航，既有文件／世界规则不变。不在本案做提交、推送、归档或设备构建。
