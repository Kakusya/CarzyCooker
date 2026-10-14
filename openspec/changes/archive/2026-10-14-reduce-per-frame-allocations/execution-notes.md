# 执行说明

## 当前状态

2026-10-14 归档续记：用户明确授权归档后 commit。CLI 已归档至 `2026-10-14-reduce-per-frame-allocations`，本案无 delta specs，无主规格同步。实现未变，复用上方各组运行证据；归档后修正相对链接、README 与进度索引，提交实现、测试及相关资料，原始日志／临时截图不纳入。以下“未提交或归档”为当时实施收尾的历史状态。

归档后全量严格验证 46/46（10 个活动 change、36 份主规格）通过；本案 tasks 仍为 9/9。12 份相关 Markdown、515 个本地引用无缺失，编码／尾随空格与 `git diff --check` 通过。功能和性能实现未变，未因归档重跑已有完整验收；原始日志、临时截图及无关修改排除在本次提交之外。

2026-10-14 后续授权：用户要求继续将本案做到 9/9。剩余 4.1、4.2、5.1、5.2 现已逐项验收，九项任务全部完成；未提交、push 或归档。第一批结果在行为与条件未变时复用，以下批次记录各自范围。

### 5.1 综合入口已验收

当前固定步／寻路实现编译完成后，先发现真实 PlayMode FullName，再用既有 NUnit category `PerFrameIntegration` 运行 4 项：

| 真实入口／功能 | 当前运行用例 | 结果 |
|---|---|---|
| 普通 Launcher 地图点击／拖动、实际移动、HUD 防穿透、Buff 面板、受管关闭重开／池复用 | `SoloRestaurantUiLifecycleInputAndProjectionWithSoloBuffDrawReopen` | Passed |
| 受管拿／放／烘烤、进度与完成、暂停冻结及恢复继续 | `SoloRestaurantUiProjectionActualBakingSeparatesReceiptProgressAndCompletion` | Passed |
| 实际 KCP 客户端独立投影、远端拿放、HUD 重开、退休 Host／旧 Mono 回调不能操作新 owner | `CoopCoreUiRemoteManagedReopenAndRetiredHostCallbacksKeepTheirOwners` | Passed |
| 真实断线落物、稳定恢复按钮、Feedback／新尝试更新、重开和手动恢复原成员／新绑定、退休输入拒绝 | `CoopMemberResumeUiRealDisconnectManualButtonReopenAndRetiredInput` | Passed |

4/4 Passed，AutoTesting 退出 0，Console 覆盖完整；原始终态在 `Unity/Temp/pipeline_test_status.json`，完整输出在本会话 CLI／原 Editor.log。第一批纯表现的 `PerFrameViewportContinuousQueriesObserveActualLayoutChanges`、`PerFrameBoardStableProjectionInterpolationChangesAndRemovals`、`PerFrameHudSameVersionEventsFeedbackPanelsAndBindingUseCurrentContext`、`SoloRestaurantUiInformationHierarchyTargetDetailsAndPrimaryActionUseCurrentProjection` 实现和行为未变，复用各自 Passed，分别证明实际布局／插值、高亮与同格落物排序／删除、同版本反馈／面板、详情／主要动作，不重复跑。

另从普通 `SceneHelper.StartScene("Assets/Launcher.unity")` 真实选档按钮进入两菜试玩：查看 Ready 后 `Unity/Temp/first-batch-board.png` 的厨房工位、角色、伙伴及 HUD；真实选择 Buff／开门／点击格子／拿面包／放烤箱／烘烤后，拖动相机并查看 `Unity/Temp/full-batch-front.png`，前厅餐桌／顾客阶段标签、伙伴问询与订单显示正常。截图只补画面检查；动作、落物／高亮、暂停和重开的行为由上列 UTF 及 4.x 对应断言证明。

### 5.2 同条件性能对照与边界

