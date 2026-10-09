## Context

见 proposal 的已授权范围。当前 ET 分支仍声明旧包，Game.Editor 显式引用旧程序集，Editor/AgentBridge 下有八份命令源和 meta，另有 AIBridgeSettings 与六处专属宏。机器实际已有 Editor 6000.3.18f1_5ebeb53e4c07、CLI 1.0.0-beta.11；Unity 官方 registry 当前 Pipeline 为 0.8.0-exp.1。

## Goals / Non-Goals

**Goals:** 清除旧接入的可执行内容和文档协议；固定实际 CLI/包配对，验证正确工程发现、依赖解析和编辑器状态；提供技能改造逐项审批材料。

**Non-Goals:** 做饭业务、打包/玩法测试、端口治理工具、额外表编辑工具、技能自动安装或复制其他项目能力声明。

## Decisions

- 使用现有 CLI 和官方 pipeline install 锁定包；不套用参考仓已经落后的 beta.8/0.6 配对，也不改全局 CLI。
- 删除旧接入专属包、命令、meta、设置和宏；服务器 ActorBridge 与 HybridCLR method bridge 属底座结构，保留。
- 复用已存在且版本核实的 Editor；D:/UnityEditor 是缺版本时用户指定的下载位置，不移动现有安装。
- 由 Editor 自行解析 packages-lock 和新包依赖，避免手填派生依赖。移除旧包时只删对应 lock 条目，其余保留。
- 显式 project-path 匹配本工程，先发现实例和命令，不固定端口；描述文件的 token 不进入文档或日志。
- unity-cli 接入仅作可审阅提案；用户随后明确要求复用官方现成技能，已核实 CLI 的内嵌原件及项目 Pipeline 包附带技能。官方原文由上游维护，项目规则放入口和手册；现有项目技能新增行为另逐项批准。

## Risks / Trade-offs

- 首次导入/编译耗时 → 查看本次 Editor 原日志与进程状态；不反复重启或把未就绪写成成功。
- 存量第三方依赖可能失败 → 区分本次删除/依赖改动与原有错误；缺依赖或许可如实报告，不擅自改底座。
- 新 Pipeline 依赖 test-framework 等 → 官方 registry 与 Editor resolver 确认实际版本，安装不等于执行玩法测试。
- 旧自定义命令删除后不再提供自动改表能力 → 保留 ExcelExporter 与人工工作簿流程；若需要专用工具，单独审批。
