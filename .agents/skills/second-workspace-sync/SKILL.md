---
name: second-workspace-sync
description: 从本地 main 挑选必要已提交 commit，并按主工作区明确清单 cherry-pick 到固定分支 multiplayer-baseline；第二工作区只执行版本维护，不开发或修源码冲突。
---

# 第二工作区必要提交维护

主工作区负责选择和协调，第二工作区只执行明确有序清单。使用现有Git、`orca-cli`及需要时的`unity-cli`；不创建调度脚本或新worktree。

## 身份与输入

- main的本地目录固定为 `D:/MyWork/CarzyCooker`；该目录当前HEAD不一定是main，源版本始终读取本地 `main` ref，不用HEAD、origin/main或未提交文件替代。
- 第二工作区按同仓固定分支 `multiplayer-baseline` 识别，目录可变。加载现有 `orca-cli`，按本仓repo发现worktree；同一路径重复记录先核对Git归属，多个不同实际路径不得任选。确认实际分支与Git共同仓库，后续命令均显式目标目录。
- 维护请求包含runId、实际目录、源main版本、用途、依赖有序commit清单、运行/验证范围。接收方不得扩展清单；第二工作区缺少清单时回报主工作区，不自行选择提交。

## 主工作区：挑选提交

查看本地main相对第二工作区尚未包含的提交，例如：

```text
git -C <main目录> log --reverse --no-merges --format="%h %s" multiplayer-baseline..main
git -C <main目录> cherry multiplayer-baseline main
git -C <main目录> show --stat --oneline <候选commit>
git -C <main目录> show <候选commit> -- <相关路径>
```

按当前维护目的挑选必要代码、协议生成结果、配置、包与工具前置。检查完整diff及先后依赖；不能只按提交标题或路径过滤，也不能默认挑全部候选。`git cherry`的负号表示等效补丁已包含，可结合历史/实际文件确认后省略；已经祖先包含的提交不重复应用。

对每个候选用 `git merge-base --is-ancestor <commit> main` 确认本地main可达性。默认选择普通提交；merge commit只有在主工作区明确给出mainline父序及理由时执行，接收方不猜父序。清单为空时报告无需维护，不造空提交、不切换源分支找替代提交。

挑选完成后，按已有用户授权把清单和动作发到第二工作区原线程；目标版本和文件复制不增加执行权限。维护会创建第二工作区的cherry-pick提交，但不授权其开发、推送或发布。

## 第二工作区：执行清单

执行前检查实际分支、`git status --porcelain`、当前未合并条目及Git操作状态，确认没有merge/rebase/cherry-pick在途。未提交身份/精简配置也属于脏现场：回报主工作区安排保存或维护提交，不自动stash、reset、restore、clean或临时提交用户文件。Editor必须处于本轮可维护状态；Play/测试/编译忙碌时按现有SOP拒绝，不擅自停止。

主工作区交接清单后若main ref移动，重新核对可达性和选定版本；若第二工作区HEAD变化，重新确认已包含/等效提交和清单。记录本轮起点与已经成功的提交，使用明确commit参数按给定顺序执行，例如：

```text
git -C <第二工作区实际目录> cherry-pick -x <commit1> <commit2>
```

不要用范围隐式扩展提交，不改为merge/pull或复制DLL。结果为空、依赖缺失或发生冲突时停止。只有确认cherry-pick是本轮自己启动且仍在途，才执行：

```text
git -C <第二工作区实际目录> cherry-pick --abort
```

回报冲突路径、实际最后版本和中止结果；第二工作区不得编辑冲突源码、表、协议、测试或提案，不运行`--continue`修代码，不擅自`--skip`或改清单。中止失败保留现场并回报，不强制清理。

## 维护后核对与回报

- 核对源commit与目标新commit的实际结果、工作区状态；Git身份仅用于正常版本操作，不做hash校验或生成证据包。
- 固定产品身份仍为 `GDK / GameDevelopmentKit-NetTest`；只调整ProductName需要时走既有安全Editor入口，不用旧完整ProjectSettings覆盖新版其他设置。
- 主工作区核对 [短AGENTS模板](references/second-workspace-agents.md) 和其中允许的6项项目技能。若提交带回完整入口或开发技能，由主工作区明确安排配置维护/文件删除，第二工作区不自主改规范。删除前核对绝对路径、链接和用户修改，其他目录/账户共享技能不动。
- 有代码/包变更时按指定范围核对编译，纯文档不重跑UTF/联调；未执行写NotRun。确认收件、开始、终态分开，在原线程回报源/目标版本、成功清单、跳过理由、失败及原日志位置。

这是版本维护技能；源码修复和新增功能始终交给主工作区。原始日志留原位，长期工作区、Editor、终端及用户数据按本轮授权保留。

Git语义见 [git-cherry-pick 官方文档](https://git-scm.com/docs/git-cherry-pick)；共同运行/通信规则以固定主工作区的 [协作规程](D:/MyWork/CarzyCooker/Docs/AI/multiplayer-peer-workflow.md) 为准，不从第二工作区旧基线的文档恢复开发职责。
