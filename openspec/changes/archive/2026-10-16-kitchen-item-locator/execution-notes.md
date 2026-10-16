# 探索与执行说明

## 最新交付与逐项验收

| 任务／Scenario | 实际用例或操作 | 最新结果 |
|---|---|---|
| 1.1／1.2 容器、手持、筛选、唯一实例和暂停 | `EntryItemLocatorFollowsContainerHandAndActorWithoutRebuildingButtonsOrSendingActions`：原拿鸡蛋／加入碗，名称和类别过滤、结果总数、外层宿主；原拿碗和真实移动后跟随持物者，纯移动不重建按钮；暂停阅读、池化及旧回调 | Passed |
| 1.1 脏／净池和地面、不自动取用 | `EntryItemLocatorShowsActualSavedPoolsAndGroundWithoutPickingBeforeExplicitAction`：实际合法文件恢复三种位置，查找不拿物；关闭后原拾取才将同一落物入手 | Passed |
| 1.1／1.2 锁定、真实消费和失效 | `EntryItemLocatorTracksRealJuicerLockAndLiquidConsumptionWithoutSelectingAnotherItem`：原榨汁加工期间定位同一输入，完成后解除锁定说明；真实盛杯后保持查找，用原水池请求消耗液体，目标栏显示消失、取消旧高亮且不改选其他物品 | Passed |
| 1.2 Host与真实KCP队友容器落物 | `EntryItemLocatorActualKcpTeammateContainerFallsOnDisconnectAndHostQueryFollowsIt`：远端实际拿蛋、入碗、持碗；Host定位容器内原料；真实断线后同一容器落地，内容及唯一身份保持，查询跟随地面格，共享sender未停 | Passed |
| 原动作／分组边界 | `EntryActionNavigationFiltersOriginalButtonsWithoutCommandsAndKeepsCurrentTarget`：筛选原动作、原拿放、暂停和池化 | Passed |
| 2.1 540×960普通入口 | Launcher→单机内存试玩→物品查找→按“碗”查找并选第一实例；实际碗台(3,11)高亮可见，展开／收起说明保留选择，命令始终0 | 通过 |

四个不同新增UTF最新Passed。名称失败后只修夹具并重跑失败项；地图可见和说明切换调整后，只补跑本机通用路径、消费失效和受变原动作导航，事实及业务路径未变的KCP／文件位置结果复用。所有有效运行Console覆盖完整、AutoTesting退出0，原始输出 `Unity/Temp/pipeline_test_status.json` 由工具保留原位，不复制或改写。

最终截图 `Unity/Assets/Temp/entry-item-locator-map.png`、`entry-item-locator-details.png` 已实际查看：原滚动列表可访问分类与结果，地图高亮、目标栏和说明可读，原查看详情切换可点。产品操作说明与当前进度已同步；没有改表、Prefab、CodeBind或Entity类型／资源。不声称真人理解、设备或物理LAN验收，没有提交／推送／归档。

## 2026-10-16 实施进展与首跑

最新补验：持物／移动用例在地图可见及展开说明调整后1/1 Passed；消费用例补充“已消失”的目标栏断言后1/1 Passed；受变的 `EntryActionNavigationFiltersOriginalButtonsWithoutCommandsAndKeepsCurrentTarget` 1/1 Passed，纯分类仍筛选原按钮、无命令、主要动作保持。全部Console覆盖完整、退出0，未重跑已覆盖且行为未变的KCP断线及池／地面文件用例。四个不同新增用例的最新结果均有效通过。540×960入口检查仍在收尾。

名称夹具改用实际投影值后，加工／消费用例新运行1/1 Passed，Console覆盖完整、退出0；原三项通过结果可复用。为让地图高亮真正可见，选中结果后收起原大详情区，目标栏保留当前宿主和格子，原“查看详情／收起详情”只切换查找说明，不丢失选择。没有更改Prefab结构或新建视图owner；对应持物／移动用例补充地图可见和详情开关断言并运行。

已沿原1101 HUD及Board.Render高亮参数接入Items页、名称／生成枚举类别筛选、唯一ItemId选择。沿容器外层链解析台面、手持、地面和餐具池，位置与锁定状态只读当前投影；不增库存索引、不发查找请求、不代取物或移动角色。持物者Cell只进入信息依赖，纯移动不重建动作按钮。HUD／关卡／世界换轮和原详情互斥按原owner清理。

PlayMode `EntryItemLocator` 首跑3/4：本机容器→手持→角色移动／暂停／池化通过；真实有效文件恢复脏／净池和地面，再显式原拾取通过；真实KCP队友持物→断线同一容器落地、Host查询跟随且共享sender存活通过。锁定／消费用例在 `ItemLocatorUiTests.cs:145` 的结果查找失败（Expected physical item is in this query，index0），夹具名称硬编码“橙子块”未匹配当前实际名称；改为原投影item.Name输入，再仅跑该失败项。未扩大字符串匹配或改业务规则，任务暂未勾选。原生输出 `Unity/Temp/pipeline_test_status.json` 保留工具原位，不复制／改写。

## 2026-10-16 依据与决定

物品已真实分布在工位、角色手上、容器、餐具池和地面，当前详情聚焦选中工位。查找某份材料或餐具需要逐处看场景。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationInformation.cs)、[主规格](../../../specs/restaurant-ui/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：沿 Items／Actors／Stations／作业投影解析位置链，以 ItemId 和当前世界代次定位结果；复用原目标详情和 Board 高亮，不添加全仓索引或持久缓存。物品转移时更新／失效选中结果。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

主要风险为把实物数量或位置查询变成第二套可消费库存，后果是显示误导或操作失效，属于可恢复的局部体验问题。采用原 owner、只读派生和原命令入口，最小相关 PlayMode UTF 加普通入口检查足够，不做全仓回归。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | 各位置、容器链、锁定／消费／落物、暂停与旧结果失效；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

本案 proposal／design／specs／tasks 均已齐备，`openspec validate kitchen-item-locator --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
