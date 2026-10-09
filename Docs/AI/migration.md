# Codex / OpenSpec 迁移记录

日期：2026-10-09。范围：协作入口、文档、技能和静态规格。没有改动玩法或底座代码。

用户随后要求切换到 ET 分支，已建立本地 `ET` 跟踪 `origin/ET`；已有修改/删除保留。main 分支的历史扫描与归档不作为 ET 当前能力声明；入口、项目技能与主规格已按 ET 重新核对。

随后按用户指令，将原 ET 分支改名为 `main`，替换旧 main；本地和 origin 只保留 main，跟踪 `origin/main`。origin 的默认分支仍为 main；新增远端 `upstream` 指向 `https://github.com/XuToWei/GameDevelopmentKit`。分支引用更新已推送，未提交的协作迁移和 Unity CLI 修改保留在工作区。这里涉及旧 main、ET 的扫描与归档描述保留历史含义。

后续入口审阅中，用户要求尽量保留参考的结构与适用原文，确认补齐根文件名与 `references/`。本记录保留原迁移验收历史；当前路由及后续修订以 [AGENTS](../../AGENTS.md)、[参考对齐记录](reference-alignment.md) 为准，不把这里的“本次”限制当成后续任务永久禁令。

## 实际交付

- AGENTS.md 统一路由；Book 保留框架用法，Docs 分开 AI、Framework、Development 与 Product。
- OpenSpec 全局 CLI 固定 1.14.1；Codex 生成七项官方技能，另有需求拷问、Luban、UI、UGFEntity 四项项目技能。
- 技能融入后全仓文本扫描，十四项源码支持底座基线加一项协作资料规格，合计十五项主规格。
- 原始资料 `Book/业务逻辑.txt.txt`、`Book/禁止事项.txt.txt` 保留，适用产品原文与完整候选表迁出；正式路由没有旧工程路径。暂停命令、断线手持物和传输选择仍待裁决。
- 三份框架手册修正 UGFEntity 输出路径/类型名与 ET-Admin 表项，不改变生成器或运行时。

## 旧知识迁出

| 旧知识主题 | 当前去处 |
|---|---|
| 业务分层、复用、错误责任、字段注释 | [实现约定](../../references/business-code-conventions.md) |
| GF/ET 生命周期与异步 owner | [生命周期](../../references/framework-lifecycle.md)、UI/Entity/热更主规格 |
| Luban/Proto/本地化与生成边界 | [生成约定](../../references/data-generation.md)、数据/协议/本地化主规格 |
| 数据模型、同步/HTTP、响应式投影 | [数据与投影](../../references/reactive-and-network.md)、服务端/网络/响应式主规格 |
| 架构与程序集、跨层检查、模块定位 | [模块索引](module-index.md)、[框架入口](../Framework/README.md) |

旧文件已被用户删除，只从 Git HEAD 只读读取知识，没有恢复。旧 bootstrap task 是规范建立任务，不转换成做饭实施授权；旧 journal 没有可迁出的业务会话内容。

## 接入与全局核对

剩余十二项 Trellis 项目技能的文件、三项代理、三项 hooks 和 hooks.json 已删除；Codex config 保留通用 AGENTS 入口，移除旧工作流默认和 depth 覆盖。CLAUDE.md 仅保留文档指针，不再维护独立接入。用户已有旧工作流/Claude 文件删除保持。

PATH 没有 trellis；npm 全局列表未安装 Trellis CLI/core，当前 Python 环境未发现 trellis/mindfold-trellis，当前用户与账号技能根未发现全局 Trellis 技能。未找到可卸载的全局安装，因此没有执行无目标卸载或删全局历史记录。新安装是已授权 OpenSpec；profile/workflows 全局设置范围见 [工作流](workflow.md)。

自动审批拒绝清理残留空目录，工具仅返回 `blocked by policy`，没有具体理由。目录保留，但不存在技能或 hooks 文件，不构成活跃接入。

## 验证范围

严格 OpenSpec 格式校验、正式引用、技能 YAML/名称/边界、产品候选表保留、六条关键实现链路与变更范围采用静态检查。技能创建器的 Python quick_validate 因缺 PyYAML 不可用；不安装额外依赖，改用已安装 OpenSpec 的 YAML parser 核对 frontmatter。

| 验收 | 实际结果 |
|---|---|
| CLI 与配置 | openspec --version = 1.14.1；doctor root ok；项目 YAML 解析通过 |
| 格式 | 十五项主规格严格验证通过，迁移增量严格验证通过 |
| 引用 | 41 份维护中 Markdown 的 280 个本地引用通过（归档前统计） |
| 技能 | 七个官方与四个项目技能 YAML/名称/描述检查通过，均有可发现 SKILL.md |
| 原文 | 候选菜品 87 行和准备状态 60 行逐行保留；两份原件仍在原位置 |
| 源码抽查 | 启动、UI、Entity、Luban、网络、热更六链路静态核对通过 |
| 变更边界 | git diff --check 通过；无 Unity/Design/工具运行时代码修改；已有删除保留 |
| 协作增量 | 四项要求映射到入口/技能/产品文档，四个场景采用文档静态核对；无运行时代码模式审查需求 |

