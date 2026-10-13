# Execution Notes

## 当前集成准备（2026-10-13）

三个开发前置为核心7/7、前厅5/5、清理供应5/5；原案已实际验收3/3，普通独立Host／Client双Editor两种角色完整合作、真实Client失联后Host继续收货及整轮退出新世界隔离均通过。当前协议17，内部Submit／伙伴请求仍不对外开放；没有新增传输／监听入口。独立Player／AOT、LAN／公网、联机装修／存档／原成员恢复不属于已交付范围。

### 指定同步与普通独立入口1.1

用户授权后，本轮main实现`906829ef`和记录`4bfe1a2c`已提交。第二工作区仅纳入指定四个运行依赖：`820bc3a2`→`a099f888`，`a494229e`→`fc7a7842`，`5c6ffede`→`c4492f38`，`906829ef`→`fed20ee2`。源码、Proto／表与生成物无冲突；文档冲突合并保留来源功能说明与第二本轮未验收边界，AGENTS维持原短执行规则及维护授权。工作树干净，固定`GDK / GameDevelopmentKit-NetTest`和6项技能不变，没有push。

两个Unity6000.3.18f1 Editor按完整projectPath核对：主工程PID59672／工具端口7800，第二PID102004／端口7801。第二启动时使用Orca computer-use处理自己的管理员提示；随后真实Pipeline可达、编译未失败、Error0、Warning2。第二原始日志为`D:/MyWork/CarzyCooker-NetTest/Unity/Logs/coop-loopback-editor-20261013.log`，保留原位。

排列A通过普通Instance Launch／SceneHelper进入`coop-dual-a`：主Host实际KCP endpoint为127.0.0.1:57520，第二Client显式连接。两端Inspect均Passed，world=8d999576a0f1446f8349edef8678be41，level=1284111875440667，成员分别1／2，双方roster=2、库存7，协议17。实际本人Joined、Grid.Confirmed、所属Board／HUD.Ready均满足，Client无authority且无共享控制，三个供应源均实际读取默认40。1.1已完成；此时2.1／2.2完整功能尚未通过，不提前勾选。

### 排列A：主Host／第二Client完整流程2.1

临时验收脚本仅位于主工程Temp，两端通过Pipeline内存编译执行；普通入口、实际HUD Pointer／地图输入和所属消息贯穿全程，不写业务状态、时间、物资或完成结果，没有复制未提交实现到第二工程。

| 功能 | 实际结果／身份与job | 结果 |
| --- | --- | --- |
| Host布局／Buff及番茄交接 | HostPrepare job de8cc2b3b9c94e02ad7b663c3db17881：Buff3，交接台1284111875440650实际移到2,12，切配产物1284112026435733交给独立Client | Passed |
| Client实际蛋液／煮汤 | ClientStartSoup job79cdcb13cb7043039b74cdff91f902d2：原锅1284111875440653，汤批次1284112072573307，冻结耗时60ticks | Passed |
| 实际加工中的暂停恢复 | Host观察实际烘烤开始，job804a4459c56a4439b8d4f95297d3096a：业务tick4849、原烘烤1284112281240098／progress1停300ms不变，再恢复 | Passed |
| 耗尽补货／满槽／拆份／清理 | ClientDishesAndSupply jobb6b165da0bc44090b9c63a4cdc86c1fe：初始面包／吐司真实耗尽，满槽拒绝后收包1284112303260587；原材料1284112303260588制作新吐司，1284112303260589与空壳实际丢弃且额度4；原剩汤批次退休，原锅和盛出汤碗保留 | Passed |
| 真实两菜交付与清桌 | ClientServeAndClear job0c14575ba58746949a1d5d987709ea3b：原汤碗1284111875440647／盘1284111875440660，真实订单交付20分；原餐具实际清桌入池 | Passed |
| 洗净原餐具／自然结算／实物换关 | HostCompleteAndContinue job578587ef4721467ebb13b8e087bc73a2：伙伴真实洗净，自然20分2星，关1284111875440667→1284112570648363，精确4件身份保持，取回原碗并放交接台；第二Client实际确认同一新关／本人／库存4／Buff0 | Passed |

首个HostPauseRestore检查失败：客户端异步job结束后人工观察晚于实际3秒汤耗时，Host已暂停但汤已Ready，不能证明工作中冻结；保留原失败，恢复该实际暂停后，以同步观察下一真实烘烤的新job通过，不改产品代码。采购失联首次jobe760b95c173542fcb7217fcac937456c失败：餐厅已Draining，真实HUD没有新采购按钮，因此没有预留或货物；保留当前原碗手持与运行，失联采购改在自然下一Preparing执行，未绕阶段门槛或重放旧失败结果。两项均为验收脚本阶段／观察问题，完整合作行为依据上表最新适用证据。

### 实际Client失联、Host继续及退出新轮

