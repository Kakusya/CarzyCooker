# CarzyCooker 当前进度

人读的交付进度入口，不是业务状态副本或产品验收证明。冲突时以代码/表、OpenSpec 与用户最新意图为准；改变实际能力时同步更新本页。

## 单机与联机总览（2026-10-13 核对）

**单机已到完整餐厅的连续试玩与本机成功检查点；联机已完成普通双Editor两种角色排列的完整餐厅合作验收。** 两菜交付、补货清理、暂停恢复、原餐具回收、自然换关、真实Client失联后Host收货和整轮退出新世界隔离均已运行。下文分别记录实际范围。

本页是动态交付事实的唯一摘要入口，依据当前主工作区源码、主规格、change tasks 和 execution-notes 更新。AGENTS 保留纪律，模块索引保留定位，产品目录保留目标与候选；逐功能运行结果仍只引用原 change。已通过结果只在原记录的功能和环境范围内有效，新实现不因文件已经出现而升为已验收。

| 交付线 | 当前可确认的阶段 | 主要缺口 |
| --- | --- | --- |
| 单机 Solo | 九案66/66已归档；两菜、顾客餐具、伙伴、Buff、布局、暂停、自然换关和受管UI已验收；清理／供应／检查点各7/7、装修6/6已验收 | Solo 三关终点和原工位升级已验收；鲜榨橙汁已验收；Player/AOT未验收 |
| 联机 Host/Client | 核心7/7、前厅5/5、清理供应5/5、集成3/3；普通双Editor两种排列均真实合作交付20分2星、补货清理、原餐具回收和自然换关；Client失联后Host收货与退出新轮隔离通过 | 联机装修、存档与原成员恢复仍未实现 |
| 设备与网络范围 | Solo普通Launcher及联机普通双Editor本机loopback已实跑；相关真实KCP／受管UI UTF结果复用 | 独立Player、物理两机LAN、Android真机／AOT、公网未验收 |

“Host 权威”是玩法写入职责，不等于联机完整餐厅已接通；“本地请求”可以是 Solo 或同进程 Peer，也不等于远端 wire 已开放。

## 单机游戏进度

