# Design

## Context

动机和已确认权限见 [proposal.md](proposal.md)。GridSupplySystem已有来源/菜单/阶段/配额验证与真实SupplyShipment，来源配额和货物归持续KitchenSupply；本案增加的是协调请求，不能再建立一份可消费库存。跨成员处理和一次采购需明确交接，因此创建设计。

## Goals / Non-Goals

**Goals:** 原采购的明确协作入口，失败可重试且没有额外Host审批。

**Non-Goals:** 不做自动补货、数量建议、钱包、审批或运输调度框架。

## Decisions

```text
Host Fiber / Scene(ProductRoot) + ProcessInnerSender
  +-- Scene(ProductHost)
  |     +-- HostSessionComponent + Ordered MailBox -> Child HostPeer
  |     +-- Child Match -> Child RestaurantRuntime
  |           +-- Component GridKitchenComponent -> Component KitchenSupply
  |           |     +-- Child SupplyShipment (actual persistent-run shipment)
  |           +-- Child Level -> Component CooperativeSupplyRequests
  +-- Scene(ProductClient)
        +-- ClientSessionComponent + Ordered MailBox -> GridClientComponent
        +-- GridPresentationComponent -> UIComponent/UIGridKitchen
Remote Fiber / ProductRoot -> ProductClient
  +-- Session + ProductClientBinding -> same client projection and HUD
```

请求按发起MemberId保留一项、携带唯一RequestId/修订与SourceId，具名状态为待处理/已下单。首版数量固定一包，避免借本案扩大成需求预测或批量订单。替换只换本人提示，不撤旧采购；已下单行可链接原供应总览显示关联Shipment，而不是又存运输进度。

发布/取消/处理都走原可信Client→HostPeer→HostSession：Host本地通过Root ProcessInnerSender.Call到Peer ActorId，远端经Session、ProductClientBinding进入相同Peer入口，再转authority ActorId的ProductHost提交handler。命令带世界/关/绑定和请求身份，沿原去重/排队。处理复验RequestId/状态/原采购权限；在GridSupplySystem实际扣配额并创建真实Shipment的同一提交分支记已下单和关联Id，Accepted不算完成。两成员处理同请求时后一个不再采购；正常独立采购不自动冒充处理了某请求。失败不改请求与配额，原运输/接收/拆份保持。

Proto从当前目录排序尾部新增，既有opcode保持，实际远端白名单和协议版本随新合同更新；若原SupplyIntent需增加协调RequestId，只改协议源并重新生成。有限动作/状态用enum，整数边界验证。请求组件随当前Level创建/退出；Peer断线/退出只撤本人协调提示，整体关店拆Level；已经存在的Shipment仍归KitchenSupply，不停止共用sender或逆向退款。

提交后原GridBroadcast复制独立请求值；本地sender.Send到ClientSession.GetActorId()/ProductClient邮箱，远端Session到ProductClientBinding，均由GridSnapshotHandler验证当前身份、复制后发布GridProjectionApplied。原handler finally释放DTO，Call等待者和UI异步owner收尾请求/答复；不持池化列表/Host实体，不增加SceneType.All事件。HUD读取当前值并显式处理，旧world/level/binding/epoch行退役；暂停/收尾按原采购限制说明，不伪造审批等待或已收货。

## Risks / Trade-offs

- [同时处理重复采购] -> 在原采购提交处分配关联Id，真实Host/KCP竞争及失败重试断言一次配额/一次货物。
- [退出撤销了运输] -> 协调与持续KitchenSupply分开，实际退出/跨关后仍运输并沿原接收。
- [越权] -> 不从请求发起人身份借采购权，每次校验真实处理者原资格。

## Migration Plan

不改成功档中的持续供应格式，不保存协调请求。生成消息后接原提交/投影与HUD，按execution-notes运行最小集；协议混连沿原版本拒绝。回退撤本关提示与关联入口，不删除真实货物或用户文件，不自动提交或归档。
