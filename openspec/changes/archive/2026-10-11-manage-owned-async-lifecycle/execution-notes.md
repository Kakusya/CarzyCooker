# 执行说明

## 严谨程度与验证取舍

主要风险是公共 owner 退出影响其他对象、取消后仍写状态、未消费任务不清理、池复用使旧结果误匹配、原生资源重复释放，以及退出依赖已停止的更新循环。采用可控完成时机的小集测试加一条真实 Unity 资源路径，先把测试条件定清再施工。已有本地 sender 与产品会话结果仅复用原机制证明，不当作新异步接口验证，也不重复跑全量套件。

明确接受调用者必须使用受管启动、等待和写回；任意直接字段写入、绕过入口或强制中止代码不作保证。业务等待立即结束与原生操作实际回收分开记录；整体退出不等待加载成功，尚未终结的原生句柄不能冒充已释放。若真实原生退出出现异常、残留、阻塞或继续调用旧 manager，针对该路径补验并修订具体适配，不能用模拟通过覆盖。

## 已确认决定

- 2026-10-11 本轮：用户明确要求开始 apply；源码实施、任务规定的小集 UTF、主工程 Editor 启动及必要微型测试资源已授权。提交、推送、sync 与归档仍未授权。

- 2026-10-11：用户要求两个 change，第二项先确定测试条件，再施工。
- 资源加载之外也管理普通异步工作，包括延时和请求等待。
- 采用统一托管调用，公共基类自动接入，迁移本轮相关旧路径；不是可选 token 接口。
- 对象关闭及正常运行退出均立即结束业务等待，不依赖下一帧或加载结束。
- 规划阶段未启动 Editor/测试；本轮 apply 已授权必要实施与验证。提交、发布及归档不在授权内。

## 实施前静态证据

ResourceContainer 的版本只在 Create 增加；失败/进度/依赖回调没有轮次检查。Awaitable.ResourceComponent 先等待 IsFinished 再检查 token，且 GetResult 才触发原 ReturnAction。ADynamicUIWidgetLoader 和 UGFComponent 直接调用底层资源等待。GameEntry/Base/ResourceManager 的退出会清除队列和解绑 agent 回调，所以只等底层回调无法覆盖整体退出。

详细条件见 [test-conditions.md](test-conditions.md)；源码和退出树见 [design.md](design.md)。以上是静态核对，不是故障复现或原生清理证明。

## 规划阶段验证状态（历史）

当时规划产物及测试条件齐全，11 项实施任务均未开始。`openspec validate manage-owned-async-lifecycle --strict`：Passed。两个新 change 共 17 份 Markdown、48 个本地引用的存在性/编码/空白检查：Passed；当时工作区仅新增这两个 change，无已跟踪文件改动。

Unity 编译、UTF、新接口、真实资源关闭/停止、普通任务及 RPC 适配：NotRun。格式通过不表示行为验收通过。Player/AOT、物理 LAN、强制杀进程和全仓异步迁移不在本轮验收内。

后续执行只保存简短结果及原输出路径，原始输出留在原位，不建立 JSON 证据包、完整状态副本或 hash 清单。

## 实施中的测试先行记录

先加入未实现 Stop 的可编译核心骨架与受控资源测试声明。首次编译因测试程序集缺少 GameFramework 引用失败，修正引用后 Unity recompile 为零错误；这次编译失败不计行为证据。

核心基线：EditMode，filter `CarzyCooker.Tests.OwnedAsyncLifecycleTests`，9 项中 2 Passed / 7 Failed。失败断言包括 Stop 后仍 Pending、旧 Apply 仍写回、关闭续体未触发、运行退出未失效。骨架失败用于锁定新增契约，不冒充存量实现的故障复现。资源声明尚未实施，未用 NotImplementedException 计行为失败。

原始即时作业输出留在 `Unity/Temp/pipeline_test_status.json`（Pipeline 后续运行会按自身规则覆盖），原 Editor 日志为 `%LOCALAPPDATA%/Unity/Editor/Editor.log`；本会话工具输出保留完整失败消息/项目栈，不搬运或改写原输出。

## 本轮实现及必要修正

Game 新增 IAsyncOwner/AsyncRuntime/AsyncScope/AsyncContext，登记在委托启动前完成；外部完成记录不池化，内部唯一观察者消费结果。停止先失效/分离原轮次，再直接提交取消；Apply 保持原执行上下文，错误在入口统一观察。资源仍使用原 ResourceContainer/ReferencePool，原 backend 成对捕获加载和释放。UnloadAllAssets/Clear 隔离旧资源轮次，成功引用登记和取消注册清理不依赖 GetResult。

