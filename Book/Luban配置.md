# Luban 配置与导表

> **ET 底座适用说明（2026-10-09）**：当前 ProcedurePreset 直接进入 ET；GameHot 源码、配置和 GF Network 运行时已移除。本文保留的 GameHot/纯 GF 示例与双模式切换段落仅作历史参考，不是当前可执行流程。ET、GF 公共组件与 HybridCLR 的适用部分仍以当前源码/配置为准。

GDK 在 Luban 之上增加了多工程发现、并行执行、路径变量、产物复制和二次代码生成。日常开发只维护 Excel 与各工程的 `luban.conf`，不需要手写完整命令行。

## 导出链路

```text
Design/Excel/*/luban.conf
  → Share.Tool 扫描 active 工程
  → 并行执行每条 Luban cmd
  → 复制多目标产物
  → 生成 UI / Entity / Scene / Sound 常量
  → 导出 Localization.xlsx
```

导出实现位于 `Share/Tool/ExcelExporter/`。Luban 可执行文件与自定义模板位于 `Tools/Luban/`。

## 前置条件

先在仓库根目录编译工具：

```powershell
dotnet build Kit.sln
```

构建后应存在 `Bin/Tool.exe`（Windows）或 `Bin/Tool.dll`。Unity 菜单和批处理最终都调用这个工具。

## 执行方式

### Unity 菜单

| 入口 | 输出格式 |
| --- | --- |
| `Game/Tool/ExcelExporter` | 代码 + 二进制数据 |
| `Game/Tool/ExcelExporterForJson` | 代码 + JSON 数据 |
| Toolbar 的 `ExportExcel` | 与 ExcelExporter 相同 |

两个 Unity 导表菜单只在工具成功退出后刷新 AssetDatabase、清理 UXTool 的编辑器本地化缓存并恢复选择对象。启动异常或非零退出会记录失败原因并结束菜单流程，不执行这些后续步骤。

### 修改 Excel 源表

按当前 ET 工程的工作簿和实际表头编辑源表，保留公式、样式与数据验证；先核实文件未被其他进程占用。现有 ExcelExporter 负责校验、导出及 ID/本地化派生。专用自动改表工具尚未批准，Unity CLI/Pipeline 不自动提供工作簿语义编辑能力。

### 批处理

```text
Design/Excel/gen all bin.bat
Design/Excel/gen all json.bat
```

批处理会切换到 `Bin/` 再运行工具，因此相对路径能正确解析。

### 命令行

```powershell
Push-Location Bin
./Tool.exe --AppType=ExcelExporter --Console=1
./Tool.exe --AppType=ExcelExporter --Console=1 --Customs=Json
Pop-Location
```

macOS 或 Linux 使用 `dotnet Tool.dll`。

## 工程发现规则

工具只扫描 `Design/Excel/` 的直接子目录。目录中必须存在 `luban.conf`，且 `active` 为 `true`，其中的 `cmds` 才会执行。

当前工程包含：

| 工程 | 用途 | 模式 |
| --- | --- | --- |
| `Design/Excel/ET` | ET Client、ClientServer、Editor 与 GF 公共数据 | `UNITY_ET` |
| `Design/Excel/GameHot` | GameHot、Editor 与 GF 公共数据 | `UNITY_GAMEHOT` |

`Game/Define Symbol` 会切换两个工程的 `active`。不要同时启用两个会写入相同输出目录的工程，否则并行任务可能互相覆盖。

## `luban.conf` 结构

文件同时包含标准 Luban 配置和 GDK 扩展字段。

### GDK 扩展字段

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `active` | bool | 是否参与本次导出 |
| `customTemplate` | string | `Tools/Luban/CustomTemplates/` 下的模板目录，空值使用 `Default` |
| `cmds` | string[] | 每个元素是一条 Luban 导出命令 |

工具会自动为每条命令补上 Luban DLL、模板目录、当前 `luban.conf` 和默认本地化参数。

### 标准 Luban 字段

`groups`、`schemaFiles`、`dataDir`、`targets` 等由 Luban 自身读取。目标名称通过命令中的 `-t` 选择，例如 `client`、`clientserver`、`editor`。

