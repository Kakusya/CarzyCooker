# Execution Notes

## 最终交付：4/4任务，10个不同UTF Passed

`Unity/Temp/Companion-presence-files-UTF.log` PlayMode CompanionPresenceFiles两项、`Companion-presence-UI-warm-UTF.log` PlayMode CompanionPresenceUi两项均Passed、0Skipped，Console原历史1条保留，无本轮新增异常。首次冷eval5秒超时仅为启动前查询失败，`Companion-presence-UI-UTF.log`及各discovery日志保留，不算测试执行或通过。

五项功能验收和五项受变回归合计10个不同用例：上述文件2／入口UI2，`Companion-presence-panels-UTF.log`新增1项实际“收起详情”关闭聊天／切换菜谱只读验收，`Companion-presence-regression-config-UTF.log`原无效定义4项、`Companion-presence-regression-hud-UTF.log`原共享显示上下文1项均Passed、0Skipped。自审发现聊天打开时通用详情按钮文案不一致，已把聊天纳入同一显示选择并在收起时清选择；新增普通Launcher用例验证该改动。原两完整入口及文件行为未变，复用已有完整结果，不重跑旧套件。

主规格fixed-companions保留原5项工作要求，新增3项固定识别／只读聊天／提交后反馈，Purpose同步已确认称呼。完整性4/4任务、3/3新增要求；正确性3/3场景由上述真实入口及文件覆盖；设计／模式检查符合原Restaurant/Level业务owner与ProductClient显示owner，无新增消息／存档权威。未发现待修关键项或范围内验收缺口。

| 任务／功能 | 实际运行与断言 | 结果 |
| --- | --- | --- |
| 1.1 同一定义与有限文案 | Files(False/True)实际配置唯一Id1、Name小麦、原30/20/61参数；两个Ui普通控件显示三类原文 | Passed |
| 2.1 聊天与工作反馈 | 两个Ui核对聊天前后NextCommand/物品/业务Tick/营业进度/订单/成长不变；实际询问/洗碗完成后正面反馈，第三项显示原成长文案；暂停下重复FrontWork/Projection事件不增次数或反馈修订 | Passed |
| 3.1 重开、旧回调、换关／恢复 | 两个Ui关闭重开不播历史，保留旧聊天回调再次调用无效；实际自然收口后普通下一关，仍小麦/定义1、完成0、无旧反馈；Files分别实际Solo成功档／合作结算票主动领取后恢复原定义、关内成长0、源文件字节不变 | Passed |
| 4.1 普通入口及远端同事实 | Solo Launcher和Host房间→独立Fiber真实KCP，从受管聊天／地图／交付／玩家清桌到伙伴后续洗碗、第三次反馈、自然结算及下一关，全程原命令与独立投影 | Passed |

玩家清桌等待真实State=Completed；完成次数无变化时显示修订也无变化，未把“已认领”当清理完成。伙伴只做原Ask/Wash，原岗位/第三次加速裁决/玩家不计成长等未改，复用原32项记录；本案只新增显示基线，不另存人物/成长、不补奖励。文件12/wire34不变。受管UI沿原Prefab/CodeBind、信息区与按钮owner；不改根显隐或新增订阅。

本次主要风险是只读入口误发动作和历史反馈重播；两种实际入口及实际成功文件往返已覆盖，故不扩大为旧伙伴规则全套。真机、物理LAN、Player/AOT/构建、人物扩编及更多能力仍NotRun／范围外；发现跨关重播或玩家误计再补相应最小用例。未提交、push、归档或操作第二工作区。


## 配置/显示实施阶段记录（历史）

原CompanionConfig一份源已Check五目标、生成五目标/四语言成功，日志Companion-presence-check.log和Companion-presence-gen.log；稳定Id1/Post1/出生与30/20/61时长未改。新文案是现有中文字段，不建新本地化通道。GridPresentation沿原受管HUD显示三类Preparing聊天；原角色投影的完成次数增量触发一次工作反馈，跨越第三次附原成长文案。关闭/重开/换关清显示基线，不重播；没有新增协议、人物存档或业务请求。首次冷eval发现超时日志保留，第二次正常发现632用例，明确仅运行CompanionPresenceFiles两项，随后CompanionPresenceUi两项。当前功能运行尚未终态，任务保持未完成。


## 源配置准备：已修改及Check，尚未生成

咖啡共享HUD的清桌验收仍运行；本次只改Unity目录外的CompanionConfig.xlsx源，不执行会写Unity资产/脚本的生成。Id1、Post1、出生(2,5)、Ask30/Clear20/Wash61保持，只改Name小麦，追加PreparationChat/CooperationChat/ClosingChat及WorkFeedback/GrowthFeedback五个只读字符串。沿原artifact-tool编辑一份工作簿，源日志`Unity/Temp/Companion-presence-source-edit.log`；原Luban Check五目标成功，`Companion-presence-check.log`。目前运行中仍加载原生成配置，15的生成/编译/功能UTF全部NotRun，不勾任务1。

## 2026-10-15 实施交接准备（尚未改源表/业务）

