# Design

## Context

动机及用户确认见 [proposal.md](proposal.md)。订单已有稳定Id/CustomerId/Level代次、Waiting/Served/Expired及独立投影；原订单导航只读，不具备共享制作意向。新增共享状态与成员退出路径，因此需要设计。

## Goals / Non-Goals

**Goals:** 通过原Host统一裁决提供小规模共享意向，保留原制作和交付资格。

**Non-Goals:** 不引入工作独占、库存分配、自动派工、持久统计或另一套传输。

## Decisions

```text
Host Fiber / Scene(ProductRoot) + ProcessInnerSender
  +-- Scene(ProductHost)
  |     +-- HostSessionComponent + Ordered MailBox
  |     |     +-- Child HostPeer + Ordered MailBox
  |     +-- Child Match -> Child RestaurantRuntime -> Child Level
  |           +-- Component CooperativeOrderIntents (member -> order)
  |           +-- Child Order / Customer / FrontWork
  +-- Scene(ProductClient)
        +-- ClientSessionComponent + Ordered MailBox -> GridClientComponent
        +-- GridPresentationComponent -> UIComponent/UIGridKitchen
Remote Fiber / Scene(ProductRoot) -> Scene(ProductClient)
  +-- Session + ProductClientBinding -> same projection/HUD ownership
```

组件只存当前成员的一条独立意向值，版本沿原厨房提交版本；选择另单原子替换本人旧意向，同单不去重到单一成员。Level创建后建组件，Peer失联/退出时只清本人，订单结束清对应关系，Level退出整体清理。恢复不复制旧意向；单成员退出不停止共用sender。相对于把意向放UI或HostPeer，Level归属更符合订单寿命，也不把绑定恢复等同意向恢复。

显式设置/撤销从原订单页发出，包含当前世界/关/绑定/命令身份及OrderId/CustomerId，成员取可信Peer。Host本地Client经Root ProcessInnerSender.Call到HostPeer.GetActorId()/ProductHost邮箱；远端经Session和ProductClientBinding到同一Peer入口。Peer转交HostSession authority ActorId的提交handler，沿原命令排队/去重与阶段、暂停限制验证；只在原Host业务提交点写意向，不把Accepted当成功。新enum用于有限动作，Proto新增到生成排序尾部，旧opcode保持，必要远端wire白名单/协议版本随新增合同更新。

提交后原GridBroadcast把独立意向值复制进快照；本地sender.Send到ClientSession ActorId，远端Session到ProductClientBinding，均由ProductClient GridSnapshotHandler验证当前身份后复制安装，再发布原观察事件。请求/答复由原Call等待者及handler finally收尾，UI不保存池化消息、Host实体或候选清单副本。Event仅通知已提交投影，不承担请求答复，不增加SceneType.All消费者。

界面复用原订单行/制作上下文；阅读不设置意向，明确按钮才发送。只显示成员和订单关系，不改需求计数、饮品绑定、加工资格和实际交付人。暂停可读，变更沿原暂停限制拒绝；旧行由原world/level/binding/HUD epoch guard退役。

## Risks / Trade-offs

实际UI验收发现“提交快照已到、RPC答复未到就断线”的原等待者错误上报。原Session值化适配器仅在捕获的Session实例已失效时把RPC异常转为取消，保留原请求/响应finally回收；没有将正常业务错误或超时吞掉，也不撤销已经提交的意向。以真实Session双owner销毁/同Id新实例成功和原单owner取消两项小集补验，另以实际KCP断线恢复界面验证。

- [误以为独占] -> 文案明确“制作意向，可多人”；真实另一成员交付验证不被阻止。
- [同桌换客或退出后残留] -> 以订单/顾客身份清理和拒绝，实际KCP失联/恢复、换关和池化验证。
- [范围扩大] -> 不做聊天系统或可配置分工框架，验证投入见execution-notes。

## Migration Plan

无成功文件字段。按原消息/独立投影/UI接入，旧opcode不改；协议不同仍按原版本拒绝混连。功能回退撤入口和本关协调状态，不删除用户文件；先按tasks实际运行再同步主规格，不自动提交或归档。
