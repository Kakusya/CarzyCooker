---
name: build-acceptance-sop
description: 按 CarzyCooker 既有构建入口核对 HybridCLR、资源和独立 Player 包体验收；用于已授权构建或包体故障，不声称已接入双包编排器。
---

# 打包与包体验收 SOP

- auto-testing-sop：开发期在本工程 Editor 内运行真实 UTF 回归。
- 本技能：准出期按现有工具完成授权的热更、资源、Player 构建及包体验收。

先读 [根入口](../../../AGENTS.md)、[打包与包体验收 SOP](../../../Book/打包与包体验收SOP.md)、[官方 unity-cli](../unity-cli/SKILL.md)。正文只维护在 Book。

先确定平台和执行范围，核实目标模块/许可与既有 BuildHelper。保留 HybridCLR，不按参考删除 AOT/热更资源。每阶段分别验收，提交构建命令不等于完成。

专用 Tools/BuildAcceptance 双包编排和 Player 测试参数未接入，不能运行不存在的脚本或宣称七项矩阵已通过。无头构建须避免同工程 Library 争用并保留 stdout/stderr。失败给原输出，NotRun/Skipped 如实；不新增 JSON 证据、hash 或自动提交。
