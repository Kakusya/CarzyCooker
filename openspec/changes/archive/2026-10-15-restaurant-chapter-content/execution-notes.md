# Execution Notes

## 2026-10-15 实施与运行（完成）

使用既有 ProductRoot/Host/Client owner；新增内容章元数据，不建新业务树。内容章1“家常开张”映射Solo11/合作21、1101/2101起；章2“进阶小馆”映射Solo12/合作22、1201/2201起，每条三关。原Chapter1/2、101/201/301/401/501/601/701/801各原链均不变。新增RestaurantChapterConfig源和表注册、追加12行Level/CustomerFlow；Check及五目标生成/本地化成功，原输出 `Unity/Temp/Chapter-luban-check.log`、`Chapter-luban-gen.log`。源编辑输出 `Chapter-source-edit.log`；Proto经原生成器，输出 `Chapter-proto-gen.log`。

首章菜单汤→汤/吐司→汤/吐司/F01，第二章F01/F02/橙汁→加汤→加吐司。首章营业500/650/800 Tick，次章600/750/900 Tick（50ms/Tick）；来客间隔120/100/80 Tick、排队上限2、进食60 Tick、星阈值10/20/30。首章订单等待300/280/260 Tick，次章340/320/300 Tick；两章首关可选扩建8，其余无必选奖励。都是首包可调参数，不宣称真人平衡。

Room公开请求、内部Submit和本地意图均携带ContentChapterId并复制值；本地仍经HostPeer/HostSession有序邮箱，真实KCP经Session进入同一裁决。Host权限先于选择校验；已存Level/Chapter身份足以定位内容，无新载荷字段。wire31、文件仍v9，旧文件只读且不补货。Solo选择、Host房间选择、阶段标题、检查点和完成记录使用同一配置读口；首单缺省按本关CustomerFlow首个合法菜品，既有目标/订单优先保持。

| 任务/功能 | 实际用例与输出 | 当前结果 |
| --- | --- | --- |
| 1.1 四条三关映射、生产闭包、原引用 | `RestaurantContentChapterConfigHasTwoThreeStageRoutesInBothModesWithoutChangingOldReferences`及Check/生成；`Unity/Temp/RestaurantContentChapter-core-UTF.log` | 配置及核心合计5/5 Passed |
| 2.1 两章Solo/实际KCP选择、Host权限、自然终点、独立复玩 | 同一核心类别四个参数用例：false/true × 1/2；实际Room请求、独立KCP、自然完成三关、终关拒绝继续、原sender/房间保留、旧World拒绝 | 4/4 Passed，包含于上述5项 |
| 3.1 实际新章检查点/完成记录与原v9旧链 | `RestaurantContentChapterActualCheckpointAndCompletionKeepChosenIdentityAcrossSoloAndClaimReentry(False/True)`；`RestaurantContentChapterActualExistingV9CheckpointKeepsItsOriginalChainAndInventory`；`Unity/Temp/RestaurantContentChapter-files-UTF.log` | 3/3 Passed |
| 3.1/4.1 普通入口、逐关菜谱、首单补货、自然收尾/完成记录与复玩 | `RestaurantContentChapterUiSoloActualSecondChapterOrderSupplyTerminalRecordAndIndependentReplay`及`RestaurantContentChapterUiCooperativeActualSecondChapterRemoteDeliveryTerminalAndSameRoomReplay`；`Unity/Temp/RestaurantContentChapter-UI-ready-UTF.log` | 2/2 Passed |

文件用例真实发布检查点、关闭旧运行、只读载入第二关，再通过实际Root保存继续第三关并发布完成记录；合作通过既有证明/结算票/主动Claim再入Solo分支，不能声称独立远端自动保存。完成记录不能重开第四关，旧来源字节不变。普通UI已通过：真实Launcher分别从选档/Host房间选章，橙汁从原橙子切配/榨汁/分杯/贴单到Solo或独立KCP成员交付10分；实际采购/到货/拆份回台，原来源5→4、实物跨关保留；客户自然离桌后玩家清桌；第二/三关菜谱只读、三关自然营业完成，旧继续回调不发命令。Solo发布两检查点及一个完成记录，读完成不建世界，再选章1；合作原房间两成员和sender保留再选章1，新世界回7×18/两桌/库存5，无旧实物。

主规格原“同菜单三关/只有两菜”已向用户说明差异并同步当前开放菜单；保留其余终关/保存失败/旧档/只读权限场景。最新有效结果合计10个不同用例（核心5+文件3+普通UI2）；重复运行不累加。首次Solo UI在首关真实交付/补货/收口后，因测试尚未选装修即提前查找保存按钮失败，原输出`Unity/Temp/RestaurantContentChapter-UI-Solo-UTF.log`；修正为实际选择并等待当前按钮后新建运行，两模式均通过。期间一处测试错误属性造成编译失败，删除不存在的断言后重新编译成功；没有把编译或失败算通过。主规格与change严格检查通过（全68项含20change/48spec），8份相关文档44本地引用有效，任务功能覆盖齐全，无需重跑未变套件。

当前新增菜尚未纳入（12–14按后续实际交付），真机/物理LAN/Player/AOT、真人理解及全量平衡NotRun。

## 实施前交接（2026-10-15）

00–10已经完成，从文件v9/wire30交接。两个可选内容章“家常开张”与“进阶小馆”，各三关，均有Solo/合作映射。内容身份1/2与旧ChapterId1/2（旧Solo/合作链）分开：新章使用Solo11/12、合作21/22及独立新LevelId，旧1/2与所有旧Level/Next保持。新增最小RestaurantChapterConfig元数据，复用原Level/关间/终关与记录；章节不是MenuVariant，不建立新的经营owner。

