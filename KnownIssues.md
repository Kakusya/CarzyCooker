# 已知问题与验证边界

以下记录当前 main 分支的 ET 底座仍存在的限制。初始清单来自源码/手册静态核对；后续已修问题直接移除，实际验证范围见对应 change 的执行说明。

| 事项 | 核实事实 | 后续入口 |
|---|---|---|
| 分支与历史手册差异 | 当前 ET 的 ProcedurePreset 直接进入 ET，GameHot 源码/配置与 GF Network 运行时已移除；README/Book/DefineSymbol 工具还有双模式文字或旧条件分支 | 相关手册有 ET 适用说明；不按历史章节或残留宏恢复已删除模块 |
| Entity 骨架手册路径过时 | `UGFEntityCodeCreator.cs` 输出 `ModelView/Client/Game/UGFEntity/<Name>/UGFEntity<Name>.cs`、MonoUGFEntity 与对应 HotfixView System；原手册曾写 GFEntity | 本次修正文档和技能，生成器保持 |
| ID 开关与 active 不同 | ExcelExporter.Luban.cs 的 IsEnableET/IsEnableGameHot 按直接子目录名称置 true；Luban cmds 才受 active 控制 | 当前 main 分支的 ET 底座没有 GameHot 目录；不把导出器旧分支当作支持双模式，其他调用仍按日志与实际输出核对 |
| Proto 手册漏 ET-Admin | Design/Proto/ET-Admin/proto.conf active=true、ET、startOpcode=30000，输出 DotNet/Model/Generate/Message | 本次补文档；不同服务/生成域的重叠编号不能简单合成一份全局区间 |
| 已配置依赖不等于可编译 | manifest 部分 Git 依赖浮动；DotNet.ThirdParty 链接 Unity PackageCache 数学源码，第三方/付费插件和 Editor 状态未验证 | 当前版本由项目配置/锁文件与实际环境确定，不自动升级/补依赖 |
| 历史原件引用失效 | 两份原件含旧工程路径与链接 | 保留原件；现行路由仅在 AGENTS/references/Docs/OpenSpec，不沿旧路径执行 |
| 示例业务数据清空后的使用边界 | UIEntity／Scene／音频／Unit／AI 表保留类型、无数据，DTDemo／OneConfig 整表删除；旧登录页与 Product Solo 地图曾分别验证，普通 Launcher 现改为默认 Solo 并通过直接 Play／HUD 操作；旧 Demo 代码硬编码的单位 1001／AI 配置不再存在 | 旧入口仅显式 legacy 可用，单位／AI 演示不再可用；保留初始化链路不表示继续支持旧示例玩法。新业务按实际需求注册配置，不回填演示行 |

## 2026-10-09 规格深度扫描补充

以下是代码静态确认的边界；没有执行故障复现或运行时修复。完整场景和来源见 [扫描记录](Docs/AI/spec-scan.md) 与对应主规格。

| 事项 | 核实事实 | 使用与后续边界 |
|---|---|---|
| 字典字节切片参数未消费 | LubanLocalizationHelper 字节 ParseData 接收 startIndex/length，却直接用完整 dictionaryBytes 构造 ByteBuf | 当前完整资源入口不因此被声明失败；不能保证任意子区间解析正确，不将切片支持写成现有能力 |
| 字典切换与解析不是事务 | LoadLanguageAsync 先清旧字典再初始化内置字典；ParseData 逐键 AddRawString，失败只返回 false，没有撤销先前键 | 外部加载或解析失败后不保证恢复旧字典，也不保证失败前零写入；具体恢复策略须另行设计 |
| AOT 元数据返回码未检查 | HybridCLRHelper 逐项调用 LoadMetadataForAOTAssembly，仍未检查 LoadImageErrorCode；所属资源加载和异常/退出收尾已接入 ReadAssetAsync | 结束日志仍不足以证明全部元数据成功；本次只修资源生命周期，Player/AOT 验证仍 NotRun |

源码：[ResourceContainer](Unity/Assets/Scripts/Game/Container/ResourceContainer.cs)、[字典 helper](Unity/Assets/Scripts/Game/Localization/LubanLocalizationHelper.cs)、[语言加载](Unity/Assets/Scripts/Game/Localization/LocalizationExtension.cs)、[AOT helper](Unity/Assets/Scripts/Game/HybridCLR/HybridCLRHelper.cs)。

跨关许可和正式传输选择仍属[产品待裁决](Docs/Product/backlog.md)，不是当前底座bug已被证明。单机暂停取消未执行动作并冻结已开始工作、前厅自然收尾已按用户裁决实施，真实顾客／伙伴及最后洗碗、零星两关均已验收，见[关卡流程](openspec/changes/archive/2026-10-13-solo-restaurant-level-flow/execution-notes.md)。有限供应／运输／实物拆份已验收，见[供应记录](openspec/changes/solo-supply-delivery/execution-notes.md)。PC Solo 成功检查点7/7已验收，普通入口选档／加载取消、实际文件失败重试及保存后Stop/Play重入均通过；仅本设备主动新建，不覆盖、无授权／跨设备／联机领取，见[检查点记录](openspec/changes/solo-success-checkpoint/execution-notes.md)。Board标签较密、基础美术保留，手机／Player／AOT及LAN／公网未验收。断线手持物已按[格子样机change](openspec/changes/archive/2026-10-11-portrait-grid-kitchen-prototype/design.md)裁决。
