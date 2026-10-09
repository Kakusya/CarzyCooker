# 公共 GameEntry、ET 分层与框架示例的职责

## Purpose

记录当前 CarzyCooker 底座的公共 GameEntry、ET 分层与框架示例的职责。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；本次不运行产品构建、玩法或编辑器验证，不描述未来做饭业务已实现。

## Requirements

### Requirement: 公共组件与层

业务 SHALL 复用 GameEntry 公共组件，ET Model/ModelView 声明数据与视图关联，Hotfix/HotfixView 实现系统；程序集由 asmdef/csproj 决定。

#### Scenario: 公共组件与层的适用行为

- **WHEN** 新增组件或系统
- **THEN** 放在实际运行模式对应层，保持 owner 与视图边界

### Requirement: 示例隔离

文档 MUST 把 ET Demo/LockStep/Benchmark 等标为现有示例，不将其能力写成做饭领域已经实现。

#### Scenario: 示例隔离的适用行为

- **WHEN** 索引示例战斗、移动、AOI 或锁步代码
- **THEN** 用作可复用实现定位，不能增加本次玩法范围

## Sources

- [GameEntry.cs](../../../Unity/Assets/Scripts/Game/Base/GameEntry.cs)
- [GameEntry.Game.cs](../../../Unity/Assets/Scripts/Game/Base/GameEntry.Game.cs)
- [Share](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share)
- [Share](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share)
- [Server](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Server)
