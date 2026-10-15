# Spec Delta

## Purpose

合作房间可选择合法检查点建立新的持续餐厅，文件归属与 Host 世界裁决分开。合作房间选择有权限的持久基线并恢复新餐厅。本能力区分已提交世界事实、持久记录和玩家操作，保证拒绝或中断有明确边界，并为后续有限内容扩展提供可观察的合同。

## ADDED Requirements

### Requirement: 选中基线才创建合作餐厅

合作房间 MUST 只在选中基线的身份、权限、修订、格式和完整载荷均通过校验后创建唯一餐厅；浏览只读元数据，不实例化额外世界。

#### Scenario: 加载损坏记录
- **WHEN** Host 选择载荷损坏或权限不符的记录
- **THEN** 拒绝并保留房间及原文件，不静默新开、不创建半份厨房

### Requirement: Host 与文件 Owner 分离

Host MUST 仅裁决本轮世界；选择或运行他人合法授权基线不改变源文件 Owner，不由 Host 身份赋予写入权限。

#### Scenario: 非 Owner Host 运行授权基线
- **WHEN** 甲作为 Host 取得乙协议允许的基线
- **THEN** 甲建立新运行世界，乙的源文件归属和修订不因加载发生变化

### Requirement: 恢复新身份及真实库存

恢复 MUST 从已校验载荷重建实物、份数、加工冻结参数、布局升级和供应剩余状态，使用新运行身份，不叠加初始物资或重放旧命令。

#### Scenario: 恢复半批次与在途货
- **WHEN** 合作载入含剩余一份汤及未到货采购的合法记录
- **THEN** 两端就绪后观察同一真实余量和剩余运输时间，旧关命令失效

### Requirement: 长期身份须证明当前绑定

长期 Profile MUST 通过本次 Match、连接绑定和一次性挑战的私钥证明验证；公钥须经本端明确导入绑定，显示名或 ResumeCredential 不能替代证明。同平台、用途、目标 Host/Match、完整基线签名及源修订均须匹配。

#### Scenario: 原成员恢复后再次证明
- **WHEN** 成员以有效 ResumeCredential 恢复同一逻辑成员但获得新的连接绑定
- **THEN** 原 Profile 身份保留，新绑定先重新证明私钥持有；旧挑战证明不能取得当前绑定的长期授权

#### Scenario: 读取取消与迟到
- **WHEN** 玩家在文件读取阶段取消，原文件答复随后到达
- **THEN** 答复按原请求释放，不提交创建餐厅；已经提交的世界以实际投影为准，界面不再提供读取取消

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs)。
