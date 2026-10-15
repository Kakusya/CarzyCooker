# Spec Delta

## MODIFIED Requirements

### Requirement: ET 固定入口

ProcedurePreset SHALL 直接进入 ProcedureET；在 UNITY_ET 编译条件成立时，由 CodeRunner.StartRun("ET.Init") 启动，离开时仅对本轮已启动入口 StopRun。普通 Launcher 无显式请求时 MUST 在启动 Loader 前建立本轮默认 Solo 配置，进入 ProductRoot 并显示受管模式菜单，选择有效餐厅后再加载地图与 HUD；显式 legacy 继续走旧 ET 示例路线，显式 Solo／Host／Client 使用其原配置。显式实例配置错误、交接失效或产品能力缺失时 MUST 明确失败，不回退到默认 Solo 或旧示例。不声明已删除的 GameHot／纯 GF 示例分支仍可切换，也不把进入 ProcedureET 等同于 ET 已成功初始化。

#### Scenario: ET 固定入口的适用行为
- **WHEN** GF Procedure 进入 Preset，且本轮没有显式实例请求
- **THEN** ProcedureET 建立新的 Solo 配置后启动 ET 路线，产品根先显示模式菜单，选择前不建运行餐厅，不要求使用实例窗口

#### Scenario: ET 编译条件成立时退出
- **WHEN** 已编译 UNITY_ET 路线且 ProcedureET 离开
- **THEN** 仅在本轮确实启动 CodeRunner 时停止它；缺少 UNITY_ET 时不能从流程类存在推断 ET 启动回调已编译

#### Scenario: 显式产品角色尚无启动能力
- **WHEN** 用户显式请求产品角色，但适用产品启动能力尚未接入
- **THEN** 报告 ProductBootstrapUnavailable，不调用旧 ET 示例入口，不创建 Demo 服务／客户端，不报告产品 Ready

#### Scenario: 实例请求交接失败
- **WHEN** 本轮显式实例配置或跨 Play 交接失效
- **THEN** 不推进 ET 业务启动，不回落到默认参数、默认 Solo 或旧 Demo 链路；不宣称该检查能撤销此前 GF 初始化读写

### Requirement: 产品角色 ET 根入口分流

产品启动能力实际接入后，普通 Launcher 默认 Solo 以及合法显式 Solo／Host／Client 请求 SHALL 经现有 ET 稳定装载入口创建独立产品根场景；普通入口选择前只显示模式菜单，选择餐厅后才建立适用角色子树；只有显式合法 legacy 保留旧 ET Main 路线。产品路径不得触发 Demo 根启动处理，不得用工作区职责或 CodeMode 替代运行角色。能力缺失、配置失效及初始化失败仍须明确拒绝，不回落 Demo。

#### Scenario: 显式产品角色启动
- **WHEN** 合法 Solo／Host／Client 配置且适用产品能力已编译并装载
- **THEN** 创建产品根与适用角色子树，不发布旧 Main 的示例启动事件，不连接 Realm／Gate 或启动数据库服务

#### Scenario: 产品能力不适用
- **WHEN** 产品角色所需代码未装载或本轮初始化失败
- **THEN** 保留具体失败并收尾本轮实际资源，不改成 legacy 入口或报告就绪

#### Scenario: 普通 Launcher 回归
- **WHEN** 未提交显式实例请求而运行普通 Launcher
- **THEN** 建立新的 Solo 配置并进入产品根模式菜单，不继承上一轮产品角色与会话，不进入示例 Main；选择合作经完整退出和新配置交接，不原地修改 Prepared 配置

#### Scenario: 显式 legacy 回归
- **WHEN** 明确选择合法 legacy 请求
- **THEN** 保留原 ET Main 的适用入口，不创建 Solo 产品会话

## Sources

- [实施与运行证据](../../execution-notes.md)。
- [普通入口用法](../../../../../../Book/快速开始.md)。
