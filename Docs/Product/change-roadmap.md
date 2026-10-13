# 由浅到深的 change 路线

2026-10-13：按用户“意见都提、由浅到深写成 change、真机测试放最后”的授权创建。**本轮新增 14 个完整规划案、58 项未执行任务，复用现有消息与合作计划；仅规划，不是实施或设备操作授权。**

本页是依赖与阅读入口，不复制运行进度台账。最新状态查 `openspec list`、各案 tasks 和 execution-notes；当前能力查[根进度](../../CarzyCooker当前进度.md)，有冲突以源码/表和原运行证据为准。

## 方向与使用方式

保留现有 ET / GF / HybridCLR / Luban 底座，把投入逐步移向“看懂操作、完整经营、有意义的内容选择、合作可持续”。工程整理围绕具体重复和错误展开，不为理论完整性再造框架、消息总线、测试平台或资产安全系统。

下列层次表达从局部整理到跨模块、跨进程的深度，**不是你必须接受的业务优先级，也不是所有案都要串行完成**。真正前置写在各案 design。先在 Editor 验收本次选定功能，最后进入 PC Player / LAN 和 Android 真机；设备层不会倒过来阻挡前面的可独立工作。

现有消息案已经覆盖广泛业务，按其任务组逐段替换和验收，避免与正在改同一模块的功能案交叉写入。资料与夹具整理也只处理实际问题，不成为玩法开工的全仓前置。

## 第一层：资料与验证基础

| Change | 解决的问题 | 可观察交付 |
| --- | --- | --- |
| [streamline-agent-project-context](../../openspec/changes/archive/2026-10-13-streamline-agent-project-context/proposal.md) | Agent 入口过长、动态事实多处过期 | 稳定纪律保留，进度/规格/目标各有唯一正文；可审阅的提交和归档分组建议 |
| [stabilize-product-test-fixtures](../../openspec/changes/archive/2026-10-13-stabilize-product-test-fixtures/proposal.md) | 初始化、投影等待与测试收尾重复出错 | 原行为断言不减，消息投递和业务时间明确，失败诊断可定位 |

两案都不改变产品合同，使用 `skip_specs: true`。提交、push、归档和第二工作区同步仍按相应授权；本轮不执行。没有具体重复问题就不扩大整理范围。

## 第二层：看得清、操作清楚

| Change | 解决的问题 | 可观察交付 |
| --- | --- | --- |
| [restaurant-ui-information-hierarchy](../../openspec/changes/restaurant-ui-information-hierarchy/proposal.md) | 信息密集、标签与动作难辨 | 阶段/手持物/目标/动作/详情分层；状态与拒绝可读，原操作仍能到达 |
| [kitchen-recipe-guide](../../openspec/changes/kitchen-recipe-guide/proposal.md) | 新玩家不知从何下手 | 真实两菜步骤与缺口提示，可跳过/重看首单引导，联机只读菜谱 |

先整理实际 UI 结构，再在保留区域接入内容。沿现有受管 UI/Entity 和本地消息，不创建第二份业务状态。声音、美术方向后续可据实际资源另提，不在本轮凭空引入资产管线。

## 第三层：完整一轮经营

| Change | 解决的问题 | 可观察交付 |
| --- | --- | --- |
| [solo-first-chapter](../../openspec/changes/solo-first-chapter/proposal.md) | 下一关链没有清楚终点 | 三关经营、结果解释、终关完成记录与返回选档，旧有效档可读 |
| [solo-playability-tuning](../../openspec/changes/solo-playability-tuning/proposal.md) | 规则可跑不等于新玩家理解 | 实际新玩家 Editor 试玩记录，针对卡点调少量已有参数并复玩 |

三关依赖已有供应、装修和成功检查点。调校只改现有规则内参数和文案，因此也使用 `skip_specs: true`；新规则回对应案。没有真实试玩者时保留未完成，不把脚本结果写成“玩家已经懂了”。

## 第四层：消息衔接与内容选择

| Change | 归属 | 可观察交付 |
| --- | --- | --- |
| [product-local-message-boundaries](../../openspec/changes/archive/2026-10-13-product-local-message-boundaries/proposal.md) | 复用已有案 | 入口、终态和功能衔接可从发送端追到接收端；保持唯一时钟、原 owner 与原子效果 |
| [level-buff-variety](../../openspec/changes/level-buff-variety/proposal.md) | 新案 | 切配/烘焙加速，旧作业不重算，Solo 与已交付合作核心分模式验收 |
| [interlevel-station-upgrade](../../openspec/changes/interlevel-station-upgrade/proposal.md) | 新案 | Solo 原工位单级升级，保留身份/内容/旧作业并保存 |
| [first-drink-order-binding](../../openspec/changes/first-drink-order-binding/proposal.md) | 新案 | Solo 鲜榨橙汁、杯子供应、锁壶/分装、贴单、交付与清理 |

内容案依赖相关消息合同；不用完整迁移每个无关模块来阻挡已经稳定的独立任务组，但开始前应明确与原消息案的任务交接。工位升级依赖 Buff 时间算法及已有保存/装修，不能在恢复时重复乘加成。

## 第五层：完整合作与成员恢复

