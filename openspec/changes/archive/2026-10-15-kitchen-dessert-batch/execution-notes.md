# Execution Notes

## 当前交付：4/4，13个不同UTF最新有效结果Passed

00–13依次完成，本案不提交、push或归档。下文早期规划/NotRun段落保留创建时事实，当前任务结果以本节及实际运行映射为准。文件v11/wire33；两个内容大关数量及旧档Level/Next保持。

| 任务／功能 | 实际功能验收 | 结果 |
| --- | --- | --- |
| 1.1 三原料/载具/闭包 | Check/Gen五目标；DessertActualConfiguration…；全布局通路与缺设备拒绝 | Passed |
| 2.1 有限批次/锁定/脱模 | DessertActualLocalAndKcp…两模式；KcpLastPortionRace…；KcpCannotServe…实际订单拒绝 | 4/4 Passed |
| 3.1 供应/菜谱/图形/文件 | DessertFiles三项；下述Solo/Coop普通UI两项；全51物品/52工位方向受管渲染 | Passed |
| 4.1 普通Launcher全链 | DessertUiSoloActualThreeIngredientsBakeUnmoldDeliverySameMoldReuseAndPlateWash；DessertUiCooperativeActualHostMixAndKcpMoldBakeUnmoldDeliveryReuseAndWash | 2/2 Passed |

普通界面组`Unity/Temp/Dessert-UI-UTF.log` 2/2 Passed、0Skipped，266.56秒：Solo130.06秒/真实KCP合作136.33秒，各自经原模式/房间进入甜品菜单，实际采购预拌粉/牛奶、鸡蛋投入、空包投垃圾、只读三原料/两份菜谱、搅拌、装模、烘烤、脱模，原模具再装最后一份并第二次烤熟。真实订单交付10分，玩家清桌/伙伴洗净原甜品盘、营业自然收口；合作实际Host混料后由独立KCP成员装模/烤制/脱模/交付。无直接填成果或代改分数。

三项受变回归：`Dessert-regression-art-ready-UTF.log`全51独立物品图形/52工位方向及分层；`Dessert-regression-fry-ready-UTF.log` HotFoodRealLocalAndKcpFryingLocksOneInputPausesAndCompletesOnceAlongsideOriginalOven(True)；`Dessert-regression-file-UTF.log` HotFoodActualSoloAndClaimFilesKeepPartialBreakfastAndUnfinishedFrozenFrying(False)，各1/1 Passed。后两项证明改后直接煎制/原烤箱并行、Carrier=0及原半早餐存档仍可实际接续，不重跑未变四条热餐UI。art/fry原预检日志与file-discovery日志保留冷启动eval5秒超时NotRun，确认Editor ready/stopped后新日志成功，不清Console、不重写旧结果。

自审与规格同步：3项ADDED Requirement／3场景分别落到GridSoupSystem的有限份数和末份竞争、GridBakeSystem的外模具/内部输入、GridDessertSystem的原子脱模，以及真实订单、文件和普通UI用例。Design的唯一持续厨房owner、消息入口、冻结加工和限定单层关系相符；源/生成边界、Model状态/Hotfix逻辑、原受管HUD/Board生命周期保持，无新架构或私有库存。完整性4/4、正确性3/3、设计/代码模式一致，未发现本案Critical/Warning。主规格dish-recipes、kitchen-item-processing已同步并保留旧要求；过时Purpose及固定协议3说明已校正。严格验证本案及48/48主规格通过，14份Markdown/280本地引用存在；全仓diff-check仍有5份原Luban生成文件共35处尾空白（含本次新增Carrier字段的生成模板），不手改生成物，不将其报成全仓格式通过。环境外NotRun仍为真机/物理LAN/Player/AOT/构建及其他候选甜品；发生实际兼容或模具丢失回归时再补对应最小验收。

## 2026-10-15 实施与核心运行结果

