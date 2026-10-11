# Proposal

## Why

当前格子样机已能寻路、拿放和联机交接，但碗中内容仍是字符串占位，玩家无法切配或打蛋。先把已确认代表菜“番茄蛋花汤”的备料阶段做成可玩的合作片段，逐步进入实际制作。

## What Changes

- 增加最小物品与手工配方定义，覆盖番茄、番茄块、鸡蛋、蛋液、碗及切配／打蛋，不整批导入候选目录。
- 容器持有真实材料身份；支持把手中鸡蛋放入台面上的空碗，完整校验后转移，搬碗带走内容。
- 增加切配台切番茄和手持碗打蛋；点击开始，由 Host Tick 推进，停止／移动／失联保留进度并释放执行资格，队友可以接续，完成只产生一次新半成品。
- 场景与 HUD 共用动作选择；显示工位名、材料／容器内容、加工进度、开始／停止／继续和明确拒绝原因。
- **BREAKING**：容器及加工快照改变，产品协议由版本 2 升至 3；两端使用同一实现，旧版本明确拒绝，不兼容旧占位内容。
- 只在一台电脑验收：普通 Launcher Solo 与同机独立 Fiber 的真实 loopback KCP Host／Client。

## Capabilities

### New Capabilities

- `kitchen-item-processing`：临时厨房的真实材料／容器、切配与手持打蛋、手工认领和暂停接续。本次仅落地备料子集，不照搬尚未实现的完整目标规格。

### Modified Capabilities

- `grid-kitchen`：扩展目标默认动作与执行前校验，允许向兼容容器添加材料，继续保证普通单槽、无默认交换及四邻自动接近。

## Impact

- Model／Hotfix 的 `Share/Product/GridKitchen`、既有产品 handler／wire／版本，ModelView／HotfixView 的受管地图与 HUD，现有 `ProductSessionTests`。
- `Design/Excel/ET/Datas/__tables__.xlsx` 及两张最小源表、ET 现有 Luban 输出；`Design/Proto/ET-Client/ZZGridKitchen.proto` 与正常生成链。复用原 UI／Entity 注册及 CodeBind 引用，不新增工具框架。
- 同步当前进度、受影响的产品状态说明与网络／配置使用记录；严谨程度、样机参数、复用依据及逐功能验收见 [execution-notes.md](execution-notes.md)。
- 范围外：煮汤、锅与盛装、顾客／订单／洗碗、供应、正式 RestaurantRuntime／Level／存档、全局暂停与重连、手机／Player／AOT、LAN／公网、第二工作区同步及双 Editor 必测。

用户已在后续回合明确要求 apply；本 change 的实施与逐功能验收见执行说明。
