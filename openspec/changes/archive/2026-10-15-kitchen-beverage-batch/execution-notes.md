# Execution Notes

## 最终交付：4/4，23 个不同 UTF Passed

所有下列原始运行均 PlayMode、0 Skipped；按方法和参数去重共23项，不累计历史重复运行。任务1对应core-replay的配置闭包及Check/五目标生成；任务2对应core-replay 4项（含配置）、binding 1项；任务3对应files 4项和UI-clear 2项的真实文件/绑定/一次性杯/逐桌实际Clear完成；任务4对应同一UI-clear 2项普通Solo/独立KCP逐款交付。chapter-core 5项、chapter-UI 2项证明新开第二章实际订单及三关入口；regression-art/dessert/assembly各1项及regression-chapter-files 2项，共5项最小受变回归通过。原始日志均为Unity/Temp/Beverage-上述组名-UTF.log。

UI-clear是最终清桌证据，每桌认领后均等FrontWork.State=Completed；历史delivery只证明认领，不据此声称清理完成。未变橙汁、原工作规则和离线授权沿原案证据复用。本案文件v12/wire34，图集266切片/17行/63物品。普通咖啡UI不额外等待全营业自然结算；真实文件及章节核心/UI另有自然收口证据。真机、物理LAN、Player/AOT和产品构建未运行，出现具体对应回归再补最小验收。未提交、push或归档。


## 新开第二章普通入口2/2 Passed

`Unity/Temp/Beverage-chapter-UI-UTF.log` PlayMode RestaurantContentChapterUi 2/2 Passed、0Skipped：普通Solo/合作均实际进入Cafe新1501/2501链，真实橙汁备料、补货及独立KCP交付，保持同原料/半批穿三关、自然关尾和终关；Solo实际成功档/完成记录只读查看，合作返回原房间保成员再开C1。配方查看及旧关按钮隔离也覆盖。此处与此前5/5章节核心分别证明真实控件流程和实际三关新菜订单，不新增大关数或改旧文件链。

清桌用例语义校正：前述普通咖啡`delivery`组2/2明确证明三款Served/杯消耗/30分，但HotUiCommand对FrontWork只等待“认领执行”，不等该工作完成；此前“各桌清理通过”的措辞过宽。已补每次实际ClearWork.State=Completed后才取下一杯，新运行另记，不改旧结果；这也避免夹具移动取消尚未结束的清桌。产品清桌/停手规则未改，额外UI自然结算仍不扩大为任务前置。

## 普通三款交付最新2/2 Passed

| 普通入口逐款终态 | Solo | 同Editor独立Fiber真实KCP合作 |
| --- | --- | --- |
| D15 浓缩／Dish9 | 咖啡1份、贴实际9订单、Served、原杯消耗、累计10分 | 同项Passed，Host萃取、KCP制杯/交付 |
| D17 热拿铁／Dish10 | 咖啡1+热奶2、明确混合、贴实际10订单、Served、累计20分 | 同项Passed，KCP热奶/调制/交付 |
| D18 卡布奇诺／Dish11 | 咖啡1+热奶1后最后奶泡1、混合、贴实际11订单、Served、累计30分 | 同项Passed，KCP打泡/最后加泡/交付 |

三款均清理对应餐桌，源份数与原杯内容由真实命令产生。以上对应同一组两个完整方法内按三菜循环的逐款行为断言，不把一项方法计成三项用例。任务4的全部实际终态已具备，章节和受变渲染另记录；任务3尚等待其呈现相关小回归收尾。

`Unity/Temp/Beverage-UI-delivery-UTF.log` PlayMode BeverageUi 2/2 Passed、0Skipped：BeverageUiSoloActualThreeDrinksFiniteBasesFinalFoamBindingAndThirtyPointDelivery、BeverageUiCooperativeActualParallelHostExtractionKcpMilkAndThreeDrinksDelivery。每模式均由普通Launcher模式/房间选择进入，真实采购/研磨/萃取，咖啡与热奶两设备确实并行，第二奶缸明确打泡；杯内真实1份咖啡、1+2拿铁、1+1后最后奶泡的卡布，后两款明确混合。三种合法订单逐款贴单、交付，原一次性杯消耗，3单30分及各桌玩家清理通过；合作Host萃取与独立KCP成员热奶/后续调制分工。此组不等待普通咖啡菜单自然结算，关尾范围按上节复用。后续章节UI当前独立运行，未将其预报为通过。

## UI验证成本修订（任务范围不变）

