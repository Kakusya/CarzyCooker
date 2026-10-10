# Proposal

## Why

本地请求还在等待答复时关闭 ProcessInnerSender，现有 Destroy 只移除消息队列，没有主动结束等待。需要在原有消息机制中补齐“停止接单、结束等待、清理资源”，避免退出依赖超时，或让旧答复与旧计时影响重建后的运行。

## What Changes

- 为既有本地消息发送器增加可重复调用的停止入口，停止后拒绝新请求；销毁入口使用相同收尾作为兜底。
- 从现有等待名单取出未完成请求并先清空，再直接通知退出终态，不通过已经关闭的消息队列发通知，也不等待远端回复。
- 明确退出结果：沿用 ERR_Cancel；需要异常的调用收到 RpcException，不需要异常的调用收到对应失败响应，保留正常成功、既有业务错误和超时分支。
- 正常答复、超时与停止只有一种终态；旧等待任务检查实例身份，请求编号在当前托管域内不跨发送器重建重复，防止旧答复误认新请求。
- 补上真实本地消息生命周期的 UTF 回归与使用说明，证明退出期间的等待结束、重复停止安全及下一轮不串结果。

## Capabilities

### New Capabilities

无；沿用现有消息领域。

### Modified Capabilities

- `network`: 新增进程内请求的停止／销毁终态、单次完成、旧消息隔离和兼容范围；不修改 Session 的既有契约。

## Impact

- 改动定位为 ET.Core 的 ProcessInnerSender、ProcessInnerSenderSystem，及必要的 MessageSenderStruct 元数据／收尾支持；该源码也被 DotNet.Core 引用，须验证对应编译边界。
- 复用 MessageQueue、现有等待者、UniTask 和 UTF；必要时只增一个 Editor 测试程序集，不新增消息框架、协议字段、业务程序集、队列服务或持久状态文件。
- 使用说明同步 references/reactive-and-network.md 与 Docs/AI/module-index.md；主规格同步在完成实施与验证后进行，本轮只创建增量规格。
- 本项独立于已建立的启动项和第二工作区 change，不创建 worktree，不调整 ProductName、日志或端口。后续产品会话再调用该停止能力，本项不创建产品 SceneType、成员、网络桥接或自动重连。
- 结束等待不等于撤销接收端已经执行的操作；本项不提供跨进程重启或托管域重载后的请求恢复。
- 当前源码事实与新增契约分开记录。本轮只形成规划，未修改代码、运行 Editor 或测试。
