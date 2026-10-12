# Proposal

## Why

单机的菜品、布局、前厅和关卡控制都需要可验证的请求与结果。现有 ET 本地链路已成立，下一步应统一产品命令的身份、单次执行和退出测试，而不是再建消息框架。

## What Changes

- 在既有产品入口补齐类型明确的命令上下文、接收回执与最终结果；新功能复用原发送器、MailBox 和所属等待。
- 定义最新编号重放、同编号不同载荷、旧关／旧绑定和取消等待的行为；不承诺保存所有历史结果。
- 扩展现有 ProductSessionTests 的本地建树／泵消息方法，让测试经真实 handler 再推进业务 Tick。
- 本轮只建立规划；不实施、不启动 Editor、不执行 UTF、不增加网络监听或测试框架。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `product-session`：增加面向单机产品命令的上下文、结果重放和真实本地测试契约，保持已有会话与发送器规则。

## Impact

适用 Model/Hotfix Share/Product、OwnedRequestExtensions、现有 ProductSessionTests 及后续类型化消息；不改 ET.Core 等待表。是其它单机 change 的共同前置。[设计](design.md) 写明实际树与两段 Call；验证投入见 [执行说明](execution-notes.md)。
