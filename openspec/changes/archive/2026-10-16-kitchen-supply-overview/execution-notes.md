# 探索与执行说明

## 2026-10-16 实施与实际验收

已接入原补货动作路径。页面仅持来源/货物ID，配额、运输、待接收、已收货记录、当前包装和包内材料分别派生；原采购/接收/拆份命令与权限保持。包装入口只查看同一实际物品，不领取；暂停、收尾、结算仍能阅读，世界/Level与HUD换轮清理旧选择。

PlayMode过滤 `EntrySupplyOverview`：先实际发现3项，最新3/3 Passed、0 Failed/Skipped，Console覆盖完整，退出码0。原始输出保留 `Unity/Temp/pipeline_test_status.json`，未复制或改写；后续运行自然替换即时文件。

| 任务／功能 | 实际用例／操作 | 结果 |
|---|---|---|
| 1.1 配额/在途不作库存、满槽保留、接收及拆份守恒 | `EntrySupplyOverviewSoloSeparatesQuotaTransitPackageAndRejectsFullReceiverWithoutDuplication`：原采购预留5→4，满槽拒绝保持同一货物且PackageId=0，腾空后只生一个两份包装；拆份移动同一材料，剩余一份 | Passed |
| 1.1/1.2 暂停、配额耗尽、真实收尾/换关 | 上述Solo用例暂停运输冻结；`EntrySupplyOverviewExhaustedSourceRealDrainingAndNextLevelKeepFactsButClearSelection`实际采购5次、拒绝第6次；仅缩短测试自身OperatingTicks，真实顾客进入Draining再自然结算，换关保留原货物/配额并清除查看目标 | Passed |
| 1.2 Host本地/真实KCP Client、旧货物回调、一次点击一次请求 | `EntrySupplyOverviewActualKcpPurchaseReceiveSplitAndPackageLookupUseOneOriginalRequest`：保留Host owner交接单例HUD，远端真实采购/到货/接收/拆份；HUD重开旧行失效，包装定位零请求，Client不获得经营控制权 | Passed |
| 1.2/2.1 返回原制作 | Solo总览关闭重开后原切配真实产生番茄块；旧采购/接收/货物行不重复发请求 | Passed |
| 2.1 普通Launcher 540×960 | 原主页→单机→内存试玩→确认→补货总览→番茄补货，阅读时命令0；动作列表实际滚动可达采购，原一次采购/接收后命令2、Executed，显示配额4/5、记录1、实物包装1/材料2，收起详情返回地图 | 已实际操作并检查画面 |

画面留在 `Unity/Assets/Temp/entry-supply-overview.png`、`entry-supply-actions.png`、`entry-supply-received.png`。截图只证明本次可读布局，功能行为由上述实际用例证明；手动Play已经按本轮所有权停止。

首轮3项失败均停在来源按钮查找：测试用“来源·番茄/鸡蛋”作精确标题，实际有效配置名称为“番茄补货/鸡蛋补货”。已只修测试标题，产品代码与配置未为通过用例而改名；新运行3项全部通过。CLI status发现偶有空列表，但pipeline list精确项目及editor_status可达，未重启Editor或清Console。

严谨程度沿下文局部UI风险判断，验证覆盖真实守恒/拒绝和两端，不扩为全仓。真人上手、Player/LAN/Android真机均NotRun；原未变裁决复用主规格Sources已有证据，不据此扩大验收。

严格格式校验已通过。整理时将增量中“暂停/收尾不推进运输”的合并句澄清为暂停冻结、收尾禁新采购且继续已有运输，与既有主规格/源码一致；未改运输实现，不重复运行未变业务。下文保留建案阶段记录，当前验收以上方最新结果为准。

## 2026-10-16 依据与决定

采购按钮展示余包数，运输信息拼入 HintText，收货与拆包又在动作列表中。玩家很难同时区分配额、在途货、待收货与已经进厨房的材料。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSupplySystem.cs)、[主规格](../../../specs/kitchen-supply/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：复用 SupplySources／SupplyShipments／Items 以及实际 DTSupplyConfig；已接收货物与包装建立关联后不重复计数。原 SendSupplyCommand 为唯一命令入口，页面只持来源／货物选择，受原世界／Level／layout guard 约束。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

主要风险为把在途或剩余配额当作现成材料，或重复计算已收包装，后果是显示误导或操作失效，属于可恢复的局部体验问题。采用原 owner、只读派生和原命令入口，最小相关 PlayMode UTF 加普通入口检查足够，不做全仓回归。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | 真实采购／满槽／收货／拆份、暂停／跨关及本地与 KCP；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

本案 proposal／design／specs／tasks 均已齐备，`openspec validate kitchen-supply-overview --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
