# Spec Delta

## ADDED Requirements

### Requirement: 运行程序集的模式约束

ET Model/Hotfix SHALL 按 asmdef 的 UNITY_ET 和热更编译条件参与 Unity 编译；这些层声明 noEngineReferences，客户端表现放在适用的 View 层。DotNet 通过 csproj 链接共享 ET 源并采用自身编译符号，不能从 Unity Editor 编译结果推断服务端配置也通过。

#### Scenario: 给 Model 或 Hotfix 增加代码

- **WHEN** 新代码落入不引用 UnityEngine 的 ET 数据或逻辑程序集
- **THEN** 遵守实际程序集引用边界；需要引擎表现的代码放入适用 View 层，不通过改生成工程绕过边界

#### Scenario: 共享源被服务端链接

- **WHEN** 修改被 DotNet csproj 的 Compile Include 链接的 Unity ET 源
- **THEN** 同时核对服务端定义与链接范围；不复制同义实现，也不假定客户端 view 自动参与服务端编译

### Requirement: 依赖声明和可用性的区分

依赖核对 MUST 区分 manifest 声明、lock 解析记录、实际程序集编译及 Player 行为。带版本后缀与不带后缀的 Git 依赖不能一概描述为全部固定；服务端链接 Unity PackageCache 中数学源码的配置依赖本地解析产物存在，单凭 .NET SDK 安装不足以证明可构建。

#### Scenario: 仅检查包配置

- **WHEN** 本轮只读取 manifest 与 lock，没有重新解析或编译
- **THEN** 报告配置事实及解析/编译 NotRun，不把旧 lock 的存在当作本轮实际成功

#### Scenario: 缺少服务端链接的缓存源码

- **WHEN** DotNet.ThirdParty 所引用的 Unity 数学缓存目录在环境中不存在
- **THEN** 记录该构建前置；文档扫描不自行下载、升级依赖或执行产品构建

## Sources

- [Model 程序集](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Game.ET.Code.Model.asmdef)
- [Hotfix 程序集](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Game.ET.Code.Hotfix.asmdef)
- [View 程序集](../../../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Game.ET.Code.ModelView.asmdef)
- [DotNet Model 链接](../../../../../../DotNet/Model/DotNet.Model.csproj)
- [服务端第三方链接](../../../../../../DotNet/ThirdParty/DotNet.ThirdParty.csproj)
- [包声明](../../../../../../Unity/Packages/manifest.json)
- [解析记录](../../../../../../Unity/Packages/packages-lock.json)
