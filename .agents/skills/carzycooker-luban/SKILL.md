---
name: carzycooker-luban
description: 处理 CarzyCooker Excel/Luban 配置、校验导出、生成 ID 与本地化，核对当前模式和源到输出链路。
---

# Luban 配置

读根 [AGENTS.md](../../../AGENTS.md)、[生成约定](../../../references/data-generation.md)、[Luban手册](../../../Book/Luban配置.md) 与 [规格](../../../openspec/specs/data-generation/spec.md)。路径相对仓库根。

1. 读当前 main 分支的 ET 底座的 `Design/Excel/ET/luban.conf` 的 active、targets、cmds；GameHot 工程已移除。确认 ET 输出目标，不让并行命令写同一源输出。
2. 定位源工作簿、实际表头、类型、ID 和约束。UI/Entity 表在`Design/Excel/ET/Datas/Game/UI.xlsx` / `Entity.xlsx`，不改派生 JSON/ID。
3. 用已有且获授权的工作簿编辑方式，保留公式、样式、隐藏表和数据验证；缺能力先提出具体依赖与改动，不自行安装 Excel 工具。Editor 查询/刷新使用已接入的 Unity CLI/Pipeline；专用自动改表能力另行审批。
4. 检查 `Bin/Tool.exe` 或 `Tool.dll` 可用；构建按任务授权。以 Bin 为 cwd：`./Tool.exe --AppType=ExcelExporter --Console=1 --Customs=Check,ShowCmd`。通过后获授权的导出去掉 Customs；JSON 格式用 `--Customs=Json`。
5. 检查源与所有目标 diff，公共 Editor JSON 到 UI/Entity/UIEntity/Scene/Sound ID、本地化及 ET `Config/Luban` 复制。复制可清空目标，不混放手写文件。

实现：`Share/Tool/ExcelExporter/ExcelExporter.Luban.cs`、`ExcelExporter.Localization.cs`、`Generate/`。Check 不写产物且跳过本地化导出。核对真实退出码和失败日志，不用文件存在冒充成功；Editor 刷新/编译未运行写 NotRun。禁止 SHA/hash 与额外 JSON 证据。
