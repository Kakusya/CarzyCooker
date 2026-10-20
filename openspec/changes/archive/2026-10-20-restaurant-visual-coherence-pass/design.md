# Design

## Context

当前存在生产图集与 HandmadeBistro 样板，四方向八帧动作已经接入，不能将本案表述为从零补动画。`MonoUIGridKitchen` 的手持切片数组和启用范围仍固定21；Board布局由导出器生成，当前物品已超出该范围。历史参考文中的图集数量属于各批次记录，实际范围从当前生成布局读取。

## Goals / Non-Goals

**Goals:** 在一个真实厨房中形成可反复扩展的统一样板；手持/订单图标与实际物品一致。

**Non-Goals:** 不重制全部章节/角色，不新增模型运行框架、Entity类型、骨骼系统、镜头玩法、碰撞、布局容量或物品定义；不以资源数量代表美术质量。

## Decisions

### 样板范围固定

| 类别 | 本案内容 |
|---|---|
| 角色 | 本人厨师，原四向待机/行走/持物/作业/递餐帧与挂点 |
| 汤链 | 番茄→切番茄、鸡蛋→蛋液、锅中混合/加工/成汤、空碗/盛汤/脏态 |
| 吐司链 | 面包→烘烤中→吐司、空盘/装盘/脏态 |
| 三类设备 | 切配台、炉台、烤箱，四方向与现有加工状态 |
| HUD | 上述手持/订单图标与标题、正文、主要动作层次 |

角色、器具和工位统一温暖圆润、45°正交、既有世界格尺度与脚底锚点；背景低饱和、操作物轮廓清楚，状态同时使用形态/文字而非只靠红绿。相机保持正对地图边；不为好看的静帧改变真实可走格和操作位置。文字尺寸在布局案的最大字号合同下工作。

一张风格对照图记录相机、相对尺寸、主要颜色、轮廓和阴影方向；用汤/吐司真实操作检验挂点和层级。人体动作沿当前Bistro源修整，不废弃已工作的八帧集。本人以外角色保留身份识别和现有图形。

### 源与导出只有一条对应关系

沿 `Art/CarzyCooker/export_art.py` 和 `HandmadeBistro` 现有制作/导出脚本维护模型、相机、图集布局和挂点。图集、生成布局C#与资产清单必须同次导出；禁止只改生成坐标。样板优先映射到Bistro覆盖，未覆盖内容使用生产图集；在现有README记录确切覆盖，不为同一物品保留两个冲突的优先级。

全部物品的图标修正属于可用性修复：从生成物品数量/定义映射获取切片，去掉21常量和固定数组限制，不批量重画所有图标。对有效映射使用正确图；未知/无映射显示明确占位和名称，不展示前一个物品的残留图。容器外壳、内容、脏净/份数和饮品绑定继续独立显示；订单图标表示订单菜品，不能使用当前手持物代替。

### 受管资源owner与实际消息

```text
ProductRoot Fiber / SceneType.ProductRoot
  ProductRuntimeComponent
  Child ProductHost / SceneType.ProductHost
    Match -> RestaurantRuntime -> GridKitchenComponent + Level（权威）
  Child ProductClient / SceneType.ProductClient
    ClientSessionComponent + Ordered MailBox -> GridClientComponent（投影）
    GridPresentationComponent（当前选择/显示）
    UIComponent -> UIGridKitchen -> MonoUIGridKitchen（本轮图标切片）
    GFEntityComponent -> GridKitchenBoard -> MonoGridKitchenBoard（图集切片/挂点）
GF Resource持有Prefab引用纹理；各Mono释放自己创建的Sprite
```

本案没有新业务请求或消息。沿原Host提交→ProductDelivery→本地ClientSession ActorId/Ordered MailBox或远端Session/ProductClientBinding→ProductClient快照/outcome handler→独立投影→Board/HUD呈现。原DTO在handler finally或发送序列化后Dispose，图形不保存消息/Host实体。

原 `ObserveBistroOutcome` 只响应本人当前身份的Serve Executed，继续使用它触发一次递餐；声画案不能再次触发同一动画。Event只观察，Invoke保持原创建，不新增All订阅。图像变化不写业务状态、发送命令或驱动进度。

投影确认后通过原GFEntity/UI owner加载视图、创建本轮切片，几何重配复用；Pause沿冻结tick显示。关闭/退出按原owner清理，加载await后检查EntityRef/epoch，旧回调不把图贴到新视图。单成员与整体退出沿原边界。

### 技术交付与视觉确认分开记录

先交付完整有限样板及同场景前后图，再由用户在真实游戏里确认风格、辨识度与尺度；这是批量扩产之前的具体评审，不是本轮创建规划的权限请求。技术任务能完成时照常验收，视觉确认保持单独未完成项。若不满意，在本样板内调整或保留原资源，不能擅自扩为全资产返工。

## Risks / Trade-offs

- [混用图集显得拼贴] → 固定两条完整链和三类设备一起检查；其他内容明确回退，先收敛样板。
- [资源替换导致状态失真] → 覆盖空/脏/装料/加工/完成/份数，以及暂停/换关/重开，不只检查静态PNG。
- [导出破坏已有映射] → 保留源和生成对应关系，检查全部当前物品可取图；不手改源表/生成布局。

## Migration Plan

现有运行截图和>21图标复现→制作样板与同次导出→原Prefab/非生成消费者接入→真实生产及最小美术UTF→视觉确认。布局案是HUD最终整合前置；离线样板可先制作。回退按原源和同次导出资源整体恢复，保留用户存档。见 [execution-notes.md](execution-notes.md)。
