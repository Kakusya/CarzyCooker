# Execution Notes

## 实施交接与验证取舍（2026-10-15）

00–07已经交付。本案从文件v6/wire27接入，沿原ManualProcess使用明确Assembly方式、稳定RecipeId和原盘内容；完整预检、同Tick唯一新输出，停手保留原盘/材料/进度。已开始的盘内容锁定，仍可搬运及交接；明确清理会按原设施规则取消原作业，不私建库存。

共享GridManualMode及新增菜单值由Excel枚举生成，新增有限值不裸写业务魔数；旧MenuVariant整数边界集中验证，0语义不变。原盘扩大到四份输入，成品独占字段保留原吐司单份拒绝及成品出餐身份。新增字段/配置语法采用文件v7、wire28，旧v1–v6只读兼容；未完成Assembly无需复制第二份投入列表，原盘内实物为唯一事实。

主要风险是精确份数、唯一消费、停手交接和新菜单遗漏消费者；选配置/本地KCP/真实临时文件及普通Launcher UI小集逐项验证。不全仓测试、不改用户档、不做真机/Player/AOT/物理LAN。出现重复产出、实物丢失或存档不能接续则修复后补对应失败用例。其余原功能复用未变证据，扩大盘容量改变的原吐司拒绝须补跑相关用例。

工作簿沿现有格式使用可用artifact-tool编辑10份源，保留既有工作表和配置约定；五目标Luban Check/Gen日志为Unity/Temp/Assembly-luban-check.log、Assembly-luban-gen.log，均出现明确success。Proto原生成器日志Assembly-proto-gen.log。没有手改派生输出。首轮配置UTF 1 Passed；两项运行用例因新增台面占用已有格被真实布局校验拒绝，GridAssembly-core-UTF.log保留。源布局已修正并重新Check/Gen，运行复验尚未完成。

美术沿可编辑export_art.py/Blender模型源扩展生菜、切片、黄瓜、芝士、两种沙拉和包装，重新生成图集/布局；无需新图像服务或另造占位资产。以下规划记录仍为历史，不表示缺少全部实施授权。

## 2026-10-15 规划优化

本轮仅修订未开工计划，统一已确认范围、真实前置和逐功能验收；以下早期“仅规划/待裁决”措辞属于创建记录，当前交接以本节、design与tasks为准。没有实施业务或执行UTF/Editor/构建，功能结果仍为NotRun。本案严格格式、正文/引用、任务到验收映射检查通过；全批汇总见[路线记录](../../../../Docs/Product/full-version-roadmap.md)。

## 当前状态与范围

