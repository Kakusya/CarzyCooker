# 探索与执行说明

## 2026-10-16 实施与实际验收

已在原 GridPresentation/UIGridKitchen 接入“查看处理说明”，从原 LastStatus/LastReason 与 SaveState 派生说明。八类为手满、加工锁定、容器不符、不可达、生产许可、收货台满、旧目标和保存失败，另保留暂停说明。未知原因仅给原始诊断与主动复制，不猜测操作。查看当前物品、原请求目标、菜谱、供应或保存状态均只导航，不重试、不丢物、不解绑、不采购、不改布局。

页面只持本轮反馈/选择、两件相关实物的独立显示值和最近一次本端请求目标；内容/锁定、状态、目标或关/HUD轮次改变则撤下旧建议，关闭/换关清掉请求目标元数据。目标变化类不提供旧请求目标链接。当前命令号/状态/原因参与原动作显示依赖，未改变实物的拒绝也能刷新说明入口。原 Model 消息水位、文件/协议、Prefab/CodeBind不变。

实际发现712项PlayMode UTF，相关 EntryAdvice 类别5项。5个不同用例均有最新适用Passed结果、Skipped=0；原动作导航另补滚动保留断言并按受变范围执行，结果单独记于原动作案。第一轮5项仅保存用例通过，另4项失败均为拒绝后找不到说明按钮；真实拒绝已经到达，定位为显示依赖未包含当前反馈。补齐依赖后新3项3/3 Passed，供应单项1/1 Passed；保存逻辑/该路径依赖未变，复用其真实通过结果，不重复长关卡。远端用例增加实际暂停及帮助互斥后，仅该受变项1/1 Passed。全部有效运行Console覆盖完整、无新增Error、AutoTesting退出0；最后编译0错误/0警告。原失败原位保留，不改写成成功。

| 任务／功能与Scenario | 实际用例／操作和行为断言 | 结果 |
|---|---|---|
| 1.1/1.2 正常Scenario：手满/容器/不可达/旧目标/锁定 | `EntryAdviceActualHandContainerPathTargetAndLockedRejectionsKeepMaterialsAndOriginalCommands`：真实原本地请求产生HandFull、RequiresHeldEggBowl、BlockedCell、TargetChanged与ProcessLocked；阅读/导航不增命令、不改实物或剩余份数；旧目标无请求目标链接；原切配、打蛋和煮汤实际完成后仍可合法放回面包 | Passed |
| 1.1 生产许可及未知原因 | `EntryAdviceProductionPermissionUnknownDiagnosticLateOutcomeAndNextLevelRetireOldViews`：真实成长701受限采购MenuRestricted，配额不扣/无货物；未知枚举由原Host拒绝InvalidCommand，说明不猜测，主动复制含当前命令/世界/原因而无凭证字段；旧复制不能改剪贴板，测试最终恢复原剪贴板 | Passed |
| 1.1/1.2 失效Scenario：较早失败迟到不覆盖当前成功 | 同一成长用例：已拿碗成功后，通过真实根ProcessInnerSender向ProductClient Ordered MailBox投递较早受限采购命令的迟到终态，原handler消费流序号但保持较新Executed及当前命令，不重新出现说明入口 | Passed；这是消息边界输入，不当作新业务拒绝证据 |
| 1.1/1.2 关闭重开及换关Scenario | 同一成长用例：实际关闭/池化重开HUD，原Mono实例复用，旧关闭/复制回调无影响，请求目标清零；真实短营业自然前厅收口、原装修确认/下一关后旧说明回调不能写新页面或剪贴板，原命令一次一次提交 | Passed |
| 1.1/1.2 Host本地、真实KCP与同容器内容变化 | `EntryAdviceHostAndActualKcpClientReadContainerRejectionThenRetireChangedContentWithoutRetry`：Host原鸡蛋入锅拒绝、查看菜谱零请求；真实KCP番茄入碗被拒绝、打开说明。Host通过原本地消息把蛋加入同一个碗后，远端内容更新而原失败命令不变，旧说明撤下；旧目标回调无效，原番茄仍在远端手中，随后原放番茄/拿原碗及蛋均Executed | Passed |
| 1.1/1.2 暂停说明与帮助互斥 | 同一KCP用例：Host真实暂停，Client原移动请求得到Paused；说明跳原阶段帮助、再回说明，旧帮助链接无效、命令与Tick不变且不获Host权限；最后仅Host明确恢复并实际提交 | Passed |
| 1.1/1.2 正常Scenario：收货满槽与供应导航 | `EntrySupplyOverviewSoloSeparatesQuotaTransitPackageAndRejectsFullReceiverWithoutDuplication`：真实采购/到货/满槽拒绝ReceivingFull，查看说明/供应不改配额、货物或实物；腾空后原接收仅生成同一两份包装，拆份同材料、旧建议不能重试；最后原切配真正产出 | Passed |
| 1.1/1.2 保存失败与原文件重试 | `SoloSaveUiOneClickCloseAndReopenKeepsResultOwnedCommit`：真实临时repository路径被自有文件阻塞，原保存失败；说明/保存状态零请求，阻塞文件不变、原结果和Completed保留；恢复路径后原明确重试只新建一档并进入下一关，HUD关闭不取消Match所属提交 | Passed |
| 2.1 普通Launcher540×960实际入口 | 默认Solo201，采购番茄command1、拿碗2/放收货台3/接收4得到ReceivingFull；说明与供应导航仍command4、配额4/5、Ready且无Package。原拿回碗5/放碗台6/接收7 Executed，实际Received、仅一个包装、内容2份、配额仍4 | 已实际操作并查看画面 |

