# 执行说明

## 归档记录

2026-10-21：用户明确授权归档、提交和push。两案均5/5完成，13条delta要求已同步3份主规格，52份主规格严格校验通过；原任务与运行证据逐项对应，复用本轮已通过且行为未变的UTF／Launcher结果，不为归档重跑。独立Player、真机与物理LAN仍NotRun，归档不扩大验证结论。当前目录为 `openspec/changes/archive/2026-10-21-product-entry-flow-clarity`；本轮提交只包含这两案所需内容，Android／LAN其他未完成改动和用户现场保留在工作区。

## 实施验收状态（归档前）

2026-10-21：用户授权实施两案。本案5/5已实施并完成当前Editor范围内验收；逐项结果见下表，独立Player未运行。

首轮 EntryFlowClarity：3项中2 Passed、1 Failed。失败位于测试仓库替换后的等待条件，断言前仍显示旧仓库数量；修正为同时等待当前仓库实例，单项新运行1/1 Passed。补充完成记录只读路径后发现从空列表返回内容页的动作容器激活时机，已在添加首个入口条目时显式恢复该内部区域。最新540×960运行3/3 Passed，Console覆盖完整，退出码0；失败输出未改写。

已通过 Unity recompile_status 与 Console groundTruth 编译核对。初次新增调用触发的 FriendOf 访问诊断改为所属 System 导航接口；新增测试遗漏的 using 已补齐；未清 Console。

## 严谨程度与验证取舍

本案主要是可回退的入口表现和导航，主要后果为漏入口、选错章节或旧回调重复开局；继续入口还需要保护真实记录。验证聚焦完整入口可达、明确确认、记录读取状态、一个适用真实 KCP 路径及原 UI owner 交接，配合代表视口的真实输入即可；不重跑全菜谱、性能或全仓套件。未改动且证据完整的保存／配方／房间业务可在实施时注明原结果复用，改变的新导航和布局必须补跑。

接受本案不含独立 Player、Android 真机、物理 LAN 和真人新手理解结论；后续交付相应包体／设备，或出现实际输入与 Editor 不同的证据时补验。当前没有复用旧结果冒充本案验收。

## 功能验收映射

表中均为现有Tools/AutoTesting实际发现并执行的PlayMode用例，不以文件存在代替通过证据。

| 任务／功能 | 实际 UTF 或操作 | 当前结果 |
| --- | --- | --- |
| 1.1 首页主次、原入口全可达、浏览无副作用 | EntryFlowHomeHasThreePrimaryActionsAndRetainsSecondaryNavigation：普通 Launcher／真实射线导航设置、进度与试玩；三项主操作、旧回调及无世界断言 | Passed |
| 1.2 同页章节预览、精确确认、取消和旧按钮 | EntryFlowInlineChaptersCancelRetiredChoiceAndStartExactChapter：当前三章逐项选择、取消回真正首页、旧按钮失效、改选第二章精确开局与真实拿碗 | Passed，首次3840×2160；代表视口补验见2.1 |
| 1.3 读取状态、明确旧档恢复、完成记录只读 | EntryFlowRecordsFailureRetryEmptyAndExplicitOldSaveRestore：真实文件空集→目录IO失败→重试两份继续＋一份完成记录，完成只读，明确选择原旧档恢复并拿碗，记录总数不变 | Passed |
| 1.4 关闭重开、加载退出、合作及显式启动不变 | ProductEntryUiOrdinaryHostRoomRealKcpReopenAndRetiredWorldCallbacks：真实KCP提案／准备／开局、回房重开和旧世界拒绝；SoloRestaurantUiLifecycleExitDuringActualInitialLoadDoesNotShowOrphanHud：真实加载中退出，无孤立UI | 两个单项新运行均Passed，Console完整；显式角色解析代码未改，原角色交接证据仍适用 |
| 2.1 原入口串联和代表环境真实输入 | EntryFlow 三项均由普通 Launcher 与真实射线输入执行；InlineChapters 在1920×1080、540×960分别新运行Passed，使用测试所属仓库保持最大字号，检查非零安全区、当前三章、取消回首页及开局后拿碗 | Passed；布局与输入运行不以截图替代 |

## 规划检查

- `openspec validate product-entry-flow-clarity --strict`：Passed，退出码0。
- 相对引用、proposal 能力与 delta 对应、任务映射已核对；本案规划4/4齐备，实施任务5/5完成。
- 本轮实施原入口与相关测试；不修改源表和生成配置，不提交、推送或归档。
- 运行原输出：`Unity/Temp/pipeline_test_status.json`；历史过程保留原 Editor 日志。本案未修改 Prefab 结构和 CodeBind 字段，入口分区由原 Mono 布局作用于原 Prefab 实例，无需重生成绑定或导表。
- 最大字号补验的初次运行因夹具换库会关闭原选档HUD而失败，已把换库移至打开章节页之前；修正后宽屏／竖屏两个单项均Passed。未修改失败输出，未扩大全仓测试。
- 首页设置自动初始化同步改为只读：空目录不创建本机身份。该行为由 Home 用例的实际空路径／无Repository／无目录断言验证；设置的显式打开／保存仍走原入口。
- 实际屏幕检查发现竖屏Canvas逻辑宽1080也超过旧分栏阈值，已改为同时判断宽高比例；首页按可用高度放大三项主按钮，并收起没有按钮的底部导航。最终540×960的EntryFlowClarity扩展组5/5 Passed，1920×1080的InlineChapters再次1/1 Passed，包含正确分栏断言、最大字号及非零安全区。
- 原FirstSessionPath与Solo开局预览测试已同步同页章节按钮和保留试玩路径；旧通用选择器夹具明确导航到仍保留的Home子页，新普通入口由EntryFlow用例验证。新增到本组的两项均Passed，不留下已知旧页面假设冒充验收。
- 最终两案严格校验Passed、23处相对引用全部存在、相关diff空白检查Passed。按openspec-verify-change核对当前范围的任务、规格与设计，无阻断问题；独立Player等未验证范围保持原说明，不宣称包体验收或归档就绪。
