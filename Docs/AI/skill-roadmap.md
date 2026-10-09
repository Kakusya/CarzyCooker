# 技能逐项审批清单

用户已授权本轮移除旧编辑器接入、加入 Unity CLI/Pipeline 和核实 Editor。新增及进一步改造的 skill 依本清单逐项批准；目前没有安装下面的新技能。现有技能本轮只修正过时能力与路由文字。

## 第一项：接入官方 unity-cli（建议先批准）

- **用途**：使用 Unity 官方现成说明，指导 Codex 调用命令行工具；Unity CLI 是工具，unity-cli skill 是使用指导，二者分开。
- **必要性**：CLI/包已接入，官方技能尚未进入项目 Codex 发现目录。复用官方原件，避免自写版本重复维护或落后于 CLI。
- **依赖**：已有 CLI 1.0.0-beta.11、Pipeline 0.8.0-exp.1、Editor 6000.3.18f1；不再增加软件、MCP、端口工具或自定义命令。
- **官方来源与命令**：Unity CLI 内嵌与自身版本匹配的技能；官方仓库见 [Unity-Technologies/skills](https://github.com/Unity-Technologies/skills/tree/main/skills/unity-cli)。在仓库根执行 `unity skill install codex --local`。已执行带 `--dry-run` 的预览，确认目标为本项目根目录，尚未安装。
- **文件清单**：由官方安装器写入 `.agents/skills/unity-cli/`，包括 SKILL.md、CHANGELOG.md、SECURITY.md 和自带 references；不新增自写 command-usage.md。接入时在 `AGENTS.md`、`Docs/AI/module-index.md`、`Book/UnityCLI.md` 加正式路由，本审批页更新状态。
- **改造原则**：官方技能原文由上游维护；本项目路径、版本、授权、MCP/测试/打包边界留在 AGENTS 与手册，用户授权和项目规则优先。不从参考项目复制自写技能，不因官方说明覆盖多项能力就自动启用它们。
- **验收**：官方文件可被 Codex 发现，元数据和内部引用有效；用当前工程验证发现、状态、编译与 Console，真实报告未就绪/失败，遵守项目证据边界。接入不自动扩展为玩法测试。

Pipeline 0.8.0-exp.1 包还附带官方 `unity-pipeline` skill，目前在其 PackageCache 的 `.claude/skills/unity-pipeline/SKILL.md`；这只是上游包装位置，不构成本项目 Claude 接入。若要让 Codex 发现这项技能，接入范围另行列明，不自行改写或默认扩大第一项授权。

## 后续候选：按顺序单独裁决

| 候选 | 用途 / 必要性 | 依赖 | 拟改文件 | 可观察验收 |
|---|---|---|---|---|
| 改造 carzycooker-requirements | 与参考拷问方式进一步对齐，明确逐轮问题、冲突和决策账本，避免为已授权任务重复造 gate | 已有 explore、requirements；只读参考问题组织 | 对应 SKILL.md；必要时新增 references/interview.md；AGENTS 仅同步真实边界 | 简单文档直接做；复杂需求只问影响范围的问题；原样保留待裁决，不自动提案 |
| 改造 carzycooker-luban | 删除旧自动改表能力后，补完整 ET 的“读表 → check → gen → diff”职责和失败出口 | 既有 ExcelExporter 与可用工作簿编辑方式；新增编辑工具另案审批 | 对应 SKILL.md、references/data-generation.md；若获准再加技能 references | Check 不写产物；失败不继续使用生成物；公式/样式不丢失；无隐式工具安装 |
| 改造 carzycooker-ui、carzycooker-ugfentity | 把参考 Prefab/CodeBind/owner 的 SOP 接到本项目 ETUI/Entity 层 | 前项 CLI 技能、已有 UI/Entity 表和生成器；不额外建结构注册工具 | 两项 SKILL.md 和必要 references；相应 Book/模块路由 | 普通 Entity/UIEntity ID 不混；骨架生成职责真实；加载中 Dispose、反复进入退出验收可执行 |
| auto-testing-sop | 在需要产品验证时统一 UTF 测试发现、执行、失败诊断，避免把编译当作行为验收 | 已有 Pipeline 引入的 UTF、CLI 技能；首个测试行为需单独确认 | 新技能 SKILL.md/测试引用；AGENTS 与模块索引；产品测试代码另案批准 | 能发现/执行一个已授权测试，准确报告失败、跳过与新运行；不改失败结果 |

本轮建议只裁决第一项 `unity-cli`。打包验收、端口治理、动态 UI 迁移与专用表编辑工具暂列实际需求出现后的候选，不把整套参考管线一次迁入。
