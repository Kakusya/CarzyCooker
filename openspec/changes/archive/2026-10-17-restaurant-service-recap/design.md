# Design

## Context

动机见 [proposal.md](proposal.md)。原 `GridPresentationPanels` 结算显示总数；`GridFrontBroadcast` 为当前Level复制全部Order结果，`GridOrderProjection`已含DishId/State/Score。跨结算导航及关寿命需要明确口径，因此保留设计。

## Goals / Non-Goals

**Goals:** 从同版当前完成关的订单形成只读复盘。

**Non-Goals:** 不写历史统计、不新增网络查询或改变计分，不替代营业总览和结算下一步。

## Decisions

### 当前结果和显示归属

```text
Host Fiber / Scene(ProductRoot) + ProcessInnerSender
  +-- Scene(ProductHost)
  |     +-- HostSessionComponent + Ordered MailBox -> HostPeer
  |     +-- Child Match -> Child RestaurantRuntime -> Child Level
  |           +-- Level children: Order / Customer / FrontWork
  +-- Scene(ProductClient)
        +-- ClientSessionComponent + Ordered MailBox -> GridClientComponent
        +-- GridPresentationComponent (recap selection)
        +-- UIComponent -> UIGridKitchen (1101)
Remote Fiber / Scene(ProductRoot) -> Scene(ProductClient)
  +-- Session + ProductClientBinding -> same client projection / HUD owners
```

沿原建树/退出，不添加复盘领域实体。Host提交后由GridBroadcast按Peer投递：本地Root sender.Send到ClientSession.GetActorId()/ProductClient邮箱，远端经Session和ProductClientBinding到同一GridSnapshotHandler。ApplyGridSnapshot验证并复制独立值后发布原GridProjectionApplied；handler finally释放DTO，本案不保存消息或Host实体。

只在Completed开放复盘。按DishId分组，Served贡献Order.Score，Expired计未满足；若完成态残留Waiting或合计与权威总数不符，明确显示结果尚未完整而不自行补造。复盘页存当前LevelId/generation及菜品选择；分组仅在完成结果变化或打开时构建，平时不逐帧排序。

复用原SetInformation/AddTarget，提供返回结算。查看无请求、答复或新Invoke；Event只在原ProductClient范围通知显示，不新增SceneType.All。换关、HUD重开、恢复新绑定按原EntityRef/epoch失效。单成员退出不停止共用sender；整体退出沿原owner收尾。

## Risks / Trade-offs

- [重复计数/伪造分数] -> 同菜多单、两菜交付/超时与零分自然完成核对；只显示不写回。
- [旧复盘污染新关] -> 实际继续及池化旧回调验证；不保存整份世界。
- [体验] -> 普通540×960及真实KCP确认可读；不声称平衡或真人理解通过。

## Migration Plan

无数据迁移。原界面分部新增只读入口，回退只撤入口。按 [execution-notes.md](execution-notes.md)逐点运行，不扩大到构建/真机，不自动提交或归档。
