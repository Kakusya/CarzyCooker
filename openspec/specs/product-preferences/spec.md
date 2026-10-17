# product-preferences Specification

## Purpose

管理本设备的显示、指针操作与经营提醒偏好，使玩家可从入口或餐厅调整阅读和拖动习惯。设置独立于餐厅进度、网络权限和他人设备，保存失败不破坏旧设置，首次运行和旧设置缺项使用明确默认值。

## Requirements

### Requirement: 本机设置可查看调整和恢复默认

开始页和餐厅 SHALL 提供字号标准/较大/大号、地图拖动灵敏度慢/标准/快、经营提醒类别开关与临期阈值设置，支持取消、明确保存和恢复默认。默认字号/灵敏度标准，提醒开启、临期5个业务秒。阈值可选3/5/10秒。只保存本机偏好，不生成检查点、不修改其他成员或网络运行的业务规则。

#### Scenario: 从入口保存并进入餐厅
- **WHEN** 玩家保存大号文字、慢速拖动及关闭到货提醒，重新进入餐厅
- **THEN** 当前设置生效且重开仍保留；取消后续草稿不改变已保存值，恢复默认可回到标准参数

### Requirement: 设置应用保持原交互和可读性

字号调整 MUST 保持正文、动作列表和关闭/返回入口可读可访问；地图拖动灵敏度只改变镜头移动量，原指针进入UI不穿透、拖动不点击和单击原动作保持。鼠标与已有主触点沿同一规则，设置页面不自动暂停或发业务命令。HUD池化重开、换关和恢复后只应用当前已保存偏好，旧设置回调不覆盖新轮。

#### Scenario: 大字号与拖动后释放
- **WHEN** 玩家采用最大字号与快速拖动，在540×960原入口打开长说明并拖动地图后释放
- **THEN** 能滚动读完并关闭，拖动不增加移动或拿放请求；一次真实单击仍只发原一次请求

### Requirement: 本机偏好持久化失败保留旧值

偏好保存 MUST 验证具名选项和数值边界，成功提交后才公布已保存；失败保留旧有效值并显示可重试结果。缺失文件/旧格式缺项使用明确默认值；损坏或未知格式明确提示且不静默覆盖。设置不写入成功存档、合作授权或领取载荷。

#### Scenario: 写入失败和旧设置
- **WHEN** 保存偏好发生真实写入失败，或读取只有旧字段的有效设置
- **THEN** 前者保留原设置且允许重试，后者只为新增缺项使用默认，均不改动任何成功记录

## Sources

- [ProductPreferencesSystem](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductPreferencesSystem.cs).
- [ProductPreferencesPresentationSystem](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductPreferencesPresentationSystem.cs).
- [product-input-readability-settings](../../changes/archive/2026-10-17-product-input-readability-settings/execution-notes.md).
