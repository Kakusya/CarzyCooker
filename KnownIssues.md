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

暂停命令、断线手持物、NPC 收口、跨关许可和传输选择属于 [产品待裁决](Docs/Product/backlog.md)，不是当前底座 bug 已被证明。
