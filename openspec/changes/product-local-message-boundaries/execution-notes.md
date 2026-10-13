# 执行说明

## 决策与当前状态

- 2026-10-13：用户要求查看项目并构建 change；对“产品业务入口、结束及功能间衔接统一消息，内部计算／每 Tick 进度仍直接调用”的推荐回答“是，按你想的来”。已授权规划，尚未实施。
- 已加载 openspec-explore、carzycooker-requirements 和 openspec-propose；按实际源码／主规格构建提案。无剩余阻塞性产品决定。有限消息调度延后、框架建树／强制退出例外及存档交接见 design。
- 静态基线：源码协议 16；供应、清理和装修已有实现，solo-success-checkpoint 仍在实施。部分入口文档的旧协议摘要滞后，本案以源码和原 change 最终交接为准。工作树存在大量其他任务修改，本轮仅新增本 change 规划文件。
- 产品功能、UTF、普通 Launcher、构建与生成：**NotRun**。文末记录本轮规划检查，不能用它代表功能通过。

## 严谨程度与取舍

主要风险是跨功能从同步衔接变为消息后漏掉／重复效果、提前自然结算、跨关旧实例写回，以及公共丢弃路径重复回收。保留原 owner、业务步和原子效果，用每类功能的真实消息行为断言加几个代表性退出／丢弃测试即可定位；不建立全历史存储、可靠消息平台或穷举调度实验。

已有配置纯校验和未变规则的证据可以引用原 change，但改过的请求／终态／自动衔接不能用旧通过代替。自动断言尽量扩展已有 ProductSessionTests／Solo* 测试；一次普通 Launcher 连续流程覆盖多个功能。覆盖齐全后不因整理文档或归档重跑。接受功能衔接经过后续消息轮次，不保证原直调链同 Tick；明显交互停顿、前厅卡住、一次效果出现两次才触发针对性补验。

本案明确未验证 Player/AOT、手机、LAN／公网、双 Editor、新合作餐厅玩法、任意断电文件恢复及跨进程可靠投递。这些不因消息化自动升级为支持项。存档使用原 change 的文件保护合同，若改动实际触及文件写入路径则复跑其失败／冲突保护用例。

## 功能与验收对应

下列是计划，全部 **NotRun**。现有名称指源码中的用例族，新增消息断言和拟议测试尚未实现；执行时必须通过 Tools/AutoTesting 发现真实 PlayMode FullName。每行中的不同动作均须得到运行覆盖，可以由一个参数化用例或同一流程完成。

| 任务／功能 | 最小相关 UTF／实际操作与关键断言 | 结果 |
| --- | --- | --- |
| 1.1–1.3 协议、终态、丢弃收尾 | 真实 handler 路由；两条命令分别终结；旧 Opcode；未交付／缺邮箱／锁等待退出／Stop 的一次 Dispose | NotRun |
| 2.1 移动及物品 | 原 Grid／备料／汤用例覆盖移动、拿放、掉落拾取、装容器、倒入、分汤；命令结果与守恒 | NotRun |
| 2.2 手工加工 | 原备料用例＋入口／Started／停手接续／完成消息；到点未投递不产物 | NotRun |
| 2.3 自动加工 | SoloDishBakeLocalReplayLocksInputAndProducesOneNewIdentity 等及汤用例；暂停与跨关后合法完成一次 | NotRun |
| 2.4 清理与供应 | SoloCleanup／SoloSupply：接水、排液、固体倾倒；采购、运输到货、接收、拆份；余额与内容守恒 | NotRun |
| 3.1 顾客订单 | SoloCustomerFlow*：来客、队满跳过、入座、询问后开单、到期／用餐后真实离场 | NotRun |
| 3.2 玩家／伙伴 | SoloCompanionWork／SoloCompanion：认领竞用、到位、半程接续、第三次成长、迟到旧消息 | NotRun |
| 3.3 出餐和原碗循环 | SoloServe／SoloCustomer／SoloFrontFlow：一次消费计分、清桌→洗碗间隙不结算、同一原碗洗净 | NotRun |
| 4.1 关卡控制与收尾 | SoloLevel：开店、暂停恢复、自然零星完成、Continue、旧关拒绝及新关就绪 | NotRun |
| 4.2 布局与 Buff | SoloStationLayout／SoloBuff：成功／拒绝、准备与候选、重复选择、旧上下文 | NotRun |
| 4.3 装修 | SoloRenovation：候选空／合法、选择一次、下一关安装一次、失败零安装 | NotRun |
| 4.4 已有存档 | 原案完成后 SoloSaveSelection／Commit／Restore：选档、取消、写盘失败、重试、恢复与迟到完成 | NotRun，依赖原案交接 |
| 5.1 会话与公共生命周期 | ProductSession 本地／现有 RealKcpSharesAuthorityKeepsAliveAndLeaveReturnsCommitted、WireInternalMessageCannotJoinOrTakeLocalIdentity 及必要 SoloMessage／LocalMessageShutdown | NotRun |
| 5.2–5.3 UI 与连续玩法 | SoloRestaurantUi＋普通 Launcher；新轮→两菜／前厅／供应清理→暂停恢复→自然结算→装修／保存→下一关，关闭重开和旧按钮 | NotRun |

运行顺序按 tasks 的依赖，先本地功能后必要 KCP 和真实界面。已有相关小集当次完整对应某项时直接复用，不再以集成为名重复。测试发现、过滤、原始输出和失败信息遵循 [自动测试 SOP](../../../Book/自动化测试SOP.md)，不复制证据包或修改原结果。

## 规划检查

2026-10-13 本轮规划检查：

- `openspec validate product-local-message-boundaries --strict`：通过。
- 6 份 Markdown、24 个本地引用：目标均存在；UTF-8 与尾随空白检查通过。
- 17 项实施任务均未勾选；十二类功能与验收对应已列出。ET 树已对照创建语句，持续 Companion 归 RestaurantRuntime，角色归 Level。
- CLI 的 planning complete 仅表示规划产物齐全；未执行生成、编译、UTF 或普通 Launcher，不代表功能完成。
