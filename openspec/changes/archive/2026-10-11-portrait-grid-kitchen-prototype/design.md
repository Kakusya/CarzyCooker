# Design

## Context

动机与行为范围见 [proposal.md](proposal.md)，已裁决输入见 [execution-notes.md](execution-notes.md)。当前 `ProductRuntimeBootstrap` 已在一个 Main 调度 Fiber 内创建 ProductRoot、ProductHost、ProductClient；HostPeer 与 HostSession 的 Ordered MailBox 共同处理 Join／Probe／Leave。Host 本地客户端已有进程内消息路径，远端已有 loopback UDP-KCP；当前没有地图、固定玩法 Tick 或无监听 Solo。

当前 Match.Children 全部按 Participant 遍历。Model／Hotfix 的 Product 数据与逻辑和 ModelView／HotfixView 的表现职责已存在，UIComponent、GFEntityComponent 及 owner 异步入口可以复用。正式目标餐厅依赖 LoadedSave，而此样机尚无存档选择；不得凭一个示例地图宣称 RestaurantRuntime 已落地。

## Goals / Non-Goals

**Goals:** 在现有会话树内增加一个临时地图 owner，明确格子／物品唯一状态、业务 Tick、两条传输入口及独立投影；通过简单 2D 托管视图验证 [grid-kitchen](specs/grid-kitchen/spec.md) 的触控玩法。

**Non-Goals:** 不新增 Fiber／SceneType／程序集、通用消息框架、私有 RPC 等待表、地图编辑器或配置平台；不把 Sample 当作正式厨房存档。加工持续输入规则保留在决策账本，未来接 Process 时使用，本轮不创建空加工系统。其余范围外事项见提案。

## Decisions

### 1. 在 Match 下增加临时地图 Component

以下新增类型名为实施目标；带“既有”的节点沿用源码。每进程的 ProductRoot 位于现有 `ConstFiberId.Main`、`SchedulerType.Main` 的 Fiber，Child Scene 共用此 Fiber，Main 调度不等于 SceneType.Main。

```text
Fiber: Main / SchedulerType.Main
└─ ProductRoot Scene [SceneType.ProductRoot，既有]
   ├─ Timer / CoroutineLock / ProcessInnerSender [Component，既有]
   ├─ ProductRuntimeComponent [Component，本轮配置和退出 owner]
   ├─ ProductHost Scene [Child；SceneType.ProductHost；Solo/Host 才有]
   │  ├─ Match [Child，既有，成员与临时世界的释放 owner]
   │  │  ├─ Participant... [Child，既有，逻辑成员]
   │  │  └─ GridKitchenComponent [Component，新增；唯一地图权威]
   │  │     ├─ GridActor... [Child；MemberId/绑定代次/位置/路线/手槽]
   │  │     ├─ GridStation... [Child；固定台面位置/单物品槽]
   │  │     └─ GridItem... [Child；ItemId/唯一位置/容器内容]
   │  ├─ HostSessionComponent [Component，既有；控制入口，不复制地图]
   │  │  ├─ Ordered MailBox [Component，既有]
   │  │  └─ HostPeer... [Child，既有；可信绑定和在途计数]
   │  │     ├─ Ordered MailBox [Component，既有]
   │  │     └─ LocalDelivery 或 RemoteDelivery [Component，既有]
   │  └─ NetComponent / Session... [Component/Child；仅联网 Host]
   └─ ProductClient Scene [Child；SceneType.ProductClient，既有]
      ├─ ClientSessionComponent [Component，既有；本人加入和绑定]
      │  └─ GridClientComponent [Component；独立值投影/最新命令状态]
      ├─ LocalMessageComponent [Component；Solo/Host 本地]
      │  或 NetComponent / Session [Component/Child；远端 Client]
      ├─ GridPresentationComponent [Component；本轮输入和视图引用]
      ├─ UIComponent [Component，既有 API]
      │  └─ UIGridKitchen [Component；托管 ETUI，唯一实例]
      └─ GFEntityComponent [Component，既有 API]
         └─ GridKitchenBoard [Component；托管 UGFEntity，唯一实例]
```

选择 Component 而不是 Match 的新 Child，避免破坏现有 `Cast<Participant>()` 名单遍历。地图实体和容器内容由地图／物品持有，连接、UI 和 Transform 均不拥有权威物品。角色只引用 MemberId 和绑定代次；断线保留 Participant，但角色可独立释放。后续真实餐厅 change 再处理从临时 owner 到 Kitchen／Level 的迁移。

