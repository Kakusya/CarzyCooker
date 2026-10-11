# 实施与验证取舍

本轮已授权实现原 sender Stop；主要风险是完成等待时重入删除新队列、迟到计时/答复误认新请求，以及共享源码只在一端编译。使用真实 Call、Queue、Mailbox、Timer 和 Dispose 的独占 UTF 小集，测试时间推进真实 Timer，不降低生产 40 秒超时。测试拒绝覆盖已活跃 ET World，finally/Teardown 释放自身环境。

Unity 最终核心与测试程序集编译完成，`failed=false`、`compilationFailed=false`，Console groundTruth 无当前编译错误。`dotnet build DotNet/Core/DotNet.Core.csproj` 在必要 NuGet 恢复后成功，0 警告、0 错误，耗时 28.97 秒；后续核心释放顺序/构造修改的必要增量编译为 1.08 秒，同样 0 错误；未启动服务。

PlayMode `ET.LocalMessageShutdownTests` 最终 9/9 Passed，Console 覆盖完整，工具退出 0。覆盖真实成功/业务错误、两种取消/超时、直接 sender/Fiber Dispose、重入与批次内 Stop、实际池化 sender 复用、真实旧计时恢复和旧答复进入新队列、编号耗尽、Opcode/Pool/Timer/Queue 单例退出及替换队列隔离。前两次 6 条运行全部被过宽的测试环境判定挡住：实际仅有 EditorLogHelper 的 Options/Logger；安全检查改为拒绝活跃 Fiber/CodeTypes/Queue/EntitySystem。首次加严 8 条时 7 通过、1 失败，原因是测试推进时钟却未更新 FrameTime，导致新请求也用了旧时间；修夹具后最终 9 条通过，未降低生产超时。

自审修复：异常响应先释放后通知，避免 await 重入销毁池后继续回收；本地提交时捕获响应类型，MessageSenderStruct 的可选元数据不改变 ProcessOuterSender 原构造行为，发送目标不存在时不分配无用等待者。network 主规格已同步，严格 change/main-spec 检查通过，未归档。

收口自审：完整性 9/9 tasks，正确性覆盖增量 4 项要求与 10 个场景，一致性对齐原 owner/队列/等待名单，没有新协议或等待表。136 个相关本地文档链接全部存在，git diff --check 无空白错误。发现与修复均如上，当前范围无剩余阻碍；本地回归不证明产品联机或双 Editor。

原日志和 UTF 原始输出保留原位，沿用启动项的 Editor 日志，不新增证据副本。最小回归覆盖两种取消/超时结果、重入、旧回复与旧计时、耗尽拒绝；只有新失败或确切未覆盖的生命周期才补跑。明确不验证跨托管域恢复、网络会话或产品玩法。
