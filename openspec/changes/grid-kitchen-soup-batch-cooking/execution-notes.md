# 探索与执行说明

当前14/14任务完成，已按后续九案apply授权实施，具体功能／生成／UTF及普通Launcher结果见本页后段“实施”记录。后续持续厨房、前厅、buff与完整UI交接已经完成，见[关卡记录](../solo-restaurant-level-flow/execution-notes.md)与[UI记录](../solo-restaurant-ui/execution-notes.md)。下面早期仅规划／NotRun及单独制作范围保留为历史，不代表当前九案状态；主规格同步、归档和提交未执行。

## 本轮状态

2026-10-11：用户授权根据长期目标探索并建立 change，明确不施工。只读取本工作区资料／源码，新增本 change 的规划产物；没有修改其他会话的 change、业务源码、表、协议或资源，没有启动／停止 Editor、运行生成或功能测试，也未提交／归档。

2026-10-12：按用户后续“暂时只考虑单机部分”修订本提案，去除新增 KCP／双端实施与验收，更新已归档备料引用。备料的历史联机记录保留原位；本 change 仍仅规划，煮汤全部功能 NotRun。

## 方向选择与决策账本

| 方向／决定 | 依据与取舍 |
| --- | --- |
| 选择煮汤与按份盛装 | [长期目标](../../../long-term-goals.md) 的制作／交接；[产品设计 D4–D9](../../../Docs/Product/design.md) 已确认可搬锅自动加工、成品晚生成与份数守恒。直接承接已归档备料输出。 |
| 暂缓订单／前厅 | 出餐需要真实碗中成品；顾客、询问、餐桌、伙伴及自然收尾会同时扩大范围。先完成厨房制作片段，之后再探索最小顾客订单交付与餐具回收。 |
| 暂缓经营／供应／存档 | 需要正式 RestaurantRuntime／Level 与跨关边界，还会触及许可、收口或存档协议；不作为这次制作片段的前置。 |
| 延用临时 GridKitchen owner | 当前源码和 [grid-kitchen 主规格](../../specs/grid-kitchen/spec.md) 已有 Match Component、四邻交互、固定 Tick 与独立投影。无需先重构正式餐厅。 |
| 前置顺序交接 | [备料设计](../archive/2026-10-11-grid-kitchen-prep-processing/design.md) 和 [实际验收](../archive/2026-10-11-grid-kitchen-prep-processing/execution-notes.md) 已有真实内容／手工／协议 3 证据；核对当前实现后复用未变结果，不能据文件存在自动通过汤。 |
| 相邻范围 | 本 change 不接全局暂停、NPC 或跨关；用户后续暂停／人工自然收尾的已裁决规则由 [流程 change](../solo-restaurant-level-flow/design.md) 接入。正式传输与跨关许可收窄仍另定。 |

候选目录 F04 是番茄汤，使用洋葱和汤底，**不是本轮番茄蛋花汤**；依据 [目录区别](../../../Docs/Product/catalog.md) 与产品 D9，不借此引入另一配方。

## 可调样机参数

以下为可调初值，不是正式产量、人数、容量或平衡：50ms Tick、煮制 120 Tick、一批 2 份、锅容量 2，番茄块×1＋蛋液×1，增加一锅和第二只空碗。两碗足以单机观察 2→1→0；盛一份后只剩一个空碗，不能拿此生产场景宣称双人争末份。竞争仅用足够空碗的局部本地 UTF，不增加生产第三碗。没有自动补原料、碗或批次。

本轮不增加脏净状态或回收池；样机碗均为可复用空碗／盛汤碗，实际交给顾客后的变脏与洗碗留给后续 change。所有参数通过现有配置链定义，测试不向生产入口注入假配置。

## 严谨程度与验证成本

这是可恢复的局部玩法增量，没有产品存档或线上资产；主要风险是材料双重占位、拒绝丢料、末份重复成果和角色退出错误停煮。少量真实本地 UTF 覆盖关系与生命周期，再经普通 Launcher Solo 完整输入足够。不跑新增 KCP／联网或全仓矩阵；源表仍须保护、Check 后导出并核对真实日志／diff。

可复用前置已验收且未改变的切配、打蛋、单槽和基础成员结果，以归档中的具体用例及原输出为准。本轮新锅、批次、DTO 和目标动作必须取得新运行证据。覆盖齐全后不因文档整理重跑，不设性能百分比或全仓门槛。

接受联机／KCP、手机／Player／AOT、双 Editor、LAN／公网、正式跨关和全局暂停未验证。后续涉及这些入口或出现具体回归再另案补验，不以未验项阻止规划交付。

## 任务／功能验收映射

测试名前缀均为**拟新增**，不是当前存在或已通过用例。后续 apply 通过现有 AutoTesting 发现实际模式和名称，再按最小相关本地过滤执行。

