# 玩家入口业务与 UI 补齐路线

2026-10-16 收尾：原批 **18案已实施并验收，54/54项任务**。逐案实现/运行/普通入口证据见下方完成复核与各案execution-notes；下文原建案方案及规划校验数字是当时快照。后续第二、三批共21案及原队友总览仍按其独立任务推进，不计入本批完成。

## 2026-10-16 完成复核

本节逐案对照原任务、增补规格、设计、当前源码/行为断言及 execution-notes 的适用运行记录；覆盖原 **18案、54项任务、36项Requirement、56项Scenario**。每案正常路径、变化/取消及HUD重开旧回调均在其执行记录映射；领取案额外的身份缺失和Slot/修订场景也有实际文件/KCP断言。未新增或缩小原18案范围，后建21个补充规划案及原队友总览不计入本批完成数。

| 维度 | 复核结果 |
|---|---|
| 完整性 | 原18案各3项任务均有实现、运行与文档证据；不是以CLI勾选单独证明完成 |
| 正确性 | 36项增补需求/56场景逐案对应以下实现、UTF和正常入口；真实文件/本地消息/KCP按各案适用范围验证 |
| 一致性 | 保留原UI1101/Board/ET层与owner；角色实际挂Level、厨房仅索引的图注已纠正；未改表、协议或文件格式 |
| 格式/引用 | 逐案严格校验18/18通过；正文/引用与当前任务状态另按本轮检查结果记录 |
| 已知范围 | 真人理解、PC独立Player/物理LAN及Android真机保持原NotRun，不属于本批入口交付的已验收范围 |

| 序号 | 原案与详细证据 | 当前实现/断言核对重点 | 普通Launcher540×960实际检查 |
|---|---|---|---|
| 1 | [主入口任务导航](execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryNavigation.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/EntryNavigationUiTests.cs)：分组/返回不建世界或写档；旧页面隔离；真实KCP及显式参数原结果 | 主页/合作/单机，浏览0命令、原拿碗1 |
| 2 | [开局预览](../2026-10-16-restaurant-start-preview/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/RestaurantStartPreview.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/StartPreviewUiTests.cs)：真实配置关链/菜单；取消无世界/文件写；Solo确认及KCP Client只读 | 第二章预览→确认1501，原拿碗一次 |
| 3 | [选档只读检查](../2026-10-16-solo-save-inspection/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSaveInspection.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/SaveInspectionUiTests.cs)：完整Select、时间/类型筛选、修订变化再确认、损坏/旧格式/完成只读 | 真实富载荷文件→关2/半批次/在途货 |
| 4 | [地址与连接帮助](../2026-10-16-cooperative-connection-assistance/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryConnectionHelp.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/ConnectionHelpUiTests.cs)：当前endpoint复制/粘贴、无效输入保留、真实KCP/重试、旧复制失效 | 帮助→Host地址复制，房间无餐厅/命令0 |
| 5 | [准备总览](../2026-10-16-restaurant-preparation-overview/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPreparation.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/PreparationOverviewUiTests.cs)：原加成门槛及可选建议、无预制仍开店、暂停/换关/KCP权限 | 浏览0、原加成1/开店2，实际Operating |
| 6 | [厨房动作导航](../2026-10-16-kitchen-action-navigation/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationActionNavigation.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/ActionNavigationUiTests.cs)：原按钮集合/当前目标、单请求、暂停/池化/拖动；补真实帧滚动0.35保留 | 分组0、拿碗1 Executed |
| 7 | [订单制作导航](../2026-10-16-order-production-navigation/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationOrderNavigation.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/OrderNavigationUiTests.cs)：真实同菜两单、精确缺口/物品、合法饮品绑定优先、过期/换关及KCP | Host顾客2/桌2只读查看并返回 |
| 8 | [实物查找](../2026-10-16-kitchen-item-locator/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationItemLocator.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/ItemLocatorUiTests.cs)：唯一实物/内容宿主、手持/池/地面/锁定、实际消费及KCP断线落物 | 按碗查询高亮实际宿主，浏览0 |
| 9 | [供应总览](../2026-10-16-kitchen-supply-overview/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSupplyOverview.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/SupplyOverviewUiTests.cs)：配额/货物/包装分开、满槽/拆份守恒、暂停/收尾/跨关及KCP | 实际采购接收2次、包装1/材料2 |
| 10 | [前厅工作看板](../2026-10-16-front-service-workboard/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationFrontWorkboard.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/FrontWorkboardUiTests.cs)：真实询问/清桌/洗碗、Claim/Stop、伙伴/两端竞争、最后脏碗自然收尾 | 准备/营业查看→原拿取3 Executed |
| 11 | [工位能力说明](../2026-10-16-workstation-capability-inspector/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationStationInspector.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/StationInspectionUiTests.cs)：定义/许可/载具及占地操作格；移旋/扩建、旧关与KCP多能力 | 烤箱标记/详情0、原拿面包1 |
| 12 | [装修预览](../2026-10-16-renovation-choice-preview/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRenovationPreview.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/RenovationPreviewUiTests.cs)：同候选升级/扩建取消、确认、单次安装、旧作业冻结、KCP只读 | 取消2/确认3/下关4，新作业45Tick |
| 13 | [结算下一步](../2026-10-16-settlement-next-step-guidance/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSettlementGuidance.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/SettlementGuidanceUiTests.cs)：菜单差异/唯一下一步、保存失败重试、零星终关无第四关、KCP回原房间 | 201→202、原拿碗5 Executed |
| 14 | [合作进度待办](../2026-10-16-cooperative-progress-inbox/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryClaimInbox.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/ClaimInboxUiTests.cs)：真实持久凭证计数、未知/失败、身份缺失不补建、去向修订及KCP双方主动领取 | 待领1→明确领取1档→原选档/拿碗 |
| 15 | [授权任务入口](../2026-10-16-save-authorization-task-flow/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryAuthorizationTasks.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/SaveAuthorizationUiTests.cs)：三种用途/Host约束、独立信任和有损确认、文件锁失败/修订冲突、KCP | 取消0档→明确导入1档→原Solo运行 |
| 16 | [离开确认](../2026-10-16-restaurant-leave-confirmation/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryLeaveConfirmation.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/LeaveConfirmationUiTests.cs)：Solo取消/无自动暂停保存；Client落物/释放工作；Host退役保房/整体关闭实际回执 | 单机与Host三种确认真实往返 |
| 17 | [操作帮助](../2026-10-16-product-operation-help/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationOperationHelp.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/OperationHelpUiTests.cs)：六主题、容器实物/拖动、阶段/暂停/池化、KCP权限；说明页互斥补验 | Solo/Host阅读及原拿碗，真实拖动无命令 |
| 18 | [失败恢复引导](../2026-10-16-product-action-recovery-guidance/execution-notes.md) | [实现](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationActionAdvice.cs) / [UTF](../../../../Unity/Assets/Tests/Editor/ET/ActionAdviceUiTests.cs)：八类真实拒绝、未知/复制、迟到结果、同容器变化、文件失败、池化/换关/KCP | 满槽拒绝4→阅读仍4→原接收7/包装1材料2 |

