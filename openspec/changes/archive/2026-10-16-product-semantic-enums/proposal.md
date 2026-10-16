# Proposal

## Why

产品表与消费者仍用 `Kind=1/2`、`Mode=1/2`、`WorkKind=1/2/3` 等数字表达分类；已有运行时枚举也没有进入表字段类型，配置与代码容易脱节。将这些有限选项统一为具名枚举，让填表和编译阶段发现类别错误，并保持现有玩法与数据编码。

## What Changes

- 用户已确认范围：全部做饭产品代码与产品表中的状态、类型、模式、方向和能力标记。实施核对清单为 14 个工作簿、22 个字段，完整映射见 [design.md](design.md)。
- 在既有 Luban 枚举源注册配置相关枚举，表格类型和数据改用枚举及具名组合，正常生成 Client、ClientServer、Editor 输出；复用已有枚举名称，消除重复手写定义。
- 产品 Model／Hotfix／ModelView／HotfixView 的状态、意图、投影和参数使用相应枚举；删除相应魔法数字判断。角色朝向与工位旋转分别建型，避免数值相同却含义不同。
- 保留原枚举数值、位标记、表行 ID、Proto 字段和存档 DTO 编码；整数边界显式转换与验证。未知枚举值、未知能力位不得默认为合法值。
- 同步填表说明，并用最小相关 UTF、普通入口、真实本地消息／KCP 与存档恢复验证改动。规划阶段运行结果为 `NotRun`。

范围外：数量、时长、坐标、比例、版本、索引、表行和实例 ID；ET／GF 底座、第三方、框架注册常量及 `ProductError` 的整数错误码注册；新玩法、表内容扩充、协议／存档格式升级、新生成或检查框架。`GridKitchenRules.Tomato` 等物品定义 ID 保留现有具名常量。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `data-generation`：产品分类配置采用单一枚举源、具名值与 Flags 组合，生成强类型字段，校验非法选项并保持旧数字含义。

## Impact

- 源：`Design/Excel/ET/Datas/__enums__.xlsx` 和 `Datas/Product/` 的指定字段；既有 `luban.conf` 五目标及 `Config/Luban` 复制链。
- 消费者：ET 四层的 `Product/`，包括加工、菜谱、工位、伙伴、Buff、装修升级、菜单选择、房间、快照及存档适配；不改其 owner 和裁决顺序。
- 测试：现有 `Unity/Assets/Tests/Editor/ET/` UTF，补齐枚举源数据、数字兼容和非法值的行为断言；文档同步 `Book/Luban配置.md`、`references/data-generation.md`。
- 主要风险是枚举重排、不同分类误合并、Flags 拒绝合法组合和存档／消息值误转。验证按具体字段及业务分组，控制为必要小集；严谨程度与复用边界见 [execution-notes.md](execution-notes.md)。

本轮已授权实施及相关验收。当前工作区的其他变更属于实施基线，不能覆盖、回退或当成已提交 main；不包含提交、推送、归档及第二工作区同步。
