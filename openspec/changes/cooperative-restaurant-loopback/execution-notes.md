# Execution Notes

## 当前集成准备（2026-10-13）

三个开发前置现为核心7/7、前厅5/5、清理供应5/5；原案仍0/3。前置普通Host＋独立Fiber真实KCP结果可以复用，但当前普通独立Client入口和完整双Editor尚未执行，1.1不提前勾选。当前协议17，内部Submit／伙伴请求仍不对外开放；没有新增传输／监听入口。

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

### 当前严谨程度与验收

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