AExUIForm、AUIWidget 和共同 AEntity 层接入，AExEntity 与 ETMonoUGFEntity 继承 view scope。动态 Widget 绑定真实父 scope，关闭时移除实例并释放资源，重新 Open 可再加载；原 GetComponentInParent 会匹配加载出来的 Widget 自身，已改从父 Transform 查找。Close/Hide 先移开原 ResourceContainer，并用捕获的 exitedScope 完成基类收尾，避免重入新请求被旧关闭二次清理。

ET AsyncOwnerComponent 经 Component API 创建，用原 EntityRef/InstanceId 和捕获的 Init scope 限定生命周期。Unit/LSUnit 视图创建走实际 owner/Apply；LSUnit 单独核对 EntityRef。CodeLoader 保留 World 检查；ConfigReader/Recast/UGFHelper/Tables/HybridCLR 转为所属加载，独立值读取通过 ReadAssetAsync 收尾；AOT 返回码、表源和生成代码未改。

Session 适配复用原等待表和传输；RpcInfo 改为不复用的记录，在移除等待和取消注册后通知，ERR_Cancel 归取消，值转换后释放请求/响应。本地 Call 仅立即结束业务等待，内部观察者沿原答复/40秒超时消费响应，不 Stop 共用 sender。Ping 是本次普通 ET 延时/Session 消费者；既有产品会话未全量改写。

原 AutoResetUniTaskCompletionSourcePlus 的同步 GetResult 会先回收再执行取消动作；这会丢 Timer 的 Remove/取消注册。泛型与非泛型源现将回收到完成通知和原动作全部返回后，并捕获原回调。补验目的限定为 Timer 登记实际移除及原发送器真实超时保留，不为本 change 重跑全部历史 sender/KCP 套件。

DefaultLoadResourceAgentHelper 将未转交的创建请求交给自身 completed 尾部；原 manager 已拥有的 bundle/依赖经 DefaultResourceHelper 唯一释放，资产读取未结束时保留句柄并暂缓卸载。UnityWebRequest 明确 Abort/Dispose。原生退役不调用 GameEntry、agent 或池，业务退出不等这一尾部。GF Base/GameEntry 的中性预拆卸通知捕获原 AsyncRuntime；Init 在 World.Dispose 前停止自己的 scope。

## 实际终态与复用范围

| 验证 | 模式 / filter | 终态 |
| --- | --- | --- |
| 新核心/资源受控小集 | EditMode / `CarzyCooker.Tests.Owned`（当时16项） | Passed 16/16 |
| ET 实例、Session 两 owner、本地 Call、Timer 延时 | PlayMode / `ET.LocalMessageShutdownTests.Owned` | Passed 4/4 |
| 公共 Widget/普通延时/真实原生 | EditMode / category `OwnedIntegration`（当时3项） | Passed 3/3 |
| GF Hide、Base 预拆卸、真实失败/类型失败 | EditMode / category `OwnedRemaining` | Passed 3/3 |
| Timer 物理登记断言补验 | PlayMode / `ET.LocalMessageShutdownTests.OwnedETDelay` | Passed 1/1（复用其他3项 ET 结果） |
| 原发送器两种真实超时 | PlayMode / `ET.LocalMessageShutdownTests.RealTimerKeepsBothTimeoutModes` | Passed 1/1 |
| 公共 Widget 关闭重入/再次关闭 | EditMode / category `OwnedFinal` | 初次 Failed 1/1：最后的 EditMode DestroyImmediate 清理未触发临时 Mono 的 OnDestroy，释放数1而非2；改用公共 OnClose 收尾后，新运行 Passed 1/1 |
| Unity `recompile` | 主工程 `D:/MyWork/CarzyCooker/Unity` | Passed，最终零错误/警告、compilationFailed=false |
| DotNet 共享编译 | `dotnet build DotNet/Model/DotNet.Model.csproj --no-restore -v minimal` | Passed，零错误/警告，7.40秒 |
| OpenSpec | `openspec validate manage-owned-async-lifecycle --strict` | Passed |
| 文件空白 | `git diff --check` | Passed，仅现有 CRLF 提示 |

27 个新增不同用例各有 Passed 证据，另补1个历史计时用例；未把同一用例的补验重复计数。范围依次为9核心、9资源、4 Mono、1原生、4 ET。所有正式成功运行的 AutoTesting Console 覆盖完整，无新 Error；没有空集或 Skipped 冒充通过。最后只修测试夹具的 ReferencePool 归还与编译，不因此重跑套件。

