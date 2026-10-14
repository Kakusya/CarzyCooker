# Design

## Context

见 [proposal.md](proposal.md)。现有 StationConfig 表示可信占地/交互面，InitialLayoutConfig 创建工位，准备态 Layout 命令仅搬旋转已有实例。Restaurant 的 Continue 已先预检配置和出生位再退役旧角色；将新工位方案纳入同次预检，不复刻布局引擎。

## Goals / Non-Goals

**Goals:** 成功选择进入持续餐厅，在下一 Preparing 合法安装一次，并保留旧厨房。

**Non-Goals:** 不扩地图、升级现有能力、改变评分、缩小菜单或实现持久文件；首版不开放联机装修。

## Decisions

### 1. Owner 和状态来源

```text
Main Fiber / ProductRoot
  ProductHost (Solo)
    Match
      Participant...
      RestaurantRuntime
        RestaurantRenovation [Component]
        GridKitchenComponent -> GridStation / GridItem / Process...
        Level -> GridActor / Customer / Order / FrontWork / Buff
        Companion
    HostSession + Ordered MailBox
      HostPeer + Ordered MailBox + LocalDelivery
  ProductClient
    ClientSession + GridClient
    GridPresentation / UIComponent / GFEntityComponent
```

RestaurantRenovation 归 Restaurant，持已获得奖励的DefinitionId→StationId应用记录、当前成功的固定候选和不可变待安装选择；实际工位仍是Kitchen Child，应用映射只记录关联，不复制可写地形。候选绑定成功Level/Generation和递增ChoiceVersion，不归LevelBuff，也不把相同工位定义的不同奖励误当重复实例。

整体退出随 Restaurant 收尾，无存档时内存丢弃；成功换关保留已获得奖励，旧候选/请求失效。创建沿现有树，单成员退出不释放餐厅装修或公共 sender。

### 2. 可调样机候选与预检

RenovationConfig源为Id／Name／StationDefinitionId／Quantity／InstallX／InstallY／Orientation，LevelConfig末尾AllowedRenovationIds引用候选，正常五目标生成。普通台面与切配台各数量1，锚点分别`(2,13)`和`(4,12)`、朝向0；数量沿本地x方向排布并完整预检。首轮实际运行证明`(4,13)`会与第一奖励组合堵路，已修源，不放宽空间约束。参数为可调样机，不加能力或价格。

自然 Completed 时，从未获得奖励中过滤当前布局可安装项，全部展示并冻结；空集是正常可继续状态。复用 StationConfig 和 GridStationGeometry 的占地/通路/出生位/引用预检，检查原接近面和角色/顾客共享地形。选择时验证当前合法性，Continue 再整体预检。

新增工位不附送初始食材，不复用某个原 InitialLayout 运行实例 Id；按奖励定义创建新实例并保留应用记录。准备态仍可按已有布局命令移动原/新工位，营业不可移动。

### 3. 成功后安装与检查点交接

选择仅提交待安装计划，当前 Completed 的物品和布局不变。Continue 同步预检原 next config、角色出生位、待安装工位及全部承接引用；失败保持原关/选择/物品，加成不被清掉。成功在单一服务步安装新 Preparing 和新工位，记录奖励已获得，旧角色手持按原规则落物；下一业务步才接续加工。

选择锁定后不换方向；相同最新请求重放返回事实。现有规格只要求允许选择，本轮按已说明默认保留未选时直接继续；无候选和池用尽也可继续。选择队列在同一服务步先于Continue处理，避免已接收选择被继续越过；已选方案再整体预检、最多安装一次。有限奖励不会无限塞满地图。

未来检查点记录原持续厨房、已获得奖励、待安装计划和下一关定义。存档案必须在成功载荷中保留待安装选择，恢复时应用一次；当前内存模式不调用假 SaveSuccess。准备态后续移动的布局只在下次成功保存，不立即写盘。

### 4. 收发、生成与 UI

结算候选按钮→ClientSession所属ChooseRenovationRequest→LocalMessage的Peer ActorId→root sender.Call→ProductHost/Peer Ordered MailBox的RenovationPeerHandler→可信ChooseRenovationSubmitRequest→HostSession ActorId/Ordered MailBox的RenovationSubmitHandler→Restaurant服务步选定／锁定。Context先校验当前关，再核对最新完整ChoiceVersion／DefinitionId重放；内部Submit不作用Peer，Host模式明确拒绝Solo装修。原Continue使用相同安装计划预检。协议15新增10109～10113和Snapshot49～54。

两段回复沿原 Call；每端独立 GridWorldSnapshot 包含候选/选择/已获得与布局同 Version → LocalDelivery sender.Send(ClientSession ActorId) → ProductClient 原投影 handler →复制、Dispose、观察。界面绑定当前 Level/结果版本与 owner，关闭重开不再抽取或自动选择；await 后核对实例。

源表注册 c,s,e，正常 ET 五目标 Check/生成，Proto 只追加。结算面板复用当前受管 UIGridKitchen 模板，确需绑定时改 Prefab/Mono 源并重生成，不创建新的 UI 框架。实现同步装修边界说明，不删除待裁决的菜单许可条目。

## Risks / Trade-offs

- [重复继续生成多个设备] → 奖励应用身份与已有最新命令重放共同核对。
- [新增占地损坏旧加工或路径] → 全局预检后同次安装，拒绝保持原现场。
- [把装修和 buff 混同] → 不同 owner 和字段，换关分别保留/重置。

## Migration Plan

关卡/UI/布局前置具备完整运行证据后，按源配置、选择/预检/安装、投影/UI顺序实施。UTF 覆盖空集、重复、冲突拒绝及物品/加工保留；一次 Launcher 零星选择→下一关→搬新工位验收即可。持久化和联机装修由对应后续范围接入。
