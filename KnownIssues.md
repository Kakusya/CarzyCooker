# 已知问题与验证边界

以下来自当前 main 分支的 ET 底座的源码/手册静态核对；本次未修运行时代码、未运行 Unity 或产品测试。

| 事项 | 核实事实 | 后续入口 |
|---|---|---|
| 分支与历史手册差异 | 当前 ET 的 ProcedurePreset 直接进入 ET，GameHot 源码/配置与 GF Network 运行时已移除；README/Book/DefineSymbol 工具还有双模式文字或旧条件分支 | 相关手册有 ET 适用说明；不按历史章节或残留宏恢复已删除模块 |
| Entity 骨架手册路径过时 | `UGFEntityCodeCreator.cs` 输出 `ModelView/Client/Game/UGFEntity/<Name>/UGFEntity<Name>.cs`、MonoUGFEntity 与对应 HotfixView System；原手册曾写 GFEntity | 本次修正文档和技能，生成器保持 |
| ID 开关与 active 不同 | ExcelExporter.Luban.cs 的 IsEnableET/IsEnableGameHot 按直接子目录名称置 true；Luban cmds 才受 active 控制 | 当前 main 分支的 ET 底座没有 GameHot 目录；不把导出器旧分支当作支持双模式，其他调用仍按日志与实际输出核对 |
| Luban 失败不一定非零退出 | DoExport 汇总失败只 Log.Warning，Share.Tool 的 ExcelExporter 分支随后 return 0；非 Check 仍执行复制/二次生成 | 使用日志与目标 diff 判断；单独 bug 任务才修实现 |
| 同步 UIEntity 泛型 overload 路由 | Game/Entity/EntityExtension.cs 的 ShowUIEntity<T> 调用 ShowEntity；Type overload 才查 DTUIEntity | 本次不修。采用接口前核对实现，异步路径另查真实 helper |
| Proto 手册漏 ET-Admin | Design/Proto/ET-Admin/proto.conf active=true、ET、startOpcode=30000，输出 DotNet/Model/Generate/Message | 本次补文档；不同服务/生成域的重叠编号不能简单合成一份全局区间 |
| 已配置依赖不等于可编译 | manifest 部分 Git 依赖浮动；DotNet.ThirdParty 链接 Unity PackageCache 数学源码，第三方/付费插件和 Editor 状态未验证 | 当前版本由项目配置/锁文件与实际环境确定，不自动升级/补依赖 |
| 历史原件引用失效 | 两份原件含旧工程路径与链接 | 保留原件；现行路由仅在 AGENTS/references/Docs/OpenSpec，不沿旧路径执行 |

## 2026-10-09 规格深度扫描补充

以下是代码静态确认的边界；没有执行故障复现或运行时修复。完整场景和来源见 [扫描记录](Docs/AI/spec-scan.md) 与对应主规格。

| 事项 | 核实事实 | 使用与后续边界 |
|---|---|---|
| 回调式资源加载的退出范围 | ResourceContainer.m_Version 只在 Create 时递增；LoadAsset 成功回调检查该版本，但 UnloadAllAssets 不改版本，也不取消回调式请求。异步入口另外使用 CTS | 版本变化能拦截容器复用后的成功回调；不能据此保证同一容器只执行 UnloadAllAssets 后的迟到结果、失败或进度回调均失效。使用者需核对真实退出路径，修复另行授权 |
| 字典字节切片参数未消费 | LubanLocalizationHelper 字节 ParseData 接收 startIndex/length，却直接用完整 dictionaryBytes 构造 ByteBuf | 当前完整资源入口不因此被声明失败；不能保证任意子区间解析正确，不将切片支持写成现有能力 |
| 字典切换与解析不是事务 | LoadLanguageAsync 先清旧字典再初始化内置字典；ParseData 逐键 AddRawString，失败只返回 false，没有撤销先前键 | 外部加载或解析失败后不保证恢复旧字典，也不保证失败前零写入；具体恢复策略须另行设计 |
| AOT 元数据返回码未检查 | HybridCLRHelper 逐项调用 LoadMetadataForAOTAssembly，却不检查返回的 LoadImageErrorCode；资源卸载也未置于异常收尾中 | 结束日志不足以证明全部元数据成功，异常时不保证配置资源已卸载。本轮仅补规格和限制，Player/AOT 验证仍 NotRun |

源码：[ResourceContainer](Unity/Assets/Scripts/Game/Container/ResourceContainer.cs)、[字典 helper](Unity/Assets/Scripts/Game/Localization/LubanLocalizationHelper.cs)、[语言加载](Unity/Assets/Scripts/Game/Localization/LocalizationExtension.cs)、[AOT helper](Unity/Assets/Scripts/Game/HybridCLR/HybridCLRHelper.cs)。

暂停命令、断线手持物、NPC 收口、跨关许可和传输选择属于 [产品待裁决](Docs/Product/backlog.md)，不是当前底座 bug 已被证明。
