# Execution Notes

## 当前实施与运行（2026-10-13）

用户已授权继续完成五案，前置九案、设施清理和有限供应已取得对应运行证据。装修已完成源配置、持续owner、真实本地选择、Continue预检／一次安装及同版投影／受管模板按钮；普通Launcher两次换关验收已完成，6/6。

`RestaurantRuntime → RestaurantRenovation(Component)`持本次成功Level/Generation、固定ChoiceVersion／候选、已选名称／不可变安装值和已应用奖励→StationId记录；真实工位仍为Kitchen Child，不复制地形。自然Completed后才筛选合法未获得候选；选择在服务步处理且先于Continue控制，同一次继续只安装一次，不附加初始物品。现有规格未要求合法候选必选，本轮按已说明的默认允许跳过继续；选择后锁定，空集／用尽均可继续。关于必选的可选澄清仍待用户回应，不把等待当授权。

新增`RenovationConfig.xlsx`、__tables__第33行c,s,e和LevelConfig末尾AllowedRenovationIds，三份工作簿只改相关sheet XML、其余ZIP部件保留。样机普通台面定义1／切配台定义2，各数量1，最终锚点为`(2,13)`／`(4,12)`、朝向0；数量沿本地x方向排布并整体预检，参数可调。两次实际五目标Check／正常导出及四语言均成功，退出0，无生成物手改；Level ByteBuf fixture正常追加新源字段。

协议15；正常Proto2CS末尾新增ChooseRenovationRequest／Response／SubmitRequest／SubmitResponse与GridRenovationAppliedState（10109～10113），Snapshot49～54复制候选／选择／结果身份／应用记录。ClientSession所属值请求→LocalMessage.Peer ActorId→根ProcessInnerSender→ProductHost/Peer Ordered MailBox的RenovationPeerHandler→可信Submit→authority ActorId/HostSession Ordered MailBox的RenovationSubmitHandler→Restaurant服务步选定。两段答复沿原Call，最终原独立Snapshot回ProductClient，复制值后Dispose DTO；无新监听／wire，Host模式明确拒绝Solo装修。

| 任务／功能 | 实际PlayMode运行 | 最新有效结果 |
| --- | --- | --- |
| 1.1 源／类型／数量／锚点／关引用及无免费初始物资 | SoloRenovationConfig实际源＋五种非法参数 | 6/6 Passed |
| 1.2 零星／固定候选／最新重放／冲突／锁定／旧关 | SoloRenovationChoiceZeroStarsLatestReplayLockAndOldLevelUseRealMailboxes、InvalidOrStaleResult | Passed |
| 1.2 空集／用尽／无合法空间继续 | SoloRenovationChoiceUnavailableAndExhaustedPoolsContinueWithoutFreeItems、NoLegalSpace | 修正样机第二锚点后相关Choice小集4/4 Passed，奖励最多两个、初始物品不补充 |
| 2.1 原容器／未完成手工／冻结参数／一次安装 | SoloRenovationInstallKeepsContainerManualIdentityAndOnlyAddsOneStationOnReplay | Passed；同物品ID／ProcessId／进度／RequiredTicks保持 |
| 2.2 冲突／堵路／承接引用／整体零写入 | SoloRenovationLayoutLateGroundConflict、BlocksDisconnectedGeometryAndPreservesRelationships | Passed；失败保留旧关／选择／物品／布局 |
| 2.2 新工位移动／营业拒绝／DTO独立 | SoloRenovationLayoutNewStationUsesOriginalMoveRuleAndIndependentProjection | Passed |
| 3.1 真实结算模板／池化重开／旧按钮／选择后才安装 | SoloRenovationUiRealZeroStarChoicesReopenOnceInstallAndOldCallbacks | Passed |
| 3.1 真实布局堵候选／空集反馈／继续 | SoloRenovationUiActualBlockedCandidatesShowsEmptyFeedbackAndCanContinue | Passed |
| 3.2 普通Launcher零星→选择→安装→移动→再换关 | 临时内存脚本仅实际PointerClick／地图指针及读投影，见下述阶段 | Passed；新工位与原7件物品保持，第二次继续不重复授予 |

首轮14项装修UTF为12 Passed／2 Failed：NoLegalSpace的实际Layout返回LayoutBlocked，UnavailableAndExhausted第二轮没有候选2。原因是初始拟定`(4,13)`与先装`(2,13)`组合会堵公共通路；未放宽预检，改源锚点到`(4,12)`并正常重新生成，相关Choice新运行4/4 Passed。其余不变用例复用，当前14项业务／配置最新有效结果全部Passed。真实UI小集2/2 Passed、零Failed／Skipped／Inconclusive，Console覆盖完整。原始输出留`Unity/Temp/pipeline_test_status.json`，Editor日志`Unity/Logs/solo-five-changes-editor-20261013.log`，不另存证据包。

最后recompile completed、failed=false、compilationFailed=false。UI沿原UI1101、原Prefab／CodeBind的目标模板、inputGuard及UIGridKitchen.AsyncOwner；无需新增结构字段。装修状态文字每次投影刷新，按钮仅在候选／选择／owner变化时重建。仅内存Solo；产品档、联机装修及Player仍未交付。

