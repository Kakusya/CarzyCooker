# CarzyCooker 当前进度

人读的交付进度入口，不是业务状态副本或产品验收证明。冲突时以代码/表、OpenSpec 与用户最新意图为准；改变实际能力时同步更新本页。

| 范围 | 当前事实 | 入口 |
| --- | --- | --- |
| 框架底座 | 当前 main 分支的 ET 底座保留 GF + ET 双端、HybridCLR、UniTask、Luban 与 ET 示例；GameHot/GF Network 已移除 | [框架总览](Docs/Framework/README.md)、[模块索引](Docs/AI/module-index.md) |
| 底座入口与导表 | 同步泛型 UIEntity 统一独立路由；Luban 失败传播为 CLI 非零并阻断复制/ID/本地化，Editor 导表失败停止后续刷新。隔离成功/失败验证和工具/Unity 编译通过；真实工作表导出、UIEntity 显示未运行 | [执行记录](openspec/changes/archive/2026-10-11-fix-foundation-entry-and-export-errors/execution-notes.md)、[Entity 用法](Book/Entity开发.md)、[导表用法](Book/Luban配置.md) |
| 协作体系 | 主工作区保留十二项OpenSpec、五项项目技能、官方unity-cli和四项SOP，共二十二项；第二工作区按执行职责保留6项项目技能，开发技能物理删除；十九份中文主规格已建立，十四项底座契约已细化 | [AGENTS](AGENTS.md)、[工作流](Docs/AI/workflow.md)、[规格索引](openspec/README.md)、[扫描记录](Docs/AI/spec-scan.md) |
| 做饭玩法 | 房间、厨房、订单、顾客、伙伴、供应和产品存档尚未实施 | [未来产品设计](Docs/Product/README.md) |
| 联机启动基础 | 显式 legacy/host/client 配置、工程范围请求、普通 Launcher 兼容与本轮 owner 收尾已实施；产品未接入时 Host/Client 拒绝，不启动 Demo | [执行记录](openspec/changes/archive/2026-10-11-multiplayer-instance-bootstrap/execution-notes.md)、[实例规格](openspec/specs/runtime-instances/spec.md) |
| 本地请求退出 | 原 ProcessInnerSender 的幂等 Stop/Destroy、等待取消、旧计时/答复隔离已实施；真实消息 UTF 9/9 Passed，Unity/DotNet.Core 编译通过，不证明产品联机 | [执行记录](openspec/changes/archive/2026-10-11-local-message-request-shutdown/execution-notes.md)、[网络规格](openspec/specs/network/spec.md) |
| 产品会话原型 | Unity loopback Host/Client、独立产品树、Match/Participant、共用有序裁决、本地消息及 UDP-KCP 已实现；18个产品UTF各有Passed证据，双Editor两种角色排列的名单/Probe/心跳、断线保留/Leave移除及重复Stop释放已核实，不含厨房/存档/身份恢复 | [执行记录](openspec/changes/archive/2026-10-11-product-session-bootstrap/execution-notes.md)、[设计](openspec/changes/archive/2026-10-11-product-session-bootstrap/design.md) |
| 第二工作区 | 固定本地分支multiplayer-baseline、目录可变，当前CarzyCooker-NetTest；独立Editor/缓存/日志、产品身份与两种联调已核实，代码基线20582007保留。AGENTS37行、6项项目技能，禁止开发；主工作区挑main必要提交后交接cherry-pick，第二工作区本地精简配置尚未提交。F盲收件回读通过后两个Editor正常退出，目录/缓存/日志/代理终端保留 | [共用规程](Docs/AI/multiplayer-peer-workflow.md)、[环境记录](openspec/changes/archive/2026-10-11-multiplayer-peer-workspace/execution-notes.md)、[精简记录](openspec/changes/archive/2026-10-11-simplify-second-workspace-executor/execution-notes.md) |
| 验证 | 初始迁移与规格细化采用静态检查；后续启动、本地消息、产品 KCP 和 Editor 结果各见本项记录；产品构建、厨房玩法、物理两机 LAN 仍 NotRun | [Unity CLI](Book/UnityCLI.md)、[测试/SOP迁移](Docs/AI/testing-sop-migration.md)、[规格验证](openspec/changes/archive/2026-10-09-deepen-foundation-specs/verification.md) |
| 自动化配套 | Unity CLI/Pipeline、Tools/AutoTesting、相关技能及启动/资源/测试/打包/错误/端口/UI/归档 SOP 已迁入；专用双包编排、PortRegistry、UI 结构 runner 等仍未接入 | [已迁入配套](Docs/AI/skill-roadmap.md)、[剩余缺口](Docs/AI/reference-alignment.md) |

不会因设计写完、候选表存在、任务勾选或格式通过，把未来玩法列为已实现。后续玩法必须有实际代码/配置和对应验证结果后再更新。
