# 逐帧逻辑与内存分配审查（2026-10-14）

本轮完成静态排查及 AGENTS 约定补充；业务源码未修改。结论针对当前工作区，包含正在实施、尚未提交的恢复／饮品／装修代码，不代表这些功能已验收。此记录不自动授权实施优化。

## 结论与优先顺序

优先处理地图渲染、视口临时数组和恢复界面重复建按钮。这些路径有明确的逐帧分配，适合局部改动。随后处理 HUD 重复刷新与固定步内的排序、寻路缓冲；全量快照／消息池化涉及所有权，收益需实测后决定，不能直接删复制或把所有消息改成池化。

| 建议顺序 | 路径 | 触发频率 | 已确认的开销 |
|---|---|---|---|
| 先做 | 地图 `Render` | 地图就绪后每个渲染帧 | 5 处输入 `ToArray`、新 `HashSet`、集合差集、捕获谓词、标签字符串；全图重复写颜色 |
| 先做 | `MapViewport` | 地图就绪后每个渲染帧 | 每次 `new Vector3[4]` |
| 先做 | 恢复界面 | 失联／恢复条件成立的每帧 | `ClearTargets` 后重新创建按钮与闭包 |
| 随后 | HUD 刷新 | 投影提交事件＋版本轮询，营业通常每业务步 | 同版本重复计算；长字符串键在比较前已构造 |
| 随后 | 角色／顾客／工作遍历 | 50ms 业务步 | 重复排序、LINQ、数组快照；部分用于遍历期间增删保护 |
| 随后 | 寻路／伙伴选工作 | 新目标及等待重试，可能每业务步 | BFS 队列／字典／结果列表；每个展开格新建四邻数组；候选反复寻路 |
| 先量收益 | 快照生成与安装 | 每次脏提交；营业时通常 20 次/秒 | 独立非池化 DTO、捕获闭包、投影临时字典、数组与只读包装 |
| 谨慎局部改 | 本地消息批次 | 每帧检查；有消息时复制批次 | `list.ToArray`；必须保留 Stop／Dispose／重建时重入安全 |
| 次要 | 声音淡入淡出／色盲效果 | 淡变期间／效果启用后 | 循环新建 `WaitForEndOfFrame`；色盲回调还会创建协程迭代器 |
| 按需 | 旧示例、调试器、第三方编辑器 | 相应入口／窗口启用后 | 动画枚举转字符串、调试绘制、Inspector 分配、吸附临时容器 |

这里的顺序是按调用频率、分配证据和修复成本判断，未测得实际 GC 字节、耗时或帧率提升。空集合的 `ToArray` 可能复用空数组，不能把每个调用都算成非空数组分配。

## 扫描范围与入口

通过 `rg --files` 枚举 2561 个可见 C# 文件，扫描并提取 224 个相关候选方法体；补查 UGF 生命周期接口、ET 逐帧定时器、PlayerLoop 等待、协程、Editor update 与 GUI／SceneView 订阅。源码声明和 32 处回调注册、20 处等待位置见[完整入口索引](2026-10-14-per-frame-entry-index.md)。其中同名普通方法、第三方 API、测试辅助方法及 Editor 条件编译代码单独标注，224 不是产品实际逐帧系统数量。

覆盖 `Unity/Assets/Scripts/Game`、`Unity/Assets/Scripts/Library`（ET、UGF、UXTool、SocoTool 等源码）、DotNet、Share 和可见 Tests。Share 工具／生成器未发现产品逐帧入口；DotNet 工程链接的 Unity ET 源不重复计数。排除 Git 对象、忽略文件、Unity Library／PackageCache／Temp、HybridCLRData、bin／obj、node_modules 等缓存和构建产物。已安装包、原生插件和二进制内部实现未逐行审查，反射或外部运行态注册也不能靠文本扫描穷尽。

