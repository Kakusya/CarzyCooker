# Design

## Context

动机见 [proposal.md](proposal.md)。已核对的直接入口为 [GridKitchen/GridPresentationHudLifecycle.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationHudLifecycle.cs)，主规格为 [restaurant-ui](../../../specs/restaurant-ui/spec.md)。本案跨页面选择、投影刷新与受管 UI 寿命，需要明确显示上下文，因此创建设计。

## Goals / Non-Goals

**Goals:** 入口清楚、经营地图完整、详情可读、主要动作可触达；在同一受管 HUD 中解决空间竞争，同时保留原长列表和生命周期合同。

**Non-Goals:** 仅处理现有 UIGridKitchen 结构/绑定和对应消费者，不改业务权限、表配置语义或创建第二套菜单框架；不承诺已完成 Android 真机适配，也不先造 UI 结构 runner。 不扩展到新传输、持久格式、公共工具或全套 UI 架构。

## Decisions

### 入口与经营采用明确布局模式

在现有 Mono/UI owner 内使用具名布局模式区分入口、正常经营和详情，不再用 `ShowPanels(false, true)` 将入口当结算内容区堆叠。入口自上而下为标题/当前页面、可滚动正文或列表、固定返回和主要控制；经营自上而下为阶段/时间、地图、目标/手持摘要和动作。结算保持原结果/继续语义。模式只描述布局，不持有新的业务阶段。

正常经营态收起长文、工作清单和全动作列表，厨房须完整容纳在剩余地图视口内；前厅继续纵向拖动。详情使用受控覆盖层，背景地图不接收其指针，关闭恢复原选中目标和镜头。订单摘要可见，展开订单与长详情采用明确返回栈，不叠加多个遮挡层。

在 540×960、1920×1080、540×960 非零安全区三种代表环境检查；最大已有字号也须使按钮文本完整或可换行、固定返回可触达。采用内容自适应/滚动而非缩小字体硬塞内容。截图检查空间，真实 EventSystem 射线与点击/拖动验证操作；仅对 Button 调用回调不足以证明没有遮挡。

迁移清单覆盖 `ProductEntryPresentationSystem`、`SoloSavePresentationSystem`、`GridPresentationPanels`、菜谱/设置/拒绝帮助/结算及已有各详情入口。枚举所有 CodeBind 消费者后再改结构，保留 UI1101 注册与原 Canvas。装饰不接收射线；不使用隐藏受管根代替 Close。

### 六案交接

本案提供标题、摘要、主动作、次要动作、详情正文、长列表和固定导航的呈现区域/API；[首局路径](../2026-10-20-product-first-session-path/design.md) 决定入口内容与首单提示，[情境动作](../2026-10-20-kitchen-contextual-actions/design.md) 决定推荐排序。两案均不再重构 Prefab。本案先落地，后两案接入最终 API。美术案只更换图形/字号层次；声画案沿已有详情和控制入口呈现设置。其余进行中 inspector 不在本案制造假数据或假入口。

### 数据与交互方案

先按已加载 UI skill/SOP 核对原1101、Prefab、Mono与CodeBind消费者，使用Unity CLI安全修改既有 Prefab；必要绑定改源并重新生成。单一现有 UIComponent owner，不另开同类型并行单例；保留原输入消费与拖动判定。

UI结构必须先按 references/dynamic-ui-sop.md 实际核对资源 Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab、MonoUIGridKitchen 与 CodeBind 源/生成绑定、原详情和动作消费者。使用项目 Unity CLI 安全编辑原 Prefab；新增必要源绑定才重新生成，不手改 Bind partial。保留1101 ID与单实例资源，不修改表。正文滚动与动作滚动各自消费手势，地图只使用实际保留下来的工作区；固定返回区不能随长文滚出屏幕。没有专用结构 runner，本案不把造工具作为前置。

选择复用已有投影及原动作，原因是既有业务已经裁决身份、权限和消费。未选“UI 自行调用权威对象”或“为每个看板新建缓存世界”的方案，避免读页面获得写权限及重复状态。

### 实际 owner 树与创建/退出

