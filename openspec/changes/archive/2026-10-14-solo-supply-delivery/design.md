# Design

## Context

见 [proposal.md](proposal.md)。AddConfiguredStation 只生成初始实物，GridDishConfig 的 Raw 来源只读取 InitialLayout，现无供应 owner。GridItemConfig 的 Kind=2 容器已经表达真实 Contents，原料为 Kind=1；复用它表达包装，避免另建可消费库存。

## Goals / Non-Goals

**Goals:** 在同一业务钟上完成有限预留、运输、收货与拆份，并让真实制作可持续。

**Non-Goals:** 不做价格、无限来源、取消采购、采购退款、成品供应、网络和新仓储架构。

## Decisions

### 1. 持续供应 owner

```text
Main Fiber / ProductRoot
  ProductHost (Solo)
    Match
      Participant...
      RestaurantRuntime
        GridKitchenComponent
          GridItem / GridStation / Process...
          KitchenSupply [Component]
            SupplyShipment... [Child]
        Level
          GridActor / Customer / Order / FrontWork
        Companion
    HostSession + Ordered MailBox
      HostPeer + Ordered MailBox + LocalDelivery
  ProductClient
    ClientSession + GridClient
    GridPresentation / UIComponent / GFEntityComponent
```

KitchenSupply 归持续厨房，SupplyShipment 归该 Component；已收货包装/材料仍是 Kitchen 的 GridItem Child。Shipment 引用包装身份但不拥有该物品。外部有限余额只在采购预留时减少，实际消费从 Contents 转移，展示数量由内容推导。拒绝“两份都能取料”的数字库存。

初始化先验证源/菜单/收货位，再创建厨房和一次供应余额。统一 Restaurant 业务步在现有进度阶段推进供应；暂停/Completed 不推进，Draining 停新申请但不停止既有货物，下一 Preparing 接续。换关不创建新余额。整体退出停入口/时钟后释放所属等待和树，单成员只结束个人操作。

### 2. 配置和收货位置

拟增 SupplyConfig.xlsx（原料、包装定义、包内份数、外部包数、运输 Tick、ReceivingLayoutId），__tables__ 注册 c,s,e；新包装定义用 Kind=2，仅接受对应普通原料。配置必须正数且引用有效，不把候选数值变成固定产品平衡。

首版将既有具名“交接台”（InitialLayoutId=4）配置为收货点，沿 Counter／单外层槽和 LayoutId 稳定关联，不增加占地或另造地图。移动收货台照常整体预检，Shipment 按工位身份解析当前地点，不把生成时坐标当永久收货点。初始物资仍一次创建，空手取包/搬包用原拿放。

菜单闭包的 Raw 来源扩展为可信初始实物加合法 Supply 定义的有限可生产来源；不会在校验阶段生成材料。包量与配方投入量分开，用实际内容表达重复材料。

### 3. 采购、接收与拆份

供应命令采用类型化 Purchase/Receive/TakePortion，使用 ProductCommandContext 的统一业务编号。预留包数提交后 Shipment 冻结本次包量和需要 Tick；到货变 Ready，收货完整预检单槽后一次生成包装及普通原料 Id，标 Received 并保留有限结果引用，重复收货返回既有事实或明确已接收，不再生货。

TakePortion 固定 PackageId/ContentItemId，仅合法邻格空手操作台面包装。将同一普通原料从 Container 转 Hand，不分配第二份原料。嵌套容器不纳入本案。空包装按定义作为可丢弃固体外壳，必须持包走到垃圾桶四邻后处理；固体包内容也只能向垃圾桶倾倒，保留外壳。含料包不得随身整体丢弃，清理不返配额。

### 4. 完整消息路径与投影

ClientSession 所属 SupplyCommandRequest → LocalMessage 捕获 Peer ActorId → root sender.Call → Peer Ordered MailBox/SupplyPeerHandler（ProductHost）→ 注入可信 Peer 实例的 SupplySubmitRequest → HostSession ActorId/Ordered MailBox/SupplySubmitHandler → 校验入队、快速回执 → Restaurant 唯一步再验和提交。SupplyAction区分Purchase/Receive/TakePortion；采购提交后形成独立货物，接收和拆份转换为原Grid的内部到位意图，不走第二套路线。协议14，独立Snapshot的47/48携带来源配额和货物值；wire仍不开放。

答复经原两段等待表返回；Supply/Shipment/包内容及最终事实合入原 GridWorldSnapshot，同 Version 每 Peer 独立 DTO，LocalDelivery sender.Send(ClientSession ActorId) → ProductClient 的原投影 handler → 校验身份、复制、Dispose DTO、观察事件。UI 不保留可写供应状态。暂停提交取消未执行的新供应动作；已经预留的货物不退款，进度仅冻结。

### 5. UI/生成与交接

原 HUD 增供应模板行及显式申请、接收和拆份按钮；原 UIComponent owner 管理，关闭清绑定，旧关按钮不发新关操作。改 Prefab/CodeBind 源仅在确需字段时进行。新增源表和包装定义、协议末尾消息沿现有工具正常生成，不开放 wire。

实施按清空 → 供应顺序交接同一 GridKitchen/源表/协议；联机案随后适配完整外部请求。后续存档保存外部余额、货物状态和已用业务 Tick、包装内容，不恢复旧请求队列。

## Risks / Trade-offs

- [满槽吞货或重复生成] → 接收前完整预检，Shipment 状态与生成包装同一步提交。
- [跨关补满供应] → owner 留在 Kitchen，只由首次创建安装余额。
- [新收货位置破坏动线] → 复用现有布局预检和真实 Launcher 制作/搬运。

## Migration Plan

先源定义/菜单校验和生成，再 owner/命令/业务步/投影，最后 HUD。最小行为 UTF 加一次真实耗尽→补货→再制作→跨关验收；不扩展全仓或产品构建。文档记录有限供应和样机参数，正式菜单许可收窄保持待裁决。
