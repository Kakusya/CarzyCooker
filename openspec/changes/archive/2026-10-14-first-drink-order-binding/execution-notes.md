# 执行说明

## 2026-10-14 当前验收结果

首饮功能验收已完成。新增15个唯一核心 PlayMode UTF及1个真实受管 UI UTF Passed；受影响的既有汤11、KCP2、供应配置1、两菜交付2、原碗完整回收1、终关选档UI1共18个唯一相关用例 Passed。当前协议21，保存v4，主规格的饮品、前厅及首章菜单变体合同已同步。下文进展／失败段保留历史，最新结果以本节及表格为准。

| 任务 | 对应实际证据 | 结果 |
| --- | --- | --- |
| 1.1 配置 | Luban Check／正常生成各5/5、四语言导出；FirstDrinkConfig及供应配置闭包真实初始化 | Passed |
| 2.1 壶与份数 | FirstDrinkRealBatch；汤11项回归；普通命令3–22采购切橙、120 ticks／2份、原壶搬运、分杯 | Passed |
| 3.1 贴单／清理 | FirstDrinkRealBinding／TwoCups／WrongDish；普通29–33有效贴单、对应桌交付及玩家清桌；原两菜与碗回收3项 | Passed |
| 4.1 投影／保存 | FirstDrinkActualFile／Unfinished；真实旧12成员批次文件v1–v3三项及非法字段两项；真实受管UI、终关选档UI及KCP2 | Passed |
| 5.1 普通整链 | 同一普通Launcher世界三关201→202→203、有限采购到交付／清理、45号结束命令返回选档，原生Console Error0 | Passed |

收尾核对补充 `FirstDrinkConfigRejectsChangingDrinkMenuWithinThreeStagesBeforeWorldCreation` Passed，并在最后这次配置校验变更后增量重跑 `FirstDrinkConfigValidatesClosedMenuFiniteCupsAndKeepsLegacyWorld` Passed；保证相同饭菜与饮品列表的合同都经过验证，不接受中途丢掉饮品的配置，实际有效初始化仍正常。其他实现和验收行为没有变化，沿用上述运行结果。

普通运行世界 `ac695d204501462f8688ef25e8e0e816`：1–14采购橙子、收到有限袋并拆份切配／装壶／榨汁，剩余原料和袋按固体规则清理；15–22接收杯袋／拆杯／搬原壶／分一份，留下半壶1份；25–26零星升级原切配台并进入202，厨房、半壶和成品杯原身份承接；29–33把杯1284158857937330绑定有效订单1284159087576702，走到餐桌1交付并耗时清桌，杯与内容消失、10分1星、成功1／未满足5，无洗杯后继；34–35升级原烤箱并进入203；36–42丢弃剩余空杯、空袋、将剩余1份果汁在水池排液并放回原空壶，最终10实物／21工位、两原工位等级1，橙子／杯库存各4包，其余三源各5包，批次0。第三关自然0分0星／未满足6；45号真实按钮结束内存试玩，确认无Host／Client、选档owner可用、7条既有记录仍可见，随后停止Play，Editor ready。

普通脚本首次把多个按钮文字拼成单个前缀，没提交贴单而错过201订单窗口，原杯与半壶保留；修正PowerShell数组括号后在202的有效订单完成上述链，未增加成品、订单或得分。两张原生画面保留在 `Unity/Assets/Temp/pipeline-screenshots/first-drink-cleanup.png`、`first-drink-terminal.png`，实际1469×719尺寸已查看；它们不代表手机或Player。

验证边界：本轮不声称人类理解／再次经营意愿、Android／Player、联机饮品已通过。旧菜单和存档语义有运行证据，已有其他未变能力引用原change结果；相关功能覆盖齐全后停止扩测，不全仓跑。所有失败保留原输出和修正说明，生成物未手改；未提交、未归档、未同步第二工作区。

最终自审：完整性5/5；正确性10个新增／修改Requirements均有实现及对应上述运行证据，旧未修改场景保留；设计一致性符合实际owner树、LocalMessage→HostPeer→HostSession服务步与独立值投影、MenuVariant交接及v4适配。主规格与三个delta对齐，严格格式通过。手写源码／文档diff空白检查通过；Unity Prefab空名称尾空格和Luban生成模板空白保持生成结果，未手改生成物。其他五案已有执行映射沿用，试玩案按用户豁免记录人工Skipped，工程复玩3/3。不执行归档；UI信息层和首章的已同步ADDED在当前CLI报告归档重复提示，后续获归档授权时对齐delta，不影响本轮功能完成。

## 实施进展与失败历史