```text
Host / Fiber / Scene(ProductRoot)
  +-- Component: ProcessInnerSender / ProductRuntimeComponent
  |     +-- Component: ProductResumeIntent (runtime owner, when applicable)
  +-- Child: Scene(ProductHost)
  |     +-- Component: HostSessionComponent + Ordered MailBox
  |     |     +-- Child: HostPeer + Ordered MailBox
  |     +-- Child: Match
  |           +-- Child: RestaurantRuntime
  |                 +-- Component: GridKitchenComponent
  |                 |     +-- Children: GridStation / GridItem / processing entities
  |                 +-- Child: Level
  |                       +-- Children: GridActor / Customer / Order / FrontWork / CompanionRole
  +-- Child: Scene(ProductClient)
  |     +-- Component: ClientSessionComponent + Ordered MailBox
  |     |     +-- Components: GridClientComponent / ProductRoomClient
  |     +-- Component: GridPresentationComponent (view selection only)
  |     +-- Component: UIComponent -> Component: UIGridKitchen
  |     +-- Component: GFEntityComponent -> Component: GridKitchenBoard
  +-- Components: ProductEntryPresentation / UIComponent (entry HUD phase)

Remote / Fiber / Scene(ProductRoot)
  +-- Component: ProductRuntimeComponent -> Component: ProductResumeIntent
  +-- Child: Scene(ProductClient)
        +-- Component: NetComponent -> Child: Session + ProductClientBinding
        +-- same client projection and presentation owners
```

原创建顺序为 runtime/Host房间 -> 餐厅与 Level -> 本人确认及独立投影 -> 当前 owner 打开1101。入口页与厨房页在各自原阶段交接资源使用权，不同时强开同一单例 Prefab。本案新增的筛选、选中ID、草稿或滚动值只归原 Presentation；Mono view 只绑定与表现。

换页/关闭先失效本轮回调上下文，HUD close/dispose 由原 UIComponent/GF owner 完成。await 后复查 EntityRef、epoch、world/binding/level generation 及适用的业务版本；旧结果只收尾原等待。单成员退出不 Stop 共用 ProcessInnerSender，整体退出仍由原 runtime 按原顺序拆树；持续厨房批次不能因关内页面退出而消失。

### 显示数据的发送与接收

Host 提交领域状态后，原 GridBroadcast/ProductDelivery 向各 peer 发布。Host本地通过 LocalDelivery 保存的 ClientSession.GetActorId()，由 Root ProcessInnerSender.Send 入 ClientSession Ordered MailBox；远端通过 RemoteDelivery 的 Session 序列化，ProductClientBinding 分发到同一 ProductClient GridSnapshotHandler。ApplyGridSnapshot 校验身份并安装独立值后，本案读当前投影刷新；房间摘要沿原房间快照安装更新。

只读查看没有新请求或答复。本地DTO由原接收 handler 的 finally 释放，远端原对象按既有序列化边界收尾；UI 不保存池化消息、Host实体或消息集合。玩家明确执行原动作后，响应仅复制值交还原适配器，最终状态来自原提交快照与匹配的 outcome，不能用 Accepted 提前显示成功。

Event 只在原 ProductRoot/ProductClient 实际消费者范围通知显示；不新增 SceneType.All 订阅，Invoke 不承载本案业务裁决，也不以 Event 代替网络发送、请求响应或命令成功。

### 页面协作与刷新

与 kitchen-action-navigation、product-operation-help及本批所有详情入口 明确分工，复用最终原入口。显示面板与菜谱/详情互斥或明确返回，不为依赖尚未实施的功能制作假按钮。纯进度/位置变化只更新相关文本和高亮，候选集合或可用动作改变才重建按钮；不每帧复制、排序全世界。有限状态分类用具名 enum，ID/数量/版本保留数值语义。

## Risks / Trade-offs

- [事实或数量解释错误] -> UI结构变更影响共享引用与输入，因此独立成案并核对Prefab/绑定/消费者；以代表尺寸和真实交互覆盖，不扩建测试工具或全设备矩阵。
- [旧目标或 UI 池复用] -> 针对 非零安全区、HUD关闭重开或打开中退出，旧按钮随后触发 验证 布局不遮盖必要控制，旧回调无效，无重叠订阅或残留视图；重开后的真实操作仍可用。
- [文字断言代替真实业务] -> 540×960长正文/长列表、一个非零安全区与宽屏代表尺寸、地图拖动不穿透、关闭重开和加载中退出，运行映射放在 execution-notes。
- [体验尚无真人证据] -> 自动化只能证明行为及可操作性；真人上手、Player和真机保持未验证。

## Migration Plan

先在原 owner 接入本案最小显示状态，再整合对应原入口与必要绑定，随后执行最小相关 UTF 和普通 Launcher 检查。无需数据迁移；撤回本案入口/显示改动即可恢复原交互，原已提交世界和存档不回写。结构调整需同步 Prefab/绑定源/生成结果及所有消费者，验收重开和输入。 不做提交、推送、归档或主规格同步。
