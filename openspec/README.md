# 中文底座规格索引

这些主规格描述当前源码／配置支持的底座与产品契约；逐项交付状态、协议和模式范围以 [当前进度](../CarzyCooker当前进度.md) 及相应 change 的 execution-notes 为准。供应、装修、本机检查点与合作核心已有实现，不能从早期索引文字推断未实现。未来方向读 [产品文档](../Docs/Product/README.md)，模块路由读 [模块索引](../Docs/AI/module-index.md)。

2026-10-09 的 [深度扫描](../Docs/AI/spec-scan.md) 已细化十四项底座规格；合计十六份主规格、74 项要求、120 个场景。新增 31 项要求和 70 个场景，保留原要求；仅给固定 ET 入口补充实际编译条件。增量、来源和验证见 [deepen-foundation-specs](changes/archive/2026-10-09-deepen-foundation-specs/proposal.md) 与 [verification](changes/archive/2026-10-09-deepen-foundation-specs/verification.md)。场景是契约及后续验证目标，不代表这些动态场景已运行。

2026-10-13 已同步并归档十项完成 change，增加厨房批次、工位、菜品、伙伴、前厅服务、buff、关卡流程和餐厅 UI，当时共三十份主规格；新增项见下表。启动、本地消息、真实 KCP、历史双 Editor 会话、格子样机、备料和所属异步的结果分别见对应 change 的 execution-notes，不能以格式验证代替动态运行，也不能以历史双 Editor 会话验收替代本次加工的双进程验收。

2026-10-14 已同步并归档 UI 信息分层、菜谱引导、Solo 三关、Buff 扩展、原工位升级、首饮贴单和试玩调校七案，任务合计29/29。现有34份主规格严格验证通过；真人观察按用户豁免记 Skipped，工程运行证据及例外仍以各归档案的 execution-notes 为准。

| 规格 | 范围 |
|---|---|
| [startup](specs/startup/spec.md) | 启动/模式 |
| [runtime-foundation](specs/runtime-foundation/spec.md) | 公共组件/流程/示例 |
| [runtime-instances](specs/runtime-instances/spec.md) | 显式实例配置、工程范围请求、Play 交接与本轮入口收尾 |
| [product-session](specs/product-session/spec.md) | Unity 本机最小产品树、共同裁决、逻辑成员/连接分离、本地/远端消息与收尾 |
| [grid-kitchen](specs/grid-kitchen/spec.md) | 竖屏固定地图、四方向行走、台面交接、断线落物与同格拾取 |
| [kitchen-item-processing](specs/kitchen-item-processing/spec.md) | 真实材料／碗内容、切配与手持打蛋、单工作者 Tick、暂停接续与合作交接 |
| [kitchen-batch-cooking](specs/kitchen-batch-cooking/spec.md) | 蛋液倾倒、显式煮汤、可搬批次、有限盛装与最后一份复位 |
| [station-upgrades](specs/station-upgrades/spec.md) | Solo 原位单级升级、冻结比例及旧格式恢复 |
| [kitchen-workstations](specs/kitchen-workstations/spec.md) | 工位能力、单槽／有限池、准备态布局预检和共用地形 |
| [dish-recipes](specs/dish-recipes/spec.md) | 菜品身份、依赖闭包、真实烘焙和盘装吐司 |
| [fixed-companions](specs/fixed-companions/spec.md) | 固定伙伴、单执行者、接续、关内成长及新关重置 |
| [orders-front-service](specs/orders-front-service/spec.md) | 有界客流、询问开单、类型化交付、原餐具回收和自然排空 |
| [drink-preparation](specs/drink-preparation/spec.md) | 鲜榨橙汁、加工锁壶、完成搬壶与有限分杯；Solo 原三关证据保留 |
| [cooperative-drinks](specs/cooperative-drinks/spec.md) | 合作饮品公开动作、独立投影与争抢；新增验收进行中 |
| [cooperative-renovation](specs/cooperative-renovation/spec.md) | Host 独占选择、新增工位一次安装、双方只读结果；双 Editor 待验收 |
| [level-buffs](specs/level-buffs/spec.md) | 本关固定候选、一次选择、新作业冻结效果和跨关隔离 |
| [restaurant-level-lifecycle](specs/restaurant-level-lifecycle/spec.md) | 持续厨房、统一时钟、暂停、零星结算及内存换关 |
| [kitchen-recipe-guide](specs/kitchen-recipe-guide/spec.md) | 实际配方、当前缺口及只读首单提示；玩家理解已豁免 |
| [restaurant-chapters](specs/restaurant-chapters/spec.md) | Solo 三关、明确终关、完成记录与旧档兼容 |
| [restaurant-ui](specs/restaurant-ui/spec.md) | 同版显示、真实输入、前厅拖动和受管关闭重开 |
| [multiplayer-peer-workspaces](specs/multiplayer-peer-workspaces/spec.md) | 稳定第二端、独立缓存/身份/日志、精确目标及必要维护 |
| [ui](specs/ui/spec.md) | GF UI/ETUI/Widget |
| [entity](specs/entity/spec.md) | GF Entity/ETEntity/UIEntity |
| [resource-management](specs/resource-management/spec.md) | 容器/资源/AssetSet |
| [owned-async-lifecycle](specs/owned-async-lifecycle/spec.md) | owner 所属任务、立即取消、轮次隔离、受控写回与整体退出 |
| [data-generation](specs/data-generation/spec.md) | Excel/Luban/生成 ID |
| [proto-generation](specs/proto-generation/spec.md) | Proto/Opcode/消息生成 |
| [localization](specs/localization/spec.md) | 本地化/UX 文本 |
| [network](specs/network/spec.md) | ET Session/RPC/HTTP |
| [server](specs/server/spec.md) | 服务端/DB/Agent/Admin |
| [hot-reload](specs/hot-reload/spec.md) | HybridCLR/程序集/打包 |
| [editor-tools](specs/editor-tools/spec.md) | Editor/Toolbar/骨架/服务工具 |
| [reactive-ui](specs/reactive-ui/spec.md) | ET reactive/外部 ReactiveBinding |
| [dependencies](specs/dependencies/spec.md) | Analyzer/第三方/程序集/包 |
| [ai-collaboration](specs/ai-collaboration/spec.md) | Codex 路由、授权、技能/SOP 与产品资料状态 |
| [automated-testing](specs/automated-testing/spec.md) | 项目测试发现、显式执行、终态和失败报告 |

工作流见 [Codex/OpenSpec](../Docs/AI/workflow.md)，当前配置见 [config.yaml](config.yaml)。每项要求附场景和来源；协作资料规格描述文档与授权行为，不作为玩法能力。

2026-10-14：纯重构 `reduce-per-frame-allocations` 已 9/9 并归档，无 delta specs／主规格变更。地图、HUD、固定步与 BFS 的逐项 UTF、真实入口及性能限制见[执行记录](changes/archive/2026-10-14-reduce-per-frame-allocations/execution-notes.md)。
