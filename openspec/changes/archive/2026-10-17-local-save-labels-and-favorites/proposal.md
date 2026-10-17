# Proposal

## Why

已有选档能查看身份、时间和来源，但多份成功检查点不容易辨认。玩家需要本机名称和收藏，区别常用餐厅及独立分支。

## What Changes

- 允许为现有记录设置/清除显示名称、添加/取消收藏，并按收藏筛选；原身份、修订、章节和来源始终可查看。
- 元数据归本机repository/Profile/Slot，不写成功载荷、不重签授权、不改来源身份，不传播到其他设备。
- 名称首版限制32个Unicode文本元素，拒绝换行和控制字符；空名称恢复原显示。写入失败保留旧名称/收藏并允许重试。
- 不删除、覆盖、合并或移动进度；继续游戏仍经原完整Select/revision校验。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `solo-save-progress`：增加本机显示名称、收藏与独立元数据的失败保护。

## Impact

扩展原 [SoloSaveRepository](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs)及 [SoloSaveInspection](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSaveInspection.cs)路径。元数据单独持久化，不改现有成功文件或授权/领取载荷；本地请求沿原Proto源生成，不新增远端白名单。真实临时文件与原UI验收见 [execution-notes.md](execution-notes.md)。
