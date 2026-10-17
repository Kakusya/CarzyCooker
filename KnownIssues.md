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
| Console 压缩脚本误标普通日志 | compact_console.py 未先过滤原始级别，error_message_compactor 对 Log 默认标为 Error；普通 Initialize 日志已复现误标 | 诊断时核对原始 level/logType 与 console_status.groundTruth，不据压缩标签判断失败；Tools/AutoTesting 的原始级别校验保持。见[本轮记录](openspec/changes/archive/2026-10-13-product-local-message-boundaries/execution-notes.md)，技能修复另行处理 |
| 示例业务数据清空后的使用边界 | UIEntity／Scene／音频／Unit／AI 表保留类型、无数据，DTDemo／OneConfig 整表删除；旧登录页与 Product Solo 地图曾分别验证，普通 Launcher 现改为默认 Solo 并通过直接 Play／HUD 操作；旧 Demo 代码硬编码的单位 1001／AI 配置不再存在 | 旧入口仅显式 legacy 可用，单位／AI 演示不再可用；保留初始化链路不表示继续支持旧示例玩法。新业务按实际需求注册配置，不回填演示行 |

## 2026-10-09 规格深度扫描补充

以下是代码静态确认的边界；没有执行故障复现或运行时修复。完整场景和来源见 [扫描记录](Docs/AI/spec-scan.md) 与对应主规格。

| 事项 | 核实事实 | 使用与后续边界 |
|---|---|---|
| 字典字节切片参数未消费 | LubanLocalizationHelper 字节 ParseData 接收 startIndex/length，却直接用完整 dictionaryBytes 构造 ByteBuf | 当前完整资源入口不因此被声明失败；不能保证任意子区间解析正确，不将切片支持写成现有能力 |
| 字典切换与解析不是事务 | LoadLanguageAsync 先清旧字典再初始化内置字典；ParseData 逐键 AddRawString，失败只返回 false，没有撤销先前键 | 外部加载或解析失败后不保证恢复旧字典，也不保证失败前零写入；具体恢复策略须另行设计 |
| AOT 元数据返回码未检查 | HybridCLRHelper 逐项调用 LoadMetadataForAOTAssembly，仍未检查 LoadImageErrorCode；所属资源加载和异常/退出收尾已接入 ReadAssetAsync | 结束日志仍不足以证明全部元数据成功；本次只修资源生命周期，Player/AOT 验证仍 NotRun |

源码：[ResourceContainer](Unity/Assets/Scripts/Game/Container/ResourceContainer.cs)、[字典 helper](Unity/Assets/Scripts/Game/Localization/LubanLocalizationHelper.cs)、[语言加载](Unity/Assets/Scripts/Game/Localization/LocalizationExtension.cs)、[AOT helper](Unity/Assets/Scripts/Game/HybridCLR/HybridCLRHelper.cs)。

跨关许可缩小已裁决并在[许可案](openspec/changes/archive/2026-10-15-restaurant-unlock-permissions/execution-notes.md)实施；正式传输选择仍属[产品待裁决](Docs/Product/backlog.md)，不是当前底座bug已被证明。单机暂停取消未执行动作并冻结已开始工作、前厅自然收尾已按用户裁决实施，真实顾客／伙伴及最后洗碗、零星两关均已验收，见[关卡流程](openspec/changes/archive/2026-10-13-solo-restaurant-level-flow/execution-notes.md)。有限供应／运输／实物拆份已验收，见[供应记录](openspec/changes/archive/2026-10-14-solo-supply-delivery/execution-notes.md)。PC Solo 成功检查点7/7已验收，普通入口选档／加载取消、实际文件失败重试及保存后Stop/Play重入均通过；Solo仍仅本设备主动新建、不覆盖；[合作基线读取与离线授权](openspec/changes/archive/2026-10-15-cooperative-save-baseline/execution-notes.md)已接入且不改源文件，[个人合作结果领取](openspec/changes/archive/2026-10-15-cooperative-result-claims/execution-notes.md)已接入，该次交付格式为v9；现行格式及适配范围统一查[根进度](CarzyCooker当前进度.md)和[成功文件规格](openspec/specs/solo-save-progress/spec.md)；[授权文件导入与显式替换](openspec/changes/archive/2026-10-15-save-branch-import-and-replace/execution-notes.md)已交付，保持目标Owner、验证当前修订与有损确认且无旧备份。重复返回原Slot/revision元数据，不能恢复后续已覆盖载荷，见[检查点记录](openspec/changes/archive/2026-10-14-solo-success-checkpoint/execution-notes.md)。当前色块已替换为统一餐厅精灵，选中／加工与前厅标签仍有密集场景；新美术已通过相关 UTF 与普通 540×960 制作交付，见[美术记录](openspec/changes/archive/2026-10-15-restaurant-art-production/execution-notes.md)。图集暂用 RGBA32，Android 纹理压缩、手机／Player／AOT及LAN／公网未验收。断线手持物已按[格子样机change](openspec/changes/archive/2026-10-11-portrait-grid-kitchen-prototype/design.md)裁决。

