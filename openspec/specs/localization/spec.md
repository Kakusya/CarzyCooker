# Excel 本地化导出、生成路由和运行时字典

## Purpose

记录当前 CarzyCooker 底座的Excel 本地化导出、生成路由和运行时字典。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；本次不运行产品构建、玩法或编辑器验证，不描述未来做饭业务已实现。

## Requirements

### Requirement: 源与导出

本地化 MUST 以 Design/Excel/Localization.xlsx 为源，通过 ExcelExporter.Localization 生成语言资源与代码；Check 模式立即返回不导出。

#### Scenario: 源与导出的适用行为

- **WHEN** 修改语言键或占位文本
- **THEN** 修改源表并走现有导出链路，不手改 LocalizationKey、AssetUtility 或派生资源

### Requirement: 运行时加载

LubanLocalizationHelper SHALL 解析 JSON 字符串对或 ByteBuf 键值序列；重复/非法键或解析失败返回失败，不宣称格式转换即完成运行时显示验收。

#### Scenario: 运行时加载的适用行为

- **WHEN** 字典解析出现无效键或异常
- **THEN** 保留实际失败语义，具体文本刷新与语言显示另需运行验证

## Sources

- [Localization.xlsx](../../../Design/Excel/Localization.xlsx)
- [ExcelExporter.Localization.cs](../../../Share/Tool/ExcelExporter/ExcelExporter.Localization.cs)
- [LubanLocalizationHelper.cs](../../../Unity/Assets/Scripts/Game/Localization/LubanLocalizationHelper.cs)
- [LocalizationExtension.cs](../../../Unity/Assets/Scripts/Game/Localization/LocalizationExtension.cs)