`Beverage-UI-bind-UTF.log`仍为0/2 Failed，不能记通过：Solo/合作分别已从普通入口完成三款精确基底/并行/混合、奶泡最后、贴单、真实三单30分及各桌清理，最后新增的“自然完成100秒”等待超时。Solo终态诊断仍Draining、余1顾客，未报告为产品崩溃或穷尽诊断；本案三款任务的终态是合法订单Served，原tasks并不要求这两UI重复等全营业收口。自然营业/关尾未改，实际新咖啡Solo与合作文件组ContentComplete、两个三关核心及后续普通章入口分别覆盖适用收口。按项目“控制验证成本，不减少功能覆盖”删除额外UI关尾等待，保留三款全部制作/交付/一次性杯/清桌断言，新运行通过前任务3/4继续未完成。接受咖啡菜单普通UI本次不等待自然结算，不以旧失败结果改写通过；出现实际持续不收口报告时再诊断对应最小流程。

## 普通UI首轮失败与修复

`Beverage-UI-UTF.log`0/2：Solo/合作均已通过三款真实备料、两设备并行、明确热奶/打泡、准确取份与奶泡最后加入/实际混合，停在第一杯贴单按钮不存在。实际GridPresentation仍硬编码一次性橙汁杯19/橙汁18/订单3；改为只读实际容器/唯一成品与RequiresOrderBinding菜谱，显示同菜品有效订单，保留原owner回调与Host最终裁决，不改为直接调用业务。旧失败保留，完整普通UI重新运行前仍不完成任务3/4。

新内容章订单前列安排三批新菜，`Beverage-content-flow-source.log`/check/gen均为原表原生成入口、五目标成功，未改旧1201/2201、1401/2401或原营业时长；配合真实三关核心观察实际创建的订单验证新增内容可遇到，不以列表存在代替运行。所有新表inspect副产物按本案精确清单清理。

## 实际文件4/4、贴单1/1 Passed；普通UI运行中

`Unity/Temp/Beverage-files-UTF.log` PlayMode BeverageFiles 4/4 Passed、0Skipped。ActualSoloAndClaimFilesRestoreHalfCupPausedLatteFrozenFoamAndFiniteBasesWithoutBindings(False/True)分别实际Solo成功文件、证明Profile后合作结算票主动领取：同一文件含半卡布杯56/57、停手10Tick的拿铁原杯及混合过程、5000Tick冻结未完打泡、浓缩/热奶各余1份及豆/牛奶/杯有限库存；此前真实浓缩已贴真实订单，自然结算/新关恢复不带旧贴单。重建立即核对冻结进度/原内容/物品数，继续原拿铁及打泡后半杯最后补奶泡，分别识别10/11，源文件不改写。长时仅在测试启动时替换配方6，之后恢复源60Tick，证明恢复冻结时长而非重算。

同组ActualV11DessertFileKeepsCarrierAndHalfBatterWithoutAddingCafeResources实际v11甜品文件保留外模具/内部面糊锁定与剩1/2批，不补咖啡资源；ActualOldSecondChapterFileFollowsOriginal1202RouteAfterNewChapterMenuChanges使用旧元数据创建真实1201成功记录，写结构未变的v11文件，在当前新元数据下加载仍走1202→1203/Growth，不补热餐/甜品/咖啡且源文件不变。旧元数据只在创建夹具期间替换并finally恢复，未修改生成表或把新库存伪装旧档。

`Unity/Temp/Beverage-binding-UTF.log` PlayMode BeverageBinding 1/1 Passed：真实KCP浓缩杯绑定真实浓缩订单，再向真实拿铁订单换绑被WrongDish拒绝，原绑定/原浓缩保持；已贴单杯加热奶拒绝DrinkAlreadyBound、不扣源；合法交付消费原杯和原液，再交付同请求对象语义被OrderEnded拒绝，只10分/1单。杯身份与订单分开，无第二份状态。

同一原生Blender源导出`Beverage-art-export.log`终态ART EXPORT COMPLETE/exit0，266切片、17行、63物品、16类工位四向。已目视核对咖啡豆/粉、独立壶/奶缸、纸热饮杯/袖套、咖啡液/热奶/泡/拿铁/卡布以及三设备；图集与布局同源一次生成。普通UI及全定义受管渲染尚待终态，任务3/4不勾选。两主规格3项新增要求已同步，保留原橙汁规则，48/48主规格严格格式验证通过。

## 制作组最新结果：4/4 Passed

