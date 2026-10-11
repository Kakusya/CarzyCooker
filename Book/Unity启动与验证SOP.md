# Unity 启动与验证 SOP

本页随main来源功能同步；其中玩法运行与历史Passed属于源记录，第二工作区本轮Unity／UTF／双Editor结果另行报告，尚未执行项保持NotRun。

自动测试统一入口是 [auto-testing-sop](../.agents/skills/auto-testing-sop/SKILL.md)；本页只保留启动上下文，不重复测试编排。

本 SOP 统一本工程 Unity Editor 启动上下文。客户端从 `Assets/Launcher.unity` 进入；底座实际链路是 GF Procedure → ProcedurePreset → ProcedureET → ET.Init/CodeLoader → ET.Entry.Start。底座启动不表示做饭世界已启动。

## 当前可执行启动方式

| 场景 | 固定入口 | 当前行为 | 验证方式 |
|---|---|---|---|
| 客户端 | Unity/Assets/Launcher.unity，工具栏 Launcher | ProcedurePreset 直接进入 ProcedureET；按 ET CodeMode 装载程序集；无显式请求时默认 Solo 地图与 HUD | Editor 状态、编译、Console 和实际画面；Play 按当轮授权 |
| UTF 测试 | 已打开本工程 Editor，Pipeline list_tests/run_tests | 只执行实际发现并已授权的用例 | 终态、断言、堆栈、Console；空集 NotRun |
| Excel 导表 | Game/Tool/ExcelExporter | 工具 AppType=ExcelExporter，不进入 Play | Luban check/gen 输出及 diff |

没有迁入参考的玩法 Profile、座位、投币前置、工具栏 Demo 或专用测试入口。

## 显式单实例请求

通过 `Game/Multiplayer/Instance Launch` 统一校验并提交本轮配置，再复用 Launcher。场景及实例请求只存本工程完整路径范围的 SessionState，旧全局 EditorPrefs 请求不读取、不清理、不回退。窗口草稿和已提交请求分开，工具栏普通 Launcher 清理本工程显式请求；没有显式请求时 ProcedureET 建立本轮默认 Solo 配置，直接显示格子地图与 HUD。显式 legacy 才进入旧 ET 路线；显式产品请求及失败不被默认值覆盖。

支持默认 Play 和关闭 Domain Reload、保留 Scene Reload；显式启动拒绝关闭 Scene Reload。正常域重载在 AfterAssembliesLoaded 交接配置，BeforeSceneLoad 消费 Launcher 场景，AfterSceneLoad 清场景请求；进入 EditMode 清本轮请求并收尾。Play 中编译域重载报告 `PlayDomainReloadInterrupted` 并结束自身显式实例 Play，保留中断失败，不透明恢复或重启；普通 Launcher 不由本项自动结束。

`Prepared` 不等于 ET 或产品 Ready；适用代码装载后进入 ProductRoot，缺能力仍失败为 `ProductBootstrapUnavailable`。Host 必须实际监听且本地 Joined 才 HostReady，Client 须本人确认和名单才 ClientReady；普通 Launcher 进入 Solo ProductRoot，只有显式 legacy 保留 Main。ProcedureET 只停止本轮实际启动的 CodeRunner，Init/原 OnShutdown Invoke 在 World.Dispose 前结束产品等待和监听，失败保留具体终态。GF 设置保存/关闭仍归原 owner。配置/作用域小集、普通/legacy、两种 Play 模式的连续两轮及真实编译中断收尾见 [启动执行记录](../openspec/changes/archive/2026-10-11-multiplayer-instance-bootstrap/execution-notes.md)；本地/KCP 与产品退出见 [产品执行记录](../openspec/changes/archive/2026-10-11-product-session-bootstrap/execution-notes.md)。

格子样机在同一窗口增加 `solo`：无 listen／connect，创建本地权威及本地玩家，经原 ET 消息加入后为 SoloReady，不创建网络监听。`host` 仍使用实际 loopback endpoint，`client` 指向它。会话 Ready 与地图 GridReady 分开：地图还需匹配世界／本人角色快照、Board 与 HUD 均加载完成，具体玩法及验证见 [格子样机](../openspec/changes/archive/2026-10-11-portrait-grid-kitchen-prototype/execution-notes.md)。

PlayerSettings 默认 Portrait、窗口 540×960；Editor 体验选择 GameView 的 9:16 或 540×960。点击地面行走，点击台面或 HUD 目标执行默认拿放，纵向拖动查看下方餐厅占位；本轮没有菜谱加工或订单。PlayerSettings 已保存不等于手机包体验收。
