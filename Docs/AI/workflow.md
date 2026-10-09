# OpenSpec 与 Codex 工作流

只维护 Codex skills，不配置其他代理、hooks 或 MCP；Unity CLI/Pipeline 已获接入授权，编辑器操作与技能新增分别按当轮任务和逐项审批执行。

## 安装与维护

依据 [官方安装说明](https://github.com/Fission-AI/OpenSpec/blob/main/docs/installation.md)，使用现有 npm：

```powershell
npm install -g @fission-ai/openspec@1.14.1
openspec --version
openspec init --tools codex --language zh --no-animation
```

Node 要求 20.19.0 或更高，迁移时为 24.21.0。初始化前检查已有 OpenSpec marker、commands 和用户目录 `opsx-*` prompts，避免意外删除；不修改 shell profile 或自动升级 Node。

1.14.1 core 含 propose、explore、apply、update、sync、archive。本机设 custom，在这六项上加 verify。`openspec config` 的 profile/workflows 是全局设置，会影响后续其他项目初始化/更新，不改其他项目现存文件。刷新用 `openspec update`，官方技能会再生成；项目规则放在入口、`openspec/config.yaml` 和自定义技能，不补丁官方技能。

## 意图到归档

需求/玩法/设计讨论按根入口硬规则，在分析前实际加载 `openspec-explore`；复杂/高风险需求在现状与候选方向梳理后使用 `carzycooker-requirements` 收敛。拷问输出决策账本，不自动创建提案；确认提案授权后才进入 propose，已有明确授权无需重复询问。阶段适用时主动读取技能，不只口头提及。

| 阶段 | Codex 入口 | 交付与边界 |
|---|---|---|
| 探索 | `$openspec-explore`、`$carzycooker-requirements` | 核实实现，澄清范围/owner/验收；探索不授权实施 |
| 提案 | `$openspec-propose` | change 下 proposal、design、specs 增量、tasks |
| 调整 | `$openspec-update-change` | 修订当前变更，未知项保留待裁决 |
| 实施 | `$openspec-apply-change`、适用项目技能 | 完成授权任务并更新 tasks，不扩大工具/产品范围 |
| 验证 | `$openspec-verify-change` | 核对实现、任务与规格；按风险和授权选检查 |
| 同步 | `$openspec-sync-specs` | 合并已落实增量，不把草案写成已实现 |
| 归档 | `$openspec-archive-change` | 完成后归档；不自动提交或推送 |

普通明确小修可直接编辑；行为、架构或跨层契约变化创建 change。不强制每次对话建任务。本次是实施已批准计划，不重新发起需求审批。

文档层级对齐后的正文在根 `KnownIssues.md` 和 `references/` 唯一维护；原 `Docs/Development/` 保留跳转。长期方向见 [long-term-goals.md](../../long-term-goals.md)，实际交付见 [当前进度](../../CarzyCooker当前进度.md)，尚缺配套见 [对齐记录](reference-alignment.md)。历史迁移限制不成为后续任务的永久禁令。

## 中文规格

正文中文，保留英文结构：`## Purpose`、`## Requirements`、`### Requirement:`、`#### Scenario:`；要求含 SHALL/MUST，场景使用 WHEN/THEN。增量使用 ADDED/MODIFIED/REMOVED/RENAMED Requirements。

主规格记录当前底座与协作契约。未来做饭设计在 `Docs/Product/`，实施后才建立玩法能力规格。候选、历史授权和冲突不晋级。

```powershell
openspec list
openspec list --specs
openspec status --change <name>
openspec validate --all --strict --no-interactive
```

格式验证不证明编译、玩法或联网成功。文档任务检查引用、源码对应和接入残留，不构建产品。生成/编译/运行按当轮授权，未运行写 NotRun。不执行 SHA 或额外复制 JSON 状态/证据。
