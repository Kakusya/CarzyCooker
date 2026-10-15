# 从当前餐厅到完整版：业务差距与 change 优先级

2026-10-15 当前工作区业务扫描。用户确认：**以已确认产品目标为基准，菜品分批扩展**，随后明确要求完成全部 16 个 change。**不考虑真机实测**，PC LAN、Android、Player/AOT、物理网络和包体验收均不成为本轮依赖。当前已完成 00 容器取物、01 模式与房间入口、02 合作三关终点、03 合作工位升级、04 离线协议文档、05 合作基线读取、06 合作结果领取、07 授权导入/替换、08 多输入组装/两沙拉、09 持久解锁/逐关许可、10 有限空间/单客桌扩建、11 两个递进内容章、12 首批热餐及13 小蛋糕/模具链、14 三款咖啡和15 小麦的有限陪伴；16案已全部完成并归档，运行事实以各案 execution-notes 为准。

本页替代旧 [change-roadmap.md](change-roadmap.md) 作为当前业务规划入口；旧路线保留原阶段交付与设备案出处。现状事实继续只在[当前进度](../../CarzyCooker当前进度.md)汇总，运行证据查原 change。本页不把任务数量换算成游戏完成百分比。

## 判断

已经具备一个可连续试玩的餐厅：汤/吐司/橙汁、两沙拉、汉堡/早餐和原味小蛋糕，两个各三关内容章、游戏内模式/房间、顾客/餐具/伙伴、有限供应、装修/升级、持久解锁/有限扩建、成功检查点及合作基线/领取/授权导入与替换。三款咖啡及共享基底/顺序调饮、小麦的有限陪伴反馈均已交付；新开的第二内容章已纳入热餐、甜品及咖啡，旧档关链保留。

完整版业务基准采用 [long-term-goals](../../long-term-goals.md)、[产品设计](design.md) 和[八领域目标](specs/README.md)。87 菜/60 状态/72 物料/19 功能工位仍为候选；当前 16 类 StationKind 还包含台面、餐桌和餐具池，不能直接按两类目录的数量相减计算设备缺口。

## 本轮交付与验收

16案任务全部完成，离线授权协议案只交付文档；其余功能均按各案最小相关UTF与普通入口验收，原实现和行为未变的结果按记录复用。咖啡批次23个不同用例、伙伴有限陪伴10个不同用例最新Passed；未将重复运行累计为新增覆盖。16案严格格式与48份主规格检查通过，本地主文档引用另经检查。

当前交付包含两沙拉、汉堡／早餐、原味小蛋糕及三款咖啡，两个各三关内容章、一块有限扩建与普通单客桌、合作基线／结算领取／离线分支导入和有损替换，以及一位小麦的三类准备态聊天和工作反馈。87菜／19类候选工位、更多伙伴与其他长期方向仍按后续具体范围规划。设备实测、物理LAN、Player/AOT和产品构建未运行；16案已于2026-10-15归档，本次提交与推送由用户明确授权。

## 创建时的差距扫描（历史）

下表保留最初创建这批 change 时的扫描，不代表 00–05 交付后的现状。入口、合作终点、原工位升级、容器取出与合作基线已交付；许可缩小、有限内容及伙伴范围也已裁决。当前实施状态以顶部摘要和各案 execution-notes 为准，剩余交付见下文。

