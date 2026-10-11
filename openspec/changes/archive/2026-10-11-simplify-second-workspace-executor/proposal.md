# Proposal

## Why

用户已明确第二工作区只听主工作区指令执行，不承担开发。当前366行AGENTS和21项项目技能仍复制主要开发流程，且共同规程允许获准修源码，与最新职责冲突。

## What Changes

- 新建项目技能 `second-workspace-sync`：主工作区挑选本地 `main` 上已提交的必要commit，核对依赖与重复，再把明确有序清单cherry-pick到固定分支 `multiplayer-baseline`；第二工作区只执行清单，不选择需求或修冲突代码。
- **BREAKING**：第二工作区禁止开发，包括写/修业务源码、修改表/协议源、创建提案及解决源码冲突；编译、运行、观察、获准测试/构建和指定提交维护属于执行。
- 以短AGENTS模板替换第二工作区完整开发入口；在其实际目录物理删除12项OpenSpec及4项业务开发技能目录，保留5项运行/验证技能并加入维护技能。主工作区和账户共享技能不删除。
- 固定main本地目录与第二工作区分支身份，第二工作区目录动态发现；保留独立ProductName、缓存、日志和用户现场。
- 同步共用规程、主规格及手册，保留原联调历史与原始日志。严谨取舍见 [execution-notes.md](execution-notes.md)。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `ai-collaboration`：职责从完整共同主体改为严格执行端，技能按职责物理精简，保持共同现场保护规则。
- `multiplayer-peer-workspaces`：按固定分支动态定位目录，版本维护从全量升级改为main必要提交的明确cherry-pick，失败回报主工作区。

## Impact

主工作区新增 `.agents/skills/second-workspace-sync/SKILL.md` 和执行端AGENTS模板，修改AGENTS/共用规程/相关手册与规格；第二工作区修改AGENTS、安装同一维护技能并删除16个开发技能目录。只依赖现有Git、Orca、Unity CLI和现有Python校验器；不新建服务、脚本管线、worktree或业务程序集。本轮不推送、不发布、不合回main、不归档，也不为技能验收向实际第二工作区重放业务提交。