### 复用与本次补验

原案每个发生改变的行为已有实际运行证据；本次在行为未变且条件仍适用的范围复用，未因完成审查重跑全仓。最后一案相关5个不同UTF最新适用结果均Passed（首轮保存路径1项、修正显示依赖后新3项及供应1项）；远端补实际Paused/帮助互斥后仅该项1/1 Passed。动作导航另补原滚动位置随真实刷新保持的断言，单项1/1 Passed，证明当前反馈缓存接入未破坏原分组/拿放/暂停/池化/拖动链。

保存用例的真实文件失败路径未受显示依赖修正影响，不重复其自然长关卡；既有配方、签名发布、消息裁决和保留的显式参数合同复用各案明确引用的旧证据。最新一轮即时Pipeline文件只证明当前这一轮；旧轮结果按原工具输出和各案执行记录保留，不将该文件当全部历史、也不复制JSON结果包。失败/修复/新运行分别记载，未清Console。

本轮自有Play已停止、Editor ready；原GameView选择索引6已恢复，且只移除了自有“Entry acceptance (540x960)”尺寸（custom=0），没有SaveToHDD。首次按相对索引移除未生效，经只读核对确认尺寸仍在后，按实际API绝对索引7删除；原7个内置项保持。

未发现本批所需功能缺口或缺失的必要场景证据。设备/真人理解不由脚本推断；第二工作区未承担开发或接收未提交改动。2026-10-16 按用户后续归档及commit授权，同步36条要求／56个场景至10份主规格并归档原18案；全量严格验证72/72通过，原功能运行结果复用。后建21案继续保留规划，不归档或报告功能完成；本次不推送。

## 观察与取舍

现有经营／合作／进度骨架已交付，缺口集中于“找到入口、看懂当前事实、预览结果、明确操作、回到原流程”。本轮以实际 UI 和业务路径为依据扩展，业务补充包括只读完整选档预览、当前订单阅读上下文、物品位置查询、来源／货物分区、结算和领取事实解释；不将它们退化为单纯改按钮标题。

