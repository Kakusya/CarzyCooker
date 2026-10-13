# Execution Notes

## 当前实施与已运行结果（2026-10-13）

用户已授权完成五个 change；前置十项完成 change 已同步主规格并归档。本案已完成源配置、设施业务、真实本地消息、受管 HUD、相关 UTF 及普通 Launcher 完整链。原汤默认供水保留；本案 kitchen-batch-cooking 的完整 MODIFIED 要求明确熟汤可以到水池销毁，煮制中仍锁定。

三张 Excel 源追加 Form／Discardable、WaterDefinitionId／WaterUnits、水定义 11 和垃圾桶定义 9／布局 16 `(5,10)`，原 Wash 保留能力并增加接水／排液位。没有安装工作簿工具；用 Python 标准库只修改对应 sheet XML，保留其它 ZIP 部件、样式、公式和验证。实际 ET 五目标 Check 5/5、正常导出 5/5 与四语言导出均成功，退出 0；Client／ClientServer／Editor 的 DR、bytes／JSON 和 Config/Luban 来自正常生成，无生成物手改。

产品启动在创建 Host／Restaurant 前校验形态、供水引用、碗兼容、不可丢弃餐具与必要设施。共享 Station 校验支持 Wash 共址的成对水池能力及独立 Trash，不放宽其它 Kind 的能力。旧 Station 测试 ByteBuf fixture 追加源字段，并按当前配置更新工位／地形计数。

| 任务／功能 | 实际运行 | 结果 |
| --- | --- | --- |
| 1.1 形态／兼容／设施／初始化拒绝 | PlayMode `ET.ProductSessionTests.SoloCleanupConfig`：真实源及原有限七件库存；缺水、碗不兼容、非法形态、可丢弃锅、错供水、错数量、半套能力、缺垃圾桶在建树前拒绝 | 9/9 Passed，0 Failed／Skipped／Inconclusive，工具退出 0，Console 覆盖完整 |
| 1.1 受影响工位配置 | PlayMode `ET.ProductSessionTests.SoloStationConfig`：单槽／有限池、非法容量／几何及两台烤箱身份 | 6/6 Passed，工具退出 0，Console 覆盖完整 |
| 1.1 受影响前厅配置 | PlayMode `ET.ProductSessionTests.SoloCustomerTargetsActualConfigAndPersistentTablesHaveUniqueOwners`：真实餐桌、厨房 owner 和更新后的有限设施布局 | 1/1 Passed，工具退出 0，Console 覆盖完整 |
| 2.1 设施／类型／到位／原身份 | SoloCleanupFacilities 三项：真实盘装吐司及普通番茄垃圾桶处理、错工位；原始混合两端拒绝；真实两份汤盛一份后持锅排液，接收／行走中不消费，剩余批次与相关输入／作业引用同时收尾 | 3/3 Passed；后续仅对新增烘烤引用收尾和强化到位断言分别补跑相关单项，最终均 Passed |
| 2.2 实际接水／容量／卫生／复用 | SoloSinkWater 三项：真实远处接近、填水／满碗拒绝、倒水后原碗复用；脏碗和不兼容盘拒绝 | 3/3 Passed；原汤未改配方且普通入口按两种真实材料完成，见下表 |
| 2.3 锁定／引用／共址 Wash | SoloCleanupLocks 七项：三种空餐具保护、手工参与先拒绝及停手后清理、煮制／烘烤锁定、共址洗碗先占用后允许接水、非法嵌套不得连带删除；原 SoloCustomerUtensilsActualEatClearWashAndTakePreserveSameBowlAndStock | 7/7＋1/1 Passed；暂停／完成手工及完成烘烤引用随消费收尾，不删除正在加工记录 |
| 3.1 请求／独立内容／旧身份 | SoloCleanupMessages 三项：真实请求复制完整预期内容、最新重放／冲突、源内容／设施布局在途替换拒绝、旧绑定／暂停门槛、实际换关后旧清理原请求拒绝 | 3/3 Passed；加强实际换关断言后该单项新运行 Passed |
| 3.2 实际 HUD／场景输入／owner | SoloCleanupUiActualSinkButtonsApproachWrongTrashAndManagedReopen：真实 Launcher／资源、指针单次提交、远处未到位不接水、场景错设施拒绝、同 Mono 池重开旧 callback 不提交、HUD 不穿透 | 1/1 Passed；原 Prefab／CodeBind 和输入 guard 未改变，不重跑未变绑定或全仓套件 |
| 4.1 普通入口全链 | 下述四次普通 Launcher 调用，均仅经真实 PointerClick／ProcessGridPointer 操作并读取独立投影 | Passed；无领域状态写入替代操作 |