| 任务／功能 | 后续 UTF 或实际操作 | 当前结果 |
| --- | --- | --- |
| 1 配置、锅和初始物资 | 源表回读、五目标 Luban Check／导出；必需定义有效／缺失断言 | NotRun |
| 2.1–2.2 蛋液倾倒／番茄块添加 | 拟 `GridSoupTransfer*`：同一材料入锅、原碗空出、次序无关；错误／重复／旧目标无部分写入 | NotRun |
| 2.3 自动加工／锁定 | 拟 `GridSoupCooking*`：开始 Tick 不推进、到点完成、移锅继续、不能提前盛装／重复启动／改输入 | NotRun |
| 2.4 有限批次盛装 | 拟 `GridSoupPortion*`：2→1→0、末份争抢、拒绝不扣、最后消费输入／同锅复位、新批次拒绝旧 BatchId | NotRun |
| 2.5 成员与整轮退出 | 同 Cooking／Portion 小集：本地 Leave 落物继续煮、合法角色同格拾锅、重复收尾、整体 Stop 无旧成果 | NotRun |
| 3 本地消息／独立投影 | 拟 `GridSoupLocal*`：真实 Solo 倾倒／煮制／盛装、池化回收后有效、重复／旧世界／旧版本拒绝 | NotRun |
| 4 HUD／地图／输入 | 普通 Launcher Solo 实际备料→倒蛋液→加料→启动→搬锅→两次盛装→交接；验证动作提示与不穿透 | NotRun |
| 4 界面退出 | 普通 Solo 受管视图关闭重开单次提交、旧世界及旧批次隔离 | NotRun |
| 5 文档与规格 | 严格 OpenSpec 格式、引用及范围一致性检查 | 本轮规划检查 Passed；实施后的文档／功能收口仍 NotRun |

原始测试输出未来保留在工具原目录，本页仅填结果、失败原因与路径。静态规划检查不证明功能已验收。

2026-10-12 修订后 `openspec validate grid-kitchen-soup-batch-cooking --strict` 返回有效、退出码 0，4/4 规划产物齐全；关联九案共 46 份 Markdown 本地引用、UTF-8 与行尾空白检查通过。14 个实施任务全部未勾选。规划检查不能证明煮汤／盛装通过；备料自身已有归档运行证据。

## 2026-10-12 授权实施与实际验收

用户后续明确要求完成九个 change，已进入 apply；以上 NotRun 表保留为提案时的历史记录，下表是当前实施结果。共享表／协议／Tick／HUD 在本会话顺序修改，前置备料已归档且没有并行手写业务改动。没有提交、归档、主规格同步或操作第二工作区。

配置源回读为七种物品、一锅两碗、c,s,e 自动配方注册，输入 2,4／数量 1,1，120 Tick／2 份／盛装定义 5。源工作簿使用标准库 ZIP/XML 修改，保留其它 ZIP 条目；正常五目标导出和 Proto2CS 已完成，当前三份自动配方 bytes 相同、实际 Tables 与源一致。新增断言后再次只读 Check 五目标 5/5，退出码 0；未重复导出。环境仅有 .NET 10，现有 Tool 用本 shell 的 `DOTNET_ROLL_FORWARD=Major` 运行，没有安装依赖。

| 任务／功能 | 实际用例或操作 | 结果 |
| --- | --- | --- |
| 1.1 备料交接 | 对照备料归档的 Container／Work／本地消息结果和当前实现；汤用例真实切配及打蛋，无替代手工系统 | Passed；前置原始路径和失败历史仍见归档 |
| 1.2–1.3 配置、初始物资与建树 | `GridSoupConfigAndActualInitialSupplies`；`GridSoupConfigRejectsMalformedCountsAndIncompatibleOutput` 检查数量缺项／非正／重复材料／超过容量／输出不兼容；源回读、正常生成、五目标 Check、Unity 编译 | Passed |
| 2.1–2.2 实物转移与批次归属 | `GridSoupTransferOrdersPreserveBowlAndActualMaterials(False/True)`；`GridSoupTransferRejectionsKeepBothSides`；`GridSoupTransferFullPotAndCookingLocksKeepMaterial` | Passed；两顺序、原碗／蛋液 ID、完整拒绝、满锅和锁定保留两端 |
| 2.3 显式自动煮制 | `GridSoupCookingLocksInputsAndMovesOriginalPot` 及上述 FullPot 用例 | Passed；开始 Tick 为 0、搬锅保持批次、原灶台占用、缺料／错工位／早盛装拒绝、到点仅 Ready |
| 2.4 逐份和新旧批次 | `GridSoupPortionsConsumeInputsOnLastAndRejectOldBatch`；`GridSoupPortionLastRaceUsesRealLocalRequests` | Passed；2→1→0、满碗拒绝、末份仅一个赢、消费原输入并复位同锅、新批次不同 ID 且旧批次不扣份 |
| 2.5 退出与作业寿命 | `GridSoupCookingLocalLeaveDropsOnceAndWholeStopDisposesProcess`；原 `GridCoreItemRaceDropIdentityAndSameCellPickup` 收尾保护；普通 Launcher 停止重开 | Passed；离开落锅后继续、合法同格拾取、整体释放且新轮无旧汤／作业 |
| 3.1–3.2 本地消息／独立投影 | `GridSoupLocalReplayContextAndRecycledProjection`；Transfer／Portion 的真实 handler 链；源／目标／Process 身份随意图及消息复制 | Passed；开始与盛装重放只执行一次、同编号改批次冲突、旧协议／世界／绑定拒绝、旧快照拒绝、DTO 回收后内容／进度／份数仍有效 |
| 3.2 最小前置回归 | `GridPrepContainerDefinitionsAndUniqueContents`、`GridPrepContainerRejectionsDoNotTransfer`，四项 `GridCore*` | 2/2＋4/4 Passed；未跑 GridPrepWire／GridWireKcp 或全仓套件 |
| 4.1–4.2 普通输入与显示 | `GridSoupSoloAcceptance.Prepare/CookMoveAndPortion` 经 Button PointerClick 和 Board Camera 坐标→`ProcessGridPointer` 完成整链，20 个单次命令；地图断言煮制、Ready 2／1 份及汤名可见字符数 | Passed；实际原材料加工，无直接写物品或进度；两碗独立汤、同锅为空、成品普通交接且无订单完成提示 |
| 4.1–4.2 受管重开与新轮 | `ReopenAndCheckShield` 验证旧按钮销毁、重开不发动作、HUD 不穿透、新按钮一次；`NewRoundAndDirectPot` 验证新世界、初始空碗、缺料可读拒绝与直接拿锅 | Passed；没有残留旧批次、订阅或成品 |
| 5 集成与文档 | 最终编译、Console 完整范围、文档／引用与严格格式 | 功能证据齐全；严格格式和最终静态检查结果见末尾 |

