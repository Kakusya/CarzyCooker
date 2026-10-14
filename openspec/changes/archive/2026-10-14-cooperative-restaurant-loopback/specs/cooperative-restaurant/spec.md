# Spec Delta

## Purpose

定义普通本机 Host／Client 双进程入口下完整合作餐厅的组合体验，连接已交付的共同制作、前厅、设施和供应行为。两种工作区角色排列遵守同一权限与退出规则，验收范围明确区别于同 Editor 样机、独立 Player 和跨设备联网。

## ADDED Requirements

### Requirement: 普通双进程入口完成连续合作

本机 Host 与 Client SHALL 从普通产品入口进入同一持续餐厅，分工完成汤与吐司、真实顾客交付、餐具回收、设施清理和有限补货，并按自然前厅收口进入下一关。两端观察同版事实；同 Editor 结果不能代替此双进程体验。

#### Scenario: 两工作区交换运行角色
- **WHEN** 主工作区与第二工作区使用相同交付基线，分别以 Host／Client 两种排列运行
- **THEN** 两种排列均能分工制作与交付、实际补货／清理并自然换关，开发工作区身份不改变玩法权限

### Requirement: 完整合作保持共同控制与退出边界

完整餐厅 MUST 沿用 Host 本地玩家的共享控制资格和唯一业务时间，队友个人操作进入同一裁决。暂停冻结各域进度，单成员失联保留实物及已提交事实，其他成员继续；Host 整体退出只终止本轮，旧消息不能写下一轮。

#### Scenario: 服务补货途中暂停与失联
- **WHEN** 餐厅存在加工、顾客工作及在途货物，Host 暂停恢复后 Client 失联
- **THEN** 暂停中业务进度不变，恢复后已开始工作接续；失联只释放该成员认领并落物一次，Host 能继续服务与接收已提交货物

#### Scenario: 退出后再次启动普通入口
- **WHEN** Host 结束有在途操作的本轮后再次启动
- **THEN** 新运行使用自身世界与成员，旧等待和通知不恢复旧物资、分数或成功结果

### Requirement: 合作交付范围明确

本能力 MUST 标明只交付本机 loopback 内存餐厅。合法新 Join 不等于原成员恢复，退出不报告持久保存，当前通过不代表 Player、LAN、公网、联机装修或存档已支持。

#### Scenario: 查看联机进度
- **WHEN** 本案验收结果写入当前进度
- **THEN** 明确记录双 Editor 两种角色排列与逐功能结果，并保留独立 Player、跨设备、身份恢复和持久经营的未交付状态

## Sources

- [核心合同](../../../2026-10-14-cooperative-restaurant-core/specs/cooperative-kitchen/spec.md)、[前厅合同](../../../2026-10-14-cooperative-front-service/specs/orders-front-service/spec.md)、[物流合同](../../../2026-10-14-cooperative-cleanup-supply/specs/cooperative-kitchen-logistics/spec.md)。
- [集成设计](../../design.md)、[协作规程](../../../../../../Docs/AI/multiplayer-peer-workflow.md)。