本次持有的主工程 Editor PID 68792，Unity 6000.3.18f1／Pipeline 0.8.0-exp.1，经精确 projectPath 匹配；未操作第二工作区。原始 Editor 日志在 `Unity/Temp/solo-five-changes-editor.log`，原生测试输出在 `Unity/Temp/pipeline_test_status.json`，没有另建状态或证据副本。以下规划阶段记录保留历史语义。

早期配置阶段格式检查为 35/35（30 主规格＋5 活跃 change），118 份 Markdown／687 个本地引用通过；该阶段只有任务 1.1 完成。最终收口以下述新增行为和真实入口结果为准，不把早期 1/7 或规划 NotRun 当作当前状态。

## 设施业务与消息路径

协议从 12 递增到 13；只追加原 GridCommand／Submit 的完整 ExpectedContentIds、ExpectedRemainingPortions、ExpectedLayoutVersion，以及原同版 Snapshot 的材料形态／可丢弃和供水字段。正常 Proto2CS 生成 Client／ClientServer，旧 opcode ID 文件无语义 diff。GridIntent 在构造时复制预期列表，重放比较完整载荷，原 DTO 回收后本地意图与客户端投影仍有效。

原 UI → ClientSession 所属值请求 → LocalMessage.Peer ActorId／根 ProcessInnerSender → Peer Ordered MailBox 的 GridPeerHandler → authority ActorId／HostSession Ordered MailBox 的 GridSubmitHandler →快速接收 → Restaurant 唯一业务步完整预检、自动接近及到位重验 →一次状态提交。原两段 Call/Reply 收尾；每 Peer 独立 Snapshot 经 LocalDelivery 回到 ProductClient，复制普通值后回收 DTO，再通知观察者。没有新监听、发送器、私有回调表或另一份库存；wire 仍明确拒绝新增 Solo 动作。

清理先保护有效手工执行者、煮制／烘烤锁定及共址洗碗占用，不能靠自动行走先暂停输入再销毁。成功消费同步撤下相关暂停／完成手工作业、完成烘烤输出及熟汤批次引用，保留原锅碗盘和卫生；垃圾桶不形成库存、退款或分数。原始混合和嵌套拒绝，当前水池固定一份实际水，现有汤仍默认供水。

首次编译有 CS1628（out 参数 source 被 lambda 捕获），改为复制 sourceId 局部值后正常编译。首轮 Facilities 为 2 Passed／1 Failed，RawMixture 预期 MixedContents、实际 TargetChanged（SoloCleanupTests.cs:146）；原因是旧 PrepDo 已观察权威结果而客户端快照尚未投递。清理 fixture 增加仅 PumpMessages 的等待及手槽一致断言，未改世界或弱化拒绝；新运行 3/3 Passed。原失败输出仍留工具／Editor 原位，不改写为通过。

## 普通 Launcher 完整设施链

同一世界 `554ce0698f244f9a887e642142b9dece`，精确工程 D:/MyWork/CarzyCooker/Unity，普通 editor_play 启动 Assets/Launcher.unity。临时 `Unity/Temp/SoloCleanupAcceptance.cs` 通过既有 run_script 内存编译，仅真实按钮／地图输入及投影检查，不加入工程资源或直接写领域状态。