同一 Unity 6000.3.18f1／Editor、1469×719 GameView、默认设置、普通 Launcher 两菜菜单 0，复用 1.1 的临时 Pipeline 脚本、操作和采样方法。方法预热 10 次／计时 60 次，帧计数预热 15 帧／采样 60 帧；恢复仍真实 Host＋CoopUiRemote、Session.Dispose，稳定失联 30 帧后计 60 帧。Preparing 稳态 Tick=3（原基线为 6，均为准备冻结态）；营业采样 Ready=true、Operating，实际操作不改业务状态。只使用有效帧 GC／Main Thread recorder 和 Stopwatch 耗时，不采用不可靠的局部 GC 字节值。

| 场景 | 帧 GC 基线 → 当前 | Main Thread 基线 → 当前 | 局部方法基线 → 当前（ms／次） |
|---|---|---|---|
| 准备态无输入 | 319785 → 298268 B（约 -7%） | 16.664 → 16.666 ms | Viewport 0.000413→0.000287；Render 0.047713→0.006837；HUD 0.055137→0.005820 |
| 同入口营业移动／拿放烘烤／自然来客 | 394854 → 338037 B（约 -14%） | 16.661 → 16.670 ms | Viewport 0.000410→0.000270；Render 0.050520→0.007503；HUD 0.062738→0.006112 |
| 真实失联恢复页 | 322119 → 345605 B；针对异常补一次为 316704 B | 16.383 → 16.387／16.389 ms | 恢复刷新 0.120647→0.000318／0.000347 |

准备／营业的整体帧分配下降，局部渲染与 HUD 重复工作明显减少，帧耗时无明显退化。恢复页目标重建已由按钮实例断言和方法耗时确认消除，但整个 Editor 帧分配两次波动，基线落在附近范围；不能从较低的一次声称恢复场景总体 GC 稳定改善。两次数据全部保留，不再扩大采样；准备／营业的改善及各目标路径明确移除的分配足以验收本次局部优化。

本次移除或复用的是 Render 复制／差集／标签重复格式化、视口数组、稳定 HUD／恢复页重建、固定步排序快照、内部 BFS 队列／前驱／节点邻格数组和候选完整路线。仍保留首次创建／扩容、变化事件／按钮、公开独立路线、工位几何数组、非热 OrderedActors、全量消息／快照／投影安装及 Editor 开销；未声称全局零 GC，也未将表现优化扩为消息池或全量安装复用。

原输出是本会话 `FirstBatchBaseline.Main`／`Cooking`、`FirstBatchRecoveryBaseline.Main` 回执与原 Editor.log，恢复实际临时监听 `127.0.0.1:59883`／`:60055`，远端 finally Dispose、随后停止本次 Host Play。首个准备采样为 362001 B，但发现 UTF 留下 runInBackground=true，与基线不同，恢复 false 后才取得表中可比结果；不同条件数据不混用。一次选档脚本早于 HUD 加载发生 NullReference，已补等待；一次 Host 域重载期间 CLI 连接重置未启动采样，确认 playing／无 reload 后执行。临时恢复设置脚本曾调用不适用于持久对象的 SaveToSerializedFileAndForget，Console Error 1 条明确定位到 Temp 脚本；移除该调用，沿 SerializedObject／SetDirty／SaveAssets 保存后，ProjectSettings.asset 无差异。原失败和错误未清除、未当功能通过。

最终编译 completed／failed=false；4.1、4.2、综合 UI 共 5＋4＋4 个用例全部 Passed，无 Skipped／空集。最后两次 Host 运行的 Console 游标增量均无 Error／Exception，当前 groundTruth consoleErrors=0。Editor 保持 stopped；runInBackground=false，未修改其他工程、Player 或设备。

### 最终自审