| 功能 | 实现与交付状态 | 已有运行证据／边界 |
| --- | --- | --- |
| 默认入口、移动、拿放、容器、切配与打蛋 | 已实现、已验收 | 普通 Launcher 默认 Solo；场景／HUD 输入、容器身份、加工停续和退出新轮；见[备料记录](openspec/changes/archive/2026-10-11-grid-kitchen-prep-processing/execution-notes.md) |
| 汤与吐司两菜 | 已实现、已验收 | 真实原料加工、有限分装、烘烤、装盘及原餐具保留；见[菜品记录](openspec/changes/archive/2026-10-13-solo-dish-recipe-chain/execution-notes.md) |
| 持续厨房、暂停、自然结算与换关 | 已实现、已验收 | 普通入口两关，10 分 1 星及 0 分 0 星，真实前厅收口、实物结转、退出重开；尚非“三关完成”的大关；见[关卡记录](openspec/changes/archive/2026-10-13-solo-restaurant-level-flow/execution-notes.md) |
| 工位移动与旋转 | 已实现、已验收 | 准备态操作，重叠拒绝，带料／作业保留与移动后的服务路径；见[布局记录](openspec/changes/archive/2026-10-13-solo-workstation-layout/execution-notes.md) |
| 顾客、订单、交付、清桌与洗碗 | 已实现、已验收 | 有限池、原碗回收、超时离店与自然排空；见[前厅记录](openspec/changes/archive/2026-10-13-solo-customer-order-service/execution-notes.md) |
| 固定伙伴、工作接续与关内成长 | 已实现、已验收 | 玩家停手后伙伴认领、暂停冻结、洗碗及换关成长清理；见[伙伴记录](openspec/changes/archive/2026-10-13-solo-fixed-companion-work/execution-notes.md) |
| 本关随机 Buff | 已实现、已验收 | 固定候选、一次选择、新批次冻结耗时及换关重选；现有效果范围以煮汤为主；见[Buff 记录](openspec/changes/archive/2026-10-13-solo-random-level-buffs/execution-notes.md) |
| 受管餐厅 UI 与信息分层 | 已实现、已验收；信息分层 4/4 | 原 owner／资源 ID、23 字段 CodeBind、主要动作与只读详情；最终 Solo UI 6/6、远端与设施各 1/1，普通 540×960 两菜制作／分别交付／前厅／拖动通过；不是手机或 Player 证据；见[信息分层记录](openspec/changes/archive/2026-10-14-restaurant-ui-information-hierarchy/execution-notes.md)与[原 UI 记录](openspec/changes/archive/2026-10-13-solo-restaurant-ui/execution-notes.md) |
| 水池接水／排液、垃圾桶固体处理 | 已实现、已验收，7/7 | 设施到位、分类、加工锁定、引用收尾及原容器卫生；原汤仍默认供水；见[清理记录](openspec/changes/solo-kitchen-clear-and-discard/execution-notes.md) |
| 有限供应、运输、收货与拆份 | 已实现、已验收，7/7 | 16项供应配置／业务及2项真实HUD的最新有效结果Passed；普通Launcher耗尽面包后采购、满槽保留、原身份拆份制作与代表性长运输在途跨关通过，默认参数已恢复并实际读取40；见[供应记录](openspec/changes/solo-supply-delivery/execution-notes.md) |
| 关间装修奖励与下一关安装 | 已实现、已验收，6/6 | 14项配置／业务及2项实际UI最新有效结果Passed；普通Launcher零星选择、一次安装、移动新工位和再次跨关通过；初期为内存Solo，后续检查点及原工位升级已接入保存，见[装修记录](openspec/changes/solo-interlevel-renovation/execution-notes.md) |
| 选档、新档、成功检查点与恢复 | PC Solo已实现并验收，7/7 | 本机稳定身份、轻量选档／取消／拒绝、实际文件不覆盖提交、失败重试和新身份恢复通过；普通Launcher半锅汤／3/60暂停加工／在途货保存后Stop/Play选档恢复11物品17工位，库存不补满；见[检查点记录](openspec/changes/solo-success-checkpoint/execution-notes.md) |
| 原工位单级升级 | 已验收，4/4 | Solo 原位升级，不换身份／占地／实物；新作业与 Buff 一次取整，v1/v2 默认零等级、v3 冻结恢复；十六项新例及相关回归通过，见[升级记录](openspec/changes/archive/2026-10-14-interlevel-station-upgrade/execution-notes.md) |
| 首饮与独立贴单 | 已验收，5/5 | Solo 真实切橙、锁壶榨汁、搬壶分杯、有效订单贴单与一次性杯清桌；15核心＋1UI新UTF及18相关回归Passed，普通201→202→203整链／清理／返回选档通过；v4保存，旧v1–v3只读兼容；见[首饮记录](openspec/changes/archive/2026-10-14-first-drink-order-binding/execution-notes.md) |
| 试玩调校 | 工程复玩完成，3/3；真人观察Skipped（用户豁免） | 无依据调整平衡，保留现有数值；首单、有限补货和三关收口有对应实际结果，不宣称理解或留存；见[试玩记录](openspec/changes/archive/2026-10-14-solo-playability-tuning/execution-notes.md) |
| 切配／烘焙 Buff | 已验收，4/4，Solo／真实 KCP | 原三汤效果加指定切配／烘焙，启动冻结／暂停跨关不重算；新增十项及原二十项通过，普通 HUD 30/60 Tick 已核实，见[加成记录](openspec/changes/archive/2026-10-14-level-buff-variety/execution-notes.md) |
| Solo 首个三关大关 | 已验收，5/5 | 新开独立三关，终关无装修／第四关；完成记录只读、失败重试返回选档、v1 原 Next 兼容，新增消息／文件 4 项与 UI 1 项通过，见[三关记录](openspec/changes/archive/2026-10-14-solo-first-chapter/execution-notes.md) |
| 菜谱与首单引导 | 代码与自动验收完成；玩家理解按用户要求跳过 | 两菜配方图、当前缺口、预制菜／拒绝／暂停／池复用／旧关与 KCP 只读，3 项 UTF 通过；新玩家首单／另一单观察 Skipped（用户豁免），效果未验证，见[菜谱记录](openspec/changes/archive/2026-10-14-kitchen-recipe-guide/execution-notes.md) |
| 三关终点、更多 Buff、升级、橙汁 | 本轮已授权实施，尚未完成 | 依赖与交付边界见[change 路线](Docs/Product/change-roadmap.md)；当前以各案 tasks 和 execution-notes 为准 |

