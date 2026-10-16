# 合作结果与本端领取

## Purpose

合作每关成功后形成共享结果，合资格玩家能主动保存自己的独立进度，其他成员离线或不领取不阻止经营。本规格描述开局资格、不可变结果、凭证持久化与ACK、主动新建领取和整个Match正常关闭；区分运行世界、持久记录与玩家操作，沿原ET owner、邮箱和本端repository实现。

## Requirements

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


### Requirement: 合作进度待办与领取详情可访问

系统 SHALL 提供以下入口行为：在模式页、房间和结算入口显示已持久化本端可领数量，提供结果详情、状态分类和已领取记录去向；个人领取仍显式操作。

#### Scenario: 合作进度待办与领取详情的正常使用
- **WHEN** 有效凭证持久化后返回房间，随后主动领取并重开页面
- **THEN** 入口计数从真实可领变为已领取，详情能说明结果与本机记录去向，重复不另建档

### Requirement: 合作进度待办与领取详情保持事实与操作边界

系统 MUST 遵守以下边界：只统计 repository 实际可用凭证；网络收到但未持久化不能计为可领取。刷新不发 ACK／领取或关闭资格，状态未知／查询失败不显示零；不跨设备承诺唯一领取。

只读查询 MUST 在缺少既有本机身份资料时报告查询失败，不补建密钥、公钥或自身信任；显式身份操作仍沿原入口。已领去向按当前Slot/修订定位；离线进入原单机选档后仍需明确确认才开局，合作查看不得替玩家退役房间或取得Host控制。

#### Scenario: 查询时本机身份资料缺失
- **WHEN** 原凭证存在，但本机密钥/公钥或自身信任资料缺失
- **THEN** 待办显示查询失败，不新建替代身份或信任文件；恢复自有资料后显式重试读取实际状态

#### Scenario: 从已领去向进入离线选档
- **WHEN** 空闲Solo玩家明确选择按Slot和当前修订前往单机选档
- **THEN** 原选择页先完整只读检查，明确确认前不创建餐厅；修订不符显示记录已变化，旧跳转不重复创建页面

#### Scenario: 合作进度待办与领取详情的取消或状态变化
- **WHEN** 查询失败、凭证已关闭或只收到未完成持久化的通知
- **THEN** 不把未知当零或可领，不恢复被关闭资格，旧刷新不能覆盖新 owner

#### Scenario: 合作进度待办与领取详情关闭重开与旧回调
- **WHEN** 玩家关闭并重开本入口，随后旧页面回调或迟到结果到达
- **THEN** 页面只作用于当前有效上下文，原操作不因重开重复提交，旧结果不覆盖新选择，原合法入口仍可使用

## Sources

- [资格和结果](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/CooperativeResultSystem.cs)、[投递与ACK](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/CooperativeClaimMessages.cs)、[关闭](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProgressCloseMessages.cs)。
- [本端文件](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProgressClaimFiles.cs)、[领取界面](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryClaimsSystem.cs)、[离线协议](../../../Docs/Product/offline-progress-protocol.md)。
- [逐功能运行及限制](../../changes/archive/2026-10-15-cooperative-result-claims/execution-notes.md)。
- [cooperative-progress-inbox 逐项验收](../../changes/archive/2026-10-16-cooperative-progress-inbox/execution-notes.md)?
