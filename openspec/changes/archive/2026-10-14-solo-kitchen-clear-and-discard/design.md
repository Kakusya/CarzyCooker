# Design

## Context

见 [proposal.md](proposal.md)。立案时源码只有 Wash 和洗碗台／脏净池，没有接水／排液／垃圾桶能力；候选取水台不是实现。用户撤回随身清空设计后，本案已在原 Wash 增能力并配置 Trash，按下述合同实施及验收，见 [execution-notes.md](execution-notes.md)。

GridItem Kind 继续区分材料和容器，本案源表另声明 Form／Discardable；汤完成批次保留原输入到份数消费完。排液按完成产物语义分类，不看到锅里留着番茄块就误送垃圾桶。

## Goals / Non-Goals

**Goals:** 玩家真实到水池接水/排液，到垃圾桶处理固体；复用同一物品、工位和业务驱动。

**Non-Goals:** 不实现随身删除、垃圾库存/容量经济、自动加工取消或网络新入口；不在本轮改动现有汤默认水规则。首版不引入固液自动分离，未能统一分类的原始混合内容拒绝整体倾倒，后续再定义混合处理。

## Decisions

### 1. 保持 owner，增设施能力

```text
Main Fiber / ProductRoot
  ProductHost (Solo)
    Match
      Participant...
      RestaurantRuntime
        GridKitchenComponent
          GridStation (Sink/Wash, Trash, existing stations)
          GridItem (material form / container Contents)
          ManualProcess / AutoProcess / BakeProcess
        Level -> GridActor / Customer / Order / FrontWork
        Companion
    HostSession + Ordered MailBox -> HostPeer + Ordered MailBox + LocalDelivery
  ProductClient
    ClientSession + GridClient
    GridPresentation / UIComponent / GFEntityComponent
```

材料形态、能力和供水引用落源表/Model，操作与统一步提交落 Hotfix，显示落 ModelView/HotfixView。水池优先复用当前 Wash 工位，追加 FillWater/DrainLiquid 能力，保持已有洗碗 Kind/能力/认领；垃圾桶作为新 Station Kind/能力进入同一布局和共享地形。工位不拥有另一份物品系统。

配置/定义完整后才创建设施和厨房。单关换关保留设施和实物，只重置 Level；单成员退出不删除设施或停公共 sender。整体先停输入/驱动和视图等待，再原 sender.Stop 与所属树释放。

### 2. 形态、内容和目的设施

GridItemConfig 源新增材料 Form 与允许丢弃字段：水/蛋液/汤为 Liquid，番茄/番茄块/鸡蛋/面包/烤面包为 Solid，锅碗盘为 Container。具体 ID 从实际源分配；Kind 继续表达材料/容器，不能拿 Kind 猜固液。

操作统一以玩家手持物为源、实际设施为目标。水池倒液只消费液体内容，垃圾桶处理固体内容或允许的手持固体；容器本身保留。完成汤锅按 AutoProcess.OutputDefinitionId 的 Liquid 形态清理剩余份数及锁定原输入/批次；尚在 Cooking 的输入仍拒绝。普通未加工内容只有同一形态时可整体处理，原始固液混合首版明确拒绝，不偷偷只删其中一部分。

暂定水池配置显式无限供水，只给合法干净空容器一份实际 Water 内容；当前样机碗声明兼容 Water，锅只按当前配方兼容规则处理。接水不消费或改变有限食品供应配额，不因材料有水就改变番茄蛋花汤已确认的默认供水。是否将现有汤改为显式加水正在询问用户，答复前沿用原制作合同。

水池与 Wash 共址时复用已有效洗碗占用检查，不能在已有占用中绕开工位资格。排液不是洗碗，保留原脏净；供水不能先清掉旧内容再填水。垃圾桶不设置垃圾实体或满桶玩法，只有合法交互后才消费指定实物。

### 3. 自动接近与一次完整提交

界面构造实际 Sink/Trash StationId、源 ItemId、内容/批次预期身份和明确动作。远处点击沿原四邻接近；到达后再核对工位能力/位置、源手槽、内容/批次、容量、脏净和锁定。相邻且合法才一次提交；普通台面和地面不接受清理动作。

删除集合在完整预检后确定。处理暂停备料同步撤下相关作业、Items/Processes/容器/角色引用；完成汤处理剩余批次及其原输入，保留外层锅。接水一次创建 Water 并建立唯一 Contents 关系，拒绝不创建半份水。意图携带必要内容/批次预期值，接近中变更就拒绝，不重新推断另一物件或动作。

### 4. 完整收发与投影

场景设施点击/HUD设施动作 → GridPresentation → ClientSession 所属值请求 → LocalMessage 的 Peer.GetActorId() → root ProcessInnerSender.Call → Peer Ordered MailBox/SceneType.ProductHost 的 GridPeerHandler →可信 GridSubmitRequest → authority.GetActorId()/HostSession Ordered MailBox/GridSubmitHandler →快速回执 → Restaurant 唯一步到位重验/提交。

回复经原两段 Call/Reply 等待表。最终结果及设施能力/物品形态/水内容合入同 Version 的独立 GridWorldSnapshot，每 Peer 单独 DTO →LocalDelivery sender.Send(ClientSession ActorId) → ProductClient MailBox/原投影 handler →复制普通值、Dispose DTO、观察事件。界面 await 后核对原 EntityRef/World/Level/绑定，关闭只结束显示等待，物理观察者仍收尾响应和在途责任。

新增动作和必要预期值正常追加 Proto 源，不重排旧编号。首版保持 wire 的 SoloOnly 边界，联机案以后按同一目的设施规则接入，不开放任何绕过设施的清空动作。

### 5. UI 与生成

删除独立随身“清空/丢弃”入口，展示地图水池和垃圾桶目标，HUD 入口绑定这些实际设施。明确接水、倒液和投垃圾；远处操作显示接近中，到位结果才成功。场景和 HUD 使用同一身份固定意图，拖动/点击不穿透。

修改 GridItemConfig、StationConfig、InitialLayoutConfig 源及全部相关读取者，沿 active ET 五目标 Check/生成；绑定确需时安全改原 Prefab/Mono CodeBind 源并重生成。没有当前能力时按新设施配置真实接入，不把现有 Wash 命名就当接水已实现。

## Risks / Trade-offs

- [绕设施删物] → 目的工位和到位条件是 authority 提交前的必要校验，UTF 从普通台面/地面请求验证零写入。
- [把完成汤的留存输入当固体] → 完成批次按成品定义形态；原始混合内容不擅自分离。
- [共址破坏已有洗碗] → 保留旧 Wash 行为并补受影响的真实占用/伙伴洗碗小集。
- [接水改坏已验收菜品] → 保持汤默认水，Water 接水/倒水独立验收；改汤须按用户答复修订菜品范围。

## Migration Plan

源定义/布局和生成 →设施动作与完整到位校验 →请求/投影/受管目标UI。使用真实本地请求 UTF 验证水池接/倒水、固体垃圾桶、错工位/未到位/锁定拒绝，再普通 Launcher 搬容器走设施完整链。用户已授权完成五案；前置九案已归档并同步主规格。本案同时明确修订熟汤排液的批次合同，避免旧规格的禁止清空条文与设施约束冲突。