| 业务领域 | 已有事实 | 距离目标的缺口 | 代码/配置依据 |
| --- | --- | --- | --- |
| 游戏入口/房间 | Solo 有选档；Host/Client 由启动角色直接进入 | 没有普通玩家游戏内模式选择；Host Join 前已建餐厅，不能先集合再选运行、结束回原房间 | [Bootstrap](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs)、[Root 生命周期](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeSystem.cs) |
| 合作终点 | Solo 201→202→203 有终关；合作当前 3→4→4 | 合作仍循环，FinishChapter 只接受 Solo；缺一致结果和回房间 | [LevelConfig 源](../../Design/Excel/ET/Datas/Product/LevelConfig.xlsx)、[餐厅控制](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs) |
| 原工位升级 | Solo 单级升级与冻结耗时已交付 | 合作 RewardKind=2 被过滤；只需补原候选/权限/投影链 | [装修系统](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs)、[升级规则](../../openspec/specs/station-upgrades/spec.md) |
| 容器基础取出 | 包装能拆份，普通容器可整体拿放 | 缺少普通容器内指定未加工固体取出；包装取料明确要求 SupplyConfig 来源，不能当通用取出 | [动作集合](../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)、[包装专用取出](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSupplySystem.cs) |
| 长期合作进度 | PC Solo 本机新建检查点；内存成员恢复 | 合作不能选持久基线；没有 Profile 资格、共同结果领取、授权导入/覆盖或关闭认证 | [保存初始化](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs)、[文件仓库](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveFile.cs)、[目标合同](specs/save-progress-claims/spec.md) |
| 解锁/许可 | 当前关有允许菜单和装修记录 | 没有独立定义解锁集合；许可缩小 UX/操作仍未裁决 | [装修状态](../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/RestaurantRenovation.cs)、[待裁决](backlog.md) |
| 空间/餐桌成长 | 可移动/旋转工位、安装有限新增台面 | 本轮有限9×18及单客桌已交付，具体范围见扩建记录 | [GridKitchenRules](../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)、[前厅布局校验](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridFrontLayout.cs) |
| 制作宽度 | 实际 DishConfig 仅汤、吐司、橙汁三条 | 缺多组件组装、煎制熟料、模具/脱模、咖啡基底与顺序收尾 | [菜谱闭包](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs)、[备料](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs)、[烘焙](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs) |
| 章节与节奏 | 首个大关三关，但菜单/营业参数基本相同 | 缺有限后续章节、递进菜单、设备/客流组合与动态总关数 | [关表](../../Design/Excel/ET/Datas/Product/LevelConfig.xlsx)、[HUD](../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs) |
| 固定伙伴陪伴 | 一位持续 Companion，实际询问/洗碗与关内成长 | 名称仍“伙伴”，缺准备态聊天及适度工作正面反馈；最终人数未定 | [CompanionConfig](../../Design/Excel/ET/Datas/Product/CompanionConfig.xlsx)、[身份/角色](../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/FrontService.cs) |

以上为静态业务差距扫描，结合原交付结果判断已有什么，不是全仓逐行审计或本轮运行验收。已读取全部八份目标规格、相关主规格、现行产品设计/待办、核心建树/控制/存档/加工/展示源码及实际导出配置，测试文件用于核对已有验证入口。

## 按优先级建立的 change

P0 先把已交付体验收成完整可操作的一轮，P1 补长期进度与经营成长，P2 用有限内容包扩大分工和复玩。编号是建议推进顺序；无硬依赖的案可独立选做，协议讨论也可提前进行。

