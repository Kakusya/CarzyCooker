# Design

## Context

见[proposal.md](proposal.md)、[工位合同](specs/kitchen-workstations/spec.md)。提案前初始化固定工位位置；当前已由StationConfig／InitialLayout真实配置替代，6/6完成。Blocked为三种角色共用寻路输入，GridStation／GridItem／Process保持稳定身份；Preparing调整在持续厨房唯一提交，实际运行见execution-notes。

## Goals / Non-Goals

**Goals:** 一份工位定义、一份运行布局、一份权威地形，合法调整保留物品和加工。

**Non-Goals:** 不建布局编辑器、复制厨房、装修奖励或区域扩建。烤箱仅提供能力与槽，烘烤逻辑由菜品 change 实施。

## Decisions

### 1. Owner、程序集和创建顺序

```text
Main Fiber / SceneType.ProductRoot
  Component ProcessInnerSender / ProductRuntime / Timer
  Child SceneType.ProductHost
    Child Match -> Child RestaurantRuntime
      Component GridKitchenComponent (LayoutVersion / shared terrain)
        Child GridStation (DefId / Cell / Orientation / item relation)
        Child GridItem / GridManualProcess / GridAutoProcess / GridBakeProcess
      Child Level -> Child GridActor / CompanionRole / Customer
    Component HostSession + Ordered MailBox -> Child HostPeer + Ordered MailBox
  Child SceneType.ProductClient
    Component ClientSession + Ordered MailBox -> Component GridClient (copied layout)
    Component GridPresentation / UIComponent / GFEntityComponent -> managed HUD / Board
```

源配置加载和布局闭包检查后创建厨房、一次创建工位及物品，再创建 Level 角色、绑定和视图。Model 持 Station／布局状态，Hotfix 预检提交，View 只画值。LayoutVersion 属厨房，不因换关清零；单关退出保留工位与作业，整体退出先停入口驱动和视图再释放原树。UI 候选位置只为短寿命显示值，不成为可写世界。

### 2. 工位定义和同一通路

StationConfig 包含 Kind、FootprintOffsets、ApproachOffsets、ItemSlotCapacity 与明确加工能力；InitialLayoutConfig 保存定义、原点和朝向。首期台面／切配台／灶台／烤箱，顾客接入时增加餐桌、洗碗及有限池能力。普通外槽一格一物；池明确声明有限 ItemId 队列容量，不用“单槽”伪装无限库存。

Blocked 由提交布局派生，玩家、伙伴和顾客读取同一地图与路径函数。旋转转换占地及操作位；生成布局不得越界、重叠、覆盖角色所在格或堵死必要入口、出生位与已存在设备操作位。前厅目标接入后加入同一预检集合。本轮 NPC／顾客不占玩家既有“两人停驻”名额，执行排他由工作认领保证；精细角色碰撞另案，不复制一套地形。

### 3. 准备态一次提交

移动／旋转请求固定 StationId、ExpectedLayoutVersion、新 Cell 与朝向。在未暂停 Preparing 完整预检目标占地、物品承接、作业关联和必要通路；失败不改任何位置或版本。成功只修改原 Station 的位置／朝向，安装派生地形、加版本，并使经过变化区域的未开始路线取消或重新计算；不瞬移角色。

原 ItemId、容器内容和 ProcessId 均保留，以 StationId 关联的作业不因设备移动重置。已开始作业仍走当前 Tick 与进度；移动后不再位于操作位的人工执行者释放认领保留进度。移动不能把现有地面物或角色压入占地。营业、收尾、Completed 和暂停拒绝业务布局请求。

### 4. 消息两端与所有权

原 HUD 布局按钮 → ClientSession 所属值请求 StationLayoutRequest → LocalMessage 捕获 Peer.GetActorId → root sender.Call → Peer Ordered MailBox／ProductHost 拟 LayoutPeerHandler → 捕获可信 Peer 的 LayoutSubmitRequest → authority ActorId → HostSession MailBox／拟 LayoutSubmitHandler。收到只记待提交意图，Restaurant 服务步在合法阶段完整检查并一次安装。答复沿原两段等待表，最终结果和布局版本随快照返回。