## 联网游戏进度

| 功能 | 目前实际支持／证据 | 尚缺的餐厅接入或验证 |
| --- | --- | --- |
| 启动、Join、名单、Probe、心跳与 Leave | 已实现；[会话记录](openspec/changes/archive/2026-10-11-product-session-bootstrap/execution-notes.md)含真实 KCP 与双 Editor 两种角色排列 | 历史名单／Probe 验收不证明餐厅玩法；普通 Join 新增成员；本轮 Resume 已接入并有真实 KCP／受管 HUD 证据，双 Editor 新增恢复仍待验收 |
| 共同裁决、本地消息、可信 Peer 与独立投影 | 早期会话／地图范围已实现并运行 | 新餐厅请求仍需逐类网络桥接、身份校验和字段复制，不因本地 handler 存在而自动支持 |
| 格子移动、交接、掉落与拾取 | 旧样机已有真实 KCP 证据 | [格子记录](openspec/changes/archive/2026-10-11-portrait-grid-kitchen-prototype/execution-notes.md)为同 Editor 独立 Fiber；不能当作新餐厅双进程验收 |
| 番茄切配、碗内打蛋、停手交接与接续 | 已实现；[备料记录](openspec/changes/archive/2026-10-11-grid-kitchen-prep-processing/execution-notes.md)有同 Editor 真实 KCP＋受管 HUD 实操 | 当次未运行独立 Client 进程；关闭 Peer 的失联操作不等于进程重启重连 |
| Host持续餐厅、当前Level、唯一业务时钟 | 已接入与Solo相同Restaurant／持续厨房／当前Level；单步与暂停已验收 | legacy驱动仅无Restaurant时适用，真实Host不双推进；见[核心记录](openspec/changes/cooperative-restaurant-core/execution-notes.md) |
| 煮汤／盛装、烘焙／装盘与交付 | 已显式开放核心Grid1–12及ServeOrder；真实KCP与两个所属受管HUD分工两菜并按真实订单交付 | 核心制作证据复用；前厅19项UTF及普通入口首关20分／2星通过，见[前厅记录](openspec/changes/cooperative-front-service/execution-notes.md) |
| 开店、暂停／恢复、换关、布局、Buff | 已接通Level／Layout／Buff与Host本地可信控制；远端越权零改动，真实前厅暂停／旧关隔离通过 | 普通入口首关20分／2星、次关0分／0星自然结算，双方新角色与新Buff就绪，精确6件库存保留 |
| 顾客、订单、伙伴、洗碗和前厅 HUD | 远端Serve／FrontWork已开放并验收；同版顾客／工作／伙伴成长／池投影及受管重开通过 | 竞争单执行者／单时钟、远端Stop／Leave／失联接续、原碗清洗回收与下一关取回均有真实KCP／普通入口证据 |
| 水池／垃圾桶、供应 | 远端网络／HUD已接入，物流案5/5：15项真实KCP／受管UI最新结果通过 | 普通入口耗尽面包后补货制作并交付、满槽保货、剩液／空包装清理与同一在途货物跨关通过；默认运输40已恢复并由新Host实际读取，见[物流记录](openspec/changes/cooperative-cleanup-supply/execution-notes.md) |
| 餐厅中的争抢与成员退出 | 核心末份／原物落下／自动与手工接续、前厅三方认领／远端断线洗碗接续、公共货物唯一收货及采购未读确认便断线保货已验收；双Editor真实Client失联后同一原碗落物与同一货物被Host收取通过 | 新 Join 是新成员；本轮凭证 Resume 复用原成员，双 Editor 新增链尚待验收 |
| 联机关间装修 | 代码及 4 核心＋1真实 HUD UTF 已运行；待双 Editor 增量 | Host 独占选择／继续，Client 只读，饮品布局新增工位 5／6，失败零安装；见[装修记录](openspec/changes/cooperative-renovation-choice/execution-notes.md) |
| 原成员恢复 | 代码及10核心＋1真实HUD UTF Passed；待双Editor增量 | 内存凭证／新 Session／同成员新绑定、当前关空手、旧回调隔离；回执丢失／超时／平台／旧请求均已验，双 Editor 待验，见[恢复记录](openspec/changes/cooperative-member-resume/execution-notes.md) |
| 合作橙汁与贴单 | 5核心＋1采购到清桌真实HUD UTF Passed；待双Editor增量 | Host 默认 3→4→4 饮品关卡、公开制作／分杯／贴单动作及独立投影；双 Editor 待验，见[饮品记录](openspec/changes/cooperative-drink-order-binding/execution-notes.md) |
| 联机存档／Owner／恢复进度 | 未实现，现有联机案明确排除 | 单机成功档不能直接充当联机存档，Host 也不自动成为 Save Owner |
| LAN、公网、独立 Player | 未验收；当前仅 loopback | 配置仍限制回环地址；公网、正式传输选择和 Host 迁移均不属于已有交付 |