14咖啡普通UI已2/2 Passed，章节UI及小回归仍在收尾，15共享HUD代码暂不动，按同owner依次交接。已读取原Companion/Role/Growth、独立GridCompanionProjection.CompletedCount、受管HUD生命周期与显示字段比较：现有完成工作后会清WorkerKind，不能据最后一条通用FrontWork提示把玩家清桌归为伙伴。推荐直接观察当前已提交伙伴完成次数增量，显示有限的通用正面文案，第三次跨越才显示既有洗碗成长；重复投影/重开只建基线不重播。聊天按当前Preparing投影本端只读，使用既有信息区；回调冻结当前World/Level/HudLoadEpoch，不能发业务请求。

源CompanionConfig仍Id1/Post1/Spawn(2,5)/Ask30/Clear20/Wash61，Name当前“伙伴”；准备同一源追加三类聊天及工作/成长两条短文案，改称“小麦”，不改数值、人数、稳定定义或存档。Temp中只准备未执行的原artifact-tool builder；尚无15源表修改、生成或功能UTF。本次低影响表现，选只读聊天/一次反馈/旧回调/跨关及实际成功档身份的小集，原32项伙伴工作规则不变只复用对应结果。接受设备/Player/AOT/物理网络/人物扩编及新成长不验证；出现玩家操作误计、重复提示或旧关回调泄漏则补对应最小验收。

## 2026-10-15 规划优化

本轮仅修订未开工计划，统一已确认范围、真实前置和逐功能验收；以下早期“仅规划/待裁决”措辞属于创建记录，当前交接以本节、design与tasks为准。没有实施业务或执行UTF/Editor/构建，功能结果仍为NotRun。本案严格格式、正文/引用、任务到验收映射检查通过；全批汇总见[路线记录](../../../../Docs/Product/full-version-roadmap.md)。

## 当前状态与范围

2026-10-15：用户授权继续扫描业务、按优先级建立 change，明确不考虑真机实测，并确认“按已确认目标，菜品分批扩展”。本案只完成规划，任务未勾选；功能实现、UTF、Unity Editor 运行、改表/导表、构建均 **NotRun**。规划检查结果统一见[本轮路线记录](../../../../Docs/Product/full-version-roadmap.md#本轮规划检查)。

PC LAN 与 Android 原案保留，不是本案依赖。没有提交、push、归档或操作第二工作区；此前授权的美术提交不扩展成本轮业务实施许可。

## 严谨程度与已接受边界

低影响表现改动，主要是误触动作或旧回调重复反馈。使用受管视图/只读聊天和原成长行为小集，一次 Editor 跨关检查即可，不扩大伙伴规则测试。

只选择实际改变行为的最小相关 PlayMode UTF 与普通 Editor 入口；可复用下列已交付主规格关联的原证据，前提是实现、消费者和验收点均未变。 不建立全目录矩阵、额外状态副本或通用测试平台。

接受未验证：真机、设备输入、Player/AOT、物理 LAN/公网不在此次规划内；全目录平衡、未选择内容批次及跨设备持久故障不由本案证明。 需要相关产品交付时另定范围；本案新增/修改行为或出现具体回归时补相应最小验证，不因整理/审查重复跑旧套件。

## 任务与未来验收映射

| 任务/功能 | 最小验证与断言 | 当前结果 |
| --- | --- | --- |
| 1.1 称呼与有限文案 | 源配置/生成检查：一位固定定义、称呼与三类文案正确。 | NotRun |
| 2.1 只读展示 | UI/消息 UTF：聊天只读、真实完成才反馈、成长保持原规则。 | NotRun |
| 3.1 重开与跨关 | UI/文件小集：一次点击、旧回调失效、跨关/恢复身份保持。 | NotRun |
| 4.1 普通入口集成 | 普通 Editor：聊天、工作反馈、跨关身份，合作只读显示。 | NotRun |

同一运行可覆盖多行；先发现真实 PlayMode 用例并记录过滤范围，再执行改变行为的小集。任务中写的是验收目标，不是已经存在或已通过的测试。已有通过结果只在实现和验收点不变时复用，缺口补跑；失败、空集、Skipped及缺输出仍未验收。

## 可复用的既有合同

已有运行记录：[2026-10-13-solo-fixed-companion-work](../2026-10-13-solo-fixed-companion-work/execution-notes.md)。只复用未变的原行为；本案新增功能不因引用它们而通过。


- [fixed-companions](../../../specs/fixed-companions/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。

## 实施交接

2026-10-15 已确认保留一位伙伴“小麦”，准备态聊天围绕备菜/合作/收店，真实工作后简短正面反馈。文案与识别表现可在该范围内编写，最终伙伴人数仍未确定。

参数按配置和实际消费者核对；运行后回填真实方法、结果、原日志路径和失败原因，不保存额外业务状态副本。


最终文档检查：CLI逐案apply重新读取确认16案均all_done、合计66/66；16案严格格式通过，48份主规格严格格式通过；96份相关Markdown UTF-8及703个本地文件引用全部有效。当前全仓空白提示限12份Luban生成文件74处，保持原生成入口，不手改。交付页由answer-me-with-html使用--no-open生成，中文／字体链检查通过。