| Change | 归属 | 交付边界 |
| --- | --- | --- |
| [cooperative-restaurant-core](../../openspec/changes/cooperative-restaurant-core/proposal.md) | 复用已有案 | Host Restaurant、两菜、Host 控制及基本投影的原案证据 |
| [cooperative-front-service](../../openspec/changes/cooperative-front-service/proposal.md) | 复用进行中案 | 前厅、认领、清桌洗碗、自然结算与换关 |
| [cooperative-cleanup-supply](../../openspec/changes/cooperative-cleanup-supply/proposal.md) | 复用已有案 | 合作清理、有限采购/收货/拆份与争抢 |
| [cooperative-restaurant-loopback](../../openspec/changes/cooperative-restaurant-loopback/proposal.md) | 复用已有案 | 前述结果交接及普通双 Editor 两种角色排列，不重复开发 |
| [cooperative-renovation-choice](../../openspec/changes/cooperative-renovation-choice/proposal.md) | 新案 | Host 选原新增工位奖励，双方观察，一次安装；不含升级/联机存档 |
| [cooperative-member-resume](../../openspec/changes/cooperative-member-resume/proposal.md) | 新案 | 本轮进程保留时手动恢复同一成员，凭证/绑定/就绪/旧消息有边界 |
| [cooperative-drink-order-binding](../../openspec/changes/cooperative-drink-order-binding/proposal.md) | 新案 | 在 Solo 橙汁基础接远端公开动作、独立投影与多人争抢 |

现有核心不等于完整餐厅；单机测试不等于远端通过。同 Editor KCP、双 Editor、Player 和物理两机各证明自己的范围。恢复、装修、饮品可按独立前置分开推进；饮品仅在已选恢复能力时补原成员恢复场景。

## 第六层：最终设备验收

| Change | 先完成的基础 | 最终交付 |
| --- | --- | --- |
| [pc-cooperative-lan-trial](../../openspec/changes/pc-cooperative-lan-trial/proposal.md) | 所选合作功能的 Editor 证据 | 真实构建 → 当轮 Player 同机双进程 → 两台 PC LAN；显式私网输入、实际监听与自身进程收尾 |
| [android-solo-playable](../../openspec/changes/android-solo-playable/proposal.md) | 所选 Solo 功能、三关、成功档与体验调校 | 真实手机触控、后台暂停/手动继续、资源/AOT、成功档恢复与完成记录 |

**真机与独立包体验收位于最后。** 两线不要求彼此完成，但都先声明本次纳入的功能。装修、恢复、饮品等未纳入本次设备版本时明确列外；若纳入，先完成其 Editor 工作再设备补验。不给所有远期候选设置永远无法越过的打包门槛。

## 关键依赖与交接

```text
UI 分层 → 菜谱引导 ───────────────────┐
供应 + 装修 + 成功检查点 → 三关终点 ──┴→ 试玩调校 → Android 真机（最后）
                         └→ Solo 橙汁 ───────────────┐
消息边界 → 新 Buff → Solo 工位升级                    │
         └→ Solo 橙汁                               │
合作核心 → 前厅/换关 → 清理/供应 → 双 Editor          │
                                  ├→ 合作装修        │
                                  ├→ 成员手动恢复    │
                                  └───────────────→ 合作橙汁
选定合作功能的 Editor 验收 → PC Player 同机 → 两台 PC LAN（最后）
```

图只显示主要关系，完整依赖以各案 design 为准。涉及同一文件时按合同交接串行修改；各文件格式版本与协议版本基于实施时最新值递增，不能各案都硬编码同一个“下一版”。

## 已确认决定与仍保留的缺口

用户本轮确认：

- 第三关结束不发装修、不建第四关，保存“大关完成”记录后返回选档。
- 橙汁加工期间锁果汁壶；完成后可搬走逐杯分装；产量与耗时由配置决定。

沿用既有确认：合作装修由 Host 选择；原成员恢复首版只做本次运行内手动恢复；Android 后台暂停，前台手动继续，强杀后读最后成功检查点。文中有限单级升级、只读提示与验收拆分是本次工程方案，写入了相应 design，不声称都是用户逐项口述。

仍不代定：下一关许可缩小、正式传输与公网方案、联机 Save Owner/跨设备许可、中心服务、复杂经济、更多伙伴/顾客，以及全目录数值平衡。这些仍在[待裁决](backlog.md)和[长期目标](../../long-term-goals.md)。联机持久经营是独立后续范围，不能由 LAN 或手动重连宣称已经支持。

## 本轮规划的检查范围

每案都有 proposal、design、tasks、execution-notes；涉及行为的 11 案共 15 份 delta spec，另外 3 案显式免除规格。14 案共 58 项实施任务全部未勾选。消息边界原案仅同步当前基线及 owner 说明，原 17 项任务仍未完成。

本轮实际检查：14 个新案及修订的消息边界案均通过 `openspec validate <名称> --strict --no-interactive`；80 份相关 Markdown 的引用、编码、空白及原规格场景保留检查未发现问题，新案依赖无环。没有运行功能、UTF、Unity、Player 或构建，没有提交或归档。规划通过不代表功能完成。
