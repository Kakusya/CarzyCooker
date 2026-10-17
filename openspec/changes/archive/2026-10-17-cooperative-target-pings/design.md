# Design

## Context

见 [proposal.md](proposal.md)。目标身份和位置已有原Grid投影，当前没有共享主动标记；本案需要Host统一状态及消息序列，不将本地高亮当作网络传递。

## Goals / Non-Goals

**Goals:** 当前成员每人一个真实目标标记，有限寿命及明确清理。

**Non-Goals:** 不创建订单认领、角色移动权、跨关或持久标记。

## Decisions

```text
Host Fiber / Scene(ProductRoot) + ProcessInnerSender
  +-- Scene(ProductHost)
  |     +-- HostSessionComponent + Ordered MailBox -> Child HostPeer + Ordered MailBox
  |     +-- Child Match -> Child RestaurantRuntime -> Child Level
  |           +-- Component target marker state (member / binding / target / expiry)
  +-- Scene(ProductClient)
        +-- ClientSessionComponent + Ordered MailBox -> GridClientComponent
        +-- GridPresentationComponent -> UIComponent / UIGridKitchen
        +-- GFEntityComponent -> GridKitchenBoard (marker visuals only)
Remote Fiber / Scene(ProductRoot) -> Scene(ProductClient)
  +-- Session + ProductClientBinding -> same client owners
```

标记状态归当前Level，按可信MemberId保存独立值和serial，目标类型为具名enum，ID与到期Tick保持数值。本人新标记替换本人旧值；10业务秒换算Tick，暂停不推进。物品位置每次从原投影解析，允许随原容器/手持移动，消费后移除，不能换成同格另一物。

原ClientSession取得本地HostPeer.GetActorId()或远端Session.Call，公开标记请求经ProductHost Peer handler校验可信绑定，再转authority.GetActorId()/HostSession有序邮箱；在该邮箱内完整复验Level/目标后一次提交。它没有到位或加工过程，不另设待执行队列，也不推进领域Tick；提交后原快照及Executed终态才更新标记。原厨房提交前清理失联、目标消失及到期值，Level退出整体回收；取消同样在原Host提交点完成。

扩展原Proto源、wire显式白名单和每Peer独立投影值，不预占协议版本。Host本地经Root sender及MailBox，不UDP向自己发送；远端仍用既有KCP。handler finally释放各跳消息，await后复查EntityRef/InstanceId及代次；等待按原所属Call/物理观察者收尾，不新建等待表。

UI沿原目标详情/物品查看发明确标记，查看无操作请求。Board沿原实体owner持有限成员标记表现，状态变化创建/更新，退出释放；不覆盖本人原选中高亮，不建私有池。事件只在原ProductClient通知观察，不使用SceneType.All/Invoke作权威写入。单成员退出只清本人标记和绑定，不停止Root共用sender。

## Risks / Trade-offs

- [冒名/旧目标/跨关] -> 本地与真实KCP验证可信成员、代次、消费目标和同格替换；原实物不变。
- [幽灵标记或重复延时] -> 暂停、到期、重放、断线恢复、换关和HUD池化实测。

## Migration Plan

无存档迁移。按本案新增Proto源生成及原协议消费者顺序实施，不手改生成物；最小UTF/真实入口见execution-notes。撤标记入口不删除领域实物，提交/归档另行授权。
