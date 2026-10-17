# Proposal

## Why

现有结算只显示总成功/未满足数与总分，玩家难以知道哪类菜经常遗漏。补充本关按菜品的实际结果，帮助下一轮分工和备料。

## What Changes

- 自然结算中提供按菜品查看已交付数、超时数和实际得分贡献；未询问顾客、满队列跳过来客不算订单。
- 使用当前关真实订单，不新增历史统计档；营业总览继续负责实时需求，本案负责完成后复盘。
- 查看与返回不保存、不替玩家选择装修或继续；零星正常成功、Host/Client原权限保持。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-ui`：补充本关按菜品复盘及当前结果身份保护。

## Impact

复用 [GridPresentationPanels](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)、[独立订单投影](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridFrontProjection.cs)和原结算导航；无需协议、表或存档格式变化。验收取舍见 [execution-notes.md](execution-notes.md)。
