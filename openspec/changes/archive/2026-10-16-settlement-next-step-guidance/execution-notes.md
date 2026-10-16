# 探索与执行说明

## 2026-10-16 实施与验收

已接入原HUD的结算摘要和“结算详情”，集中显示结果、记录事实、唯一下一步及目的地；详情保留原装修/保存说明，并按生效定义列出下关菜单的新增/移出。终关明确无下一关，零星仍可继续。摘要保留已选奖励、具体升级目标和安装拒绝；解锁不补物资仍可见。Continue/SaveAndContinue/FinishChapter仍走原请求，无自动保存、协议或文件改版。

只增加原Presentation的Settlement页面选择，HUD退出/重开和换关失效。合作结果、文件提交和个人领取分别解释，不把Host结算显示为个人已领；实际领取状态沿原查询。完整待选升级目标放在只读结算详情，摘要仍显示已选具体目标。

PlayMode发现700项，仅运行相关范围。5个不同用例均有适用Passed结果，原始输出保留`Unity/Temp/pipeline_test_status.json`与本轮工具输出；Pipeline后续自然更新该文件，未复制或改写。两项单机通过后未重跑，合作两项通过结果复用，失败升级回归修正后仅重跑单项1/1。各终态Console覆盖完整，Unity编译0错误/0警告。

| 任务／功能及Scenario | 实际用例／操作 | 结果 |
|---|---|---|
| 1.1/1.2 非终关保存失败重试、重开、一次提交 | `SoloSaveUiOneClickCloseAndReopenKeepsResultOwnedCommit`：普通Solo真实临时repository，实际路径阻塞失败，保留结算及结果ID，摘要提示重试；详情零命令，重开后成功只建一档并继续，旧详情回调无效 | Passed |
| 1.1/1.2 零星终关、失败重试、无第四关与试玩 | `SoloChapterUiTerminalFailureRetryReturnReadOnlyCompletedFileAndIndependentNewRun`：真实文件恢复103，营业自然收尾零星；终关无下关菜单，失败不离开；成功写完成记录返回选档，完成记录只读；另开内存201不写档 | Passed |
| 1.1/1.2 Host/Client菜单、只读、旧回调及返回 | `EntrySettlementActualKcpMemoryThreeZeroStarResultsReadonlyDetailsAndTerminalRoomReturn`：普通Host/真实KCP三关自然结算零星，菜单来自定义，两端阅读零请求；Client无继续权，Host各关仅前进一次，终关回同一房间、sender保留，无第四关 | Passed |
| 1.1 未就绪/写入中/已提交的显示边界 | 上述KCP用例隔离改变显示投影后立即还原，不运行帧或发请求：候选未到不开放继续；Writing不显示已提交且不开放保存；Committed明确已提交；Client合作结果标志不误称内存或个人已保存 | Passed；这是UI显示夹具，真实写盘由前两项证明 |
| 1.2 原装修升级及完整目标阅读 | `CoopUpgradeUiOrdinaryHostChoiceRemoteTargetsAndNewWorkKeepStation`：Client从结算详情读原目标且零命令；Host确认升级，摘要显示目标，跨关原工位/物品保留，新切配45Tick并完成 | Passed |
| 1.2 解锁/扩建与装修预览兼容 | `EntryRenovationPreviewActualKcpReadOnlyUnlockAndExpansionStaySeparateUntilHostConfirmation`：真实KCP只读，他端选择使旧预览失效；解锁不补物资摘要、原下一关整体扩建及实物守恒仍通过 | Passed |
| 2.1 普通入口540×960及原流程 | 主页→单机→内存试玩→确认，真实201选buff/开店自然结算；摘要/详情/关闭可读，命令仍2；原装修预览仍2、确认3；下一关4进入202，原全部动作/拿碗后命令5 Executed、手持非零、升级1、Page=None | 已操作并检查画面 |

画面：`Unity/Assets/Temp/entry-settlement-overview.png`、`entry-settlement-details.png`、`entry-settlement-selected-upgrade.png`，实际540×960可读可点。截图只证明画面，行为以UTF和操作读回为准，本轮自有Play已停止。人工入口未改阶段、得分或营业时长；UTF在各自世界缩短OperatingTicks后仍走真实前厅收尾。

失败修正：新增KCP首轮只等Ready便查“下一关”，重开控件尚未刷新；沿原ChapterUiCommand模式等待真实控件出现后单项通过。原升级回归第一次完成新详情及跨关，随后仍在Layout分组找切配；补真实“全部动作”导航后单独通过，并保留Host Presentation owner避免单实例HUD争用。没有修改权限、保存、营业或加工规则。

旧`CooperativeChapterUi`只将终关等待措辞断言改为含义断言，未单独重跑；相同终关只读/回房间由新增KCP实际覆盖，独立再开沿未变[章节验收](../2026-10-15-restaurant-chapter-content/execution-notes.md)复用。个人真实领取、持久ACK和Match关闭沿未变[原领取验收](../2026-10-15-cooperative-result-claims/execution-notes.md)复用；不把显示夹具冒充新的持久合作验收。

严谨程度：主要风险为把未保存或个人领取误称完成，或导航绕过原门槛。真实文件失败重试、KCP权限/生命周期和普通入口足够覆盖本次展示变化，未改后端沿原证据复用，不扩全仓。真人理解、独立Player、物理LAN、Android真机NotRun；出现设备遮挡或保存/资格规则变化再补对应范围。下文保留建案历史，当前结果以上方为准。

收尾：本案5份Markdown、18个本地引用及编码/行尾检查通过，严格校验通过。三个任务逐项按CLI返回行核对后勾选，刷新apply确认3/3、all_done；原18案当前13/18、39/54。

## 2026-10-16 依据与决定

结算文案由统计、装修、保存状态分段追加，持久与试玩、Solo 与合作的下一步不同。玩家需要清楚知道当前卡在哪、什么已保存及继续会到哪里。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)、[主规格](../../../specs/restaurant-ui/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：集中派生展示状态，继续用原 RefreshSaveStatus／RenovationReadyForContinue 和 LevelControlAction；不会为统一按钮名称重写 Continue／SaveAndContinue／FinishChapter 的业务含义。下关菜单从有效定义查，不臆造下关奖励。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

主要风险为用统一展示抹平保存提交、结果与领取的差异，后果是显示误导或操作失效，属于可恢复的局部体验问题。采用原 owner、只读派生和原命令入口，最小相关 PlayMode UTF 加普通入口检查足够，不做全仓回归。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | Solo持久／试玩、合作Host／Client、零星／终关、失败重试和旧回调；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

本案 proposal／design／specs／tasks 均已齐备，`openspec validate settlement-next-step-guidance --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