`Unity/Temp/Beverage-core-replay-UTF.log`的PlayMode BeverageCore 4/4 Passed、0Skipped：三款配置/源叶/通路；LocalAndKcpGrindExtractHeatFoamPauseOrderedPortionsAndUniqueBlend(False/True)经实际豆包/牛奶补货、单输入研磨、独立萃取和热奶/打泡，锁定、暂停冻结、过早奶泡两次拒绝不扣份、满杯拒绝、原杯精确混合只产一份，原业务号重放回复Executed且浓缩仍只扣一份；LastEspressoPortionKcpRace实际同源最后一份竞争只一份56、原批清空，不补满。新增字段仍由原过程/有序杯内容投影持有，动作/owner详见design。

`Beverage-core-storage-UTF.log`2/4：本地整链和KCP末份争用Passed，新增远端重放误调用仅泵本地的SoloReply，等待Pending后退出触发未观察RPC异常并污染随后配置用例。改为项目原Finish(client.CallProduct(...),500)等待实际KCP答复、正确释放返回对象；未加LogAssert绕过，新的完整4/4成功，旧失败保留。新绑定/文件/普通UI仍待运行，功能不据已有4项扩张为完成。

## 2026-10-15 配置已验收，制作组调试中

任务1：14本源工作簿经既有artifact-tool保留原表结构修改，`Beverage-source-edit.log`、`Beverage-luban-check.log`/gen、修正杯标志后的`Beverage-cup-source-fix.log`/check/gen保留Unity/Temp。最终Check五目标、生成五目标和四语言导出成功；原临时inspect sidecar14份按精确清单删除，不手改生成物。新配置和参数已同步design。

`BeverageActualThreeRecipesHaveFiniteRawLeavesDistinctModesAndConfiguredLastFoam`在`Unity/Temp/Beverage-core-cups-UTF.log`及transition运行中Passed，真实初始化并验证三款闭包/全布局/两内容章、新热饮杯3份与原橙汁杯1份、无免费咖啡粉/基底、最后奶泡源字段及错误前序拒绝。制作组其余3项尚未通过，任务2/3/4不勾选。

早期失败保留：`Beverage-core-UTF.log`0/4，源表I/J标志次序填反，杯未标一次性而包装错误一次性；已按实际表头修源并重新Check/生成。`Beverage-core-cups-UTF.log`1/4，新增自动模式遗漏内部完成消息接收分支，到点但没有Ready；补ProductTransitionHandlers按Beverage取原GridAutoProcess，与发送及二次裁决匹配。`Beverage-core-transition-UTF.log`1/4，此后制作已推进，但夹具把已有番茄的备料台当空台放杯，被TargetChanged拒绝；改用实际空切配台暂存，不改产品校验。编译时一次批量替换误触旧PourContents，及NUnit集合链API使用错误，均已修正并重新编译成功；未把编译或静态检查冒充功能通过。发现冷启动eval5秒超时的日志单独保留，不清Console或跳守卫。

## 2026-10-15 实施准备

前案13已4/4，文件v11/wire33。本案沿GridBakeProcess增加咖啡豆单次研磨，GridAutoProcess按显式萃取/热奶/打泡能力区分同份牛奶，原剩余份数/锁定/冻结参数持有真实基底；热饮杯仅作为本批三份容量复合杯，普通橙汁杯仍一份。原手工组装接定份混合，配方新增LastInputDefinitionId表达奶泡前序；保存沿原有序Contents和RecipeId，不建另一份调饮状态。奶泡加入前完整核对咖啡/热奶及容量，再扣源批次；失败两端保持，已贴单杯禁止改变配方，须先合法解绑。

计划参数：52咖啡豆→53咖啡粉（研磨40Tick）；54咖啡接液壶萃取80Tick产56浓缩液四份；55奶缸热奶60Tick产57热奶四份，打泡60Tick产58奶泡两份，三个自动模式均锁载具；59热饮杯容量3，D15一份56，D17一份56+两份57混合40Tick成60，D18一份56+一份57后最后一份58混合40Tick成61。62豆包和63热饮杯包各有限五包，每包豆一份/杯三只，牛奶沿原13源。咖啡粉/萃取液/热奶/奶泡不能采购。三个设备和两奶缸位置须由全布局真实通路验证后落实。

资源组合Cafe=6包括已交付有限批次；普通咖啡菜单1401/2401各三关，第二内容章新开映射1501/2501三关逐步包含已交付热餐/甜品/咖啡，保留1201/2201及全部旧Next行、旧档不补物资。两内容章数量/稳定章身份保持，旧v9第二章仍按原行恢复。新内容版本v12保护新目录，wire按当前33顺次演进，原批次/杯内容载荷字段无需复制。