| 顺序 | 优先级 | Change | 可观察交付 | 真正前置 |
| --- | --- | --- | --- | --- |
| 00 | P0 | [kitchen-container-extraction](../../openspec/changes/archive/2026-10-15-kitchen-container-extraction/proposal.md) | 普通容器取出指定未加工固体 | 已有交付，无新案硬前置 |
| 01 | P0 | [product-mode-room-flow](../../openspec/changes/archive/2026-10-15-product-mode-room-flow/proposal.md) | 游戏内模式选择与合作房间 | 已有交付，无新案硬前置 |
| 02 | P0 | [cooperative-chapter-completion](../../openspec/changes/archive/2026-10-15-cooperative-chapter-completion/proposal.md) | 合作大关终点与返回房间 | [product-mode-room-flow](../../openspec/changes/archive/2026-10-15-product-mode-room-flow/proposal.md) |
| 03 | P0 | [cooperative-station-upgrades](../../openspec/changes/archive/2026-10-15-cooperative-station-upgrades/proposal.md) | 合作原工位升级 | 已有交付，无新案硬前置 |
| 04 | P1 | [progress-authorization-contracts](../../openspec/changes/archive/2026-10-15-progress-authorization-contracts/proposal.md) | 共同进度授权与关闭协议定稿 | 已有交付，无新案硬前置 |
| 05 | P1 | [cooperative-save-baseline](../../openspec/changes/archive/2026-10-15-cooperative-save-baseline/proposal.md) | 合作选档与授权基线载入 | [product-mode-room-flow](../../openspec/changes/archive/2026-10-15-product-mode-room-flow/proposal.md)、[progress-authorization-contracts](../../openspec/changes/archive/2026-10-15-progress-authorization-contracts/proposal.md) |
| 06 | P1 | [cooperative-result-claims](../../openspec/changes/archive/2026-10-15-cooperative-result-claims/proposal.md) | 共同成功结果与主动领取进度 | [cooperative-chapter-completion](../../openspec/changes/archive/2026-10-15-cooperative-chapter-completion/proposal.md)、[progress-authorization-contracts](../../openspec/changes/archive/2026-10-15-progress-authorization-contracts/proposal.md)、[cooperative-save-baseline](../../openspec/changes/archive/2026-10-15-cooperative-save-baseline/proposal.md) |
| 07 | P1 | [save-branch-import-and-replace](../../openspec/changes/archive/2026-10-15-save-branch-import-and-replace/proposal.md) | 授权分支导入与显式覆盖 | [progress-authorization-contracts](../../openspec/changes/archive/2026-10-15-progress-authorization-contracts/proposal.md)、[cooperative-save-baseline](../../openspec/changes/archive/2026-10-15-cooperative-save-baseline/proposal.md)、[cooperative-result-claims](../../openspec/changes/archive/2026-10-15-cooperative-result-claims/proposal.md) |
| 08 | P1 | [kitchen-multi-input-assembly](../../openspec/changes/archive/2026-10-15-kitchen-multi-input-assembly/proposal.md) | 首批多材料组装与共享备料 | 已有交付，无新案硬前置 |
| 09 | P1 | [restaurant-unlock-permissions](../../openspec/changes/archive/2026-10-15-restaurant-unlock-permissions/proposal.md) | 持久解锁与本关许可 | 已有交付，无新案硬前置 |
| 10 | P1 | [restaurant-space-expansion](../../openspec/changes/archive/2026-10-15-restaurant-space-expansion/proposal.md) | 餐厅空间与餐桌扩建 | [restaurant-unlock-permissions](../../openspec/changes/archive/2026-10-15-restaurant-unlock-permissions/proposal.md) |
| 11 | P2 | [restaurant-chapter-content](../../openspec/changes/archive/2026-10-15-restaurant-chapter-content/proposal.md) | 递进经营关卡与有限内容包 | [cooperative-chapter-completion](../../openspec/changes/archive/2026-10-15-cooperative-chapter-completion/proposal.md)、[restaurant-unlock-permissions](../../openspec/changes/archive/2026-10-15-restaurant-unlock-permissions/proposal.md) |
| 12 | P2 | [kitchen-hot-food-batch](../../openspec/changes/archive/2026-10-15-kitchen-hot-food-batch/proposal.md) | 首批煎制与组合热餐 | [kitchen-multi-input-assembly](../../openspec/changes/archive/2026-10-15-kitchen-multi-input-assembly/proposal.md) |
| 13 | P2 | [kitchen-dessert-batch](../../openspec/changes/archive/2026-10-15-kitchen-dessert-batch/proposal.md) | 首批面糊甜品与模具出品 | [kitchen-multi-input-assembly](../../openspec/changes/archive/2026-10-15-kitchen-multi-input-assembly/proposal.md) |
| 14 | P2 | [kitchen-beverage-batch](../../openspec/changes/archive/2026-10-15-kitchen-beverage-batch/proposal.md) | 首批咖啡基底与顺序调饮 | [kitchen-multi-input-assembly](../../openspec/changes/archive/2026-10-15-kitchen-multi-input-assembly/proposal.md) |
| 15 | P2 | [fixed-companion-presence](../../openspec/changes/archive/2026-10-15-fixed-companion-presence/proposal.md) | 固定伙伴身份与关间陪伴 | 已有交付，无新案硬前置 |

