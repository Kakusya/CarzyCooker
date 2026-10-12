# 执行说明

规划阶段检查：2026-10-12，`openspec validate solo-local-message-contracts --strict` 返回有效，退出码 0；当时仅证明规划有效。后续 apply 的实际实现和运行结果见下文，不以规划检查代替功能验收。

2026-10-12：用户授权持续建立单机 change，不施工。已沿当前源码核对 owner、两段本地 Call 和 fixture；所有未来实施／生成／UTF／入口验收均为 NotRun。

## 严谨程度与取舍

2026-10-12 apply：用户授权完成九个 change。本工程无运行 Editor，启动本轮持有的 Unity 6000.3.18f1 batch Editor（PID 65000，带渲染，项目 D:/MyWork/CarzyCooker/Unity）。复现用例 `ET.ProductSessionTests.SoloMessageCanceledOwnerKeepsPhysicalPeerUntilReply` 经真实 PlayMode UTF 运行，1 项 Failed：取消等待后 `InFlight` 预期 1、实际 0（SoloMessageTests.cs:66）。这是修复前的真实失败，不记通过；原始作业在 Unity/Temp/pipeline_test_status.json，Editor 日志在 Unity/Temp/solo-nine-changes-editor.log。新增测试初次编译的 EntityRef 访问错误已修正，随后编译无错误并发现实际用例。

主要风险是取消后 Peer 提前释放、池化响应被重复收尾、跨关旧命令误写和测试隐式推进业务时间。这一改动影响共享入口和验收可信性，采用少量真实 MailBox／等待终态交错测试，比只测纯函数更合适。不重写 ET.Core，不建新框架或历史结果库。已有发送器 Stop／跨实例隔离与所属异步结果可按未变范围复用；产品在途交接改动必须取得新结果。

接受跨设备、KCP、Player/AOT、任意消息穷举及无限历史恢复未验证；单 owner 取消后原物理 Call 仍可能保持到原 40 秒超时。出现无法收尾、旧写回或其它 owner 被停止时补对应局部用例，不扩大全仓测试。

## 决策与来源

- 用户明确仅单机；产品单玩家，测试可创建额外本地调用者验证隔离，不构成多人产品交付。
- 最新业务编号保留有限结果，旧编号明确过期；不自动重试、不回滚已提交事实。
- `CallProductValueAsync` 外层 EndEntry 的取消寿命差异已由真实用例复现并修复，失败与新通过运行分开记录。
- 既有 fixture 加载实际 Tables，拒绝替换活跃 World，复用现有 AutoTesting／UTF。

## 验收映射

以下用例已经在当前实例实际发现并按 PlayMode 运行，完整前缀 `ET.ProductSessionTests.`。源码／协议变化后运行 7 项 SoloMessage，7/7 Passed、Console 覆盖完整；另运行受影响的 `GridWireSoloAndLocalUseSameTickAndRejectStaleIdentity`，1/1 Passed。初版 5/5 仅对应修复／fixture，小集追加后以新 7/7 覆盖上下文结果。

| 任务／功能 | 最小 UTF 或实际操作 | 结果 |
| --- | --- | --- |
| 1.1–1.2 在途和收尾 | SoloMessageCanceledOwnerKeepsPhysicalPeerUntilReply：旧 Peer／MailBox 保留、另一本地调用者正常、输入／响应各释放一次；TimeoutReleasesEntryAndDisposesLateResponseOnce、WholeStopCancelsHeldCallAndRetiresItsLateInput、StoppedSenderAndInactiveOwnerReleaseUnsentInput 覆盖原超时／Stop／未交接同步失败 | Passed；初版 1 Failed 保留为失败事实 |
| 2.1–2.2 身份与编号 | SoloMessageSharedSequenceLatestFactsAndContextRejections：共享编号、最新回执／最终事实、载荷冲突、旧编号、旧世界／Match／绑定／关身份／协议拒绝 | Passed |
| 2.2 投影所有权 | SoloMessageOneItemTransferAndIndependentProjection：真实拿取／重放只有一次；池化快照回收后手槽、工位、物品值仍有效 | Passed |
| 3.1 fixture | SoloMessagePumpAndBusinessTimeAreIndependent：真实 Solo 无监听、只泵消息 Tick 不变、AdvanceTicks 精确一步；各用例 teardown 后独占创建，无活跃 World 重入 | Passed |
| 3.2 输入与跨轮 | 普通 Launcher 两轮 Solo；真实碗台 Button PointerClick，各轮 NextCommandId／结果编号均为 1、Executed、手持真实碗且在途归零，新轮 WorldId 不同且起始编号／手槽为零 | Passed |
| 规划文档 | 正文、引用、状态与严格 OpenSpec 校验 | Passed；最终九案严格有效，55份Markdown／450个本地引用及局部diff检查通过 |