2026-10-15：用户授权继续扫描业务、按优先级建立 change，明确不考虑真机实测，并确认“按已确认目标，菜品分批扩展”。本案只完成规划，任务未勾选；功能实现、UTF、Unity Editor 运行、改表/导表、构建均 **NotRun**。规划检查结果统一见[本轮路线记录](../../../../Docs/Product/full-version-roadmap.md#本轮规划检查)。

PC LAN 与 Android 原案保留，不是本案依赖。没有提交、push、归档或操作第二工作区；此前授权的美术提交不扩展成本轮业务实施许可。

## 严谨程度与已接受边界

多输入易重复消费或在失败时丢料。相关 UTF 验证数量、错料拒绝、多人抢料、半组装交接与保存恢复；用普通 Editor 实际制作交付首批菜。

只选择实际改变行为的最小相关 PlayMode UTF 与普通 Editor 入口；可复用下列已交付主规格关联的原证据，前提是实现、消费者和验收点均未变。 不建立全目录矩阵、额外状态副本或通用测试平台。

接受未验证：真机、设备输入、Player/AOT、物理 LAN/公网不在此次规划内；全目录平衡、未选择内容批次及跨设备持久故障不由本案证明。 需要相关产品交付时另定范围；本案新增/修改行为或出现具体回归时补相应最小验证，不因整理/审查重复跑旧套件。

## 创建时未来验收映射（历史）

| 任务/功能 | 最小验证与断言 | 当前结果 |
| --- | --- | --- |
| 1.1 配方及配置 | 配置 UTF及Check/生成：两菜完整生产闭包、无效数量/依赖拒绝。 | NotRun |
| 2.1 组装与交接 | 本地/KCP UTF：拒绝无损、唯一消费、半组装交接和投影独立。 | NotRun |
| 3.1 菜单入口与持久化 | UI/文件 UTF：两菜可达、提示准确、半组装恢复及旧档不补料。 | NotRun |
| 4.1 普通入口集成 | 普通 Editor：两菜在 Solo/合作均完成合法交付；记录对应结果。 | NotRun |

同一运行可覆盖多行；先发现真实 PlayMode 用例并记录过滤范围，再执行改变行为的小集。任务中写的是验收目标，不是已经存在或已通过的测试。已有通过结果只在实现和验收点不变时复用，缺口补跑；失败、空集、Skipped及缺输出仍未验收。

## 可复用的既有合同

已有运行记录：[2026-10-13-solo-dish-recipe-chain](../2026-10-13-solo-dish-recipe-chain/execution-notes.md)、[kitchen-container-extraction](../2026-10-15-kitchen-container-extraction/execution-notes.md)。只复用未变的原行为；本案新增功能不因引用它们而通过。


- [dish-recipes](../../../specs/dish-recipes/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。
- [kitchen-item-processing](../../../specs/kitchen-item-processing/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。

## 实施交接

2026-10-15 已确认首批 F01 田园沙拉、F02 芝士蔬菜沙拉，按目录配方收敛。具体配置 ID、耗时及布局是实施参数；其余目录不属于首批。

参数按配置和实际消费者核对；运行后回填真实方法、结果、原日志路径和失败原因，不保存额外业务状态副本。


### 已运行的组装小集

- `GridAssembly-layout-clear-UTF.log`：配置及真实KCP两项Passed；保存用例当时因旧1800Tick观察上限不足停在Draining，真实失败保留。新菜单1200营业Tick/600等餐Tick，改为本用例4000Tick有限自然收口观察，不改玩法状态或全局旧测试。
- `GridAssembly-file-UTF.log`：Solo及实际合作两例Passed。F02精确四料、F01不能吃掉多余芝士；半组装停止后自然经营收口，Solo提交或Host证明资格后的真实签名票/Root持久化/主动领取保存。重新从实际文件创建下一关、原盘四输入及进度保持、无新补料，接续只产一份F02；原文件字节不变。
- 生菜台最终(1,15)、黄瓜台(5,16)、芝士台(3,13)，已避开原备料台、食材台及烤箱两格footprint；各菜单的真实创建校验通过。原两轮布局失败输出分别为GridAssembly-core-UTF.log、GridAssembly-layout-fixed-UTF.log。
- 原Blender源生成214切片、31个物品独立图形，日志Assembly-art-export.log明确EXPORT COMPLETE及exit0；图集已目视核对新增原料、切配料、两成品和包装；Board/HUD使用生成的ArtItemCount，未改生成C#。普通界面制作交付仍在运行。

### 普通Launcher及供给/旧档结果

- `GridAssembly-UI-UTF.log` 四项全部Passed（140.37秒）：GridAssemblyUiSoloF01、SoloF02、CooperativeF01、CooperativeF02。均从普通Launcher的真实模式/选档或Host房间入口选择新菜单，通过实际指针切配、加料、拿盘、明确组装、停止、关闭重开/原进度接续和订单交付。合作为Host切番茄、独立Fiber真实KCP队友切生菜/黄瓜、加芝士、组装及交付；每道每种模式都实际Served、10分、原盘仍存在。旧“继续组装”回调不提交，菜谱实际缺口随盘内内容更新。没有用测试夹具注入成品。
- `GridAssembly-supply-legacy-UTF.log` 两项Passed：有限生菜包装采购扣一包、真实运输/接收两份原料、同一实物拆一份，未免费产生切配料或成品；实际28成员v6基础档只读读取并创建下一关，物品数保持、不增加沙拉台/物/供应，原字节不变。v1–v5适配未变，复用05–07已运行的真实旧布局证据。
- 10份工作簿对照原有工作簿的工作表/显隐、公式、数据验证、合并与窗格检查均保持；新增行/列为本案明确源变更，自动产生的旁路inspect文件已删除。不增加证据JSON或业务状态副本。

### 收口与逐功能结果

- 原两菜配置/有限原料、原吐司满盘拒绝、全部31物品/四方向与分层表现各1/1 Passed，原日志GridAssembly-old-menu-UTF.log、GridAssembly-old-toast-UTF.log、GridAssembly-art-UTF.log。吐司用例的第二片面包夹具由旧3,12（已被饮品布局使用）移到无占用2,16，实际烘烤两份后断言第二次追加拒绝，业务规则没有绕过。
- 新增同类两份的真实KCP断言及中文拒绝提示后，只补对应两个用例，GridAssembly-quantity-KCP-UTF.log、GridAssembly-UI-rejection-UTF.log均1/1 Passed；不重复其余稳定套件。界面确实呈现“盘内材料或份数不匹配”，失败保留盘内番茄。
- 合计10个新增（6核心/文件、4UI）和3个受变回归，共13个不同用例最新有效结果Passed。普通入口四例复用其真实完成证据，不因整理再跑。2026-10-15 08:14:05 UTC直接Console groundTruth原生Error/Warning各0、编译成功；累计历史计数未清理。

| 任务/功能 | 实际运行映射 | 结果 |
| --- | --- | --- |
| 1.1 精确配方/容量/供应/闭包 | ConfigurationRequiresExactCounts…、NewSupplyUsesFiniteRawPackages…、原两菜与吐司回归；Luban五目标Check/Gen | Passed |
| 2.1 单执行者/唯一消费/交接/投影 | RealKcpSharedCutting…（含实际双份配方不足拒绝），四项普通UI的停手与续作 | Passed |
| 3.1 菜单/供给/菜谱/表现/保存 | 四项普通Launcher UI、F02文件False/True、V6BaseFile…、Supply…和31物品图形；全部源与消费者同步 | Passed |
| 4.1 两菜两模式实际交付 | GridAssemblyUiSoloF01/F02、CooperativeF01/F02，各真实Served、10分、保留原盘 | Passed |

本案未做真机/物理LAN/Player/AOT或产品构建，未提交/push/归档或操作第二工作区。下列创建时NotRun表仅保留历史。

### 文档与格式

本案及主规格47/47严格检查通过；15份受影响UTF-8文档及本地链接无缺失。git diff --check未通过：新增Luban字段的原模板产生行尾空白，全部定位到Generate/Luban派生C#；手写源/文档无此错误。按生成物只读规则不手改，也不为格式清理改全局模板或扩大生成范围；此格式缺口保留，功能/严格规格结果不变。既有CRLF归一提示保留。主规格只合入本案三个新增Requirement，保留原场景，无归档。
