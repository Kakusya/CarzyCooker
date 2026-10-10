# Spec Delta

## Purpose

为单个运行实例提供明确的角色与 endpoint 启动输入、Editor 请求交接和本轮资源归属，使现有 Launcher 能为后续产品会话提供可靠配置。本规格仅定义启动基础，不包含第二 worktree 的建立、代理协作、应用数据隔离或产品联机验收。

## ADDED Requirements

### Requirement: 显式启动与普通 Launcher 兼容

系统 SHALL 使用显式提交的合法请求启用实例启动配置。配置无效时 MUST 在该入口进入 Play 前拒绝，不解释为普通启动。普通 Launcher 没有显式请求时保留旧路线，不消费窗口草稿或继承上一轮角色。

#### Scenario: 普通 Launcher 启动
- **WHEN** 用户没有提交显式实例请求而运行普通 Launcher
- **THEN** 继续既有 ET 底座路线，不使用窗口未提交的草稿

#### Scenario: 显式请求无效
- **WHEN** 显式请求包含非法角色或不完整配置
- **THEN** 进入 Play 前报告错误，不回落到默认角色或旧 Demo

### Requirement: 最小不可变运行配置

实例配置 MUST 区分运行标识、实例标识、legacy／host／client 角色及 endpoint 意图，生效后本轮不可变。标识不得为空；角色不能由分支职责、ET AppType 或 CodeMode 推导。配置不得要求工作目录职责、产品名、基线版本或自定义数据根。

#### Scenario: 改变下一轮草稿
- **WHEN** 本轮配置已经生效，用户修改窗口草稿
- **THEN** 本轮配置保持原值，下一轮只消费新提交的请求

#### Scenario: 切换运行角色
- **WHEN** 下一轮以另一合法运行角色启动
- **THEN** 使用新角色，不依据工作目录的开发／稳定职责固定 Host 或 Client

### Requirement: endpoint 校验只表达意图

Host MUST 只接受 loopback 监听意图，省略时记录端口 0；Client MUST 提供非零端口的 loopback 连接目标；legacy 不接受网络 endpoint。启动配置校验不得绑定或连接，不得把意图报告为实际网络就绪。

#### Scenario: Host 省略监听配置
- **WHEN** 合法 Host 请求未指定监听 endpoint
- **THEN** 记录 loopback 与端口 0 的意图，不报告监听已建立

#### Scenario: Client 目标非法
- **WHEN** Client 缺少连接目标、使用端口 0，或出现角色不允许的 endpoint
- **THEN** 请求明确被拒绝，不连接旧 Demo 默认服务

### Requirement: 场景请求按工程作用域管理

Launcher 场景请求及显式实例请求 MUST 按工程保存、消费和清理，一个工程的消费不得改变另一工程作用域的请求。新入口不得读取、删除或回退到旧全局场景请求键。

#### Scenario: 独立作用域的请求
- **WHEN** 两份工程作用域各有场景请求，一份完成消费和清理
- **THEN** 另一份请求保持原值，原请求的角色和场景不被替换

#### Scenario: 旧全局请求仍存在
- **WHEN** 旧版本 Launcher 留有全局场景请求
- **THEN** 新入口只消费自己的工程范围请求，不读或删除旧键

### Requirement: Editor 请求交接与支持边界

Editor 入口 MUST 保留请求跨进入 Play 的正常域重载交接，每轮建立新上下文。Play、编译、导入或测试忙碌时拒绝新请求。首期支持默认 Play 及关闭 Domain Reload、保留 Scene Reload；关闭 Scene Reload 时拒绝，运行中编译重载报告中断，不自动恢复或重新启动。

#### Scenario: 正常进入 Play
- **WHEN** 已提交合法实例请求，进入 Play 时发生正常域重载
- **THEN** 本轮上下文使用原请求，不丢失运行角色或 endpoint

#### Scenario: 连续两轮运行
- **WHEN** 第一轮结束后提交第二轮请求并进入 Play
- **THEN** 第二轮只使用新请求，上一轮上下文和订阅已清理

#### Scenario: Editor 忙碌
- **WHEN** Editor 正在 Play、编译、导入或测试时收到新请求
- **THEN** 拒绝请求，不停止、取消或覆盖已有用户操作

#### Scenario: 关闭 Scene Reload
- **WHEN** 显式入口检测到 Scene Reload 已关闭
- **THEN** 进入 Play 前拒绝，不修改用户全局 Play 设置

#### Scenario: 运行中编译重载
- **WHEN** 本轮运行中发生编译导致的域重载
- **THEN** 报告本轮中断，不透明恢复角色、启动第二轮或回落到 Demo

### Requirement: 配置准备与产品成功分开报告

准备完成 SHALL 只表示本轮启动配置已建立，不证明 GF 预加载、ET 初始化、端口绑定、成员加入或玩法成功。后续产品能力检查或交接失败 MUST 保留为最终失败，不能用先前准备完成覆盖。

#### Scenario: 配置已建立但产品不可用
- **WHEN** 本轮配置准备完成，随后产品启动能力检查失败
- **THEN** 最终报告启动失败，不报告 Host／Client Ready

### Requirement: 本轮入口和请求的收尾

退出 MUST 只停止本轮确实启动的入口，清理自身请求、上下文与订阅，不影响另一工程或用户已有运行。未启动入口、部分准备失败及重复收尾都不得改变他方资源；本项不接管 GF 的应用数据保存或文件系统 owner。

#### Scenario: 未启动入口便退出
- **WHEN** 显式角色被拒绝，尚未启动 CodeRunner 就进入收尾
- **THEN** 不停止其他入口，清理本轮状态并保留具体失败

#### Scenario: 正常退出与重复清理
- **WHEN** 本轮已启动入口结束，随后再次执行收尾
- **THEN** 仅处理自身入口和请求，不复用旧配置或改变其他运行

## Sources

- [本项设计](../../design.md)
- [Launcher 与 SceneHelper](../../../../../Unity/Assets/Scripts/Game/Editor/ToolBar/LauncherSceneToolBar.cs)
- [当前 ET 入口](../../../../../Unity/Assets/Scripts/Game/Procedure/ProcedureET.cs)
- [当前 Play 配置](../../../../../Unity/ProjectSettings/EditorSettings.asset)
