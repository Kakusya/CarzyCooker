# Proposal

## Why

当前地图每帧复制集合、拼接标签和申请视口数组；失联界面每帧重建按钮，HUD 同一投影可能重复刷新。固定业务步也重复排序和申请寻路缓冲，形成可以局部消除的持续分配；依据见[逐帧审查](../../../../audit/2026-10-14-per-frame-performance.md)。

## What Changes

- 地图沿原 Board owner 复用遍历／增删缓冲，按投影及高亮变化更新静态内容和标签；移动插值保持逐帧执行。
- HUD view 复用视口四角数组，统一正常 HUD 刷新入口，以字段比较替代每次构造长字符串键；恢复页面只在显示上下文变化时重建。
- 固定业务步复用有序遍历缓冲，容量统计直接遍历；厨房复用 BFS 工作区，消除每个展开节点的四邻数组和无必要完整路线构造。
- 对上述改动补最小相关行为 UTF、真实 Launcher/HUD 检查和一次代表性性能前后对照，逐项记录结果。

本案是保持既有玩法及外部合同的性能重构。保留 50ms Tick、MemberId／工作 ID 顺序、四邻顺序、满员等待、暂停、权限、消息独立值和退出安全。

排期见 [tasks.md](tasks.md#批次与优先级)：先做 P1 的地图／视口和恢复／正常 HUD，再做 P2 的固定步／寻路，基线与整体验收为必需步骤。用户暂时不考虑真机测试，当前批次聚焦 Editor、UTF 和实际 Launcher/HUD，不扩大本案设备范围。

范围外：全量快照／投影安装复用、消息池化、本地 sender 批次重构、协议／Excel／Prefab／CodeBind 结构变更、私有按钮池、第三方库／编辑器工具治理、音频效果优化、Player／Android／双 Editor 构建或验收。它们保留为审查候选，不作为本案任务。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

无。`restaurant-ui`、`grid-kitchen`、`fixed-companions`、`ui`、`entity` 和 `product-session` 的既有行为合同保持；`.openspec.yaml` 声明 `skip_specs: true`，不创建 delta specs。缓存与刷新调度细节写入 design，验收映射写入 execution-notes，不新增“整局零 GC”规格。

## Impact

- ModelView／HotfixView 的 GridPresentation、MonoGridKitchenBoard、MonoUIGridKitchen 及 HUD 生命周期与恢复刷新。
- Model／Hotfix 的 GridKitchenComponent、格子时钟／容量检查／寻路，以及顾客、前厅工作与伙伴调度的相关遍历。
- 既有 `ProductSessionTests`／`SoloRestaurantUiTests` PlayMode 测试程序集；按需要增加有行为断言的用例。
- 实施后同步相关实现约定与[审查记录](../../../../audit/2026-10-14-per-frame-performance.md)，标明实际消除的分配及仍保留的候选。

原规划授权之后，用户先授权实施第一批，再明确要求继续完成到 9/9；表现、固定步／寻路、综合入口与性能对照均已完成，逐项运行及采样限制见 [execution-notes.md](execution-notes.md)。缓存失效与遍历重入是主要风险；选择局部 owner 缓冲、代表性行为小集和一次实际计时，严谨程度、复用证据与接受的 NotRun 同见该说明。恢复页面与已归档 [cooperative-member-resume](../2026-10-14-cooperative-member-resume/design.md) 共用文件，保留其当前身份及等待合同，不重做恢复功能。
