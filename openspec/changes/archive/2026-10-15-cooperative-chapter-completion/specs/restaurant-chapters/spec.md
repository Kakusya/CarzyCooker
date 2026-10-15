# Spec Delta

## ADDED Requirements

### Requirement: 合作大关由配置定义终点

合作大关 SHALL 使用明确关序和终关标记；无效下一关引用须拒绝，不解释为通关，零星仍可自然完成。

#### Scenario: 合作终关自然收口
- **WHEN** 合作当前关是已确认配置中的终关且必要前厅活动已排空
- **THEN** 两端观察同一 Completed 和统计，不生成终关装修或第四关

### Requirement: 合作完成返回房间

合作终关 MUST 仅由 Host 当前合法控制请求结束餐厅并返回原 Match 房间；客户端结果先于退役可观察，不能宣称个人进度已保存。

#### Scenario: 终关确认重放
- **WHEN** Host 已成功返回房间，相同请求及旧界面回调再次到达
- **THEN** 不重复关闭新餐厅或解散房间，成员能重新开局，旧终关输入失效

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs)。