排列A的自然下一Preparing中，第二Client经实际Pointer拿交接台原汤碗1284111875440647、采购番茄，随后释放自己实际所属Session：job742c96d6e83349eaab8ea1a842fd4d38 Passed，货物1284112651388825已提交。Host并行观察原手持到失联，job1a5ef934dc4747aaa5580ef1825c2928 Passed：offline1，同一原碗落地保留，Host实际收取同一货物，包装1284112653486023；没有重新预留、退款或停止公共sender。

随后仅停止本轮两端Play；两端VerifyStopped均Passed，ProductBootstrapBridge.Root均为空。主Host旧KCP端口57520不再出现在对应PID的UDP端点查询中。两个Editor／长期目录保留。

角色互换经普通入口建立`coop-dual-b`：第二Host实际endpoint127.0.0.1:52526，主Client显式连接；world=cb755cd276b44e379d92f19a75646ac8，level=1284112919822363，与排列A不同。双方Inspect Passed，成员重新为1／2、当前roster2，库存7、总配额15、分数0、Preparing，实际供应默认40，不继承上一轮4件库存／扣减配额／离线成员。旧关／迟到消息拒绝的未变核心UTF证据复用，不注入伪造完成结果。

### 排列B：主Client／第二Host完整流程2.2

| 功能 | 实际结果／身份与job | 结果 |
| --- | --- | --- |
| 第二Host布局／Buff与交接 | HostPrepare jobcc810e352e874b02812016b2b61dc7e1：Buff3、交接台1284112919822346移至2,12，真实切配1284112993222805 | Passed |
| 主Client蛋液／煮汤与第二Host暂停恢复 | ClientStartSoup jobde2c9b09883d476c9030d9d5eff91b8d：锅1284112919822349、批次1284113048797563／required60；并行HostPauseRestore job5c25958eef614df4a3e756965effcda3：tick2447、Cooking原进度1冻300ms后恢复 | Passed |
| 耗尽补货、满槽、拆份、剩液／空壳清理 | ClientDishesAndSupply job586c809a75464257bf9ab0a5ee7fddf7：实际包1284113157850021，原材料1284113157850022制作新吐司、1284113157850023和空壳实际垃圾桶处理，配额4；原锅／盛出汤碗保留，剩汤批次退休 | Passed |
| 实际两菜交付与原餐具清桌 | ClientServeAndClear job55d32a3fd01845a09a49b60d65a3107b：汤碗1284112919822343、原盘1284112919822356真实交付20分，均实际清桌入池 | Passed |
| 伙伴洗净、自然结算与换关 | HostCompleteAndContinue job9097efe2b535410685f7ce939085df1c：自然20分2星，关1284112919822363→1284113405314798，精确4件原实物保留，第二Host取回同一汤碗并放交接台 | Passed |
| 主Client当前新关与本人成员 | ClientInspectNext Passed：同一world／新关1284113405314798、member2、库存4、Confirmed、Buff0；无旧顾客／工作 | Passed |

运行结束时两端Console groundTruth均compilationFailed=false、Error0／Warning0；未清Console，脚本早期两项失败保留上述原job和原因。随后只停止本轮两端Play，VerifyStopped均Passed且产品Root为空；Get-NetUDPEndpoint正常查询确认PID59672的57520和PID102004的52526均已释放。两个Editor、各自缓存／日志、长期worktree及产品身份保留，第二本轮编译与完整合作均以实际结果报告。

全部功能按上表和前置记录逐项覆盖，复用未变的Solo与真实KCP UTF，不再为阶段收尾重跑套件。同步与运行原日志保留原位；本轮没有产品构建、push或归档。

| 可复用验收 | 当前证据 | 集成所需补验 |
| --- | --- | --- |
| 唯一Host餐厅、可信控制、两菜、投影与成员退出 | [核心记录](../cooperative-restaurant-core/execution-notes.md)，7/7；未知0／99及物流开放后的核心UI已新运行通过 | 普通独立Host／Client两种角色排列 |
| 实际交付、伙伴接续、原餐具循环及自然两关 | [前厅记录](../cooperative-front-service/execution-notes.md)，5/5；普通首关20分2星／次关0分0星及同碗取回 | 两Editor全流程中的对应输入与状态 |
| 原物清理、满槽／唯一收货、配额、拆份与断线保货 | [物流记录](../cooperative-cleanup-supply/execution-notes.md)，5/5；15唯一UTF及普通补货交付／在途换关，默认40已实际恢复加载 | 两Editor清理补货及至少一次实际Client失联后Host继续 |

第二工作区已按分支／同仓发现为`D:/MyWork/CarzyCooker-NetTest`，分支`multiplayer-baseline`，当前HEAD `5d55a55778d3f339ca0a8696b5c5be3695cb1c8d`；工作树干净、无merge／rebase／cherry-pick在途。主工作区本地main为`b2848b0df96a675a1a706d8196eafca05b637e83`。当前CLI仅发现主工程Editor，第二Editor尚未启动。本轮只读核对，没有向第二发送执行任务或复制未提交源码／DLL。

