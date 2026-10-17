# Proposal

## Why

当前文字大小和地图拖动参数固定，不能适应玩家的阅读与操作习惯。提供本机设置，同时维持已交付的鼠标/主触点单击和拖动规则。

## What Changes

- 开始页及餐厅HUD提供字号标准/较大/大号和地图拖动灵敏度慢/标准/快；默认标准，可恢复默认。
- 显式保存本机设置，重开与重新进入保留；设置不属于产品检查点，不改变Host或其他成员参数。
- 字号只影响可阅读文字及必要排版，仍可滚动阅读、关闭和执行原动作；拖动始终不转成点击。
- 不新增手柄、键位重绑定或强制横屏。该批经营提醒开关使用同一本机设置owner。

## Capabilities

### New Capabilities

- `product-preferences`：管理本设备的显示、指针操作及经营提醒偏好，明确默认、保存失败和生命周期，与业务进度和网络权威隔离。

### Modified Capabilities

无。

## Impact

现有 [MonoUIGridKitchen](../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoUIGridKitchen.cs)、[GridPresentationSystem](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)及入口导航；复用原ProductRoot owner和本地存储根，本机请求沿原Proto源生成，不增远端白名单、公共服务或表。与已有可读布局案分别负责参数选择/应用及结构滚动，参见 [execution-notes.md](execution-notes.md)。