| 调度链 | 实际覆盖 |
|---|---|
| Unity ET | `Init.FrameDriver.Update` → TimeInfo → FiberManager／MainThreadScheduler → Fiber → EntitySystem → 各 IUpdate；LateUpdate 还处理帧末等待和同步上下文 |
| UGF | `BaseComponent.Update` → GameFrameworkEntry → Fsm、UI、Entity、Event、TaskPool、Resource、ObjectPool、Download、WebRequest 等模块 |
| UGF→ET 表现 | UIForm／Entity／Widget OnUpdate → UGFSystemSingleton marker 检查和 system 分发 → 各实际消费者 |
| 产品世界 | `RestaurantRuntime.Update` → 50ms 累积器 → AdvanceRestaurantStep → Grid／加工／物流／前厅／伙伴 → CommitGridState |
| 产品表现 | `GridPresentation.Update` → 恢复分支／MapViewport／Render／版本 HUD／输入；SoloSavePresentation 每帧只查 Escape |
| 旧样机时钟 | HostSession.Update 仅在 Match 没有 Restaurant 时推进 AdvanceLegacyGridClock，不能与餐厅时钟重复算 |
| 定时器／等待 | TimerComponent → NewFrameTimer 的 MoveTimer；Fiber 帧末等待；Awaitable IPlayerLoopItem；协程循环 |
| .NET／其他调度 | DotNet.App 及 ThreadScheduler／ThreadPoolScheduler 的循环调用同一 Fiber；不是 Unity 渲染帧。UNITY_EDITOR 把三类调度映射到主线程 |
| 编辑器 | EditorApplication.update、OnGUI、Hierarchy／Project GUI、SceneView；GUI 回调可在一帧内多次执行 |

## 热点依据与改法

### 1. 地图每帧复制集合并重新整理静态内容

2026-10-14 实施更新：`reduce-per-frame-allocations` 2.1 已移除 Render 的集合复制、差集和捕获 LINQ；沿原 Board 复用待删与着色工作区，标签字段变化才格式化。插值／跟随不短路。稳定投影、目标／进度／落物／成员与顾客移除 UTF 和实际烘烤暂停 UTF 均 Passed，普通 Launcher 画面已检查；具体证据见该案 execution-notes。以下行号为改动前依据。

[MonoGridKitchenBoard.cs](../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoGridKitchenBoard.cs) `Render`（118–209 行）：

- 123–125、131、153 行把物品、自动加工、烘烤、角色、前厅工作复制为数组；154 行新建 HashSet。
- 151、173、196 行使用 `Except(...).ToArray()` 查待删对象，即使成员不变也执行差集。
- 145、158、183–188 行对每个角色／工位重复扫描其他集合，并捕获当前对象；206 行对每个落物重新筛选、排序和计数。
- 162、170、184–193 行每帧拼接伙伴、顾客、工位标签；126–130 行重写每块地砖颜色，即使地图和高亮均未变化。

建议沿 Board owner 保留复用缓冲和 ID 索引，成员变化时才整理增删。工位布局、地砖路径／高亮、标签内容随相关投影变化更新；每帧保留移动插值、跟随位置及必要排序值。落物顺序一次整理或直接统计同格较小 ID，保持现有 ID 排列语义。不要用事件更新替代移动插值，也不要把容器内物品显示成地面物品。

数组复制、LINQ 迭代器与捕获闭包是分配线索；Vector3、Color、Rect、GridCell 的普通值类型构造本身不是托管对象分配。Text 相同值写入可能自行短路，但调用前新字符串已经生成；主要收益来自少构造字符串。

### 2. 视口计算每帧分配四角数组

2026-10-14 实施更新：`reduce-per-frame-allocations` 2.2 已消除四角数组的逐帧申请，沿 MonoUIGridKitchen 复用工作区和本轮 Canvas；保持每次计算真实布局。连续查询／尺寸变化和真实 Launcher 点击／拖动／受管重开两项 UTF 均 Passed，详见该案 execution-notes。以下行号描述改动前依据。

