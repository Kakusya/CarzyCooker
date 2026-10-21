# 中文底座规格索引

2026-10-21 启动入口与开发测试入口两案已归档，10/10任务；13条delta要求已同步3份主规格，52份主规格严格校验通过。沿用本轮逐项UTF、普通Launcher和真实KCP结果；独立Player、真机与物理LAN仍NotRun。用户已授权归档、提交与推送。见[入口验收](changes/archive/2026-10-21-product-entry-flow-clarity/execution-notes.md)及[测试入口验收](changes/archive/2026-10-21-development-test-level-entry/execution-notes.md)。

2026-10-20 HUD、首局路径、情境动作、第一章节奏、短声画和有限视觉样板六案已归档，29/29任务；8份delta的23条新增与1条修改已同步6份主规格，保留原场景。原UTF及正常三关运行证据随案保留，用户已接受有限视觉方向和短音试听；没有增加Player、真机或真人新手结论。本轮授权本地提交，不推送。

这些主规格描述当前源码／配置支持的底座与产品契约；逐项交付状态、协议和模式范围以 [当前进度](../CarzyCooker当前进度.md) 及相应 change 的 execution-notes 为准。供应、装修、本机检查点与合作核心已有实现，不能从早期索引文字推断未实现。未来方向读 [产品文档](../Docs/Product/README.md)，模块路由读 [模块索引](../Docs/AI/module-index.md)。

2026-10-09 的 [深度扫描](../Docs/AI/spec-scan.md) 已细化十四项底座规格；合计十六份主规格、74 项要求、120 个场景。新增 31 项要求和 70 个场景，保留原要求；仅给固定 ET 入口补充实际编译条件。增量、来源和验证见 [deepen-foundation-specs](changes/archive/2026-10-09-deepen-foundation-specs/proposal.md) 与 [verification](changes/archive/2026-10-09-deepen-foundation-specs/verification.md)。场景是契约及后续验证目标，不代表这些动态场景已运行。

2026-10-13 已同步并归档十项完成 change，增加厨房批次、工位、菜品、伙伴、前厅服务、buff、关卡流程和餐厅 UI，当时共三十份主规格；新增项见下表。启动、本地消息、真实 KCP、历史双 Editor 会话、格子样机、备料和所属异步的结果分别见对应 change 的 execution-notes，不能以格式验证代替动态运行，也不能以历史双 Editor 会话验收替代本次加工的双进程验收。

2026-10-14 已同步并归档 UI 信息分层、菜谱引导、Solo 三关、Buff 扩展、原工位升级、首饮贴单和试玩调校七案，任务合计29/29。现有34份主规格严格验证通过；真人观察按用户豁免记 Skipped，工程运行证据及例外仍以各归档案的 execution-notes 为准。

同日已完成并归档合作装修、原成员恢复与合作饮品贴单三案，合计13/13；新增链在主工作区Host／第二工作区Client的真实双Editor通过，复用22项新增UTF及11项相关回归结果。三份delta的13项需求与场景已核对主规格，当前36份主规格严格验证通过；原始失败及设备未验收范围保留在各归档案记录。

2026-10-15 已完成并归档[16项业务变更](../Docs/Product/full-version-roadmap.md)，66/66任务。涉及27份delta、5组共享规格按实际最终实现核对，保留扩建后续合同；离线协议案显式跳过delta。48份主规格严格验证通过。原功能运行记录随各案归档，未因归档重复执行未改变的套件；设备／物理LAN／Player/AOT／构建边界保持。

2026-10-16 已归档动画样板、动画真实接入、菜谱实物提示、菜单相关加成、扩展工位升级及产品枚举六案，合计21/21任务；11条新增需求已核对同步，48份主规格严格验证通过。PC合作与Android单机尚未全部完成，仍在活动目录；原运行证据随完成案归档，未因归档重跑未改变的功能套件。

2026-10-16 原18项入口业务与UI变更已完成并归档，54/54任务；36条增补要求／56场景合并至10份主规格，全量严格验证72/72通过。实现、UTF及普通Launcher逐项记录见[入口归档索引](changes/archive/2026-10-16-product-entry-navigation/entry-roadmap.md)。本地提交包含已完成的枚举、菜单加成、扩展升级和菜谱实物提示前置；后续21案仍为规划，设备／Player／物理LAN未由本批证明。

2026-10-17 [合作经营与蒸茶内容13案](../Docs/Product/cooperation-and-content-delivery-roadmap.md)已归档，33/33任务完成；14份delta的31条要求已核对主规格。原功能运行记录随案保留，本轮只检查归档格式和引用，不重复执行未变功能。

