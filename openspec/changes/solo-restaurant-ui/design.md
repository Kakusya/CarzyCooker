# Design

## Context

见 [proposal.md](proposal.md)、[UI 合同](specs/restaurant-ui/spec.md)。现有 UIGridKitchen 注册 1101，Prefab 为 Res/UI/UIForm/Product/UIGridKitchen.prefab，Mono 与生成 Bind partial 在 ModelView/Client/Product/GridKitchen；动态目标按钮由现有 TargetTemplateButton 创建，关闭清理。地图仍由同 ProductClient 的 GFEntityComponent 保管。

## Goals / Non-Goals

**Goals:** 扩展当前受管 HUD 与 Board，用已提交投影解释全部单机阶段并提供合法操作。

**Non-Goals:** 不让界面保存第二份订单、加工或得分；不建 UI 框架、资源私池、结构 runner 或联机大厅。

## Decisions

### 1. 实际 owner 树

```text
Main Fiber / SceneType.ProductRoot
  Component ProcessInnerSender / ProductRuntime / Timer
  Child SceneType.ProductHost
    Child Match -> Child RestaurantRuntime (Kitchen + current Level)
    Component HostSession + Ordered MailBox -> Child HostPeer + Ordered MailBox
  Child SceneType.ProductClient
    Component ClientSession + Ordered MailBox
      Component GridClient (copied Kitchen / Level / front / buff values)
    Component LocalMessage
    Component GridPresentation (input / HUD load waiting / camera presentation)
    Component UIComponent
      Component UIGridKitchen + AsyncOwner (command waiting) -> managed MonoUIGridKitchen
    Component GFEntityComponent
      managed GridKitchenBoard
```

运行与加入完成、合法投影就绪后由原GridPresentation打开原Board／UI。HUD加载等待归Presentation.AsyncOwner，HudLoadEpoch核对迟到加载轮次；玩家操作等待归当次UIGridKitchen.AsyncOwner。OnClose停止该scope、解订阅并清理所属按钮，原物理请求由所属helper继续收尾；UIComponent.RemoveComponent关闭受管UI，不直接SetActive根。加载中整体退出由原UGFUIForm所属加载取消及epoch收尾。整个运行停入口、关闭Presentation／UI／Board、原sender.Stop后释放树；关闭单个HUD不暂停营业或停止共享发送器。

### 2. Prefab 与生成源边界

保留 Prefab 路径、UI id 与单实例 owner。结构增量明确为 PhaseText、ClockText、ScoreText、HandText、WorkText、OrderPanel、BuffPanel、SettlementPanel、ControlPanel 和 CameraDragArea；订单与候选条目复用归该 Mono 的普通模板按钮，持目标身份的短寿命绑定。不要为了每条订单引入 UIEntity 表或新的业务实体。

实际保留原WorldSpace Canvas／GraphicRaycaster，新增绑定后正常生成19个属性。顶栏显示阶段、时钟、得分、工作和反馈，其下为订单或准备buff区，中间CameraDragArea对应地图，下方Control和原手持／滚动目标区，Completed显示结算面板。模板按钮使用同一owned列表；ClearTargets先解绑、立即禁用该动态按钮再Destroy，避免同帧旧caption／订阅残留。只显隐所属子面板，没有新增UI表行或资源私池。

先记录现有 Canvas、RectTransform、CodeBind 与消费者，再通过精确匹配本工程的安全 Editor 操作修改 Prefab／Mono 绑定源，重生成 Bind partial，迁移 HotfixView 消费者。不手改生成 partial。UI.xlsx 仅在实际配置行为需要时修改，不能因增字段虚构新 UI 行；文字需要本地化时改源并正常导出。结构改动与领域逻辑保持在各自 change，不附带结构工具。

### 3. 投影和显示状态

Phase 与合法控制从 Level 投影推导；订单按钮固定当前 World／Level／绑定、OrderId、CustomerId、TableId，临时选择和提示属于显示状态。旧关按钮、异步回调 await 后检查原 owner 和这些身份，无法匹配就忽略，不重新定位同桌新顾客。

每次构建按钮还捕获UIGridKitchen／GridClient EntityRef和世界／关代次；即使保存的旧UnityEvent delegate被直接调用，也先检查原owner Available及这些身份。

