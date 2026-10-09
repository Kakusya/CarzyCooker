# DotNet 启动、链接源码和服务器数据/HTTP 模块

## Purpose

记录当前 CarzyCooker 底座的DotNet 启动、链接源码和服务器数据/HTTP 模块。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；本次不运行产品构建、玩法或编辑器验证，不描述未来做饭业务已实现。

## Requirements

### Requirement: 共享源码与入口

DotNet 项目 SHALL 按 csproj 链接 Unity 的 ET Core/Model/Hotfix 等源；App 经 Loader 初始化 World 单例和 CodeLoader，循环驱动 Update/LateUpdate。

#### Scenario: 共享源码与入口的适用行为

- **WHEN** 修改服务器共享业务
- **THEN** 核对 Unity 源与 DotNet Compile Include，不复制一份实现或把客户端 view 引入 server

### Requirement: 持久化与 HTTP 的既有边界

DBComponent MUST 使用 MongoClient/IMongoDatabase，HttpComponent 使用 HttpListener；这些底座模块不代表做饭 SaveSlot、领取或 Host 权威系统已实施。

#### Scenario: 持久化与 HTTP 的既有边界的适用行为

- **WHEN** 设计做饭存档或 HTTP 业务
- **THEN** 先确认具体消费者与需求，再复用模块，不从模块存在推断产品契约已完成

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

- [Program.cs](../../../DotNet/App/Program.cs)
- [Init.cs](../../../DotNet/Loader/Init.cs)
- [DotNet.Core.csproj](../../../DotNet/Core/DotNet.Core.csproj)
- [DotNet.Model.csproj](../../../DotNet/Model/DotNet.Model.csproj)
- [DotNet.Hotfix.csproj](../../../DotNet/Hotfix/DotNet.Hotfix.csproj)
- [DBComponent.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Server/Module/DB/DBComponent.cs)
- [HttpComponent.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Server/Module/Http/HttpComponent.cs)
- [DB 操作](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Module/DB/DBComponentSystem.cs)
- [HTTP 生命周期](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Module/Http/HttpComponentSystem.cs)
