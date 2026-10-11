---
name: second-workspace-sync
description: 为稳定与测试用途，从本地 main 自主挑选必要基础功能及依赖提交，cherry-pick 到 multiplayer-baseline；用户调用即授权选择与维护，无需向主工作区请示，不开发或修源码冲突。
---

# 第二工作区必要提交维护

第二工作区用于稳定和测试。用户要求同步基础功能或调用本技能时，第二工作区自行检查本地main、挑选必要已提交功能及依赖并执行cherry-pick，无需主工作区预先给出清单或批准。选择和纳入已有提交属于版本维护，业务开发与源码冲突修复仍由主工作区承担。使用现有Git、`orca-cli`及需要时的`unity-cli`；不创建调度脚本或新worktree。

## 身份与输入

- main的本地目录固定为 `D:/MyWork/CarzyCooker`；该目录当前HEAD不一定是main，源版本始终读取本地 `main` ref，不用HEAD、origin/main或未提交文件替代。
- 第二工作区按同仓固定分支 `multiplayer-baseline` 识别，目录可变。加载现有 `orca-cli`，按本仓repo发现worktree；同一路径重复记录先核对Git归属，多个不同实际路径不得任选。确认实际分支与Git共同仓库，后续命令均显式目标目录。
- 直接用户请求可从会话和工程现场确定用途、实际目录、源main版本与最小验证范围，不因缺少runId或commit清单要求向主工作区请示。跨终端协作请求仍核对runId与目标；用户指定有序清单时只执行该清单，自主挑选适用于未指定清单的维护请求。
- 不修改主工作区或本地main的文件与分支。维护职责以用户最新授权、本端AGENTS和本技能为准；主工作区旧文档中的“只能由主工作区挑选”不覆盖已授权的自主维护。

## 第二工作区：自主挑选提交

查看本地main相对第二工作区尚未包含的提交，例如：

```text
git -C <main目录> log --reverse --no-merges --format="%h %s" multiplayer-baseline..main
git -C <main目录> cherry multiplayer-baseline main
git -C <main目录> show --stat --oneline <候选commit>
git -C <main目录> show <候选commit> -- <相关路径>
```

按稳定与测试需要，优先纳入基础运行功能、联机与请求生命周期、测试入口和必要Editor工具，以及它们的代码、协议生成结果、配置、包与工具前置。检查候选完整diff及先后依赖，形成明确有序清单；不能只按提交标题或路径过滤，也不能默认挑全部候选。没有本轮用途的实验玩法、纯进度/归档文档不纳入；功能提交随附的必要用法与规格保持完整。`git cherry`的负号表示等效补丁已包含，可结合历史/实际文件确认后省略；已经祖先包含的提交不重复应用。

检查所选提交引用的类型、程序集、包、协议和配置在目标基线中是否存在；缺少依赖时从本地main补选必要前置并按依赖顺序执行。依赖不明确或提交夹带无关实验功能且无法完整纳入时，跳过并报告原因，不拆写源码补丁。

对每个所选提交用 `git merge-base --is-ancestor <commit> main` 确认本地main可达性。默认选择普通提交；merge commit需已明确的mainline父序及理由，不猜父序。清单为空时报告无需维护，不造空提交、不切换源分支找替代提交。

执行前在当前线程简短说明选取、已包含与跳过的提交及原因，然后直接执行；不把该说明变成主工作区审批。维护会创建第二工作区的cherry-pick提交，但不授权其业务开发、推送或发布；推送须已有明确用户授权。

## 第二工作区：执行清单

执行前检查实际分支、`git status --porcelain`、当前未合并条目及Git操作状态，确认没有merge/rebase/cherry-pick在途。未提交身份/精简配置也属于脏现场：报告具体路径，不自动stash、reset、restore、clean或临时提交用户文件。用户明确授权的技能/入口维护改动可形成维护提交；不能借此提交其他用户现场。Editor必须处于本轮可维护状态；Play/测试/编译忙碌时按现有SOP拒绝，不擅自停止。无运行中Editor时可以维护文件，编译与界面验证记NotRun，不自动启动Editor。

挑选后若main ref移动，重新核对可达性和选定版本；若第二工作区HEAD变化，重新确认已包含/等效提交和清单。记录本轮起点与已经成功的提交，使用明确commit参数按选定顺序执行，例如：

```text
git -C <第二工作区实际目录> cherry-pick -x <commit1> <commit2>
```

不要用范围隐式扩展提交，不改为merge/pull或复制DLL。结果为空、依赖缺失或发生冲突时停止。只有确认cherry-pick是本轮自己启动且仍在途，才执行：

```text
git -C <第二工作区实际目录> cherry-pick --abort
```

回报冲突路径、实际最后版本和中止结果，交由主工作区修复；第二工作区不得编辑冲突源码、表、协议、测试或提案，不运行`--continue`修代码，不擅自`--skip`继续后续提交。中止失败保留现场并回报，不强制清理。

## 维护后核对与回报

- 核对源commit与目标新commit的实际结果、工作区状态；Git身份仅用于正常版本操作，不做hash校验或生成证据包。
- 固定产品身份仍为 `GDK / GameDevelopmentKit-NetTest`；只调整ProductName需要时走既有安全Editor入口，不用旧完整ProjectSettings覆盖新版其他设置。
- 本端核对 [短AGENTS模板](references/second-workspace-agents.md) 和其中允许的6项项目技能。维护不得恢复完整开发职责或开发技能；若候选会覆盖本端入口/固定身份，先确认完整提交可保留这些约束，否则跳过并报告，不制造待修源码冲突。用户要求修改维护规范时，同步本端AGENTS、技能与模板；不擅自修改主工作区。
- 有代码/包变更时执行与改动相称的最小编译检查；运行UTF或构建仍须明确范围与授权，纯文档不重跑UTF/联调。未执行写NotRun，空集/覆盖缺口不冒充通过。跨终端请求分别确认收件、开始、终态；直接请求在当前线程回报源/目标版本、成功清单、已包含/跳过理由、失败及原日志位置。

这是版本维护技能；源码修复和新增功能始终交给主工作区。原始日志留原位，长期工作区、Editor、终端及用户数据按本轮授权保留。

Git语义见 [git-cherry-pick 官方文档](https://git-scm.com/docs/git-cherry-pick)；运行/通信参考固定主工作区的 [协作规程](D:/MyWork/CarzyCooker/Docs/AI/multiplayer-peer-workflow.md)。提交选择以用户最新授权、本端AGENTS和本技能为准，不从旧文档恢复开发职责或主工作区审批要求。
