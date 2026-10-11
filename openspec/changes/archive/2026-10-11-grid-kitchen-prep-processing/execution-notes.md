# 执行说明

## 本轮状态与授权

2026-10-11 后续用户明确“开始 apply”，已授权本 change 实施及相关最小 UTF、普通 Launcher Solo 和同 Editor loopback KCP 验收；该阶段没有提交、push、主规格同步、归档或第二工作区维护授权。随后用户在获知双进程验收未运行后明确要求“先归档，然后 commit，再然后 push”，本次收尾按此顺序执行，并同步本 change 主规格；第二工作区维护仍不在本次范围。

2026-10-11 规划阶段：用户在下一步探索后要求“形成 change”，并明确测试只在一台电脑上进行。当时仅创建规划产物；后续 apply 授权及结果分别记录，保留规划阶段的验证边界。

change 收敛为第一轮“备料与手工加工”：番茄切块、鸡蛋放入碗、手持碗打蛋、暂停接续和半成品交接。后续煮汤、盛装、订单、顾客及洗碗分开推进。本轮验收采用同一电脑上的 Solo 与真实 loopback KCP Host／Client；一个 Editor 内使用独立 Fiber／独立投影即可，不要求第二工作区或第二台设备。

## 严谨程度与验证取舍

主要风险是材料同时留在手中和碗中、加工重复产物、移动或失联后旧工作者继续推进，以及 UI 把“开始成功”显示为“加工完成”。这些是局部玩法错误，可定位并重启临时样机恢复。采用现有 Host Tick 单写者、动作完整预检和所属树；少量 UTF 覆盖身份／拒绝、停续／认领、真实消息／投影，再各做一次普通 Launcher 和本机 KCP 的真实操作，足以覆盖本轮功能。

Excel 涉及文件覆盖，继续按 Luban SOP 保护原工作簿、核对注册与实际输出，Check 失败即停止；不能因玩法易恢复而降低源文件保护。复用既有会话和异步生命周期证据。由于容器表示、默认动作和协议本轮会改变，旧 GridCore／GridWire 不能整体直接复用；只重跑实际受影响的相关用例，不跑全仓套件。

接受手机触控、Player／AOT、双 Editor、LAN／公网、正式存档、全局暂停队列、长期运行及极端消息排列未验证。本 change 选择同 Editor 独立 Fiber 的单电脑验收；单电脑也可以运行两个独立程序，但本轮未覆盖此方式。出现输入失效、重复／丢失物品、旧绑定继续推进或明显卡顿时，补对应局部检查；新增双进程或跨设备要求时补对应验收。不给本轮增加新测试框架、专用双包工具或穷举矩阵。

## 已确认约束与样机参数

| 项目 | 来源与处理 |
| --- | --- |
| 测试环境 | 用户明确：一台电脑；Solo + 同机独立 Fiber 的真实 loopback KCP |
| 手工输入 | 已确认产品规则：点击开始，停止／移动中断并保留进度，单一执行者，队友接续 |
| 空间与退出 | 复用四邻操作、自动接近、当前一步完成后改目标；断线最后提交格落物 |
| 本轮大小 | 第一轮备料加工；不把完整汤、关卡或存档纳入同一 change |
| 工程初值 | 碗容量 1，接受鸡蛋／蛋液；切配和打蛋各 60 Tick，沿用 50ms Tick；均为可调样机值，不是正式平衡 |
| 初始物资 | 固定地图各一份番茄、鸡蛋及空干净碗；现有空台面改为切配台；用重启样机补充初始物资，不实施供应系统 |

## 功能验收映射

下表 UTF 均来自实际 PlayMode 发现并执行；完整用例名为 `ET.ProductSessionTests.` 加表中名字。相关用例共 12 个，各自有 Passed 结果，局部修复／增补后只重跑对应项；不把第一轮的 11/12 改写成整轮全通过。

