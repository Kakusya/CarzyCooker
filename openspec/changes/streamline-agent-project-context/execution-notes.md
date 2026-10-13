# 执行说明

## 规划阶段状态与决策

2026-10-13：用户授权“由浅到深写成 change”，并要求真机测试最后。本案目前仅为规划；所有 tasks 未勾选，未实现业务、未执行 UTF、未启动 Unity/Player、未构建或操作第二工作区。第三关保存完成后返回选档、无装修和第四关；橙汁加工锁壶、完成可搬壶分杯、产量/耗时配表，已获用户确认。具体相关合同以本案 specs/design 为准。

## 相称的严谨程度

主要风险是压缩文档时丢掉有效纪律或把历史状态误写成当前能力。用约束逐项对照、正文与引用检查即可；纯文档不运行 UTF。 所选验收按下表覆盖本次改变的行为，已有结果只有在实现/行为未变且明确对应该点时才复用，并填写原执行记录路径。不因提案完成或阶段切换重跑全仓；测试失败、空集、Skipped、缺输出均不算成功。

接受的未验证范围：本轮所有运行行为 NotRun；除最后设备层外，手机/独立 Player/LAN 仍待对应案。没有玩家、设备或已授权运行条件时保留相关任务未完成。若改到共享 owner、文件格式、协议、行为断言或出现实际回归，则补对应局部验证，不机械放大为完整矩阵。

## 规划阶段的任务与验收映射

| 任务 | 功能／交付 | 拟使用的最小验收 | 本轮结果 |
| --- | --- | --- | --- |
| 1.1 | 入口与纪律 | 对照原纪律确认授权/保护/UTF/owner/生成物规则未丢失，检查唯一正文与跳转。 | NotRun（规划阶段） |
| 2.1 | 当前与历史 | 按源码及原执行记录核对当前范围；旧失败、NotRun 与最新结果均可追溯。 | NotRun（规划阶段） |
| 3.1 | 资料交付 | 运行限定路径的链接/文本检查与 openspec validate --strict；提交/归档仅给建议清单。 | NotRun（规划阶段） |

UTF 执行阶段使用现有 Tools/AutoTesting，先发现模式与方法再记录实际过滤范围；涉及产品消息/UI 的候选集按 PlayMode 核对，目录名不能证明模式。原始输出保留原位，只在此引用运行路径及功能对应关系。纯正文任务只检查文本、链接与严格格式，不制造占位 UTF。

## 实施时回填

每项记录“实际方法或真实操作 → 原输出路径 → Passed/Failed/NotRun/Skipped → 未覆盖点”。文档格式验证只证明规划可解析，不证明功能已完成。当前不预填执行日期、通过计数或不存在的测试结果。

## 规划阶段检查

2026-10-13：`openspec validate streamline-agent-project-context --strict --no-interactive` 退出码 0，规划格式通过。当前相关文档检查共 80 份 Markdown，链接缺失 0、编码/尾随空格/原规格场景遗漏 0；新案依赖无环。以上均为静态规划检查，表内功能仍全部 NotRun，任务未勾选。

## 2026-10-13 实施记录

用户授权按三个基础设施 change 实施，顺序为本案、stabilize-product-test-fixtures、product-local-message-boundaries。本案仅修改资料和任务记录，不修改技能、生产代码或主规格合同。原规划阶段 NotRun 与格式检查保留其阶段语义，后续实际结果以下表为准。

| 任务 | 实际交付与核对 | 结果 |
| --- | --- | --- |
| 1.1 | AGENTS 去掉动态进度与重复模块表；模块索引纠正旧 feature 分支和 Match 厨房 owner，统一指向根进度；产品入口与工作流只维护职责路由。对照 HEAD，第二工作区职责、工程取舍、探索门槛、技能加载、UTF、ET 树与消息、协作保护各节全文不变，技能路径集合不变。 | Passed（正文对照） |
| 2.1 | 根进度更新唯一摘要职责，区分已有规划与未实施；核对协议源代码 ProtocolVersion=17、Host/Solo 建树及原检查点和普通双 Editor 最终记录。旧失败与 NotRun 均保留在原执行说明，不改原输出。 | Passed（源码与原记录核对） |
| 3.1 | 检查本案与五份维护正文共 9 份 Markdown、316 个本地引用，缺失引用／编码／尾空白 0；git diff --check 退出 0；openspec validate streamline-agent-project-context --strict --no-interactive 返回 valid、退出 0。提交／归档建议见下文，未执行版本操作。 | Passed（静态检查） |

本案无功能行为变化，不运行 UTF；既有检查点、供应装修和双 Editor 结果按原范围复用，不提升 Player/AOT、Android、物理 LAN 或原成员恢复支持。根进度只维护摘要，原执行说明负责实际功能对应和失败历史。风险仍为误删纪律或误报当前能力，以上述逐节对照和局部引用检查足够。

### 提交与归档分组建议（未执行）

- 当前资料整理建议单独提交：AGENTS、根进度、Docs/AI/module-index、Docs/AI/workflow、Docs/Product/README 及本案 tasks／execution-notes。后续夹具与消息改动另按各案交付，避免把未验收任务混入文档提交。
- 已有单机交付建议一组核对归档：solo-kitchen-clear-and-discard、solo-supply-delivery、solo-interlevel-renovation、solo-success-checkpoint。逐项以其原验收映射和主规格同步情况为门槛，不只依据勾选数。
- 已有合作交付建议一组核对归档：cooperative-restaurant-core、cooperative-front-service、cooperative-cleanup-supply、cooperative-restaurant-loopback。保留原先前置拆分账本及普通双 Editor 结果，不把 loopback 提升为 Player/LAN。
- 当前 baseline 的既有实现与验收已在本地 main 提交保存；原记录中早期“未提交／仅规划／NotRun”按各自阶段解释。新增资料仍未提交，未执行 push、PR、归档或第二工作区同步。
- 排除 Unity/Temp、Library、原始测试输出、截图、用户现场资源、额外状态副本和未改的技能；不以本案授权处理历史用户文件。