真实原生用例复用 `Assets/Res/ET/ClientServer/Luban/dtaiconfig.bytes`，微型 bundle 在 `Unity/Temp/OwnedAsyncLifecycle/<本次随机目录>/owned`。先断言创建请求真实 isDone=false，关闭 owner/Reset 后不再推进业务更新，原生完成后未转交 bundle 已卸载、句柄登记归零。第二段在资产读取真实 isDone=false 时 Stop 运行和 backend，旧 manager 的 bundle 保留到读取完成才卸载；没有迟到业务写回或访问死 backend。下一运行重新加载并正常释放成功。这里使用生产 DefaultLoadResourceAgentHelper/DefaultResourceHelper，测试 backend 只连接测试 bundle 与通知；受控共享引用小集证明请求计数，原生段证明真实创建/读取/卸载，两种证据不互相冒充。

最新 Pipeline 原生结果仍在 `Unity/Temp/pipeline_test_status.json`；Editor 原始日志仍在 `%LOCALAPPDATA%/Unity/Editor/Editor.log`。每次执行的完整 stdout、失败消息和项目栈保留在本会话工具输出；未创建证据 JSON、hash、状态副本或新 runner。

## 最小自审与接受的 NotRun

| 维度 | 核对结果 |
| --- | --- |
| 完整性 | 实施与必要验收完成；12项 ADDED/MODIFIED Requirement 有实现，没有 REMOVED/RENAMED；收尾任务更新后11/11 |
| 正确性 | T1–T9 核心/代表入口有 Passed，全部场景有代码映射；具体动态 Widget Prefab、完整应用退出及未启用启动模式的动态覆盖为 Not verified，详见下文 NotRun |
| 一致性 | owner 树、消息输入/响应归属、依赖方向与规划一致；实际 AEntity 共同 view 层、Session 不复用记录及计时等待源收尾修正在上文说明 |

未发现剩余必修实现问题。场景动态覆盖有明确接受边界，不能据此宣称全场景或归档前的全部验证已通过；未执行归档。

完整性：11 项任务的指定入口已落地，5份 delta specs 共12项 ADDED/MODIFIED Requirement 均可映射到实现。统一启动/终态/Apply/观察/退出/隔离对应 AsyncScope 和核心小集；资源归属与原生尾部对应 ResourceContainer、资源 await、helper 和原生用例；UI/Entity 对应公共基类、ET 适配、Mono/ET 用例；网络对应 OwnedRequestExtensions/Session 及4项 ET 代表。无第二 RPC 表、调度器、资源池或业务状态。

采用本文已有结果，不在文档整理或收口阶段重新全跑。保留旧 World 身份检查，迟到成功先判归属后判类型，清理引用捕获原 backend，原消息发送/接收与 sender.Stop 不变。真实资源测试只观察本次微型 bundle 的两种进行中请求；完整产品资源集合、复杂依赖图、每个 Prefab 的动态 Widget 展示、默认关闭的 CodeBytes 模式、Player/AOT、物理 LAN、完整应用退出及强杀均 NotRun。启动加载的取消通过相同所属加载/运行停止路径与静态直接消费者核对证明，不宣称已在全部启动模式实跑。公共 Mono 的 Close/Hide 路由由真实基类方法调用证明；实际 Play 的各 Mono 销毁顺序不逐个重复矩阵。

如以后出现某个 Prefab 的绑定/重开失败、复杂资源依赖提前卸载、原生句柄残留、旧 manager 访问或默认未启用启动模式的晚到写回，针对该路径补验。强杀/断电及任意直接字段写入不属本契约。旧 baseline/夹具失败如实保留，不能将其改成通过。提交、push、同步主规格与归档未执行。

## 2026-10-11 归档与提交授权

用户明确要求归档、commit 和 push。本轮复用上文已有通过证据，不因归档重跑 Unity/UTF/产品构建。已同步 owned-async-lifecycle 新主规格，以及 entity/network/resource-management/ui 四份既有规格，共新增11项 Requirement、修改1项，保留原 Purpose 和未涉及场景。

变更已正常归档至 `openspec/changes/archive/2026-10-11-manage-owned-async-lifecycle/`，外部链接和归档内的相对引用同步修正。全量严格格式检查与相关本地链接检查在提交前执行；动态 NotRun 范围保持上文事实。提交范围为本 change 的实现、测试、相关文档、主规格和归档产物；工作区的 URP 预过滤标记另有修改，保留未纳入提交。