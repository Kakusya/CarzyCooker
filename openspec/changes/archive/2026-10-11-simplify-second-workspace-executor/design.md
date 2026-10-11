# Design

## Context

动机见proposal；实际第二工作区当前在D:/MyWork/CarzyCooker-NetTest、固定分支multiplayer-baseline，AGENTS366行、项目技能21项。main实际e71a6bac，第二工作区已有20582007；此前开发提交仍位于feature，不属于main来源。第二工作区有本轮已知AGENTS/ProductName未提交差异。

## Goals / Non-Goals

**Goals:** 纯执行职责、物理删除开发技能、基于现有Git/Orca的必要提交维护，现场及产品身份保留。

**Non-Goals:** 第二工作区开发、自动合并main、修源码冲突、新编排服务、移动目录、调整业务树或扩大玩法/构建验收。

## Decisions

### 主工作区选择，第二工作区执行

主工作区在固定主目录检查本地main提交、依赖及必要性，准备runId/实际路径/明确有序commit清单/观察范围；第二工作区检查自身分支、现场和Editor归属后执行并原线程回报。命令维护不代表开发。交接路径为：主工作区代理→现有Orca消息→第二工作区代理→Git或Unity CLI→原线程结果；没有新增Fiber、SceneType、Mailbox或业务状态。

### 一个短技能和一个执行入口模板

`.agents/skills/second-workspace-sync/SKILL.md`区分主工作区选择与第二工作区执行；`references/second-workspace-agents.md`保存短AGENTS模板及允许的项目技能清单。主工作区维护配置模板，第二工作区不自行编辑规范、修源码或产生开发提交。主工作区安装此技能，第二工作区安装同一内容；其余账户共享技能保留，不删除共用来源。

### 物理精简与Git维护前置

第二工作区保留unity-cli、unity-error-extraction、port-management、auto-testing-sop、build-acceptance-sop，并安装维护技能。删除12个OpenSpec目录及carzycooker-requirements/ui/ugfentity/luban，共16个；删除前用实际绝对路径、重解析点及Git改动核对，只操作本端。完整源码、表、工具、Book和references仍供既有命令使用。

cherry-pick依Git官方干净现场要求执行；配置未提交或用户改动先回报，由主工作区安排保存/维护提交，不自动stash或reset。候选不含merge commit，除非明确父序获准；检查main可达性、已包含/补丁等效与依赖，不偷换到HEAD/origin/main或全量merge。只中止本轮自己启动的冲突序列，不由第二工作区修冲突源码。源/目标提交身份用于Git操作和简短记录，不作hash校验。

更新后主工作区核对ProductName与执行入口/技能清单，必要配置重应用只按明确维护动作进行，不对整个ProjectSettings文件覆盖以抹掉新版本其他设置。实际入口维护模板保留在主工作区，未来提交覆盖入口时有可审查来源。

## Risks / Trade-offs

- [删除误伤用户文件] → 明确16个本端目录、验证绝对路径无链接且无用户改动，源码和共享技能保留。
- [cherry-pick覆盖身份或遗漏前置] → 主工作区明确挑选与依赖，干净现场前置，冲突中止并回报，不让第二工作区开发。
- [现有会话仍缓存旧技能] → 实际文件删除与后续会话发现分别说明，不以删文件证明当前会话上下文立即消失。

## Migration Plan

先写并校验技能/模板，再由主工作区重写第二入口及删除本端16目录，同步规程与主规格；用一次临时Git仓库验证顺序、重复识别与冲突中止，检查实际技能及引用。无需Play/UTF；当前没有需要应用的main候选，不重放feature业务提交。回退精简由主工作区从保留的Git版本恢复明确文件，不清理用户现场或删除长期目录。
