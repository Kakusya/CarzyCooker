# Tasks

全部尚未实施。本轮只捕获规划。业务交接已经明确逻辑成员独立于连接，删除／保留不是待裁决问题；自动重连与厨房规则仍不在本项实现范围。

本期目标及三条范围已在会话中确认，短摘要见 [proposal 的快速审核](proposal.md)。下列技术任务由 Agent 按规格实现和验证，不再次要求用户逐项审阅；实施仍需后续明确 apply 授权，新增行为取舍另行讨论。任务未勾选不表示已确认范围需要重新裁决。

## 1. 业务契约与前置能力

- [ ] 1.1 对照交接 A3／G3／G5 与业务目标规格，落实最小 Match／Participant 和连接绑定数据边界；核对 proposal、design、product-session 增量及 5.2 的离线保留／显式 Leave 场景一致，不恢复多余裁决门槛，不实现重连凭证或厨房。
- [ ] 1.2 核对启动项配置与本地请求 Stop 已实际实施且适用验证完成，确认真实 CodeMode、Model／Hotfix／Loader 引用和 UTF 程序集；记录缺失的明确前置，不能以其他 change 文件存在当作能力完成，不自动 apply 其他项。

## 2. 产品根与角色子树

- [ ] 2.1 在既有 Model／Hotfix Share 产品目录落实 design 的 owner 树、最小字段、产品 SceneType 与显式初始化；验证 Participant 归 Match、Session 只引用 Peer、索引与投影没有第二份名单，每 Fiber 一份 sender，ActorId／Mailbox 所属准确，无新业务程序集。同步树和唯一写入口说明。
- [ ] 2.2 接入 ProcedureET／Entry 的同一配置分流与阶段报告，保持 Loader 装载和普通／legacy；验证产品路径不发布 Main 示例事件，消息泵启动前不等待加入，缺能力与异步失败保留真实终态。同步 Book/快速开始.md 的入口和就绪含义。
- [ ] 2.3 补根初始化失败、Host 仅监听、加入中退出、连续两轮和普通路线 UTF；在 references/reactive-and-network.md 记录产品 owner 树及发送／接收目标，测试缺环境记 NotRun，不将根存在当作联机通过。

## 3. 本地消息与共用裁决

- [ ] 3.1 按 design 协议表修改启用 Proto 源并经既有 Proto2CS 生成 Join／Probe／Leave／名单／心跳／关闭及内部消息；验证 MatchId、成员与绑定代次、版本、必需字段、载荷限制和外层／内层响应映射，确认 Opcode 未重排且 Client／ClientServer 输出适用。不手改生成物；同步字段、限制和协议版本说明。
- [ ] 3.2 实现 LocalMessageComponent、可信 HostPeer 与 HostSession 有序 handler，以真实 sender.Call／队列／Mailbox 完成本地 Join／Probe／Leave；验证 Peer 接收及提交前二次资格核对、未加入、伪造身份、重复加入／退出及并发交错，无 UDP 自发或等待表副本。同步调用、接收、身份、答复及关闭内部消息路径。
- [ ] 3.3 实现 Match 内不复用 MemberId、递增完整名单及共同客户端应用；验证通知先到但尚未确认 Match、Join 答复较旧、提交后答复丢失、旧版本／旧绑定、消息回收复用与客户端修改，确保深复制且 Joined 需要本人有效确认。按事件表验证提交后观察和 All 匹配，不借根 DynamicEvent 假定隔离；同步投影、事件和所有权。

## 4. 远端直连与生命周期

- [ ] 4.1 在现有 UdpTransport 暴露必要只读实际绑定地址，由产品 Host 创建并持有 NetComponent；真实 loopback 验证端口 0 回报非零、指定端口、冲突失败和释放，不用工具端口或新增注册服务。核对 ET.Core 同源编译并同步本机启动用法。
- [ ] 4.2 实现 ProductHost／ProductClient 精确 OnRead Invoke、wire 白名单、参数校验及可信 Session 绑定，复制转交请求并恢复原 wire RpcId；真实 KCP 验证远端与本地进入相同 handler、并发编号不串、内部消息不可直达、失效 Session 不接收晚到结果。同步消息桥接、失败类型和每段释放说明。
- [ ] 4.3 实现加入通过后解除接入超时、有限连接／请求超时及每连接一个在途心跳；测试协议不兼容、无监听、接入超时、空闲保活、失联及旧计时恢复，本地无网络心跳；文档明确实际参数与既有阈值，不复用 Demo Gate 协议。

## 5. 分层收尾与失败回归

- [ ] 5.1 延用已有 OnShutdown Invoke 键接产品同步 Stop，补 Init 在 Runner 尚未创建时的失败清理；验证整体先拒绝新工作、结束本地／网络等待再释放树，重复退出及迟到初始化不创建新轮，记录实际步骤和直接销毁兜底。
- [ ] 5.2 实现断线保留 Participant 并标离线、显式 Leave 移除成员和 Match 解散；验证提交前／后加入中退出、重复清理、旧绑定晚到、新连接无凭证不能接管旧身份及 Host 本地玩家单独 Leave。单连接关闭不停止共用 sender／Host 监听，其他成员 Probe 仍有效，状态版本只变更一次；同步名单和原型不含身份恢复的边界。
- [ ] 5.3 补齐完整退出交错和对象复用 UTF，使用真实 Call／Timer／Mailbox／Dispose 与原消息所有权；核对 Console 无本项未预期异常、预期失败有明确结果，同步 references 与 Docs/AI/module-index.md 的生命周期边界。
- [ ] 5.4 实现 Peer Closing 时先关闭资格、保留 Mailbox、记录入口在途计数并在请求结束后释放；用真实队列回归关闭前入队、共同提交前失效、成功 Leave 回复后关闭和未提交 Leave 先断开。验证正常失效请求返回拒绝而不拖到 40 秒、异常走既有超时、计数归零才释放且其他成员 Probe 有效；不修改核心单请求取消接口、不新增等待表，并同步退出顺序。

## 6. 集成验收

- [ ] 6.1 按精确本工程 projectPath 执行获准 Pipeline 编译、Console groundTruth 及实际发现的本项 UTF，核对 ET.Core、Model／Hotfix、Loader／HotfixView 和测试程序集；核对同源 DotNet 的适用编译，不启动服务或 BuildPKG。未运行、空集、跳过和失败分别报告。
- [ ] 6.2 第二工作区真实可用且两端包含必要版本后，按 design 验收矩阵验证开发 Host／稳定 Client 及角色反转，观察实际 endpoint、MatchId、成员号、同版本名单、Probe 请求／答复、心跳空闲、单端断线和整体退出；原日志保留原位，未满足条件记 NotRun，不把同机双 Editor 视为物理两机 LAN 验收。
- [ ] 6.3 核对本项五份规划与已实施行为、Book/Unity启动与验证SOP.md 和模块索引一致，检查所有引用、空白和 OpenSpec 严格格式；分别报告单端、本地消息、实际 KCP 和双 Editor 范围，不以编译或代理通信冒充产品联机。

## Workflow follow-up

- 本轮不实施，不勾选任务，不同步主规格，不归档、提交或推送。规划文件的 done 状态不代表前置能力或产品已实现。
- 后续明确 apply 请求进入实现；启动项、本地退出和第二工作区各按自己的范围与授权推进，本项不重复创建工作区或接管产品名／日志。
- 完成实现和必要验证后再同步 startup／product-session 增量与归档；固定 Player 包、正式传输、自动重连和玩法回归继续另案讨论。
