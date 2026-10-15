# Execution Notes

## 2026-10-15 最终交付

4/4功能已验收。当前文件v10（v1–v9只读）、wire32；13个不同用例的最新有效结果Passed：核心3（配置1+本地/KCP2）、实际文件3、普通Launcher/UI4、受变回归3。不将失败/前置NotRun或重复运行累加为通过。主规格dish-recipes/kitchen-workstations已按当前规则同步，保留原场景；参数与早期失败见下文。

| 任务/功能 | 实际运行与原输出 | 结果 |
| --- | --- | --- |
| 1.1 两菜精确闭包、有限生料、设备与通路 | `HotFoodConfigurationRequiresDifferentRawBranchesExactAssemblyAndReachableEquipment`；`HotFood-core-side-UTF.log`中的配置1项，最终侧边布局Check/五目标生成见下文 | Passed |
| 2.1 显式煎制、锁输入/占用、拒绝、暂停/同冻结参数、原烤箱并行、唯一成果 | `HotFoodRealLocalAndKcpFryingLocksOneInputPausesAndCompletesOnceAlongsideOriginalOven(False/True)`；`HotFood-core-action-UTF.log` | 2/2 Passed |
| 3.1 原菜/物品图形、两菜识别、有限补货、清理、未完/部分早餐保存及旧链 | `HotFood-files-UTF.log`3项；base/toast-position/art-ready三份受变回归输出各1项；下述普通UI四项 | 全部Passed，重叠不重复计数 |
| 4.1 F11/F43各自Solo/合作真实全过程 | `HotFoodUiSoloActualBurgerSupplyAssemblyDeliveryAndOriginalPlateWash`、`HotFoodUiSoloActualBreakfastThreeBranchesDeliveryAndOriginalPlateWash`、`HotFoodUiCooperativeActualBurgerHostFryingAndKcpAssemblyDelivery`、`HotFoodUiCooperativeActualBreakfastParallelFryingAndKcpToastAssemblyDelivery`；`Unity/Temp/HotFood-UI-map-warm-UTF.log` | 4/4 Passed |

四条普通UI真实从模式/选档或Host房间选择热餐入口。F11使用初始生肉饼实际煎熟、采购/到货/拆份汉堡面包、清空并丢弃原包装、按两件组件组装。F43从原鸡蛋煎成蛋、生香肠采购并煎熟、原面包烘成吐司，三件组件同盘组装；实际同时观察到煎台和原烤箱两个自动作业。合作为Host煎制、独立Fiber真实KCP队友备料/烤吐司/组装交付，沿受管HUD真实切换所有者，使用实际地图移动操作格。每条都真实按订单交付10分、同一原盘玩家清桌后伙伴洗净入池、前厅自然收口；没有免费熟料或后台代交。当前回归已覆盖，后续文档整理不重跑这些套件。

严谨程度仍按本案实际物品/载具风险选择：核心锁定和一次提交、真实文件重建及两菜两模式经营足够覆盖这次有限扩展。明确接受真人平衡、真机/Player/AOT、物理LAN、公网及其他候选热餐NotRun。若新改行为/出现输入丢失或旧档回归，再补对应小集；不因归档阶段或整理扩大测试。

## 2026-10-15 实施前交接

前置00–10已交付，11为文件v9/wire31且核心/文件通过、普通UI收尾中；首次配置失败已定位并修正为牛肉饼台侧边(5,7)，重新Check/五目标生成通过；配置UTF已Passed，两个加工用例因本地动作上限仍为StartAssembly拒绝StartFry，修复后正在新建运行，失败不算通过。F11为汉堡面包32×1+熟牛肉饼34×1→成品38，F43为熟香肠36×1+煎蛋37×1+既有吐司9×1→成品39，均用现有餐盘10。新增煎台Kind.Frying/Capability.Fry4096与显式StartFry，使用既有单物件自动加工owner和Tick，不把烤箱变成煎台。新DishStageKind.Fry沿现有冻结输入、输出和耗时的加工记录保存；存档验证依据实际配方能力，旧Bake仍只认烤箱。

有限资源组合Hot=4包含既有基础/饮品/沙拉资源，加一台煎台(2,16)、牛肉饼台(5,7)上的一份生牛肉饼，以及面包/生肉饼/生香肠的有限原料供应；不供应熟料。新资源只用于新开热餐菜单，旧Level/Next/资源集合及两个内容章元数据不变。热餐沿既有模式/菜单三关变体入口（Solo Chapter1，合作Chapter2）开放，不把菜单变体宣称为第三个内容章；两个可选内容章继续沿11已交付入口。具体首包参数以Check后的源表和以下实际运行为准。