| 入口步骤 | 实际结果 |
| --- | --- |
| Water | 从远处持碗到水池接水；接收及行走 100ms 后仍为空，到位才有 Water。错投垃圾桶 RequiresSolid 保留内容；水池倒空保留原碗 1284060979658759，水 1284060985950368 已消费，库存仍 7。executeMs=9213，success=true、diagnostics=[] |
| PrepareSoup | 复用同碗真实切配、打蛋、倾倒；锅 1284060979658765 含两个真实输入，不要求接水入锅。executeMs=16429，success=true、diagnostics=[] |
| CookAndDrain | 真实启动／完成／盛一份，剩 1 份持锅；错垃圾桶保留熟汤。走到水池前原输入仍在，到位后批次 1284083526140448／原输入被消费，同锅保留；另一碗中的汤 1284083533480641 完整保留，库存 6。executeMs=12190，success=true、diagnostics=[] |
| ToastAndTrash | 真实烘烤、装盘、持盘去垃圾桶；途中保留吐司，到位消费吐司 1284083659309998 和对应完成烘烤记录 1284083655115611，原盘 1284060979658772 保留，库存 5／餐具仍 4／零分。executeMs=9964，success=true、diagnostics=[] |

已查看原生 screen GameView 图 `Unity/Assets/Temp/pipeline-screenshots/solo-cleanup-facilities.png`，看到垃圾桶、保留的锅／碗中汤、手持空盘及设施目标；原样机文字较密的表现取舍保留，截图不替代上述输入断言。Console session `0d8cc20a53754c8295ac582810518da8`，seq197–294 连续98条，reset=false／dropped=false／新增 Error=0／Warning=0，原生 compilationFailed=false／consoleErrors=0；保留此前失败的历史 buffer，不称历史全绿。四次脚本均终态成功后才停止本轮 Play，Editor ready／Play stopped。

主要风险及相称验证沿本页 Rigor and scope：目的设施、到位、固液／熟汤、锁定、引用和真实 UI 均有本地行为断言与真实入口证据，足够当前 Solo 交付。原发送器／guard 未变证据复用；混合自动分离、显式汤加水、垃圾经济、联机新动作、手机／Player／AOT 未实施或未验收，仍按范围外保留。开通 wire、改配方／材料形态或新增设施占用时补对应局部验收，不重跑全仓。

## Planning status

2026-10-12：用户已授权写五案，本轮进一步明确撤回随时随地清理，要求水池接水/排液、固体向垃圾桶倾倒。只修订清空案和必要引用/产品规则，未实施、未导表、未编译，Unity/UTF/包体均 NotRun。

事实：当前只有 Wash/洗碗台和脏净池，无接水/排液/垃圾桶功能；候选取水台不是实现。拟复用 Wash 共址水池并新增垃圾桶，所有清理必须到对应设施。当前汤默认水合同保留；已询问是否改成必须接水入锅，未答不视为批准。

## Rigor and scope

主要风险是绕设施删物、误分类完成汤、残留作业引用或共址影响洗碗。通过目的设施/四邻到位/形态/锁定的真实本地请求断言，加一次 Launcher 接水排液和固体倾倒足够；原 Wash 受影响部分补验，未改的发送器/制作证据复用。明确接受原始固液混合首版不作自动分离或整体倾倒、无垃圾容量/经济、仅 Solo及未验 Player/AOT；新增混合规则、改汤用水或开放 wire 时补对应行为。

## Acceptance mapping

以下名称为拟补范围，实际执行前发现真实用例；输出留工具原位，不复制证据包。

| 任务 | 功能 | 对应运行 | 结果 |
| --- | --- | --- | --- |
| 1.1 | 形态/水/设施源与布局 | 拟 SoloCleanupConfig | NotRun |
| 2.1 | 水池排液/垃圾桶固体/到位与类型限制 | 拟 SoloCleanupFacilities | NotRun |
| 2.2 | 接水/容量/同容器复用 | 拟 SoloSinkWater | NotRun |
| 2.3 | 锁定/引用/Wash 共址 | 拟 SoloCleanupLocks + 相关洗碗小集 | NotRun |
| 3.1–3.2 | 真实消息/目标UI/重开 | 拟 SoloCleanupMessages + 实际UI UTF | NotRun |
| 4.1 | 实际设施操作完整链 | 普通 Launcher + 最小相关UTF | NotRun |

## Planning validation

2026-10-12：修订后的 `openspec validate solo-kitchen-clear-and-discard --strict` 退出码 0，valid；清空、供应、联机三案及两份产品目标共18份 Markdown 的 UTF-8/空白/引用检查通过。`git diff --check` 无格式错误。清空案现为0/7，功能/Unity/UTF全部NotRun，严格校验不代表设施已实现。
