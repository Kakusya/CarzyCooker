# Proposal

## Why

现有十一款菜品已有实际制作链，但只读菜谱仍以任意同类物存在判断备料就绪。模具内熟蛋糕、加工锁定原料、不同饮品绑定及半杯基底可能使提示跳步或要求重复制作。

## What Changes

- 按当前载具、加工状态和真实订单选择可用成品与下一步。
- 未完成配方先展示已投入材料与剩余份数；不把其他载具的内容当成可直接取用材料。
- 保留原菜谱、受管 HUD、Solo 首单与合作只读入口。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `kitchen-recipe-guide`：补充载具、锁定与绑定的事实约束。

## Impact

限于 HotfixView 的 GridRecipeGuide 与既有 UTF；不改配方、状态、协议、文件版本或 Prefab。使用最小相关行为断言及普通 Launcher/真实 KCP 只读界面验证，不做真机测试。验证取舍见 [execution-notes.md](execution-notes.md)。
