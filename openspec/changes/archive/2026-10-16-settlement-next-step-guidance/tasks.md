# Tasks

本案已按后续18案实施授权接入并完成范围内实际验收，证据见execution-notes。沿[设计](design.md)和[增量规格](specs/restaurant-ui/spec.md)，不改表和生成物，不自动增加设备或新工具范围。

## 1. 结算到下一步的导航与局部验证

- [x] 1.1 在原 owner 中落实本案事实派生、选择和显式操作交接：集中派生展示状态，继续用原 RefreshSaveStatus／RenovationReadyForContinue 和 LevelControlAction；不会为统一按钮名称重写 Continue／SaveAndContinue／FinishChapter 的业务含义。下关菜单从有效定义查，不臆造下关奖励。 补齐字段语义注释，并增加／运行有行为断言的相关 PlayMode UTF，验证Solo持久／试玩、合作Host／Client、零星／终关、失败重试和旧回调；先发现真实用例和过滤范围，文件读取／授权相关使用真实临时文件，不以文本拼接测试代替行为。对应结果记入 execution-notes。
- [x] 1.2 沿原 UIGridKitchen 接入“结算到下一步的导航”，保持既有合法入口、角色权限与本轮回调 guard。运行普通 Launcher 的实际 UI UTF，覆盖“非终关保存失败后重试，或零星完成终关”以及“Client 结算只读、旧继续回调或装修尚未准备”、关闭重开／旧结果和一次点击一次请求；涉及合作时使用 Host 本地及真实 KCP Client。同步已有操作说明中本案实际入口，并检查文档与行为一致，不只改按钮文案。

## 2. 真实入口集成

- [x] 2.1 从正常入口实际使用本案并返回原流程，确认 540×960 可读可点且原制作／经营或记录流程仍可用；将各 Scenario 逐项对应真实用例／操作／结果及原始输出写入 execution-notes，复用已覆盖且行为未变的结果。更新 CarzyCooker当前进度.md 的实际交付范围，检查引用并运行 `openspec validate settlement-next-step-guidance --strict`。功能失败、Skipped、空集或未运行时相关任务保持未完成；严格格式通过不代替功能验收。

本案不包含提交、推送、归档、主规格同步或设备构建。其他案的共享文件按路线协调，不默认重跑全仓或全设备。
