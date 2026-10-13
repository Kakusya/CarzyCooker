# Spec Delta

## MODIFIED Requirements

### Requirement: endpoint 校验只表达意图

Host MUST 默认只接受 loopback 监听意图，省略时记录端口 0；Client MUST 默认提供非零端口 loopback 目标。仅显式 LAN 模式可改用私网 IPv4，Host 须指定本机网卡地址，Client 目标端口非零；拒绝通配、公网、广播、组播及未支持 IPv6。legacy 与 solo 不接受网络 endpoint。启动配置校验不得绑定或连接，不得把意图报告为实际网络就绪。

#### Scenario: Host 省略监听配置
- **WHEN** 合法 Host 请求未指定监听 endpoint
- **THEN** 记录 loopback 与端口 0 的意图，不报告监听已建立

#### Scenario: Client 目标非法
- **WHEN** Client 缺少连接目标、使用端口 0，或出现角色不允许的 endpoint
- **THEN** 请求明确被拒绝，不连接旧 Demo 默认服务

#### Scenario: Solo 携带网络配置
- **WHEN** solo 请求包含监听或连接 endpoint
- **THEN** 进入运行前拒绝，不静默忽略 endpoint，也不升级为联网 Host

#### Scenario: 未显式开启 LAN
- **WHEN** 普通回环模式请求带私网或其他非回环地址
- **THEN** 拒绝该输入，不扩大监听范围

#### Scenario: 显式 LAN Host 合法意图
- **WHEN** LAN Host 指定合法本机私网 IPv4 和允许的端口
- **THEN** 记录监听意图，仍需实际绑定成功才能报告可连接