每 Peer 独立 DTO → LocalDelivery sender.Send(ClientSession ActorId) → ProductClient 原快照 handler 扩展布局字段 → 复制值并回收 DTO → 观察事件。确认显示从已提交投影产生，预览不是成功结果。所有 handler 输入、内部响应和关闭等待按 [本地消息设计](../2026-10-13-solo-local-message-contracts/design.md)，迟到布局核对世界／关／绑定及布局版本，不新增 SceneType 或自定义等待表。

### 5. 分段交接

源表落 Design/Excel/ET/Datas/Product，注册 __tables__，领域组 c,s,e，Check 成功再运行当前生成链。先定义与初始布局替换硬编码、再输出 Oven 能力供菜品接入，最后做布局命令与原 HUD 基础操作。UI 完整布局由 UI change 负责；此 change 的通路验证用少量代表布局，不建立通用结构 runner。

## Risks / Trade-offs

- [移动时丢物或作业被重建] → 稳定关系只改空间值，UTF 同时检查 ItemId／ProcessId／内容／进度。
- [通路预检成本扩大] → 同一小地图只查必要目标可达；不穷举所有布局。
- [多个 change 改同一表或协议] → 明确基础定义先交接，串行源改动，协议末尾追加并按实际版本生成。

## Migration Plan

保留现有初始样机行为，先将实际布局写为源配置，再迁移创建者、寻路与 Board 读取者。只有本 change 明确新增能力进入样机，不导入全部候选设备。验收见 [执行说明](execution-notes.md)。

### 当前基础接入

`StationConfig` 的占地／操作格以 x,y 成对 int 列表声明；0～3 朝向逆时针旋转。配置校验及几何转换只读源定义，运行 `GridStation` 缓存 DefId／朝向／容量／能力，物品与作业仍归原厨房。`InitialLayoutConfig` 替换七个硬编码工位并加入空烤箱；该样机烤箱占两格，用于实际核对占地与操作格，烘烤规则交给菜品 change。脏池／干净池暂只有有限容量定义，不创建假前厅工作或补餐具。

当前协议 7 扩展原 `GridStationState` 的定义、朝向、容量、能力、占地及操作格，并在同版 `GridWorldSnapshot` 增 LayoutVersion；所有旧 Opcode 保持。每 Peer 仍独立 DTO，Client 复制只读格值后回收消息，不让 UI 另算权威地形。场景点击设备第二个占地格也映射到原 StationId；Board 显示当前两格矩形占地。未来加入非矩形设备时需同步表现，当前源定义均为单格或两格矩形。

`ZZStationLayout.proto` 排在原协议源之后，追加本地 Request／Response／可信 Submit 四类（10078～10081）。实际路径是原 HUD／Board → ClientSession 所属值等待 → LocalMessage 捕获 HostPeer ActorId → Root ProcessInnerSender.Call → Peer Ordered MailBox／LayoutPeerHandler → 可信 Submit → HostSession ActorId／Ordered MailBox／LayoutSubmitHandler → Kitchen.LayoutCommands。Restaurant 服务步先提交关卡控制，再核对布局；暂停时仍能提交明确拒绝结果。快速回执走原两段 Call，最终事实与 Kitchen／Level 共用 GridWorldSnapshot；没有单独布局权威或结果等待表。

合法布局在一次完整预检后更新原 Station 空间值、原容器及其内容格值、同一 Blocked 和 LayoutVersion；取消未开始路线，保留当前一步，释放不再到位的人工认领。业务时间照常推进，自动作业不因搬迁停钟或重设耗时。退出清理原队列及所属等待，不影响共用发送器。前厅尚未接入，保留明确 CheckFrontLayout 接入点供真实伙伴／顾客位置与目标校验；未运行的真实前厅路径保持 NotRun。

## Sources

- [当前初始化与寻路](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridKitchenSystem.cs)、[模型](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)。
- [流程建树](../2026-10-13-solo-restaurant-level-flow/design.md)、[当前产品设计](../../../../Docs/Product/design.md)。