两张实际画面：`Unity/Assets/Temp/entry-action-advice-supply-rejection.png`、`entry-action-advice-original-receive.png`。说明短正文、关闭和两条只读链接在540×960可读可点，原供应流程仍可用；本轮自有Play已停止。手工检查不替代上述文件、消息和KCP断言。

严谨程度：核心风险是建议触发消费/重试或旧失败串页。真实本地与KCP命令、同容器内容变化、原水位、文件失败、池重用/换关和普通入口已覆盖本案影响；不建立错误历史、额外事务或全仓测试。原配方/守恒/传输及发布规则未改，沿相关主规格及原归档记录复用。未验证真人理解、独立Player、Android真机或物理LAN；出现具体设备遮挡、错误原因新增或消息/文件规则变化时再补对应范围。

原始Pipeline作业输出保留 `Unity/Temp/pipeline_test_status.json`、Editor原始日志及工具会话记录；后续运行自然更新即时文件，未复制JSON、哈希、清Console或改写失败。下文保留建案阶段记录；NotRun是当时状态，当前以本节为准。

收尾复核：原18案的36项Requirement/56项Scenario已逐案对照实现、行为断言及本轮适用运行记录；严格格式18/18通过，93份Markdown及527个本地引用/编码/行尾检查通过。每案运行范围与复用理由汇总在原入口路线。最后2.1包含的普通入口、文档及严格格式证据已齐备；本轮自有Play停止，原GameView索引6已恢复且只移除自有540×960尺寸（custom=0），没有保存全局尺寸配置。没有提交、推送、主规格同步或归档。

## 2026-10-16 依据与决定

原反馈已区分接收、到位与终态，也翻译了许多拒绝原因，但默认分支仍可能显示内部 reason，且缺少通往解决当前问题的查看入口。

依据：[实际源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)、[UI 主规格](../../../specs/restaurant-ui/spec.md)、[长期目标](../../../../long-term-goals.md)。本案为本轮用户要求扩展入口的规划，不表示功能已实现。

方案：复用 LastStatus／LastReason 与原 ProductCommandOutcome 水位，不建无限操作历史；在 HotfixView 集中已有 reason 到说明／相关入口的映射。查看当前目标、菜谱、供应或保存状态只改本轮选择；显式新动作仍由原入口生成新命令。

## 严谨程度与验证取舍

主要风险是建议按钮暗中重试或丢物，及旧失败覆盖当前反馈，后果为局部界面误导或误操作。沿原消息水位、输入 guard 与 owner，以最小相关 PlayMode UTF 和普通 Launcher 入口检查覆盖足够；不引入新框架或全仓回归。原命令／守恒／生命周期的行为未变部分复用适用归档记录；新增入口必须实际运行。

本轮全部产品验收 NotRun。真人理解、目标设备、物理 LAN 及未实现输入不由脚本证明；发生具体功能或设备回归再补对应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 解释事实与边界 | 选定拒绝族、迟到旧结果、未知原因与相关入口无业务副作用，有行为断言的最小 PlayMode 小集 | NotRun |
| 1.2 界面与生命周期 | 普通 Launcher 本地及真实 KCP Client，取消／重开／旧回调及原动作可用 | NotRun |
| 2.1 集成 | 540×960 可读可点，逐项 Scenario 与实际运行对应并核对文档／严格格式 | NotRun |

## 规划校验

本案 proposal／design／specs／tasks 均已齐备，`openspec validate product-action-recovery-guidance --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
