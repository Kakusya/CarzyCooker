# CarzyCooker 当前进度

人读的交付进度入口，不是业务状态副本或产品验收证明。冲突时以代码/表、OpenSpec 与用户最新意图为准；改变实际能力时同步更新本页。

| 范围 | 当前事实 | 入口 |
| --- | --- | --- |
| 框架底座 | 当前 main 分支的 ET 底座保留 GF + ET 双端、HybridCLR、UniTask、Luban 与 ET 示例；GameHot/GF Network 已移除 | [框架总览](Docs/Framework/README.md)、[模块索引](Docs/AI/module-index.md) |
| 协作体系 | Codex 入口、十二项官方 OpenSpec 技能、四项项目技能、官方 unity-cli 与四项 SOP 技能，合计二十一项；十六份中文主规格已建立，十四项底座契约已细化 | [AGENTS](AGENTS.md)、[工作流](Docs/AI/workflow.md)、[规格索引](openspec/README.md)、[扫描记录](Docs/AI/spec-scan.md) |
| 做饭玩法 | 房间、厨房、订单、顾客、伙伴、供应和产品存档尚未实施 | [未来产品设计](Docs/Product/README.md) |
| 验证 | 初始迁移与本轮规格细化采用静态检查；CLI 接入的 Editor/包验证见专页；自动测试迁移包含工具自测及测试发现，产品构建、玩法、实际联网仍为 NotRun | [Unity CLI](Book/UnityCLI.md)、[测试/SOP迁移](Docs/AI/testing-sop-migration.md)、[规格验证](openspec/changes/archive/2026-10-09-deepen-foundation-specs/verification.md) |
| 自动化配套 | Unity CLI/Pipeline、Tools/AutoTesting、相关技能及启动/资源/测试/打包/错误/端口/UI/归档 SOP 已迁入；专用双包编排、PortRegistry、UI 结构 runner 等仍未接入 | [已迁入配套](Docs/AI/skill-roadmap.md)、[剩余缺口](Docs/AI/reference-alignment.md) |

不会因设计写完、候选表存在、任务勾选或格式通过，把未来玩法列为已实现。后续玩法必须有实际代码/配置和对应验证结果后再更新。
