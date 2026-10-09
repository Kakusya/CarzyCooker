# 现有包、第三方源码、分析器与环境的证据边界

## Purpose

记录当前 CarzyCooker 底座的现有包、第三方源码、分析器与环境的证据边界。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；本次不运行产品构建、玩法或编辑器验证，不描述未来做饭业务已实现。

## Requirements

### Requirement: 依赖源

依赖记录 MUST 依据 manifest、packages-lock、asmdef、csproj 和 .gitmodules；Unity 包声明与缓存/付费插件可用性分开，不自动升级或补装。

#### Scenario: 依赖源的适用行为

- **WHEN** 扫描发现 HybridCLR、UniTask、CodeBind、ReactiveBinding、StateController 与 Pipeline
- **THEN** 记录声明与本次未验证环境，不声称编辑器可用或所有依赖已固定

### Requirement: 生成与分析

Share.Analyzer/SourceGenerator SHALL 按项目引用提供分析与生成；现有 ET/UGF/LubanLib/扩展库作为复用入口，不把整库改造当文档前置。

#### Scenario: 生成与分析的适用行为

- **WHEN** 新增业务涉及生成诊断或扩展
- **THEN** 定位现有类型与消费者，避免新建重复框架

## Sources

- [manifest.json](../../../Unity/Packages/manifest.json)
- [packages-lock.json](../../../Unity/Packages/packages-lock.json)
- [ProjectVersion.txt](../../../Unity/ProjectSettings/ProjectVersion.txt)
- [DotNet.ThirdParty.csproj](../../../DotNet/ThirdParty/DotNet.ThirdParty.csproj)
- [Share.Analyzer.csproj](../../../Share/Analyzer/Share.Analyzer.csproj)
- [Share.SourceGenerator.csproj](../../../Share/SourceGenerator/Share.SourceGenerator.csproj)
- [.gitmodules](../../../.gitmodules)
