# 探索与执行说明

## 2026-10-16 实施与实际验收

沿原ProductEntryPresentation和1101视图增加当前endpoint复制、地址粘贴与连接帮助；错误输入停在原可编辑页，帮助往返保留输入。加入中展示原Client.Joining阶段及目标；取消／失败仍走原初始化释放与切换。只通过GUIUtility复制公开ListenEndpoint，不改网络范围、RuntimeInstanceConfig校验、协议或监听入口。端口按原Host临时回环地址，CLI精确匹配本工程Pipeline7800；没有额外Editor、扫描或固定业务端口。

| 任务／Scenario | UTF或实际操作 | 最新结果 |
|---|---|---|
| 1.1／1.2 Host复制、帮助和关闭重开 | `EntryConnectionHelpCopiesCurrentHostEndpointAndRetiredCopyCannotOverwriteClipboard`：当前地址准确、旧页不能覆盖剪贴板、池化重开、复制值实际KCP加入、零玩法命令 | Passed |
| 1.1／1.2 粘贴、无效输入和重试 | `EntryConnectionHelpPasteRetainsInvalidInputAndRetriesThroughOrdinaryKcpClient`：帮助往返保留文本、错误不建Client；修正为真实Host后普通Launcher新Client加入，旧按钮无效且无重复请求 | Passed |
| 1.2 超时／取消／后续单机、原制作 | 受变 `ProductEntryUi` 四项：实际超时、取消、非法地址、KCP Client和Host重开，后续原单机制作动作 | 4/4 Passed |
| 2.1 540×960 | 普通Launcher实际合作→帮助→返回→创建→复制，复制值等于当前ListenEndpoint，Joined、无餐厅、命令0；帮助及房间按钮实际可读可点 | 通过 |

PlayMode `EntryConnectionHelp` 2/2 Passed；后续 `ProductEntryUi` 4/4 Passed，均Console覆盖完整、AutoTesting退出0。新测试首次编译发现EntityRef需转Match后访问，修正后0错误／0警告；未把编译当功能验收。原生输出 `Unity/Temp/pipeline_test_status.json` 保留工具原位，不复制／改写。实际截图 `Unity/Assets/Temp/entry-connection-help.png` 和 `entry-host-address.png` 已查看。操作说明与当前进度已同步；物理LAN、目标设备与真人体验仍未验证，不据此宣称跨机可用。未提交、推送或归档。

以下保留规划阶段记录，其中NotRun表示当时状态。

## 2026-10-16 依据与决定

创建房间只显示 ListenEndpoint，加入页默认一个地址输入框，连接错误常来自底层文本。已有连接流程缺少能让玩家正确填写和转交地址的说明。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryPresentationSystem.cs)、[主规格](../../../specs/product-entry/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：沿 RuntimeInstanceConfig 和当前 ListenEndpoint，视图调用现有复制缓冲 API；保留 ChooseCooperativeEntry／原 Join 和取消路径。输入失败停在原页并保留可编辑文本，重试创建新所属连接；不新增发现协议、监听或网络白名单。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

主要风险为把仅适用回环的 endpoint 宣称为可跨机使用，后果是显示误导或操作失效，属于可恢复的局部体验问题。采用原 owner、只读派生和原命令入口，最小相关 PlayMode UTF 加普通入口检查足够，不做全仓回归。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | 有效／无效输入、复制、真实 KCP 加入失败与取消重试；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

规划阶段历史记录（不代表当前状态）：本案 proposal／design／specs／tasks 均已齐备，`openspec validate cooperative-connection-assistance --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
