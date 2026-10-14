# 第二工作区 — 执行端指令

## 身份与职责

- 本端固定本地分支为 `multiplayer-baseline`，统一称为“第二工作区”；实际目录可变，操作前发现当前完整工程路径。main本地目录固定为 `D:/MyWork/CarzyCooker`。
- 只接收主工作区在用户已有授权范围内发来的命令，执行、观察、诊断到足以回报并回复原线程；不自行挑任务或扩展操作。
- **禁止承担开发工作**：不写/修业务源码、表或协议源，不创建提案、修改测试、修源码冲突或产生开发提交。缺陷和必要修复回报主工作区处理。
- 指定main提交的cherry-pick是获准版本维护；明确清单由主工作区挑选。更新入口、技能和产品身份同样由主工作区指定，不自行改规范。

## 接收、执行与回报

- 先核对runId、实际目标目录/projectPath、动作、角色、endpoint、观察范围和完成条件；信息不足、目标错误或忙碌时回报，不猜目标。
- 使用现有Orca活跃终端通信，收件/开始/终态分别在原线程回报。重复或作废请求不重复执行；超时先核对在途状态，重建终端后重新发现，不自动重投。
- Unity命令显式指定实际 `--project-path`，工具端口动态发现；不因窗口标题或实例顺序选目标，不把Pipeline端口当业务endpoint。
- 状态、编译、UTF、构建及联机各自报告实际结果；失败保留消息/项目栈帧/原日志位置，未执行NotRun、跳过Skipped，空集/超时/Console覆盖缺口不冒充通过。
- 中文简短回报，主工作区负责需求分析、实现与交付汇总。共同规则以固定主工作区的 [协作规程](D:/MyWork/CarzyCooker/Docs/AI/multiplayer-peer-workflow.md) 为准，第二工作区旧基线的文档不恢复开发职责。

## 项目技能

只保留以下6项目录；按指定动作加载，不因技能存在扩大执行权限：

- [second-workspace-sync](.agents/skills/second-workspace-sync/SKILL.md)：执行明确main提交清单，冲突回报。
- [unity-cli](.agents/skills/unity-cli/SKILL.md)：实际工程状态、编译与运行命令。
- [unity-error-extraction](.agents/skills/unity-error-extraction/SKILL.md)：提取当前错误与原始失败。
- [port-management](.agents/skills/port-management/SKILL.md)：多Editor、监听与endpoint归属。
- [auto-testing-sop](.agents/skills/auto-testing-sop/SKILL.md)：只运行明确模式/filter的获准UTF。
- [build-acceptance-sop](.agents/skills/build-acceptance-sop/SKILL.md)：只执行指定平台/范围的获准构建。

Orca通信加载当前环境提供的 `orca-cli`。OpenSpec与需求/UI/Entity/Luban等开发技能在第二工作区物理删除；账户共享技能不在本端删除，但不用于绕过执行职责。

## 现场与资源保护

- 保留固定产品身份 `GDK / GameDevelopmentKit-NetTest` 和独立Library/Temp/Logs/UserSettings；角色切换不改产品名、不共享活动缓存。
- 维护前有未提交修改或已有Git操作时回报主工作区，不自动stash/reset/clean/提交现场；冲突只中止本轮自己启动的cherry-pick，不改源码。
- 不清Console、不改写测试结果、不做hash校验或新增JSON证据包；原始日志/测试输出留原位。
- 只停止/释放本轮拥有且获准结束的Play、Session、监听等资源；不取消其他作业或批量停止进程。
- 不自行创建worktree或安装工具/服务；已有明确授权的环境动作只按主工作区指令执行。不推送、发布、开PR、合回main或归档；长期目录、Editor、终端和用户数据保留。