16 案均含 proposal、design、tasks、execution-notes；协议文档案显式 skip_specs。当前 00–15 全部完成，共66/66任务；逐项运行依据见各案execution-notes。产物齐备只代表规划齐备，运行验收须逐项交付。

## 依赖与可独立推进

```text
product-mode-room-flow -> cooperative-chapter-completion
product-mode-room-flow + progress-authorization-contracts -> cooperative-save-baseline
cooperative-chapter-completion + cooperative-save-baseline
  + progress-authorization-contracts -> cooperative-result-claims
progress-authorization-contracts + cooperative-save-baseline
  + cooperative-result-claims -> save-branch-import-and-replace

cooperative-station-upgrades             (independent local parity)
kitchen-container-extraction             (independent basic operation)
kitchen-multi-input-assembly -> kitchen-hot-food-batch
kitchen-multi-input-assembly -> kitchen-dessert-batch
kitchen-multi-input-assembly -> kitchen-beverage-batch
restaurant-unlock-permissions -> restaurant-space-expansion
restaurant-unlock-permissions + cooperative-chapter-completion
  -> restaurant-chapter-content
fixed-companion-presence                 (independent small content)
```

章节内容可以先编排现有三款产品，不强依赖所有新菜批次。持久进度线与制作内容线不互相强绑；在同一个 ProductWire/载荷/表或 UI 文件交叉修改时按实际合同依次交接，协议和文件版本不能各案抢占同一个“下一版”。

经营/内容案先交付 **Solo 成功档 + 合作内存事实**；合作持久保存接入后才扩大验收，不以共享载荷或共用代码宣称远端已保存。早于领取案完成的持久字段由领取案集成验证；晚于领取案完成的字段由对应内容案补已有合作保存入口的一次往返，复用未变结果。扩建在解锁案后执行，涉及同一装修 Requirement 的 delta 已按此顺序累积，避免后案撤回前案解锁合同。

已核实两项具体技术交接：00 案已将 Grid 动作的饮品范围判断改成明确集合，后续新增动作沿显式集合维护；新章节/内容也不能只增加菜表，创建时 GridPrep/RestaurantConfig 只接受 MenuVariant=0/1，现已由08扩展必要消费者至沙拉批次2，Grid 初始布局、Supply、Drink 贴单和 SoloSaveValidation 都消费该字段。首个新增内容的组装案负责必要菜单消费者，解锁案解除同章菜单必须相同的校验，章节案消费这些合同；保留旧 0/1 存档语义，不搭第二份目录系统。

章节身份另须区分：当前 ChapterId=1/2 对应 Solo/合作，合作 301→302→303 与 401→402→403 是原模式/菜单链，不能算成两个可选内容大关。章节案交付每种模式两个可选大关、每关三小关，复用已有 ChapterLevelCount。

## 推荐有限内容批次

| 批次 | 推荐内容 | 新增的有效分工 | 当前身份 |
| --- | --- | --- | --- |
| 组装 | F01 田园沙拉、F02 芝士蔬菜沙拉 | 多人切配、共享材料、精确份数合流 | 2026-10-15 用户确认首批范围 |
| 热餐 | F11 经典牛肉汉堡、F43 香肠鸡蛋早餐 | 煎制熟料与既有吐司/组装并行 | 2026-10-15 用户确认首批范围 |
| 甜品 | S04 原味小蛋糕 | 面糊批次、模具、烘烤、脱模 | 2026-10-15 用户确认首批范围 |
| 饮品 | D15 浓缩咖啡、D17 热拿铁、D18 卡布奇诺 | 共享咖啡/热奶、模式区别、最后奶泡 | 2026-10-15 用户确认首批范围 |

其余蒸/炸/烤架、茶/珍珠/冰沙、更多甜品/顾客/伙伴等仍为后续内容候选，按实际选定批次再建案。本轮不把所有目录项推成首发承诺，也不把烧焦、火灾、工资、疲劳、复杂经济、公网或主机迁移加入已确认完整版基础。

