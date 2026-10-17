# 执行说明

## 本次范围与状态

2026-10-17，初始授权为扫描并创建规划；后续用户要求“总结为change，然后都完成”，现已进入本案及十二方向的实施。其他原有change不扩大实施，保留用户已有未提交修改。原规划阶段状态与后续运行分开记录。

扫描为静态核对，不是全仓穷尽审计或运行故障复现。以 `openspec list`、当前源码、主规格与原运行记录判断现状；不把目标目录或旧文档状态句当成现有功能缺失。

## 业务扫描结论及建案选择

扫描前活动项为22个未开工业务/UI案及2个未完成设备案。已交付制作/供应/前厅/成长/装修/跨关/成功记录/合作领取与离线授权不重复立项。后两批入口共21案与独立队友总览仍未开工，不能把规划视作交付，也不将其缺口再次拆案。

| 发现 | 依据 | 本次处理 |
| --- | --- | --- |
| 缺少按菜品汇总的营业需求 | 原订单导航逐单列 Waiting；菜谱目录按定义找菜，加工看板规划按作业查看 | 本案需求区：同菜数量/最早期限 -> 显式选具体订单；不抵扣成品或建立认领 |
| 有工作看板，但真实队列压力难以查看 | 原前厅看板只有总顾客/工作和粗略桌数；投影已含WaitingCustomers/SkippedArrivals，进场即占队列 | 本案客流区：队列占用/上限、进场/等位、桌位及跳过说明；工作详情仍归原看板 |
| HUD过程星级容易误读，缺门槛说明 | Panels直接显示LevelStars，LevelTransitions只在Complete算星；三阈值已在可信LevelConfig | 本案评分区及原HUD过程文案：当前分数参考与最终提交明确区分；不改计分 |
| 既有入口仍待落地 | 工作区中的第二/第三批入口路线与各tasks（本批未纳入提交）；已知待办见[产品待办](../../../../Docs/Product/backlog.md) | 保留原案；优先完成已有22案，或本案独立补营业信息，不生成重复看板 |
| 新合作协调与玩法内容仍需产品决定 | 既有候选：Ready、制作意向、补货请求、伙伴换岗、新菜/特殊顾客等 | 保留候选身份；本案不默认决定强开、独占、派工、人数、配方和内容规模 |
| 平衡与外部体验仍无充分结论 | 产品待办仍列客流/产量/时长/阈值与真人体验；PC和Android实际任务未全结束 | 不把静态代码推为好玩/平衡或设备已通过，不新增重复设备案 |

[长期目标](../../../../long-term-goals.md)仍含“当前仍是未来设计”的旧状态句，部分根进度旧表也保留较早设备摘要，与后续源码/运行记录已有差异。这会影响仅凭概览判断缺口，因此本次采用原案最新记录和实际实现；该差异已在会话说明，本案不改这些用户正在编辑的文件或扩大为文档清理。

### 与相邻案的分工

| 现有案/能力 | 本案的新增部分 | 保持原负责范围 |
| --- | --- | --- |
| 已交付order-production-navigation | 同菜需求分组后选择具体订单 | 某单真实菜谱/制作缺口/原动作 |
| kitchen-production-workboard | 不统计或预测生产量，只读订单需求 | 每项厨房加工、剩余批次与待取产物 |
| finished-dish-delivery-inspector | 不以成品抵扣待交付单 | 从一件成品找有效交付目标 |
| customer-table-inspector | 总体队列/桌位压力，而非对象详情 | 某个顾客/桌当前状态及原动作 |
| front-service-workboard | 进场/等位分母、队列上限和跳过原因 | 原工作执行者、进度及收尾详情 |
| settlement-next-step-guidance | 营业过程评分参考/真实阈值 | 装修、保存、下一关及终关后续 |

本案没有相邻未实施案作为硬前置；原订单导航、前厅看板与结算入口已交付。共享Presentation/DisplayContext/MonoUI文件按最终实现顺序整合，可复用必要原导航方法，不建立新菜单框架。

## 证据定位

- [需求显示与过程评分](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)、[原订单导航](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationOrderNavigation.cs)、[原前厅看板](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationFrontWorkboard.cs)。
- [真实客流转换](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridCustomerFlow.cs)、[客流配置验证](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/FrontConfig.cs)、[结算计算](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/LevelTransitions.cs)。
- [前厅快照](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridFrontBroadcast.cs)、[客户端安装与handler回收](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridClientSystem.cs)、[独立值投影](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridFrontProjection.cs)。
- 相邻已存在 UTF：[SoloFrontFlowTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SoloFrontFlowTests.cs)中的满队列FIFO/暂停/超时/收尾；[OrderNavigationUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/OrderNavigationUiTests.cs)中的同菜目标和KCP；[FrontWorkboardUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/FrontWorkboardUiTests.cs)中的真实询问/清理与旧回调。

## 严谨程度与验证投入

主要风险是统计口径或过程评价误导玩家，以及共享HUD旧回调选错顾客；本案不写用户文件、不新增协议/权限，不改变物品消费、评分或客流裁决。因此以最小相关PlayMode UTF和普通Launcher实际操作覆盖新显示及其原业务触发足够，不创建新工具、全量性能门槛或庞大矩阵。参考星级只读派生，必须确认不写权威LevelStars。

可复用[原订单导航](../2026-10-16-order-production-navigation/execution-notes.md)、[原前厅看板](../2026-10-16-front-service-workboard/execution-notes.md)和[原关卡流程](../2026-10-13-solo-restaurant-level-flow/execution-notes.md)中行为未变的裁决与原入口结果；这些不证明本案新总览通过。本案至少实际覆盖同菜/贴单需求、真实队列满与入座、真实出餐跨门槛和自然完成，以及真实KCP的新界面行为；同一次运行可覆盖多个验收点，覆盖齐全后不因文档收尾重跑。

