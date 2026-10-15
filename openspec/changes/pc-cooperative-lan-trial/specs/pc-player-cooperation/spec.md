# Spec Delta

## Purpose

为选定且已完成 Editor 验收的合作功能提供独立 PC Player 入口和明确的设备交付范围，分别证明真实构建、同机双进程与两台物理 PC 的局域网合作，避免把源码编译或同 Editor 结果误认为包体已可玩。

## ADDED Requirements

### Requirement: Player 显式启动角色
独立 Player MUST 经正常启动链消费显式角色和 endpoint 输入，无显式输入沿现有默认Solo上下文显示模式选择，选择前无餐厅或监听；无效输入须报告失败，不能依赖 Editor 草稿或自动回退成其他角色。

#### Scenario: 无参数启动
- **WHEN** 玩家直接打开没有显式角色参数的Player
- **THEN** 先显示现有模式选择，不继承Editor草稿、不自动开餐厅或监听

#### Scenario: 使用包体显式启动 Client
- **WHEN** Player 收到合法 Client 角色和目标
- **THEN** 通过正常启动建立 Client 接入，实际加入/视图完成前不报告可操作

### Requirement: 实际监听结果可交接
Host Player SHALL 显示实际绑定地址和非零端口，Client 显式使用该目标；工具端口、配置端口零或猜测值不得被作为已就绪地址。

#### Scenario: 动态端口启动 Host
- **WHEN** Host 使用合法地址及端口零意图
- **THEN** 成功监听后报告实际端口，失败显示真实原因，不扫描替代目标

### Requirement: 包体与设备验收分别报告
交付记录 MUST 区分构建成功、当轮包同机双进程成功及两台物理 PC LAN 成功，并列明已验证功能。前一阶段成功不得替代后一阶段，也不得声称未纳入功能已验证。

#### Scenario: 构建成功但跨机失败
- **WHEN** 实际构建成功而第二台 PC 无法完成加入或玩法
- **THEN** 保留 Player 日志并报告 LAN 未通过，不以同机结果覆盖失败

### Requirement: 本轮进程与资源独立
停止与失败清理 MUST 仅释放本次实际拥有的进程、连接与临时输入；不能终止其他工程、已有测试或用户进程。

#### Scenario: 一个 Player 启动失败
- **WHEN** Client 包启动失败而 Host 和其他工程仍运行
- **THEN** 仅清理失败接入，其他进程保持原状态
