# 探索与执行说明

2026-10-16 原18案收尾复核补验：在现有Solo原动作导航用例增加原ScrollRect的真实帧断言，将列表滚动到0.35后让投影/界面实际刷新，位置保持；原过滤、拿放、暂停、池重开和拖动链继续执行。仅该受变单项新运行1/1 Passed、Console完整且退出0，未重复远端用例。源码未改变分组或滚动合同；本批设计图同时澄清角色实际挂Level、厨房只持索引。

## 2026-10-16 依据与决定

RefreshGridHud 为菜谱、采购、前厅和每个工位持续追加动作，虽有主要动作区，全部动作仍混排。内容增加后寻找目标与保持按钮稳定的成本升高。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)、[主规格](../../../specs/restaurant-ui/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：复用 PrimaryActionsRectTransform、TargetsRectTransform 和原 AddTarget／AddControl，不改 Prefab／CodeBind。用具名分类枚举与原 action 生成路径分组，不复制另一套业务资格判定；显示 owner 与 guard 沿原 Presentation。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

主要风险为筛选漏掉原合法动作或高频重建打断点击，后果是显示误导或操作失效，属于可恢复的局部体验问题。采用原 owner、只读派生和原命令入口，最小相关 PlayMode UTF 加普通入口检查足够，不做全仓回归。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | 新旧动作集合对照、真实点击、拖动、暂停和 HUD 重开；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

## 2026-10-16 实施与失败诊断

已接入具名动作分类和原按钮的可见筛选，默认保留全部动作；当前目标主要动作在各分类仍可访问。分类切换直接更新原按钮，不重建请求或业务资格，不重建动作对象；被隐藏按钮的旧回调也不再可执行。

第一轮两个 `EntryActionNavigation*` PlayMode 用例中，Solo 的真实拿放／暂停／重开／拖动 Passed，KCP 失败在 HUD 资源占用并产生收尾 Error，整轮退出2。第二轮仅KCP重跑仍失败，证明“等待资源释放”不足：删除 Host 的 GridPresentationComponent 后，下一投影事件按原契约会重新创建它，重新占用 HUD。已据实际 GridProjectionView 源码修正夹具，改为保留 owner、仅 CloseGridHud，沿现有真实 KCP 夹具的显示交接方式。原两次失败不改写。

修复后创建的新运行仅选择 `EntryActionNavigationActualKcpFiltersAndExecutesOneOriginalPickup`，1/1 Passed、无新增Console错误、退出0；复用首轮唯一 Solo 用例 Passed，合计两个不同用例最新有效结果通过。功能映射：过滤前后原按钮实例集合保持、隐藏旧回调不提交、当前主要动作保留及真实原碗拿放→Solo用例；暂停、拖动、池化重开与旧分类回调→同一Solo用例；真实KCP读取分类不增命令、Client权限、一次真实拿碗及远端重开→KCP用例。原始结果仍在 `Unity/Temp/pipeline_test_status.json` 与当轮会话输出，未复制或改写。

普通 Launcher 已按真实按钮走主页→合作分组→返回→Solo选档→内存试玩，实际 GameView 为540×960；分类切换保持命令编号0，真实拿碗只分配编号1并最终 Executed、同一原碗入手。地图、主要动作、滚动分类入口均已实际检查；画面位于 `Unity/Assets/Temp/entry-kitchen-actions.png`／`entry-current-target.png`。这些实际操作与UTF共同覆盖本案新增行为，不声称真人理解、物理LAN或Android通过。操作说明已同步。

规划阶段历史记录（不代表当前状态）：本案 proposal／design／specs／tasks 均已齐备，`openspec validate kitchen-action-navigation --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