文件组PlayMode `DessertFiles` 3/3 Passed、0Skipped，`Unity/Temp/Dessert-files-UTF.log`：`DessertActualSoloAndCooperativeClaimFileKeepHalfBatchAndUnfinishedMoldAndResumeWithoutRefill(False/True)`分别实际Solo成功文件、已证明Profile的合作结算票主动领取文件；保存剩1/2批、5000Tick冻结未完烤模、原载具/内部面糊锁定、有限库存4包，重建恢复后只产一块蛋糕，再脱模并用同一模具取最后一份，源文件字节不变。5000Tick仅测试启动时替换配方所需时间，启动后恢复源配置100Tick，证明读取冻结值而非重新算时长。`DessertActualV10SevenMemberBakeFileAndSignedClaimRemainDirectAndDoNotAddDessertInventory`用真实七成员旧Bake DTO和13成员旧根载荷写v10文件及重新签名旧内容版本票；读取载具键默认0，新领取发布当前v11，原直接煎制接续且不加甜品物资，旧源文件不写回。

当前源码文件v11/wire33，真实参数和owner交接见design「已落实的首批参数」。源表12本修改，`Dessert-source-edit.log`、`Dessert-luban-check.log`、`Dessert-luban-gen.log`、`Dessert-proto-gen.log`保留Unity/Temp；Check/导出五个启用目标及四语言导出成功。源GridBakeRecipe增加CarrierDefinitionId，旧四条为0，新5为47；不手改生成物。新原生Blender源生成242切片/16行/51物品，`Dessert-art-export.log`含ART EXPORT COMPLETE，已目视核对图集；受管渲染验收随后单列。

PlayMode `DessertCore`当前5/5 Passed、0Skipped，日志`Unity/Temp/Dessert-core-projection-UTF.log`。配置闭包/通路用例验证三项真实叶原料、两模具、无免费面糊/蛋糕、缺搅拌设备拒绝；LocalAndKcp…(False/True)实际锁盆、暂停冻结、两份扣减、锁模烤制、独立Carrier快照、未脱模非成品、满盘保物、丢弃第一块后脱模第二块及同模具复用；KcpLastPortionRace实际争最后一份只产生一份实物；KcpCannotServe…经真实订单分别拒绝未熟/未脱模，原物和分数不变。

首轮`Dessert-core-UTF.log`实际2/5 Passed、3Failed（此前口头3/5已纠正）：锁盆返回ProcessLocked而夹具预期WorkBusy；远端清包装/启动烤模前未等快照，构造了旧ExpectedTarget。修正夹具断言并等当前持物/内容/工位投影，未放宽产品校验；新运行5/5。一次重编译后发现命令冷启动eval5秒超时，确认Editor ready/stopped再发现成功，空集/预检错误不算通过。当时文件与普通UI尚未运行，后续结果已在本页顶部补齐。

## 2026-10-15 实施准备（尚未改源码或表）

12热餐当前文件v10/wire32，核心/文件及三项受变回归均已通过，普通UI正在收尾。S04严格消费P38三项：甜点预拌粉、鸡蛋、牛奶。计划沿原GridAutoProcess的精确输入/有限剩余份数表达搅拌（锁盆、产2份），原单输入烘焙记录仅增加实际需要的模具引用，烤熟替换内部面糊而保留外模具；显式脱模预检原模具/成品和目标空甜品盘后一次转移。模具、盆、盘之间不嵌套，不建立新库存或计时owner。

原烘焙只持输入/工位且要求输入在台面，新链实际需要“工位持模具、模具持面糊”的一层关系；将在手写Model、保存DTO/捕获/恢复/验证、Proto源及本地/远端快照复制中补同一引用，旧直接烘焙/煎制保持carrier=0。新格式/wire由实施时真实值顺次演进，不预占或改旧Level/Next。牛奶原料可以用一份瓶装原料表达，面糊才是必须在盆/模具中取用的液体，不把整瓶定义成可嵌套容器。

候选最小源定义：43预拌粉、44瓶装牛奶原料、45搅拌盆、46面糊、47模具、48小蛋糕、49甜品盘、50/51原料补给包。保留既有鸡蛋3与烤箱4能力；搅拌机新增实际能力和显式启动/逐份装模/脱模动作。候选布局：搅拌机带盆(1,7)、模具台(5,9)、甜品盘台(2,11)、第二模具台(4,6)，须通过实际全布局/各菜单及旧夹具通路UTF后才落实。原共享单槽不扩容，原蛋/面包等旧档不会补物资；新增Dessert资源菜单变体保留两个内容章元数据。