地图权威数据放 `Model/Share/Product/GridKitchen/`，逻辑放 `Hotfix/Share/Product/GridKitchen/`，维持产品代码现有非 DOTNET 原型边界；投影值类型与协议数据在 Model，客户端安装逻辑在 Hotfix。视图模型和 System 分别放 `ModelView/Client/Product/GridKitchen/`、`HotfixView/Client/Product/GridKitchen/`，使用现有四个 ET 程序集，不把 Mono 引用带入共享模型。

### 2. Solo 复用权威与本地客户端，网络为可选入口

扩展稳定层 `RuntimeInstanceRole`、字符串解析、不可变配置及启动窗口，新增 `solo`，无 endpoint。普通 Launcher 无显式请求时，由 ProcedureET 在启动 CodeRunner 前建立本轮默认 Solo 配置，再走同一个 ProductRoot；直接 Play 必须看到 Board／HUD。显式 legacy／solo／host／client 保留原配置，默认值不覆盖请求或失败，也不读窗口未提交草稿。此前“普通 Launcher 保留示例”被用户直接启动的验收要求替换。

启动顺序：创建根组件与 Sender → Solo／Host 创建 ProductHost、Match、固定地图和 HostSession MailBox → 仅 Host 建 UDP 监听并报告实际 endpoint → 创建 ProductClient 和本地 Peer／LocalDelivery → 本地 Join → Tick 生成角色与地图快照 → 本端视图加载和投影就绪。远端 Client 仅创建客户端树、建立指定 Session 并 Join。入口立即返回，让现有 Runner 推进 Fiber；不在 Fiber 初始化中等待第一次本地 RPC。

新增明确 SoloReady 会话状态；HostListening 只用于真实网络监听，现有 HostReady／ClientReady 仍证明会话成功，不冒充地图视图就绪。地图输入使用额外的本端 GridReady：成功加入、匹配世界及本人角色快照、Board 与 HUD 可用缺一不可。无监听 Solo 能验证同一套规则，也避免把“单人开 UDP Host”作为正式单机入口。

### 3. 同一命令路径，快速 RPC 与 Tick 执行分开

外部玩法意图包含协议版本、MatchId、WorldId／WorldGeneration、递增 CommandId、Move／CounterAction／PickupDrop、目标格或目标 ID，以及发起时默认动作和预期 ItemId。消息不允许客户端提供可信 MemberId 或内部 ActorId。地图固定且首轮动作简单，入口采用标量字段、类型白名单和当前地图范围检查，不接客户端路径或任意对象数组。

```text
Solo / Host 本地 UI
 → GridPresentation 读取投影，生成相同玩法请求
 → ClientSession 的既有 CallProduct / LocalMessageComponent
 → Root.ProcessInnerSender.Call(当前 Local.Peer.GetActorId())
 → MessageQueue → HostPeer Ordered MailBox
 → [MessageHandler(SceneType.ProductHost)] 玩法 Peer handler

远端 UI
 → ClientSession.CallProduct → 当前 Session.Call
 → ProductHost 的 NetComponentOnRead Invoke 白名单
 → 捕获 wire RpcId、Session/Peer EntityRef 和绑定代次；复制外部载荷
 → Root.ProcessInnerSender.Call(当前 Session 绑定 Peer.GetActorId())
 → 同一个 HostPeer MailBox / 同一个玩法 Peer handler

共同转交
 → Peer 注入 PeerId/InstanceId/BindingGeneration 等可信元数据
 → Sender.Call(peer.Authority.GetActorId())
 → HostSession Ordered MailBox
 → [MessageHandler(SceneType.ProductHost)] 地图接收 handler
 → 验证入口并复制为地图 owner 的普通值意图，快速返回 Accepted/Rejected
 → GridKitchenComponent 固定 Tick 再验证并提交
 → 每端独立 GridWorldSnapshot / 本人 CommandResult
 → 本地 Sender.Send(client.GetActorId()) → ClientSession MailBox
   或 RemoteDelivery.Session.Send → ProductClient NetComponentOnRead Invoke
   → Sender.Send(client.GetActorId()) → 相同 ClientSession MailBox
 → [MessageHandler(SceneType.ProductClient)] 复制并安装独立投影
 → 已提交后发布 GridProjectionApplied 观察事件，驱动表现
```

每个 ActorId 来自当前 ET 实例 `GetActorId()`；Peer 地址来自实际 LocalMessage 或 Session.ProductPeerBinding，Authority 来自 Peer 的 EntityRef，不来自 wire。沿用产品 actor handler 的 RpcId 捕获、回复和释放模式：每段 Call 用自己的 RPC 关联；返回网络时复制响应并恢复原 wire RpcId，只回原仍有效 Session。Host 本地路径不使用 UDP。

