# 实施与验证取舍

当前实施分支 `feat/multiplayer-bootstrap`，起点 `e71a6bac`，开始时工作区干净。历史规划中的“本轮不实施”指规划阶段；2026-10-11 用户已授权四项实施、自审、本地提交及必要验证，禁止 push、PR、发布、合回 main、自动归档。

主要风险是启动请求被另一工程消费、连续 Play 沿用旧角色及 Prepared 被误报为产品 Ready。先用配置与真实 SessionState 小集验证，不做 Player 构建或全量测试；再用当前 Editor 的真实 Play 交接补齐生命周期，复用已有通过结果。配置校验不打开网络，产品阶段由后续会话 change 接入。

当前证据：本工程 Pipeline 编译完成，`failed=false`、`compilationFailed=false`；Console groundTruth 无当前编译错误。EditMode `CarzyCooker.Tests.RuntimeInstanceTests` 10/10 Passed，含非法输入、两份真实请求作用域、失败保留、旧阶段拒绝与 UTF 忙碌拒绝。原始作业输出保留 `Unity/Temp/pipeline_test_status.json`，原 Editor 日志 `C:/Users/Administrator/AppData/Local/Temp/CarzyCooker-Multiplayer-Editor.log`。首次 API/Analyzer 编译错误已修复，未清 Console。

单 Editor 已实际验证：默认两轮 Host→Client 分别保留本轮 runId/role 并在 ProcedureET 报 ProductBootstrapUnavailable，CodeRunner=false、无 FiberManager；合法 legacy 为 Prepared、CodeRunner=true、已创建 ET Fiber；其后普通 Launcher 为 NotRequested、Config=null，不继承旧角色，仍创建 ET。Play 中再次启动拒绝为 EditorBusy。关闭 Scene Reload 的显式入口拒绝。关闭 Domain Reload/保留 Scene Reload 的 Host→Client 两轮也各消费新配置，未复用旧轮角色。

真实 Play 编译中断曾发现 UGF DebuggerComponent.Update 与 EditorResourceComponent.Update 在新域持续刷空引用；原始日志保留。收尾改为在本轮显式标记下通过 ProcedureET.StopOwnedEntry 停止实际拥有的 CodeRunner，暂时停用本轮 GF root 的更新，再结束自身 Play；新域恢复中断失败，下一轮清理中断标记。普通未提交运行不由该钩子停止，GF 保存/Shutdown owner 未重写。最终中断实测为 Play stopped、Failed/PlayDomainReloadInterrupted、无 CodeRunner/Fiber，自本次 Console cursor 起没有新增 Error，覆盖无 dropped/reset。随后新的 legacy 能启动，StopOwnedEntry 连续两次无异常，CodeRunner=false、FiberManager=null，证明重复清理与新轮标记恢复。

验证仅临时改变当前 Editor 的 EnterPlayModeOptions，并恢复默认 Domain/Scene Reload；没有提交 Play 设置、场景、用户数据或临时 eval 文件。Unity 6000.3 的旧 enterPlayModeOptionsEnabled setter 会改变 flags，测试改用实际 flags 核对，不以旧布尔字段推断配置。双 Editor、应用数据隔离和 KCP 归后两项，Player/Android 与跨域恢复不验证；只有实际交接丢失或旧轮写入触发对应补验，不因文档收口重跑已通过的小集。

自审修复包含 Unity endpoint API/Analyzer 适配、普通 Launcher 保持原 Scene Reload 兼容范围、Play 编译中断的实际 owner 收尾，以及中文文档管道编码。配置 UTF 10/10 与本地消息 UTF 9/9 的适用结果复用；没有为文档收口重跑测试。startup/runtime-instances 主规格同步，严格格式与本地引用检查收口后更新 tasks。

收口：12/12 tasks，187 个相关本地链接有效。人工源码/文档的 staged whitespace 检查无错误；全量检查只报告 Unity 自动生成的 7 份 folder/asmdef `.meta` 默认空值行尾空格，按生成物只读要求保留，不手改或扩大 Git 规约。
