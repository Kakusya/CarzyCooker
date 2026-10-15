# Design

## Context

动机与范围见 [proposal.md](proposal.md)。当前 `MonoGridKitchenBoard` 将 Palette 的一个白色精灵用于地面、设备、人物和物品，另有世界空间短标签；`MonoUIGridKitchen` 沿现有 Prefab 和 CodeBind 配置 HUD。业务客户端提供定义身份、工位 Orientation／Footprint／UpgradeLevel、人物 Facing／Motion／WorkId、容器 Contents／Hygiene、批次与饮品 BoundOrderId，均可直接作为表现输入。

## Goals / Non-Goals

**Goals:** 用一套源文件、模型、固定灯光与相机生成完整现有资源；在原视图增添美术映射、方向、内容与反馈。制作顺序是样板、检查、全量覆盖。

**Non-Goals:** 不添加资源加载框架，不改变 ET 数据、消息、协议或 Excel 字段；不把客户端动画当业务时间，不重做地图和 UI 状态。此次无表配置改动。

## Decisions

### 制作源和软件

在 `Art/CarzyCooker/` 保存美术源脚本、模型、矢量源和 README／清单。Blender 4.5 LTS 官方便携版用于统一软边几何、材质、光向和渲染；Inkscape 用于可编辑界面组件及图标。软件在 `D:/MySoftware`，只作为离线制作依赖。复杂手绘需要时才补 Krita，不为已具备的可控导出能力安装新插件。

游戏继续使用透明 2D 精灵。固定相机正对地图边、俯视 45°，方向通过模型旋转后分别导出，不旋转已经带光影的平面图片。使用温暖木色、奶油墙面、雾蓝绿设备与清楚食物色；角色帽型和围裙区分身份。地板与低墙模块拼接，保持原合法地形。

### 最小资源接入

精灵图集由原 Board Prefab 的已有资源引用链持有；在该 view 创建本轮切片并缓存，ReleaseBoard 释放切片和表现子节点，纹理由 Prefab／GF 持有。优先保持现有 CodeBind 字段，必要时只扩展原 Mono 的序列化美术引用，通过 Editor API 保存 Prefab；不手改生成 partial。

美术选择由定义 ID、工位种类／方向与当前投影驱动，不靠名称判断规则。容器内容、脏污、贴单与加工反馈为表现子节点；同一个物品仍只有业务投影的一处位置。设备、角色与物品排序使用脚底位置与局部层次，标签用于选中目标或实际进度。重复帧复用子节点、精灵和缓冲，不逐帧创建字符串、列表或 Sprite。

### ET owner 和输入路径

```text
ProductRoot Fiber (SceneType.ProductRoot)
  ProductRuntimeComponent
  child Scene (SceneType.ProductHost; Host and Solo)
    component HostSessionComponent + Ordered MailBox
    child Match -> child RestaurantRuntime
                          component GridKitchenComponent
                          child current Level
  child Scene (SceneType.ProductClient; same Fiber)
      ClientSessionComponent -> GridClientComponent (projection)
      GridPresentationComponent
      UIComponent -> UIGridKitchen -> MonoUIGridKitchen
      GFEntityComponent -> GridKitchenBoard -> MonoGridKitchenBoard
                                                  art slices / view children
```

Host／Client 子 Scene 由 EntitySceneFactory 在 ProductRoot 内创建，不是两个新 Fiber；Solo 同样使用 ProductHost SceneType。此树表示现有归属，不创建新业务实体。开局先确认客户端投影，再由现有 GFEntity/UI owner 加载 Board/HUD，TryGridReady 配置地图和视口；退出原 Presentation/owner 移除视图，Mono 释放直接持有的美术对象。

现有输入完整路径为 HUD／场景点击 → GridPresentationSystem → GridClient 的原请求 → ClientSession 原发送边界；Solo／Host 本地继续 ProcessInnerSender.Call，远端继续 Session 入口 → 权威厨房 handler 验证和提交 → 原答复及 GridProjectionApplied → 原 HUD/Board。ActorId、Mailbox、请求等待和消息回收仍由现有发送／接收双方承担；本轮只消费投影，动画不发送完成消息或写业务状态。

### 样板和完整覆盖

先渲染厨师、切配台、灶、碗／锅与番茄链并检查整体比例。随后扩展全部 11 类现有工位及方向、当前物料和容器状态、伙伴／顾客、橙汁贴单、前厅与 HUD。短动作优先共用基础模型：四方向待机、行走、持物与作业；可见加工由业务状态驱动，可用有限帧配合已有插值，不新增游戏计时器。

## Risks / Trade-offs

- 小屏遮挡 → 低墙、少量边缘装饰、控制人物与物品尺寸，在实际竖屏检查。
- 贴图状态误导 → 新 UTF 对比真实容器内容、批次、脏污与贴单状态，并从 Launcher 检查制作链。
- 原生 Sprite 泄漏或重用旧图片 → 资源由实际 view 持有，UTF 检查关闭与新运行，保留原资源释放流程。
- 图集或渲染质量欠佳 → 先看样板／联系表再接入完整资源，不以文件数量证明品质。
- 下载较慢 → 验证实际传输后可用官方提供的镜像，不改源来迁就工具缺失。

具体验证取舍及未验证设备范围见 [execution-notes.md](execution-notes.md)。

## Migration Plan

新增源文件和资源，再修改原 Mono 与 Prefab 引用，编译、刷新资源集合并执行相关验收。需要回退时只撤本 change 的表现和资源引用，不恢复／覆盖其他未提交工作。初始实施不含提交与归档；2026-10-15 用户追加授权 commit、归档与 push，本次收尾按此执行。