Accepted 只说明意图进入地图，不持有一个“等走完”的 RPC。Move 和 CounterAction 在寻路／行走／等待空位期间保持 PendingApproach；Move 只有实际提交到达目的格才发 Executed，CounterAction 到达后再校验并发 Executed／Rejected。被新目标替换的旧行走或交互均发 Cancelled，不能把目的地已安装显示为动作已完成。结果是数据通知，不新建另一套异步等待字典。客户端以最新 CommandId 展示自身动作状态，不将旧动作结果覆盖最新操作。

地图持有每绑定的命令高水位、当前意图及有限最近结果；重复或已过期编号不再执行，编号重复且载荷不同返回错误。队列存普通值，不保留池化 DTO。一个角色只保留当前动作及最新待切换意图，避免连续点击无限积累路线。提交时仍核对世界实例、当前 Participant 绑定和 Peer 资格，Accepted 后断线的旧动作不能继续写世界。

入站 actor handler 负责请求释放，Call 的等待者释放对应 response；网络入口释放外层 request／转交 response，发送成功后按既有 Session 约定转交发送消息所有权。广播为每个接收者构造独立消息。接收端先复制布局、角色、台面、物品与结果为值投影再回收 DTO，快照不含 Host EntityRef 或可变集合引用。

### 4. Host 固定 Tick 与地图状态

`GridKitchenComponent` 的 IUpdate 在既有 Main Fiber 上累积运行时间，以固定步长推进 Tick；客户端心跳、Mono Update 和插值均不推进世界。步长和移动时长为命名的样机参数，不是最终平衡承诺；测试可以用现有 fixture 的时间推进验证，不另建时钟服务。

每 Tick 顺序：处理可信成员加入／角色退出记录 → 接收并替换合法玩家意图 → 推进一步移动和默认动作 → 增加变更版本并生成投影／结果 → 发布只读观察事件。加入／离线名单仍由已有控制入口写入；地图角色增删和落物由 Tick 写入。生命周期记录复制 MemberId、绑定代次与角色实例身份，Peer Dispose 后也能收尾；旧退出记录不能删除新绑定的角色。

首轮固定布局使用不可变样机定义，包含可走地面、阻挡台面、厨房／前厅范围、出生格和少量普通物品；不为一个布局新增地图 Excel／编辑器。世界拥有单独 WorldId／Generation、Tick、Version，快照包含相同步长／移动起点终点信息。移动进度、手槽、台面和地面物品均来自 Host。

GridItem 的位置为互斥的 Hand(MemberId)／Counter(StationId)／Ground(Cell)；容器内容属于同一个 GridItem。拿放预检完整通过后，一次提交改变两个位置引用，不在动画完成回调再写物品。普通台面默认动作只有空手拿取与持物放空台面，不提供自动交换。

### 5. 寻路、停驻与自动接近

选用固定邻居顺序的四方向 BFS，当前地图小而静态，不需要导航网格或动态寻路框架。寻路只把地图障碍视为不可通行，玩家与落物不阻挡经过。台面接近从四邻格中选择可达的最短位置，同长按固定坐标顺序；目标始终是选中的 StationId，不悄悄切换附近台面。

区分 Transit、Standing、Waiting 状态。经过满员格可以继续行走，实际停止前核对每格两人的容量；满员目的地把当前路线的停点收在最后合法格，保留原目标，并在后续 Tick 发现空位后继续。停点预约与同 Tick 稳定顺序避免两个角色同时占走最后一个名额，预约只影响停点而不封锁经过。新成员出生也选择有停驻容量的出生格，不把出生位置写成无限重叠。

新意图不改变已经开始的 NextCell 步骤；一步结束才安装最新路线或台面目标。替换目标时同步重新取得合法停点，保持满员格中的经过状态，不在那里转为第三名站立者。自动接近到操作格后设置四向朝向，并再次核对目标、预期物品与容量；目标改变即失败，不推断另一个默认动作。地面拾取只查询当前同格物品，UI 列出可选 ItemId；不同格请求拒绝，不能借台面接近规则从邻格拿落物。

### 6. 断线与整轮生命周期

