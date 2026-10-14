# 执行说明

## 2026-10-14 实施与验收完成

用户已授权完成本案及另外六个指定 change。保留主工作区 `main` 与用户文件 `untitled.md`；未提交、归档或操作第二工作区。

原 UI1101 / GridKitchenBoard owner 与注册保持。通过本工程 Unity CLI 的安全 Prefab API 调整原 HUD，正常 CodeBind 菜单生成并序列化 23 个字段（新增 TargetText、PrimaryActions、DetailsPanel、DetailsText）。顶区阶段／时间／得分，中区地图，底区手持／目标／主要动作与其他动作滚动区；详情覆盖层只读取当前投影，订单可折叠。Board 精简容器内容标签；选中工位的完整内容及加工改在详情查看。

主要风险仍为输入遮挡、绑定遗漏与池重用旧回调。采用真实资源断言、相关 UI PlayMode 与一次普通 540×960 Launcher 操作足够；不扩大领域算法回归。权限裁决与传输实现未修改，可复用 `cooperative-restaurant-core/execution-notes.md` 中 CoopCorePermissions 八例的实际 KCP 越权拒绝结果；本案另跑受影响远端 UI。手机／Player／LAN 输入保持 NotRun，出现新设备、owner 或协议变化才补对应测试。

| 任务／功能 | 实际验收 | 当前结果 |
| --- | --- | --- |
| 1.1 原资源／Canvas／CodeBind／射线／池重用 | SoloRestaurantUiBindingsRealPrefabHasGeneratedFieldsAndOriginalCanvas；SoloRestaurantUiLifecycleInputAndProjectionWithSoloBuffDrawReopen；SoloRestaurantUiLifecycleExitDuringActualInitialLoadDoesNotShowOrphanHud | 最终 6 项小集 Passed；真实菜单生成 4 个新增引用均非空 |
| 2.1 等待／拒绝／加工与只读详情 | SoloRestaurantUiInformationHierarchyTargetDetailsAndPrimaryActionUseCurrentProjection；SoloRestaurantUiProjectionRealOrderRejectionPauseAndOldLevelButton；SoloRestaurantUiProjectionActualBakingSeparatesReceiptProgressAndCompletion；SoloCleanupUiActualSinkButtonsApproachWrongTrashAndManagedReopen | Passed，主要动作固定原工位／物品，详情开关不发令、旧详情回调无效；真实加工／暂停／完成及拒绝保持物品；普通两菜输入完成 |
| 2.1／3.1 远端 UI 与旧 callback | CoopCoreUiRemoteManagedReopenAndRetiredHostCallbacksKeepTheirOwners | Passed 1/1，真实 KCP Client 没有 Host 控制；旧 Host／远端 callback 不发令，原 Mono 池重用 |
| 3.1 540×960 普通 Launcher 两菜／前厅／拖动 | 以下普通 Launcher 记录；上述相关 UI 小集 | Passed，两菜真实制作与分别交付、清桌／洗净、拖动不发令、跨关原实物 |
| 4.1 用法／逐功能记录 | 本节、Book/UI开发.md、restaurant-ui 主规格 | Passed，正文／引用／严格格式核对；手机触控／Player／LAN 为 NotRun |

模式 PlayMode，精确过滤 `ET.SoloRestaurantUiTests.SoloRestaurantUi`：首次 5/5 Passed，原作业 duration 69.97 秒；过滤 `ET.SoloRestaurantUiTests.CoopCoreUi`：1/1 Passed；过滤 `ET.SoloRestaurantUiTests.SoloCleanupUi`：1/1 Passed。三轮 AutoTesting 均退出 0，Console 覆盖完整、无新增 Error。Pipeline 原始结果保持 `Unity/Temp/pipeline_test_status.json` 原位；后续作业会按原管线更新该固定文件，未另建结果副本。

普通入口首次已实际进入 540×960 内存试玩，截图发现部分地图多行标签重叠，已进一步精简为工位名称与必要加工进度。此后发生延后测试程序集编译／域重载，运行中 ET 与 GF 单例丢失，原日志出现 Init.Runner 和 EditorResourceComponent 空引用；已退出本次自建试玩，未清 Console。首次 5 项结果不能证明尚未导入的新增详情断言，因此执行了下面的最终 6 项新运行。

最终源码显式 recompile 完成且 failed=false 后，发现新增 InformationHierarchy 用例（PlayMode 共 421 个真实方法，本次只选 6 个）。过滤 `ET.SoloRestaurantUiTests.SoloRestaurantUi` 新运行 6/6 Passed，duration 74.22 秒，AutoTesting 退出 0，完整 Console 覆盖。新增详情／主要动作／旧详情 callback 断言及扩展烘焙详情断言均在本轮真实执行。远端与清理的有效结果对应未改变的输入、owner、命令及权限实现；之后仅精简地图标签与背景，不重复这些用例。

### 普通 Launcher 540×960 实操

通过原普通 Launcher 进入内存试玩，世界 `23f2dd66b1714862ab73cba4bbd5dcca`。每个按钮先滚动至可见位置并经真实 GraphicRaycaster 核对可达，再发送 PointerClick；每次仅增加一个 CommandId，逐次等待实际 Executed，不写 Host 状态、不直接推进业务 Tick。