当前源码支持这些入口的主要数据与原操作，不需要另造世界或引入正式传输。本轮不选择新菜批次、人物或新合作规则；玩家准备确认／共享分工等更大方向保留候选。原 PC／Android 尚未完成的环境验收沿其原案继续，不重复建案。长期目标中“玩法尚未实现”等旧状态句与当前进度已有差异，不能当成重复立项的依据；本轮不改历史归档。

**推荐先做两项导航组织，再沿开局、经营、结算和退出扩展。** 以下顺序用于减少共享文件反复整合；各案只需已有入口即可先独立运行，不要求其他18案全部完成才交付。

## 建案清单与建议顺序

| 顺序 | Change | 玩家得到什么 | 关键验收 |
|---|---|---|---|
| 1 | [主入口任务导航](proposal.md) · `product-entry-navigation` | 将开始游戏、继续记录、合作房间、合作进度和存档授权组织为清楚的入口分组，展示当前位置与返回目标；保留所有现有合法入口和显式启动参数。 | 模式／选档／房间／领取／授权的往返及启动参数入口 |
| 2 | [厨房动作分组与目标导航](../2026-10-16-kitchen-action-navigation/proposal.md) · `kitchen-action-navigation` | 在现有 HUD 提供明确的功能分组与当前目标动作，其他合法动作通过全部动作入口抵达；保持当前选中目标、滚动和必要反馈。 | 新旧动作集合对照、真实点击、拖动、暂停和 HUD 重开 |
| 3 | [章节与新开餐厅预览](../2026-10-16-restaurant-start-preview/proposal.md) · `restaurant-start-preview` | 选择已交付章节或已有餐厅菜单后，先显示章节名称、实际关数、各关菜单、模式及保存语义，再明确开始；取消返回原列表。 | Solo／Host 确认与取消、Client 只读及原档不受新开预览影响 |
| 4 | [选档详情与只读检查](../2026-10-16-solo-save-inspection/proposal.md) · `solo-save-inspection` | 提供记录详情与显式继续，展示章节／关序、结果类型、时间／修订／来源和已校验厨房摘要；按继续／完成分类及时间排序，刷新保留有效选择。 | 真实临时文件预览／变化／损坏／取消与普通选档恢复 |
| 5 | [合作地址与连接帮助](../2026-10-16-cooperative-connection-assistance/proposal.md) · `cooperative-connection-assistance` | 按创建／加入阶段显示合法地址示例、当前实际监听地址、复制入口和可理解的连接／取消／重试说明；把回环地址的适用范围说明白。 | 有效／无效输入、复制、真实 KCP 加入失败与取消重试 |
| 6 | 合作队友状态总览 · `cooperative-team-awareness`（另案规划，保留工作区） | 集中看本人／队友的持物、位置和当前工作 | 本人静止时队友更新、失联／恢复、重开与只读 |
| 7 | [通用操作帮助入口](../2026-10-16-product-operation-help/proposal.md) · `product-operation-help` | 提供可随时开关的操作帮助，说明当前实际点击／拖动规则、目标与动作区、普通拿放／容器处理、工作与暂停，以及 Solo／Host／Client 权限差异；链接原菜谱与相关入口。 | 实际点击／拖动、容器操作与阶段／角色说明、阅读无命令及重开 |
| 8 | [订单到制作的导航](../2026-10-16-order-production-navigation/proposal.md) · `order-production-navigation` | 从当前订单直接查看对应菜谱、精确制作缺口和关联顾客／餐桌；同菜多单可明确选择目标，并链接可用物品／工位的查看入口。 | 同菜多单、合法绑定、订单结束／换客／换关及真实制作后返回 |
| 9 | [厨房物品查找入口](../2026-10-16-kitchen-item-locator/proposal.md) · `kitchen-item-locator` | 按物品名称／类别查看当前实物位置、外层宿主和状态，区分手持、台面、容器内、地面、脏净池及加工锁定；选择结果仅查看／高亮当前宿主。 | 各位置、容器链、锁定／消费／落物、暂停与旧结果失效 |
| 10 | [补货与收货业务总览](../2026-10-16-kitchen-supply-overview/proposal.md) · `kitchen-supply-overview` | 增加按来源查看的供应页，分区展示剩余外部配额、运输／待接收、实际包装和可用拆份入口，解释满收货台及当前阶段限制。 | 真实采购／满槽／收货／拆份、暂停／跨关及本地与 KCP |
| 11 | [前厅工作与收尾看板](../2026-10-16-front-service-workboard/proposal.md) · `front-service-workboard` | 增加前厅任务看板，按询问／清桌／洗碗展示当前工作、关联桌位／餐具、可用／前往／执行状态及真实执行者；收尾时显示仍在进行的顾客和必要工作。 | 询问／清桌／洗碗、伙伴与玩家竞争、真实收尾及旧认领失效 |
| 12 | [工位能力与操作位置说明](../2026-10-16-workstation-capability-inspector/proposal.md) · `workstation-capability-inspector` | 在选中工位详情展示可信能力、当前许可中的匹配工序、必要容器、当前实际占地与操作格；提供只读高亮帮助理解位置和旋转。 | 多能力设备、旋转／移位／扩建、许可缩小、Client 只读与退出释放 |
| 13 | [开店准备总览](../2026-10-16-restaurant-preparation-overview/proposal.md) · `restaurant-preparation-overview` | 增加准备总览，集中显示本关菜单、必需加成选择／准备中状态、有效开店控制、现有厨房与供应摘要，并链接原菜谱／补货／布局入口。 | Buff 未就绪／必选／可选、Solo／Host／Client 与暂停／换关 |
| 14 | [装修奖励与升级对比预览](../2026-10-16-renovation-choice-preview/proposal.md) · `renovation-choice-preview` | 候选先进入只读详情，解释奖励类型、实际目标、升级前后匹配作业基础耗时及生效边界；明确确认才提交原选择，Client 可看同一候选说明。 | 新增／解锁／扩建／升级候选、取消／确认／过期、原作业冻结与双端权限 |
| 15 | [结算到下一步的导航](../2026-10-16-settlement-next-step-guidance/proposal.md) · `settlement-next-step-guidance` | 按当前模式和真实阶段组织本关结果、待处理装修／保存、下一关菜单变化或大关终点，并给出唯一清楚的主要下一步和原合法次要入口。 | Solo持久／试玩、合作Host／Client、零星／终关、失败重试和旧回调 |
| 16 | [合作进度待办与领取详情](../2026-10-16-cooperative-progress-inbox/proposal.md) · `cooperative-progress-inbox` | 在模式页、房间和结算入口显示已持久化本端可领数量，提供结果详情、状态分类和已领取记录去向；个人领取仍显式操作。 | 持久化前后、领取／已领／关闭、失败取消、重复打开和普通 KCP 结果入口 |
| 17 | [操作拒绝与处理导航](../2026-10-16-product-action-recovery-guidance/proposal.md) · `product-action-recovery-guidance` | 围绕手满、加工锁定、容器不符、不可达、生产许可、收货台满、旧目标及保存失败，提供可理解原因和对应查看入口；未知原因保留可复制诊断信息。 | 选定拒绝族、迟到旧结果、未知原因与相关入口无业务副作用 |
| 18 | [经营中离开与返回导航](../2026-10-16-restaurant-leave-confirmation/proposal.md) · `restaurant-leave-confirmation` | 提供按角色区分的离开入口及后果说明：Solo 返回模式、Client 离开本次运行、Host 仅关闭餐厅回房间与结束整个房间分别确认；确认前可取消。 | Solo／Client／Host各关闭范围、取消／失败／重复及正常关闭回执 |
| 19 | [存档授权的任务式入口](../2026-10-16-save-authorization-task-flow/proposal.md) · `save-authorization-task-flow` | 按我想继续他人餐厅、导入独立进度、导出给朋友和替换我的记录组织流程，逐步说明来源／目标／用途／结果，并提供返回和可取消步骤。 | 各用途导航与原实际文件链、取消／失败／修订冲突及源档保留 |

