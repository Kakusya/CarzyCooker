# Design

## Context

见 [proposal.md](proposal.md)。`ProductOutcomeHandlers`在校验身份、版本和serial后发布KitchenWorkOutcomeApplied/ShipmentOutcomeApplied独立值事件。读取LastOutcome只保留最后一项，不能满足同一步多结果，本案按已存在的每项事件接收。

## Goals / Non-Goals

**Goals:** 有限提醒、显式查看、按类本机开关及可靠去重。

**Non-Goals:** 不增加音效/通知服务、领域请求、自动操作和持久事件历史。

## Decisions

### owner与消息路径

```text
Host Fiber / Scene(ProductRoot) + ProcessInnerSender
  +-- Scene(ProductHost) -> HostSessionComponent + Ordered MailBox -> HostPeer
  |     +-- Child Match -> Child RestaurantRuntime
  |           +-- Component GridKitchenComponent: processing / shipments
  |           +-- Child Level: Orders
  +-- Scene(ProductClient)
        +-- ClientSessionComponent + Ordered MailBox -> GridClientComponent
        +-- Component notice state (current binding, bounded values)
        +-- GridPresentationComponent -> UIComponent / UIGridKitchen
Remote Fiber / Scene(ProductRoot) -> Scene(ProductClient)
  +-- Session + ProductClientBinding -> same client notice / projection owners
```

通知状态与HUD分离，挂原ProductClient owner；仅保存最多8条显示值、必要的对象/阶段去重键和当前绑定基线，不建第二份世界或事件总线。使用该领域原具名enum，不用整数常量分类。

Host先提交再发布原Outcome。本地经Root sender向ClientSession ActorId投递，远端经原Session/KCP和ProductClientBinding到ProductClient handler；handler复制值并finally释放消息。新增接收器仅订阅原ProductClient范围事件，安装当前投影后解析目标，不保留DTO或跨场景Host引用。到货仅 `ShipmentStage.Ready` 转换，加工仅Finished；同一步每条独立接收。

临期按当前Waiting订单和grid.Tick判断阈值跨越；首次有效投影/恢复记录基线、不把已有临期视为新发生。重复高版本快照不重播；暂停冻结业务Tick，失联不使用Unity帧时间推进。关闭某类后清该类当前提醒并继续推进去重水位，重新开启不重播过去结果。

显示采用原HUD信息区中可查看的有限列表；显式查看跳原补货/工位/订单入口，过期目标只说明已变化。HUD关闭只结束显示轮次，去重随ProductClient保留；换关清旧订单提醒，持续厨房实体按原身份排除重复；恢复新绑定重建基线。队列满时移除最早显示行，正文标明仅列最近8项，不虚构完整历史。

列表不设墙钟自动消失；暂停不推进临期或业务事实，列表仅因新项挤出、类别关闭、相应owner退出或旧关订单清理而变化。设置提交后由原ProductRoot本地库发布到当前ProductClient的观察事件，不增加网络消息；只提交成功后刷新提醒开关。

偏好读取 [本机设置案](../2026-10-17-product-input-readability-settings/design.md)，新增该案提醒字段需保留旧字段默认。无新Call/等待者，Event非请求答复，Invoke不参与裁决；通知关闭不Stop共用sender。整体退出解除原owner订阅。

## Risks / Trade-offs

- [漏掉同一步结果或重复弹出] -> 运行同一步多作业/到货、原消息回收与重复serial断言，不能用LastOutcome轮询替代。
- [旧世界提醒] -> 新运行/新绑定基线、HUD池化与旧目标回调，复制独立值并限制列表。
- [高频分配] -> 仅变化时追加/刷新，临期在业务Tick推进，页不每帧重建按钮。

## Migration Plan

不迁协议或存档；依赖本机设置owner，显示结构继续原1101。按 [execution-notes.md](execution-notes.md)验收，各类行为均须真实运行；撤入口及接收器可回退。
