# 探索与执行说明

## 2026-10-16 实施与真实验收

已在原HUD接入“前厅工作看板”，满手/工作中也可查看。仅保存选中WorkId，读取原FrontWorks、Customers、Tables、Companions和Actors；显示真实可认领/前往/执行/完成、执行者、进度、冻结耗时、顾客及餐具。原Claim/Stop按钮继续捕获WorkId/ClaimSerial；阅读不认领或派工。暂停可读，收尾不要求排空后厨，换关/关闭重开清除旧查看目标。

实际发现 `EntryFrontWorkboard` 两项PlayMode UTF。KCP用例最新Passed；单机补桌位/停手断言后只重跑 `EntryFrontWorkboardSolo`，最新1/1 Passed，Console覆盖完整、退出码0。两个不同用例均有适用的通过结果，未为整理重跑无变更的远端竞争。即时原始输出在 `Unity/Temp/pipeline_test_status.json`，按原工具自然更新，不复制结果包。

| 任务／功能与Scenario | 真实用例／实际操作 | 结果 |
|---|---|---|
| 1.1 询问/清桌/洗碗，满手仍可查看 | `EntryFrontWorkboardSoloActualAskClearWashPauseAndNaturalDrainingKeepOriginalBowl`：实际备料煮汤，持原汤碗读看板零命令；小麦询问后真实交付，玩家前往清桌，显示真实关联顾客/桌位/餐具 | Passed |
| 1.1/1.2 Claim/Stop与进度，伙伴洗碗及收尾Scenario | 同一单机用例：原停手释放认领、保留进度，走完当前步后重新认领接续；小麦实际洗原碗，暂停冻结进度，Draining自然完成；厨房仍有剩余汤批次也正常结算，同一原碗只进净池一次 | Passed |
| 1.1/1.2 两端竞争、伙伴占用与旧认领Scenario | `EntryFrontWorkboardActualKcpClaimRaceAndCompanionOwnershipStayAuthoritative`：真实远端试认已占伙伴工作被原规则拒绝且不抢占；本地经原LocalMessage与远端UI同刻竞争一项Clear，只有一个Executed/一个Rejected且serial仅加一，最终只一项洗碗/一个原碗 | Passed |
| 1.2 旧行/旧Claim与HUD池化Scenario | 两用例：旧Claim不再发请求；单机换关后旧行不选新工作，远端关闭重开清理选择且不获得Host权限，共享sender仍运行 | Passed |
| 2.1 普通Launcher 540×960和返回原操作 | 原主页→单机→内存试玩→确认→看板，准备态顾客/任务为0且阅读命令0；实际选加成、开店后看原询问行及真实桌位/顾客/进度，阅读仍命令2；收起详情→全部动作→原拿备料台，一次请求使命令3、Executed且手持真实物品 | 已实际操作并检查画面 |

画面位于 `Unity/Assets/Temp/entry-front-workboard-preparing.png` 和 `entry-front-workboard-live.png`。普通入口中原顾客离开后旧询问行显示“原顾客已离开”，不改为同桌新顾客。截图只证明布局，工作、时间与实物效果由上述运行断言证明。手动Play已经按本轮所有权停止。

失败及修正保留：首轮两项失败分别为远端处于前厅筛选却试点隐藏拿碗，以及单机换关后试取已被真实制作消耗的初始番茄；修为显式全部动作和取回同一洗净碗后两项Passed。新增停手断言的单次运行遇到原规则WorkerBusy：停手后当前一步未结束即重认领；用例等待实际Standing再接续后新运行Passed。未放宽业务检查、未改伙伴调度或虚报原失败。

风险与投入沿下文局部UI判断。未变裁决复用orders-front-service主规格Sources原证据；真人理解、Player、物理LAN/Android真机均NotRun。补货与前厅两案严格校验通过；10份变更Markdown、29个本地引用及编码/空白检查通过。下文是建案阶段记录，当前实施与验收以上方为准。

## 2026-10-16 依据与决定

当前只给空手且无工作玩家列出可认领动作，WorkText 主要显示伙伴与最近结果。玩家难以理解哪些桌待询问、谁在清理，以及收尾为什么尚未完成。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)、[主规格](../../../specs/orders-front-service/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：从 FrontWorks／Customers／Tables／Companions／Actors 的同版投影派生，使用 WorkId／ClaimSerial 固定操作身份；沿 SendFrontWork 发原 Claim／Stop，不新增任务队列或派工器，不从最近 outcome 猜整体状态。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

主要风险为把可见任务误当保证可认领，或误列后厨为结算阻塞，后果是显示误导或操作失效，属于可恢复的局部体验问题。采用原 owner、只读派生和原命令入口，最小相关 PlayMode UTF 加普通入口检查足够，不做全仓回归。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | 询问／清桌／洗碗、伙伴与玩家竞争、真实收尾及旧认领失效；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

本案 proposal／design／specs／tasks 均已齐备，`openspec validate front-service-workboard --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