| 任务／功能 | UTF 或实际操作 | 当前结果 |
| --- | --- | --- |
| 1.1–1.2：定义、生成与启动加载 | 源表回读、五目标 Check／导出；`GridPrepContainerDefinitionsAndUniqueContents` 验有效／必需物品、配方缺失明确失败；普通 Launcher 加载实际 Tables | Passed |
| 2.1、2.3：容器身份、添加与拒绝 | `GridPrepContainerDefinitionsAndUniqueContents`：源槽清空、容器关系、地图 owner、Participant 独立及带料交接；`GridPrepContainerRejectionsDoNotTransfer`：错误原料／满碗／旧目标／套容器拒绝保持原状态，重复添加只一次 | Passed |
| 2.2、3.3：消息与独立投影 | `GridPrepWireLocalKcpDuplicateAndIndependentVersionedGraph`：真实 KCP／本地、同版本进度内容一致、独立复制、重复开始不重置／多产物、版本 2 Join／Command 拒绝；旧 GridWire 覆盖旧世界／绑定、DTO 独立及内部 wire 关闭单端 | Passed |
| 3.1：切配停续、争抢与结果 | `GridPrepWorkChoppingStopsResumesMovesAndProducesOnce`：开始 Tick 不推进、以后固定推进、重复／双人认领拒绝、非法入口不停止原工作、显式停／合法移动暂停、材料换台续作、旧工作者停止拒绝、新番茄块身份且仅一份 | Passed（补单工作者前置拒绝与双人断言后单独复验） |
| 3.2：手持打蛋与退出 | `GridPrepWorkHeldBowlHandoffDisconnectLeaveAndNewWorld`：持碗停续、换人保留进度、失联释放／内容随碗落物、重复收尾、旧绑定拒绝、同格拾取续作、同碗新蛋液身份；Leave 移除角色；整体停止释放未完成作业，新世界无旧 Process／成果 | Passed（首轮失败修复后复验；补未完成作业整体停止断言后再单独复验） |
| 4.1–4.2：普通入口、显示与输入 | 540×960 普通 Launcher Solo，真实 Button PointerClick、Board 坐标经 ProcessGridPointer：两条备料链、加工／暂停／完成、暂停材料交接、碗内容、关闭重开后旧按钮销毁／新按钮单次提交、HUD 指针不穿透；第二轮普通 Play 世界 `c08388129ae245d1bed60e008f4f2af5` 独立 | Passed；画面 `Unity/Temp/pipeline-screenshots/grid-prep-solo-frame.png` 已查看 |
| 2.4、5.1：旧行为与最小集 | `GridCoreMoveCompletesOnArrivalAndCancelsReplacedMove`、`GridCoreRetargetFinishesStepAndCancelsOldInteraction`、`GridCoreTwoStandThirdWaitsAndCanCrossFullCell`、`GridCoreItemRaceDropIdentityAndSameCellPickup`，以及三项 GridWire（SoloAndLocal、KcpSharesItemsDrops、ProjectionCopiesDto）；全部 4+3 Passed，容器旧断言改为真实初始空碗 | Passed，复用第一轮各项结果 |
| 5.2：同机真实合作 | 同 Editor 独立 Main Host／Client Fiber，Host 实际 UDP-KCP `127.0.0.1:53156`；依次通过两端受管 HUD 续切配、交番茄块、鸡蛋入碗、打蛋停续交接；同版本内容和进度一致且集合独立；本地成员退出后远端同格拾碗并完成；明确套容器拒绝；只移除临时 Client Fiber | Passed；画面 `Unity/Temp/pipeline-screenshots/grid-prep-kcp-frame.png` 已查看，停止 Play 后监听释放 |
| 5.3：文档与规格 | 配置／网络／UI 用法、产品目标状态、当前进度和 AGENTS 同步；本 change 严格校验与手写文件引用／正文／局部 diff 检查 | Passed；不代替以上运行结果 |

原始运行输出保留原目录；实施时只补真实结果、失败原因和原日志路径，不保存全量业务状态、额外 JSON 证据包或 hash 清单。UTF Passed、编译通过和真实入口操作分别记录。

## 发现的文档差异

部分根 AGENTS 现状文字仍未体现临时格子样机及最新断线裁决，产品目标规格索引的“所有业务仍未实施”表述也过宽。本轮以实际 GridKitchen 源码、主规格和样机归档记录为依据；实施收尾只同步涉及本轮能力的状态说明，不重写历史归档，也不把正式餐厅／完整产品列为已实施。

