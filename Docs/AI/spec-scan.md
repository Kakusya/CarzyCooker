# 底座规格深度扫描

2026-10-09，用户授权继续扫描并完善 Spec。变更为 [deepen-foundation-specs](../../openspec/changes/archive/2026-10-09-deepen-foundation-specs/proposal.md)。本页记录本轮范围与契约映射，验证结果统一在 [verification](../../openspec/changes/archive/2026-10-09-deepen-foundation-specs/verification.md)；它不是新的状态副本或运行日志归档。

## 读取范围

以当前 main 工作区枚举 6878 条文件路径，读取 2639 个源码/配置/工具/文档文本和 3627 个 Unity 序列化/元数据文本；610 个二进制或其他类型文件只计路径，两个不可按 UTF-8 文本读取的文件不读正文。统计发生在本轮增量规格写入前，包括当时已有协作文档和 change scaffold，不代表最终文件总数。

排除 Git 对象、Unity/Library、PackageCache、Temp、Bin/bin/obj、构建/发布目录、日志、用户设置、node_modules、Python 缓存和 HybridCLRData。Unity/Assets/Scripts/Library 是源码，纳入读取；文件枚举不跟随目录符号链接。导表源工作簿只定位路径，未编辑或执行导表。

读取范围用于查找模块与配置入口；语义精读围绕下表及各增量 Sources，不声称对全部第三方/生成源码逐行审计。为确认 AOT 返回类型，另定点只读已解析 HybridCLR 包的 RuntimeApi 签名，未遍历 PackageCache 正文；正式引用仍指向项目 helper。

## 契约映射

十四个 capability 沿用既有路径；31 项新增要求、1 项条件修订。增量有 71 个场景，其中保留原 ET 固定入口的 1 个场景，净新增 70 个场景。未增设新 capability。