[MonoUIGridKitchen.cs](../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoUIGridKitchen.cs) `MapViewport` 25–29 行创建 `Vector3[4]`；[GridPresentationSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs) 85 行每帧调用。

建议将四角缓冲归 HUD view 并复用，缓存稳定的 Canvas 引用；随后按分辨率／RectTransform／布局变化刷新视口。Board 的 SetHudViewport 已有相同 Rect／aspect 短路，但发生在 MapViewport 分配之后，不能消除该数组。GetComponent 首先是重复查找开销，不能不经测量就说它每次都会 GC。

### 3. 恢复页面每帧销毁和重建按钮

2026-10-14 实施更新：`reduce-per-frame-allocations` 3.2 已按恢复显示完整上下文变化刷新，稳定失联帧保留按钮。扩展真实断线／手动恢复／重开 UTF 1/1 Passed，含 12 帧实例稳定及同状态反馈／新尝试／旧回调；未缓存凭证内容。具体见该案 execution-notes；以下为改动前依据。

[GridPresentationResumeSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationResumeSystem.cs) 9–46 行由 Update 76 行调用。条件成立后 27 行 ClearTargets，30 行新建 inputGuard，40／44 行重新 AddControl。[MonoUIGridKitchen.cs](../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoUIGridKitchen.cs) 36–38、55–65 行实际 Destroy／Instantiate，且为每个按钮创建监听闭包。

建议每帧只判断连接状态；Stage、AttemptId、Feedback、凭证可用性、HUD 实例或 Client 身份变化时才更新恢复页面。沿既有 HUD／Widget owner 保留按钮，状态切换更新文本和可用性，退出解绑。必须保持旧尝试、旧 HUD、旧绑定回调失效；不能只缓存 Stage 而漏掉同 Stage 的反馈／身份变化。本路径属于进行中 cooperative-member-resume 的当前实现，未作真实断线复现。

### 4. 同一投影可能刷新两次 HUD，长字符串比较键仍持续分配

2026-10-14 实施更新：`reduce-per-frame-allocations` 3.1 已记录已展示版本并用复用字段序列替代长字符串键，信息与动作结构分别比较。事件＋轮询稳定实例、同版本反馈、本地面板／菜谱、旧绑定及真实目标详情 UTF 均 Passed；受管远端重开／旧回调和新缓存下烘烤／暂停补验 Passed。详见该案 execution-notes；以下为改动前依据。

[GridClientSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridClientSystem.cs) 安装快照后 ConfirmGridProjection 发布事件；[GridPresentationSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs) 521–524 行事件立即 RefreshGridHud，而 Update 94 行仍用 HudVersion 检查。RefreshGridHud 自身不把 HudVersion 同步为已展示版本，所以之后的 Update 还会刷新同一版本。

RefreshGridHud 282–305 行为按钮重建比较键，对多个集合排序、逐项插值、string.Join 和连续拼接，然后才判断 HudTargetsKey 相等。相等可避免重建按钮，却没有避免计算键和上方信息文本。营业时 RestaurantRuntimeSystem 145–148 行每业务步推进时间并置 Dirty，因此“按版本刷新”仍通常是 20Hz。

建议合并为一条 HUD 刷新入口，事件只标记待刷新或刷新时记录已展示版本；不能漏掉版本不变但命令结果、选中目标或本地面板变化。用现有版本及具体显示字段比较替代序列化长字符串，不另造 hash 清单；将时间／进度文本与按钮结构分开，结构不变则不重建。优先减少刷新次数，再考虑沿 owner 复用按钮。

### 5. 固定步的排序与快照遍历可以复用，但顺序和增删语义必须保留

2026-10-14 实施更新：`reduce-per-frame-allocations` 4.1 已按厨房／FrontLevelState／FrontWorkCommands owner 复用有序 EntityRef 快照与原批次命令；CanStop 直接计数。保留 MemberId、工作 ID、Ask 优先／FIFO 和 Stop→推进→Claim；物品争抢、伙伴调度、顾客退出／工作取消后继续推进 5/5 Passed。以下为改动前依据；非热控制入口、AutoWork／BakeWork 等其余候选仍保留。

[GridKitchenSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridKitchenSystem.cs) OrderedActors 116 行每次 OrderBy／Select／Where／ToArray，AdvanceGridTick 205 行调用，CanStop 318 行再次调用。CanStop 只统计容量，通常不需要排序；主 Tick 的 MemberId 顺序不能随意换成字典顺序。

[GridPrepSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs) 145 行每步复制 Process；[GridCustomerFlow.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridCustomerFlow.cs) 23 行排序复制 Customer；[GridFrontWorkSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridFrontWorkSystem.cs) 56–61、114 行复制待执行命令、两次筛选及排序遍历伙伴和工作。AutoWork／BakeWork／SupplyShipments 也有 Select／OfType 迭代链。

建议容量统计直接循环；需要稳定顺序的列表在成员变化时维护，或每步复用排序缓冲。可能在处理过程中 Dispose／增删的遍历保留安全快照；前厅 Stop 先于 Claim 的执行顺序也必须保持。避免拿“集合本来有 owner”当作可以删 ToArray 的证明。

### 6. 等待重试和伙伴候选选择会放大寻路分配

2026-10-14 实施更新：4.2 已复用厨房固定容量 BFS 队列／前驱数组，内部改为长度／可达性查询或填充各自 Path；展开邻格不再创建数组，公开路线仍独立。换目标、满格等待／穿行、新布局三类角色路径及连续／不可达／同格搜索 4/4 Passed。工位几何数组、公开 Neighbours 与公开 FindGridPath 的独立结果分配保留。

[GridKitchenSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridKitchenSystem.cs) 230、293–354 行：等待目的格空位时重新 FindGridPath；每次新建 Queue、Dictionary、结果 List。展开节点调用 Neighbours；[GridStationGeometry.cs](../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridStationGeometry.cs) 32 行每次返回一个新四邻数组。

[GridCompanionSchedule.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridCompanionSchedule.cs) 19–24 行为可用工作排序，对候选工位各接近格试算 FindGridPath，只取“是否可达”仍建立完整路线。[GridStationAccess.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridStationAccess.cs) 每次通过 Cells 创建占地／接近数组；后者也用于工作资格检查和快照生成。

建议先消除每个 BFS 节点的邻居数组，再归厨房 owner 复用一次搜索的队列／前驱／结果缓冲；试算可达性时只生成需要的结果。缓存工位几何须随 LayoutVersion、朝向、装修／升级及换关正确失效。保留四邻顺序、等待满员格和伙伴请求路径；不能为减少重复寻路而让伙伴忽略目标变化或直接改写裁决状态。

### 7. 消息和投影的全量复制是大分配候选，同时也是既有隔离合同

[GridMessages.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridMessages.cs) CreateSnapshot 生成世界及其嵌套状态、每端独立消息；[ProductMessages.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductMessages.cs) 11 行明确 `ObjectPool.Fetch(..., false)`，当前并非可复用消息池。对象的 List 初始化、捕获配置闭包、路径／占地及所有前厅／物流状态一起放大分配。

[GridClientSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridClientSystem.cs) 71–100 行先建立 14 组临时字典以及路径／内容／候选只读数组，115–137 行再 Clear／Add 到已有投影字典。营业每步更新计时会触发脏快照，即使工位布局等静态信息未变也复制。

先减少无必要排序与捕获闭包，按实际 owner 预设容量；用一次代表性运行确认快照是不是剩余主热点，再决定是否复用安装暂存区或缩小不变数据更新。仍需完整复制后安装、消息回收后值独立、旧世界／旧关／旧绑定拒绝、每端独立消息和快照后结果顺序。不要直接持有 Host EntityRef、共享消息 List、把 Fetch false 改 true 或为减少 GC 改协议频率。

