---
name: port-management
description: 在 CarzyCooker 新监听、固定 endpoint 或多 Editor 任务前核对实际端口角色、工程发现、消费者和释放边界；使用现有配置与 CLI，不假设已安装 PortRegistry。
---

# 端口管理

读 [端口 SOP](../../../references/port-management.md) 与 [Unity CLI](../../../Book/UnityCLI.md)。在变更监听、连接固定 endpoint 或启动多实例任务时加载，不在普通文档任务中启动服务。

Pipeline 用 `unity pipeline list` 按 projectPath 精确发现，所有 Editor 命令显式指定工程；不固定、扫描、预留参考范围或输出 token。现役服务端配置/消费者按源码核对，业务传输未决项不代定。

当前没有 Tools/PortRegistry 或运行时租约 API，不执行参考脚本或恢复旧工具。冲突先报告实际 owner，仅释放本轮拥有且已获授权的资源，不强占或批量停止用户进程。
