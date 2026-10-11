# CarzyCooker 当前进度

人读的交付进度入口，不是业务状态副本或产品验收证明。冲突时以代码/表、OpenSpec 与用户最新意图为准；改变实际能力时同步更新本页。

| 范围 | 当前事实 | 入口 |
| --- | --- | --- |
| 框架底座 | 当前 main 分支的 ET 底座保留 GF + ET 双端、HybridCLR、UniTask、Luban 与 ET 示例；GameHot/GF Network 已移除 | [框架总览](Docs/Framework/README.md)、[模块索引](Docs/AI/module-index.md) |
| 所属异步生命周期 | 已纳入 main 的 IAsyncOwner/RunAsync/Apply、公共 UI/Entity 与 ET owner、资源轮次、延时/请求等待和预拆卸取消，保持源提交的有限迁移范围。源执行记录包含27个新增 UTF、真实 AssetBundle 退役/再加载及原计时用例的主工作区结果；本端本轮 Unity 编译与 UTF 未执行 | [源执行记录](openspec/changes/archive/2026-10-11-manage-owned-async-lifecycle/execution-notes.md)、[生命周期用法](references/framework-lifecycle.md) |
| 底座入口与导表 | 已纳入 main 的同步泛型 UIEntity 独立路由、Luban 失败传播及 Editor 导表失败阻断。主工作区的隔离验证与编译结果见源执行记录；本端本轮编译与行为验证尚未执行 | [源执行记录](openspec/changes/archive/2026-10-11-fix-foundation-entry-and-export-errors/execution-notes.md)、[Entity 用法](Book/Entity开发.md)、[导表用法](Book/Luban配置.md) |
| 协作体系 | 本端保留6项项目技能，独立业务开发仍由主工作区承担；版本维护完整采用 main 已有代码，代码冲突优先 main，文档冲突自主合并。当前二十份主规格保留 | [AGENTS](AGENTS.md)、[同步技能](.agents/skills/second-workspace-sync/SKILL.md)、[规格索引](openspec/README.md) |
| 做饭玩法 | 房间、厨房、订单、顾客、伙伴、供应和产品存档尚未实施 | [未来产品设计](Docs/Product/README.md) |
| 联机启动基础 | 显式 legacy/host/client 配置、工程范围请求、普通 Launcher 兼容与本轮 owner 收尾已实施；产品未接入时 Host/Client 拒绝，不启动 Demo | [执行记录](openspec/changes/multiplayer-instance-bootstrap/execution-notes.md)、[实例规格](openspec/specs/runtime-instances/spec.md) |
| 本地请求退出 | 原 ProcessInnerSender 的幂等 Stop/Destroy、等待取消、旧计时/答复隔离已实施；真实消息 UTF 9/9 Passed，Unity/DotNet.Core 编译通过，不证明产品联机 | [执行记录](openspec/changes/local-message-request-shutdown/execution-notes.md)、[网络规格](openspec/specs/network/spec.md) |
| 产品会话原型 | Unity loopback Host/Client、独立产品树、Match/Participant、共用有序裁决、本地消息及 UDP-KCP 已实现；单端和真实 KCP 小集已有结果，双 Editor 角色反转与最终收口尚待完成，不含厨房/存档/身份恢复 | [执行记录](openspec/changes/product-session-bootstrap/execution-notes.md)、[设计](openspec/changes/product-session-bootstrap/design.md) |
| 稳定第二端 | 长期 CarzyCooker-NetTest / multiplayer-baseline 已建立，保留独立缓存/日志与固定产品名。本端代码已包含联机启动、本机产品会话和 Editor 分支标识，本轮继续纳入底座修复与所属异步生命周期；本轮双 Editor 产品联调未执行 | [共用规程](Docs/AI/multiplayer-peer-workflow.md)、[执行记录](openspec/changes/multiplayer-peer-workspace/execution-notes.md) |
| 验证 | 启动、本地消息、产品 KCP 和 Editor 的源/历史结果各见对应执行记录，不代表本端本轮重跑。本端本轮 Unity 编译、UTF、产品构建及联调均 NotRun；厨房玩法、物理两机 LAN 仍 NotRun | [Unity CLI](Book/UnityCLI.md)、[测试/SOP迁移](Docs/AI/testing-sop-migration.md)、[规格验证](openspec/changes/archive/2026-10-09-deepen-foundation-specs/verification.md) |
| 自动化配套 | Unity CLI/Pipeline、Tools/AutoTesting、相关技能及启动/资源/测试/打包/错误/端口/UI/归档 SOP 已迁入；专用双包编排、PortRegistry、UI 结构 runner 等仍未接入 | [已迁入配套](Docs/AI/skill-roadmap.md)、[剩余缺口](Docs/AI/reference-alignment.md) |

不会因设计写完、候选表存在、任务勾选或格式通过，把未来玩法列为已实现。后续玩法必须有实际代码/配置和对应验证结果后再更新。
