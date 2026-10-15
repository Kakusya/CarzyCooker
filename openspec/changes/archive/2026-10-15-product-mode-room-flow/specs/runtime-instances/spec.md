# Spec Delta

## MODIFIED Requirements

### Requirement: 显式启动与普通 Launcher 兼容

系统 SHALL 使用显式提交的合法请求启用实例启动配置。配置无效时 MUST 在该入口进入 Play 前拒绝，不解释为普通启动。普通 Launcher 没有显式请求时 MUST 建立本轮默认 Solo 配置，先显示受管模式选择，选择单机后再进入选档和地图，不消费窗口草稿或继承上一轮角色；只有显式 legacy 才进入旧 ET 示例路线。默认启动不得创建网络监听或掩盖失败。

#### Scenario: 普通 Launcher 启动
- **WHEN** 用户没有提交显式实例请求而打开 Launcher.unity 并进入 Play
- **THEN** 创建新的本轮 Solo 配置并显示模式菜单；选择前没有运行餐厅或网络监听，无须实例窗口或调试代码；选择合作时完整交接为新的不可变配置

#### Scenario: 显式请求无效
- **WHEN** 显式请求包含非法角色或不完整配置
- **THEN** 进入 Play 前报告错误，不回落到默认角色或旧 Demo

#### Scenario: 显式角色不被默认值覆盖
- **WHEN** 已经提交合法 legacy／solo／host／client 请求
- **THEN** 使用原请求，普通 Launcher 的默认 Solo 不替换其角色或 endpoint

## Sources

- [实施与运行证据](../../execution-notes.md)。
- [普通入口用法](../../../../../../Book/快速开始.md)。
