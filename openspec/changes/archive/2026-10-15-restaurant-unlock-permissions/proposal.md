# Proposal

## Why

RestaurantRenovation 记录已安装工位，但尚无独立菜谱/设备解锁事实；当前校验强制同章节菜单相同。许可缩小已于2026-10-15裁决，尚需实现共同许可读口。

让经营奖励真正打开新的菜谱和设备，同时让每关菜单、供应与合法操作保持一致。

## What Changes

- 增加持续餐厅所属的稳定定义解锁集合，装修/剧本按定义发放，不与运行物件 ID 混用。
- 本关许可从已解锁与关卡允许集计算，菜单、采购、投料、启动和 HUD 读取同一结果。
- 解锁进入成功检查点，旧档使用明确初始基线；许可缩小执行已确认的保旧物、保已开始作业并禁新生产/新盛装规则。

优先级：**P1**；建议顺序：**09**。前置：无新增 change 硬前置，复用已交付业务。优先级表示用户价值和依赖，不要求所有案串行。

范围外：不以星级卡解锁，不新增货币树或账号等级，不清除存量物品，已完成的受限批次也禁止新盛装。

## Capabilities

### New Capabilities

- `restaurant-unlocks`：餐厅持续解锁及与本关允许集的交集。

### Modified Capabilities

- `restaurant-renovation`：新增解锁奖励与当前关加成分离。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/RestaurantRenovation.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/RestaurantRenovation.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RenovationConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RenovationConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs)
- [Docs/Product/backlog.md](../../../../Docs/Product/backlog.md)

实施交接：2026-10-15 已确认许可缩小规则：保留旧物、在途货物和已开始作业；拒绝受限菜品新采购、投料、启动及新盛装，已完成批次也不例外。按实际配方闭包区分共用原料与受限产物，不再等待产品裁决。

风险是 UI 与裁决许可不一致或换关吞旧物。以一种新增解锁和一组已裁决缩小场景验证真实入口及原物保留，不做全目录许可穷举。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

本案范围已确认并授权实施；已有运行结果逐项记录于execution-notes，尚未通过的任务保持未完成。Editor/UTF按既有实施范围执行，真机、物理LAN、Player/AOT与构建保持暂缓；不自动提交、push或归档。优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
