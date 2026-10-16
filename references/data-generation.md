# 生成链路约定

**非规格**；冲突以代码/表、进行中 change、已同步主规格及用户最新意图为准。**阅读 ≠ 授权**文中未做能力。

Excel、Proto、模板和配置是源，生成 C#/ID/JSON/bin/Localization 和 CodeBind *.Bind.cs 是派生结果；修改源或生成器，不打手工补丁。

## Luban

读取 Design/Excel/ET/luban.conf 的实际 active、targets 和 cmds。当前 ET=true，GameHot 配置工程已随分支移除；导出器中旧模式分支不等于本项目仍支持双模式。不要让并行任务写同一源输出。

Excel/Defines → Share.Tool 发现 active 工程 → 并行 Luban → 多输出复制 → UI/Entity/UIEntity/Scene/Sound ID → 本地化。复制目标可能清空，不混放人工维护文件。

工具已构建且本轮授权校验/导出时，以 Bin 为 cwd：

```powershell
Push-Location Bin
try {
    ./Tool.exe --AppType=ExcelExporter --Console=1 --Customs=Check,ShowCmd
    # 通过且已获导出授权后，执行：
    # ./Tool.exe --AppType=ExcelExporter --Console=1
} finally {
    Pop-Location
}
```

Check 不写产物且跳过本地化输出。缺工具说明前置，不把构建自动加入文档任务。目标与选项见 [Luban手册](../Book/Luban配置.md) 与 [规格](../openspec/specs/data-generation/spec.md)。

当前源表按实际用途清理示例业务数据：公共 UI／Entity／UIEntity／Scene／Sound／UISound／Music 保留结构，UIEntity、Scene 和三类音频为空表；ET UnitConfig／AIConfig 保留公共类型、数据为空。UI 保留 ET Demo／Lockstep 初始化链路六条注册和 Product 地图 HUD 一条，Entity 仅 GridKitchenBoard 一条。无手写消费者的 DTDemo／OneConfig 已从 __tables__ 和源表删除，派生输出经正常导表移除。登录页测试 Widget 的加载代码、嵌入节点、独立资源和脚本已删除；旧 Demo 单位／AI 演示不再作为可用玩法。四张 StartConfig 源表及 Builtin 初始化资源保留。

所有启用 Luban 子命令退出且完整输出读取成功后，非 Check 才复制和派生 ID。启动/执行异常、非零退出及现有错误输出判据命中均抛出失败，经 CLI 返回非零，阻断 ExportAll 后续本地化。两个 Editor 导表菜单收到失败后停止刷新、缓存清理和选择恢复。

子命令期间可能已有部分源输出；复制/派生自身失败前也可能已有写入，没有整批回滚保证。复制目标只放该生成链路产物。当前修正和验证见 [导表失败变更](../openspec/changes/archive/2026-10-11-fix-foundation-entry-and-export-errors/execution-notes.md)，对应契约已同步主规格。

## 协议、本地化与绑定

临时格子备料新增两张 `Design/Excel/ET/Datas/Product/` 源表：GridItemConfig（定义、显示名、材料／容器、容量、接受定义）与 GridManualRecipeConfig（方式、输入、输出、Tick 耗时），__tables__ 注册 c,s,e。ET 运行从既有 Tables 读取，测试从当前 CodeMode 的实际导出 bytes 加载，随 World 释放；必需定义或引用非法时产品初始化明确失败。源表及五目标生成证据见 [本轮执行说明](../openspec/changes/archive/2026-10-11-grid-kitchen-prep-processing/execution-notes.md)。

- Design/Proto/*/proto.conf 选择 active/codeType/起始 Opcode/输出；Proto2CS 排序和消息顺序影响 Opcode。当前配置只有 ET 生成域，使用 ET/MemoryPack。工具保留 UGF 生成器不等于客户端仍有 GF Network packet 运行时；不恢复已删除模式。见 [Proto手册](../Book/Proto生成工具.md)。
- 本地化源 Design/Excel/Localization.xlsx，由 ExcelExporter.Localization.cs 生成资源与常量。见 [多语言](../Book/多语言.md)。
- CodeBind 在 prefab 上生成 Mono partial 引用；ET 骨架工具不会自动填表。STATE_CONTROLLER_CODE_BIND 按依赖配置启用，不由宏推断绑定完成。
- 产品分类统一在 `Design/Excel/ET/Datas/__enums__.xlsx` 维护，group 为 `c,s,e`。表字段使用生成枚举，单值写成员名；`GridStationCapability` 是 Flags，例如洗碗池使用 `Wash|FillWater|DrainLiquid`（原值 400），蒸汽机使用 `HeatMilk|FoamMilk`。`StationWorkKind` 的 Batch／Chop／Bake 是加成与升级的加工管线分类，不能与 `KitchenProcessKind` 混用。角色 `GridFacing` 为运行域方向；工位 `GridOrientation` 为配置生成的逆时针旋转，两者各自使用。整数仅保留在消息／存档格式与索引边界，先验证再转换；生成代码不可手改。
- 检查源改动、生成 diff、重复 ID、资源名/分组与目标编译。Editor 刷新需环境和授权，未运行写 NotRun。禁止 SHA/hash 与状态/证据副本。