| 规格 | 范围 |
|---|---|
| [startup](specs/startup/spec.md) | 启动/模式 |
| [runtime-foundation](specs/runtime-foundation/spec.md) | 公共组件/流程/示例 |
| [runtime-instances](specs/runtime-instances/spec.md) | 显式实例配置、工程范围请求、Play 交接与本轮入口收尾 |
| [product-entry](specs/product-entry/spec.md) | 普通模式菜单、无餐厅房间、开局/返回/重开及取消失败 |
| [development-test-levels](specs/development-test-levels/spec.md) | 开发环境Solo指定起点、临时进度隔离、真实规则与重开返回 |
| [product-preferences](specs/product-preferences/spec.md) | 本机字号、地图拖动和提醒偏好，独立文件、明确保存/失败保旧及原交互保护 |
| [cooperative-save-baselines](specs/cooperative-save-baselines/spec.md) | 当前绑定Profile证明、离线授权基线读取及真实库存恢复 |
| [product-session](specs/product-session/spec.md) | Unity 本机产品树、共同裁决、逻辑成员/连接分离、本轮手动恢复、本地/远端消息与收尾 |
| [cooperative-target-markers](specs/cooperative-target-markers/spec.md) | 每成员一个真实工位/物品/餐桌共享标记，10业务秒、暂停/退出/恢复和原操作边界 |
| [grid-kitchen](specs/grid-kitchen/spec.md) | 竖屏固定地图、四方向行走、台面交接、断线落物与同格拾取 |
| [kitchen-item-processing](specs/kitchen-item-processing/spec.md) | 真实材料／碗内容、切配与手持打蛋、单工作者 Tick、暂停接续与合作交接 |
| [kitchen-batch-cooking](specs/kitchen-batch-cooking/spec.md) | 蛋液倾倒、显式煮汤、可搬批次、有限盛装与最后一份复位 |
| [station-upgrades](specs/station-upgrades/spec.md) | Solo/合作原位单级升级、冻结比例及旧格式恢复 |
| [kitchen-workstations](specs/kitchen-workstations/spec.md) | 工位能力、单槽／有限池、准备态布局预检和共用地形 |
| [dish-recipes](specs/dish-recipes/spec.md) | 菜品身份、依赖闭包、真实烘焙和盘装吐司 |
| [fixed-companions](specs/fixed-companions/spec.md) | 固定伙伴、单执行者、接续、关内成长及新关重置 |
| [orders-front-service](specs/orders-front-service/spec.md) | 有界客流、询问开单、类型化交付、原餐具回收和自然排空 |
| [drink-preparation](specs/drink-preparation/spec.md) | 鲜榨橙汁、加工锁壶、完成搬壶与有限分杯；Solo 原三关证据保留 |
| [cooperative-drinks](specs/cooperative-drinks/spec.md) | 合作饮品公开动作、独立投影与争抢；恢复后采购到清桌已双Editor验收 |
| [cooperative-renovation](specs/cooperative-renovation/spec.md) | Host 独占选择、新增工位一次安装、Client只读结果；选择／换关／移动已双Editor验收 |
| [level-buffs](specs/level-buffs/spec.md) | 本关固定候选、一次选择、新作业冻结效果和跨关隔离 |
| [restaurant-level-lifecycle](specs/restaurant-level-lifecycle/spec.md) | 持续厨房、统一时钟、暂停、零星结算及内存换关 |
| [kitchen-recipe-guide](specs/kitchen-recipe-guide/spec.md) | 实际配方、当前缺口及只读首单提示；玩家理解已豁免 |
| [restaurant-chapters](specs/restaurant-chapters/spec.md) | Solo 三关、明确终关、完成记录与旧档兼容 |
| [restaurant-ui](specs/restaurant-ui/spec.md) | 同版显示、真实输入、前厅拖动和受管关闭重开 |
| [restaurant-feedback](specs/restaurant-feedback/spec.md) | 已提交动作的经营短音、去重、本机即时声音与所属播放回收 |
| [restaurant-art](specs/restaurant-art/spec.md) | 统一斜俯视美术、当前资源、方向／内容图层、竖屏可读性与本轮回收 |
| [kitchen-supply](specs/kitchen-supply/spec.md) | 有限采购、业务运输、满槽待收货、真实包装拆份及跨关保留 |
| [restaurant-renovation](specs/restaurant-renovation/spec.md) | 成功固定候选、下一关一次安装、整体布局预检及持续装修归属 |
| [restaurant-unlocks](specs/restaurant-unlocks/spec.md) | 持续定义解锁、逐关生产闭包、许可缩小保物与旧档适配 |
| [solo-save-progress](specs/solo-save-progress/spec.md) | 本机选档、成功结果新建提交、最多一次保存及完整载荷恢复 |
| [progress-claims](specs/progress-claims/spec.md) | 合作开局资格、稳定结果/凭证、持久化ACK、主动新建及整个Match正常关闭 |
| [save-authorization](specs/save-authorization/spec.md) | 同平台独立分支导入、目标Owner/修订有损确认、原子替换及消费去重 |
| [cooperative-kitchen](specs/cooperative-kitchen/spec.md) | 正式 Host／Solo 单厨房、可信共享控制、两菜共同裁决及加入就绪 |
| [cooperative-kitchen-logistics](specs/cooperative-kitchen-logistics/spec.md) | 远端设施、公共有限采购、唯一收货、包装数量和持续物流 |
| [cooperative-restaurant](specs/cooperative-restaurant/spec.md) | 普通双 Editor 两种角色完整合作、控制／退出组合及基础验收范围 |
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

2026-10-14：Solo 清理、供应、关间装修、成功检查点及合作核心、前厅、清理供应、普通双 Editor 集成八案已归档，任务合计 47/47。11 份 delta 的 41 项新增／4 项修改要求及 66 个场景已合并核对，新增六份主规格，现共 42 份；原逐功能运行与设备范围见各归档 execution-notes，本次不重复运行实现未变的测试。
