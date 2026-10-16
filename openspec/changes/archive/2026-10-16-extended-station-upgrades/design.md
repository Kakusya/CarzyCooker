# Design

## Context

动机见 [proposal.md](proposal.md)。当前源码仍是现有五目标 Luban 配置及 ProductRuntime owner；进行中的枚举/动画/设备案按本轮假设为完成基线，但实施保留其实际未提交成果。

## Goals / Non-Goals

**Goals:** 补齐现有十一款内容的可观察经营体验。

**Non-Goals:** 不扩候选目录，不设中心服务，不引入工具或新业务 owner，不做真机测试。

## Decisions

升级类别继续使用现有批次/手工/烘焙三类契约；以真实配方支持的能力集合校验，兼容蒸汽机的两种批次能力。新增稳定升级及装修行，新内容非终关扩展允许候选；旧关奖励保持。现有单级安装、冻结参数、保存捕获与恢复复用，检查实际消费者无需提高文件/协议版本。

实际 owner 树不变：

```text
ProductRoot Fiber / SceneType.ProductRoot
  Component ProductRuntime / ProcessInnerSender
  Child Scene ProductHost / SceneType.ProductHost
    Component HostSession + Ordered MailBox
      Child HostPeer + Ordered MailBox + Delivery
    Child Match -> Child Participant
      Child RestaurantRuntime
        Component GridKitchen -> Child Station / Item / Process
        Component RestaurantRenovation
        Child Level -> Component LevelBuff; Field ProductionPermit
  Child Scene ProductClient / SceneType.ProductClient
    Component ClientSession + Ordered MailBox
      Component GridClient (独立值投影)
    Component GridPresentation -> 受管 UI / Board
```


客户端阅读不发业务消息。Buff/装修按钮沿 ClientSession.CallProduct -> 本地 ProcessInnerSender.Call(HostPeer.ActorId) 或远端 Session/KCP -> ProductHost Peer handler -> HostSession 的 Ordered MailBox -> 当前 Level 固定 Tick 共同裁决。可信 Peer 注入身份，远端不能取得 Host 控制权限。状态先提交，独立快照通知送到 ProductClient 的 ClientSession MailBox，复制值后回收 DTO，再更新原受管 HUD。请求等待归原 AsyncOwner，响应归调用者释放；不增加等待表。换关释放旧 Level/Buff，保留 Kitchen/Station/Process；退出沿现有 Match/Root 收尾，不改共用 sender 生命周期。

## Risks / Trade-offs

误判提示或冻结参数会使玩家重复备料或得到错误耗时。相关 UTF 断言真实物件、份数、旧新作业及保存往返，界面走普通 Launcher 与真实 KCP。只跑改变行为的小集，可复用未变的原生命周期证据，具体记录见 [execution-notes.md](execution-notes.md)。
