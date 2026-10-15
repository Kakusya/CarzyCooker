# ET 启动与稳定加载层

## Purpose

记录 GF 壳、ET Init/CodeLoader 与实际程序集装载入口。 当前基线为 main 分支的 ET 底座；静态格式/源码核对不代表产品构建、Editor、玩法或联网已验证。

## Requirements

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

### Requirement: ET 程序集装载

CodeLoader MUST 按热更与 CodeBytes 配置取得 Model/ModelView/Hotfix/HotfixView 程序集并注册 CodeTypes，再调用 ET.Entry.Start。

#### Scenario: ET 程序集装载的适用行为

- **WHEN** ET.Init 完成基础单例初始化
- **THEN** 通过已有 CodeLoader 启动；Editor Model DLL 与资源字节分支按真实配置处理

### Requirement: 启动资源模式分流

启动 SHALL 按实际资源模式选择路径：EditorResourceMode 直接完成资源阶段，Package 先初始化资源，其余模式先检查版本。资源检查完成后，只有存在待更新资源且模式为 Updatable 时进入集中更新；其他情况进入资源完成阶段。

#### Scenario: 编辑器资源模式

- **WHEN** Splash 阶段检测到 EditorResourceMode
- **THEN** 进入资源完成与预加载，不要求在线版本请求完成

#### Scenario: Package 与在线模式

- **WHEN** 未启用 EditorResourceMode
- **THEN** Package 走资源初始化；其他模式走版本检查，不能把编辑器启动成功作为在线更新通过

#### Scenario: 资源检查仍未完成

- **WHEN** 资源检查回调尚未到达
- **THEN** 保持检查流程；回调完成后以更新数量及 ResourceMode 选择更新或完成路径

### Requirement: 预加载完成顺序

预加载 MUST 依次等待公共配置表、当前语言资源与 UX 初始化；UNITY_HOTFIX 且 ENABLE_IL2CPP 时再等待 AOT 元数据装载，然后进入 Preset。前置 await 失败不能报告 ET 业务已就绪。资源完成阶段的 Launcher 路径保存是独立异步任务，不保证预加载等待它结束。

#### Scenario: 正常完成预加载

- **WHEN** 适用的预加载步骤依次完成
- **THEN** 才进入 Preset；Editor 的 UI/Entity 分组核对发生在该步骤中

#### Scenario: 前置加载失败

- **WHEN** 表、语言或 UX 初始化中的 await 抛出异常
- **THEN** 后续步骤与进入 Preset 不在该调用中继续执行；现有流程没有据此提供自动恢复或回滚保证

### Requirement: 版本检查终态边界

版本检查 MUST 区分本流程的响应与其他请求；解析失败、请求失败或强制更新提示不设置版本检查完成。成功解析且无需强制更新后才设置资源地址，并按版本列表结果选择下一流程。现有失败分支不提供自动重试或离线回退保证。

#### Scenario: 无关请求返回

- **WHEN** WebRequest 回调的 UserData 不属于当前版本检查
- **THEN** 忽略该回调，不推进当前流程

#### Scenario: 检查失败或强制更新

- **WHEN** 版本解析失败、请求失败或服务要求强制更新
- **THEN** 保持未完成状态；强制更新提示提供更新与退出入口，不报告资源检查已经通过

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

### Requirement: 产品启动状态按实际阶段报告

系统 MUST 区分配置准备完成、产品根初始化、监听建立和客户端加入；Host 完全就绪须同时具有实际监听及本地玩家加入成功，Client 就绪须有当前加入确认。失败和停止不被旧阶段成功覆盖，后续 await 结果须核对实例身份。本轮结束只收尾实际创建的树和入口，不透明恢复或启动新轮。

#### Scenario: Host 只有监听成功
- **WHEN** Host 已建立监听但本地玩家尚未完成加入
- **THEN** 可以报告监听地址，不报告 Host 完全就绪；本地加入失败使本轮失败并清理监听

#### Scenario: 加入依赖消息泵
- **WHEN** 产品根已创建，本地 Join 尚需 Fiber 更新处理
- **THEN** 装载先允许既有 Runner 驱动消息，再等待本地加入；不在消息泵启动前阻塞等待队列答复

#### Scenario: Client 仅已创建 Session
- **WHEN** Client 已创建网络 Session，但没有当前 Join 成功答复和有效名单
- **THEN** 仍报告连接中或加入中，不以对象存在替代业务 Ready

#### Scenario: 等待中停止或下一轮启动
- **WHEN** 本轮停止，旧初始化或加入答复后来完成
- **THEN** 旧结果不改变已停止状态或下一轮会话，退出只影响原本轮入口和资源

## Sources

- [ProcedurePreset](../../../Unity/Assets/Scripts/Game/Procedure/ProcedurePreset.cs)
- [ProcedureET](../../../Unity/Assets/Scripts/Game/Procedure/ProcedureET.cs)
- [ET.Init](../../../Unity/Assets/Scripts/Game/ET/Loader/Init.cs)
- [CodeLoader](../../../Unity/Assets/Scripts/Game/ET/Loader/CodeLoader.cs)
- [资源模式](../../../Unity/Assets/Scripts/Game/Procedure/ProcedureSplash.cs)
- [资源检查](../../../Unity/Assets/Scripts/Game/Procedure/ProcedureCheckResources.cs)
- [预加载](../../../Unity/Assets/Scripts/Game/Procedure/ProcedurePreload.cs)
- [资源完成](../../../Unity/Assets/Scripts/Game/Procedure/ProcedureCompleteResources.cs)
- [版本检查](../../../Unity/Assets/Scripts/Game/Procedure/ProcedureCheckVersion.cs)

- [格子样机与默认入口执行记录](../../changes/archive/2026-10-11-portrait-grid-kitchen-prototype/execution-notes.md)。

- [游戏入口增量及验收](../../changes/archive/2026-10-15-product-mode-room-flow/execution-notes.md)。