### 联机交付基线与后续边界

- [ProductRuntimeBootstrap](Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs)：Host与Solo均建Restaurant；核心／前厅／物流及普通双Editor两种角色排列均已运行，Client只有独立投影。
- [ProductWire](Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs)：协议22远端开放Grid1–21及Level／Layout／Buff／ChooseRenovation、ServeOrder／FrontWork、SupplyCommand与专用Resume；内部Submit／伙伴消息仍关闭，未知动作仍拒绝。恢复消息含内存能力凭证，通用调试日志禁止展开。
- [会话主规格](openspec/specs/product-session/spec.md)区分会话／格子基础和新增单机合同；[批次规格](openspec/changes/archive/2026-10-13-grid-kitchen-soup-batch-cooking/specs/kitchen-batch-cooking/spec.md)不承诺网络接入。
- 原联机餐厅大案已按用户要求拆出三个前置，三案已实际交付；原案普通独立Host／Client就绪与双Editor整体验收3/3通过，具体环境和每项运行结果见下表。

| 顺序 | 已创建 change | 当前任务 | 交付范围／依赖 |
| --- | --- | --- | --- |
| 1 | [cooperative-restaurant-core](openspec/changes/cooperative-restaurant-core/proposal.md) | 7/7，已验收 | Host餐厅／两菜／可信控制／独立投影与退出；28个核心UTF及普通Host＋同Editor真实KCP受管入口通过 |
| 2 | [cooperative-front-service](openspec/changes/cooperative-front-service/proposal.md) | 5/5，已验收 | 远端前厅／伙伴接续／原餐具／自然换关；19项UTF及普通入口两菜交付、原碗回收和自然两关通过 |
| 3 | [cooperative-cleanup-supply](openspec/changes/cooperative-cleanup-supply/proposal.md) | 5/5，已验收 | 远端设施／公共物流15项UTF及普通入口补货制作交付、剩液／空壳处理、在途换关通过 |
| 4 | [cooperative-restaurant-loopback](openspec/changes/cooperative-restaurant-loopback/proposal.md) | 3/3，已验收 | 普通双Editor两种角色均实际交付20分2星／自然换关；失联收货、原物保留、退出新世界隔离通过，见[集成记录](openspec/changes/cooperative-restaurant-loopback/execution-notes.md) |

旧任务与规格迁移见[原案迁移账本](openspec/changes/cooperative-restaurant-loopback/execution-notes.md)。拆分范围20项均已验收；原案3项由真实普通双Editor结果覆盖，前置未变的UTF与同Editor证据复用。

### 验收环境不能互相替代