用户已授权完成七项 change；本案正在实施，不沿用下文历史规划的 NotRun 作为当前结果。原用户文件和存档保持；未提交、未归档、未操作第二工作区。

- 配置源新增首饮有限目录和菜单变体，Luban Check 5/5、正常生成 5/5及四语言导出成功；协议源正常 Proto2CS，当前 21；文件格式 v4，v1–v3 保留只读兼容。新增布局最初形成堵路，三轮 UTF 启动失败（其中一次修复误改朝向字段），已核对实际表头并修复为贴单台 (5,12)、杯台 (1,13)，原 16 个布局不变。最新真实初始化配置测试通过，所有工位操作位可达。
- `FirstDrinkConfigValidatesClosedMenuFiniteCupsAndKeepsLegacyWorld` Passed：源依赖闭包、21 工位／10 初始实物、5 个有限供应来源，原两菜关卡不增饮品。
- `FirstDrinkRealBatchLocksJugAndMachinePausesThenMovesAndConsumesLastPortionOnce` Passed：真实切橙、装壶、榨汁，冻结 120 ticks／2 份；锁壶和设备、暂停不推进、完成后搬原壶、非空杯拒绝不扣份、采购有限杯并盛最后一份，清退原料和批次、重复盛装拒绝。
- `FirstDrinkRealBindingRejectsInvalidRebindAndUnboundServeConsumesCupAndClearsWithoutWash` Passed：未贴单保物拒绝；贴单／显式解绑／失败换绑保留关系；投影独立字段；正确交付消费原杯与内容且一次计分；真实顾客离席、玩家领取清桌并耗时完成，无脏／干净杯和洗杯后继。首轮夹具错误等待伙伴清桌，已改为现有玩家领取合同，没有修改业务放行。
- `FirstDrinkActualFileRoundtripCarriesHalfBatchWithFrozenLockAndNoOldBindings` Passed：实际 v4 文件写入／读取／新运行恢复 202，半壶保持 1/2 份、冻结耗时与批次锁配置，旧 Level 绑定未持久化。夹具先误用无目标的升级选择、随后复用已停止 runtime，均保留失败输出并改用合法升级目标及新的运行实例。

以上共 4 个唯一 PlayMode UTF 已通过，原始 Pipeline 输出保留在 `Unity/Temp/pipeline_test_status.json`（后续运行会更新原位），完整过程在 Unity 原 Editor.log；每轮检查原生 Console 无新增 Error。当前双杯占用／换绑与失效、加工中保存、旧实际批次格式以及普通 Launcher 饮品链仍待验收，tasks 尚未勾选，不能报告整案完成。

### 后续增量（当前仍未完成普通入口）

- `FirstDrinkTwoCupsRejectOccupiedOrderSwapAtomicallyAndReleaseOnDrainAndActualExpiry` Passed：两杯双向占用拒绝、非法换绑保留原关系、内容倒掉后释放、合法原子换绑、真实订单到期释放且饮品保留。
- `FirstDrinkUnfinishedBatchActualFileRestoresFrozenProgressAndPhysicalMachineLock` Passed：真实文件恢复时进度严格相等，120 ticks 保持，机与壶原关联和锁定恢复，继续加工完成后可搬壶。首轮恢复未推进加入步，投影尚未 Confirmed；已等待真实加入／投影，不直改 Confirmed。
- `FirstDrinkLegacyActualTwelveMemberSoupBatchFileRestoresWithoutAddingDrinksOrRewriting(1/2/3)` 3/3 Passed：真实旧12成员批次文件按煮汤／无移动锁恢复，仍只有原3类供应且无饮品物资，原文件字节不变。
- 原 `GridSoup` 11/11 Passed，覆盖加工中原锅可移动、输入锁定、最后份数、本地重放和退出；新增果汁锁不改变汤锅移动合同。
- 新增真实受管 UI 链两次 Failed：菜谱、切橙、榨汁、盛杯均到达，贴单按钮转交遗漏 `ExpectedOrderId`，返回 `OrderEnded`。诊断已具体记录按钮／编号／角色到位状态，正在修正 UI 转交；没有更改订单期限或绕过贴单。

目前9个唯一新增核心 UTF Passed，UI和普通 Launcher 尚未验收；第1项配置已勾选，其余保持未完成。另发现基础菜单采购 Solo 专属来源会索引不存在的供应库存，已增加来源可用性检查并补 Host 拒绝用例，尚待运行。

### UI 转交修复后的有效结果