```json
{
  "active": true,
  "customTemplate": "Default",
  "cmds": [
    "-t client -c cs-bin -d bin -x outputCodeDir=%UNITY_ASSETS%/Scripts/Game/ET/Code/Model/Generate/Client/Luban -x outputDataDir=%UNITY_ASSETS%/Res/ET/Client/Luban"
  ]
}
```

实际配置可换行书写，工具读取时会把换行与制表符整理成单行命令。

## 路径变量

| 变量 | 展开结果 |
| --- | --- |
| `%CONF_ROOT%` | 当前 `luban.conf` 所在目录 |
| `%UNITY_ASSETS%` | `Unity/Assets` 的绝对路径 |
| `%ROOT%` | 仓库根目录的绝对路径 |
| `%GEN_CLIENT%` | `Tools/Luban/Tools/Luban/Luban.dll` |
| `%CUSTOM_TEMPLATE_DIR%` | 当前自定义模板目录 |

路径会统一转换为 `/`。仓库根路径不能包含空格，Share.Tool 启动时会直接拒绝这种工作目录。

## 当前输出

### 公共 GF 数据

| 目标 | 代码 | 数据 |
| --- | --- | --- |
| `gameclient` | `Unity/Assets/Scripts/Game/Generate/Luban` | `Unity/Assets/Res/Luban` |
| `gameeditor` | `Unity/Assets/Scripts/Game/Editor/Generate/Luban` | `Unity/Assets/Res/Editor/Luban` |

公共 Editor JSON 是 UI、Entity、Scene、Sound 常量生成器的输入。

### ET 数据

| 目标 | 代码 | 数据 |
| --- | --- | --- |
| `client` | `Game/ET/Code/Model/Generate/Client/Luban` | `Assets/Res/ET/Client/Luban` |
| `clientserver` | `Game/ET/Code/Model/Generate/ClientServer/Luban` | `Assets/Res/ET/ClientServer/Luban`、`Config/Luban` |
| `editor` | `Game/ET/Editor/Generate/Luban` | `Assets/Res/Editor/ET/Luban` |

`clientserver` 的数据会先生成到 Unity，再复制到根目录 `Config/Luban`，供 `Bin/App.dll` 的服务端读取。

### GameHot 数据

| 目标 | 代码 | 数据 |
| --- | --- | --- |
| `client` | `Game/Hot/Code/Generate/Luban` | `Assets/Res/Hot/Luban` |
| `editor` | `Game/Hot/Code/Editor/Generate/Luban` | `Assets/Res/Editor/Hot/Luban` |

## 多目标输出

`outputCodeDir` 或 `outputDataDir` 可用逗号分隔多个路径。工具以第一个目录为源，所有 Luban 子命令成功后清空并复制到其余目录。

```text
-x outputDataDir=%UNITY_ASSETS%/Res/ET/ClientServer/Luban,%ROOT%/Config/Luban
```

复制会忽略隐藏文件、临时文件和 `.meta`，并清除空目录。不要把人工维护文件放进目标目录。

## 二次生成

所有 Luban 命令成功且输出读取完成后，非 Check 的 ExcelExporter 会读取 `Assets/Res/Editor/Luban` 中的 JSON，并生成：

| 数据表 | 生成常量 |
| --- | --- |
| UI | `UIFormId` / `UGFUIFormId` |
| Entity | `EntityId` / `UGFEntityId` |
| UIEntity | `UIEntityId` / `UGFUIEntityId` |
| Scene | `SceneId` / `UGFSceneId` |
| Sound | `SoundId` / `UGFSoundId` |

生成器位于 `Share/Tool/ExcelExporter/Generate/`。需要增加新的导表后处理时，应在 Share.Tool 中扩展，不要修改生成文件。

## `Customs` 选项

`--Customs` 是一个字符串，工具按不区分大小写的包含关系启用选项。可组合传入，例如 `--Customs=Check,ShowCmd`。

| 选项 | 行为 |
| --- | --- |
| `Json` | 将 `cs-bin/bin` 替换为 `cs-simple-json/json` |
| `Check` | 只校验配置，不写输出；同时跳过本地化导出 |
| `ShowCmd` | 打印展开后的完整 Luban 命令 |
| `ShowInfo` | 显示 Luban 标准输出 |

校验示例：

```powershell
Push-Location Bin
./Tool.exe --AppType=ExcelExporter --Console=1 --Customs=Check,ShowCmd
Pop-Location
```