完整性按九项任务和下方功能映射核对；正确性以各组当前运行、纯表现未变结果和真实入口对照主合同。设计一致性核对原 owner 树、MemberId／ID／优先级、前厅阶段顺序、同步 EntityRef 快照清理、公开独立路径与原消息边界；未引入新架构、池或生成物修改。`skip_specs: true` 的 delta requirement／scenario 检查为不适用，未伪造规格覆盖率。性能限制以上述恢复页波动和范围外项目为准。

| 核对维度 | 结果 |
|---|---|
| 完整性 | CLI `progress.total=9 / complete=9 / remaining=0`，`state=all_done`；九项验收有对应原运行或采样 |
| 正确性 | 当前业务／路径／综合入口 13/13 Passed；未变化的四项纯表现结果复用；delta 规格检查按声明不适用 |
| 一致性 | 局部 owner、同步缓冲、生命周期／独立值、消息路径与 design 一致；无阻塞问题 |

最终严格 `openspec validate reduce-per-frame-allocations --strict --no-interactive` 通过，skip_specs 的零 delta 按声明接受；`git diff --check` 通过。10 份相关 Markdown、381 个本地文件引用无缺失，UTF-8／尾随空格检查通过。最终 Console 从清理后游标读取无新增错误、groundTruth errors=0，Editor ready／stopped、无编译或域重载；ProjectSettings.asset 无本轮差异。已按用户要求完成 9/9，保留上方性能及设备范围限制，未执行提交、push 或归档。

### 4.1 固定步遍历已验收

厨房持有 MemberId 排序的角色 EntityRef 工作区及手工作业快照；CanStop 直接读取索引计数，不再排序或覆盖角色步缓冲。FrontLevelState 持有顾客／餐桌缓冲，餐桌相同配置排序值仍保留原枚举次序；FrontWorkCommands 分开持有原批次命令、移动角色、工作和调度候选，Stop→创建／转换→角色移动→工作推进→Claim 的次序保持。伙伴候选保留 Ask 优先与 ID FIFO，角色调度保留原成员枚举顺序。所有快照在消费时重新解引用，阶段末及 owner Awake／Destroy 清有效引用，不跨 await；少量非热控制入口的 OrderedActors 和暂停快照仍保留原实现，不声称全域零分配。

PlayMode 先发现真实 FullName，再用现有 UTF category `PerFrameTraversal` 运行最小 5 用例：`GridCoreItemRaceDropIdentityAndSameCellPickup`、`SoloCompanionScheduleAskPriorityAndStartedWashIsNotPreempted`、`SoloCompanionScheduleSameKindUsesCreationFifo(False/True)`，以及新增 `PerFrameTraversalGoneCustomerAndCancelledWorkKeepFollowingObjectsAdvancing`。5/5 Passed，AutoTesting 退出 0，Console 覆盖完整；新增断言证明 Gone 顾客不阻止下一顾客推进，前一工作取消后后续洗碗仍推进并保留原执行者。原始输出位于 `Unity/Temp/pipeline_test_status.json`／会话 CLI，编译 completed、failed=false。未扩大到无关餐厅套件。

### 4.2 BFS 已验收

固定地图的 BFS 队列和前驱索引数组由 GridKitchen 持有，每次搜索重置前驱有效内容，热循环按上／右／下／左构造值类型邻格。内部长度／可达性查询不构造完整路线；玩家借厨房的 ActorRouteBuffer 判定停驻前缀后复制到自己的 Path，伙伴和顾客直接填充自身 Path。搜索与复制之间没有 await／消息或事件分发；公开 FindGridPath 每次创建独立结果，下一次搜索不会覆盖。容量判断、满格等待及 50ms Tick 保持；公开 Neighbours 和冷启动几何数组不改。