## 必要决定与可实施程度

以下记录提案阶段的决策门槛；2026-10-15用户已逐项确认范围，当前决定列为实施依据。技术细节按现有系统和可逆配置收敛，不重复询问已确认范围。

| Change | 已确认实施边界 |
| --- | --- |
| [cooperative-chapter-completion](../../openspec/changes/archive/2026-10-15-cooperative-chapter-completion/design.md) | 合作首个大关三关。 |
| [progress-authorization-contracts](../../openspec/changes/archive/2026-10-15-progress-authorization-contracts/design.md) | 接受同平台离线文件授权；协议正文已定稿，运行功能不由文档通过代替。 |
| [cooperative-save-baseline](../../openspec/changes/archive/2026-10-15-cooperative-save-baseline/design.md) | 按已定稿离线协议证明Profile和非Owner基线读取授权。 |
| [cooperative-result-claims](../../openspec/changes/archive/2026-10-15-cooperative-result-claims/design.md) | 按离线协议实现资格冻结、可验证凭证、关闭回执与Host提交；Resume不作为长期身份。 |
| [save-branch-import-and-replace](../../openspec/changes/archive/2026-10-15-save-branch-import-and-replace/design.md) | 离线授权只可本机拒绝未使用文件，不能远程撤回已导入分支；替换明确目标并保护旧档。 |
| [kitchen-multi-input-assembly](../../openspec/changes/archive/2026-10-15-kitchen-multi-input-assembly/design.md) | 首批F01田园沙拉/F02芝士蔬菜沙拉，按原候选配方收敛材料/份数/工位。 |
| [restaurant-unlock-permissions](../../openspec/changes/archive/2026-10-15-restaurant-unlock-permissions/design.md) | 保留旧物/在途/已开始作业，禁止受限菜品新采购、投料、启动与新盛装；已完成批次也禁止新盛装。 |
| [restaurant-space-expansion](../../openspec/changes/archive/2026-10-15-restaurant-space-expansion/design.md) | 一块有限区域加一张普通单客桌，不扩大最终餐厅规模承诺。 |
| [restaurant-chapter-content](../../openspec/changes/archive/2026-10-15-restaurant-chapter-content/design.md) | 先做两个大关、每大关三小关，菜单与客流逐步递进。 |
| [kitchen-hot-food-batch](../../openspec/changes/archive/2026-10-15-kitchen-hot-food-batch/design.md) | F11经典牛肉汉堡/F43香肠鸡蛋早餐，按候选配方；耗时和供给量为可调首包参数。 |
| [kitchen-dessert-batch](../../openspec/changes/archive/2026-10-15-kitchen-dessert-batch/design.md) | S04原味小蛋糕，面糊、模具、烘烤、脱模到甜品盘；不扩全部甜品。 |
| [kitchen-beverage-batch](../../openspec/changes/archive/2026-10-15-kitchen-beverage-batch/design.md) | D15浓缩/D17热拿铁/D18卡布奇诺，采用已提出首批配方和最后奶泡规则。 |
| [fixed-companion-presence](../../openspec/changes/archive/2026-10-15-fixed-companion-presence/design.md) | 保留一位伙伴“小麦”，准备态备菜/合作/收店聊天，真实工作后简短正面反馈。 |

2026-10-15 后续明确答复覆盖本表相应历史门槛：合作首个大关三关；上述八款首批菜品；同平台离线文件授权及其不可远程撤回/跨设备非全局一次边界；菜单缩小沿用原规则，保留旧物/在途/已开始作业，禁止受限菜品新采购、投料、启动及新盛装。离线协议、合作基线读取及共同结果领取已交付，ImportBranch 与显式替换也已交付。另确认一块有限扩建区加一张单客桌、两个大关各三小关、现有伙伴“小麦”的准备聊天与工作反馈。其他内容细节在对应案结合现有目录与可观察界面细化，不自动扩大首批菜品。

## 验收和投入取舍