## 并行行为

所有启用工程的 `cmds` 会通过 `Parallel.ForEachAsync` 并行执行。每条命令等待进程退出及 stdout/stderr 读取完成；启动/执行异常、非零退出、stdout 中的 `|ERROR|` 或非空 stderr 都判为失败。即使没有错误文本，非零退出也会失败；错误文本不因零退出而被忽略。

全部已启动命令正常收尾后再汇总结果。任一命令失败会抛出包含工程、完整目标命令和原因的异常，经现有 Tool CLI 返回非零，停止后续复制、目标清理、ID 和本地化。复制、ID 或本地化自身的异常同样传播并停止剩余步骤。日志顺序不代表配置顺序，不以输出文件存在判断成功。

非 Check 子命令可能已经写入第一个输出目录；失败不回滚这些源产物，也不回滚后处理失败前已完成的写入。先处理具体失败再按授权重新生成，不能把部分或旧输出当作本轮有效结果。

多个命令不得写同一个源输出目录。需要相同产物时使用逗号配置复制目标，而不是让两个并行命令直接覆盖同一路径。

## 运行时加载与重载

`Generate/*/Luban` 是生成输出；相邻 `LubanPartial` 是人工维护的 partial 扩展，未列入当前 outputCodeDir。跨表读取应放在所有并发 LoadAsync 完成后的 `PostResolveRef`，不能在某张表的 `PostInit` 读取另一张可能尚未完成加载的表。DTStartSceneConfig 的启动索引使用此边界；不手改生成的 Tables/DT/DR 或数据文件。

启动时，GF 通过 `TablesComponent.LoadAllAsync()` 加载公共表；ET 与 GameHot 的入口加载各自生成的 Tables。

单表重载示例：

```csharp
IDataTable table = Tables.Instance.GetDataTable("DTUnitConfig");
if (table != null)
{
    await table.LoadAsync();
}
```

表名是生成类名，不是 Excel 文件名。重载只刷新当前进程内存，不会自动通知其他进程。

当前 `DTUnitConfig`／`DTAIConfig` 保留类型、数据为空；`DTUIEntity`／场景／三类音频表也为空。`DTDemo` 和 `DTOneConfig` 演示表已删除，不能再按旧表名重载。UI／Entity 只保留初始化链路及当前地图／HUD 所需注册，四张 StartConfig 源表保留。登录页已移除测试 Widget，旧 Demo 单位与 AI 演示不再作为当前可用玩法。

## 常见问题

### 没有发现任何配置

确认 `luban.conf` 位于 `Design/Excel` 的直接子目录，而不是更深层级，并检查 `active=true`。

### 切换模块后生成了错误数据

重新执行 `Game/Define Symbol/Refresh`，检查 ET 与 GameHot 只有一个 `active=true`，再重新导表。

### 服务端找不到配置

确认 ET 的 `clientserver` 命令包含 `%ROOT%/Config/Luban` 复制目标，并从 `Bin/` 作为工作目录启动服务端。

### 输出只显示失败，没有 Luban 细节

增加 `--Customs=ShowCmd,ShowInfo`。工具链统一使用 UTF-8；若自定义外部命令仍出现乱码，应让该命令同样以 UTF-8 输出。

### ID 常量没有更新

先检查 `Assets/Res/Editor/Luban/dtuiform.json` 或对应 JSON 是否更新。如果没有，问题在 Luban 目标；如果已更新，再检查 Share.Tool 的二次生成日志。

![Luban 配置示例](png/luban_genconfig.png)

## 相关文档

临时格子备料配置源为 `Design/Excel/ET/Datas/Product/GridItemConfig.xlsx` 和 `GridManualRecipeConfig.xlsx`，注册使用 c,s,e，正常输出 ET Client／ClientServer bytes、Config/Luban 副本及 Editor JSON。五种物品及两条手工配方由启动已加载的 `Tables.Instance` 使用；碗容量 1、切配／打蛋各 60 Tick 是样机初值。修改源后仍须五目标 Check 通过再正常导出，不修改生成的 DT／DR。验收范围见 [备料执行说明](../openspec/changes/archive/2026-10-11-grid-kitchen-prep-processing/execution-notes.md)。

