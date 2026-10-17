# 执行说明

对应本轮十二方向第2项，用户已要求全部实施。领域、原消息/投影和原Board/HUD已接入，正在逐点运行验收；未覆盖项保持NotRun。

## 严谨程度与取舍

新共享状态与消息的风险是冒名/旧绑定、目标换物和幽灵标记；标记不写存档或实物。沿原有序邮箱、统一Host与独立投影，用最小本地/真实KCP验证成功/拒绝/收尾，再检查普通入口和原操作；不引入新传输/公共框架，不重跑全仓。接受真人沟通效果和设备未验证；具体失联残留或高频表现回归时补对应验收。

## 功能映射

| 任务/功能 | 验收 | 当前结果 |
| --- | --- | --- |
| 1.1 共享/拒绝/寿命 | 下述3项本地/真实KCP核心 | Passed：原锅拿放跟随、三类目标/替换取消、原切配消费和独立DTO、10业务秒/暂停/重放、失联/恢复/Leave/下一关旧身份与原拿碗 |
| 1.2 真实入口与寿命 | 新UI真实KCP旅程和普通Launcher540×960 | Passed：三类标记/查看、原高亮/实际锅拿放、HUD关闭/池化重开/断线手动恢复旧回调；普通原指针流程及实际画面已检查 |

本案规划原严格格式校验通过、4/4齐备；实现后的功能与文档校验另列，不用规划结果替代运行。未提交/推送/归档。

## 实施与过程记录

每Level一个有限成员字典、全局本关serial；可信Peer沿原HostSession有序邮箱完整验证后即时提交，不推进Tick或新增物理工作队列。原Grid提交前按业务Tick/原目标存在性及成员绑定清理；暂停原Tick冻结，重放沿原命令事实返回，不延长expires。Proto源尾部追加4消息/1DTO，原快照独立复制值。原Board只增加派生文字标记，负成员key仅为该Mono标签字典内部键，不是实体ID；原ReleaseBoard统一销毁标签，再清标记派生集合，没有另建实体/池/状态权威。

首轮编译夹具把EntityRef直接与null比较，已按原强制类型转换修正；编排器因groundTruth编译失败拒绝执行，该次为NotRun，不算测试通过。之后发现762项PlayMode，核心首跑3项：实际失联/恢复/Leave/跨关与10业务秒/暂停/重放两项Passed；移动/消费用例Failed于夹具先人为装入Version+1的DTO、随后真实一次取锅快照恰好同版被正常拒绝。把独立DTO变异检查移至真实玩法旅程末尾，避免夹具干扰后续消息，不改产品版本拒绝规则；仅该失败项新跑。

移动/消费单项新运行Passed，三项核心均有当前适用Passed，Console完整。原198个opcode全部保持，尾部新增5个至203；wire37→38，文件12未变。

- `EntryTargetPingActualKcpStableItemMovesReplacesCancelsAndConsumptionDoesNotRetargetNewContents`：两成员原工位/锅标记，非法“餐桌=锅”拒绝保旧；实际拿锅、行走和放交接台后仍指原锅并解析当前持物者/工位，替换餐桌/取消；本地真实切配消耗原番茄并产生另一身份，旧标记不转新物，旧目标拒绝；DTO变异/清空/回收不改独立值。
- `EntryTargetPingActualTenBusinessSecondsFreezePauseAndReplayDoesNotExtendOrRevive`：提交期限正好10业务秒，原本地邮箱重放不变serial/到期Tick/提交版本；暂停400实际服务步不推进Tick，暂停拒绝取消；恢复至期限清空，迟到请求不能复活。
- `EntryTargetPingActualKcpDisconnectResumeLeaveAndNextLevelClearWithoutStoppingSharedSender`：真实KCP Session失联、手动恢复新绑定、旧绑定拒绝和正常Leave均清本人；本地标记/原sender仍在，真实拿碗成功；自然完成、装修及下一关后空集合、旧关请求WrongLevel。

UI首跑在原动作查找处Failed，但原输出没有动作名。审查时另发现旧物品查找选中ID可留作页面上下文，新标记入口不得在已切到工位页后继续优先用它；已将物品目标限制到当前物品页/订单物品上下文，表页使用当前工位。补动作名/页/分组说明后新跑明确失败为“拿 灶台”，实际原HUD该操作名是“直接拿锅”，因此修正夹具文案。另补实际HUD关闭/恢复时只清派生标签、重开从当前投影重绘的处理与断言；不修改原本地高亮。仅重跑此UI项，不重跑已通过核心。

## 最终结果与普通入口

`EntryTargetPingActualHostKcpThreeTargetsFollowOriginalPotAndPooledRecoveryCannotReplay` 修正后新跑1/1 Passed，Console覆盖完整：原Host工位说明只读选择/发送、原占地高亮保持、物品选择/替换、HUD关闭清文字和池重开旧回调失效；真实KCP加入显示Host当前值、原按钮设置本人同锅标记、只读查看无命令、实际拿锅/放交接台、取消/餐桌标记、Session失联和手动恢复后原绑定回调无效，原实物保持。总共3核心＋1UI四个不同适用实例当前全部Passed，无Skipped/空集，原始失败与新运行分别保留在 `Unity/Temp/pipeline_test_status.json` 和 `Unity/Logs/Business-Delivery-Editor.log`。

UTF外普通Launcher Host内存关401、540×960沿真实指针回调完成24步：主页创建原房间/发布/准备/开始，工位说明选灶台并标记，物品查找选原锅并替换/只读查看，原“直接拿锅”及“放交接台”实际执行，工位说明选择餐桌1并标记、取消再标记，最后原暂停command11 Executed。只读查看命令数不变，最终原锅仍Counter/交接台，只有本人餐桌标记1条，业务暂停保留寿命。原地图Pan查看前厅，实际GameView含overlay截图 `Unity/Assets/Temp/pipeline-screenshots/target-ping-portrait.png` 已查看：紫色“成员1→餐桌1”可辨识。临时Editor.update流程在完成后解绑，AppDomain临时报告清空，自有Play停止、原GameView恢复并仅移除新增尺寸；无新增成功文件、监听类型、提交/推送/归档。

已同步新的cooperative-target-markers主规格，50份主规格严格通过；本案8份Markdown的编码/空白和12个本地引用检查通过，change严格通过，任务已按运行逐点勾选。物理LAN/独立Player/真人沟通效果仍未验证，不把回环KCP提升为这些结论。

## 2026-10-17 归档与提交

用户已明确授权本批归档及本地提交。任务和原逐项运行结果保留，主规格已核对；归档后全量严格检查74/74通过（50份主规格、24案）。仅调整规划状态与相对引用，未因归档重跑未变行为。提交范围排除工作区既有Android启动/后台、LAN和HandmadeBistro改动；本机设置与记录元数据依赖的平台存储定义及文件读写保留。不推送，设备、正式平衡和真人体验仍按原未验证边界。