- 命令 1–13：选择加成、拿番茄／切配／入锅、鸡蛋入碗／手持打蛋／倒蛋液、显式煮汤；逐个等待真实半成品与 Ready 批次。
- 命令 14–23：原空碗盛汤后暂存；拿面包／放烤箱／显式烘烤／等待新烤面包／入原盘，保留盘装成品。选中工位的唯一动作放在 PrimaryActions，其他目标滚动按钮仍可达。
- 命令 24–32：开店，真实汤订单交付一次得 10 分；拖动前厅视野再恢复，拖动不增加命令，地面单击仅一条并到 `(3,10)`；放回吐司、玩家清桌，之后实际 Wash 完成，原碗进入干净池。本关自然 Completed，10 分／1 星，顾客已退出。
- 命令 33–44：沿原入口跨关保留同一盘装吐司，最终一轮在真实有效吐司订单出现后立即点击实际对应餐桌的前往／交付，命令 43／44 均 Executed，另一次关内得 10 分。两次早期脚本错过吐司订单窗口而找不到有效按钮，只是未执行该交付，不计成功；后续明确的一次有效交付才计入验收。
- 命令 45：真实暂停收尾。读取当前阶段／时钟／得分及目标；原生 Pipeline 截图 `Unity/Assets/Temp/pipeline-screenshots/ui-hierarchy-prepared.png` 与 `ui-hierarchy-front.png` 已查看，仅证明显示范围。原生 ScreenCapture 在非帧尾失败一次，未作证据，改用已接入 Pipeline 的 screen 捕获成功。

本次已停止自己启动的内存试玩，恢复原 GameView 选项（Free Aspect）并移除仅本轮加入的 540×960 选项。当前原生 Console 仍保留截图失败和前次重载退出错误，历史错误不称全绿；最终正确入口期间没有新增业务空引用。真实 KCP 结果只证明同 Editor 的远端路径，不证明 LAN／独立 Player。没有提交、归档或改用户存档。

### 规格与自审

三项新增要求已合入 `openspec/specs/restaurant-ui/spec.md`，原有五项要求保留。对照三项 delta 的场景：选中加工工位由新 InformationHierarchy／扩展烘焙详情断言覆盖；拖动后操作由生命周期输入 UTF 与普通地图输入覆盖；接受未到位／实际拒绝由订单与设施 UI UTF 覆盖。owner 树、原消息链、资源 ID 与权限未改变，任务 4/4；手机、Player、LAN 按设计仍在后续设备案，不进入本案完成结论。

## 规划阶段状态与决策（历史）

2026-10-13：用户授权“由浅到深写成 change”，并要求真机测试最后。本案目前仅为规划；所有 tasks 未勾选，未实现业务、未执行 UTF、未启动 Unity/Player、未构建或操作第二工作区。第三关保存完成后返回选档、无装修和第四关；橙汁加工锁壶、完成可搬壶分杯、产量/耗时配表，已获用户确认。具体相关合同以本案 specs/design 为准。

## 相称的严谨程度

结构变化可能留下旧绑定、遮挡输入或使池化按钮重复订阅。真实 Prefab／CodeBind 检查、UI 小集与普通入口点击拖动足够；真机尺寸与触控在最后补验。 所选验收按下表覆盖本次改变的行为，已有结果只有在实现/行为未变且明确对应该点时才复用，并填写原执行记录路径。不因提案完成或阶段切换重跑全仓；测试失败、空集、Skipped、缺输出均不算成功。

接受的未验证范围：本轮所有运行行为 NotRun；除最后设备层外，手机/独立 Player/LAN 仍待对应案。没有玩家、设备或已授权运行条件时保留相关任务未完成。若改到共享 owner、文件格式、协议、行为断言或出现实际回归，则补对应局部验证，不机械放大为完整矩阵。

## 任务与验收映射

| 任务 | 功能／交付 | 拟使用的最小验收 | 本轮结果 |
| --- | --- | --- | --- |
| 1.1 | 层次与绑定 | 核对 Prefab/CodeBind/Canvas/射线与真实引用，并运行受影响关闭重开和池复用用例；记录新区域用途。 | NotRun（规划阶段） |
| 2.1 | 动作与反馈 | 真实点击验证等待/成功/拒绝，UTF 检查单次发令、旧回调无效和 Client 越权拒绝，更新用法。 | NotRun（规划阶段） |
| 3.1 | 入口集成 | 按发现结果执行相关 PlayMode，并在 540×960 普通 Launcher 用真实输入完成两菜/前厅及拖动。 | NotRun（规划阶段） |
| 4.1 | 范围记录 | 核对逐功能运行映射，截图只说明显示；真机触控仍记 NotRun。 | NotRun（规划阶段） |

UTF 执行阶段使用现有 Tools/AutoTesting，先发现模式与方法再记录实际过滤范围；涉及产品消息/UI 的候选集按 PlayMode 核对，目录名不能证明模式。原始输出保留原位，只在此引用运行路径及功能对应关系。纯正文任务只检查文本、链接与严格格式，不制造占位 UTF。

## 实施时回填

每项记录“实际方法或真实操作 → 原输出路径 → Passed/Failed/NotRun/Skipped → 未覆盖点”。文档格式验证只证明规划可解析，不证明功能已完成。当前不预填执行日期、通过计数或不存在的测试结果。

## 本轮规划检查

2026-10-13：`openspec validate restaurant-ui-information-hierarchy --strict --no-interactive` 退出码 0，规划格式通过。当前相关文档检查共 80 份 Markdown，链接缺失 0、编码/尾随空格/原规格场景遗漏 0；新案依赖无环。以上均为静态规划检查，表内功能仍全部 NotRun，任务未勾选。

## 2026-10-14 归档核对

用户已授权归档、提交并推送。任务均已闭环；复用上文适用于当前实现的 UTF 与普通入口运行结果，本轮归档未改变功能，不重复运行产品测试。主规格已同步，七案共33项增量要求核对通过，34份主规格严格验证通过。