单成员退出顺序：关闭原 Peer 入口并使 Session 等待失效 → 共用控制裁决离线／Leave，登记对应角色退出记录 → 下次 Tick 撤销旧意图、用角色最后提交 Cell 将 hand Item 改为 Ground、移除角色和停点预约 → 通知仍在线端。原 Peer 的在途计数继续沿现有 BeginEntry／EndEntry／CloseFinished 收尾；单成员不停止公共 Sender。重复记录检查角色实例与绑定，不重复落物。显式 Leave 同样保留物品，区别只在 Participant 是否移除。

整体退出顺序：ProductRuntime 标记 Stopping 并拒绝新入口 → 地图停止 Tick、终结当前／排队意图并取消自身工作 → 本端 GridPresentation 失效，退出 ETUI／UGFEntity 与订阅 → 向现有有效端发送 MatchClosed → 沿既有顺序结束 Client 等待与公共 Sender → Dispose Host／Client 场景，清理桥与本轮状态。临时地图随 Match 释放，不生成存档。初始化部分失败只清理已经创建的对象。

跨 await 使用原 EntityRef／InstanceId、Match／WorldGeneration、绑定代次及视图轮次；不能靠同名 WorldId 判断旧对象仍有效。请求和资源等待接入既有 `GetAsyncOwner`／owned request 入口；取消只是结束业务等待，已提交动作不回滚。旧加载失败／完成仍由原请求释放，不挂到新 Board，不清除新轮订阅。UI 隐藏或地图平移不等于退出。

### 7. 2D 视图、输入和表配置

Board 以 2D 精灵构成地面、台面、角色与落物，使用统一 Cell↔地面投影表达正对地图边的 45° 俯视，正交渲染，横轴不旋转成菱形；角色按底部落点排序，点击命中由同一投影反算格坐标。单个场景 Camera 与投影层由 Board 本轮管理，进入时捕获原相机引用和状态，退出只在原实例仍有效时恢复，复用已有渲染管线，不修改无关 URP Asset。厨房初始视野按可用竖屏区域完整取景，纵向拖动平移整个地图视野并限制在示例地图范围；不做固定厨房加独立下窗。

PlayerSettings 默认方向设为 Portrait，默认窗口为 540×960，使后续 Player 沿同一竖屏约定启动；配置回读只证明设置已保存，不替代手机包体或真实触控验收。

HUD 保留手槽、可操作台面目标及同格落物选择；目标按钮点击立即生成默认动作。GameEntry 保留 DebuggerComponent，但默认 AlwaysClose，避免其浮层挡住竖屏顶部状态和提示。手势按命名阈值区分点击与纵向拖动，UI 命中先消费输入；已经拖动的松手不提交命令。角色平滑插值只使用已确认起终点，不预测手槽、物品或权威格。点击可以立即高亮请求目标；路径显示从最新投影推导，Host 后续拒绝时清除／更正，不把显示路径当权威。

在 ProductClient 的精确 HotfixView 生命周期观察者中启动本轮展示，GridPresentation 自己持有加载结果、Ready／Failure；事件返回不等于展示成功。GridProjectionApplied 在投影安装后发布，只通知表现；事件不作为网络或命令回执。新增 handler 精确注册 ProductClient／ProductHost，保留 `ProductJoinedAllObserver` 等 All 消费者的事实；现有 NumericChange All handler 使用不同事件类型，不让产品初始化落入 Demo DynamicEvent 路线。

`GridKitchenBoard` 通过 GFEntityComponent 的唯一 Component API 显示；其精灵子节点由 Board 拥有，保存的只有渲染句柄，不另建物品状态或私有池。`UIGridKitchen` 通过 UIComponent 的唯一 Component API 打开，沿用 UGF 生命周期和 CodeBind，配置 AllowMultiInstance=false／PauseCoveredUIForm=false，UIGroupName=Default。Dispose 包装对象关闭本轮 GF 对象，不能直接 SetActive 托管根代替收尾。

注册源为 `Design/Excel/ET/Datas/Game/UI.xlsx`、`Entity.xlsx`，类型 ID 从未占用的源表 ID 选择，运行 serial ID 由 GF 分配。拟新增资源分别为 `Unity/Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab`、`Res/Entity/Product/GridKitchenBoard.prefab`，AssetName 为 `Product/UIGridKitchen`／`Product/GridKitchenBoard`。GameEntry 现有 Entity 组没有地图组，因此新增并注册 GridKitchen 组，不借 Aircraft／UI 组；Board 相机和排序层引用作为 Prefab 配置核对，不新增生成配置通道。

