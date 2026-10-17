# 执行记录

## 投入取舍

主要风险是布局误拒绝已有菜品玩法，以及错误配置晚到运行期才暴露。使用原 Hotfix 入口局部修正，不涉及文件覆盖、原生内存或新架构；小集 UTF 覆盖有效十一菜配置、冲突拒绝、直接／载具烘烤、非法关系、本地／实际 KCP，再以普通 Launcher 检查实际入口。未改的生产链可引用原归档证据，不重跑全部产品套件。

接受本轮不运行独立 Player/AOT、物理 LAN 和 Android 真机；若修改协议、存档、资源或目标平台行为再补相应验收。阶段／版本／几何规则沿用原合同，不建立穷举布局和生产规划平台。

## 功能与结果

- 审查：十一菜／16种工位／63种物品，11手工（含显式组装）／6批次／6烘烤配方；审查正文在 audit，10个本地文档链接检查有效。
- 修复前 PlayMode `DishWorkstationContracts`：10项，1 Passed／9 Failed。两项本地／实际KCP合法载具布局在 LayoutDo 报 `LayoutRelationshipsChanged`；四项歧义启动均预期 InvalidOperationException、实际未抛。三个坏关系拒绝用例在“内部缓存 Cell 必须等于工位”断言失败，修为“拒绝前后原 Cell 不变”，不修改产品位置语义。
- 首次测试编译因错误枚举名 CounterClockwise180 失败，修为生成枚举 HalfTurn；最终代码及UI测试的 recompile_status completed、failed=false，详见下表。
- 修复后 PlayMode category `DishWorkstationContracts`：11项，10 Passed／1 Failed，退出码2；10项核心用例全部通过且 Console 无新增错误。UI 在旋转时收到 `LayoutOccupied`，原因是玩家仍在旋转后将占据的 (4,14)，属于合法拒绝；测试改为通过原地图入口先走到 (3,13) 再旋转，只补跑该UI及尚未跑的直接烘烤回归，不重跑已通过10项。

| 任务／功能 | 实际用例 | 当前结果 |
|---|---|---|
| 1.1 代码健康与业务缺角 | audit 正文及10个本地链接检查 | 已完成静态审查 |
| 1.2 手工／烘烤／乱序批次／菜品歧义 | DishRoutingAmbiguityRejectsBeforeWorldAndNamesBothDefinitions 的4个参数 | Passed；初始化Stopped，HostScene/Authority未建立，错误含原／冲突ID |
| 1.2 当前十一菜、热奶／打泡、显式组装合法 | DishRoutingActualElevenMenusKeepSteamActionsAndExplicitAssemblyDistinct | Passed；全部实际菜单图校验及真实甜品启动 |
| 2.1 载具布局与完整制作 | DessertBakingLayoutMovesSameCarrierAndCompletesOnceAtNewApproach(False/True) | 两项Passed；真实本地／KCP制作，移动／旋转保留物品和过程，快照一致，旧版本／入口堵塞拒绝，唯一完成及新位置拿取脱模 |
| 2.1 坏关系完整拒绝 | DessertBakingLayoutRejectsBrokenRelationshipsWithoutPartialMove 的3个参数 | Passed；输入反向引用／载具反向引用／重复内容均拒绝，原位置、布局版本和进度保持 |
| 2.2 普通Launcher入口 | DessertUiOrdinaryLauncherMovesAndRotatesBakingCarrierThenUnmoldsAtNewStation | 修正操作后独立补跑1/1 Passed，退出码0；原模式／选档／新开甜品入口，实际两包采购拆料、搅拌、装模烘烤；原地图走开、原按钮旋转、地图移位，新位置拿取及脱模到原盘全部通过，Console覆盖完整 |
| 2.2 直接烘烤回归 | SoloDishBakeLayoutPreservesOriginalInputProcessAndFrozenDuration | 独立补跑1/1 Passed，退出码0；原面包／作业／80 Tick冻结耗时保留，移动后一次完成；Console覆盖完整 |
| 3.1 最终编译／规格及实现核对 | recompile_status、openspec validate --strict、逐项检查 | 当前实现编译completed、failed=false、compilationFailed=false；严格格式及15个本地链接通过；核对结果见下文 |

修复后共有12个不同相关UTF具有适用于当前实现的最新Passed结果（10核心＋1直接烘烤回归＋1普通Launcher UI），不是一次全套12/12。补跑仅改测试操作，10项核心结果完整复用；保留上述原失败说明，不称首次全绿。表源和生成物均未改，故未导表；Player/AOT、Android真机、物理LAN均NotRun。

## 实施核对

- 完整性：审查记录、proposal/design/两份delta/tasks均齐，5项任务均有对应静态或运行证据。
- 正确性：两项新增要求、五个场景均对应实现和上述行为测试；手工与烘烤歧义4参数中的2项、批次乱序1项、成品身份1项，合法菜图和两种蒸汽能力另有通过结果；布局成功、本地／KCP投影及坏关系拒绝均实际运行。
- 一致性：本轮生产代码只改 GridDishConfig 的初始化预检与 GridStationLayoutSystem 的完整关系检查；复用原 ET owner、消息、业务步、投影和退出，未新增权威或计时。直接／载具关系分支与原 Bake/Restore合同一致。自审未发现本change的阻断问题；主规格已同步，change已按用户授权归档；提交与推送沿本轮明确授权执行。

原始测试输出保持 `Unity/Temp/pipeline_test_status.json`，Editor 日志保持 `C:/Users/Administrator/AppData/Local/Temp/CarzyCooker-UnityCLI-Editor.log`；Pipeline 后续运行会更新自身作业输出，本案不复制历史原始结果。

## 归档与提交范围

2026-10-17 用户明确要求归档、commit及push。两份主规格各新增一项要求、合计五个场景，其原Purpose和其他要求保持；随后归档至本目录，全量严格验证72/72通过。仅修正文档和归档链接，复用上述当前实现适用的12项UTF，不重复运行。

提交仅包含本案手写代码、测试、审查、主规格、归档产物和当前进度中本案段落；其他美术／平台适配／未实施提案及进度历史改动留在工作区。推送沿main现有历史，包括原本领先origin/main的入口功能提交，作为本次原界面／测试依赖的基线。
