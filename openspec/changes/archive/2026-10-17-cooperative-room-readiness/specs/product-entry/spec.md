# Spec Delta

## ADDED Requirements

### Requirement: 合作开局内容有共享提案

Host SHALL 在房间发布当前章节/菜单或合法来源记录的开局提案；全体当前已连接成员能查看同一版本并明确准备或取消。准备针对提案版本和当前成员绑定，不创建餐厅、不写进度，也不免除原来源授权、身份和完整开局校验；Solo及进行中餐厅加入/恢复保持原流程。

#### Scenario: 同一提案准备
- **WHEN** Host发布一个章节提案，远端查看后选择准备
- **THEN** 双方显示同一提案与远端准备状态，尚未开始时没有新餐厅，旧版本准备拒绝

#### Scenario: 显式Host启动与合法来源载入
- **WHEN** 玩家使用显式Host参数启动或在房间载入已校验合作来源
- **THEN** 只发布对应内容/来源提案并等待成员准备，不自动Ready或直接建餐厅；原来源身份和修订保持可见，仍由全员准备后Host明确开局

### Requirement: 全部连接成员准备后由Host开局

Host MUST 仅在全部当前已连接成员（含Host）对当前提案准备且原开局条件满足时启动，不能强开。内容、来源记录修订或成员加入/退出/失联/恢复绑定变化清除全部准备；房间新建和餐厅退役回房间后重新准备。重复/迟到Ready或Start不应用于新版本，不重复创建餐厅。

#### Scenario: 新成员加入和Host尝试提前开局
- **WHEN** 已准备成员等待时另一成员加入，Host随后尝试开局
- **THEN** 准备全部失效，开局明确拒绝，成员重新对当前提案准备后Host才能只创建一次餐厅

## Sources

- [现有房间裁决](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRoomSystem.cs)。
- [设计](../../design.md)。