主要风险是输入提前解锁、完成重复产物，以及早餐把吐司当组件时被旧独占规则拦截。只允许某条真实组装配方的合法部分组合突破成品独占，原吐司+番茄/多份吐司仍拒绝；运行最小本地/KCP加工与两条全链、部分组装/未完煎制Solo及现有合作领取真实文件、普通Launcher两菜两模式验收。复用未变汤/吐司/沙拉、存档发布/授权/房间终点证据，不跑全仓或构建。接受真机/物理LAN/Player/AOT、其他候选热餐与真人平衡NotRun；出现输入丢失、跨关增产或旧文件拒绝则补对应最小用例。

现有Plate独占列表含吐司9，同时早餐需要9作为组件，已向用户说明差异；本案扩展共享容器判定及对应存档验证，保留普通成品与错误混装拒绝。源表编辑先用现有artifact-tool，随后原Check/五目标生成；美术沿已授权Blender源导出，不手改生成图集布局。

## 2026-10-15 规划优化

本轮仅修订未开工计划，统一已确认范围、真实前置和逐功能验收；以下早期“仅规划/待裁决”措辞属于创建记录，当前交接以本节、design与tasks为准。没有实施业务或执行UTF/Editor/构建，功能结果仍为NotRun。本案严格格式、正文/引用、任务到验收映射检查通过；全批汇总见[路线记录](../../../../Docs/Product/full-version-roadmap.md)。

## 当前状态与范围