必要版本维护候选顺序为`820bc3a2`（格子与受管UI基础）→`a494229e`（备料）→`5c6ffede`（九案餐厅）→本轮待授权的新实现提交；前三项均已核对main可达。基础修复`2864caf2`／异步生命周期`25b568e2`已在第二分别以`bee72ad5`／`61499bb2`带原cherry-pick来源落地，不重复加入。`889957c0`、`3f171eb5`、`7da8480b`、`05d3c79e`和`b2848b0d`仅记录／规划／维护规范，不作为产品运行依赖同步。正式执行前仍核对选定提交完整差异、依赖和最新两端状态，不默认使用整个范围。

本轮提交审阅范围共244个路径：210个实现／生成／行为UTF路径与34个当前change记录路径，分别见主工程`Unity/Temp/CoopLoopbackRuntimeCommitScope.txt`和`Unity/Temp/CoopLoopbackRecordCommitScope.txt`；总清单仍为`Unity/Temp/CoopLoopbackCommitScope.txt`。实现与验收记录分开提交，第二工作区只同步新实现提交，避免引入`05d3c79e`纯规划依赖。用户其他候选提案、本地消息边界规划、历史归档移动、URP／Recovery及无语义生成格式变化保留。实际双Editor任务继续保持NotRun。

### 用户授权与本地实现提交

2026-10-13，用户明确回复“确认授权”，授权本地提交、指定同步和继续双Editor验收，不包含push或归档。210个实现路径已精确核对并提交到本地main：`906829eff5549348cbbe5bc8ade895e173a3168c`，新增9097行／删除138行；手写范围diff检查通过。15个生成／Unity meta路径保留生成器默认尾空白，未手改生成物。验收记录随后独立提交，不扩大第二工作区运行依赖。

同步runId为`coop-loopback-sync-20261013`，目标为同仓`multiplayer-baseline`实际目录`D:/MyWork/CarzyCooker-NetTest`，用途为普通双Editor完整合作；有序来源为`820bc3a2d577531a52d9a73c00b985906f516d1c`→`a494229ebec4b20f1a86c479ec4e45bcc68c9806`→`5c6ffede6adc8dd95814ad7ce71d9a51ef40b466`→`906829eff5549348cbbe5bc8ade895e173a3168c`。保留第二当前维护授权、产品身份与六项技能，先检查完整基线与编译，再进入已授权普通双Editor两种角色排列。历史运行证据可以复用，不重跑全部UTF。

### 隔离索引同步预演

2026-10-13，以第二当前HEAD初始化自己持有的临时Git索引，完整依次应用上述三个历史commit补丁，再应用210个待提交实现路径的完整补丁；没有改动两端实际索引、文件、分支或创建提交。历史`820bc3a2`仅五处文档冲突：Book/Unity启动与验证SOP、Book/快速开始、当前进度、openspec/README、references/reactive-and-network；`a494229e`仅AGENTS冲突，`5c6ffede`无冲突。仅为验证后续补丁适配，在临时索引中选择来源commit文档；这不决定实际第二工作区文档合并结果。随后210路径实现补丁apply退出0、无未合并条目，源码／表／协议依赖可适配。临时索引已删除。

第二工作区当前AGENTS／同步技能保留用户后来授权的维护规则：采用main已有实现、源码冲突优先main、文档自主合并，并保持固定产品身份、执行职责和6项技能。该规则与主工作区旧模板不同，实际维护不能覆盖成旧审批模板或恢复开发职责；预演未修改任何维护规范。此规则允许其已授权版本维护，不自动授权主工作区提交本轮未提交实现。

## 初次拆分与授权（历史，2026-10-13）

用户在分别核对单机／联机进度后明确要求创建缺失的前置联机 change。本轮新增核心、前厅、清理供应三案，并将本案收敛为原承诺的完整双 Editor 合作验收。只有规划调整，三案分别 0/7、0/5、0/5，本案 0/3，全部功能 NotRun。没有业务代码、Unity、协议生成、提交或第二工作区执行。

### 原任务迁移账本