## 本轮规划校验

- `openspec validate grid-kitchen-prep-processing --strict`：Passed，退出码 0。
- 六份 Markdown 的正文编码、尾空白和 15 个相对链接检查：Passed；三个 MODIFIED Requirement 的原 Scenario 名称均保留。
- 规划产物齐备：proposal、design、两份 delta、tasks 和本说明；新增 9 项需求、修改 3 项需求，共 27 个 Scenario；14 项实施任务全部未勾选。
- 本轮仅生成上述规划文件；原用户 URP asset 修改保留，未修改业务代码、表源、生成物或主规格。所有功能验收仍为 NotRun，规划校验不代表功能通过。

## Apply 运行记录

- 主 Editor 按完整 projectPath 发现：`D:/MyWork/CarzyCooker/Unity`，本轮启动 pid 101624，Pipeline 实际端口 7800；该端口只用于编辑器控制。产品测试 Host 使用动态 loopback UDP-KCP endpoint。
- 源表保护副本：`Unity/Temp/gridprep-original-tables.xlsx`；回读确认注册工作簿只有目标 sheet 改变，其他 ZIP 条目保留。两张新表 c,s,e 注册；五目标 Check 5/5 及正式导出 5/5 Passed，生成 JSON 回读显示五物品、空碗及两条 60 Tick 配方。
- Proto2CS 正常生成 Passed；新 Process DTO 追加末尾，协议为 3。最初分步编译的类型切换／测试 LubanLib 引用及测试缺少 UniTask using、IPEndPoint.Parse 错误均已修正；实际 `recompile_status` 最终 completed、failed=false、compilationFailed=false。历史 Console 未清除。
- 第一轮 PlayMode 过滤 `ET.ProductSessionTests.Grid`，实际发现并执行 12 用例：11 Passed，1 Failed。通过包括 4 GridCore、3 GridWire、2 GridPrepContainer、1 GridPrepWire、切配 Work。失败为打蛋 Work 在 CloseEntry 的 Mailbox 尚未提交清理时读取碗位置，实际 (3,8)、预期 (3,9)，`GridPrepTests.cs:144`；改为泵消息后检查，并补新轮上下文 Reset。此失败保留记录，随后单独新运行复验。
- 原始 Pipeline 输出保持 `Unity/Temp/pipeline_test_status.json`，Editor 日志实际路径由 Application.consoleLogPath 确认为 `C:/Users/Administrator/AppData/Local/Unity/Editor/Editor.log`；未复制证据包、未改写失败结果。工具正常新运行会写当前原生结果文件。

- 后续局部 PlayMode：打蛋 Work 修正后 1/1 Passed；切配 Work 增补双人认领与在改路线前拒绝后 1/1 Passed；打蛋 Work 增补未完成作业 Stop／释放后 1/1 Passed；Wire 增补版本 2 Join 拒绝后 1/1 Passed。一次把两个全名用 `|` 拼接的过滤没有匹配，NotRun，未启动作业；已改为各自真实全名单独运行，不算一次通过。
- 普通 Solo 实操第一次完成两条链后，在重开 Ready 但按钮尚未生成的帧查找按钮而失败；验收脚本等待真实按钮出现后，在新的普通 Play 完整复验 Passed，旧按钮已实际销毁。这是临时验收脚本等待条件修正，未改写业务世界。
- 实操脚本在 `Unity/Temp/GridPrepSoloAcceptance.cs`／`GridPrepKcpAcceptance.cs`，由 Pipeline `run_script` 在内存执行；Button 使用 Unity PointerClick，场景走实际 Camera 坐标及 ProcessGridPointer，未直接写材料或进度完成链。KCP 临时端沿现有测试 fixture 的 Fiber／NetComponent／Session 建树方式挂入主线程调度，仅创建测试端，不新增生产入口或工具框架；UGF 唯一表单依次在两端 owner 关闭／打开，不添加双画面产品 UI。
- KCP 实操返回 Passed；Client Fiber 92011 已通过 FiberManager.Remove 移除，主 Host 后续正常停止 Play，实际 UDP 53156 不再监听。最后 Console groundTruth compilationFailed=false、consoleErrors=0；Pipeline 保留分步编译的六条历史错误与运行 warning，未清 Console，不称历史错误为零。
- 生成器在无关 Admin／Server 消息上只产生尾空格差异；已恢复这两份原文件，其余新生成 DT／DR／数据保持正常导出结果。未改用户 URP、`Assets/_Recovery` 或并行出现的 `grid-kitchen-soup-batch-cooking` change。

