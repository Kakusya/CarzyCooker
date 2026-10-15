# Spec Delta

## ADDED Requirements

### Requirement: 现有伙伴可稳定识别

现有一位固定伙伴 SHALL 以原稳定定义、已确认称呼“小麦”和固定岗位展示，跨关或新运行不变成任意招募劳动力，最终人数不由当前一位配置推导。

#### Scenario: 跨关再见伙伴
- **WHEN** 玩家进入下一关或恢复合法成功档
- **THEN** 仍能识别相同固定搭档，关内成长按既有合同重置

### Requirement: 准备态聊天不改经营

准备态聊天 MUST 为可选只读展示，不修改物品、订单、成长、时间或开店状态；关切换和视图重开不遗留旧动作回调。

#### Scenario: 聊天后开店
- **WHEN** 玩家打开并关闭伙伴聊天再点击开店
- **THEN** 聊天期间业务无额外效果，开店只发送一次合法请求

### Requirement: 工作反馈只观察提交

伙伴完成和成长反馈 SHALL 只表现已提交事实，不重复提交工作或增加经验；旧关与重复消息不在当前关重复庆祝。

#### Scenario: 完成通知重复
- **WHEN** 同一伙伴完成通知被重复投递或视图重开
- **THEN** 完成次数不增加，当前提示不把旧关当成新成长

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/FrontService.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/FrontService.cs)。