PlayMode `PerFramePath` 发现并运行 4 用例：`GridCoreRetargetFinishesStepAndCancelsOldInteraction`、`GridCoreTwoStandThirdWaitsAndCanCrossFullCell`、`SoloStationTerrainPlayerCompanionAndActualCustomerUseMovedTableTerrain`、新增 `PerFramePathConsecutiveSearchesKeepIndependentRoutesAndResetUnreachableWorkspace`。4/4 Passed，AutoTesting 退出 0、Console 覆盖完整。覆盖当前一步完成后换目标、第三人等待及满格穿行、实际布局变化后玩家／伙伴／顾客沿新通路、同长路线选上再右、连续搜索独立结果、不可达后清理工作区和同格单元素路线。原始输出仍在 Pipeline 原位与会话；编译 completed、failed=false。

第一批历史记录（后续授权已由上方覆盖）：2026-10-14 用户当时授权 roadmap 第一批（1.1、2.2、2.1、3.2、3.1），第二批当时不在实施范围。以下保留各组原始结果与早先“仅规划”语境。

### 1.1 改动前基线

源码未改前，经普通 `SceneHelper.StartScene("Assets/Launcher.unity")` → 受管选档的“内存试玩”（保留两菜菜单 0）进入。Unity 6000.3.18f1、默认 Editor 配置、实际 GameView 1469×719，Ready=true，准备态 Tick=6 稳定。通过现有 Pipeline `run_script` 临时调用与 ProfilerRecorder 读取，不新增项目工具。方法预热 10 次，帧计数预热 15 帧后采 60 帧；后续同配置复测可直接复用临时脚本 `Unity/Temp/FirstBatchBaseline.cs`。临时脚本不进入 Assets，不触发资源导入。

| 操作／观测范围 | 平均帧 GC Allocated In Frame | 平均 Main Thread | 方法耗时（预热后 60 次） |
|---|---|---|---|
| 无输入 Preparing，普通 Launcher 实际 HUD／Board | 319785 B | 16.664 ms | MapViewport 0.000413 ms；Render 0.047713 ms；RefreshGridHud 0.055137 ms |
| 同入口选择 Buff、开始营业、点格移动、拿面包→放烤箱→开始烘烤，前厅自然来客 | 394854 B | 16.661 ms | MapViewport 0.000410 ms；Render 0.050520 ms；RefreshGridHud 0.062738 ms |
| 实际 KCP Session.Dispose 后稳定 Disconnected，受管恢复页 | 322119 B | 16.383 ms | RefreshGridHud 恢复分支 0.120647 ms |

恢复页使用现有 CoopUiRemote 夹具，先按既有测试释放 Host 的受管表现，再创建远端表现，实际监听 `127.0.0.1:57943`，采样后远端由原 Dispose 释放，Host Play 已停止。没有双 Editor／Player。GC 与 Main Thread recorder 均有效；帧数据包含 Editor 与既有快照成本，只作代表性基线，不解释为产品独占成本。局部 `GC.GetAllocatedBytesForCurrentThread` 在该 Mono 返回 0，另试 GC.Alloc marker 的逐样本值不稳定，因此不采用其字节结果；方法只记录 Stopwatch 耗时，不报告虚假的零分配。

原输出：本会话 Pipeline CLI 的 `FirstBatchBaseline.Main`／`Cooking` 和 `FirstBatchRecoveryBaseline.Main` 回执；Editor 原日志 `C:/Users/Administrator/AppData/Local/Unity/Editor/Editor.log`。临时探测首次在 EditMode 等待帧超时（已由随后 Play 域重载结束）；两次远端等待失败分别查明运行 ID 和单实例 HUD 占用，修正调用上下文后取得上述有效结果。原失败未删除，均未作为 UTF 通过。未清 Console。

### 2.2 视口已验收

MonoUIGridKitchen 持有四角数组与本轮 Canvas 引用；每次仍重新计算真实角点及 rootCanvas 相机，ReleaseHud 清 Canvas。未改表、Prefab、Canvas 结构或 CodeBind。Board 的原 Rect／aspect 短路保留。

