# Spec Delta

## MODIFIED Requirements

### Requirement: UIEntity 独立路由

UIEntity MUST 保持独立 DTUIEntity、资源根和生成 ID；同步泛型与 Type 入口 SHALL 按同一 UIEntity 配置解析资源、分组和优先级，并保留运行 serial ID、逻辑类型和 userData 的传递契约。缺少 UIEntity 配置时不得回退查询普通 Entity 表。

#### Scenario: UIEntity 独立路由的适用行为

- **WHEN** 需要显示 UIEntity
- **THEN** 所选同步入口查询 DTUIEntity 并使用 UIEntity 资源根，不因泛型形式改变路由

#### Scenario: 泛型和 Type 入口显示同一类型

- **WHEN** 两种同步入口显示同一个 UIEntity 配置类型，使用相同逻辑类型与 userData
- **THEN** 解析出的资源、分组和优先级一致，分别分配运行 serial ID 并传递原 userData

#### Scenario: 普通表有同 ID 但 UIEntity 表缺行

- **WHEN** UIEntity 入口找不到本表配置，而普通 Entity 表存在同 ID
- **THEN** 保留已有警告和 null 失败行为，不显示普通 Entity 或借用其资源

## References

对应入口见 [EntityExtension.cs](../../../../../../Unity/Assets/Scripts/Game/Entity/EntityExtension.cs)，验证范围见 [执行说明](../../execution-notes.md)。
