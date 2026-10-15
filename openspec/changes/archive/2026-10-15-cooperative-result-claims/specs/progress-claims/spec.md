# Spec Delta

## Purpose

合作每关成功后形成共享结果，合资格玩家能主动保存自己的独立进度，其他成员离线或不领取不阻止经营。合作不可变成功结果、逐人资格投递及主动新建领取。本能力区分已提交世界事实、持久记录和玩家操作，保证拒绝或中断有明确边界，并为后续有限内容扩展提供可观察的合同。

## ADDED Requirements

### Requirement: 开局资格与不可变共同结果

Level MUST 在开始提交时冻结合资格 Profile，成功、前厅收口且必要结算选择确定后只生成一次不可变结果及各资格凭证；迟加入或提交前中断不产生该关领取资格。

#### Scenario: 迟加入与原成员返回
- **WHEN** 本关开始后新成员加入，原合资格成员断线后返回
- **THEN** 新成员不获本关资格，原成员按同一已验证 Profile 保留资格，不因换连接多发结果

### Requirement: 凭证持久化先于 ACK

Client MUST 持久化有效凭证后才 ACK；Host 在 Match 存续期间重发同一未 ACK 身份，重发不新增资格且不阻塞他人。

#### Scenario: ACK 丢失
- **WHEN** Client 已保存凭证但 ACK 丢失
- **THEN** Host 重发相同凭证，Client 去重并可再次确认，不形成两份独立领取资格

### Requirement: 主动新建与失败保留

领取 MUST 由玩家显式请求新建文件，以结果及合资格 Profile 保证本设备一次成功；目标记录与领取回执同次发布；发布前失败保留凭证和旧文件，重复请求或发布后答复丢失均返回原成功记录。

#### Scenario: 写入失败后重试
- **WHEN** 领取写盘失败后玩家重试成功，再重复请求
- **THEN** 失败不消耗资格，成功仅创建一档，重复返回同一记录

### Requirement: 正常关闭按本端持久观察

Client MUST 仅在收到并持久化经验证的整个 Match 正常关闭回执后关闭本地未领取资格；未观察回执的异常终止可以保留已持久有效凭证。仅关闭餐厅返回原房间不关闭整个 Match 的资格，已领取记录不受关闭影响。

#### Scenario: 两端观察不同
- **WHEN** 一端保存正常关闭回执，另一端已离线
- **THEN** 两端分别按实际观察处理，不假定全设备撤销或由 Dispose 临时生成结果

#### Scenario: 回执先于凭证
- **WHEN** 同一 Match 的正常关闭回执已经持久化，随后其凭证迟到
- **THEN** 不重新开放资格；旧Match或未经验证的回执不关闭当前资格

#### Scenario: 回房间后再开餐厅
- **WHEN** Host 只退役餐厅并在同一房间开启下一轮餐厅
- **THEN** 不因上轮回房间把整个Match标为已关闭，新餐厅按自己的开局资格产生结果

### Requirement: Host 保存成功后公布前进

Host 显式选择保存并继续或保存结束 MUST 等待本端合法提交成功；写入失败保留结算及结果并可重试。其他成员离线或未领取不阻塞本端成功提交，Host 不因世界控制权取得源档覆盖权限。

#### Scenario: Host 保存失败而队友已领取
- **WHEN** 队友已合法领取，Host 本端保存失败后重试
- **THEN** Host 保持原结算且不宣称已保存；重试成功后只前进一次，不重复队友记录

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs)。