- 新行为用最小相关 PlayMode UTF，经真实本地/KCP 请求、领域提交和终态断言覆盖；UI/输入/流程补普通 Editor 入口。
- 只读文档检查正文、引用、编码和 OpenSpec；不为协议讨论添加占位 UTF。
- 文件权限/覆盖用隔离临时 repository 的实际文件验证，覆盖前旧档保护和失败保资格比普通内容案更严格；不触碰用户现有记录。
- 已有行为未变且结果适用时引用原 execution-notes；不因后续提案、审查或归档机械重跑。
- 规划阶段的 NotRun 记录保留在末节；当前实施按各案补充真实结果。真机、Player/AOT、物理 LAN/公网和真人体验研究不作为此批实施门槛。

## 资料差异与本轮修正

本节保留创建这批提案时的修正记录；其中协议 21/22 和交付摘要属于当时版本。本次规划优化按实施现状更新各案交接，不把历史版本当作下一案固定版本。

扫描发现[当前进度](../../CarzyCooker当前进度.md)保留了已完成三关/升级/橙汁仍“尚未完成”的旧行；部分早期环境表未包含后来的装修/恢复/饮品增量，且协议摘要仍为 21，源码为 22。本轮仅校正这些动态摘要，保留原历史证据和用户未提交的两份主规格。

目标设计/architecture 的“当前样机”树是历史阶段，并不能当作现在没有 RestaurantRuntime/存档/恢复的事实。本轮在入口加时间边界说明，不重写已定稿目标或代定尚未裁决的产品规则。

## 本轮规划检查

以下为创建时的历史检查结果；当前16案66/66任务交付见本文“本轮交付与验收”，不把原未勾选数量作为当前进度。

以下两段是最初创建 16 案时的历史检查，数量和“原案无变更”仅对当时成立。本次优化结果见下一节。

16 案均通过 `openspec validate <change> --strict --no-interactive`。111 份相关规划/入口文件的 UTF-8、653 个本地引用和空白检查通过；16 节点/15 依赖边无环，7 个修改 Requirement 块保留全部原场景标题，扩建 delta 保留前置解锁合同。25 份 delta 共 51 项要求、55 个场景；64 项任务全未勾选，16 案 schema 规划产物齐备。

原 PC LAN、Android 两案无变更；业务源码、Excel/Proto 源和生成物无变更。保留用户原有未提交的 grid-kitchen、runtime-instances 主规格及截图。功能、UTF、Editor、构建均 NotRun；未提交、push 或归档。此处通过只表示规划检查，待裁决门槛及实际功能验收仍按各案执行。

## 未开工规划优化（2026-10-15）

本次更新扫描时尚未开工的 12 案：表中 06–15 共 10 个业务案，以及暂缓的 [PC LAN](../../openspec/changes/pc-cooperative-lan-trial/proposal.md)、[Android](../../openspec/changes/android-solo-playable/proposal.md)。已完成的 00–05 作为前置证据引用，不重复规划或重跑。

取消过时的产品确认门槛，落实已确认菜品配方、单区域扩建和伙伴内容；按实际章节身份、菜单闭包和共享字段明确交接。领取与替换按目标及回执同次发布，关闭整个 Match 与返回原房间区分。每项任务对应可观察的最小验收；设备案拆分构建与设备步骤，继续暂缓。

10 个业务案共 42 项、2 个设备案共 12 项任务，均未勾选。当前仅优化规划，业务实现、改表/导表、UTF、Editor、Player、构建及设备验证均 NotRun；未提交、push 或归档。

12 案均通过 `openspec validate <change> --strict --no-interactive`。70 份规划/入口 Markdown 的 UTF-8、尾随空白和本地引用检查通过；54 项任务与验收映射一一对应且全部未勾选。6 个 MODIFIED Requirement 保留对应主规格的全部 10 个原场景；扩建仍保留前置解锁场景。按 design 明确前置检查的 16 个节点、13 条依赖边无环。此次通过仅表示规划可交接，运行功能仍 NotRun。
