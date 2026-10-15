# Spec Delta

## MODIFIED Requirements

### Requirement: 产品会话所有者与角色分离

产品会话 MUST 通过 ET Child／Component 关系持有本轮状态、入口及下级资源。Host 同时包含权威会话和本地客户端，Solo 包含相同权威会话和本地客户端但不创建网络监听，远端 Client 仅包含客户端；运行角色不得由主线／稳定工作区职责决定。成员权威和客户端投影须分开，不共享可变业务状态。Host 的 Match 可以先无餐厅；合法开局后，正式 Host 与 Solo 须持有同一持续厨房与当前关卡合同。关闭餐厅回房间不自动解散 Match，远端不得创建本端餐厅权威。

#### Scenario: 启动 Host
- **WHEN** 合法 Host 配置进入产品入口
- **THEN** 创建本轮权威会话及本地客户端，本地玩家必须获得真实加入确认，不能仅因拥有 Host 而报告已加入；房间阶段可无餐厅，合法开局后餐厅由本轮权威持有且只有一个业务驱动

#### Scenario: 启动远端 Client
- **WHEN** 合法 Client 配置进入产品入口
- **THEN** 仅创建本端客户端，通过指定目标加入，不创建本端成员权威或使用 Demo 账号服务

#### Scenario: 启动 Solo
- **WHEN** 合法 Solo 配置进入产品入口
- **THEN** 创建本地权威及独立客户端，完成真实本地加入，不创建网络监听或对自身 UDP 连接，不报告网络监听就绪

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs)。