`FirstDrinkUiRealLauncherRecipePreparationLabelAndOrderDeliveryUseActualButtons` Passed：普通 Launcher 场景实际选内存试玩201；菜谱只读不提交动作，真实按钮切橙／装壶／榨汁／盛杯／贴单／到桌交付，原杯在投影消失、只有一次计分。修复仅补齐受管 UI 的 ExpectedOrderId 转交，没有放宽订单期限或合法位置检查。`FirstDrinkHostRejectsSoloSupplyWithoutMintingMaterialOrDecrementingStock(4/5)` 2/2 Passed，保护未开放内容的 Host 来源库存。

当前新增11个唯一核心 UTF和1个真实 UI UTF均 Passed，普通 Launcher 的采购、清理及三关完整复玩仍 NotRun；不把 UTF 中加载 Launcher 当作普通运行替代。

## 本轮状态与决策

2026-10-13：用户授权“由浅到深写成 change”，并要求真机测试最后。本案目前仅为规划；所有 tasks 未勾选，未实现业务、未执行 UTF、未启动 Unity/Player、未构建或操作第二工作区。第三关保存完成后返回选档、无装修和第四关；橙汁加工锁壶、完成可搬壶分杯、产量/耗时配表，已获用户确认。具体相关合同以本案 specs/design 为准。

## 相称的严谨程度

风险集中在守恒、加工锁定、旧订单绑定和一次性杯收尾。复用现有物品、服务与供应，仅增加新链相应 UTF 加一条普通经营链；保存增加实际往返。 所选验收按下表覆盖本次改变的行为，已有结果只有在实现/行为未变且明确对应该点时才复用，并填写原执行记录路径。不因提案完成或阶段切换重跑全仓；测试失败、空集、Skipped、缺输出均不算成功。

接受的未验证范围：本轮所有运行行为 NotRun；除最后设备层外，手机/独立 Player/LAN 仍待对应案。没有玩家、设备或已授权运行条件时保留相关任务未完成。若改到共享 owner、文件格式、协议、行为断言或出现实际回归，则补对应局部验证，不机械放大为完整矩阵。

## 任务与验收映射

| 任务 | 功能／交付 | 拟使用的最小验收 | 本轮结果 |
| --- | --- | --- | --- |
| 1.1 | 饮品配置 | Check/生成和依赖校验通过，确认初始物资/供应包含合法器具、原料和杯；更新有限目录。 | NotRun（规划阶段） |
| 2.1 | 壶与份数 | 真实消息用例验证原料消费、锁壶/机、完成搬运、最后一份和拒绝保物，记录汤/汁差异。 | NotRun（规划阶段） |
| 3.1 | 贴单和清理 | 真实请求用例验证双向占用、原子重绑、过期/错单、一次计分和杯消耗/清桌，更新前厅规则。 | NotRun（规划阶段） |
| 4.1 | 投影与保存 | 独立投影/实际文件往返测试保留份数与冻结加工，旧格式兼容且下一关无旧绑定，更新格式说明。 | NotRun（规划阶段） |
| 5.1 | 首饮集成 | 普通 Launcher 走采购到清桌全链并跨关，补相关消息用例，不能仅直改物品过关。 | NotRun（规划阶段） |

UTF 执行阶段使用现有 Tools/AutoTesting，先发现模式与方法再记录实际过滤范围；涉及产品消息/UI 的候选集按 PlayMode 核对，目录名不能证明模式。原始输出保留原位，只在此引用运行路径及功能对应关系。纯正文任务只检查文本、链接与严格格式，不制造占位 UTF。

## 实施时回填

每项记录“实际方法或真实操作 → 原输出路径 → Passed/Failed/NotRun/Skipped → 未覆盖点”。文档格式验证只证明规划可解析，不证明功能已完成。当前不预填执行日期、通过计数或不存在的测试结果。

## 本轮规划检查

2026-10-13：`openspec validate first-drink-order-binding --strict --no-interactive` 退出码 0，规划格式通过。当前相关文档检查共 80 份 Markdown，链接缺失 0、编码/尾随空格/原规格场景遗漏 0；新案依赖无环。以上均为静态规划检查，表内功能仍全部 NotRun，任务未勾选。

## 2026-10-14 归档核对

用户已授权归档、提交并推送。任务均已闭环；复用上文适用于当前实现的 UTF 与普通入口运行结果，本轮归档未改变功能，不重复运行产品测试。主规格已同步，七案共33项增量要求核对通过，34份主规格严格验证通过。

restaurant-chapters 按三关案先、首饮案后的语义合并：保留两道饭菜、首饮菜单变体及旧档原菜单／布局／库存；主规格已有的 ADDED 要求不重复添加。