按 ET active 工程先执行现有 Luban Check，再导出公共表与 ET 输出、派生 UGFUIFormId／UGFEntityId；CSName 属 ge 组，经公共 Editor JSON 生成。环境没有 openpyxl，两个注册行使用现有 Python 标准库 XML／ZIP 一次性修改源工作簿，保留其余包项、样式、公式、隐藏表和校验；先确认未被占用并回读新增行，不安装依赖或建立专用改表工具。不手改 JSON／ID／bytes。Prefab 与 CodeBind 经 Editor 既有入口生成和保存，新增资源按启用规则刷新 ResourceCollection 并核对 diff；真实包体与 AssetBundle 构建本轮不要求。

用户追加授权清理 example 后，按手写消费者确定范围：公共 Scene／Sound／UISound／Music／UIEntity 的类型仍被公共 API 使用，保留结构、清空示例数据；UI／Entity 保留表结构及本轮地图／HUD 注册，UI 保留 ET 登录／帮助／大厅和 Lockstep 初始化链路。DTDemo、OneConfig 均无手写消费者，删除 __tables__ 注册及对应源表，输出由既有生成器清理。UnitConfig／AIConfig 的类型仍被公共模块使用，保留结构但清空演示单位与攻击／巡逻数据；旧 Demo 地图演示不作为当前产品能力或本轮验收。StartConfig 及 Builtin 初始化资源保留。登录页去掉 WidgetTest 的动态加载与嵌入测试节点，用 Editor 修改 Prefab 并重新生成 CodeBind，删除独立测试 Widget 资源与脚本，保留账户／密码／登录按钮和初始化入口。导出后核对预期删除和全工程编译，以一次普通 Launcher 和一次 Solo 启动确认清空表可加载、无失效 Widget 请求且地图注册可用；复用已通过的六项地图 UTF，不为纯数据清理重跑玩法套件。

### 8. 协议与增量验证

新增末尾 `Design/Proto/ET-Client/ZZGridKitchen.proto`，定义外部命令／接收答复、内部可信转交、地图快照及命令结果。现有生成器按 Ordinal 排序，新增文件放在 ZProductSession 后，生成 diff 核对既有 opcode 未漂移。提升 ProductSessionRules.ProtocolVersion，Join 拒绝旧版，未知／内部地图消息不进入 wire 白名单。不加私有序列化器，也不绕过 Proto2CS 双端输出。

局部 UTF 包含移动／改目标／三人停驻与满格穿过，物品交接／断线落点／多物品同格选择，以及共同入口／重复命令／旧绑定失效。核心用例在地图组落地，消息用例在接入组落地；沿用现有 fixture，分别发现 PlayMode 的实际 FullName 后执行对应小集一次，后续复用结果，不在收口重跑相同用例。局部结果不得冒充 Player 验收。

另做一次竖屏视图检查和一次本机 Host／Client 贯通：上下拖动不误走、两种目标入口、远处接近交接、断线后同格选择拾物、整轮退出。若使用第二工作区，只下达主工作区确定的执行命令，并遵守已提交 main 的明确同步清单；未提交玩法不可复制给第二工作区代开发。复用旧会话／异步验证，不默认全量回归、额外角色排列或手机包体。实际授权与命令发现由 apply 阶段对应 SOP 核对。

## Risks / Trade-offs

- 客户端无预测回滚可能感到延迟 → 立即显示点击目标，Host 确认后插值；用本机操作判断实际手感，出现问题再调整。
- 两人停驻与穿行在改目标时容易混淆 → 明确 Transit／Standing／Waiting 和合法停点，局部三人用例覆盖容量；不穷举极端拥堵。
- 临时地图不是正式 Kitchen／Level → 明确无存档、世界 ID 独立；下一次完整烹饪提案再迁移 owner，避免现在空建整棵未来树。
- 表导出会复制清理目标，托管加载可能迟到 → 核对源／消费者和差异，沿用现有 owner、实体实例与视图轮次；失败停止后续步骤。
- 原型协议提升版本 → 双端使用一致实现，旧版明确拒绝；不自动同步工作区或掩盖版本错误。

## Migration Plan

按 tasks 先接地图模型／命令和 Solo，再接托管视图与源表，最后贯通验证并同步产品文档。本轮不发布或修改存档格式；需要回退时停掉所属样机运行，按实际变更还原源代码、源表、资源及重新生成的产物，不删除用户文件或覆盖已有运行。

实现时更新 `Docs/Product/design.md`、`architecture.md`、相应目标规格、`backlog.md`、`long-term-goals.md` 中的相关断线待裁决索引和 `CarzyCooker当前进度.md`；主 OpenSpec 规格通过后续获授权 sync／archive 流程更新，不能在 apply 中冒充已归档。暂停队列和正式传输未决项继续保留。