主要风险是载具/内部输入重复或提前成果、最后一份面糊争用和恢复补满。使用最小本地/真实KCP加工小集（锁定、末份、满盘无损、未熟/未脱模拒绝、同模具复用），Solo与现有合作领取实际文件保存半批/未完烤模并接续、真实旧v10直接煎制只读兼容，以及普通Launcher Solo/合作从三原料到脱模交付/复用模具。复用12未变煎制、08精确投入与既有发布/房间/终关证据；不跑全仓或产品构建。当前13功能均NotRun；接受真机/物理LAN/Player/AOT、其他甜品及真人平衡不验证，出现模具丢失、复份或旧文件拒绝则补对应最小用例。

## 2026-10-15 规划优化

本轮仅修订未开工计划，统一已确认范围、真实前置和逐功能验收；以下早期“仅规划/待裁决”措辞属于创建记录，当前交接以本节、design与tasks为准。没有实施业务或执行UTF/Editor/构建，功能结果仍为NotRun。本案严格格式、正文/引用、任务到验收映射检查通过；全批汇总见[路线记录](../../../../Docs/Product/full-version-roadmap.md)。

## 当前状态与范围

2026-10-15：用户授权继续扫描业务、按优先级建立 change，明确不考虑真机实测，并确认“按已确认目标，菜品分批扩展”。本案只完成规划，任务未勾选；功能实现、UTF、Unity Editor 运行、改表/导表、构建均 **NotRun**。规划检查结果统一见[本轮路线记录](../../../../Docs/Product/full-version-roadmap.md#本轮规划检查)。

PC LAN 与 Android 原案保留，不是本案依赖。没有提交、push、归档或操作第二工作区；此前授权的美术提交不扩展成本轮业务实施许可。

## 严谨程度与已接受边界

新载具关系可能造成循环包含、提前出品和模具丢失。聚焦有限批次、脱模失败保物、模具复用及实际保存，避免通用容器重构。

只选择实际改变行为的最小相关 PlayMode UTF 与普通 Editor 入口；可复用下列已交付主规格关联的原证据，前提是实现、消费者和验收点均未变。 不建立全目录矩阵、额外状态副本或通用测试平台。

接受未验证：真机、设备输入、Player/AOT、物理 LAN/公网不在此次规划内；全目录平衡、未选择内容批次及跨设备持久故障不由本案证明。 需要相关产品交付时另定范围；本案新增/修改行为或出现具体回归时补相应最小验证，不因整理/审查重复跑旧套件。

## 任务与未来验收映射

| 任务/功能 | 最小验证与断言 | 当前结果 |
| --- | --- | --- |
| 1.1 面糊与载具配置 | 配置 UTF及Check/生成：P38→装模→烤制→脱模闭包。 | NotRun |
| 2.1 制作与脱模 | 本地/KCP UTF：份数守恒、失败保物、同一模具复用。 | NotRun |
| 3.1 入口与持久化 | UI/文件 UTF：制作可达、剩余份数和冻结烤制恢复。 | NotRun |
| 4.1 普通入口集成 | 普通 Editor：小蛋糕合法交付及原模具再次装模。 | NotRun |

同一运行可覆盖多行；先发现真实 PlayMode 用例并记录过滤范围，再执行改变行为的小集。任务中写的是验收目标，不是已经存在或已通过的测试。已有通过结果只在实现和验收点不变时复用，缺口补跑；失败、空集、Skipped及缺输出仍未验收。

## 可复用的既有合同

已有运行记录：[2026-10-13-solo-dish-recipe-chain](../2026-10-13-solo-dish-recipe-chain/execution-notes.md)、[2026-10-13-grid-kitchen-soup-batch-cooking](../2026-10-13-grid-kitchen-soup-batch-cooking/execution-notes.md)。只复用未变的原行为；本案新增功能不因引用它们而通过。


- [dish-recipes](../../../specs/dish-recipes/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。
- [kitchen-item-processing](../../../specs/kitchen-item-processing/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。

## 实施交接

2026-10-15 已确认 S04 原味小蛋糕，按目录的甜面糊、装模、烘烤、脱模到甜品盘实施。耗时和批次产量按有限配置落地，不新增其他甜品。

参数按配置和实际消费者核对；运行后回填真实方法、结果、原日志路径和失败原因，不保存额外业务状态副本。
