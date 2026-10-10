# Spec Delta

## Purpose

定义合作做菜中协作关系、逻辑玩家、长期身份与连接的独立职责，使换连接、跨小关和更换存档不会错误地销毁玩家或改变归属。本文是交接资料捕获的未来目标契约，当前底座尚未实现这些业务能力。

## ADDED Requirements

### Requirement: 运行权威与长期身份分离

系统 MUST 区分 Host 世界权威、Client 操作与显示端、Participant 协作逻辑玩家及 Profile 长期身份；Host 不自动成为所加载存档的 Owner。本地玩家不得直接写权威世界。

#### Scenario: 房主加载他人授权存档
- **WHEN** 甲作为 Host 加载乙合法授权的存档基线
- **THEN** 甲裁决运行世界，存档归属仍按授权与 SaveSlot Owner 判断，不因房主角色变为甲所有

### Requirement: 协作独立于连接及小关

Match MUST 从成立持续到明确解散，可以暂时没有餐厅、跨多个小关，并在关闭现有餐厅后换存档。Connection 与 Level 结束不得自动递归销毁 Match 或逻辑 Participant。

#### Scenario: 小关结束且玩家断线
- **WHEN** 当前小关结束或某个连接意外断开
- **THEN** Match 继续存在，断线成员保持逻辑身份，其他成员可以继续协作

### Requirement: 成员身份与有效连接绑定

系统 MUST 将逻辑成员的释放归属放在 Match 一侧；每个成员同时最多一个有效连接绑定，新绑定撤销旧绑定的命令权限。成员身份不等于网络地址、Socket、ET 运行实例或存档 Profile。

#### Scenario: 原成员更换连接
- **WHEN** 同一 Match 验证原成员凭证后建立新绑定
- **THEN** 关联原 Participant，旧连接立即失去命令资格，不创建第二个逻辑玩家

#### Scenario: 旧连接释放
- **WHEN** 已被替换的旧连接进入资源回收
- **THEN** 不删除原 Participant，不释放新绑定，不影响其他成员

### Requirement: 断线恢复采用当前权威状态

系统 MUST 在断线时取消该连接尚未执行的命令并释放工作占用，保留已提交效果；原 Client 可凭有效身份找回原 Match 成员，取得当前完整状态，不重放断线期间历史命令。角色恢复初始化临时状态，不能用客户端旧状态回滚世界。

#### Scenario: 跨小关重新关联成员
- **WHEN** 原成员在同一 Match 的后续小关通过有效凭证返回
- **THEN** 保持逻辑成员身份，接收当前厨房、订单、供应、布局与 Buff，原小关命令无效

### Requirement: 凭证与协作终止边界

成员重连凭证 MUST 在同一 Match 内跨小关有效，并在 Host 关闭或 Match 解散后失效。首阶段不承诺主机迁移；运行结束不生成业务失败或伪造成功结果。

#### Scenario: 使用已经解散的协作身份
- **WHEN** Client 用旧 Match 凭证连接下一次 Host 运行
- **THEN** 不能取得新 Match 的成员权限，不搬迁旧世界对象或同步水位

### Requirement: 餐厅实例唯一与切换

Match MUST 在选中并校验存档后创建 LoadedSave 运行副本和唯一未关闭 RestaurantRuntime；同时最多一个未结束 Level。切换存档前先收尾并关闭现有餐厅，不保留多个暂停餐厅。

#### Scenario: 尚未选择存档
- **WHEN** 成员已经组成 Match，但没有校验通过的存档
- **THEN** 可以保留协作与连接，不创建虚假的餐厅运行实例

#### Scenario: 另一协作继续进度
- **WHEN** 新 Match 读取合法存档继续经营
- **THEN** 创建新的运行身份，不复制旧 ET 树、连接、待执行输入或快照水位

## Sources

- [设计交接 A2–A5、G3、G5](../../../../../../Docs/handoff/Cooking-设计交接-2026-10-09.md)
- [禁止项交接的身份与生命周期边界](../../../../../../Docs/handoff/Cooking-禁止项交接-2026-10-09.md)
- [规划归属与未决清单](../../design.md)
