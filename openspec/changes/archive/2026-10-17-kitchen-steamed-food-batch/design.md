# Design

## Context

见 [proposal.md](proposal.md)。现有GridManualRecipe支持精确组装，GridBakeSystem复用在煎/烤/研磨，GridDishConfig校验阶段闭包。新增蒸制沿这些owner增加必要具名能力，不建专属库存或另一套计时。

## Goals / Non-Goals

**Goals:** 实际混馅→包制→蒸熟→装盘及两端分工、成功文件守恒。

**Non-Goals:** 不做精细包饺互动、多层器具树、全部蒸食目录或新测试框架。

## Decisions

按Luban源新增洋葱/洋葱块、牛肉末、饺子皮、混馅、未熟饺/熟饺和蒸食盘的实际定义/必要有限供应；复用相同已存在原料定义时不重复身份。切配洋葱后，将牛肉末和洋葱块在合法备料载具混馅，再加饺子皮一次包成一份未熟饺。半成品是唯一物品，可沿原拿放/取出交接；不以显示名或订单ID识别成品。

蒸锅使用可信具名Steam能力，未熟物放入兼容设备并显式启动；复用现有自动换输入型process冻结耗时/锁定，完成一次形成新熟料，合法取出后放蒸食盘。若需要载具，沿现有限定载具关系表达，不添加无关复杂篮层。Luban源增加必要阶段/能力枚举及Grid客户端动作显式集合消费者，保留原整数消息边界，生成物只由原生成器输出。

```text
Product Fiber / Scene(ProductRoot) + ProcessInnerSender
  +-- Scene(ProductHost) -> HostSession + Ordered MailBox -> HostPeer
  |     +-- Child Match -> Child RestaurantRuntime
  |           +-- Component GridKitchen -> Child Station / Item / process
  |           +-- Child Level -> GridActor / Customer / Order / FrontWork
  +-- Scene(ProductClient) -> ClientSession + Ordered MailBox -> GridClient
        +-- GridPresentation -> UIComponent / GFEntityComponent
Remote Fiber -> Scene(ProductClient) -> Session + binding -> same client owners
```

手工/蒸制/拿放沿原ClientSession→可信HostPeer→authority.GetActorId()/HostSession队列，统一Tick复验后提交；本地Root sender/MailBox，远端原Session/wire，受理与最终执行分开。原自动阶段后继仍经既有本地消息提交，不由事件直接扣料。每跳handler/所属Call释放消息，跨await核对EntityRef与代次，状态提交后快照独立复制再释放DTO。Event只观察，不增SceneType.All订阅；退出沿原owner，单成员不Stop共用sender。

有限资源变体接入ProductMenuCatalog、初始布局/供应、菜谱/动作和保存校验。半组装/未熟/未完蒸制/已熟未装盘均由原持续厨房表达；能沿现有保存字段就不升版，需要新增字段时改源与只读旧版适配，不抢占其他案“下一版本”。旧档不补物。

实际配置采用Steamed=7，只包含Basic与本批资源；Cafe组合显式保持原Drink/Salad/Growth/Hot/Dessert/Cafe，不再自动包含未来批次。独立菜单复用备料位置，初始布局、前厅器具池与扩建校验按每个实际菜单分别检查，不能把互不共存的布局合成一张地图。Solo/合作各三关测试路线1601–1603/2601–2603，尚不增加主题章。

拌馅盆为可重复使用、不可嵌套的实际器具；混馅与包制均使用原Assembly。包制完成把盆放回台面，经原TakeContainerContent取出唯一固体，再放蒸锅显式StartSteam=33。阶段闭包允许Assembly产出的固体整份继续参与组装或蒸制，液体混杯不获得该整份资格。Steam能力/阶段来自Luban；蒸制继续使用原GridBakeProcess与Bake类耗时修饰，不增加计时owner。新增动作契约提升协议至40，未新增消息类型或文件字段，成功档保持12。

新增原料、半成品、成品、器具/蒸锅四方向沿现有Art/CarzyCooker/export_art.py模型源增加，更新既有资源集合，不手改生成布局或破坏HandmadeBistro动画；画面能区分未熟/已熟/载具。UIEntity/GF owner不改变。

## Risks / Trade-offs

- [跳阶段或重复消费] -> 实际缺料/错料、未熟交付、双人争用和重复启动保物断言。
- [保存/加工关联丢失] -> 未完蒸制与半成品的Solo及合作领取实际文件恢复、旧文件不补物。
- [验证成本] -> 仅一款两模式真实制作交付/回收，加相关最小UTF，已交付未改菜不全量重跑。

## Migration Plan

依原Excel/Luban及Proto需要的源生成入口，Check通过后才gen，编译后实际运行。完成每条功能再更新tasks/主规格/玩家说明；不自动提交、归档或设备构建。详见execution-notes。