接受未验证：真人是否更容易理解、正式数值平衡、Player/AOT、物理LAN与Android设备。仅在出现具体可读性/操作问题、客户端投影缺字段、实际帧分配退化或产品范围改变时补对应验证/修订提案，不自动升级公共基础设施或设备任务。

## 功能验收映射

以下是实施时必须兑现的运行点，尚非测试结果或已存在的新用例。执行前按auto-testing-sop发现实际用例，模式PlayMode，过滤为本案新增用例及发生行为变化的相邻用例。纯派生边界可在同一UTF用受控当前关数据断言，但交付、客流、收尾和KCP集成必须走真实业务入口。

| 任务/功能 | 最小实际验证 | 当前结果 |
| --- | --- | --- |
| 1.1 同菜需求与饮品 | `EntryServiceOverviewBoundDrinkIsStillDemandAndExpiredOrdersDisappear`真实两杯订单/贴单仍计2及超时；`EntryServiceOverviewSoloActualDeliveryExpiryAndFinalScoreRemainReadOnly`真实交付/选择/阅读零命令；受变原`EntryOrderNavigation`两项（Solo同菜及真实KCP） | Passed |
| 2.1 客流/桌位 | `EntryServiceOverviewActualFullQueueZeroScoreAndNextLevelRetireOldRows`实际满队列跳过/零分/自然收尾；Solo旅程受控当前投影显示Free桌被物占用；入座FIFO裁决未变，复用原客流归档证据 | Passed；Free占物显示为受控投影断言，不冒称新入座裁决测试 |
| 3.1 评分口径 | Solo旅程实际汤交付10分跨首阈值/超时不扣/自然权威结算；`EntryServiceOverviewAllReferenceThresholdsKeepAuthoritativeScoreAndStarsUntouched`边界派生不改权威；FullQueue零分自然Completed | Passed；高门槛派生使用受控投影，不冒称真实多次出餐 |
| 4.1 暂停与合作刷新 | `EntryServiceOverviewActualKcpIdleViewerReceivesHostDeliveryAndReopensWithoutCommands`Host实际交付/静止真实KCP更新/暂停；`CoopMemberResumeUiRealDisconnectManualButtonReopenAndRetiredInput`实际Session失联/时间冻结/手动新绑定/旧页失效 | Passed |
| 4.1 旧目标及生命周期 | 上述Solo/FullQueue/KCP池化旧行/换关；原订单导航Solo同菜/过期目标及真实KCP保持原操作 | Passed |
| 4.1 普通入口 | 自有Launcher540×960三分区、前厅往返，阅读command0→0；关闭后拖动cameraY7.111→3.710且command0→0，实际拿碗/放回command1/2均Executed；第二轮普通内存试玩暂停中选择顾客4/餐桌2汤订单并返回command3→3，自然Completed后总览/原结算往返command4→4 | Passed；原下一关command5 Executed、新关Preparing |

## 规划检查

## 2026-10-17 实施中

已接入三分区、原订单选择复用、队列/桌位说明及过程参考星级。首轮PlayMode过滤EntryServiceOverview，2项中1 Passed/1 Failed：实际绑定饮品需求计数通过；Launcher旅程交付后未执行必要清桌，等待65秒仍Draining。夹具补原清桌操作后单项新运行Passed，不增加超时、不修改产品规则。

随后EntryServiceOverview完整5项运行5/5 Passed，原EntryOrderNavigation受变最小集2/2 Passed；补Free桌被物占用显示后的Solo单项及补总览失联/手动恢复断言的CoopMemberResume单项分别1/1 Passed。均终态exit0、Console覆盖完整。6个不同直接适用用例加2个原导航用例已有有效通过结果，不相加为重复运行总数。

普通自有Play已停止，临时540×960视图项仅移除本次所有项并恢复原index6，未保存用户视图配置或修改产品记录。截图位于Unity/Assets/Temp/pipeline-screenshots/service-overview-{demand,flow,score-portrait}.png；score.png为默认输出尺寸1280×720的拉伸捕获，另显式540×960重拍并查看，不作为原生尺寸证据。原始输出保留在Unity/Temp/pipeline_test_status.json及Unity/Logs/Business-Delivery-Editor.log，不复制/改写失败结果。

以下规划检查属于此前规划阶段，功能当前仍按上方实际结果和验收映射逐点收尾。

本案 `openspec validate restaurant-live-service-overview --strict --no-interactive` 通过，schema 的4/4规划产物齐备，无条件跳过。5份Markdown的UTF-8、37个本地引用和尾随空白检查通过；delta有4项新增要求、8个场景，与4项未勾选任务和上表运行点对应。

第二次普通入口补齐订单/结算往返，与复盘共用同一零分自然收尾旅程。service-overview-order.png及service-recap-zero.png均显式540×960捕获并实际查看。自有Play已停止，原视图index6恢复，未改用户成功档。

本案4/4任务已完成，玩家用法及根进度同步，restaurant-ui主规格合并本案4项要求；48份主规格严格校验通过。未执行构建、提交、推送或归档，不把仍未实施的十二方向其余功能写成已支持。

## 2026-10-17 归档与提交

用户已明确授权本批归档及本地提交。任务和原逐项运行结果保留，主规格已核对；归档后全量严格检查74/74通过（50份主规格、24案）。仅调整规划状态与相对引用，未因归档重跑未变行为。提交范围排除工作区既有Android启动/后台、LAN和HandmadeBistro改动；本机设置与记录元数据依赖的平台存储定义及文件读写保留。不推送，设备、正式平衡和真人体验仍按原未验证边界。
