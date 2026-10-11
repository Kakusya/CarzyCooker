# CarzyCooker 当前进度

本页随main来源功能同步；其中玩法运行与历史Passed属于源记录，第二工作区本轮Unity／UTF／双Editor结果另行报告，尚未执行项保持NotRun。

人读的交付进度入口，不是业务状态副本或产品验收证明。冲突时以代码/表、OpenSpec 与用户最新意图为准；改变实际能力时同步更新本页。

| 范围 | 当前事实 | 入口 |
| --- | --- | --- |
| 框架底座 | 当前 main 分支的 ET 底座保留 GF + ET 双端、HybridCLR、UniTask、Luban 与 ET 示例；GameHot/GF Network 已移除 | [框架总览](Docs/Framework/README.md)、[模块索引](Docs/AI/module-index.md) |
| 所属异步生命周期 | IAsyncOwner/RunAsync/Apply、公共 UI/Entity 与 ET owner、资源轮次、延时/请求等待和预拆卸取消已实施；27个新增 UTF 各有 Passed 证据，真实 AssetBundle 创建/读取退役及再加载通过，另补1个原计时用例；保持有限迁移范围 | [执行说明](openspec/changes/archive/2026-10-11-manage-owned-async-lifecycle/execution-notes.md)、[生命周期用法](references/framework-lifecycle.md) |
| 底座入口与导表 | 同步泛型 UIEntity 统一独立路由；Luban 失败传播为 CLI 非零并阻断复制/ID/本地化，Editor 导表失败停止后续刷新。隔离成功/失败验证和工具/Unity 编译通过；真实工作表导出、UIEntity 显示未运行 | [执行记录](openspec/changes/archive/2026-10-11-fix-foundation-entry-and-export-errors/execution-notes.md)、[Entity 用法](Book/Entity开发.md)、[导表用法](Book/Luban配置.md) |
| 协作体系 | 主工作区保留十二项OpenSpec、五项项目技能、官方unity-cli和四项SOP，共二十二项；第二工作区按执行职责保留6项项目技能，开发技能物理删除；十九份中文主规格已建立，十四项底座契约已细化 | [AGENTS](AGENTS.md)、[工作流](Docs/AI/workflow.md)、[规格索引](openspec/README.md)、[扫描记录](Docs/AI/spec-scan.md) |
| 做饭玩法 | 正式餐厅／Kitchen／Level、加工、订单、顾客、伙伴、供应和产品存档尚未实施；临时格子样机的已实施部分见下项 | [未来产品设计](Docs/Product/README.md) |
| 竖屏格子样机 | 普通 Launcher.unity 直接 Play 默认 Solo，实际显示托管 2D Board／HUD；已直接运行两轮，540×960 场景点格和 HUD 拿碗／放交接台通过。此前普通入口仍进示例，旧交付表述过宽，已修正。Match 固定 Tick、四方向寻路／两人停驻／相邻交接／同格落物、共同本地与 KCP 消息、独立投影已实施。UTF 七项各有 Passed 证据：Core 4/4、复用 Wire 3/3；真实 KCP 交接／断线拾物通过，同一 Editor 两个独立 Fiber。双 Editor 新玩法、手机 Player／AOT／LAN／公网 NotRun | [执行说明](openspec/changes/archive/2026-10-11-portrait-grid-kitchen-prototype/execution-notes.md)、[任务](openspec/changes/archive/2026-10-11-portrait-grid-kitchen-prototype/tasks.md) |
| 示例配置与测试 Widget 清理 | 保留公共表结构；UIEntity／Scene／音频／Unit／AI 数据为空。累计清理 38 条普通示例记录，整表删除 DTDemo／OneConfig 及生成物，移除登录页测试 Widget 节点、加载代码、独立资源与脚本。UI 仅六条既有初始化链路注册及地图 HUD，Entity 仅地图一条；四张 StartConfig 源表保留。Luban 五个目标、完整 Unity 编译、普通 Launcher 登录页和 Solo 地图启动通过；旧 Demo 单位／AI 演示不再可用 | [导表用法](Book/Luban配置.md)、[执行说明](openspec/changes/archive/2026-10-11-portrait-grid-kitchen-prototype/execution-notes.md) |
| 联机启动基础 | 显式 legacy/host/client 配置、工程范围请求、普通 Launcher 兼容与本轮 owner 收尾已实施；产品未接入时 Host/Client 拒绝，不启动 Demo | [执行记录](openspec/changes/archive/2026-10-11-multiplayer-instance-bootstrap/execution-notes.md)、[实例规格](openspec/specs/runtime-instances/spec.md) |
| 本地请求退出 | 原 ProcessInnerSender 的幂等 Stop/Destroy、等待取消、旧计时/答复隔离已实施；真实消息 UTF 9/9 Passed，Unity/DotNet.Core 编译通过，不证明产品联机 | [执行记录](openspec/changes/archive/2026-10-11-local-message-request-shutdown/execution-notes.md)、[网络规格](openspec/specs/network/spec.md) |
| 产品会话原型 | Unity loopback Host/Client、独立产品树、Match/Participant、共用有序裁决、本地消息及 UDP-KCP 已实现；18个产品UTF各有Passed证据，双Editor两种角色排列的名单/Probe/心跳、断线保留/Leave移除及重复Stop释放已核实，不含厨房/存档/身份恢复 | [执行记录](openspec/changes/archive/2026-10-11-product-session-bootstrap/execution-notes.md)、[设计](openspec/changes/archive/2026-10-11-product-session-bootstrap/design.md) |
| 第二工作区 | 固定本地分支multiplayer-baseline、目录可变，当前CarzyCooker-NetTest；独立Editor/缓存/日志、产品身份与两种联调已核实，代码基线20582007保留。AGENTS37行、6项项目技能，禁止开发；主工作区挑main必要提交后交接cherry-pick，第二工作区本地精简配置尚未提交。F盲收件回读通过后两个Editor正常退出，目录/缓存/日志/代理终端保留 | [共用规程](Docs/AI/multiplayer-peer-workflow.md)、[环境记录](openspec/changes/archive/2026-10-11-multiplayer-peer-workspace/execution-notes.md)、[精简记录](openspec/changes/archive/2026-10-11-simplify-second-workspace-executor/execution-notes.md) |
| 验证 | 初始迁移与规格细化采用静态检查；后续启动、本地消息、产品 KCP 和格子样机 Editor 结果各见本项记录；完整烹饪、产品构建与物理两机 LAN 仍 NotRun | [Unity CLI](Book/UnityCLI.md)、[测试/SOP迁移](Docs/AI/testing-sop-migration.md)、[规格验证](openspec/changes/archive/2026-10-09-deepen-foundation-specs/verification.md) |
