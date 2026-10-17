# Design

## Context

见 [proposal.md](proposal.md)。当前ProductRoomSystem.CommitRoom支持Start/Close，由Host直接选择内容；Match.RoomVersion及ProductRoomSnapshot已有。本案按已确认规则增加提案和Ready，不能把原Start按钮留作绕过入口。

## Goals / Non-Goals

**Goals:** 以同一房间提案版本收齐当前连接成员准备，Host不能强开。

**Non-Goals:** 不扩公网大厅、Solo门槛或正在运行餐厅的晚加入许可。

## Decisions

```text
Host Fiber / Scene(ProductRoot) + ProcessInnerSender
  +-- Scene(ProductHost)
  |     +-- HostSessionComponent + Ordered MailBox -> Child HostPeer + Ordered MailBox
  |     +-- Child Match
  |           +-- Component room proposal / ready state
  |           +-- Child Participant (identity) / optional RestaurantRuntime
  +-- Scene(ProductClient)
        +-- ClientSessionComponent + Ordered MailBox -> ProductRoomClient
        +-- ProductEntryPresentation -> UIComponent / UIGridKitchen
Remote Fiber / Scene(ProductRoot) -> Scene(ProductClient)
  +-- Session + ProductClientBinding -> same room projection / UI owners
```

提案状态归Match，包含菜单、内容章或已校验基线来源/修订和提案版本；准备集合按当前可信成员/绑定保存，不把个人UI按钮当权威。发布或修改提案、成员加入/离开/失联/恢复绑定均推进相关房间版本并清全部Ready。成员准备及撤销只影响自己的值，同版本重复请求返回已提交事实。Host发布后也须明确准备。

原新开预览的确认改为“发布房间提案”，仍不建餐厅；房间明确展示本人准备/取消、各成员状态及Host开局。Host Start完整检查当前提案版本、全体当前连接成员Ready、原来源基线/权限及原开局条件，一次创建Restaurant。来源记录修订变化须重新发布提案并重准备，不静默更新来源。

本地ClientSession/LocalMessage取得HostPeer.GetActorId()，Root sender.Call经ProductHost Peer handler，再转authority.GetActorId()/HostSession Ordered MailBox统一裁决；远端经Session/wire白名单进入同一Peer入口。Proto源扩展发布/Ready动作及房间投影，原RoomVersion与新提案版本职责清楚，版本按实施时现值演进。广播本地经ClientSession ActorId、远端经原Session；ProductClient安装独立值后原消息finally释放。

Ready不创建餐厅、不保存数据。关闭餐厅回房间清Ready并重建提案有效性；已经运行时沿原加入/恢复，不要求当前关全部Ready。所有直接Start API/启动窗口进入合作房间的路径都须遵守Host门槛，不能只遮按钮；测试夹具从真实按钮明确准备，不在产品代码自动Ready。

兼容入口调查确认：原显式Host启动与CooperativeBaseline载入会立即建餐厅，必须一并收敛。显式Host启动只发布参数所选内容提案；载入已校验基线只发布来源提案并保留原授权/独立载荷，二者仍由每位成员明确准备再Start。提案另含实际第一关ID，来源从其NextLevel开始展示剩余菜单，不能冒充菜单首关。Root本机来源在准备/开局时重读原文件并核对revision/ResultId，已知变化撤提案/Ready；外部授权继续以已签名快照为原能力边界，收到新来源授权时重新发布并清Ready，不声称能查询另一设备未提供的修订。

Ready/取消依据提案版本，忽略仅由别人成员准备造成的RoomVersion变化，以允许同时准备；发布/Start仍核对房间版本。成员/内容变化同时推进相关版本并清所有绑定Ready。投影沿原RoomSnapshot及原RosterMember值字段扩展，不新增消息ID；wire36，既有193个opcode保持。Ready状态变更不推进厨房时钟。原授权记录解析提为两个真实消费者共用的纯值解析，广播仍由原房间系统负责，遵守ET静态类单向依赖。

UI沿原EntityRef/epoch及房间/绑定/提案版本保护。请求取消由原owner结束显示等待，原Call仍按答复/超时收尾；不清字典代替消息收尾。Event仅在现有ProductClient/Root实际消费者通知，Invoke不作业务裁决；成员变化不停止共用sender。

## Risks / Trade-offs

- [旧提案强开/成员变化遗漏] -> 真实本地/KCP两人未Ready拒绝、双人准备、新成员/退出/恢复及来源修订变化断言。
- [破坏原流程] -> 受变房间/基线/启动UTF按新显式步骤修改并运行；Solo和运行中加入/恢复复用未变证据。

## Migration Plan

房间内存新状态，不入成功档；已有协议消费者同时生成/更新。先领域门槛，再UI，再最小真实入口验收；不以旧测试绕过新规则。验收见execution-notes，未授权提交或归档。
