# Execution Notes

## 当前受管结构与生命周期（2026-10-12，协议12）

原 UI1101／Prefab 路径／UGF 单实例 owner 保留，实际结构和绑定已生成并验收。当前6/6；五个真实资源／Launcher行为UTF各有Passed，普通Launcher整链与可见检查也已完成，逐点见下文。

主要风险是生成字段失效、重开订阅累加、旧关按钮作用新对象及加载退出留界面。使用原资源、正常CodeBind和真实Launcher运行的UTF，断言所属UI实体、池复用、原请求编号／领域投影、关闭加载无孤儿；不搭UI结构runner或新框架。领域配方、前厅和buff未变结果可复用；本次改变的UI结构／owner／输入与反馈必须实际补验。手机、构建、LAN和最终美术未验证，后续进入该范围再补验。

### 安全Editor与生成

精确项目D:/MyWork/CarzyCooker/Unity、Editor stopped／ready、原Launcher未保存改动=false。Temp/RestaurantUiPrefabWork.cs在隔离PrefabContents中编辑原资源并Unload，不保存打开场景；正常执行CodeBind菜单Refresh All Binding Sources，编译完成后Refresh All Serialized Bindings并保存。新增Phase／Clock／Score／Work文字和Order／Buff／Settlement／Control／CameraDragArea矩形，原Hand／Status／Hint／TargetTemplate／Targets保留，正常生成19个属性，未手改Bind partial。UI.xlsx行为及注册未改，因此无额外表行或导表；源Prefab完整引用由真实资源UTF验证。

MapViewport来自实际CameraDragArea绑定；Board保留完整相机viewport供既有URP overlay，只把厨房投影拟合到中间地图区，并使用该区拒绝非地图按下。拖动和ScreenToCell读取同一当前相机，HUD仍通过实际GraphicRaycaster消费输入。动态按钮统一由原Mono列表管理、ClearTargets解绑／销毁；不同操作分到Control／Order／Buff／厨房滚动区，没有私有资源池。

### owner与实际UTF

GridPresentation仍归ProductClient；打开HUD由该owner等待，关闭通过UIComponent.RemoveComponent而非SetActive受管根。具体操作等待改归当前UIGridKitchen.AsyncOwner：OnClose停止该scope、解除按钮、清空显示引用；物理请求／响应仍由原所属请求helper收尾，共用sender和营业不停止。重开创建新ET owner并由GF复用原Mono；HudLoadEpoch阻止迟到加载挂到已关闭轮次。每个按钮还捕获原UI EntityRef、世界／绑定／关身份，旧轮回调先核对这些值，不能重新解释为当前轮操作。

现有CarzyCooker.ET.Tests增加真实ModelView／HotfixView、UnityEngine.UI和UGF引用；UI用例受UNITY_ET_VIEW／UNITY_EDITOR条件约束，原领域fixture不自动加载view事件。新SoloRestaurantUiTests自己加载真实Launcher，拒绝覆盖活动ET World；只清理该次捕获的Init／GameEntry Root，经原Shutdown收尾。所有用例都实际发现后以PlayMode子串运行。

| 任务／功能 | 实际UTF（ET.SoloRestaurantUiTests） | 当前结果 |
| --- | --- | --- |
| 1.1 真实资源／生成绑定／Canvas与owner | SoloRestaurantUiBindingsRealPrefabHasGeneratedFieldsAndOriginalCanvas；LifecycleInputAndProjectionWithSoloBuffDrawReopen中的真实UGF owner断言 | Passed；必需Text／Panel、模板、ScrollRect、原Canvas和CameraDragArea；实际UI entity归UIComponent、Mono指向该owner |
| 1.2 重开／池复用／一次请求 | SoloRestaurantUiLifecycleInputAndProjectionWithSoloBuffDrawReopen | Passed；真实关闭／重开两轮，原Mono复用、旧按钮订阅无请求，每次PointerClick仅一个业务编号；候选／Seed保持 |
| 1.2 初始真实加载中整个退出 | SoloRestaurantUiLifecycleExitDuringActualInitialLoadDoesNotShowOrphanHud | Passed；实际HudLoading且尚未Ready时退出Product，原Presentation disposed，客户端为空，无活动孤儿Mono |
| 2.2 真实指针与当前坐标 | LifecycleInputAndProjectionWithSoloBuffDrawReopen | Passed；拖动零请求、HUD不穿透、拖后当前坐标、实际Move到目标、厨房完整适配及保存旧callback断言均在最终新运行通过 |

