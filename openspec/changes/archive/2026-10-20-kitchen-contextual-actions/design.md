# Design

## Context

范围见 [proposal.md](proposal.md)。当前 `GridPresentationActionNavigation` 已有全部/当前目标/厨房/前厅等分类，`GridClientSystem.CounterIntent` 已依据实物生成原动作；因此本案集中改动作呈现与解释。结构沿 [HUD 案](../2026-10-20-product-hud-readable-detail-layout/design.md)，不再改 UI 注册或 Prefab 分区。

## Goals / Non-Goals

**Goals:** 让动作表达具体对象和方向，减少寻找按钮；原合法动作仍完整可达。

**Non-Goals:** 不自动执行连续步骤，不增设双向装盘、自动交换、远程取物、动作队列或客户端权威。

## Decisions

### 在原合法动作构建结果上排序

复用现有按钮/意图构造、目标选择和拒绝说明，将显示候选表示为临时只读值。取当前投影和权限后生成一个主要动作、最多两个次要动作，其余进“更多动作”。选择/展开本身不生成 CommandId，不发消息。数量上限仅限摘要区，“更多”仍能访问全部原合法动作。

优先顺序必须稳定：明确选定有效订单且手持匹配成品时展示原交付；明确选定工位时优先现有 `CounterIntent` 的直接动作；具有明确对象的分装/加工列次要动作；管理/帮助留原分类。只要动作前提改变就重算；只有时间/进度变更更新文本，不每帧重建全部候选。

| 当前事实 | 主呈现示例 | 边界 |
|---|---|---|
| 空手、台面有可取物 | 拿起「吐司」 | 保持真实实例，不拿容器中的假独立物 |
| 手持物、目标为空 | 放下「盘子」到「台面」 | 原格/距离/占用仍由权威检查 |
| 工位满足加工条件 | 开始「烘烤」 | 接受、等待、实际开始分开 |
| 手持空碗、锅有完成汤 | 盛一份「汤」到「碗」 | 使用原精确份数意图 |
| 手持成品、有效匹配顾客/订单 | 交付「汤」给「该顾客」 | 不把只读选单变成认领 |
| 手持盘、烤箱有吐司 | 显示当前操作前提/合法装盘步骤 | 不把原 AddToContainer 改为反向搬运 |

### 拒绝说明贴近目标

能从当前投影明确判断的手满、锁定、容器不符等显示短原因与“查看条件/位置”；不提供伪可执行主按钮。未知或需要最终到位检查的条件标明待裁决。UI 预判只决定呈现，不复制全套权威校验。原有效命令的最终拒绝使用现有错误映射，保留原物品与分数；再次执行必须由玩家明确点击。

目标选择和推荐之间保留真实实例、预期工序/布局版本、binding 和 Level；点击前重新核对。如果该实例消失则撤销旧候选，不能把旧按钮悄悄绑定到同类新物件。地图原单击的执行语义保持；更多动作和详情消费指针，拖动不执行。

### owner 与完整消息路径

```text
ProductRoot Fiber / SceneType.ProductRoot
  Component ProcessInnerSender / ProductRuntimeComponent
  Child ProductHost / SceneType.ProductHost
    HostSessionComponent + Ordered MailBox
      Child HostPeer + Ordered MailBox -> LocalDelivery 或 RemoteDelivery
    Child Match -> Child RestaurantRuntime
      Component GridKitchenComponent（权威）
      Child Level（角色/订单）
  Child ProductClient / SceneType.ProductClient
    ClientSessionComponent + Ordered MailBox -> Component GridClientComponent
    LocalMessageComponent（本地）或 NetComponent -> Session + ProductClientBinding（远端）
    GridPresentationComponent（目标、分类和临时候选）
    UIComponent -> UIGridKitchen；GFEntityComponent -> GridKitchenBoard
```

本地点击→GridClient 原 CreateGridCommand→`CallProductValueAsync`→LocalMessage 中保存的 HostPeer ActorId→Root ProcessInnerSender.Call→HostPeer Ordered MailBox→ProductHost `GridPeerHandler`。该handler复制独立 `GridSubmitRequest` 并Call `peer.Authority.GetActorId()`，由HostSession Ordered MailBox的 `GridSubmitHandler` 校验身份后调用原 `SubmitGridIntent`。远端点击→Session.Call→Host 的 Session/peer入口→同一GridPeer/Submit裁决。内部response由GridPeerHandler读取并Dispose，外部response回原等待者；AsyncContext复制结果并结束原peer entry。Accepted只显示接收/等待，最终outcome和快照才更新成功。

提交后的 ProductDelivery 通过本地 ClientSession ActorId 或 RemoteDelivery.Session 回送；ProductClient 原 GridSnapshot/outcome handler 安装独立值。请求/响应按现有 AsyncContext/handler 收尾，读取后不保存原 DTO；本地广播 DTO由接收 handler 释放，远端广播在序列化后释放。Event 只观察提交结果，不承担请求或写状态；不新增 SceneType/Invoke/传输。

投影确认后创建候选，HUD关闭/换关/失联时先失效，再释放显示与原等待。await 后核对 EntityRef、HUD epoch、world/binding/level；旧结果不覆盖新目标。成员退出不停止共用 sender。

## Risks / Trade-offs

- [推荐顺序让某动作“消失”] → 更多动作比对原合法集合，覆盖器具、加工、分装和服务。
- [客户端预测过强] → 仅做已知事实提示；保留最终裁决、等待和拒绝，不另造通用规则引擎。
- [联机竞争] → 真实 KCP 另一成员取走目标，旧候选不得提交针对替代物的请求。

## Migration Plan

先抽出原显示候选而不改领域行为，再接主/次要区域和短原因；按行为小集验收。无需迁移数据或协议，撤回呈现排序即可恢复原分类。与首局案只共享当前目标和原动作 API。见 [execution-notes.md](execution-notes.md)。