PlayMode `ET.SoloRestaurantUiTests.PerFrameViewportContinuousQueriesObserveActualLayoutChanges` 1/1 Passed：60 次稳定查询一致，实际 RectTransform 改尺寸后与屏幕几何相符，Board 输入区同步，恢复尺寸后恢复原视口。`ET.SoloRestaurantUiTests.SoloRestaurantUiLifecycleInputAndProjectionWithSoloBuffDrawReopen` 1/1 Passed：真实 Launcher 地图点击、拖动、HUD 防穿透、两次受管重开、池化 Mono 重用与旧回调保护。两次 AutoTesting 均退出 0、Console 覆盖完整；编译 completed／failed=false。原始测试输出在 `Unity/Temp/pipeline_test_status.json`（后续运行由 Pipeline 正常更新），完整消息在原 Editor.log 和会话 CLI。

旧生命周期用例首次失败于过期的“准备”精确文案断言；已改为当前三关的“准备 · 第 1/3 关”，重新执行后通过，未减少原输入／生命周期行为断言。该测试修正不改变产品显示。

### 2.1 Board 已验收

Board 同步读取原独立投影的具体字典；待删 ID 列表、前后路径着色字典、标签显示字段由 Board 持有，ReleaseBoard 清空。Render 不保存 GridClient／Host 实体；保留每帧位置、朝向、标签和手持物跟随。标签先比较名称、阶段、显示百分比等字段，颜色和工位几何值相同不重复写；同格落物按较小 ID 数量排列，容器内容不画成落物。

`ET.SoloRestaurantUiTests.PerFrameBoardStableProjectionInterpolationChangesAndRemovals` 1/1 Passed：稳定重复渲染保留对象／标签字符串，真实 Transform 断言插值和手持跟随，目标切换恢复路径色、路径退出恢复地砖，同格逆序插入仍按 ID 排列，容器隐藏，成员／顾客／工位与落物移除，20%→35% 标签更新。`ET.SoloRestaurantUiTests.SoloRestaurantUiProjectionActualBakingSeparatesReceiptProgressAndCompletion` 1/1 Passed：真实 Launcher 受管按钮启动烘烤、进度／完成及暂停冻结、恢复继续。两次 AutoTesting 均退出 0，Console 覆盖完整。原始 Pipeline 输出和 Editor.log 保留原位。

另从普通 Launcher 打开两菜试玩，实际 Ready 后捕获并查看 `Unity/Temp/first-batch-board.png`：地图、工位短标签、角色／伙伴和 HUD 正常显示。首次过早截图为黑图，未作为验收；等待 Ready 后画面有效。初次编译诊断 ET0002 后保持 Mono 只接收投影字典，未扩大友元权限；测试 DTO 改用既有 Create/Dispose 后编译完成、failed=false。

### 3.2 恢复页已验收

每帧继续检查恢复条件并禁用普通输入；仅在 Stage、AttemptId、Feedback、凭证可用性、原 Runtime／Client／HUD 或 HudLoadEpoch 变化时更新显示和按钮。Close／重开与恢复退出清本轮比较上下文；输入 guard 仍在实际点击时核对原实例、尝试号与阶段。缓存不含凭证内容，未改变协议／恢复裁决。

扩展的 `ET.SoloRestaurantUiTests.CoopMemberResumeUiRealDisconnectManualButtonReopenAndRetiredInput` 1/1 Passed：真实 KCP 断线后连续 12 帧按钮实例不变、同状态 Feedback 立即显示、同状态 AttemptId 更新后旧回调被拒绝；原用例同时覆盖 HUD 关闭重开、池复用、真实手动恢复、原成员／新绑定、断线落物、旧正常输入与取消恢复。AutoTesting 退出 0、Console 覆盖完整，编译 completed／failed=false，原输出仍在 Pipeline 原位与 Editor.log。原 cooperative-member-resume 的协议和跨 Editor 结果仅作为未改合同复用，本次新显示行为由该运行证明。