接收回执只显示已收到／待执行，Tick 最终成功再显示拿取或出餐成功，Process 进度和完成另显示。Rejected 提示可理解原因且不预先清手槽。一次安装相同提交版本的厨房、前厅与关卡投影后才刷新，避免新订单配旧餐具。开候选面板不重抽 buff，不发送隐式业务暂停；GF 覆盖 Pause 只是 UI 生命周期，不等于餐厅暂停。

### 4. 输入与消息完整路径

场景点击和 HUD 按钮先构造同一固定身份意图，由 GridPresentation → ClientSession 所属 typed request → LocalMessage 捕获 Peer ActorId → root sender.Call → Peer Ordered MailBox／ProductHost 对应 Grid／Level／Layout／Serve／Buff handler → 可信内部 Submit → HostSession ActorId／MailBox／对应 Submit handler → 服务步／业务 Tick 裁决。消息答复经原两段等待表返回，UI 读取普通值，不持池化响应。

提交后的每 Peer DTO → LocalDelivery sender.Send(ClientSession ActorId) → ProductClient 对应投影 handler → 检查代次、复制普通值、回收 DTO → GridProjectionApplied 等观察 → 当前 UI owner 刷新。Event 不发送请求或代替回执；关闭取消显示等待，内部物理观察者仍负责响应与在途计数收尾，按 [消息设计](../solo-local-message-contracts/design.md)。

镜头维持厨房一屏的既有棋盘视角，纵向平移显示下方前厅。指针状态区分按下／拖动／释放，达到配置拖动阈值后取消这次点击；HUD 捕获指针时棋盘不发穿透动作。镜头只属于表现，不能改角色、路径或地形。坐标变换统一取当前相机与 Board，避免拖动后沿旧相机射线点击。

实际MapViewport由CameraDragArea世界四角和rootCanvasCamera换算成Screen矩形；Board相机保持完整viewport供原URP overlay，只调整orthographicSize／中心使厨房适配无遮挡区域，拖动边界也按这个区域计算。CameraDragArea透明Image不截获地图输入，其他HUD由原GraphicRaycaster消费；窗口尺寸改变重算区域，池重用清理旧适配值。

### 5. 测试接入与分阶段依赖

领域 change 先在原 HUD 增必要基础控制，本 change 随领域投影完成后扩展整张界面；完整 UI 不是做菜／订单的反向前置。原CarzyCooker.ET.Tests未引用View；现已补必要引用，但仍不能把领域测试冒充按钮验收。UI UTF 在现有测试目录与程序集内补必要实际 ModelView／HotfixView 引用和当前 define 条件，加载真实 Prefab，模拟按钮与指针并断言发送计数、目标身份及投影后的显示；不新增测试框架或默认全仓套件。Unity 环境初始化走现有入口，行为不能测到时保持该点 NotRun。

真实 Launcher Solo 另查厨房一屏、前厅拖动、目标点击、候选、暂停恢复、出餐反馈及零星继续。截图只证明表现可见，不能证明按钮或生命期正确。覆盖和发现／执行规程见 [执行说明](execution-notes.md)。

## Risks / Trade-offs

- [结构绑定失效] → Prefab／绑定源／生成／消费者一并迁移，加载真实资源检验字段和按钮。
- [重开累加订阅或输入穿透] → UTF 断言一次点击一次请求、拖动零点击，真实入口复查。
- [UI 取消掩盖物理在途等待] → 复用所属请求收尾，不清字典冒充完成，不影响其他 owner。

## Migration Plan

已按九案apply授权完成安全Prefab编辑、正常CodeBind生成、五项UI UTF和普通Launcher两关验收，详见execution-notes。原测试程序集已补实际View／UGUI／UGF引用，独立SoloRestaurantUiTests加载真实Launcher、拒绝占用活动World，仅清理自己捕获的Init／GameEntry Root。操作未保存Launcher场景，也未手改生成partial；后续结构或行为变化再按相关小集补验。

## Sources

- [实际 Prefab](../../../Unity/Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab)、[Mono 源](../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoUIGridKitchen.cs)。
- [UI 手册](../../../Book/UI开发.md)、[动态 UI SOP](../../../references/dynamic-ui-sop.md)、[现有测试程序集](../../../Unity/Assets/Tests/Editor/ET/CarzyCooker.ET.Tests.asmdef)。
