# Proposal
## 2026-10-14 用户试玩豁免

用户对“真实新玩家操作记录”的安排答复“忽略这项”。本轮跳过新玩家理解、求助点与再次经营意愿观察，标记 Skipped（用户豁免）；实际效果仍未验证。UTF 和普通 Launcher 操作验收继续，不把脚本当人类理解证据。试玩调校仅针对可复现的实际操作问题；没有依据时不改参数。此决策覆盖下文原规划中要求无玩家仍待完成的条款，历史记录保留。

## Why

已有两菜能制作和出餐，但缺少让新玩家独立理解工序的产品入口。以真实菜谱和订单串联现有操作，不增加新的厨房规则。

## What Changes

- 为汤和吐司显示原料、工序依赖、设备与容器；从真实定义读取名称与要求。
- 以当前选择和投影显示当前阶段／缺口，不替玩家选择目标或直接完成工作。
- Solo 提供可跳过、可重看的首单引导；联机只读菜谱不授予控制权。

目标模式：Solo 引导；已开放联机只读菜谱。层次：二：看得清、操作清楚；依赖按[整体路线](../../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不做通用任务系统、账户新手标志、全菜目录、自动代操作；不重构 UI 结构，使用前置保留的内容区域。

## Capabilities

### New Capabilities

- `kitchen-recipe-guide`：配方步骤、缺口提示与可跳过首单引导。

### Modified Capabilities

无。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)
- [Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/GridPresentation.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/GridPresentation.cs)
- [Design/Excel/ET/Datas/Product](../../../../Design/Excel/ET/Datas/Product)
- [openspec/specs/dish-recipes/spec.md](../../../specs/dish-recipes/spec.md)

提示可能错误推断工作完成或成为第二业务权威。用纯值判断和真实 UI 指令断言，加一次无口头操作说明的首单试玩；外部试玩未进行就保留未完成。验证取舍与逐任务映射见 [execution-notes.md](execution-notes.md)。本轮已授权实施，代码与相关 UTF 已完成；真实玩家理解按用户要求跳过，构建 NotRun；不执行提交、归档或第二工作区操作。