### 3.1 正常 HUD 已验收

RefreshGridHud 记录实际已展示版本，事件刷新后 Update 不再为同版本重复刷新。原长字符串键改为 owner 内复用的数值／字符串字段序列，按字段与列表有效长度比较；覆盖原输入，并补齐世界／绑定／HUD 身份、餐厅权限、升级目标和饮品贴单等实际动作依赖。动作仍沿原 ClearTargets/AddTarget 和原排序创建，不加私有按钮池。信息序列另比较命令反馈、显示时间、进度、前厅工作及结果；未变化时不格式化信息，进度变化不重建动作。Close／重开／Destroy 清有效字段，容量保留给原 owner 复用。

`ET.SoloRestaurantUiTests.PerFrameHudSameVersionEventsFeedbackPanelsAndBindingUseCurrentContext` 1/1 Passed：真实投影事件立即记录版本，随后轮询／显式刷新保留按钮与状态字符串；同版本拒绝反馈更新但动作实例不变；候选和菜谱本地切换无版本变化仍生效；旧绑定回调不提交，关闭重开首次完整显示。指定 `ET.SoloRestaurantUiTests.SoloRestaurantUiInformationHierarchyTargetDetailsAndPrimaryActionUseCurrentProjection` 1/1 Passed：当前目标详情、主要动作、读详情不提交命令、拖动／点击合同保持。

统一失效和退出清理改动后，追加运行 `ET.SoloRestaurantUiTests.CoopCoreUiRemoteManagedReopenAndRetiredHostCallbacksKeepTheirOwners` 1/1 Passed，以及恢复页扩展用例 1/1 Passed；新信息缓存下再次运行真实烘烤用例 1/1 Passed，确认进度、完成与暂停显示。上述 AutoTesting 均退出 0、Console 覆盖完整，输出留 Pipeline 原位／会话／Editor.log；这些结果可供第二批 5.1 复用，不因收口再跑相同覆盖。

### 第一批局部自审

使用 apply／verify 对照本批五项任务和 design 的表现 owner／字段比较／消息边界；本案显式 skip_specs，没有 delta requirement 可验证，沿已读 restaurant-ui／grid-kitchen／ui／entity／product-session 主合同检查相关行为。固定步／BFS 及综合性能复测仍属于第二批，不把本批验收当作整个 change 可归档。

自审发现工位包围盒与落物序号此前仍每帧计算，已沿 Board 补充有限几何输入和地面排列上下文；仅输入变化时重算，退出清空。相应 Board 用例增加真实移动／占地变化、落物移格和成员变化断言并补跑；其余实现与行为未变的结果复用。当前不报告总体分配降幅或整局零 GC；5.2 的一次同配置复测留在固定步／寻路完成后。

最终 Board 补跑 1/1 Passed、Console 覆盖完整；普通 Launcher 在最终实现下重新进入 Ready 并查看同一原位截图，工位、落物、角色与标签正常。收尾通过 Unity API 恢复测试设置的 runInBackground=false，ProjectSettings.asset 无本轮差异；Editor 保持 stopped。严格 `openspec validate reduce-per-frame-allocations --strict --no-interactive` 通过，`git diff --check` 无问题。本批 5/5；整个 change 为 5/9，4.1／4.2／5.1／5.2 未勾选；未提交、push 或归档。

最后核对将 HudVersion／显示有效标志的提交点移到实际显示成功之后，避免失败的刷新被记为已展示；仅补跑同版本事件／反馈／面板／绑定用例，1/1 Passed、Console 覆盖完整。其余已覆盖的同一行为不重复运行。最终检查 8 份相关 Markdown、129 个本地引用，无缺失或编码问题。

历史规划记录：2026-10-14 先补齐必要规划文件。业务代码、测试、配置与生成物未修改，未执行 UTF、Unity 功能验收或性能采样。全部实施任务保持未勾选。`skip_specs: true` 表示本案纯重构且外部合同不变，不代表跳过功能验证。

