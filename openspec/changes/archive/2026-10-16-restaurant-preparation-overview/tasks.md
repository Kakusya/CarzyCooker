# Tasks

本案实施与验收见 execution-notes.md；当前进展以任务勾选为准。实现前读取 [设计](design.md) 和 [增量规格](specs/restaurant-ui/spec.md)，加载 apply、carzycooker-ui、auto-testing-sop；Editor 操作另加载 unity-cli。不改表和生成物，不自动增加设备或新工具范围。

## 1. 开店准备总览与局部验证

- [x] 1.1 在原 owner 中落实本案事实派生、选择和显式操作交接：只从 LevelPhase、BuffDrawVersion／RequireBuffChoice／SelectedBuffId、当前许可和原投影计算说明；入口调用既有功能，不创建 Ready 状态、持久清单或自动操作。阶段变化即失效，暂停可读但不绕过控制限制。 补齐字段语义注释，并增加／运行有行为断言的相关 PlayMode UTF，验证Buff 未就绪／必选／可选、Solo／Host／Client 与暂停／换关；先发现真实用例和过滤范围，文件读取／授权相关使用真实临时文件，不以文本拼接测试代替行为。对应结果记入 execution-notes。
- [x] 1.2 沿原 UIGridKitchen 接入“开店准备总览”，保持既有合法入口、角色权限与本轮回调 guard。运行普通 Launcher 的实际 UI UTF，覆盖“必选加成未选时打开准备总览，再完成原选择”以及“厨房没有预制成品，或远端 Client 查看总览”、关闭重开／旧结果和一次点击一次请求；涉及合作时使用 Host 本地及真实 KCP Client。同步已有操作说明中本案实际入口，并检查文档与行为一致，不只改按钮文案。

## 2. 真实入口集成

- [x] 2.1 从正常入口实际使用本案并返回原流程，确认 540×960 可读可点且原制作／经营或记录流程仍可用；将各 Scenario 逐项对应真实用例／操作／结果及原始输出写入 execution-notes，复用已覆盖且行为未变的结果。更新 CarzyCooker当前进度.md 的实际交付范围，检查引用并运行 `openspec validate restaurant-preparation-overview --strict`。功能失败、Skipped、空集或未运行时相关任务保持未完成；严格格式通过不代替功能验收。

本案不包含提交、推送、归档、主规格同步或设备构建。其他案的共享文件按路线协调，不默认重跑全仓或全设备。
