# Spec Delta

## MODIFIED Requirements

### Requirement: 显式启动与普通 Launcher 兼容

系统 SHALL 使用显式提交的合法请求启用实例启动配置。配置无效时 MUST 在该入口进入 Play 前拒绝，不解释为普通启动。普通 Launcher 没有显式请求时 MUST 建立本轮默认 Solo 配置，直接进入临时格子地图及 HUD，不消费窗口草稿或继承上一轮角色；只有显式 legacy 才进入旧 ET 示例路线。默认启动不得创建网络监听或掩盖失败。

#### Scenario: 普通 Launcher 启动
- **WHEN** 用户没有提交显式实例请求而打开 Launcher.unity 并进入 Play
- **THEN** 创建新的本轮 Solo 配置，进入地图并加载 Board／HUD，无须提交启动窗口或用调试代码创建业务树

#### Scenario: 显式请求无效
- **WHEN** 显式请求包含非法角色或不完整配置
- **THEN** 进入 Play 前报告错误，不回落到默认角色或旧 Demo

#### Scenario: 显式角色不被默认值覆盖
- **WHEN** 已经提交合法 legacy／solo／host／client 请求
- **THEN** 使用原请求，普通 Launcher 的默认 Solo 不替换其角色或 endpoint

### Requirement: 最小不可变运行配置

实例配置 MUST 区分运行标识、实例标识、legacy／solo／host／client 角色及 endpoint 意图，生效后本轮不可变。标识不得为空；角色不能由分支职责、ET AppType 或 CodeMode 推导。配置不得要求工作目录职责、产品名、基线版本或自定义数据根。Solo 表示不创建网络监听的本地权威与本地玩家，不等于普通 legacy 启动。

#### Scenario: 改变下一轮草稿
- **WHEN** 本轮配置已经生效，用户修改窗口草稿
- **THEN** 本轮配置保持原值，下一轮只消费新提交的请求

#### Scenario: 切换运行角色
- **WHEN** 下一轮以另一合法运行角色启动
- **THEN** 使用新角色，不依据工作目录的开发／稳定职责固定 Host 或 Client

#### Scenario: 显式单机启动
- **WHEN** 用户提交合法 solo 请求
- **THEN** 本轮以单机产品角色启动，不解释为 legacy，不继承上一轮 Host／Client endpoint

### Requirement: endpoint 校验只表达意图

Host MUST 只接受 loopback 监听意图，省略时记录端口 0；Client MUST 提供非零端口的 loopback 连接目标；legacy 与 solo 不接受网络 endpoint。启动配置校验不得绑定或连接，不得把意图报告为实际网络就绪。

#### Scenario: Host 省略监听配置
- **WHEN** 合法 Host 请求未指定监听 endpoint
- **THEN** 记录 loopback 与端口 0 的意图，不报告监听已建立

#### Scenario: Client 目标非法
- **WHEN** Client 缺少连接目标、使用端口 0，或出现角色不允许的 endpoint
- **THEN** 请求明确被拒绝，不连接旧 Demo 默认服务

#### Scenario: Solo 携带网络配置
- **WHEN** solo 请求包含监听或连接 endpoint
- **THEN** 进入 Play 前拒绝，不静默忽略 endpoint，也不升级为联网 Host

## Sources

- [现有实例配置](../../../../../../Unity/Assets/Scripts/Game/Base/RuntimeInstance/RuntimeInstanceConfig.cs)、[启动窗口](../../../../../../Unity/Assets/Scripts/Game/Editor/Multiplayer/InstanceLaunchWindow.cs)。
- [主规格](../../../../../specs/runtime-instances/spec.md)、[提案](../../proposal.md)：此 delta 仅扩展角色及 endpoint 行为，其他工程作用域与退出规则继续适用。