## 当前排期

2026-10-14 用户要求划分批次／优先级并暂时不考虑真机测试。已将本案划为必需基线、P1 地图／视口、P1 恢复／正常 HUD、P2 固定步／寻路及必需整体收口，顺序与退出条件唯一查 [tasks.md](tasks.md#批次与优先级)；原九项任务及下方验收映射不变。

历史排期修订当时所有功能与性能仍 NotRun；不因 P2 稍后执行就省掉覆盖。各实施组即做行为验收，最终只补缺口并做一次代表性性能前后对照。真机及配套包体当前暂缓，不影响本案已有 Editor 验收，也不替其他 change 勾选设备或双 Editor 任务。

## 相称的严谨程度

主要风险是投影／HUD 缓存未及时失效、旧按钮操作新绑定、有序遍历因增删而漏执行，以及复用寻路缓冲影响后续路线。后果局部、可定位；保留原 owner、消息入口和独立值边界，使用最小相关 PlayMode 行为 UTF与真实 Launcher/HUD 就足够，不扩大为全仓、跨平台或压测平台。

已有 UI 输入／重开／旧回调、格子移动／容量／伙伴行为用例可以复用用例和夹具；改动覆盖的验收点必须在新实现上补跑，不能引用旧 Passed 代替。恢复协议与配方算法未改变时，可复用各原 change 对应结果；只对本案恢复显示／上下文失效补验。具体选集见下表，执行前仍由 AutoTesting 发现实际模式与 FullName。

性能按同一配置、相同场景与操作做一次预热后的前后比较，区分无变化显示帧、营业更新和失联界面。目标是消除本案明确的重复分配、观察整体 GC.Alloc 降低且帧耗时无明显退化；不设百分比、采样次数或整局零分配门槛。结果接近、反常或有具体回归时才补测；原始输出留原位。

接受的未验证项：本案不验证 Player、Android、物理 LAN、双 Editor、完整快照池化或第三方工具；恢复页总体帧 GC 的稳定降幅尚无证据，已保留两次波动，不扩大为统计测量项目。若出现旧数据写回、物品／角色缺失、路径顺序变化或性能退化，则补相应局部验证；若修复必须改变协议、UI 结构、消息批次或产品合同，先更新／拆分规划，不隐含扩大实施。

## 任务与验收映射

用例名以下表及上方运行记录为依据；原计划中的新增断言已实际发现并执行，各组结果逐项对应。

| 任务 | 功能／交付 | 最小实际验收 | 当前结果 |
|---|---|---|---|
| 1.1 | 当前实现性能基线 | 同一普通 Launcher 场景：无输入显示、营业中的移动／加工／前厅、一次受管失联界面；记录相关方法 GC.Alloc 与帧耗时 | 已完成；用例／输出见上方对应小节 |
| 2.1 | 地图缓冲与内容更新 | 新增 Board 行为 UTF：相同投影重复渲染、路径／高亮变化、同格落物、角色／顾客移除、标签百分比变化、移动插值及暂停；真实入口核对画面 | 已完成；用例／输出见上方对应小节 |
| 2.2 | 视口四角缓冲 | 新增 HUD 行为 UTF：连续查询及布局／尺寸变化后的 viewport 正确；`SoloRestaurantUiLifecycleInputAndProjectionWithSoloBuffDrawReopen` 覆盖实际地图点击／拖动／重开 | 已完成；用例／输出见上方对应小节 |
| 3.1 | 正常 HUD 合并刷新与字段比较 | 新增 UI UTF：同一投影事件与轮询不重建按钮；同版本命令反馈、本地选中／详情／菜谱切换仍更新。复跑 `SoloRestaurantUiInformationHierarchyTargetDetailsAndPrimaryActionUseCurrentProjection` | 已完成；用例／输出见上方对应小节 |
| 3.2 | 恢复界面变化时刷新 | 扩展并运行 `CoopMemberResumeUiRealDisconnectManualButtonReopenAndRetiredInput`：稳定失联帧按钮实例保持，Feedback／Attempt／HUD／Client 变化正确更新，旧尝试回调失效 | 已完成；用例／输出见上方对应小节 |
| 4.1 | 固定步有序遍历复用 | `GridCoreItemRaceDropIdentityAndSameCellPickup`；`SoloCompanionScheduleAskPriorityAndStartedWashIsNotPreempted`；`SoloCompanionScheduleSameKindUsesCreationFifo`；遍历中顾客／工作退出的新增退出／取消行为断言 | 已完成；5/5 Passed，见 4.1 小节 |
| 4.2 | BFS 临时缓冲及四邻 | `GridCoreRetargetFinishesStepAndCancelsOldInteraction`、`GridCoreTwoStandThirdWaitsAndCanCrossFullCell`、`SoloStationTerrainPlayerCompanionAndActualCustomerUseMovedTableTerrain`；新增连续两次搜索的路线独立值和不可达／同格断言 | 已完成；4/4 Passed，见 4.2 小节 |
| 5.1 | 生命周期与真实输入综合检查 | `CoopCoreUiRemoteManagedReopenAndRetiredHostCallbacksKeepTheirOwners`；普通 Launcher 核对移动、高亮、落物、工位／前厅标签、面板切换、暂停、HUD 关闭重开与恢复按钮 | 已完成；4/4 Passed＋纯表现结果复用＋真实画面，见 5.1 小节 |
| 5.2 | 优化收益与文档收口 | 与 1.1 同条件复测；记录本案目标分配是否消除、整体分配与耗时变化、剩余快照等分配，检查文档／链接与严格 OpenSpec 格式 | 已完成；同条件对照、波动限制与收口见 5.2／最终自审 |

所有行为用例位于 `Unity/Assets/Tests/Editor/ET` 的 `CarzyCooker.ET.Tests`；执行模式按当前项目 PlayMode 核对，目录名不代表 EditMode。用例可共同覆盖多个任务，覆盖齐全后不因阶段切换重复跑。任何必需点 Failed／NotRun／Skipped／空集／缺输出都保持未完成。

## 回填要求

实施后在表内填写“实际用例或操作 → 原输出路径 → 结果 → 未覆盖项”，性能只保留简短前后值、来源和接受的剩余分配。编译结果与行为、性能分别记录，不制造额外 JSON 证据包或全量世界快照。

## 历史规划文件检查

2026-10-14：当前 14 个未归档 change 的必要规划产物均齐备；提案声明的 capability 与实际 delta 文件无缺项，所有 change 均有 execution-notes。本案 proposal／design／tasks 为 done，specs 按纯重构声明 skipped，CLI 的 isPlanningComplete 为 true；9 项实施任务仍未勾选。

`openspec validate --changes --strict --json --no-interactive` 退出码 0，14/14 格式有效；本案 skip_specs 产生 INFO，零 delta 被接受。检查当前 change 的 75 份 Markdown 和 223 个本地链接，缺失／编码／尾随空格问题均为 0；本案 9 个任务与验收映射一致，10 个引用的现有方法均在测试源码中存在。此项仅核对源码名称，真实 UTF 发现和执行仍 NotRun。

本轮未改写其他 13 个 change 的既有产物、任务勾选或运行结果，未修改业务代码、执行提交或归档。静态检查不证明优化已实现、功能已通过或性能已改善。

批次／优先级修订后再次严格校验：14/14 change 有效，九项原任务与验收映射仍一致；全部当前 change 文档的 227 个本地链接无缺失。全局排期写入既有 Docs/Product/change-roadmap.md，当前不安排 Player／真机任务，三个原案的双 Editor 待办保持未勾选。本轮仍只有规划修订，没有实际运行。
