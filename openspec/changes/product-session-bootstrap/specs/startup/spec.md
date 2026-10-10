# Spec Delta

## ADDED Requirements

### Requirement: 产品角色 ET 根入口分流

产品启动能力实际接入后，合法显式 Host／Client 请求 SHALL 经现有 ET 稳定装载入口创建独立产品根场景及适用角色子树；普通／合法 legacy 保留旧 ET Main 路线。产品路径不得触发 Demo 根启动处理，不得用工作区职责或 CodeMode 替代运行角色。能力缺失、配置失效及初始化失败仍须明确拒绝，不回落 Demo。

#### Scenario: 显式产品角色启动
- **WHEN** 合法 Host／Client 配置且适用产品能力已编译并装载
- **THEN** 创建产品根与适用角色子树，不发布旧 Main 的示例启动事件，不连接 Realm／Gate 或启动数据库服务

#### Scenario: 产品能力不适用
- **WHEN** 显式角色所需代码未装载或本轮初始化失败
- **THEN** 保留具体失败并收尾本轮实际资源，不改成普通入口或报告就绪

#### Scenario: 普通 Launcher 回归
- **WHEN** 未提交产品请求或明确选择合法 legacy
- **THEN** 保留原 ET Main 的适用入口，不继承上一轮产品角色与会话

### Requirement: 产品启动状态按实际阶段报告

系统 MUST 区分配置准备完成、产品根初始化、监听建立和客户端加入；Host 完全就绪须同时具有实际监听及本地玩家加入成功，Client 就绪须有当前加入确认。失败和停止不被旧阶段成功覆盖，后续 await 结果须核对实例身份。本轮结束只收尾实际创建的树和入口，不透明恢复或启动新轮。

#### Scenario: Host 只有监听成功
- **WHEN** Host 已建立监听但本地玩家尚未完成加入
- **THEN** 可以报告监听地址，不报告 Host 完全就绪；本地加入失败使本轮失败并清理监听

#### Scenario: 加入依赖消息泵
- **WHEN** 产品根已创建，本地 Join 尚需 Fiber 更新处理
- **THEN** 装载先允许既有 Runner 驱动消息，再等待本地加入；不在消息泵启动前阻塞等待队列答复

#### Scenario: Client 仅已创建 Session
- **WHEN** Client 已创建网络 Session，但没有当前 Join 成功答复和有效名单
- **THEN** 仍报告连接中或加入中，不以对象存在替代业务 Ready

#### Scenario: 等待中停止或下一轮启动
- **WHEN** 本轮停止，旧初始化或加入答复后来完成
- **THEN** 旧结果不改变已停止状态或下一轮会话，退出只影响原本轮入口和资源

## Sources

- [本项设计](../../design.md)、[既有 startup 主规格](../../../../specs/startup/spec.md)
- [启动项交接及不可用拒绝](../../../multiplayer-instance-bootstrap/specs/startup/spec.md)
- [ET Entry](../../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Entry.cs)、[现有 Main 初始化](../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/FiberInit_Main.cs)
- [稳定加载](../../../../../Unity/Assets/Scripts/Game/ET/Loader/CodeLoader.cs)、[ProcedureET](../../../../../Unity/Assets/Scripts/Game/Procedure/ProcedureET.cs)