| 环境 | 已有证据 | 当前不能宣称 |
| --- | --- | --- |
| Solo 普通 Launcher | 两菜、两关经营、清理、有限供应与 UI | Player／手机端已可玩 |
| 同 Editor 独立 Fiber＋真实 KCP | 核心两菜、前厅餐具循环、物流补货清理与在途换关 | 当前餐厅双进程已验收 |
| 两个独立 Editor | 完整餐厅两种角色排列、两菜真实交付、补货清理、暂停恢复、原餐具循环、自然换关、Client失联后Host收货及退出新轮 | Player、跨设备LAN、公网与原成员恢复 |
| 独立 Player、两台物理 PC LAN、Android 真机 | 本次未找到对应玩法交付证据，NotRun | 用 Editor 截图或配置代替运行 |

当前协议为 **21**，含Host可信控制资格与已开放的前厅／物流动作。PC Solo检查点交付为 **16**，含绑定存档控制与独立保存状态投影；装修为 **15**，供应为 **14**，清理为 **13**，早期备料为 **3**。协议数字表示兼容边界，不表示双端已经验收，历史记录保留当时语义。

## 维护规则

后续每项能力分别更新 Solo 与 Host/Client 的实现、验收环境、证据链接及缺口。共享代码完成只更新对应实现事实；远端命令、投影和实际双端运行齐备后才能提升联机交付状态。候选、进行中、已验收分开记录，任务数不作为游戏完成率。

基于这些差距修订的候选依赖与模式边界见[产品待办中的分线 change 安排](Docs/Product/backlog.md)。本页记录现状，该安排记录未来设计，不互相替代。

## 共用基础与历史交付明细

下表保留原交付证据和各次运行范围；整体玩法现状以以上两条进度线为入口。历史“当前协议”按该次交付解释，不用于推断现在的网络功能范围。

本轮三个基础设施 change 已完成：资料整理与夹具稳定各 3/3，夹具相关 85 个唯一 PlayMode UTF Passed；消息边界 17/17，原输入收尾、独立终态、厨房／供应／前厅后继、关卡／布局／Buff／装修、已有存档和受管HUD均有逐功能验收。最新54项边界UTF全部Passed，相关功能及生命周期小集的最新有效结果也均Passed，失败及修正保留在原记录。普通Launcher实际两菜20分2星、原碗／盘洗净、采购拆份清理、暂停恢复、装修写盘及半锅承接下一关通过；Continue只有新关准备完成才报Executed，另经新轮0分0星普通换关增量验证。当前源码协议18，不提升Player/AOT、Android、物理LAN/公网、双Editor或新远端玩法范围。具体映射见[资料记录](openspec/changes/archive/2026-10-13-streamline-agent-project-context/execution-notes.md)、[夹具记录](openspec/changes/archive/2026-10-13-stabilize-product-test-fixtures/execution-notes.md)、[消息记录](openspec/changes/archive/2026-10-13-product-local-message-boundaries/execution-notes.md)。三个 change 已于2026-10-13归档，network与product-session主规格已同步；提交与远端状态以 Git 为准。