### 投影／输入的最终UTF覆盖

新增强断言的LifecycleInputAndProjectionWithSoloBuffDrawReopen再次实际Passed：初始厨房四角全部位于实际无遮挡CameraDragArea；保存真实UnityEvent runtime callback，关闭重开后直接调用原callback也不增加请求，而当前按钮一点击只发一次。该方法同时验证候选重开、源owner、原Mono池复用和一条实际拿放链，结果供buff复用。

SoloRestaurantUiProjectionRealOrderRejectionPauseAndOldLevelButton实际Passed：源必选buff后开店、真正伙伴询问及独立订单，显示对应桌／顾客／菜名；暂停固定Tick／ClockText／WorkText，恢复继续。原空碗到真实桌操作格点击交付，Accepted时手持不变，最终WrongDish中文反馈、同碗／订单／0分不变。真实顾客超时自然排空到0分0星结算，点击下一关后直接调用保存的旧交付callback不发送，新关候选未选、旧订单空、结算面板关闭。此运行同时补足流程的实际零星自然收尾入口。

SoloRestaurantUiProjectionActualBakingSeparatesReceiptProgressAndCompletion实际Passed：从Launcher经真实拿面包／放烤箱／烘烤按钮，回执Accepted时没有烘烤完成物；后续实际Baking进度与Hint显示，原料没有提前变吐司，暂停Hint／进度冻结，恢复真实Completed后才显示完成并有唯一吐司，0分保持。加工状态不由按钮回执或UI计时伪造。

上述五个独立UI用例各有最终Passed，工具退出0、failed／skipped／inconclusive=0、Console覆盖完整；Bindings和LoadingExit的实现与行为未变，其结果复用。新Projection第一次失败为同一帧旧按钮Destroy尚未生效、扫描出现重复caption；已修正生产ClearTargets为先解绑、立即禁用所属动态按钮再Destroy，避免临时点击阻塞，未SetActive受管root。新Projection和实际Lifecycle／Input随后分别Passed，原失败不改写。未扩大为全仓或复跑未变化领域逻辑；最终C#原生compilationFailed=false，原日志和Pipeline输出保留原位。

首次Bindings用例在Prefab asset上使用不含inactive的GetComponentInParent导致NullReference，原失败保留，改成includeInactive=true后新运行Passed；生产资源字段均已正常绑定。CodeBind初次刷新后的Editor Inspector有一次MissingReference（旧PrefabContents检查器引用），并非运行或编译通过的证明；后续真实资源和运行测试无对应错误，不为此改写用户场景／清Console。recompile期间外层请求数次HTTP400，原生终态随后completed／compilationFailed=false、Editor ready，未重启运行中的测试或假报失败／通过。

## 普通Launcher最终整链与可见验收（任务3.2）

精确工程D:/MyWork/CarzyCooker/Unity，普通Launcher直接Play，协议12，世界`e1df831fed3f478982fc2ad6bebde671`。临时`Unity/Temp/SoloRestaurantAcceptance.cs`通过真实PointerClick／ProcessGridPointer输入，只读取投影，不写Host领域状态。以下六次调用均取得实际终态success=true、diagnostics=[]；长等待同时设置内部timeout_ms=50000和外层timeout=55，没有将观察超时计为通过。

