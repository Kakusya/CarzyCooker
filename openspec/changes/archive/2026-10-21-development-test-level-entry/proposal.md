# Proposal

## Why

现有试玩可按菜单首关开始，却不能从游戏内直接新开指定章节小关，也缺少独立的测试重开与返回路径。用户希望整理入口的同时方便测试，本案提供有限的开发测试餐厅，减少重复跑前序关卡的成本。

## What Changes

- 在 Editor 和 Development Build 提供「测试关卡」，按章节小关／功能场景选择，展示菜单、起点和不保留正式进度的说明；正式非开发环境同时隐藏入口并拒绝测试启动。
- 首期支持当前正式章节的 Solo 小关、基础操作及当前已公开的独立菜单首关。全部从准备阶段建立新的临时餐厅，不读取正式记录作为测试世界。
- 新增受限的测试开局意图和配置校验，补齐起点需要的现有工位、原料、生产许可；复用原厨房、操作、配方、时钟与消息裁决。
- 测试期间持续显示标识，支持重开当前测试、返回测试列表和返回主菜单；记住本次应用内的上次选择，旧运行完全退役后再创建新运行。
- 测试不绑定正式存档、不写正式进度、不生成合作领取凭证；普通章节与菜单试玩的保存语义保持原样。
- 与 `product-entry-flow-clarity` 共用入口位置及原受管 UI；首期单人无监听，不新增联机测试编排、CLI 命令、外部依赖或固定端口。
- 范围外：公开练习模式、任意原始 LevelId／脚本执行、结算／故障注入、无限资源、进度快照、正式存档迁移、独立 Player／真机／物理 LAN 验收。实施与实际验收范围见 execution-notes。

## Capabilities

### New Capabilities

- `development-test-levels`：开发环境限定的测试场景目录、指定起点临时运行、进度隔离及重开返回。

### Modified Capabilities

无；正常游玩、主规格章节关链及成功保存合同不变。

## Impact

涉及 Game/Base/RuntimeInstance 的有限启动意图及 Loader 初始化交接，ET Model/Hotfix 的测试目录／开局边界，ModelView/HotfixView 的目录与测试导航；复用原 ProductHost／ProductClient、RestaurantRuntime、UI1101 和 Board。读取现有章、Level、InitialLayout、Supply 等 Luban 配置，首期不新增表、复制菜谱配置或手改生成物。必要的测试前置只在新建临时世界时初始化，不修改正式源表数值。实施同步入口用法和相关 UTF。风险、验收映射及包体未验证边界见 [execution-notes.md](execution-notes.md)。
