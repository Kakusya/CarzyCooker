# Proposal

## Why

已有两个内容大关，新的蒸食与茶饮需要实际关卡承接，而非仅在调试菜单出现。用户确认新增一个三个小关的主题章，Solo与合作均可玩。

## What Changes

- 新增“蒸茶小馆”主题章及Solo/合作各自独立三关链，菜单从牛肉蒸饺/热红茶递进到冰鲜奶茶和珍珠奶茶，具备真实供应/设备/载具/订单。
- 首关清晰单线、后关形成共享备料与前厅压力，使用可调初版客流/时长/阈值，不声称已完成正式平衡。
- 扩展当前固定两个章的校验和必要枚举/菜单消费者；保留旧两个章、旧Level/Next及文件读取语义，不追加旧档物资。
- 原零星成功、成功检查点、合作领取、终关无装修/无第四关、回房间独立复玩保持。新章依赖蒸食和茶饮两案真实交付。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-chapters`：从两个扩展为三个可选内容章，增加新章全链和旧档兼容合同。

## Impact

重点防止菜单闭包遗漏、旧档补物和只有配置没有经营链；以最小相关UTF、两模式三关真实入口和文件结果验收，正式平衡与设备未覆盖，见[执行说明](execution-notes.md)。

[RestaurantChapters](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantChapters.cs)、Luban章节/关卡/客流/供应/布局源、原入口预览及保存验证消费者。新章配置在源表生成，旧关链不重编号。兼容、逐关运行及实际文件验收在design/tasks明确。