2026-10-15：用户授权继续扫描业务、按优先级建立 change，明确不考虑真机实测，并确认“按已确认目标，菜品分批扩展”。本案只完成规划，任务未勾选；功能实现、UTF、Unity Editor 运行、改表/导表、构建均 **NotRun**。规划检查结果统一见[本轮路线记录](../../../../Docs/Product/full-version-roadmap.md#本轮规划检查)。

PC LAN 与 Android 原案保留，不是本案依赖。没有提交、push、归档或操作第二工作区；此前授权的美术提交不扩展成本轮业务实施许可。

## 严谨程度与已接受边界

风险为旧过程类型被新设备混用或跨关重复产物。用共用煎制能力及两菜各自熟料的拒绝/暂停/交接/保存小集，不遍历所有候选热餐。

只选择实际改变行为的最小相关 PlayMode UTF 与普通 Editor 入口；可复用下列已交付主规格关联的原证据，前提是实现、消费者和验收点均未变。 不建立全目录矩阵、额外状态副本或通用测试平台。

接受未验证：真机、设备输入、Player/AOT、物理 LAN/公网不在此次规划内；全目录平衡、未选择内容批次及跨设备持久故障不由本案证明。 需要相关产品交付时另定范围；本案新增/修改行为或出现具体回归时补相应最小验证，不因整理/审查重复跑旧套件。

## 任务与未来验收映射

| 任务/功能 | 最小验证与断言 | 当前结果 |
| --- | --- | --- |
| 1.1 首批配置 | 配置 UTF及Check/生成：两条完整菜链，禁止免费熟料。 | NotRun |
| 2.1 煎制行为 | 本地/KCP UTF：单作业、暂停停钟、唯一输出、拒绝不改输入。 | NotRun |
| 3.1 全链与保存 | UI/文件 UTF：两菜合流、未完煎制恢复、清理及现有吐司链。 | NotRun |
| 4.1 普通入口集成 | 普通 Editor：两菜分别从生料到合法交付，Solo/合作各有证据。 | NotRun |

同一运行可覆盖多行；先发现真实 PlayMode 用例并记录过滤范围，再执行改变行为的小集。任务中写的是验收目标，不是已经存在或已通过的测试。已有通过结果只在实现和验收点不变时复用，缺口补跑；失败、空集、Skipped及缺输出仍未验收。

## 可复用的既有合同

已有运行记录：[2026-10-13-solo-dish-recipe-chain](../2026-10-13-solo-dish-recipe-chain/execution-notes.md)。只复用未变的原行为；本案新增功能不因引用它们而通过。


- [dish-recipes](../../../specs/dish-recipes/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。
- [kitchen-batch-cooking](../../../specs/kitchen-batch-cooking/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。

## 实施交接

2026-10-15 已确认 F11 经典牛肉汉堡、F43 香肠鸡蛋早餐，按目录配方制作。煎台显式启动后自动加工，耗时/供给量为可调首包参数。

参数按配置和实际消费者核对；运行后回填真实方法、结果、原日志路径和失败原因，不保存额外业务状态副本。


## 源与当前验证记录（2026-10-15，进行中）

12份源工作簿改动：__enums__、GridItem/ManualRecipe/BakeRecipe、Dish/DishStage、Station/InitialLayout、Supply/Order/Level/CustomerFlow；只新增Hot枚举项，注册表不变。各新供应每包1份、各5包、运输40Tick；生牛肉饼另有台面初始1份，汉堡面包/生香肠经真实采购获得；现有鸡蛋/面包供应不复制。煎肉饼80、香肠70、鸡蛋60Tick，两组装40Tick。三关营业900/1000/1100Tick、订单等待700Tick，菜单两道热餐，首关可选扩建8；明确首包参数，不宣称正式平衡。

源编辑原输出`Unity/Temp/Hot-source-edit.log`；最终侧边布局`Hot-layout-side-edit.log`，`Hot-layout-side-check.log`五项Check成功及`Hot-layout-side-gen.log`五目标和四语言生成成功。前两次布局产物保留原日志，其中(4,7)被UTF实际拒绝为堵路；没有把Luban通过当作通路通过。临时inspect.ndjson已只删本操作输出。原Blender源增加煎台四向与32–42物品，生成229切片、15行、42物品，原输出`Hot-art-export.log`含ART EXPORT COMPLETE；已查看生成图集，新生/熟料、汉堡、早餐和包装可区分。游戏受管渲染仍待实际UTF。

`HotFoodConfigurationRequiresDifferentRawBranchesExactAssemblyAndReachableEquipment`在`Unity/Temp/HotFood-core-side-UTF.log`为Passed（该轮总1/3，另2失败已说明），覆盖真实Hot启动、全菜单闭包/实际全布局通路、精确两/三组件、无免费熟料、缺煎台拒绝、吐司组合与错误混装。当前后续用例均以终态为准。新增文件测试一处错误命名参数导致编译失败，改回现有StartSolo后重新编译已通过，不计作运行验收。


加工最终用例`HotFoodRealLocalAndKcpFryingLocksOneInputPausesAndCompletesOnceAlongsideOriginalOven(False/True)`在`Unity/Temp/HotFood-core-action-UTF.log`为2/2 Passed：本地Host及实际独立KCP分别煎制，重复启动/提前取出/错用Bake拒绝，暂停停钟、原80Tick保持，另一成员原烤箱同时实际加工，完成一次且可移交真实新熟肉饼。与未变配置1项合计核心3个不同用例Passed。

`Unity/Temp/HotFood-files-UTF.log`三项全部Passed：`HotFoodActualSoloAndClaimFilesKeepPartialBreakfastAndUnfinishedFrozenFrying(False/True)`实际烤吐司与煎蛋合入原盘（未完成不能识别菜），真实煎肉饼冻结为测试配置5000Tick，随即恢复源80Tick；自然关末发布Solo或已证明Host合作票并主动Claim，真实文件保留原进度/5000Tick/两组件，重建新运行接续唯一输出，原来源字节不改。5000只用于可调参数冻结夹具，不改产品耗时。合作文件走真实Host结算、Root投递和Claim，独立KCP加工证据见核心，不声称远端自动持久化。`HotFoodActualV9FileRestoresOriginalMenuWithoutNewGriddleIngredientsOrStock`手工封装真实v9头与原13成员载荷，经真实文件读取/重建保持102原链、两菜、原设备/库存、原文件字节，无热餐物资。

受变回归三项最新有效结果Passed：`SoloDishConfigActualMenusAndFiniteRawInventoryHaveNoFreeDish`（`HotFood-regression-base-UTF.log`，过时的“所有非沙拉菜单都须两菜”断言改为只核对原基础/饮品模式链）；`SoloDishPlateFullPlateRejectsSecondActuallyBakedToast`（`HotFood-regression-toast-position-UTF.log`，原额外面包台夹具2,16与新煎台冲突，改夹具空位2,7，不改产品布局）；`RestaurantArtAllDefinitionsDirectionsAndLayeredContentsFollowProjection`（`HotFood-regression-art-ready-UTF.log`，42独立物品切片及12类工位四向共48，容器/人物层均实际受管渲染）。原失败`HotFood-regression-toast-UTF.log`保留，不改写。

美术组首次前置eval超时（`HotFood-regression-art-UTF.log`），尚未启动测试；确认Editor ready/Play stopped/前次测试completed后新建运行通过，历史Console错误保留。普通合作早餐首次已完成煎蛋、采购生香肠、真实清空包装和放好面包输入，随后测试调用不存在的“前往烤箱”按钮失败（`HotFood-UI-CoopBreakfast-UTF.log`）；改为真实地图处理器移动到烤箱操作格。新类别第一次又在重载后前置eval发生5秒超时（`HotFood-UI-map-ready-UTF.log`），没有作业启动，确认空闲后新建`HotFood-UI-map-warm-UTF.log`运行中。期间一次只读eval状态查询也遇Pipeline 400超时；不清Console、不取消/续写测试，后续运行期间不再插入eval。普通UI尚待终态，不勾3.1/4.1。

最终文档/格式检查：change严格通过，48/48主规格严格通过；15份相关文档268本地引用存在，编码检查通过。全工作区diff-check仍因33处既有Luban Editor生成模板尾空白返回2，仅4个生成文件；不手改生成物或扩展模板清理。未提交、push、归档或同步第二工作区。
