# Spec Delta

## ADDED Requirements

### Requirement: 语言切换的加载顺序

语言加载入口 SHALL 先清除全部旧字典，再初始化目标语言的内置字典，最后等待对应外部语言资源。外部资源失败不提供旧字典自动恢复保证；字典加载完成不代表所有存量控件都已刷新或完成语言显示验收。

#### Scenario: 切换到另一个语言

- **WHEN** 调用 LoadLanguageAsync 加载目标语言
- **THEN** 旧 raw strings 被清除，内置文本先可用，再加载该语言对应的外部资源

#### Scenario: 外部语言资源加载失败

- **WHEN** 字典已清除且内置文本已初始化，随后外部加载失败
- **THEN** 返回实际加载失败，不宣称恢复了原字典或所有控件已显示新语言

### Requirement: 字典解析失败的部分写入

字典解析 MUST 对每个键执行实际 AddRawString；遇非法键、重复键或解析异常时返回 false。解析过程按顺序写入，没有暂存整份字典再提交的事务保证；失败前成功加入的键不会由 helper 自动回滚。字节入口的切片参数限制单列为已知问题，不声明已支持任意切片。

#### Scenario: 已写入部分键后失败

- **WHEN** 前面的键已加入，后续键重复或数据解析失败
- **THEN** 解析结果为 false；不能从失败结果推断字典完全未改变

#### Scenario: 完整有效资源

- **WHEN** JSON 字符串或完整 ByteBuf 资源中的全部键值均可解析且可加入
- **THEN** 返回 true；仅证明该字典解析成功，不证明 UI 显示或语言切换时序

## Sources

- [语言加载入口](../../../../../../Unity/Assets/Scripts/Game/Localization/LocalizationExtension.cs)
- [字典解析](../../../../../../Unity/Assets/Scripts/Game/Localization/LubanLocalizationHelper.cs)
- [已知问题](../../../../../../KnownIssues.md)
