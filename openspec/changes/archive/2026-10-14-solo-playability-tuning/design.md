# Design
## 2026-10-14 用户试玩豁免

用户对“真实新玩家操作记录”的安排答复“忽略这项”。本轮跳过新玩家理解、求助点与再次经营意愿观察，标记 Skipped（用户豁免）；实际效果仍未验证。UTF 和普通 Launcher 操作验收继续，不把脚本当人类理解证据。试玩调校仅针对可复现的实际操作问题；没有依据时不改参数。此决策覆盖下文原规划中要求无玩家仍待完成的条款，历史记录保留。

## Context

UTF 能证明菜品/供应/结算合同，无法证明第一次见到界面的玩家理解规则。本案依赖可读 UI、菜谱和三关终点，安排 Editor 普通入口试玩；手机与独立 Player 留最后。 动机见 [proposal.md](proposal.md)。本案工程复玩已完成；没有数值问题依据，保持参数，真实新玩家观察按用户豁免。

## Goals / Non-Goals

目标模式：Solo 试玩与调参。不新增埋点平台、统计门槛、经济系统、新菜或 UI 结构；不以两三人反馈宣称普遍留存结论。

依赖：[kitchen-recipe-guide](../2026-10-14-kitchen-recipe-guide/proposal.md)、[solo-first-chapter](../2026-10-14-solo-first-chapter/proposal.md)。层次只表达从局部到跨模块的深度；仅上述实际合同构成依赖。

## Decisions

### 1. 先观察再调参数

由用户安排未参与实现的试玩者，目标是独立完成首单、补货和一轮三关；记录需要口头帮助的位置、误操作、无事可做的等待与再次经营意愿。没有试玩者则该任务保持未完成，不联系外部人员或用脚本代替。

### 2. 一轮只改有依据的小集合

只调现有来客、耗时、供货量、星级阈值与文案；保持两菜菜单、实物守恒、零星继续、暂停/跨关原合同。以体验问题关联少量源字段，正常 Luban Check/生成。若问题需要新机制或结构，回对应 change 修订，不暗中扩大本案。

### 3. 用够用证据作判断

使用简短原话/现象记录与代表性复玩，不加埋点 SDK、统计平台或精确改进百分比。未变行为沿用已有证据，参数影响的可完成性跑相关小集；实际复玩未改善就记下来，不预先承诺成功。

### 状态归属与交接路径

本案沿用餐厅/Level 的既有配置读取和 UI 文案所有者，不增加运行时实体、消息或指标状态。配置交给既有导表流程，试玩观察只写本案 execution-notes。

## Risks / Trade-offs

无依据调参可能制造无法完成的订单或掩盖问题。先记录实际卡点，再选择少量参数；配置检查与一次对应场景复玩足够，规则未变结果复用。 → 具体覆盖与接受的 NotRun 见 [execution-notes.md](execution-notes.md)。不引入额外测试平台、全面压测或理论完备性门槛。

## Migration Plan

实施前以最新主规格、依赖 change 和源码核对差异；保持本案可独立审阅。涉及配置/协议/CodeBind 时修改源并正常生成，不手改生成物。需要格式适配的改动先验证旧有效输入与失败保留，再开放入口。若失败则保留原用户文件和原失败结果，只撤回本案未发布改动，不重置其他人的工作；不自动提交、归档或同步第二工作区。

## Sources

- [Docs/Product/design.md](../../../../Docs/Product/design.md)
- [Docs/Product/backlog.md](../../../../Docs/Product/backlog.md)
- [Design/Excel/ET/Datas/Product](../../../../Design/Excel/ET/Datas/Product)
- [Unity/Assets/Tests/Editor/ET/SoloLevelTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SoloLevelTests.cs)
- [Unity/Assets/Tests/Editor/ET/SoloSupplyTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SoloSupplyTests.cs)
