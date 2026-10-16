# Luban 工程发现、校验、输出复制及 ID 派生

## Purpose

记录当前 CarzyCooker 底座的Luban 工程发现、校验、输出复制及 ID 派生。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；本次不运行产品构建、玩法或编辑器验证，不描述未来做饭业务已实现。

## Requirements

### Requirement: 配置发现与并行

ExcelExporter SHALL 发现 Design/Excel 的直接子目录 luban.conf，只运行 active=true 的 cmds，并按路径变量展开后并行执行。

#### Scenario: 配置发现与并行的适用行为

- **WHEN** 执行 ExcelExporter 的配置导出
- **THEN** 启用工程的命令参与，调用方避免写同一个源输出目录

### Requirement: 只检查与多输出

Customs 包含 Check 时 MUST 移除输出/生成选项且跳过产物复制、ID 与本地化写入；非 Check 支持第一输出到其余目录的复制。

#### Scenario: 只检查与多输出的适用行为

- **WHEN** 执行 --Customs=Check,ShowCmd
- **THEN** 运行校验且不写派生输出；非 Check 导出另需授权

### Requirement: ID 开关与失败事实

文档 MUST 如实记录 IsEnableET/IsEnableGameHot 按目录存在性设置，ID 由公共 Editor JSON 派生。Luban 子命令启动异常、非零退出或已有错误输出判据命中 SHALL 使本轮导出失败并传播至 Tool 非零退出；不得以存在输出文件或缺少错误文本报告成功。

#### Scenario: ID 开关与失败事实的适用行为

- **WHEN** 当前 main 分支的 ET 底座仅有 ET 配置工程，而导出器保留旧模式条件分支
- **THEN** 命令按 active 选择，现有 ET/公共 ID 输出核对日志和 diff；不从 IsEnableGameHot 的代码存在推断 GameHot 已接入

#### Scenario: 无错误文本的非零退出

- **WHEN** 一个启用的 Luban 子命令以非零退出，且没有输出错误文本
- **THEN** 本轮导出及 Tool 仍报告失败，诊断包含失败命令和退出码

#### Scenario: 输出错误但进程返回零

- **WHEN** 子命令输出命中现有错误判据而退出码为零
- **THEN** 完整读取输出后判定导出失败，不让退出码覆盖已发现的错误

### Requirement: ET 配置目标与消费范围

当前 ET 导出 SHALL 区分 gameclient、gameeditor、client、clientserver、editor 的组与输出；公共运行表、公共 Editor JSON、ET 客户端表、ET 双端表和 ET Editor JSON 各由其实际消费者读取。clientserver 数据同时复制到 Config/Luban，不把公共表与 ET 表混成一套生成 ID。

#### Scenario: 公共 UI 与 Entity 配置输出

- **WHEN** 导出 gameeditor 目标
- **THEN** 公共 Editor JSON 进入 Res/Editor/Luban，供既有 ID 生成器读取；不由 ET Editor JSON 路径替代

#### Scenario: 服务端共享配置

- **WHEN** 导出 clientserver 目标
- **THEN** 数据第一输出进入 Res/ET/ClientServer/Luban，并复制到 Config/Luban；消费者仍按 clientserver 生成的表类型读取

### Requirement: 复制和失败的写入边界

生成验证 MUST 区分子命令结果与后续写入。非 Check 路径 SHALL 仅在所有启用子命令及输出读取完成且成功后执行复制、ID 派生和 ExportAll 后续本地化；失败阻止这些后续步骤并报告可能的部分源输出，不保证回滚已发生写入。正常复制保留原目标清理规则、meta 及排除项。

#### Scenario: 多输出复制目标含手写文件

- **WHEN** 成功导出声明多个目录，复制目标内有不属于生成链路的普通文件
- **THEN** 该文件仍可能被既有目标清理删除；源配置和复制目录必须先确认，不把复制目录当手写代码存放处

#### Scenario: 子命令失败但仍出现输出

- **WHEN** 任一启用子命令失败，而生成期间已有部分源输出
- **THEN** 报告失败与部分产物，跳过后续复制、目标清理、ID 派生和 ExportAll 本地化；不得把部分或旧输出当作本轮成功

#### Scenario: 并行导出全部成功