| 范围 | 交付记录（按原验证范围） | 入口 |
| --- | --- | --- |
| 框架底座 | 当前 main 分支的 ET 底座保留 GF + ET 双端、HybridCLR、UniTask、Luban 与 ET 示例；GameHot/GF Network 已移除 | [框架总览](Docs/Framework/README.md)、[模块索引](Docs/AI/module-index.md) |
| 所属异步生命周期 | IAsyncOwner/RunAsync/Apply、公共 UI/Entity 与 ET owner、资源轮次、延时/请求等待和预拆卸取消已实施；27个新增 UTF 各有 Passed 证据，真实 AssetBundle 创建/读取退役及再加载通过，另补1个原计时用例；保持有限迁移范围 | [执行说明](openspec/changes/archive/2026-10-11-manage-owned-async-lifecycle/execution-notes.md)、[生命周期用法](references/framework-lifecycle.md) |
| 底座入口与导表 | 同步泛型 UIEntity 统一独立路由；Luban 失败传播为 CLI 非零并阻断复制/ID/本地化，Editor 导表失败停止后续刷新。隔离成功/失败验证和工具/Unity 编译通过；真实工作表导出、UIEntity 显示未运行 | [执行记录](openspec/changes/archive/2026-10-11-fix-foundation-entry-and-export-errors/execution-notes.md)、[Entity 用法](Book/Entity开发.md)、[导表用法](Book/Luban配置.md) |
| 协作体系 | 主工作区保留十二项OpenSpec、五项项目技能、官方unity-cli和四项SOP，共二十二项；第二工作区按执行职责保留6项项目技能，开发技能物理删除；十九份中文主规格已建立，十四项底座契约已细化 | [AGENTS](AGENTS.md)、[工作流](Docs/AI/workflow.md)、[规格索引](openspec/README.md)、[扫描记录](Docs/AI/spec-scan.md) |
| 做饭玩法 | 九个单机change完成66/66项并已归档；两菜、出餐、原餐具、伙伴、成长与自然换关已验收，受管UI与540×960已检查。后续清理／供应／检查点各7/7、装修6/6已验收；普通双Editor完整合作餐厅3/3已验收 | [关卡记录](openspec/changes/archive/2026-10-13-solo-restaurant-level-flow/execution-notes.md)、[检查点记录](openspec/changes/solo-success-checkpoint/execution-notes.md) |
| 厨房设施清理 | Solo 实际水池接水／排液、垃圾桶固体倾倒、固液与熟汤分类、加工锁定／引用收尾、共址洗碗和原容器卫生保留已验收，7/7，协议13。相关行为 UTF 与普通 Launcher 同局接／倒水、剩汤排液、真实吐司装盘倾倒及错设施拒绝通过；当前汤仍默认供水；协议17已开放远端清理 | [设施清理记录](openspec/changes/solo-kitchen-clear-and-discard/execution-notes.md) |
| 单机关卡流程 | 持续Restaurant／厨房、本关角色、唯一时钟、暂停取消／冻结、真实前厅排空和实物换关已验收，8/8，协议12。最终14项SoloLevel UTF通过，包含另一真实Peer换关后四类旧原请求拒绝；原重复Continue仍只换一次。普通Launcher两关实际10分1星及0分0星结算、成长6→0、buff重选、原手持落最后格、退出新世界通过 | [执行说明](openspec/changes/archive/2026-10-13-solo-restaurant-level-flow/execution-notes.md) |
| 工作台与准备态布局 | 工位源配置、真实布局预检及同物品／作业保留已验收，6/6。普通 Launcher 同一局带料移动旋转、拒绝重叠、切配接续、移带锅灶台、营业旧按钮拒绝，并用移动后餐桌完成实际顾客／伙伴／玩家服务路径 | [执行说明](openspec/changes/archive/2026-10-13-solo-workstation-layout/execution-notes.md) |
| 两道代表菜 | Dish／Stage／Bake 源与两关相同菜单、无环依赖闭包、实际烘烤新身份、原盘装同一烤面包已接入，协议 8。19 项 SoloDish UTF、45 项受源布局／Level 字段影响的容器／汤／关卡／工位回归通过；普通 Launcher 同一局 26 命令制作盘装吐司与碗装汤，真实手持／进度／暂停／提前拿取拒绝均通过，菜品 change 6/6。未接订单前暂存不加分，有限供应不补半成品 | [执行说明](openspec/changes/archive/2026-10-13-solo-dish-recipe-chain/execution-notes.md) |
| 固定伙伴工作 | 固定身份、关内Role／成长、本地有序认领、到位单执行者、停手保留及第三次完成冻结加速已验收，7/7。32项伙伴UTF通过；最终普通Launcher直接观察玩家停手约28%后伙伴第二次认领推进到约34%，暂停冻结、恢复洗净原碗，自然换关成长6→0。旧工作原请求跨关拒绝和当前关重放另已补验 | [执行说明](openspec/changes/archive/2026-10-13-solo-fixed-companion-work/execution-notes.md) |
| 顾客订单与有限餐具 | 有界 FIFO、真实入座、询问开单、超时零分离店、真实交付、脏餐具制作限制、有限池回收和同碗取回已验收，7/7。目标 15、客流 6、出餐／餐具／关尾 16 项 UTF 各 Passed，受变行为回归 35 Passed。普通 Launcher 同局制作服务、伙伴接续、10 分 1 星自然收口及新关同碗取回通过，协议 11 | [执行说明](openspec/changes/archive/2026-10-13-solo-customer-order-service/execution-notes.md) |
| 随机本关加成 | 每关三选二固定候选、真实一次选择／门槛与新锅冻结倍率已验收，6/6，协议12；20项新UTF与24项当时受影响回归各有Passed。真实UI关闭重开保留Seed／候选，普通Launcher旧锅120不变、新锅60、暂停、默认零星自然继续和新关重新选择均通过 | [执行说明](openspec/changes/archive/2026-10-13-solo-random-level-buffs/execution-notes.md) |
| 完整受管餐厅界面 | 原UI1101／WorldSpace Canvas及Board owner扩展，正常CodeBind生成19字段，6/6。五项真实Prefab／Launcher UI UTF各Passed，覆盖指针拖动、厨房适配、HUD不穿透、一次请求、加载退出、Mono池复用、真实旧callback、回执／加工完成与拒绝保物。普通Launcher完整两关及实际540×960GameView通过；最终美术与手机触控未验收 | [执行说明](openspec/changes/archive/2026-10-13-solo-restaurant-ui/execution-notes.md)、[用法](Book/UI开发.md) |
| 单机产品消息 | Peer 在途保护延续到原物理答复／超时／Stop；统一客户端业务编号、最新结果重放、冲突／旧身份拒绝、真实 Solo 无监听 fixture 与独立时间泵已实施。实施基线协议 4，当前协议 12；7 项新增 UTF＋2 项相关既有 UTF 各 Passed，普通 Launcher 两轮 PointerClick 单次提交／跨轮隔离通过 | [执行说明](openspec/changes/archive/2026-10-13-solo-local-message-contracts/execution-notes.md)、[消息约定](references/reactive-and-network.md) |
| 单机煮汤与盛装 | 同一蛋液倾倒保留原碗、锅内配方完整匹配／显式启动、自动批次随锅移动、灶台进行中占用、有限两份盛装及末份消费输入／同锅复位已验收，14/14。实施基线协议5，当前12；11项煮汤、2项容器和4项格子各Passed，普通Launcher整链及原碗重开通过。后续单机订单／洗碗／跨关证据归对应change；新增动作仅本地，KCP新动作未验收 | [执行说明](openspec/changes/archive/2026-10-13-grid-kitchen-soup-batch-cooking/execution-notes.md) |
| 备料与手工加工 | 五种真实物品、碗的 ItemId 内容、切番茄／手持碗打蛋、单工作者 Tick 推进、停止／移动／失联暂停与队友接续、新半成品身份及受管 HUD 已实施，协议 3。五目标 Luban／Proto 生成、12 项相关 UTF 各有 Passed、540×960 普通 Solo 两条链及同 Editor 独立 Fiber 真实 KCP 合作／失联后续作通过；完整汤与经营、手机／Player／AOT／LAN 未验收 | [执行说明](openspec/changes/archive/2026-10-11-grid-kitchen-prep-processing/execution-notes.md)、[设计与消息路径](openspec/changes/archive/2026-10-11-grid-kitchen-prep-processing/design.md) |
| 竖屏格子样机 | 普通 Launcher.unity 直接 Play 默认 Solo，实际显示托管 2D Board／HUD；已直接运行两轮，540×960 场景点格和 HUD 拿碗／放交接台通过。此前普通入口仍进示例，旧交付表述过宽，已修正。Match 固定 Tick、四方向寻路／两人停驻／相邻交接／同格落物、共同本地与 KCP 消息、独立投影已实施。UTF 七项各有 Passed 证据：Core 4/4、复用 Wire 3/3；真实 KCP 交接／断线拾物通过，同一 Editor 两个独立 Fiber。双 Editor 新玩法、手机 Player／AOT／LAN／公网 NotRun | [执行说明](openspec/changes/archive/2026-10-11-portrait-grid-kitchen-prototype/execution-notes.md)、[任务](openspec/changes/archive/2026-10-11-portrait-grid-kitchen-prototype/tasks.md) |
| 示例配置与测试 Widget 清理 | 保留公共表结构；UIEntity／Scene／音频／Unit／AI 数据为空。累计清理 38 条普通示例记录，整表删除 DTDemo／OneConfig 及生成物，移除登录页测试 Widget 节点、加载代码、独立资源与脚本。UI 仅六条既有初始化链路注册及地图 HUD，Entity 仅地图一条；四张 StartConfig 源表保留。Luban 五个目标、完整 Unity 编译、普通 Launcher 登录页和 Solo 地图启动通过；旧 Demo 单位／AI 演示不再可用 | [导表用法](Book/Luban配置.md)、[执行说明](openspec/changes/archive/2026-10-11-portrait-grid-kitchen-prototype/execution-notes.md) |
| 联机启动基础 | 显式 legacy/host/client 配置、工程范围请求、普通 Launcher 兼容与本轮 owner 收尾已实施；产品未接入时 Host/Client 拒绝，不启动 Demo | [执行记录](openspec/changes/archive/2026-10-11-multiplayer-instance-bootstrap/execution-notes.md)、[实例规格](openspec/specs/runtime-instances/spec.md) |
| 本地请求退出 | 原 ProcessInnerSender 的幂等 Stop/Destroy、等待取消、旧计时/答复隔离已实施；真实消息 UTF 9/9 Passed，Unity/DotNet.Core 编译通过，不证明产品联机 | [执行记录](openspec/changes/archive/2026-10-11-local-message-request-shutdown/execution-notes.md)、[网络规格](openspec/specs/network/spec.md) |
| 产品会话原型 | Unity loopback Host/Client、独立产品树、Match/Participant、共用有序裁决、本地消息及 UDP-KCP 已实现；18个产品UTF各有Passed证据，双Editor两种角色排列的名单/Probe/心跳、断线保留/Leave移除及重复Stop释放已核实，不含厨房/存档/身份恢复 | [执行记录](openspec/changes/archive/2026-10-11-product-session-bootstrap/execution-notes.md)、[设计](openspec/changes/archive/2026-10-11-product-session-bootstrap/design.md) |
| 第二工作区 | 固定本地分支multiplayer-baseline、目录可变，当前CarzyCooker-NetTest；独立Editor/缓存/日志、产品身份与两种联调已核实，代码基线20582007保留。AGENTS37行、6项项目技能，禁止开发；主工作区挑main必要提交后交接cherry-pick，第二工作区本地精简配置尚未提交。F盲收件回读通过后两个Editor正常退出，目录/缓存/日志/代理终端保留 | [共用规程](Docs/AI/multiplayer-peer-workflow.md)、[环境记录](openspec/changes/archive/2026-10-11-multiplayer-peer-workspace/execution-notes.md)、[精简记录](openspec/changes/archive/2026-10-11-simplify-second-workspace-executor/execution-notes.md) |
| 验证 | 初始迁移与规格细化采用静态检查；Solo 完整两菜与两关餐厅已有实际运行证据。联机的会话、格子和备料证据分别受上述环境限制；完整联机餐厅已通过普通双Editor验收；产品构建与物理两机LAN仍NotRun | [Unity CLI](Book/UnityCLI.md)、[测试/SOP迁移](Docs/AI/testing-sop-migration.md)、[规格验证](openspec/changes/archive/2026-10-09-deepen-foundation-specs/verification.md) |
| 自动化配套 | Unity CLI/Pipeline、Tools/AutoTesting、相关技能及启动/资源/测试/打包/错误/端口/UI/归档 SOP 已迁入；专用双包编排、PortRegistry、UI 结构 runner 等仍未接入 | [已迁入配套](Docs/AI/skill-roadmap.md)、[剩余缺口](Docs/AI/reference-alignment.md) |

不会因设计写完、候选表存在、任务勾选或格式通过，把未来玩法列为已实现。后续玩法必须有实际代码/配置和对应验证结果后再更新。
