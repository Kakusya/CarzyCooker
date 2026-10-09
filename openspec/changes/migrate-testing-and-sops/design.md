# Design

## Context

参考流程与当前工程存在 API/架构差异。当前 CLI 1.0.0-beta.11、Pipeline 0.8.0-exp.1、Editor 6000.3.18f1 已可达，编辑器 ready 且 Play stopped；源码的 TestCommands 支持异步单模式及 test_status。参考 JSON 工件、默认四条 mega、自动取消、productName 修改不符合本次约束。

## Goals / Non-Goals

目标是迁移可执行的工具与可路由 SOP，保留参考流程顺序及适用语句。非目标是新增产品用例、双包编排、端口租约、Prefab runner、业务入口、Unity MCP 或第三方 Python 依赖。

## Decisions

1. 自动测试脚本保留 run.py 入口与发现/前置/执行/轮询/Console/结果流程，内部按当前接口重写。使用标准库 subprocess 参数列表、结构化 JSON 解析、有限等待和同工程临时独占锁。相比机械复制参考编排器，这能去掉没有本项目消费者的证据与身份工具。
2. 默认列测试，执行需 --run、--mode 和 --filter。不编造默认套件；空集 NotRun、全跳过 Skipped。前置状态拒绝繁忙，而不是自动 cancel 或 editor_stop，以保留用户现场。
3. 当前 Pipeline 无稳定作业 ID，脚本不能从旧结果恢复。单写者约定、当前临时锁与 preflight 防止本工具重复启动；原生外部命令仍需遵守单飞规则。超时只报告，不自动取消。
4. 使用 Console session/cursor 检查本轮新增错误；会话重置或丢失不声称覆盖完整。原始 Pipeline 状态文件保持原位，不额外复制 JSON；不会清 Console。
5. 官方 unity-cli 由已装 CLI 自带安装器生成，项目流程放 Book/references，技能入口只路由。错误压缩器保留参考实现，增加当前 Pipeline entries 的读取适配，不迁旧读取脚本。
6. SOP 与独立工具分开：打包、端口、UI 结构用现有 API/人工检查完整描述步骤，明确专用编排器尚未接入。不因这次 SOP 授权安装渲染、包管理等独立官方候选技能。
7. 原生 UTF 忙碌检查使用已有 eval 对当前 UTF 1.1.33 注册表作只读查询；结构未知则拒绝执行，不添加 Editor 代码。Console tail 截断不一定标记 dropped，因此读取全部级别并核对 seq 连续性及原生 Error 增量；终态名单也与发现范围一致才能证明本轮结果。

## Risks / Trade-offs

- 没有产品 UTF 套件 → 实际只读发现与 Python 模拟回归验证工具；明确产品测试 NotRun。
- PlayMode 域重载可能丢 Console 会话 → 报告覆盖缺口并拒绝完整通过，不吞错。
- 其他操作者绕过独占锁启动原生命令 → 文档要求同工程单写者，不提供旧结果续跑。
- UTF 内部注册表版本变化 → 源码核实当前结构，查询不兼容时受控失败，不继续执行或自动升级包。
- 源码未提供参考双包/资源 request 标志等 API → SOP 列真实菜单/源码，不伪造现成命令。

## Migration Plan

先迁工具/技能/SOP，再更新路由和状态，运行自身回归及只读实例/测试发现，严格验证并同步主规格。保留现有未提交修改；不自动归档、提交、推送。回退只针对本次新增文件和具体 diff，不清空仓库或恢复旧工作流。