煮汤在同一 Product 目录追加 `GridAutoRecipeConfig.xlsx`（c,s,e）：容器定义、输入定义与数量列表、耗时、输出、份数和盛装容器。锅定义 6、汤定义 7，碗接受汤；输入列表与数量须一一对应且为正，锅容量和输出兼容由配置加载后的产品校验核对。样机为番茄块＋蛋液、120 Tick、2 份；水不建实物。普通 Solo 只在首次建厨房时创建一锅两碗，不自动补库存。生成与运行结果见 [煮汤执行说明](../openspec/changes/archive/2026-10-13-grid-kitchen-soup-batch-cooking/execution-notes.md)。

单机关卡源为 `Product/LevelConfig.xlsx`（c,s,e），目前字段为 Id、NextLevelConfigId、OperatingTicks、StarThresholds、SpawnX／SpawnY、AllowedDishIds 及 AllowedBuffIds／CandidateCount／RequireBuffChoice。初始化先检查下一关引用、三个递增星阈值和可走出生格，再创建餐厅；继续时按当前真实布局再验出生位。两条样机均 400 Tick，第二定义可继续复用，运行 Level 的身份／代次每关不同。两关相同的汤／吐司菜单已随真实菜品源接入；客流及 buff 引用已接入启动验证，状态见 [关卡执行说明](../openspec/changes/archive/2026-10-13-solo-restaurant-level-flow/execution-notes.md)。

工位源为 `Product/StationConfig.xlsx` 与 `InitialLayoutConfig.xlsx`（c,s,e）。前者声明 Kind、Capabilities、占地和操作格的 x/y 成对列表、ItemSlotCapacity；后者声明工位定义、格坐标、0～3 朝向、名称与一次初始物品。普通工位容量为 1，脏／干净池显式容量为 4；池的实例与目标转移接口已在顾客 change 接入，真实工作推进和询问优先自动调度已接入伙伴引擎，完整服务回收已验收。原七个工位位置和材料保持，新增空烤箱 `(5,14)`，占两格 `(5,14)`、`(5,15)`，声明 Bake 能力。实际烘烤和面包／盘库存由菜品 change 接入。初始化在创建世界前检查定义、边界、重叠、前厅入口 `(3,1)`、关卡出生格及各设备至少一个可达操作格；运行地形来自同一配置，占地旋转与操作格使用同一坐标规则。验收见 [工位执行说明](../openspec/changes/archive/2026-10-13-solo-workstation-layout/execution-notes.md)。

清理设施源沿用上述三张表：GridItemConfig 追加 Form（1 固体、2 液体、3 容器）及 Discardable，水定义为 11，碗追加兼容水。StationConfig 追加 WaterDefinitionId／WaterUnits，原 Wash 定义 6 保留 Wash 位并增加 FillWater=128、DrainLiquid=256，供水定义 11／数量 1；垃圾桶定义 9 使用 Kind=8／Trash=512，布局 16 位于 `(5,10)`。这些设施不追加初始物品。源已通过五目标 Check 和正常生成；实际接水／倾倒动作的实施与运行状态以 [清理 change](../openspec/changes/solo-kitchen-clear-and-discard/execution-notes.md) 为准。现有汤仍默认供水，不要求先接水入锅。

补货源 `Product/SupplyConfig.xlsx` 注册 c,s,e，字段为 Id／Name／MaterialDefinitionId／PackageDefinitionId／PackageUnits／AvailablePackages／TransitTicks／ReceivingLayoutId。样机三行供应番茄1、鸡蛋3、面包8，包装定义12／13／14为 Kind=2、Form=3、可丢弃空壳、容量2且只接受对应原料；每次包量2、外部包数5、运输40 Tick是可调初值。具名收货点复用原“交接台”布局4，保持单槽和原占地，不添加初始材料或包装。菜单闭包只校验合法来源和加工能力，不生成实物；配置阶段五目标 Check／生成已通过，业务与 UI 实施状态见[供应记录](../openspec/changes/solo-supply-delivery/execution-notes.md)。

