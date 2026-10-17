# Design

## Context

动机见 [proposal](proposal.md)。DR 数据由 active ET Luban 的 client/clientserver/editor 目标生成，本案只改 Hotfix 校验和裁决，不改源表／生成物。已有烘烤运行与保存均分开记录 InputItemId 和 CarrierItemId，布局检查尚未跟上该关系。

## Goals / Non-Goals

**Goals:** 将启动预检与实际配方匹配、布局与烘烤关系对齐。

**Non-Goals:** 不引入选择策略、通用生产规划器、第二套库存或新传输；已有界面提案不并入本案。

## Decisions

### 保留真实 owner 和消息链

```text
进程 ET World → 产品 Fiber.Root / SceneType.ProductRoot
├─ ProcessInnerSender（本 Fiber 共享队列）
├─ ProductRuntimeComponent
├─ SceneType.ProductClient（Child Scene，与本地 Host 同 Fiber）
│  └─ ClientSessionComponent（有序 MailBox）
│     └─ GridClientComponent（独立只读投影）
└─ SceneType.ProductHost（Child Scene）
   ├─ HostSessionComponent（有序 MailBox）
   │  └─ HostPeer（有序 MailBox，每成员入口与结果）
   └─ Match（成员 Child）
      └─ RestaurantRuntime
         ├─ GridKitchenComponent
         │  ├─ GridStation / GridItem（Child，唯一实物关系）
         │  └─ GridManualProcess / GridAutoProcess / GridBakeProcess（Child）
         └─ Level（当关阶段／菜单／前厅）
```

远端客户端属于另一产品 Fiber／进程的 ProductClient；测试使用同 Editor 独立 Fiber 的真实 KCP，并非再建立一份权威。

启动先配置校验再创建 HostScene/权威及餐厅；坏配置由原初始化失败路径停止。成员离开只关闭本 Peer，不停止共享发送器；整个运行退出按原 owner 树销毁。

普通客户端 CreateStationLayout → ClientSession.CallProduct → 本地 ProcessInnerSender 到原 Peer ActorId 的 LayoutPeerHandler（ProductHost）。远端经原 KCP Session/Wire 到同一个 Peer。Peer 从 Authority.GetActorId() 向 LayoutSubmitHandler 发送 StationLayoutSubmitRequest，校验后入原 LayoutCommands 队列；厨房业务步 ApplyStationLayout 是唯一状态写入者。接收成功只是排队，提交后原 Peer 记录结果；原 GridBroadcast 先同版快照再 ProductCommandOutcome，客户端安装独立投影及反馈。外层 UI 等待归原 AsyncOwner，物理请求和池对象仍由原请求/handler/using 收尾，本案不新增等待表和事件。

### 匹配条件唯一性

在 GridDishConfig 的现有初始化路径中，用实际运行的键判断冲突：手工（Mode、InputDefinitionId，排除显式 Assembly）、烘烤（InputDefinitionId、CarrierDefinitionId、StationCapability）、批次（ContainerDefinitionId、StationCapability、排序后的输入定义与份数）、菜品（ContainerDefinitionId、ContentDefinitionId）。定义编号不同、输出不同或菜单标签不同不消除歧义。只在启动使用短暂集合，避免逐帧新增工作；错误列出冲突编号。

不增加 RecipeId 请求或任取第一条策略，避免改变已经使用显式组装和热奶／打泡动作的业务。

### 完整验证后提交载具布局

保留现有布局入口和所有几何／角色／版本校验。烘烤预检验证 Baking 状态、原站点及输入反向引用；无载具时保持直接输入合同；有载具时核对外层就是 CarrierItemId，内部唯一是 InputItemId，位置／HolderId／BakeProcessId 全部一致。验证通过后原 Cell 更新循环已经能更新载具及内容，无需迁移或重建过程。

## Risks / Trade-offs

- 过宽的唯一性约束可能误拒绝合法定义 → 按真实匹配键覆盖不同能力和显式组装，实际十一菜配置必须通过。
- 放宽载具检查可能允许坏关系移动 → 补行为测试注入失效关系，拒绝前后核对对象和布局值。
- 局部修正不证明全部平台完成 → 代表性本地／真实 KCP、普通 Launcher 及相关 UTF 足够；Player/AOT、物理 LAN、Android 实机仍未验证。详见 execution-notes。

## Migration Plan

无需数据迁移。保留协议和存档字段；回退仅撤销本案手写校验／布局修正及对应测试。规格同步、归档及提交推送按用户明确授权执行。