- 容器拒绝用例增补真实消息重放添加，最新单项 1/1 Passed，碗内容仍仅一个原鸡蛋、手槽为空。此后没有改变运行行为，不重复整组 UTF。
- 原 Editor.log 的 GridReady 回读确认普通 Solo 第一轮世界 `fd16811464b24e1392b34ae096e97d7c`、第二轮 `c08388129ae245d1bed60e008f4f2af5`；KCP 两端世界均 `8bf9e89d2c8d4c5b87ac9d27c248813f`，成员 1／2。实际新轮隔离与合作一致性均有运行证据。
- 最终静态检查：16 份本轮手写 Markdown UTF-8／尾空白及 266 条相对文件引用通过，两份新手写 C# 检查通过；两模式已有 opcode 全部保持，新 DTO 追加 10072 无重复；两张源表回读通过。生成 DT／DR 保持既有模板格式，未手工修生成物。
- `git diff --check` 和 `openspec validate grid-kitchen-prep-processing --strict` Passed。最终 Editor ready、Play stopped，recompile_status completed／failed=false／compilationFailed=false，当前 Console groundTruth Error=0、Warning=0；历史捕获错误仍保留。主工程保持 Launcher 场景，可直接 Play。
- 14 项任务均已有实现及适用运行／文档证据；未做主规格 sync、归档、提交、push、第二工作区同步或跨设备验收。

## 归档核对与验收边界

- 用户追问真实 Host／Client 后已明确说明：本轮没有运行两个独立 Editor／Player。Host 从普通 Launcher 启动，Client 由临时验收脚本建立独立 Fiber／Session／投影，实际通过本机 UDP-KCP 连接。合作实操的退出步骤是关闭 Host 本地成员的可信 HostPeer 入口，再由远端拾碗续作；没有通过终止独立 Client 进程模拟断线。历史双 Editor 会话验收不替代本次加工玩法的双进程验收。
- 用户获知上述边界后要求归档、提交及推送；保留双进程启动、独立 Client 进程断线恢复为 NotRun，不将其报告为已验收。任务 5.2 的通过仅对应其明写的同 Editor 两端范围。
- 归档自审复用上表逐功能运行结果，不重跑未改变行为的 UTF。14/14 任务、12 项 ADDED／MODIFIED 需求均找到实现及对应验收；地图 owner／固定 Tick、Peer→HostSession 裁决、独立投影和受管 UI 与设计一致。27 个场景按上表对应 UTF／实操核对；没有新增代码或扩大测试范围。
- 已同步：grid-kitchen 保留 Purpose 和未涉及需求，合并 3 项 MODIFIED；新增 kitchen-item-processing 主规格，仅含本轮 9 项 ADDED 及原 Purpose。12 项 delta 与主规格逐块一致；22 份主规格 `openspec validate --specs --strict` 全部通过。本 change 在移动前的严格校验通过，已归档至 `2026-10-11-grid-kitchen-prep-processing`，外部文档及归档内相对引用随目录深度调整。原始日志、Temp、Recovery、用户 URP 修改及并行 soup change 不纳入提交。
- 归档后 19 份相关 Markdown 的 UTF-8／尾空白与 310 条相对文件引用通过，归档元数据保留；主规格与归档 delta 再次逐块一致。提交范围限定为本 change 的 82 个文件。暂存全部新增生成物后的完整 `git diff --cached --check` 有格式提示：6 份 Unity 自动 `.meta` 的空值尾空格、6 份 Luban Client／ClientServer／Editor DR 的模板尾空格／末尾空行；手写文件检查通过。保留自动生成格式，不手改生成物，也不把完整暂存检查报告为通过。