主要实际风险是错误模式产物、过早奶泡扣份、复合杯误识别/贴单、源批次/杯保存后补满。最小配置/Local+真实KCP核心小集覆盖模式、份数、前序、暂停和重复；实际Solo/合作领取文件保半杯/基底/模式且清旧关贴单；普通Solo/合作各实际交付三款，覆盖基底分工和奶泡最后加入。对新开第二章补受变配置/三关/普通入口验收，原章旧文件只读兼容；不重跑未变橙汁全链。原美术源扩展后一次受管全定义映射回归。功能当前NotRun，真机/物理LAN/Player/AOT/产品构建与其他候选饮品不验证；出现顺序绕过、复份或旧档拒绝时补对应最小验收。未提交、push、归档或操作第二工作区。

## 2026-10-15 规划优化

本轮仅修订未开工计划，统一已确认范围、真实前置和逐功能验收；以下早期“仅规划/待裁决”措辞属于创建记录，当前交接以本节、design与tasks为准。没有实施业务或执行UTF/Editor/构建，功能结果仍为NotRun。本案严格格式、正文/引用、任务到验收映射检查通过；全批汇总见[路线记录](../../../../Docs/Product/full-version-roadmap.md)。

## 当前状态与范围

2026-10-15：用户授权继续扫描业务、按优先级建立 change，明确不考虑真机实测，并确认“按已确认目标，菜品分批扩展”。本案只完成规划，任务未勾选；功能实现、UTF、Unity Editor 运行、改表/导表、构建均 **NotRun**。规划检查结果统一见[本轮路线记录](../../../../Docs/Product/full-version-roadmap.md#本轮规划检查)。

PC LAN 与 Android 原案保留，不是本案依赖。没有提交、push、归档或操作第二工作区；此前授权的美术提交不扩展成本轮业务实施许可。

## 严谨程度与已接受边界

主要风险为顺序绕过、热奶模式混淆和错贴单。用一条共享基底分支加顺序拒绝/重绑保旧/暂停/末份/保存小集，普通 Editor 实做三款。

只选择实际改变行为的最小相关 PlayMode UTF 与普通 Editor 入口；可复用下列已交付主规格关联的原证据，前提是实现、消费者和验收点均未变。 不建立全目录矩阵、额外状态副本或通用测试平台。

接受未验证：真机、设备输入、Player/AOT、物理 LAN/公网不在此次规划内；全目录平衡、未选择内容批次及跨设备持久故障不由本案证明。 需要相关产品交付时另定范围；本案新增/修改行为或出现具体回归时补相应最小验证，不因整理/审查重复跑旧套件。

## 任务与未来验收映射

| 任务/功能 | 最小验证与断言 | 当前结果 |
| --- | --- | --- |
| 1.1 基底与配方配置 | 配置 UTF及Check/生成：三款份数、模式与前序正确。 | NotRun |
| 2.1 基底制作与混合 | 本地/KCP UTF：模式区分、前序、份数与一次提交。 | NotRun |
| 3.1 贴单与持久化 | UI/文件 UTF：错单保旧、一次性杯、复合内容恢复及清理。 | NotRun |
| 4.1 普通入口集成 | 普通 Editor：三款从真实基底到交付，Solo/合作均覆盖。 | NotRun |

同一运行可覆盖多行；先发现真实 PlayMode 用例并记录过滤范围，再执行改变行为的小集。任务中写的是验收目标，不是已经存在或已通过的测试。已有通过结果只在实现和验收点不变时复用，缺口补跑；失败、空集、Skipped及缺输出仍未验收。

## 可复用的既有合同

已有运行记录：[2026-10-14-first-drink-order-binding](../2026-10-14-first-drink-order-binding/execution-notes.md)、[2026-10-14-cooperative-drink-order-binding](../2026-10-14-cooperative-drink-order-binding/execution-notes.md)。只复用未变的原行为；本案新增功能不因引用它们而通过。


- [drink-preparation](../../../specs/drink-preparation/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。
- [orders-front-service](../../../specs/orders-front-service/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。

## 实施交接

2026-10-15 已确认 D15 浓缩咖啡、D17 热拿铁、D18 卡布奇诺及最后加奶泡规则；采用目录游戏份数，其他饮品族不纳入。

参数按配置和实际消费者核对；运行后回填真实方法、结果、原日志路径和失败原因，不保存额外业务状态副本。


最终自审：完整性4/4任务、3/3新增要求；正确性3/3场景对应配置／精确基底与顺序／原杯贴单及实际文件和普通入口结果，设计与owner／内外消息路径一致。没有待修关键项或范围内验收缺口；原始失败与有限未验证项保留，不将静态检查代替运行。全仓git diff --check仍提示74处尾随空白，涉及12份原Luban生成文件（含6份bytes及6份Editor生成C#），不手改生成物；未发现手写改动的空白警告。