原始运行输出留在既有工具目录；这里只填结果、失败原因及原路径，不建 JSON 证据包。

协议只追加已有消息字段，没有插入新类型；两个 Client／ClientServer 输出正常 Proto2CS 生成，旧 opcode ID 文件无语义 diff。工具默认查找 .NET 8 runtime，但环境仅安装 .NET 10.0.8；仅该工具进程设置 DOTNET_ROLL_FORWARD=Major 后 `--AppType=Proto2CS --Console=1` 正常成功，没有安装依赖或更改工程目标。Unity 完整 recompile 无错误；静态依赖环通过把编号原语置于原 Hotfix 的 ProductCommandClient 消除，不添加程序集。

fixture 用法：StartSolo 加载真实 Tables，创建无 NetComponent 的原产品树；PumpMessages 只执行 Fiber／队列更新，不改变受控 clock；AdvanceTicks 每次加 50ms 后走同一更新路径。操作经真实 Call，检查回执后再推进 Tick；不得同时直接调用 AdvanceGridTick。Cleanup 只 Stop 本测试的 sender／Fiber／World 并复位原上下文，活跃 World 拒绝条件保持。

普通 Launcher 验收 Passed：batch Editor 中 ProcedureComponent 停在 WaitForEndOfFrame，退出本轮 Play、确认场景无未保存修改并正常关闭。普通 Editor PID 39328 使用原 Assets/Launcher.unity，不改入口，临时脚本只经真实 PointerClick 发动作、读取事实。第一轮世界 `38be8624529f4b17aa622b1db4dd7e69`，第二轮 `e3ad80d88ffa4c2281358cf645b925a1`；两轮一次点击各发编号 1，最终 Executed、手持原碗、Physical InFlight 为 0。新轮点击前编号／手槽为零，无旧回执安装；未直接修改世界或安装期望状态。为后台验收临时设置 Application.runInBackground=true，不改 PlayerSettings。原日志 Unity/Temp/solo-nine-changes-editor-interactive.log，Console 会话 68cf1167f0ba4ee692e314b87dcdb762 的 59 项（seq 2–60）覆盖无丢失／reset，Error／Exception／Assert 为零。

另执行受共享适配影响的 `ET.LocalMessageShutdownTests.OwnedLocal_CancelOnlyBusinessWaitAndLetOriginalReplyRetire`，1/1 Passed；真实小集合计 7＋1＋1 个不同用例，不用初版 5 项重复凑计数。整个原 Scope 停止套件未变部分复用历史证据，不跑 KCP 或全仓。

逐 Requirement 核对：上下文／可信入口对应 ProductCommandContext 和 GridPeer／Submit；编号与有限重放对应 HostPeer 及统一客户端原语；接收／最终结果对应真实消息泵与 Tick；取消收尾对应内部 physicalFinished；隔离测试对应 StartSolo／PumpMessages／AdvanceTicks。五项 Requirement 都有上表行为证据，正式 Level 创建后的非零关身份由流程 change 接入，保持当前原型零关身份边界。所有六个任务实际完成，后续修改到的行为仍需对应补验；不自动归档、同步主规格或提交。
