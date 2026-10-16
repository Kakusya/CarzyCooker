# 探索与执行说明

## 2026-10-16 实施与真实验收

已接入原装修候选：Host/Solo点击候选先显示只读详情，取消不选定，明确“确认此装修”才经原SendRenovationChoice提交。Client显示“查看装修”并可阅读相同目标/效果，无确认权限。预览只持definition/station/expectedLevel/choiceVersion及原关身份，不写SelectedRenovationId；HUD、关代次、目标或他端选择变化后清理旧预览。

四类奖励分别解释新增工位锚点/数量、解锁定义且不附赠、扩建区域/桌位与整体预检、原工位升级。升级基础耗时直接使用原StationUpgradeConfig.CombinedTicks及生效配方/实际设备能力；不含本关加成，只对安装后新作业比较，旧作业冻结值保持。原候选固定、可继续规则与终关无奖励未改变。

PlayMode实际发现新增3项，分别在原 `EntryRenovationPreview`、`RenovationPreviewLocal` 类别及失败后单项过滤运行，三个不同用例均有最新适用Passed结果；受变原装修用例另跑1/1 Passed。每次终态Console覆盖完整，退出码0；已经通过且未变的KCP和同候选集用例没有再跑。原始即时输出保留 `Unity/Temp/pipeline_test_status.json`，不复制/改写，后续运行自然更新。

| 任务／功能及Scenario | 实际用例／操作与断言 | 结果 |
|---|---|---|
| 1.1 新增预览/取消、升级比较/确认、旧作业冻结 | `EntryRenovationPreviewSoloInstallCancelUpgradeConfirmAndFrozenOldWorkUseOriginalRequests`：真实切配启动并暂停，新增工位预览取消无命令/无安装；升级预览显示3.00→2.25秒；HUD重开旧确认无效，原确认只发一请求；安装后旧作业仍60Tick并完成，有限补货后新作业实际45Tick且新关buff未选 | Passed |
| 1.1/1.2 同一候选集查看升级/扩建，取消后再确认升级Scenario | `EntryRenovationPreviewUpgradeAndExpansionSameCandidateSetCancelThenConfirmUpgrade`：普通Host成长餐厅真实结算；高效灶台与扩建详情分别取消，候选/版本及命令不变；明确确认灶台，下一关原工位升级，取消的扩建未安装，旧扩建确认无效 | Passed |
| 1.1/1.2 解锁/扩建、真实KCP、他端选择使预览失效 | `EntryRenovationPreviewActualKcpReadOnlyUnlockAndExpansionStaySeparateUntilHostConfirmation`：Host先查看/取消两种奖励；真实Client可看扩建而无确认，直接调用原UI sender也不发越权请求；Host原LocalMessage选择解锁后远端旧预览清除，解锁不扩建/补物；原Host UI确认扩建并继续，9宽/第三桌整体安装并投影到KCP，实物集合不补发 | Passed |
| 1.2 原候选/重开/单次安装兼容 | `SoloRenovationUiRealZeroStarChoicesReopenOnceInstallAndOldCallbacks`：原零星候选固定、HUD池化、真实新增台面、继续后单次安装、旧回调均通过；原Command夹具现在沿真实预览/确认两次点击，断言预览零请求 | Passed |
| 2.1 普通Launcher540×960及原流程 | 原主页→单机→内存试玩→确认，选原buff并真实营业/自然收尾后查看升级切配台；预览取消时命令仍2/Selected=0，确认后命令3/Selected=3但工位等级仍0；下一关命令4/等级1；原拿/放/切配后命令7/Executed，新作业45Tick并完成，新关buff=0 | 已实际操作并检查画面 |

画面位于 `Unity/Assets/Temp/entry-renovation-upgrade-preview.png`，实际540×960能读基础耗时与旧作业边界，取消/确认控件可用。截图不代替业务断言；本轮手动Play已按所有权停止。

原终关无奖励与独立复玩沿未变章节规则，复用[章节记录](../2026-10-15-restaurant-chapter-content/execution-notes.md)的核心四组终点及普通UI两模式通过结果；本案不改候选产生或终关配置。原工位说明的几何/生命周期未改变，复用其刚完成结果，新的装修入出口由本案实际补跑。

失败及修正：首轮Solo试对确认后已经关闭的原按钮再次发送Pointer，原生ExecuteEvents返回false；改为一次真实点击后调用捕获的旧回调，验证单请求和旧输入失效。后续Solo在收货台放了半成品导致原接收正确拒绝ReceivingFull，改为放到原空备料台；原收货规则未改，单独重跑后Passed。KCP及同候选集已通过结果继续复用。

风险/投入沿下文局部预览判断：检查取消零写入、真实原请求、固定耗时/实例和两端权限即可，不建立布局模拟器或全配方实验矩阵。真人理解、Player/物理LAN/Android真机均NotRun。下文是建案阶段记录，当前结果以上方为准。

收尾检查：本案5份Markdown、16个本地引用及编码/行尾检查通过，严格校验通过；三个任务逐项按CLI返回行核对后勾选，刷新apply确认3/3、all_done。原18案按实际tasks统计12/18、36/54，不计新增11案的规划任务。

## 2026-10-16 依据与决定

装修候选目前主要是名称和目标等级，点击即固定选择。玩家在选择前难以分清新增工位、解锁、扩建与原工位升级的实际效果。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRenovationSystem.cs)、[主规格](../../../specs/restaurant-renovation/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：复用 RenovationCandidates／UpgradeTargets 和生效定义、原耗时计算 API；预览保存 definitionId／StationId／ExpectedLevel／ChoiceVersion，不写 SelectedRenovationId。确认走 SendRenovationChoice，提交时原权威完整复验；不增加布局模拟器。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

主要风险为预览点击提前固定选择或用错误公式承诺提速，后果是显示误导或操作失效，属于可恢复的局部体验问题。采用原 owner、只读派生和原命令入口，最小相关 PlayMode UTF 加普通入口检查足够，不做全仓回归。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | 新增／解锁／扩建／升级候选、取消／确认／过期、原作业冻结与双端权限；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

本案 proposal／design／specs／tasks 均已齐备，`openspec validate renovation-choice-preview --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