### 8. 本地消息每个非空批次复制数组，不能破坏退出重入

[ProcessInnerSenderSystem.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Actor/ProcessInnerSenderSystem.cs) 86–106 行：每帧 Fetch 最多 1000 条，然后 ToArray、Clear、遍历独立批次。有消息时产生数组；空批次不应被记成同样分配量。

独立批次让 handler 结束等待时发生 Stop／Dispose／重建仍可处理剩余旧消息，依据[网络主规格](../openspec/specs/network/spec.md)。若优化，选不会被回调退出清空的局部租用缓冲，或明确隔离的双缓冲；归还时清引用并在所有出口收尾，检查实例身份及剩余消息恰好一次 Dispose。此处属于共享基础，应复跑对应消息退出／重入 UTF；优先级低于表现层明显分配。

### 9. 启用后的声音协程和色盲效果

[DefaultSoundAgentHelper.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Sound/DefaultSoundAgentHelper.cs) FadeToVolume 431–438 行在淡变循环每帧 `new WaitForEndOfFrame`；[ColorBlindnessEffect.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Components/ColorBlindnessEffect.cs) OnPostRender 43–55 行同样 new 等待对象，IEnumerator 渲染回调还涉及每次调用的迭代器。

可按语义复用 WaitForEndOfFrame（本项目 [UGuiExtension.cs](../Unity/Assets/Scripts/Game/UI/UGuiExtension/UGuiExtension.cs) 已有范例）；不能把帧末等待直接替换为 `yield return null` 而改变时序。CameraComponent／ProcedureComponent 的帧末等待是启动一次，PlatformEditor 的 WaitForSeconds 是延迟一次，不能归为常驻逐帧分配。GuideGesture 的 `yield return null` 循环没有每次新建等待对象。

### 10. 旧示例、调试界面和编辑器优先级较低

- Demo／LockStep Animator Update 使用 MotionType.ToString，但有 isStop／None gate，并非每个空闲帧都分配；可以在该示例确有消费者时缓存 Animator 参数 ID。LSUnitView、LSCamera、LSOpera 主要是数值与插值；UILSRoom 只在帧数改变时转字符串。锁步 SaveLSWorld／Record 涉及样例状态序列化，不能当当前做饭链路热点。
- GizmosDebug.OnDrawGizmos 绘制既有 Path，未见常规路径新建集合。另补查 OnRenderImage、OnAudioFilterRead、OnTriggerStay／OnCollisionStay（含 2D）及渲染管线订阅，本轮可见源码未发现其他此类实现。
- Debugger OnGUI 会按窗口绘制并格式化文本，IMGUI 本身有布局开销。ActiveWindow 有 gate；未核实当前实例窗口状态，不把调试期开销算成产品稳态。ResourceComponent 中 GC.Collect 只有卸载完成且 performGCCollect 被显式设置才执行，不是每帧无条件 GC；Game 目录未找到请求该选项的调用。
- UXTool GuideHighLight 已复用 targetCorners；GuideTargetStroke／GuideHighLight 仍可缓存 RectTransform，主要属于查找／表现开销。UXRolling／LXRolling 复用四角数组，UXRandomInitializer 主要构造值类型，不应误报为每帧 new 托管数组。
- [GameTimer.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Common/GameTimer.cs) 134、145、156 行逐次插值 `Profiler.BeginSample` 名称。计时器存活且该 Profiler 调用编译启用时会持续创建字符串；应在 timer 名称设置时缓存标签。Editor Tick／FixedTick／LateTick 已注册 update，Player 是否有消费者须查实际接入，不能据 Runtime 目录就断言产品正在运行它。
- Editor InputSlotDrawer OnGUI 反复 new ReorderableList／List／ToArray，ET BuildToolEditor 每次 new GUIStyle，ResourcePackBuilder 每次建版本数组；可按目标／配置变化缓存。只在这些面板打开时有意义。
- UXTool IntervalLineLogic 的 ListenMoving → FindInterval 会分配 flags、TagList 等（214–222 行），VisualLineManager.Init 615 行匿名订阅 update，Close 没有对应解绑该匿名订阅。应先核实实际开启、关闭次数与回调存活，再局部复用和解绑。Edge／Interval 的 FindObjectsByType 在 ResetAll（选择／层级变化）内，不是每帧全场景扫描。
- DotRecast Crowd／DynamicNavMesh／TileCache 有名为 Update 的 API 及内部容器构造；本次未找到产品调用它们的路径。不应为当前格子厨房优化整份第三方寻路库。

