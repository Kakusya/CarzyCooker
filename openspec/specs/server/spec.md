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

## Sources

- [Program.cs](../../../DotNet/App/Program.cs)
- [Init.cs](../../../DotNet/Loader/Init.cs)
- [DotNet.Core.csproj](../../../DotNet/Core/DotNet.Core.csproj)
- [DotNet.Model.csproj](../../../DotNet/Model/DotNet.Model.csproj)
- [DotNet.Hotfix.csproj](../../../DotNet/Hotfix/DotNet.Hotfix.csproj)
- [DBComponent.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Server/Module/DB/DBComponent.cs)
- [HttpComponent.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Server/Module/Http/HttpComponent.cs)