| 原本案任务 | 新归属 | 保留的行为与验收 |
| --- | --- | --- |
| 1.1 Host／owner／驱动 | 核心 1.1 | 正式餐厅、唯一驱动、Solo／Client 边界 |
| 1.2 共享控制 | 核心 1.3 | Host 本地可信关系、全部控制权限与拒绝零改动 |
| 2.1 完整请求 | 核心 1.2／2.1；前厅 1.1；物流 1.1／1.2 | 两菜、前厅、设施及供应的外部请求，原等待与内部隔离 |
| 2.2 完整投影／HUD | 核心 2.2；前厅 1.2／2.2；物流 2.2 | 同版独立数据、本人就绪、UI、旧关高版本与换关 |
| 2.3 争抢／退出 | 核心 2.1／2.3；前厅 2.1；物流 2.1／2.2 | 末份、物品、工作、货物单提交与个人／整体退出 |
| 3.1 KCP 小集／普通入口 | 三案各任务运行与 3.1；本案 1.1 | 局部行为及时验证，集成复用未变证据并补具体缺口 |
| 3.2 双 Editor 两种角色 | 本案 2.1／2.2 | 普通双进程完整流程及职责／运行角色分离 |

原 product-session 两项增量迁入核心案；新基础入口要求按已交付动作族开放，不再要求核心阶段同时完成物流。原 cooperative-restaurant 的详细开发合同分入三案规格，本案保留完整流程、控制／退出组合及交付范围。没有把移动文件视为功能完成。

### 初次拆分的严谨程度与验收（历史）

主要风险是局部 KCP 通过但普通独立进程无法完成合作，以及两工作区版本或角色错配。复用三案未变的最小 UTF 和同 Editor 证据，再执行原本已经要求的双 Editor 两种排列即可；不重跑全套历史回归，不构建新验收平台。接受独立 Player／LAN／公网、联机存档／装修／重连未验证，后续相关能力改变时再覆盖。

| 当前任务／功能 | 实际运行要求 | 当前结果 |
| --- | --- | --- |
| 1.1 适用证据与普通入口完整就绪 | 三案逐功能记录＋普通 Host／Client 入口 | NotRun |
| 2.1 主 Host／第二 Client 全流程 | 两 Editor、两菜／交付／清理／补货／暂停／回收／换关 | NotRun |
| 2.2 主 Client／第二 Host 及退出边界 | 角色互换全流程；实际连接失联、Host 继续与退出新轮 | NotRun |

### 本轮规划检查

2026-10-13：核心、前厅、清理供应与本案四次 `openspec validate <name> --strict` 均退出 0、valid。25 份 Markdown 的 UTF-8、尾空白和 164 个本地引用检查通过；所改跟踪文档的 `git diff --check` 无错误，迁走的旧 product-session 路径无剩余引用。三案规划齐备但未实施，本案仍 0/3、NotRun。

以下 2026-10-12 内容保留原规划历史；旧任务号与验收表不再作为当前待执行清单。

## Planning status

2026-10-12 用户授权先写五个 change；本轮只创建规划，未实施、未导表、未编译，Unity/UTF/包体均 NotRun。以现有 Solo 九案按契约交付为规划假设；apply 前核对前置的逐功能运行证据，不修改或替其它工作勾任务。

## Rigor and scope

主要风险是 Host 双驱动、共享控制越权、遗漏投影以及断线错误收尾，影响两端共同事实。以真实 KCP的小型行为 UTF 和双 Editor 两种角色排列中的完整制作/交付/暂停/换关覆盖；无新变化的 Solo 加工断言复用，不重跑全仓。接受仅 loopback、无原成员凭证恢复、无 LAN/公网/Player；第二工作区前置缺失就保持其验收 NotRun，不能降格为同进程通过。

## Acceptance mapping

以下名称是拟补用例范围，不是已经发现或运行的用例；实施时发现真实 PlayMode 过滤并记录原始结果。结果留现有工具原位，不复制证据包。

| 任务 | 功能 | 对应运行 | 结果 |
| --- | --- | --- | --- |
| 1.1–1.2 | 唯一驱动/Host 共享权限 | 拟 CoopRestaurantOwnership/Permissions | NotRun |
| 2.1–2.3 | 完整 wire/设施接水排液与固体倾倒/投影/争抢/退出 | 拟真实 KCP CoopRestaurantWire/Projection/Exit | NotRun |
| 3.1 | 当前普通双端入口 | 实际 Host/Client + 相关小集 | NotRun |
| 3.2 | 双 Editor 两种角色及完整合作 | 真实两工作区按明确交接执行 | NotRun |

## Planning validation

2026-10-12 设施清理规则交接：液体到水池、固体/空包装到垃圾桶，必须到真实四邻操作格；同步本案的相关规格、设计及验收任务。`openspec validate cooperative-restaurant-loopback --strict` 再次valid、退出码0，三案与两份产品目标共18份Markdown引用/UTF-8/空白检查通过。仅规划更新，新增设施行为仍NotRun。

2026-10-12：`openspec validate cooperative-restaurant-loopback --strict` 退出码 0，返回 valid；proposal/specs/design/tasks 齐全。本轮五案共 26 份 Markdown 的 UTF-8、行尾空白、本地引用及任务未勾选检查通过。上述检查仅证明规划格式/引用有效，功能运行仍全部 NotRun。
