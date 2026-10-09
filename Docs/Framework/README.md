# GameDevelopmentKit 框架用法

当前基线是 `main` 分支，来自原 ET 底座，ProcedurePreset 直接进入 ET。详细流程保留 [Book](../../Book/README.md) 的适用原文；GameHot/纯 GF 示例章节只作历史参考，框架示例不是做饭业务。

## 版本与启动

Unity/ProjectSettings/ProjectVersion.txt 指定 Unity 6000.3.18f1。服务端 ET 8.1、.NET 8，客户端 GF + ET；当前 main 分支的 ET 底座无 GameHot 或纯 GF 示例切换；HybridCLR 热更、UniTask 异步、Luban 配置。

Kit.sln 用于工具与分析器，DotNet/DotNet.sln 用于服务端，Unity IDE 工程由 Editor 生成。人工常规启动：打开 Unity 等待编译，编译 Kit.sln 准备工具，点击 Toolbar Launcher 运行示例。MongoDB 按功能需要，Odin Inspector 为付费依赖。这些操作不属于本次迁移验收。

## 结构

| 目录 | 作用 |
|---|---|
| `DotNet/` | App/Core/Loader/Model/Hotfix/ThirdParty；部分项目链接 Unity ET 源码 |
| `Share/` | Analyzer/FileServer/Tool/Libs |
| `Design/Excel/`、`Design/Proto/` | 配置和协议源 |
| `Tools/`、`Config/` | 生成/开发工具与服务端运行配置 |
| `Unity/Assets/Scripts/Game/` | 公共组件、Procedure、UI、Entity、Generate、ET、HybridCLR（无 Hot 业务目录） |
| `Unity/Assets/Scripts/Library/` | ET、UGF、LubanLib 和扩展库 |
| `Unity/Assets/Res/` | 配置资源、Prefab、场景与热更资产 |
| `Unity/Packages/`、`Unity/ProjectSettings/` | 依赖声明与设置，缓存不属于源码 |

## 用法路由

| 需求 | 保留的手册 |
|---|---|
| 启动、模式和目录 | [快速开始](../../Book/快速开始.md)、[Project结构](../../Book/Project结构.md) |
| UI / CodeBind / ETUI | [UI开发](../../Book/UI开发.md) |
| Entity / UGFEntity | [Entity开发](../../Book/Entity开发.md) |
| Luban / ID / Proto | [Luban配置](../../Book/Luban配置.md)、[Proto工具](../../Book/Proto生成工具.md) |
| 本地化 | [多语言](../../Book/多语言.md) |
| 热更、资源、打包 | [HybridCLR](../../Book/HybridCLR热更.md)、[AssetSet](../../Book/AssetSet.md)、[一键打包](../../Book/一键打包.md) |
| 编辑器、生成骨架 | [ET代码生成](../../Book/ET代码生成工具.md)、[Toolbar](../../Book/自定义Toolbar.md) |
| 服务端 | [动态扩容](../../Book/动态扩容.md)、[管理后台](../../Book/管理后台.md) |

源码和规格见 [模块索引](../AI/module-index.md)，实现约定见 [开发规范](../../references/business-code-conventions.md)，手册差异见 [已知问题](../../KnownIssues.md)。编辑器自动化用 [Unity CLI/Pipeline](../../Book/UnityCLI.md)，按本工程发现实际能力。