PlayMode 实际发现 61 项，按 `ET.ProductSessionTests.GridSoup` 执行当时 10 项 10/10 Passed，再仅跑新增 FullPot 用例 1/1 Passed；共 11 个煮汤用例各有 Passed。相关 Container 2/2、Core 4/4 均通过，Console 覆盖由 AutoTesting 判定完整，返回 0。原始结果留在 `Unity/Temp/pipeline_test_status.json`，原生新运行会覆盖该文件；未复制或改写结果。原日志为 `Unity/Temp/solo-nine-changes-editor-interactive.log`。

普通 Launcher 第一完整轮 World `2d07af5a4a3d435a8ce4a87cf224a4cd`；新轮 `f80b79c6548c4303a2e8397567b4efdd` 验证缺料和直接拿锅。发现地图长名被截断后仅修改标签尺寸／字体并加入自动进度显示，随后新轮 `cbf7c3496a8e4ee39b0797e7a450ba49` 完整复验整链和受管重开通过，不重跑未变领域 UTF。截图 `Unity/Assets/Temp/pipeline-screenshots/grid-soup-labels-accepted.png` 已查看；捕获使用实际 1469×719 Game view 尺寸，早期 540×960 导出仅是图片缩放，未冒称实际竖屏分辨率。

临时验收源在 `Unity/Temp/GridSoupSoloAcceptance.cs`，Pipeline 内存运行。脚本首次有 EntityRef/null 类型错误，改为明确解析原实例后编译成功；一次紧随 Play 的连接重载失败，先只读 Inspect 证实新轮 command=0 才重新执行，未重发已提交链。没有改业务状态完成验收。

本轮限制仍是 Solo 制作片段。真实前厅／订单／餐具回收、正式关卡暂停与跨关由后续 change 实施；KCP 新动作、双 Editor、手机／Player／AOT／LAN／公网仍 NotRun。有限初始库存不会免费补充；额外原料／碗只用于局部批次和竞争 UTF 的初始 fixture，不进入生产。若相关新入口或共享 owner 改动，按具体变化补验，不预先跑全仓。

最终 Console 检查曾发现 Pipeline 捕获 Error=0、原生 Error=1 的差异；通过已安装包的 `EditorConsoleEntries.TryReadRecent` 只读查到唯一原生错误 `Failed to handle /api/exec request: Thread was being aborted.`，堆栈在 `BasePipelineServer.cs:2257`，无游戏帧，推断对应紧随 Play 的请求重载失败。此轮不称 Console 全绿，也不修改排除规则、包源码或清 Console。

随后等待实际 Editor playing／domainReloadInProgress=false，再取原生 Error=0 和 cursor=380 的基线。新 World `5b5b28ea61de4a298e9f5d4d46152f42` 再经同一实际输入完整制作、搬锅、两份盛装及受管重开通过。结束后等延迟错误窗口，Console session `68cf1167f0ba4ee692e314b87dcdb762` 的 seq 381–408 共 28 条连续、无 reset／dropped，原生 Error=0、compilationFailed=false；存在一条已定位本地请求超过 200ms 的 Warning，未称零警告。最后 Editor ready、Play stopped，recompile_status completed／failed=false。原工具错误及先前请求超时 Warning 保留在原记录，不改写历史。

最终静态：两模式原 72 个 opcode 均保持、新 `GridAutoProcessState=10073` 无重复；五份 change Markdown 的 UTF-8／尾空白和 24 个相对引用通过；手写业务、测试与相关文档的局部 `git diff --check` 返回 0（有 CRLF 提示，未扩到既有生成物）；`openspec validate grid-kitchen-soup-batch-cooking --strict` Passed、退出码 0。没有手改生成物或为了格式重跑领域用例。