产品构建、工具构建、导表执行、Unity 编译/Editor、AOT、玩法、真实联网和存档故障测试均 **NotRun**，不把静态扫描或 CLI 通过解释为运行成功。没有 SHA 校验、额外 JSON 证据/状态副本、服务启动、端口修改、提交或推送。

本变更已同步四项要求，再以 --skip-specs 归档，避免重复应用增量。归档位置：[2026-10-09-ai-collaboration-migration](../../openspec/changes/archive/2026-10-09-ai-collaboration-migration/tasks.md)。归档后十五项主规格及归档任务校验均通过，无活跃 change。已知源码限制留在 [已知问题](../../KnownIssues.md)，不借迁移代修产品。

## ET 分支复核与参考对齐

安全切换后保留已有受版本管理的 169 项修改/删除和未跟踪文件；合并冲突按本地已有内容与删除状态解决，修改保持未暂存。随后仅调整文档、项目技能和主规格，未恢复 ET 分支已移除的 GameHot/GF Network，也未改运行时代码。

BladeGame 的 29 个二级/三级章节主题均保留；编辑器章节名称按本项目能力改写，后续已授权接入 CLI/Pipeline。通用原文尽量保留，业务、目录、程序集、技能路由和工具能力按当前 ET 源码适配；配套缺口单列，不自动安装。

ET 文本扫描范围见模块索引。十五项主规格再次通过严格验证；11 项技能的 frontmatter、名称、描述和官方生成版本检查通过；52 份维护文档的 409 个本地引用及主规格源码路径有效。Git whitespace 检查通过，无暂存内容或未解决冲突，Unity/Design/Tools/Share/DotNet 没有运行时文件修改。产品构建、导表、Editor、玩法、联网等仍为 NotRun。

## 后续已授权的编辑器接入替换

用户要求完全清除旧接入，使用 Unity CLI 和项目对应包，并逐步批准技能。已删除八份旧 Editor 命令及其 meta、目录 meta、旧项目设置；移除包/lock 条目、Game.Editor 引用和六个平台专属宏。ET 服务端桥与 HybridCLR method bridge 属原底座，不属于旧编辑器接入。

本机已有有效 Editor 6000.3.18f1，实际路径 D:/MyUnityEditor/6000.3.18f1/Editor/Unity.exe；用户指定 D:/UnityEditor 用于缺版本时下载。复用现有 CLI 1.0.0-beta.11，项目固定 Pipeline 0.8.0-exp.1。包解析、项目连接与编译状态已验证，真实探测错误与原日志见 [Unity CLI](../../Book/UnityCLI.md)。这一轮的实际 Editor 检查与初始迁移的 NotRun 分开记录。

新技能尚未安装，首项及后续候选见 [逐项审批清单](skill-roadmap.md)。本次变更在 openspec/changes/unity-cli-editor-setup 保留审阅；不运行 Play/玩法测试或产品构建，不提交或推送。

## 后续补齐 OpenSpec 全部官方技能

2026-10-09，用户明确要求补齐全部技能。初次的 core 六项加 verify 保留为历史记录；当前 custom/workflows 已选择全部十二项。官方 CLI 1.14.1 在当前仓库刷新 Codex，新增 new-change、continue-change、ff-change、bulk-archive-change、onboard；apply/update 的引导文字由生成器同步到新可用的 continue/new 入口，四项项目技能未改。AGENTS 与工作流、迁移清单、参考对齐和模块索引同步当前状态。

profile/workflows 是全局配置，会影响后续其他项目初始化/更新的选择；本次未刷新其他工程。技能安装不运行 Unity、产品构建或玩法测试，也不接入仍待逐项审批的 Unity/其他技能和配套工具。

## 后续已授权的自动测试与 SOP 迁移

用户核实缺口后明确要求迁移。本轮接入官方 unity-cli、Tools/AutoTesting 及 auto-testing-sop/build-acceptance-sop/unity-error-extraction/port-management；补齐启动、资源、测试、打包、错误、C#、归档、端口和动态 UI 正文，完善原四项项目技能。当前 21 项技能；历史 11/16 项的检查结果保持原记录，不改写旧阶段。

具体文件、原样复制与项目适配、独立工具剩余缺口、验证范围见 [SOP 迁移](testing-sop-migration.md)。AGENTS 本轮精确修改和原因见 [逐字审阅](testing-sop-agents-review.md)。未提交或推送；产品玩法和构建不因迁移自动执行。
