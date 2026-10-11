# Spec Delta

## ADDED Requirements

### Requirement: 界面本轮所属异步工作

公共界面、Widget 及迁移后的动态 Widget 加载 MUST 关联实际界面本轮 owner，统一托管其资源和普通异步工作。关闭或销毁 SHALL 使本轮失效并取消业务等待；重新打开使用新轮次，旧加载不能创建、绑定或改写新界面。可见性变化和表现暂停不自动等同于关闭。

#### Scenario: 动态 Widget 尚在加载时父界面关闭

- **WHEN** 动态 Widget 仍在加载，父界面关闭而加载 Mono 尚未销毁
- **THEN** 按父界面本轮 owner 取消等待，晚到结果不实例化或绑定旧 Widget，并由原请求收尾

#### Scenario: 界面关闭后重新打开

- **WHEN** 同一个池化界面关闭后再次打开，同时旧异步工作完成
- **THEN** 新轮次工作有效，旧工作不能写回新界面，也不能清除新轮次登记

#### Scenario: 界面只被遮挡或暂停表现

- **WHEN** 界面仍属于同一打开轮次，只改变可见性或表现暂停
- **THEN** 不以此取消所属任务；真实关闭和销毁仍负责失效及收尾

#### Scenario: 未消费加载结果

- **WHEN** 界面启动受管资源加载后未 await 或读取结果，随后关闭
- **THEN** 公共入口仍取消本轮业务等待并负责资源归属，不要求派生界面逐次补写清理

## References

新增契约以公共 owner 接入为范围，不替换现有 UI 显示等待器；入口见 [AExUIForm.cs](../../../../../Unity/Assets/Scripts/Game/UI/Common/AExUIForm.cs)、[ADynamicUIWidgetLoader.cs](../../../../../Unity/Assets/Scripts/Game/UI/Common/ADynamicUIWidgetLoader.cs) 和 [UI 手册](../../../../../Book/UI开发.md)。