首批沙拉/精确组装及半作业保存已交付，现有原盘内容/单执行者为事实来源；有损替换仍只处理成功记录。新菜单资源批次先有两沙拉的三关入口，两个各三关内容大关已由章节案交付，不把资源批次数量当额外首发大关。现行协议、文件、图集和验收范围统一查[根进度](CarzyCooker当前进度.md)；沙拉范围及真实运行见[组装记录](openspec/changes/archive/2026-10-15-kitchen-multi-input-assembly/execution-notes.md)，甜品见下文。

有限扩建9×18与一张单客桌已通过本轮范围的真实文件和普通Launcher Solo/合作验收；旧档仍为原尺寸。具体边界、原始失败及最新有效结果见[扩建记录](openspec/changes/archive/2026-10-15-restaurant-space-expansion/execution-notes.md)，不代表真机/Player/物理网络已验收。

首批热餐已交付，吐司仍可独立出餐，也可作为已定义早餐的真实组件；只允许配方规定的部分组合，错误混装及第二份吐司仍拒绝。热餐为既有模式/菜单变体，不另算第三个内容章，两个已交付内容章及原档关链不改；新菜整体编入后续新开内容时仍须保留旧Level/Next/库存语义。真实文件和普通两菜两模式范围见[热餐记录](openspec/changes/archive/2026-10-15-kitchen-hot-food-batch/execution-notes.md)。

小蛋糕采用有限两份P38面糊和限定单层模具关系，原模具只持面糊或熟蛋糕，不与盆/盘嵌套；烤熟后须显式脱模到空甜品盘。未熟、未脱模及满盘均不能绕过出餐；文件v11在原烘焙记录末尾追加载具键，旧v10直接煎制默认0且不补本批物资。只交付S04，不覆盖候选其他甜品、温控、发酵或冷却；范围见[甜品记录](openspec/changes/archive/2026-10-15-kitchen-dessert-batch/execution-notes.md)。

首批咖啡仅浓缩、热拿铁、卡布奇诺，热饮杯容量三份，奶泡必须最后；沿有限豆/牛奶/杯供应，不覆盖其他候选饮品。新开第二章映射1501/2501，旧1201/2201成功档仍原Next链、不补咖啡库存。当前兼容与未验证边界见[咖啡记录](openspec/changes/archive/2026-10-15-kitchen-beverage-batch/execution-notes.md)。

固定伙伴首批只保留“小麦”一位，三类准备态聊天和已提交工作／成长提示均只读；人数、更多岗位／能力和人物剧情仍不在本次范围。未新增人物存档或协议版本，关内成长仍换关重置；见[伙伴记录](openspec/changes/archive/2026-10-15-fixed-companion-presence/execution-notes.md)。
# 茶饮清理边界

透明冷饮杯与调饮壶允许按原设施分别排液或处理固体，保留另一物态及原壳；仅为这两类载具的明确例外，其他混合容器仍拒绝。清理后存量可保存，但继续加料仍遵守严格配方顺序；不退款或补回批次份数。茶饮参数为首包可玩配置，正式平衡与设备手感未验收。