| 规格 | 新增要求 / 增量场景 | 核对行为与主要源码 |
|---|---|---|
| [startup](../../openspec/specs/startup/spec.md) | 3 / 9；另修订固定入口 | [Splash](../../Unity/Assets/Scripts/Game/Procedure/ProcedureSplash.cs)、[检查](../../Unity/Assets/Scripts/Game/Procedure/ProcedureCheckResources.cs)、[预加载](../../Unity/Assets/Scripts/Game/Procedure/ProcedurePreload.cs)：模式分支、等待顺序、失败不推进、宏前提 |
| [runtime-foundation](../../openspec/specs/runtime-foundation/spec.md) | 2 / 4 | [Entity](../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Entity/Entity.cs)、[EntityRef](../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Entity/EntityRef.cs)：下级先销毁、实例身份、普通引用不自动取消任务 |
| [ui](../../openspec/specs/ui/spec.md) | 3 / 7 | [异步入口](../../Unity/Assets/Scripts/Game/UI/Common/UIExtension.Awaitable.cs)、[等待器](../../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Runtime/Awaitable/Awaitable.UIComponent.cs)、[Widget](../../Unity/Assets/Scripts/Game/Container/UIWidgetContainer.cs)：缺表/重复拒绝、取消、注册与关闭 |
| [entity](../../openspec/specs/entity/spec.md) | 2 / 6 | [GF 入口](../../Unity/Assets/Scripts/Game/Entity/EntityExtension.Awaitable.cs)、[ET 包装](../../Unity/Assets/Scripts/Game/ET/Loader/UGF/Entity/UGFEntity.cs)、[view](../../Unity/Assets/Scripts/Game/ET/Loader/UGF/Entity/ETMonoUGFEntity.cs)：null 与异常、取消、显示与逻辑 owner 分离 |
| [resource-management](../../openspec/specs/resource-management/spec.md) | 3 / 6 | [ResourceContainer](../../Unity/Assets/Scripts/Game/Container/ResourceContainer.cs)、[EventContainer](../../Unity/Assets/Scripts/Game/Container/EventContainer.cs)、[更新](../../Unity/Assets/Scripts/Game/Procedure/ProcedureUpdateResources.cs)：复用版本、实际卸载/解绑、整体更新终态 |
| [data-generation](../../openspec/specs/data-generation/spec.md) | 2 / 4 | [配置](../../Design/Excel/ET/luban.conf)、[导出](../../Share/Tool/ExcelExporter/ExcelExporter.Luban.cs)、[复制](../../Share/Tool/ExcelExporter/LubanFileHelper.cs)：五目标、复制清理、失败后部分写入 |
| [proto-generation](../../openspec/specs/proto-generation/spec.md) | 2 / 4 | [发现/排序](../../Share/Tool/Proto2CS/Proto2CS.cs)、[ET 编号](../../Share/Tool/Proto2CS/Proto2CS.ET.cs)：三个域、先递增后编号、空域跳过、顺序兼容 |
| [localization](../../openspec/specs/localization/spec.md) | 2 / 4 | [切换](../../Unity/Assets/Scripts/Game/Localization/LocalizationExtension.cs)、[解析](../../Unity/Assets/Scripts/Game/Localization/LubanLocalizationHelper.cs)：先清旧字典、内置/外部顺序、非事务部分写入 |
| [network](../../openspec/specs/network/spec.md) | 2 / 5 | [Session](../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Message/Session.cs)：超时与响应竞争、迟到忽略、ERR_Cancel 与异常区分 |
| [server](../../openspec/specs/server/spec.md) | 2 / 5 | [DB](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Module/DB/DBComponentSystem.cs)、[HTTP](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Module/Http/HttpComponentSystem.cs)：单文档保存、非分布式锁、监听与流关闭 |
| [hot-reload](../../openspec/specs/hot-reload/spec.md) | 2 / 4 | [CodeLoader](../../Unity/Assets/Scripts/Game/ET/Loader/CodeLoader.cs)、[AOT](../../Unity/Assets/Scripts/Game/HybridCLR/HybridCLRHelper.cs)：装载条件、Reload 不重启入口、返回码未检查 |
| [reactive-ui](../../openspec/specs/reactive-ui/spec.md) | 3 / 6 | [生成器](../../Share/SourceGenerator/Generator/ETReactiveSystemGenerator/ETReactiveSystemGenerator.cs)：首次投影、合并多源变化、次数节流、Reset 与版本集合 |
| [editor-tools](../../openspec/specs/editor-tools/spec.md) | 1 / 3 | [UI 生成器](../../Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/UIFormCodeCreator.cs)、[Entity 生成器](../../Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/UGFEntityCodeCreator.cs)：拒绝覆盖、部分输出、重载挂载、交付未完成项 |
| [dependencies](../../openspec/specs/dependencies/spec.md) | 2 / 4 | [Model asmdef](../../Unity/Assets/Scripts/Game/ET/Code/Model/Game.ET.Code.Model.asmdef)、[DotNet](../../DotNet/Model/DotNet.Model.csproj)、[第三方](../../DotNet/ThirdParty/DotNet.ThirdParty.csproj)：宏/引擎边界、共享源链接、缓存前置 |

## 对发现的限制如何处理

本轮补充 [KnownIssues](../../KnownIssues.md) 中的资源回调失效范围、字典字节切片/部分写入、AOT 返回码检查缺口。已有 Luban 零退出码、UIEntity 同步泛型路由等问题仍保留。规格明确“当前没有哪种保证”，不会把扫描变成修复授权。

框架 API 名只用于调用者可观察边界；操作步骤保持在 Book，新增代码约定保持在 references。两项协作/自动测试主规格及两个已有 change 没有重写。未来做饭设计、菜单候选和暂停/断线/传输裁决保持原状态。

## 逐字改动与来源

- `AGENTS.md` 没有改动，无新增或删除规则。
- 十四份主规格保留全部原要求、场景和 Sources；仅修订“ET 固定入口”补 UNITY_ET 前提，新增一项对应退出场景，其余均追加。
- 原主规格正文沿用本项目已有文本。本轮新增要求由当前源码/配置核对后撰写，没有从外部项目复制新业务规则；同步只把本 change 的增量内容迁入主规格，并转换 Sources 的相对层级。
- 当前进度页改为十二项官方 OpenSpec、四项项目技能、官方 unity-cli 与四项 SOP 技能，合计二十一项；已迁入的配套与尚缺的独立工具分开列出。
- 模块索引与规格索引增加本页入口；相关 references 补充边界提示并链接主规格/已知问题，避免形成第二份完整契约。

静态扫描与规格细化完成，不等于新增玩法或所有存量代码满足新增开发约定；动态并发、导表、构建、AOT、联网与产品验收未运行。
