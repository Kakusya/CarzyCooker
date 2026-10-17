# Design

## Context

见 [proposal.md](proposal.md)。RestaurantChapters.ValidateAll当前要求章数恰为2、每种模式3关；ProductMenuCatalog.Includes集中决定资源批次，不能只增加章节表。用户已确认一个新主题三关，两个模式均可玩。

## Goals / Non-Goals

**Goals:** 新章可从普通入口开局、逐关经营、保存/领取、终关及独立复玩。

**Non-Goals:** 不重排原Level/Next或往旧档追加新菜物资，不扩大成正式平衡项目。

## Decisions

依赖蒸食和茶饮两案。主题名“蒸茶小馆”，三关菜单依次为蒸饺+热红茶、加入冰鲜奶茶、加入珍珠奶茶；每关都保留真实叶供应和器具闭包。章身份/Solo关链/合作关链分开，按当前源表未占ID配置；不把模式/菜单变体当额外章节。

通过Luban源新增RestaurantChapter/Level/CustomerFlow及必要生成枚举；原Order/Supply/InitialLayout已具备两批完整内容，通过显式菜单组合复用源行，避免重复定义物资。ValidateAll改为可信章节列表的唯一身份/引用与三关契约校验，不再写死2；原章数据保留原值。新菜单资源集合显式接入ProductMenuCatalog、创建布局/供应/贴单/保存验证消费者，不用范围数值判断新动作或菜单。

实际章身份3，Solo链13（1801→1802→1803）、合作链23（2801→2802→2803）；SteamTea=9只组合Basic、Steamed、Tea，不包含Cafe。初始解锁四菜及其必要设备，当前关菜单按12/13→12/13/14→12/13/14/15收敛生产许可；不向旧档增加解锁。35个初始布局与原Cafe相同占地、复用原扩建8，第二次已安装则无候选自动收口，终关无奖励。营业600/750/900Tick，来客间隔120/100/80Tick、耐心600Tick为初版参数；文件仍12，新增菜单通过wire42拒绝旧端，不加协议字段。

```text
Host / Solo Fiber / Scene(ProductRoot) + ProcessInnerSender
  +-- Scene(ProductHost) -> HostSession + Ordered MailBox -> HostPeer
  |     +-- Child Match -> Child RestaurantRuntime
  |           +-- Component GridKitchen (persistent items / processes / supply)
  |           +-- Child Level (current themed stage)
  +-- Scene(ProductClient) -> ClientSession / independent projection
        +-- GridPresentation / UIComponent / GFEntityComponent
Remote Fiber -> Scene(ProductClient) -> Session + binding -> same client owners
```

不新增业务树或消息类型：原Host开局/Continue、真实制作/交付沿原Peer→HostSession统一裁决；本地Root sender投递ActorId，远端Session到同一handler。提交快照安装独立值后原finally释放DTO，Event只观察。成功结果沿原SoloRepository或合作结果/领取归属，Host不自动是Save Owner。

第一关提供可理解单线，后两关客流与并行准备递增；具体时长/产量/阈值采用可调整首包配置，实际三关运行反馈修明显不可执行配置，不追求统计学平衡。不开调试免费物资通过关卡，不以零星自然结束代替本批新菜实际制作交付验收。

旧文件恢复只按原Next/菜单/物品载荷，不因新资源批次补物；本案不为纯新增章字段升文件版本。旧有效Solo及合作领取文件各真实往返一次，复用其余未变兼容证据。终关无装修/第四关，Solo完成保存回选档、Host确认回原房间；新开同章建立独立运行。

## Risks / Trade-offs

- [菜单闭包缺漏/旧档被补物] -> Check/生成和全部消费者核对，加实际旧新文件及两模式运行。
- [三关只是配置存在] -> 逐关真实营业/交付/前厅收口，末关保存/领取及独立复玩，每功能记录结果。

## Migration Plan

先内容两案交付再导新章源表，依次Check/生成/编译/相关UTF/普通入口；完成后同步主规格Purpose与已确认三章实际范围，历史记录不重写。见execution-notes。
