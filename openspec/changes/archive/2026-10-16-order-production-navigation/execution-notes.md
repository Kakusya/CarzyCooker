# 探索与执行说明

## 最新交付与逐项验收

| 任务／Scenario | 实际用例或操作 | 最新结果 |
|---|---|---|
| 1.1／1.2 同菜多单、完整菜谱／缺口、物品／工位查看 | `EntryOrderNavigationSoloChoosesSameDishReadsItemsAndExpiresWithoutRetargeting`：真实两张汤单可切换，目标顾客／餐桌明确，浏览不增命令／物品／贴单；真实切配后重读不要求重做，原订单自然过期后清目标并提示；HUD重开旧按钮无效 | Passed |
| 1.1 已有合法贴单优先 | `EntryOrderGuideActualBoundDrinkKeepsOriginalOrderWhenAnotherIsSelected`：实际制作橙汁、产生两单、贴到第二单，再阅读第一单；原杯仍指第二单，命令与实际绑定不变；实际订单过期明确失效 | Passed |
| 1.2 Host／真实KCP Client | `EntryOrderNavigationActualKcpReadsSpecificCustomerAndReturnsToOriginalPickup`：普通Host房间开第一章，远端选择指定顾客，不增加控制权限和命令，关闭后Host仍能原拿碗 | Passed |
| 1.1 换关与拒绝保持原事实 | `KitchenRecipeGuideRejectedEmptyDishAndNewLevelRecomputeInsteadOfAdvancing`：实际选订单、空碗交付拒绝、自然结算及原下一关，旧回调无效且RecipeOrderId清零，原提示不沿旧桌 | Passed |
| 2.1 540×960普通入口 | 实际Launcher→合作→Host房间→第一章预览确认→原加成及开店→暂停后查看顾客2／餐桌2的汤单；滚动原列表可读新制作入口，摘要可读、返回控制可点 | 通过 |

相关4个不同UTF最新Passed，按受变范围运行：首轮3项中的Core和KCP结果复用；修正夹具后仅Solo失败项1/1重跑Passed；另补原换关用例1/1 Passed。全部有效运行Console覆盖完整，AutoTesting退出0，原始结果仍在 `Unity/Temp/pipeline_test_status.json`，未复制／改写。前两次失败和诊断保留在下文。

实际截图 `Unity/Assets/Temp/entry-order-navigation.png`、`entry-order-navigation-links.png` 已查看；截图首次读到旧GameView帧，重新请求Editor重绘后确认原滚动列表和制作入口可见，不以旧帧判断布局失败。手工检查时只有一张仍有效汤单，两张同菜订单由UTF的真实运行证明。开店前房间控制已分配编号1，加成2、开店3、暂停4；查看订单和滚动均维持4，没有暗中贴单／移动。已同步操作说明与当前进度；设备差异和真人理解未验证。没有提交、推送、归档或构建。

## 2026-10-16 实施进展与首跑

修正实际番茄来源后，失败的Solo用例新运行1/1 Passed（30.01秒），Console覆盖完整、退出0；复用首跑Core贴单和真实KCP两项Passed，3个不同新用例最新结果均通过。Solo覆盖真实同菜两单、指定顾客、物品／工位只读、完整菜谱／缺口、无新增命令／物品／贴单、池化旧按钮、真实切配后重读与自然过期不改目标；Core用例实际制作橙汁并贴到另一订单，再指定不同订单时仍按原杯贴单解释，原绑定和命令编号保持。KCP用例覆盖真实Host房间开局、远端指定顾客只读、不增加Host权及返回原拿碗。真实换关和540×960仍在收尾，不提前勾选任务。

诊断重跑仍Failed，明确缺少 `切配 切配台 ·`，当时Operating／All。夹具把“食材台”当番茄源，但该台实际是其他原料；取错材料后原合法切配按钮本就不存在。已按当前物品定义定位真正番茄源再点击原拿放／切配入口，未增加资格或绕过校验。再次仅跑该失败项，前两项Passed继续保留。

原Presentation已增加本轮OrderId、物品查看及完整菜谱／缺口切换。复用GridRecipeGuide并为显式目标增加只读参数：无贴单成品按选定订单解释；已有合法贴单继续优先。原生产、移动、贴单和交付请求不变。订单结束时清目标并提示，HUD／关卡释放原选择。关联物品和工位点击只读当前投影，不调用原拿放动作。

PlayMode `EntryOrder` 首跑2/3：真实本地树饮品制作／贴单后选择另一订单的用例Passed，真实Host＋KCP指定顾客阅读及返回原拿碗用例Passed；Solo同菜多单用例在Command的Button查找抛出Sequence contains no matching element，堆栈未给出操作caption。已为现有测试按钮查找补充caption／phase／group诊断，仅新跑失败项定位，不改变玩法资格或原请求。任务仍未勾选；原生输出在 `Unity/Temp/pipeline_test_status.json`，不改写或复制。

## 2026-10-16 依据与决定

订单有前往餐桌与交付按钮，菜谱则独立列出；自动首单选择也可能取另一订单。玩家缺少围绕自己选定订单查看制作依赖的入口。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRecipeSystem.cs)、[主规格](../../../specs/kitchen-recipe-guide/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：保存 OrderId 与世界／Level 身份，复用 GridRecipeGuide.Describe／CurrentNeed 与原订单投影；详情附加明确目标，不重写制作规则。订单失效后清选择并提示，不自动切到另一顾客。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

主要风险为将阅读目标误作实际交付或饮品绑定权威，后果是显示误导或操作失效，属于可恢复的局部体验问题。采用原 owner、只读派生和原命令入口，最小相关 PlayMode UTF 加普通入口检查足够，不做全仓回归。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | 同菜多单、合法绑定、订单结束／换客／换关及真实制作后返回；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

本案 proposal／design／specs／tasks 均已齐备，`openspec validate order-production-navigation --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