第一章依次汤→汤/吐司→汤/吐司/F01；第二章由F01/F02/橙汁逐步加入汤、吐司。使用已实际交付的Growth=3资源组合，初始定义集合明确覆盖各章将开放菜品及设备，当前菜单逐关生效。首关可选原有限扩建8，之后无必选空奖励；产量/库存沿现有配方，时长/客流仅首包参数，经代表性实际经营复玩，不宣称正式平衡。后续热餐/甜品/咖啡实际交付后再选择新开版本，不能改旧关链来补旧档物资。

主要风险是把模式链当内容章、选错首关或给旧档补货；采用四组新三关链配置/生产闭包验证、本地及KCP实际章选择/终点/独立复玩、实际旧/新/合作文件、普通Launcher代表性首单/补货/自然收尾。复用00–10未变制作/原终关/许可/扩建证据，不重跑全仓。接受真机/物理LAN/Player/AOT、其他候选菜与真人理解/留存NotRun；出现章节映射错、原档改链/补库存或重复实体则补对应最小用例。

源表新增RestaurantChapterConfig及注册，只追加LevelConfig/CustomerFlowConfig，不变旧ID；Proto追加选章定义ID并沿原Room请求/内部邮箱复制，wire演进到31。Header/载荷已有稳定Level/Chapter身份，无需为纯内容新开另造存档格式；实际运行前任务0/4，功能NotRun。原规划“只接受0/1”的创建时调查已被08–10交付取代，以本节真实前置为准。

## 2026-10-15 规划优化

本轮仅修订未开工计划，统一已确认范围、真实前置和逐功能验收；以下早期“仅规划/待裁决”措辞属于创建记录，当前交接以本节、design与tasks为准。没有实施业务或执行UTF/Editor/构建，功能结果仍为NotRun。本案严格格式、正文/引用、任务到验收映射检查通过；全批汇总见[路线记录](../../../../Docs/Product/full-version-roadmap.md)。

## 当前状态与范围

2026-10-15：用户授权继续扫描业务、按优先级建立 change，明确不考虑真机实测，并确认“按已确认目标，菜品分批扩展”。本案只完成规划，任务未勾选；功能实现、UTF、Unity Editor 运行、改表/导表、构建均 **NotRun**。规划检查结果统一见[本轮路线记录](../../../../Docs/Product/full-version-roadmap.md#本轮规划检查)。

PC LAN 与 Android 原案保留，不是本案依赖。没有提交、push、归档或操作第二工作区；此前授权的美术提交不扩展成本轮业务实施许可。

## 严谨程度与已接受边界

内容常见风险是生产链缺口与高压到无法完成既有操作，用菜单闭包加每种新关机制一次 Editor 复玩覆盖，不做全目录穷举或统计留存研究。

只选择实际改变行为的最小相关 PlayMode UTF 与普通 Editor 入口；可复用下列已交付主规格关联的原证据，前提是实现、消费者和验收点均未变。 不建立全目录矩阵、额外状态副本或通用测试平台。

接受未验证：真机、设备输入、Player/AOT、物理 LAN/公网不在此次规划内；全目录平衡、未选择内容批次及跨设备持久故障不由本案证明。 需要相关产品交付时另定范围；本案新增/修改行为或出现具体回归时补相应最小验证，不因整理/审查重复跑旧套件。

## 任务与未来验收映射

| 任务/功能 | 最小验证与断言 | 当前结果 |
| --- | --- | --- |
| 1.1 章节身份和内容配置 | 配置 UTF及Check/生成：每模式两个三关章、无孤立关、旧关链仍有效。 | NotRun |
| 2.1 入口与完成流程 | 本地/KCP UTF：选章身份、Host控制、两章终点与独立复玩。 | NotRun |
| 3.1 提示和记录兼容 | UI/文件 UTF：逐关提示、新旧记录语义、完成记录不续关。 | NotRun |
| 4.1 普通入口集成 | 普通 Editor：章选择、代表性递进关经营、第二章终点及复玩；映射全部功能点。 | NotRun |

同一运行可覆盖多行；先发现真实 PlayMode 用例并记录过滤范围，再执行改变行为的小集。任务中写的是验收目标，不是已经存在或已通过的测试。已有通过结果只在实现和验收点不变时复用，缺口补跑；失败、空集、Skipped及缺输出仍未验收。

## 可复用的既有合同

已有运行记录：[2026-10-14-solo-first-chapter](../2026-10-14-solo-first-chapter/execution-notes.md)、[cooperative-chapter-completion](../2026-10-15-cooperative-chapter-completion/execution-notes.md)。只复用未变的原行为；本案新增功能不因引用它们而通过。


- [restaurant-chapters](../../../specs/restaurant-chapters/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。
- [kitchen-recipe-guide](../../../specs/kitchen-recipe-guide/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。

## 实施交接

2026-10-15 已确认两个可选大关、每个三小关。复用合作终关和解锁许可；首包可只编排已交付菜品，新增菜实际交付后再纳入，不依赖全部内容批次。

参数按配置和实际消费者核对；运行后回填真实方法、结果、原日志路径和失败原因，不保存额外业务状态副本。


2026-10-15 咖啡批次集成：新开的第二内容章采用1501/2501、Cafe6，三关逐步加入热餐/甜品/咖啡；两个大关、稳定章身份及每章三关保持。原1201/2201和旧Next链保留，旧成功档不补新库存。受变核心5项、普通Solo/KCP三关UI2项与实际旧文件兼容已验收，详见[咖啡记录](../2026-10-15-kitchen-beverage-batch/execution-notes.md)。