装修源`Product/RenovationConfig.xlsx`注册c,s,e第33行，字段Id／Name／StationDefinitionId／Quantity／InstallX／InstallY／Orientation，LevelConfig末尾AllowedRenovationIds引用两条候选。样机普通台面／切配台各1，锚点`(2,13)`／`(4,12)`、朝向0；数量沿本地x方向排布并使用原地形完整预检。当前合法未获得项在自然成功时固定，选中后下一Preparing一次安装，不附送物品。五目标Check／导出及非法源／实际安装／原加工保持已运行，见[装修记录](../openspec/changes/solo-interlevel-renovation/execution-notes.md)。

代表菜源为 `Product/DishConfig.xlsx`（菜品、内容定义、要求容器、最终阶段）、`DishStageConfig.xlsx`（Raw／Manual／Batch／Bake／Portion／AddToContainer 分型依赖）与 `GridBakeRecipeConfig.xlsx`（输入、输出、工位能力、耗时）。均注册 c,s,e，使用当前五目标生成链。物品定义 8 面包片、9 烤面包、10 盘，盘容量 1 且只接受烤面包；样机烘烤 80 Tick。初始布局新增一片面包 `(1,14)` 和一个盘 `(3,16)`，成果不进入初始库存。LevelConfig 的 AllowedDishIds 两关均为 `1,2`：番茄蛋花汤与盘装吐司。启动前深度遍历阶段图并核对真实供应、引用、配方形态、设备和容器；循环、缺依赖或免费半成品叶子明确失败。运行菜品身份取实际容器／内容，配方 ID、物品实例 ID 和订单 ID 分别使用，验收见 [菜品执行说明](../openspec/changes/archive/2026-10-13-solo-dish-recipe-chain/execution-notes.md)。

固定伙伴源为 `Product/CompanionConfig.xlsx`（c,s,e）：Id、Name、Post、SpawnX／SpawnY、AskTicks、ClearTicks、WashTicks。当前样机一位固定前厅岗位，出生 `(2,5)`，基础耗时为 30／20／61 Tick。初始化先检查唯一身份、岗位、正耗时与真实出生格，再建餐厅；继续关卡时按当前地形复查出生格。固定身份持续，角色和成长归 Level；真实工作请求、顾客目标接口与第三次成长已接入，自动调度已接入，完整服务回收已验收，状态见 [伙伴执行说明](../openspec/changes/archive/2026-10-13-solo-fixed-companion-work/execution-notes.md)。

前厅源为 `Product/CustomerFlowConfig.xlsx`、`OrderConfig.xlsx` 与 `TableConfig.xlsx`，注册 c,s,e。客流按 LevelConfigId 关联，首客 20／间隔 60／订单等待 200／用餐 60 Tick、队列容量 2；两种订单轮换、各 10 分，仍是样机参数。餐桌通过 InitialLayoutId 关联原工位与相对座位；初始布局追加两桌、一个洗碗位及各 4 槽的脏／干净池，总计 15 个工位、16 格占地，原一锅两碗一盘和材料不补充。启动前检查引用、菜单、座位可达和池容量，运行目标接口只转移原餐具身份；真实洗碗推进已接入伙伴工作引擎；自动客流已接入；真实交付、卫生限制和同餐具取回已接入并验收，验收边界见 [前厅执行说明](../openspec/changes/archive/2026-10-13-solo-customer-order-service/execution-notes.md)。

随机加成源为 `Product/BuffConfig.xlsx`（c,s,e，第31条注册）：Id、Name、EffectKind、DurationNumerator／Denominator、BatchRecipeId；首版 EffectKind=1 只支持煮制耗时比例。三种样机倍率为3/4、2/3、1/2，两关 AllowedBuffIds=1,2,3、CandidateCount=2、RequireBuffChoice=true。启动拒绝非法比例／配方／池和必选空池，可选空池可开店。候选只在 Level 创建时抽取，选择只改变本关选定定义，新适用锅在 Begin 冻结 ceil 后至少一 Tick，旧锅／材料不改。标准 Check／导出5目标和四语言成功；20项 Buff UTF 各 Passed，真实Launcher两局及已通过的实际UI重开UTF见 [加成记录](../openspec/changes/archive/2026-10-13-solo-random-level-buffs/execution-notes.md)。

- [多语言](多语言.md)
- [UI 开发](UI开发.md)
- [Entity 开发](Entity开发.md)
- [Luban 官方文档](https://luban.doc.code-philosophy.com/docs/intro)