严谨程度沿下文Rigor and scope：主要风险是重复安装及破坏旧空间／加工，已用有限候选、拒绝零写入、原身份／进度和真实UI覆盖；供应与原加工未变结果复用。普通入口剩余一步补齐后停止局部验证，不扩大全仓；持久化或联机接入时再补相关边界。

### 普通 Launcher 与最终核对

主工程Editor PID59672，已保存Assets/Launcher.unity从普通editor_play启动；无领域写入或直接推进业务钟。`Unity/Temp/SoloRenovationAcceptance.cs`经既有run_script内存编译，通过真实PointerClick和Board的ProcessGridPointer操作：

| 阶段 | 实际结果 |
| --- | --- |
| 自然零星与候选 | 真实选择buff／开始营业，普通客流自然排空；只读后续检查确认Completed、Tick1389、Operating400、0星、候选1/2、实际命令编号2。首段CLI在30秒观察上限超时，服务端继续；没有重启或重发入口。后续ChooseAndInstall显式断言相同零星与候选事实后成功，未把超时本身改报Passed |
| ChooseAndInstall | 真实选择新增台面，当前仍16工位；真实下一关把Level1284091902165019换为1284092240855645，空工位1284092240855647只安装一次，原物品7件不变。executeMs=144、success=true |
| MoveAndStart | 真实“调整新增台面”只选中，地图指针单次提交将同工位移到`(2,12)`，随后真实新buff／开始营业。executeMs=201、success=true |
| NextCarry | 第二次真实客流收口后继续，Level变为1284092290139290；原新工位1284092240855647保持`(2,12)`，总工位17、物品7。使用CLI原生detached job `86078a9c01ab46529cb64e727f77602b`核对completed、success=true、executeMs=15109，无重跑或人工改结果 |

已查看实际固定540×960的原生screen图`Unity/Assets/Temp/pipeline-screenshots/solo-renovation-portrait.png`，新台面出现在真实移动位置；原样机标签密度和提示文本长度取舍保留。最终Console groundTruth无编译失败、consoleErrors=0，自有Play已结束。两个受Level源字段／Continue影响的最小基础回归：SoloLevelControlHasOneBusinessClockAndResumeUsesLocalMailbox与SoloBuffChoiceLatestReplayAndPayloadConflictReturnExistingFacts分别1/1 Passed。

验证收口：6/6任务、四项新增requirement均对应上述源码／14项装修配置业务最新有效结果／2项实际UI及普通入口结果；完成性、正确性、设计一致性核对无待解决功能项。可复用历史加工／布局证据与本轮拒绝、原身份／进度断言；不再扩大重跑。明确接受仅内存Solo Editor、无写盘或联机装修、Player/AOT未验证；这些能力接入或布局／加工合同改变时补对应点。未提交、未归档。

## Planning status

2026-10-12 用户授权先写五个 change；本轮只创建规划，未实施、未导表、未编译，Unity/UTF/包体均 NotRun。以现有 Solo 九案按契约交付为规划假设；apply 前核对前置的逐功能运行证据，不修改或替其它工作勾任务。

## Rigor and scope

主要风险是重复安装或新增占地破坏旧实物/加工，影响当前厨房和动线。有限两个样机候选、现有布局预检的身份/进度/拒绝断言，加一次 Launcher 选择/安装/移动足够。复用未变的基础寻路和关卡收口证据；不测全工位或扩大地图。接受内存装修、可调安装锚点、无菜单缩小和联机；接持久化、扩建或新能力再补相应点。

## Acceptance mapping

以下名称是拟补用例范围，不是已经发现或运行的用例；实施时发现真实 PlayMode 过滤并记录原始结果。结果留现有工具原位，不复制证据包。

| 任务 | 功能 | 对应运行 | 结果 |
| --- | --- | --- | --- |
| 1.1–1.2 | 源、固定有限候选、空集/重复 | 拟 SoloRenovationConfig/Choice | NotRun |
| 2.1–2.2 | 一次安装、原物/加工/地形 | 拟 SoloRenovationInstall/Layout | NotRun |
| 3.1 | 当前结算 UI生命周期 | 实际 UI UTF | NotRun |
| 3.2 | 零星选择/新工位移动/再跨关 | 普通 Launcher + 相关小集 | NotRun |

## Planning validation

2026-10-12：`openspec validate solo-interlevel-renovation --strict` 退出码 0，返回 valid；proposal/specs/design/tasks 齐全。本轮五案共 26 份 Markdown 的 UTF-8、行尾空白、本地引用及任务未勾选检查通过。上述检查仅证明规划格式/引用有效，功能运行仍全部 NotRun。

## 2026-10-13 本地提交收尾

用户确认授权后，本案实现随本地main提交906829ef保存，原验收记录随4bfe1a2c保存；此前“未提交”描述保留其当时阶段语义。源码与行为未再改变，逐功能UTF／普通Launcher结果直接复用。本轮五案均已实际验收；归档和push未执行。