| 实际入口步骤 | 已观察的行为与结果 |
| --- | --- |
| Prepare | 当前候选隐藏／重开保持Seed、列表和编号，实际选择旺火1/2；餐桌移至(3,4)、座位(4,4)，带料台移位旋转，重叠拒绝零写入；真实切配移台保留原工作并接续到60 Tick |
| CookingMoveAndOperatingReject | 真实切配、打蛋、倾倒、开锅；移带锅灶台保留锅`1284040008138765`和批次`1284040196882838`，新锅冻结60 Tick；开店后旧布局按钮WrongPhase；真实熟锅盛入原碗`1284040008138759`，剩1份，尚未出餐0分 |
| ServeAndPlayerWash | 前厅拖动、真实新桌订单`1284040201077246`交付，只得10分，原碗留桌；真实用餐、离席、玩家清桌。原Wash`1284040290206556`玩家做到0.229508，暂停冻结，恢复停手保留0.278689；同一调用直接观察伙伴第二次认领Working并做到0.343205，HUD显示洗碗34%，暂停350ms Tick／进度不变，仍Draining。PartnerObserved=true，ClaimSerial=2 |
| FinishAndContinue | 恢复后原碗实际洗净进入CleanPool，剩余真实订单超时并自然排空，10分1星；实际下一关`1284040008138778`→`1284040789328897`，固定伙伴身份保留、成长6→0、buff／得分重置。真实取回原碗；整个HUD按UIComponent关闭重开，复用同一Mono，世界、原手持碗、命令编号保持。执行24042ms，终态成功 |
| ZeroStarAndContinue | 第二关实际选择后开店，不出餐；真实订单Expired、顾客与前厅自然收尾，结算面板显示0分0星。真实继续至`1284041224488525`，原手持碗落最后提交格(5,6)，新关Preparing／未选buff／成长0。执行44847ms，取得成功终态，不采用早期超时后的间接推断 |
| FreshAfterExit | 整体退出再普通Play，新世界`6889b50f72114619a74c99f535b92fa5`，generation=1、Preparing、command=0、得分／buff／手持／成长0；只有一份初始7物品、2碗1盘，顾客订单为空，不继承旧轮 |

原生capture_game_view(source=screen)实际捕获并查看了`Unity/Assets/Temp/pipeline-screenshots/solo-restaurant-ui-screen.png`、`solo-restaurant-ui-portrait.png`和`solo-restaurant-ui-next-level.png`。540×960真实GameView中，阶段／计时／得分／工作／订单、地图、候选和手持操作均可见；截图只补充表现观察，行为由上述真实输入断言和UI UTF证明。此前screenshot game黑帧限制已通过原生screen入口解决，本轮不再将黑帧当作可见验收。临时GameView选择已恢复原Free Aspect，未改PlayerSettings或Launcher场景。

本局Console session`68cf1167f0ba4ee692e314b87dcdb762`，seq2752–2785连续34条、无reset／dropped、新增Error=0；保留一条LevelCommandRequest 456ms Warning。退出／重开范围seq2786–2822连续37条、Error／Warn=0。当前原生compilationFailed=false、recompile completed／failed=false、Editor ready／Play stopped、UTF completed；历史夹具失败和Inspector错误仍保留，不声称历史日志全绿。当前只改验收文档，不因收口重复未变测试。

接受现有样机Board标签较密和基础美术；手机触控／Player／AOT、LAN／公网、持久存档及最终美术未验证，本轮不报告为已支持。新增输入设备、Canvas结构、owner或业务行为时补相关最小验收。

## 原规划记录（历史，不代表当前状态）

规划检查：2026-10-12，`openspec validate solo-restaurant-ui --strict` 返回有效，退出码 0；proposal／specs／design／tasks 齐全。关联九案共 46 份 Markdown 的本地引用、UTF-8 与行尾空白检查通过。本记录仅证明规划有效，全部新增功能运行仍 NotRun。

仅规划，Prefab 操作、CodeBind／配置生成、编译、UTF 和 Launcher NotRun；当前测试程序集缺 View 引用，新增用例不伪称已可发现。

主要风险是绑定失效、重复订阅、旧目标操作和拖动误触。真实 Prefab 的少量 UI 行为 UTF，加一次 Launcher 完整操作可覆盖；不创建结构注册表／runner、测试框架或截图平台。接受原 HUD／Board 风格和可调拖动阈值；新动态资源类型、相机视角或 owner 结构变化再补相关覆盖。公共所属异步未变结果可复用，新增绑定和输入必须实跑。

| 任务／功能 | 拟 UTF 或真实操作 | 结果 |
| --- | --- | --- |
| 1.1 实际结构 | SoloRestaurantUiBindings：真实 Prefab、生成字段、Canvas／owner | NotRun |
| 1.2 生命周期 | SoloRestaurantUiLifecycle：关闭重开、加载退出、池重用和一次请求 | NotRun |
| 2.1 投影反馈 | SoloRestaurantUiProjection：阶段／菜品／工作／buff，旧关与拒绝 | NotRun |
| 2.2 真实输入 | SoloRestaurantUiInput：按钮／指针、拖动无动作、HUD 捕获及坐标 | NotRun |
| 3.2 可见产品 | Launcher Solo 操作、阶段转换与退出重开 | NotRun |

没有运行 UI 行为就保持该任务未完成；截图仅辅助表现观察。与领域完整链共享同一运行时须确认 UI 实际经过这些入口且实现未再改变。
