# Design

## Context

动机见 [proposal.md](proposal.md)。GridCompanionSchedule当前按Ask/Wash再按Id排序，只让空闲且无待答复认领的角色继续；FrontWork保持单执行者和冻结耗时。用户确认偏好与控制权；这是原资格内的顺序选择，不把“岗位”解释成新工种。状态跨Level且有Host控制，所以需要设计。

## Goals / Non-Goals

**Goals:** 原小麦的下一次合法调度可选，当前工作及成长保持原规则。

**Non-Goals:** 不新增伙伴、清桌能力、强制打断、招募或长期人物存档，不改配置源。

## Decisions

```text
Solo / Host Fiber / Scene(ProductRoot) + ProcessInnerSender
  +-- Scene(ProductHost)
  |     +-- HostSessionComponent + Ordered MailBox -> HostPeer
  |     +-- Child Match -> Child RestaurantRuntime
  |           +-- Component CompanionPreferenceState (runtime preference)
  |           +-- Component GridKitchenComponent -> original scheduler
  |           +-- Child Level
  |                 +-- Child CompanionRole / FrontWork
  |                 +-- Component CompanionGrowth / FrontWorkCommands
  +-- Scene(ProductClient)
        +-- ClientSessionComponent + Ordered MailBox -> GridClientComponent
        +-- GridPresentationComponent -> UIComponent/UIGridKitchen
Remote Fiber / ProductRoot -> ProductClient
  +-- Session + ProductClientBinding -> same projection/HUD owners
```

偏好以具名enum存Restaurant组件，初始自动。Level角色仍持有真实工作、路径、认领序号和冻结耗时；调度每次在原空闲门槛后读取偏好，按优先类别及原Id顺序选合法工作，无候选回退另一原有类别。自动首版等于原询问优先，界面明确当前默认，不伪称动态AI。相较把偏好塞入Role成长状态，Restaurant归属自然保留本轮跨关而不影响新关成长清零。

设置请求带当前世界/关/绑定/命令身份与enum值，成员取可信Peer。Host本地用Root ProcessInnerSender.Call到HostPeer ActorId/ProductHost邮箱，远端经Session和ProductClientBinding进入同一Peer处理，再到HostSession authority ActorId；后者核对Solo/Host权限、当前Restaurant和未暂停/未Completed阶段，沿原命令序号/去重在Host提交偏好。合作Client的请求即便绕UI也拒绝，不能靠UI隐藏按钮代替权限。已有Role.WorkId/Path/ClaimSerial/Progress/FrozenTicks均不修改。

偏好是原Host有序邮箱内立即提交的餐厅控制值，不增加工作队列或另一个业务时钟；先记录原命令身份/结果，再经原CommitGridState广播，不推进工作Tick。真正认领仍由下一次原ScheduleCompanions业务步执行。Proto尾文件为17个Z的CompanionPreference，既有189个opcode保持，新增四个请求/响应（含仅本地内部提交），快照追加字段72；wire版本35，成功文件版本不变。

Restaurant创建后建偏好组件，Level创建角色时读取当前值；关退出只销毁角色/成长，餐厅退出才销毁偏好，新餐厅/成功文件恢复重建自动。单成员退出不停止共用sender，也不把Host退出解释为可给Client控制权；整体退出按原owner拆树。

通过Proto源增加控制消息及伙伴快照值，新增消息放排序尾部保留旧opcode，实际wire版本/白名单随合同更新。GridBroadcast本地sender.Send到ClientSession.GetActorId()/ProductClient邮箱、远端经Session到ProductClientBinding；同一GridSnapshotHandler验证身份后安装独立值，再发原GridProjectionApplied，handler finally释放DTO。UI等待及答复沿原Call/AsyncOwner收尾，await后校验EntityRef/HUD轮次；不持消息对象、不增加SceneType.All订阅，不用Event替代控制或答复。

原伙伴区展示已提交偏好，明确选择才请求，暂停只读。原聊天/成长/工作提示不成为设置入口的第二状态源；换关/恢复/池化由原身份guard撤旧回调。

## Risks / Trade-offs

- [切换重置工作] -> 同时有Ask/Wash、认领/前往/半工作时切换，断言原Id/进度/冻结耗时持续。
- [只测排序未测经营] -> 实际Solo与真实KCP完成询问、洗碗及自然收口，原成长结果可复用未变部分。
- [旧档扩大] -> 不保存本轮偏好，真实旧成功文件恢复仍默认自动且不重写。

## Migration Plan

无需Luban或成功文件升版。先原Host控制/偏好与调度，再原投影/HUD，按execution-notes最小验证后同步主规格；回退恢复原排序，不删除用户文件，不自动提交或归档。
