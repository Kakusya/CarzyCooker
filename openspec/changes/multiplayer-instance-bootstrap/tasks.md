# Tasks

本项已进入用户授权实施，真实进度以任务勾选与 [execution-notes.md](execution-notes.md) 为准。第二 worktree／分支、AGENTS、ProductName、日志和 Orca 协作仍属于独立工作区 change。

## 1. 最小配置与本轮上下文

- [x] 1.1 在 Game 既有稳定层实现 runId、instanceId、运行角色及 endpoint 的不可变配置和统一校验；用 UTF 验证合法输入、非法标识、角色和 endpoint 冲突，校验不打开网络，不加入工作目录职责、产品名或基线字段。
- [x] 1.2 实现本轮 reset、配置交接与状态，验证草稿不影响生效值、Prepared 不表示产品成功、后续失败保留为最终失败；不增加文件根 owner 或 Player argv。
- [x] 1.3 更新 Book/快速开始.md 的显式启动配置及普通 Launcher 兼容说明，核对实际字段和拒绝行为，不加入第二端协作规程。

## 2. 工程范围请求和 Editor 入口

- [x] 2.1 修改既有 SceneHelper 的提交、消费和清理为同一工程作用域，不读、删或回退旧全局 UnityEditorSceneToOpen；用真实请求存储验证两个作用域互不消费，并静态核对原 Launcher 的所有调用点。
- [x] 2.2 在 Game.Editor 增加本工程启动入口，复用 SceneHelper；验证草稿／提交分离、无效配置进入 Play 前拒绝，以及 Play、编译、导入或测试忙碌时不覆盖已有操作。
- [x] 2.3 验证默认 Play 与关闭 Domain Reload、保留 Scene Reload 的连续两轮；关闭 Scene Reload 时拒绝，运行中编译重载报告中断，不自动恢复或创建新轮，旧上下文与订阅正确收尾。
- [x] 2.4 同步 Book/Unity启动与验证SOP.md 的请求交接、支持边界及已实现入口；核对说明不宣称第二 Editor、数据隔离或网络已经通过。

## 3. ET 边界与本轮入口 owner

- [x] 3.1 在 ProcedureET 启动 CodeRunner 前消费本轮上下文：普通／合法 legacy 继续旧入口，显式 Host／Client 缺能力报告 ProductBootstrapUnavailable；验证错误配置不进入 Demo，不创建 SceneType、监听或 Session。
- [x] 3.2 记录本轮是否实际启动 CodeRunner，验证未启动退出、正常退出和重复收尾只影响自身入口；同步 startup 增量与 Docs/AI/module-index.md 的后续产品接入位置，保留 GF 原关闭保存实现。

## 4. 单 Editor 集成与规划检查

- [x] 4.1 复用既有 UTF，必要时仅新增 UTF 测试 asmdef；按本工程实际 Pipeline 执行已授权编译与适用测试，核对终态和 Console groundTruth，空集／跳过／超时如实报告，不新增框架。
- [x] 4.2 在当前实际 Editor 中验证普通 Launcher、合法 legacy、显式角色拒绝、请求交接与连续 Play，分别报告配置和 ET 边界；缺环境记 NotRun，相关实测保持未完成，不要求第二 worktree 或真实 Host 会话才能验收本项。
- [x] 4.3 核对实现、五份规划与本项用法说明一致，通过本地引用、空白和 OpenSpec 严格检查；不把单 Editor 结果升级为双 Editor、数据隔离、Player 或产品网络结论。

## Workflow follow-up

- 当前按 openspec-apply-change 实施并同步相关用法/规格；仅作本地提交，禁止 push、发布、PR、合回 main 或自动归档。
- 第二 worktree 环境与协作归已建立的 [multiplayer-peer-workspace](../multiplayer-peer-workspace/proposal.md)，包含固定分支、ProductName／实际写路径、独立缓存与日志、AGENTS 职责、Pipeline 目标和 Orca 消息；本项不重复其任务。
- [product-session-bootstrap](../product-session-bootstrap/proposal.md) 独立承担产品 ET owner 树、本地消息发送／接收与远端 Session／UDP-KCP 原型；本项交接同一不可变配置，正式传输选型仍待裁决。
- 后续逐项讨论工作目录、产品会话和联调回归的实施顺序；固定 Player 包不是本项或日常联调的前置。