## 玩家路径

```text
Mode navigation
  --> start preview / save inspection / cooperative connection
  --> preparation overview
  --> kitchen action navigation
        --> order recipe / item locator / supply / front work / stations
        --> team awareness / operation help / rejection guidance
  --> renovation preview / settlement next step
  --> progress inbox / authorization tasks
  --> explicit leave scope / return to the right page
```

## 案件边界与共享文件

- 主入口导航组织页面；章节预览解释新开参数；选档检查负责合法旧记录。三者分别拥有导航、开局选择和本机记录选择，不新增互相可写的业务状态。
- 动作导航只组织原动作；订单导航只固定阅读目标；物品查找定位实物；补货总览解释外部配额／货物与实际材料；前厅看板解释工作。各案不复制资格判定或创建消费库存。
- 队友总览关注“人”，前厅看板关注“工作”，工位说明关注“设备”，准备页关注“当前开店条件”，避免各页重做同一查询服务。
- 装修预览只在确认后用原选择命令；结算页表达原推进／保存状态；领取页表达本端已持久资格；授权任务页保持原用途和有损确认。每个事实由原 owner 提供。
- 全部 UI 继续使用原 UIGridKitchen（1101）与原 GF／ET owner；本批没有 Prefab／CodeBind 结构重构或表驱动规则改造。若实施发现必要结构改造，另案说明具体价值与迁移消费者，不混进业务入口案。
- 共享的 ProductEntryPresentationSystem／SoloSavePresentationSystem／GridPresentationSystem／DisplayContext 按序整合。后案要读前案最终实现与规格，保留原功能；避免每案定义一套菜单框架或通用事件总线。
- 详情／菜谱／队友／帮助复用一个明确的显示选择，分类使用具名 enum；领域状态仍在原 Model。纯显示变化不每帧重建所有按钮，不为查询保存整份世界快照。

