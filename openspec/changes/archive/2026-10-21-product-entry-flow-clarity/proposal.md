# Proposal

## Why

当前已有单人首页、章节、记录、试玩分层及入口布局，但首页仍把游玩、合作结果、授权和设置并列，新开还要经过中间选择页。用户已确认建立优化方案的 change，本案收敛启动到餐厅准备阶段的路径，让主要选择和下一步更清楚。

## What Changes

- 首页突出「开始新游戏／继续游戏／合作游玩」，将菜单试玩、进度管理、设置放入次级区域，保留原合法功能与显式启动入口。
- 单人新开页合并章节选择与原开局预览，默认推荐「家常开张」；一次明确确认后才创建餐厅，不自动营业。
- 继续页区分读取中、无可继续记录、读取失败和有效记录，明确选档；完成记录仍只读。
- 在原入口布局中改善标题、列表、预览和固定导航的视觉主次，适配竖屏、宽屏、最大字号和安全区。
- 合作保留原房间、共享内容提案、全员准备及 Host 开局规则。启动真实阶段说明与现有 `product-startup-readiness-guidance` 衔接，不重复建设。
- 与独立的 `development-test-level-entry` 约定首页次级入口位置；该案未实施时不显示无效测试按钮。
- 范围外：经营 HUD 重构、新内容／平衡、账号、存档格式、传输／端口、公共菜单框架及新工具。实施与实际验收范围见 execution-notes。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `product-entry`：更新主入口层级与单人章节选择／预览路径，补充继续页读取状态，保留原操作及持久化边界。
- `restaurant-ui`：补充启动入口的主次布局、同页选择预览和既有代表环境下的可操作合同。

## Impact

主要涉及 ModelView/HotfixView 的 ProductEntryPresentation、SoloSavePresentation、入口导航与预览消费者，以及原 `UIGridKitchen.prefab`、Mono 布局和必要的 CodeBind 源。继续由原 ETUI／GF owner 管理 UI1101，不新增 UI 类型、表行或第二份业务状态；如需改绑定，按现有生成流程处理。实施同步 `Book/UI开发.md` 和相关入口用法。工程取舍、最小验收及可复用范围见 [execution-notes.md](execution-notes.md)。建议先实施本案，再整合[开发测试入口](../2026-10-21-development-test-level-entry/proposal.md)；两案不互相阻塞规划。
