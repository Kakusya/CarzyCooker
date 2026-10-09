# Spec Delta

## ADDED Requirements

### Requirement: Entity 显示失败与取消

配置类型 ID 的 GF 异步入口 SHALL 在表行缺失时记录警告并返回 null，普通 Entity 与 UIEntity 分别查询各自配置；ET UGFEntity 包装层遇 null 再抛出显示失败异常。GF 等待器取消时隐藏对应运行 ID，并结束为取消；这些终态不得报告为成功显示。

#### Scenario: 缺少普通或 UIEntity 配置

- **WHEN** 选中的异步显示入口查询不到对应表行
- **THEN** GF 入口返回 null；ET 包装层将其视为失败，不生成替代配置或混查另一张表

#### Scenario: 显示过程中取消

- **WHEN** GF 等待器检测到 token 取消
- **THEN** 若该运行 ID 已存在或仍在加载则调用 HideEntity，任务返回取消；加载已结束却无对象时返回失败

#### Scenario: 对已显示包装对象重复显示

- **WHEN** 同一 UGFEntity 包装对象已持有 GF Entity，再调用其显示入口
- **THEN** 拒绝重复显示，不偷偷覆盖原关联

### Requirement: ET Entity 视图回调边界

GF view 的 OnShow MUST 取得本轮 ET owner 并建立 Transform/Mono 关联后再分发 ET 显示回调；Hide 与 Recycle 分别转发相应生命周期。GF 的附加/脱离回调转发视图关联，不自动等同于更改 ET Component/Child 所有权。

#### Scenario: 视图显示回调

- **WHEN** GF 托管视图完成 OnShow 的本轮绑定
- **THEN** ET 显示 System 可使用已建立的 Mono/Transform 关联；未完成加载不能预先消费 View

#### Scenario: 仅隐藏 GF 视图

- **WHEN** 通过 GF 隐藏接口退出视图
- **THEN** 分发 ET 隐藏回调，但不会仅因 Hide 自动销毁 ET owner；释放逻辑对象需原 owner 的移除或 Dispose

#### Scenario: 附加到另一个 GF 视图

- **WHEN** GF 执行附加或脱离
- **THEN** 转发对应 ET 回调；是否迁移 ET 逻辑 owner 由实际业务入口决定，不能从 Transform 变化推断已迁移

## Sources

- [配置 ID 与异步路由](../../../../../../Unity/Assets/Scripts/Game/Entity/EntityExtension.Awaitable.cs)
- [ET 包装层](../../../../../../Unity/Assets/Scripts/Game/ET/Loader/UGF/Entity/UGFEntity.cs)
- [GF 等待器](../../../../../../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Runtime/Awaitable/Awaitable.EntityComponent.cs)
- [视图生命周期分发](../../../../../../Unity/Assets/Scripts/Game/ET/Loader/UGF/Entity/ETMonoUGFEntity.cs)