## 已有做得合适的部分

ET EntitySystem、主线程调度、ThreadSynchronizationContext、UGF 模块／UI／Entity 分发使用既有队列或集合；FsmManager 复用 m_TempFsms，UIWidgetContainer 直接遍历已有 List。未见这些分发方法稳态无条件新建业务集合，不建议因存在每帧调用就重写公共调度。

TimerComponent 无任务／未到期时短路，动态事件全面清理间隔 60 秒，AssetSet 检查间隔 30 秒且释放后重置，ObjectPool 按 auto release interval。Download／WebRequest／资源请求 helper 在在途任务或完成／失败时才工作；请求结果 byte[] 的创建属于完成事件，而不是空闲每帧。Awaitable 任务源已有 TaskPool，moveNext 的捕获通常在启动等待时创建，不能说每次轮询都新建整个 async 任务。

## 验证与下一次实施边界

后续实施验收：`reduce-per-frame-allocations` 已 9/9，当前业务小集 9/9 和真实 UI／KCP 恢复入口 4/4 Passed；未变化的视口／Board／HUD 行为复用第一批结果。同配置准备／营业帧分配约 319785→298268 B、394854→338037 B，主线程约 16.66 ms 无明显退化；恢复页单次刷新 0.120647→0.000318／0.000347 ms，但整体帧 GC 两次 345605／316704 B 波动，不能声称恢复场景总体 GC 稳定下降。原始输出及具体功能映射见 [execution-notes](../openspec/changes/archive/2026-10-14-reduce-per-frame-allocations/execution-notes.md)。下表保留最初静态审查的历史范围，不代表后续未运行。

| 本轮验收点 | 方法 | 结果 |
|---|---|---|
| 查找逐帧及间接入口 | 可见源码枚举、方法体、接口／订阅／定时器／协程检索及关键调用链阅读 | 已完成静态排查；索引保留同名误匹配类别 |
| 核实热点分配 | 阅读实际 new／复制／字符串路径和触发 gate，与相关主规格及进行中恢复 change 对照 | 已确认分配来源，收益排序为静态判断 |
| 增补 AGENTS | 检查差异、格式与约定含义 | 已补充；不声称存量已符合 |
| GC.Alloc／帧耗时／实际失联复现 | Unity Profiler／真实入口 | NotRun：本轮未实施功能或进行动态性能验收 |
| UTF／Player／Android／双 Editor | 功能实施后的最小相关验收 | NotRun：本轮为源码审查和文档修订 |

后续优先做表现层局部优化，并沿本项目 apply／UI／Entity 技能执行相关变更。只需一次代表性预热后场景比较 GC.Alloc／帧耗时，覆盖地图移动插值、路径高亮、落物、标签进度、HUD 切换及关闭重开；涉及恢复再覆盖反馈变化／重试／旧回调。消息或投影复用另按其实际风险补对应 UTF，不默认全仓测试或大规模性能平台。

通用依据：[Unity 官方 GC 最佳实践](https://docs.unity.cn/6000.1/Documentation/Manual/performance-garbage-collection-best-practices.html)说明集合／数组复用、闭包与装箱等分配来源；本报告的具体热点以链接的当前项目源码为准。规则用于减少不必要分配，不要求为了绝对零分配牺牲 owner、独立投影或迭代效率。
