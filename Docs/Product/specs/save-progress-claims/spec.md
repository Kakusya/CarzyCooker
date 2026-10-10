# save-progress-claims Target Specification

> 状态：目标规格文档已定稿，业务尚未实现；已确认、授权补足与未决范围见 [规格索引](../README.md)。

## Purpose

定义存档归属、授权导入、成功检查点、共同结果与主动领取的目标契约，避免房主身份、传输确认和持久化混淆。本文捕获未来产品语义，授权认证、密钥与断电协议尚未设计，不代表已有安全或存档实现。

## Requirements

### Requirement: 三层权威与稳定业务身份

系统 MUST 分离 Profile 文件访问授权、SaveSlot 持久基线和 Host 世界写权限；ProfileId、SaveSlotId、revision 与运行实例身份独立。未选中存档只作为文件或轻量元数据，不展开世界；路径、句柄及引擎对象不得成为传输业务身份。

#### Scenario: 浏览多份存档
- **WHEN** 本地扫描多个 SaveSlot 并选中其中一个
- **THEN** 只为校验通过的选中基线创建运行副本，每次加载使用新餐厅身份，扫描不实例化其他世界

### Requirement: 平台生态隔离

PC 与 Android MUST 保持独立产品生态，不共享 Match、Profile、SaveSlot、授权包、SaveResult 或领取凭证，也不互联或互相导入。平台适配器定位应用私有持久目录，领域层只处理逻辑 ID；共享纯逻辑不意味着共享数据。

#### Scenario: 跨平台导入领取凭证
- **WHEN** Android 尝试导入 PC 成功结果或授权包
- **THEN** 明确拒绝，不因格式可读而承认资格

### Requirement: 授权导入立即成为独立分支

同平台合法 bearer 授权导入 MUST 创建新的 SaveSlot，归接收 Profile 所有，记录直接来源 Profile／Slot／Revision、根 Profile／Slot 及授权身份。源档与分支永远独立，不自动合并或写回；谱系只用于追踪，不赋予写权限。

#### Scenario: 原 Owner 离线时继续分支
- **WHEN** 接收者已完成合法授权导入，原 Owner 不在线
- **THEN** 可独立推进接收者分支，不等待源档、不覆盖源档或其他分支

### Requirement: 成功时生成不可变共同结果

Level MUST 在启动时冻结合资格 Participant／Profile 名单，成功原子提交后生成不可变 SaveResult 和各合资格 Profile 的独立稳定凭证。迟加入者不能领取本关结果；未领取或离线不回滚成功、不阻止别人，提交前中断不产生可领取结果。

#### Scenario: 关中加入新玩家
- **WHEN** 新成员在本关开局冻结资格后加入 Match
- **THEN** 不获得本关领取资格，后续小关资格按各自开局名单确定

#### Scenario: 成功提交前 Host 崩溃
- **WHEN** 世界尚未成功提交就结束运行
- **THEN** 不产生该关 SaveResult 或领取凭证，也不伪装低星成功

### Requirement: 持久化凭证后确认送达

Client MUST 在持久化领取凭证后才发送 ACK；ACK 不等于领取。Host 在 Match 存续期间重发同一未 ACK 凭证，直到 ACK 或 Match 终止；重发不创建新身份，不阻塞其他玩家。

#### Scenario: 同一凭证重发
- **WHEN** ACK 尚未到达，Host 再次发送凭证
- **THEN** 结果和资格身份保持不变，Client 不生成第二份可独立消费资格

### Requirement: 主动新建或显式覆盖

领取 MUST 由玩家主动选择新建 SaveSlot 或显式有损替换授权谱系内允许目标，不自动写所有设备、不合并。新建归领取者；覆盖目标可属于其他 Profile，但必须验证目标身份、权限和预期 revision，保留其 Owner。

#### Scenario: 知道源存档谱系但无覆盖权
- **WHEN** 玩家仅拥有来源 ID，尝试覆盖该源 SaveSlot
- **THEN** 不因谱系关系批准写入，不消耗领取资格

### Requirement: 领取原子提交及失败可重试

领取写入 MUST 在事务提交期间保护旧记录，原子成功后才记录领取结果并消耗资格；成功后不保留上一版本或用户可恢复备份。身份、授权、修订、容量或磁盘失败保留凭证且可重试，不破坏有效旧记录。

#### Scenario: 覆盖时修订冲突
- **WHEN** 目标 revision 与预期不符
- **THEN** 原记录不变，凭证仍可用，不标记成功或静默覆盖较新档

### Requirement: 本地领取幂等与跨设备限制

同一设备的 repository MUST 以 SaveResultId 和合资格 ProfileId 保证最多成功领取一次，重复请求返回原结果；无中心服务时不承诺跨设备全局一次性，同平台凭证副本可在另一设备形成独立本地分支。

#### Scenario: 同设备重复领取
- **WHEN** 已成功领取的相同结果和 Profile 再次请求新建或覆盖
- **THEN** 返回原领取结果，不创建另一 SaveSlot 或再次覆盖

#### Scenario: 凭证复制到另一设备
- **WHEN** 无中心服务且合法凭证在同平台另一设备领取
- **THEN** 按该设备独立 repository 处理，不虚称已经实现全局防重复

### Requirement: 正常关闭与异常终止按本端观察

Client MUST 在收到并持久化 normal-close receipt 后关闭本地未领取资格；没有收到该回执的异常终止可保留已经持久化凭证。不同 Client 的终止观察可以不同，正常关闭不能撤销未观察回执的另一设备副本。

#### Scenario: 两个 Client 对关闭观察不同
- **WHEN** 一个 Client 持久化正常关闭回执，另一个已离线并未收到
- **THEN** 前者关闭其本地未领取资格，后者按异常观察保留已持久化资格，不推断全设备撤销

### Requirement: 成功检查点与技术载荷分离

产品检查点 MUST 只在小关成功、结算收口后写入，检查预期 revision 并暂存完整提交；准备、营业、半条命令及失败现场不得覆盖最近成功记录。同步快照、诊断恢复和产品存档分别处理，损坏或未知版本不得静默当成新档。

#### Scenario: 收到运行中完整快照
- **WHEN** Client 安装营业中的 Host 同步快照
- **THEN** 只更新运行投影，不把快照当成功检查点写入 SaveSlot

### Requirement: 运行对象与离线凭证寿命分离

SaveResult 和 Claim 运行对象 MUST 归 Match 管理，不能活过其释放；终止前按允许规则导出稳定离线凭证。资源 Dispose 不产生结算或授权，正常关闭认证与离线凭证安全协议须另行设计后实施。

#### Scenario: Match 释放后处理已保存资格
- **WHEN** Client 按异常观察保留合法持久凭证并离线领取
- **THEN** 使用稳定凭证和本地授权规则，不依赖已经释放的 Host Entity 或旧连接

## Sources

- [设计交接 B1–B8、G5](../../../handoff/Cooking-设计交接-2026-10-09.md)
- [禁止项交接：存档、授权和领取](../../../handoff/Cooking-禁止项交接-2026-10-09.md)
- [安全协议、断电模型与状态清单](../../architecture.md)
