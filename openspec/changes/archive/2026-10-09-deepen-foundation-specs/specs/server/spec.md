# Spec Delta

## ADDED Requirements

### Requirement: DB 操作的持久化范围

既有 DB 保存 SHALL 按实体 Id 执行单文档替换并允许 upsert；默认集合由实体类型全名决定，显式集合参数可覆盖。操作使用当前 DB CoroutineLock 分桶，不构成跨进程锁或多文档原子事务。保存空实体只记录错误并返回，不能作为保存成功的证明。

#### Scenario: 保存一个有效实体

- **WHEN** 以有效实体调用普通 Save 入口
- **THEN** 依据实体 Id 与所选集合替换或插入该文档；等待该操作不自动提交其他相关实体

#### Scenario: 保存空实体或多实体业务

- **WHEN** 保存参数为空，或业务要求多个实体共同提交
- **THEN** 空参数路径记录错误并返回；多实体一致性另需业务设计，不能从逐个 Save 推断已有事务保证

### Requirement: HTTP 监听与请求收尾

HttpComponent MUST 按地址中的非空分号分隔项注册监听前缀；销毁时停止并关闭监听。请求按 SceneType 和绝对路径分发，在 handler 执行后关闭输入与输出流；handler 异常记录错误，现有边界不保证生成统一业务错误响应或设置特定 HTTP 状态。

#### Scenario: 正常请求完成

- **WHEN** 已注册的请求路由找到对应 handler 并完成处理
- **THEN** 结束本次输入与输出流，不把监听组件当作客户端请求 owner

#### Scenario: handler 抛出异常

- **WHEN** 分发或处理请求发生异常
- **THEN** 当前入口记录异常并执行流关闭；不能以连接结束推断返回了可消费的业务失败 payload

#### Scenario: owner 销毁

- **WHEN** HTTP owner 执行销毁
- **THEN** 停止并关闭监听，接收循环以捕获的 InstanceId 判定生命周期；不由文档任务启动监听或执行系统权限修改

## Sources

- [DB 操作](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Module/DB/DBComponentSystem.cs)
- [HTTP 生命周期](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Module/Http/HttpComponentSystem.cs)
- [服务端启动](../../../../../../DotNet/Loader/Init.cs)
