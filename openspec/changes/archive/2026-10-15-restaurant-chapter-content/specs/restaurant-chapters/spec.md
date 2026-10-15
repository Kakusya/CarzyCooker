# Spec Delta

## MODIFIED Requirements

### Requirement: 三关与明确终关
Solo 首个大关 MUST 由配置定义三关及明确终关。既有基础/首饮配置与旧档保持原菜单语义；新内容章允许逐关配置不同菜单，仍须满足当前解锁许可和完整生产闭包。旧有效检查点按保存的下一关配置恢复原菜单、布局和有限库存，不因新开菜单追加物资。无效下一关引用不得被当作终关，零星完成仍按普通成功推进。

#### Scenario: 配置引用缺失
- **WHEN** 非终关指向不存在的下一关
- **THEN** 配置被拒绝，不报告玩家已经通关

#### Scenario: 新开饮品菜单与恢复基础菜单
- **WHEN** 玩家新开首饮三关，或恢复原两菜菜单的有效检查点
- **THEN** 新开使用饮品变体，旧档保持原菜单及实物，两者都按各自明确三关链推进

## ADDED Requirements

### Requirement: 章节选择与有限内容包

系统 SHALL 在Solo和合作各提供两个可选内容大关，每个三小关，以配置列出明确关序和终点；模式/菜单对应的旧配置链不能充当两个内容大关，完成记录可查看并可另起独立复玩；旧有效档仍按保存语义继续。

#### Scenario: 选择另一大关新开
- **WHEN** 玩家从章节入口选择另一已交付大关
- **THEN** 使用该大关初始配置创建新餐厅，不自动搬入上一大关实物或改变旧档

### Requirement: 递进关卡生产闭包

内容包每关 MUST 具备允许菜单全部供应、设备、容器及阶段能力，安排可执行的来客和补货流程；零星仍自然成功。

#### Scenario: 新关菜单缺设备
- **WHEN** 配置加入需要未供应能力的菜品
- **THEN** 校验拒绝该内容，不靠免费成品或调试物资掩盖缺口

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Design/Excel/ET/Datas/Product/LevelConfig.xlsx](../../../../../../Design/Excel/ET/Datas/Product/LevelConfig.xlsx)。
