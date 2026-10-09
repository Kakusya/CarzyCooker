# 端口管理 SOP

**非规格；阅读不自动授权监听、服务或多 Editor。** 保留参考“发现 → 核对角色/消费者 → 冲突检查 → 释放”的流程；当前没有 Tools/PortRegistry 或 KCP 租约 API。

1. 新增监听、固定 endpoint、进程或多 Editor 前，读实际配置及全部直接消费者，明确协议、host、实际 port、用途和 owner。不将既有端口自动改为参考值。
2. Unity 编辑器操作只用 `unity pipeline list`，在同一会话按 projectPath 精确匹配当前工程；每条命令显式指定 `<repo>/Unity`。
3. Pipeline 端口由实例发现，只作外部连接，不绑定、预留或复制参考的固定范围；不把 Library/Pipeline 描述文件 token 写到输出。
4. 服务端/运行时网络读 [网络规格](../openspec/specs/network/spec.md)、[服务端规格](../openspec/specs/server/spec.md) 与 Config 当前启动配置。不要把工具 HTTP 和业务传输混用，不代定做饭传输。
5. 发现占用或多实例歧义时先报告实际进程/消费者，只停止本轮创建且已授权释放的监听/进程；不扫描并批量结束用户进程。
6. 简短报告角色、协议、工程与实际发现结果；不创建环境 JSON 快照。确需注册表、租约工具或新监听，另案列出用途/依赖/改动/验收。

调用入口：[port-management](../.agents/skills/port-management/SKILL.md)、[Unity CLI](../Book/UnityCLI.md)。