## 本轮决策账本

| 项目 | 已确定边界 |
|---|---|
| 目标 | 让现有合作与经营业务容易找到、理解和安全操作 |
| 证据 | [模块索引](../../../../Docs/AI/module-index.md)、[当前进度](../../../../CarzyCooker当前进度.md)、各案所引实际源码／主规格／相邻测试 |
| 方案 | 复用现有数据、命令、托管 UI 与生命周期；按用户任务组织入口 |
| 接受的产品取舍 | 查看页面不自动暂停、保存、重试、认领或移动；关卡与奖励规则保持 |
| 文件／退出保护 | 选择预览与实际提交分开；原完整校验、用途权限、revision 和正常关闭回执保持 |
| 验收 | 每个新增功能至少一次适用的实际运行；相关 UTF 与普通入口，保留 NotRun |
| 未决范围 | 新合作 Ready／意向／请求规则、正式传输、平衡与额外内容不在本批默认决定 |

用户已授权继续新建入口案，无需重复询问是否生成规划；规划不进入 apply。

## 值得继续讨论的业务扩展

以下方向有价值，但会新增现有入口之外的状态或内容。当前只记录研究入口，不建立带默认答案的正式 change。

| 方向 | 潜在收益 | 建案前需要的决定 |
|---|---|---|
| 房间章节提案与准备确认 | 开局前让双方确认同一菜单 | Host 是否可强开、迟加入及准备状态何时失效 |
| 玩家分工意向／订单意向 | 让队友看见计划、减少重复制作 | 是否仅提示、保留多久、跨关／断线如何清理；不默认抢占 |
| 队友补货请求／采购清单 | 区分“需要材料”与真正采购 | 谁有审批权、重复合并、取消语义、是否允许多人直接采购 |
| 更完整的输入设置与操作适配 | 适合不同设备与玩家习惯 | 具体键鼠／触控／手柄支持范围、重新绑定与冲突规则 |
| 事件音效与可调提醒 | 视线离开时仍能感知到货／工作／订单 | 素材、默认开关、事件优先级、重复与暂停规则 |
| 存档命名与整理 | 多份独立检查点更容易辨认 | 名称保存位置、是否允许删除、恢复策略和确认范围 |
| 真人上手试玩迭代 | 验证入口是否好找、内容是否看得懂 | 实际观察人群与设备；从具体阻塞点生成后续案 |

已有首单说明、菜谱、关闭重开保护和中文拒绝反馈继续复用；本批分别增加操作帮助、订单目标入口和处理导航，不重复宣称原功能缺失。

## 验证成本与后续

每案 execution-notes 已说明风险、可复用结果与未验证项。局部 UI／只读查询使用最小相关 PlayMode UTF，文件案加真实临时文件，退出案用原成员／房间／回执链；普通 Launcher 检查当前入口，涉及合作补 Host 本地与真实 KCP Client。相同真实运行可覆盖多项任务，不因写文档或归档重复跑套件，不默认增加全仓／设备构建。

2026-10-16 规划检查：本批18案的四项规划产物均齐备，逐案严格校验18/18通过；91份 Markdown、314个本地引用均检查通过，无编码替换字符或行尾空白。54项新增实施任务保持未勾选，产品功能全部 NotRun。已有队友总览案复用上一轮规划检查，不重复宣称新增或功能通过。

以上是建案时的实施安排；原18案当前交付与适用运行证据以上方完成复核为准。后续修改仍按具体行为补最小相关验收，失败／空集／Skipped／NotRun不算通过；提交、push、主规格同步、归档和设备操作按相应后续授权处理。