- **WHEN** 所有启用子命令均成功退出，完整输出也未报告错误
- **THEN** 非 Check 导出按原配置执行复制和派生；Check 仍不写派生输出

#### Scenario: 后续派生发生异常

- **WHEN** 复制、ID 派生或本地化步骤自身失败
- **THEN** 失败传播至调用方并阻止剩余步骤，如实说明已发生的部分写入，不宣称整批回滚

### Requirement: 产品分类配置生成强类型枚举

产品配置中表示有限类型、模式、岗位、加工类别、奖励类别、菜单集合、阶段和朝向的字段 SHALL 使用统一注册的枚举类型，源表数据使用具名成员。适用客户端、双端与编辑器输出 MUST 保持同名成员和相同显式数值，生成字段使用相应枚举类型；数量、坐标、时长和数据身份不作为枚举成员。

#### Scenario: 表格与消费者共享分类

- **WHEN** 导出物品类型、材料形态、手工方式、工位类型、菜谱阶段、伙伴岗位、加成及升级加工类别、装修奖励、菜单集合和工位旋转字段
- **THEN** 各适用输出提供相应枚举字段，产品消费者可直接使用成员判断，且不另行维护同一分类的数值定义

#### Scenario: 错误的枚举名称

- **WHEN** 源表分类字段填写未注册成员名
- **THEN** 配置检查失败并定位源字段，正常生成不得被报告成功，也不得使用旧输出掩盖本次错误

### Requirement: 产品能力配置保留位组合语义

工位和配方能力字段 SHALL 使用具名位标记及其组合，保持每个原能力位的数值。校验 MUST 接受已支持的合法组合，拒绝未知能力位及违反原业务约束的组合；不得把组合值错误地限制为单一成员。

#### Scenario: 洗碗与接水排水组合

- **WHEN** 原数值为 400 的洗碗设施能力改用 `Wash | FillWater | DrainLiquid`
- **THEN** 生成数据仍表示 16、128、256 三个位的组合，设施继续提供这三种原有能力

#### Scenario: 非法能力位或不匹配能力

- **WHEN** 配置包含未声明能力位，或合法位的组合不满足原工位／配方约束
- **THEN** 配置检查或产品初始化在世界创建前明确失败，不静默忽略非法位，也不新增原本未支持的加工能力

### Requirement: 枚举迁移保持现有数字含义

产品分类枚举化 MUST 保留已有成员、组合、零值与边界数据的数值含义。既有消息和存档中的数字仍映射至相同分类，业务与投影使用类型化值；非法边界值按既有拒绝路径处理，不转换成默认有效选项。迁移 SHALL 保持配置身份、玩法结果、工位旋转规则、角色朝向及存档恢复结果。

#### Scenario: 不同语义的零值

- **WHEN** 读取值为零的普通台面类型、原料阶段、基础菜单或无旋转配置
- **THEN** 各字段保留自身原有有效含义，不因新增统一 `Unknown=0` 而发生偏移

#### Scenario: 已有存档及消息输入

- **WHEN** 旧有效存档或当前协议消息包含已知的菜单、朝向、状态或能力数字
- **THEN** 读取和投影保持原语义，恢复后的内容及冻结加工参数不变，不要求改写用户存档或升级协议

#### Scenario: 未知数字输入

- **WHEN** 新迁移分类从消息或存档边界接收到未声明值
- **THEN** 沿适用的非法请求或非法存档路径拒绝，原权威状态和源文件保持，不把类型强转本身当作校验

## Sources

- [luban.conf](../../../Design/Excel/ET/luban.conf)
- [ExcelExporter.cs](../../../Share/Tool/ExcelExporter/ExcelExporter.cs)
- [ExcelExporter.Luban.cs](../../../Share/Tool/ExcelExporter/ExcelExporter.Luban.cs)
- [GenerateUGFEntityId.cs](../../../Share/Tool/ExcelExporter/Generate/GenerateUGFEntityId.cs)
- [Init.cs](../../../Share/Tool/Loader/Init.cs)
- [复制清理规则](../../../Share/Tool/ExcelExporter/LubanFileHelper.cs)
- [产品枚举源](../../../Design/Excel/ET/Datas/__enums__.xlsx)、[产品配置](../../../Design/Excel/ET/Datas/Product)
- [枚举迁移与运行验收](../../changes/archive/2026-10-16-product-semantic-enums/execution-notes.md)
