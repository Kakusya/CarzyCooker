# Proposal

## Why

项目已经安装 OpenSpec 与 Unity CLI/Pipeline，但参考的自动测试编排和独立 SOP 只保留在清单中，无法作为实际入口使用。用户本轮明确要求迁移，沿用适用原文与流程，并保留源码、工具和玩法验证的边界。

## What Changes

- 接入版本匹配的官方 unity-cli skill，不自写替代品。
- 迁移自动测试编排流程到 Tools/AutoTesting：发现、过滤、前置检查、异步运行、终态等待、失败输出、Console 检查；仅使用 Python 标准库和现有 CLI。
- 迁入自动测试、打包验收、启动验证、资源导出、错误诊断、端口管理、C# 规范、动态 UI 和 OpenSpec 收尾 SOP；完善已有 Luban/UI/Entity/需求技能。
- 保留可复用的错误压缩脚本与验证，去除旧工具读取方式。
- 同步 AGENTS、手册、模块索引、迁移状态及逐字改动说明。

## Capabilities

### New Capabilities

- `automated-testing`: 经当前工程发现与命令核对执行指定 UTF 测试，真实报告空集、失败、超时和跳过，不生成额外证据副本。

### Modified Capabilities

- `ai-collaboration`: 已授权迁入的 SOP/技能可路由，缺少专用工具的流程明确使用现有入口，不重复申请同一授权。

## Impact

改动 Tools/AutoTesting、.agents/skills、Book、references、Docs/AI、AGENTS 与对应 OpenSpec 文件。不新增 Python 第三方依赖，不修改 Unity 业务/测试程序集、场景、配置表或架构。不迁参考专属测试、productName 修改、JSON 证据与 hash 管线；独立双包编排、PortRegistry、UI 注册表/runner 等仍是另项工具。此次验证运行工具自身测试及真实只读发现，产品构建/玩法测试 NotRun；不提交或推送。
