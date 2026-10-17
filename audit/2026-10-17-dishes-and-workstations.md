# 菜品与工作台审查（2026-10-17）

审查以当前未提交工作区为准，未覆盖别的 worktree。静态审查与运行验收分开，实际修改及结果见 [change](../openspec/changes/archive/2026-10-17-fix-dish-workstation-contracts/execution-notes.md)。

## 结论与范围

代码具备可以继续扩展的主体：实物和过程归同一厨房，手工／自动／烘烤共享 Tick，前厅只接收真实完成菜品，客户端是独立投影。当前首要问题是功能扩展后个别消费者没有同步载具合同，以及初始化验证未完全匹配运行选择规则；没有依据要求更换 ET 架构或重写制作系统。

读取了菜品／工作台／加工／批次主规格、进度和已知取舍，核对实际生成定义及 GridDishConfig、GridPrepSystem、GridSoupSystem、GridBakeSystem、GridAssemblySystem、GridDessertSystem、GridBeverageSystem、GridProductionPermissions、GridStationConfig/LayoutSystem、GridServeSystem、GridSaveRestore、消息及相邻 UTF。以下是定向业务审查，不声明全仓逐行审计或全平台验证。

## 已闭合业务

| 菜品 | 实际链路 | 工作台与交付边界 |
|---|---|---|
| 番茄蛋花汤 | 切番茄、手持碗打蛋、倒蛋液、锅内批次、有限分装 | 切配／灶台，原碗及锅保留 |
| 盘装吐司 | 面包烘烤新身份、同一吐司入盘 | 烤箱，普通暂存不加分 |
| 鲜榨橙汁 | 切橙、榨汁锁壶、逐杯分装 | 切配／榨汁／贴单，实际订单绑定 |
| 田园／芝士蔬菜沙拉 | 多路切配、精确份数组装 | 共用切配和盘，显式选择配方 |
| 经典牛肉汉堡／香肠鸡蛋早餐 | 实物煎制、熟料／吐司合流 | 煎台／烤箱，错误组合不消费 |
| 原味小蛋糕 | 原料搅拌、有限面糊、装模、烘烤、脱模 | 搅拌／烤箱，模具不是出餐容器 |
| 浓缩／热拿铁／卡布奇诺 | 研磨、萃取、热奶／打泡、按份调饮 | 磨豆／咖啡／蒸汽／贴单，奶泡最后加入 |

数据来源：[十一菜](../Unity/Assets/Res/Editor/ET/Luban/dtdishconfig.json)、[工位定义](../Unity/Assets/Res/Editor/ET/Luban/dtstationconfig.json)。Editor JSON 只用于静态阅读；运行 UTF 加载当前模式真实 bytes。菜品供应、菜单许可、订单、洗碗、关间持久厨房和存档恢复均已有消费者与行为测试，不把这些连接只存在于文档中。

## 发现与处理

| 优先顺序 | 发现／依据 | 后果与处理 |
|---|---|---|
| 1 | GridStationLayoutSystem 以 bake.InputItemId == station.ItemId 判断所有烘烤 | 蛋糕内部面糊与外层模具必然不同，合法布局误拒绝；本 change 修正并覆盖真实制作／移动／脱模 |
| 2 | StartWork 按 Mode+Input 取 FirstOrDefault；StartBake 和 StartAutoWork 要求唯一匹配；IdentifyDish 取首条，ValidateMenu 未验证这些条件唯一 | 新增表行可能通过初始化后实际无法制作或识别错误；本 change 在厨房建立前报告冲突编号 |
| 3 | 已完成 Manual/Bake 记录主要随清理或整个厨房销毁释放，实物交付后仍可能保留历史记录 | 持续厨房的集合和快照成本随制作次数增加；当前有限供应和有界关链限制规模，本轮接受，出现长局明显卡顿或引入无限供应时再做生命周期清理，不能直接删过程破坏完成通知 |
| 4 | 菜品／自动容器有具名分支；新增加工类型需要同步配置验证、动作、提示、保存等消费者 | 当前规模可维护，但不是任意配表即能扩展的通用工厂；新行为须走 change，不以整理为由创建新业务框架 |

健康方面：Model 数据／Hotfix 裁决边界清楚，先提交再快照和终态通知，菜单缩小保留原物和已开始作业，普通格单物与容器内容守恒可追踪。局部 System partial 规模和紧凑写法增加阅读成本；只有具体修改处清晰化，不进行与任务无关的格式重写。

## 业务缺角与已有规划的边界

- 厨房加工及待取状态缺统一看板，当前工作区已有 `kitchen-production-workboard` 提案，任务仍未完成。
- 菜谱缺目录检索／设备筛选，当前工作区已有 `recipe-catalog-navigation` 提案。
- 布局调整目前直接提交，缺候选预览／确认／取消，当前工作区已有 `restaurant-layout-edit-workflow` 提案。本轮修裁决，不冒充交付该界面。
- 盛装／容器／成品交付等只读说明尚有 `container-operation-preview`、`finished-dish-delivery-inspector` 等已建规划；不重复立案。这些未实施提案保留在工作区，未随本次修复提交。
- 首批十一菜不等于全部候选菜和设备；真实 Android、独立 Player/AOT 和物理网络范围查 [KnownIssues](../KnownIssues.md)，本案不升级其验收结论。

## 验证定位

本次12个不同相关UTF的最新适用结果Passed：10核心＋1直接烘烤布局回归＋1普通Launcher UI；包含配置冲突启动拒绝、合法十一菜、带模具布局、本地／真实KCP、原入口采购到脱模。最初问题复现和测试操作修正均记在 change 执行记录。旧菜全链历史证据见 [dish-recipes 主规格 Sources](../openspec/specs/dish-recipes/spec.md)，不把历史证据冒充本轮重跑。
