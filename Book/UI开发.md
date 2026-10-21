# UI 开发

Product UI1101 的入口调用 `ShowEntryLayout()`，经营/结算继续用 `ShowPanels()`；`SetInformation()` 控制独立详情覆盖区。标题、正文、记录/动作列表和固定导航各有空间，`AddControl()` 将返回、开局确认及设置保存/取消放入固定导航。入口/选档消费者为 `ProductEntryPresentationSystem`、`SoloSavePresentationSystem`；厨房及各详情消费者沿原 `GridPresentationSystem`，不另开 HUD。正文、动作和入口控制分别由原 Prefab 内的 ScrollRect 滚动，装饰文字不接收射线。原 CodeBind 目标全部保留，无新增 UI 类型或表行。

`AddOrderAction()` 和 `AddTarget()` 共用本轮原按钮；`CompleteContextActions()` 将选中订单的合法交付纳入主要动作，其他前往/交付保留在更多动作的前厅组。原订单区只显示独立滚动摘要，执行按钮不覆盖文字。`ClearTargets()`统一清理，订单图使用HUD本轮物品切片，不新建业务owner。

普通首页突出「开始新游戏／继续游戏／合作游玩」，更多玩法、进度管理与设置放在次级区域。新开直接进入章节选择与预览同页，默认「家常开张」；选择只更新预览，明确确认后才建立餐厅，并保留准备阶段。次级更多玩法保留原独立菜单及内存试玩，进度管理保留合作领取、完成记录和存档授权，合作房间沿原提案／全员准备／房主开局。

首页记录数量使用只读浏览，不为查询新建本机身份；读取中、失败、成功空集分别显示，失败可重试。继续记录须明确选档及检查后确认，完成记录只读；新开仍只在成功结算后新建记录，不覆盖原档。入口布局复用原 Prefab 绑定，通过 `ProductEntryLayout` 区分首页、章节页和其他入口；竖屏纵排、宽屏章节与预览分区，正文、菜单及固定导航各自可触达。返回／取消不代替成功保存。

Editor 或 Development Build 首页次级「测试关卡」提供当前正式章节的全部 Solo 小关及既有独立菜单首关。从准备阶段新建临时单人世界，使用原工位、有限物资、许可、配方和消息裁决；经营成长保留原初始解锁过程。测试不绑定正式存档，不提供领取和授权操作，HUD、设置和结算保留测试标识。原菜单可重开当前测试、返回测试列表或主菜单；完整退役旧运行后再进入，当前应用内记住选择，退出应用不恢复测试世界。没有新增 UI 类型、Prefab 绑定、配置表或测试框架。独立 Player 内表现尚未验收，见[实施记录](../openspec/changes/archive/2026-10-21-development-test-level-entry/execution-notes.md)。

当前 Product HUD 保留原 CodeBind 与按钮 owner，图集和手持 Image 使用直接序列化美术引用；关闭释放本轮图标切片，重开恢复当前投影。面板／按钮使用九宫格 Sprite，装饰 Image 不接收射线；制作和接入见 [餐厅美术约定](../references/restaurant-art.md)。

> **ET 底座适用说明（2026-10-09）**：当前 ProcedurePreset 直接进入 ET；GameHot 源码、配置和 GF Network 运行时已移除。本文保留的 GameHot/纯 GF 示例与双模式切换段落仅作历史参考，不是当前可执行流程。ET、GF 公共组件与 HybridCLR 的适用部分仍以当前源码/配置为准。

GDK 的 UI 配置统一由 GF UIComponent 管理。业务层可选择 GameHot 的 MonoBehaviour 工作流，或 ETUI 的 Entity/System 工作流；两者共享 UI 表、资源路径、UIGroup 和 GF 生命周期。

运行时为 `Image`、`UXImage` 或 `RawImage` 动态换图时，使用 [AssetSet 资源设置](AssetSet.md) 统一处理加载、远程缓存与回收。

## 选择实现方式

| 方式 | 业务代码 | 适用场景 |
| --- | --- | --- |
| GameHot UI | `Game/Hot/Code/UI/` | 熟悉 MonoBehaviour、界面逻辑较直接 |
| ETUI | `Game/ET/Code/ModelView` + `HotfixView` | 使用 ET Entity、System、事件和所有权模型 |

先通过 `Game/Define Symbol` 选择模块，再编辑对应模块的 Excel。模式切换会自动改变 ET 与 GameHot 的 `luban.conf.active`。

## 共同配置链路

```text
UI.xlsx
  → ExcelExporter
  → Assets/Res/Editor/Luban/dtuiform.json
  → UIFormId.cs 或 UGFUIFormId.cs
  → GF 根据 AssetName、UIGroupName 加载预制体
```

### UI 表字段

| 字段 | 含义 | 要点 |
| --- | --- | --- |
| `Id` | UI 类型编号 | 全表唯一，运行时打开接口使用 |
| `CSName` | 生成的常量名 | 必须是合法且唯一的 C# 标识符 |
| `Desc` | 描述 | 同时写入生成代码注释 |
| `AssetName` | 相对资源名 | 相对 `Assets/Res/UI/UIForm/`，不含扩展名 |
| `UIGroupName` | GF UI 分组 | 必须已在 GameEntry 的 UIComponent 中配置 |
| `AllowMultiInstance` | 是否允许多实例 | 弹窗类通常开启，主界面通常关闭 |
| `PauseCoveredUIForm` | 被覆盖时是否暂停 | 决定 Pause/Resume 生命周期 |

GameHot 表位于 `Design/Excel/GameHot/Datas/Game/UI.xlsx`，ET 表位于 `Design/Excel/ET/Datas/Game/UI.xlsx`。

## GameHot UI

### 1. 创建预制体

在 `Unity/Assets/Res/UI/UIForm/Hot/` 下创建预制体。例如：

```text
Unity/Assets/Res/UI/UIForm/Hot/TestForm.prefab
```

表中 `AssetName` 应填写 `Hot/TestForm`。

### 2. 添加配置并导表

在 GameHot 的 `UI.xlsx` 增加记录，例如：

| Id | CSName | AssetName | UIGroupName | AllowMultiInstance | PauseCoveredUIForm |
| --- | --- | --- | --- | --- | --- |
| 103 | `TestForm` | `Hot/TestForm` | `Default` | false | true |

执行 `Game/Tool/ExcelExporter`。常量生成到：

```text
Unity/Assets/Scripts/Game/Hot/Code/Generate/UGF/UIFormId.cs
```

### 3. 创建逻辑脚本

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Game.Hot
{
    public sealed class TestForm : StarForceUIForm
    {
        [SerializeField]
        private Button m_TestButton;

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            m_TestButton.onClick.AddListener(OnTestButtonClick);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            m_TestButton.onClick.RemoveListener(OnTestButtonClick);
            base.OnClose(isShutdown, userData);
        }

        private void OnTestButtonClick()
        {
            Close();
        }
    }
}
```

把脚本挂到 `TestForm.prefab`。`StarForceUIForm` 已处理字体、本地化文本和淡入淡出；不需要这些约定时可直接继承 `AHotUIForm`。

### 4. 打开界面

```csharp
GameEntry.UI.OpenUIForm(UIFormId.TestForm, userData);
```

需要等待加载结果时，使用项目提供的 `OpenUIFormAsync` 扩展。界面内部调用 `Close()` 即可关闭当前实例。

## ETUI

ETUI 由三部分组成：ModelView 中的 ET 实体、HotfixView 中的 System、预制体上的 Mono 视图。`UGFUIForm<T>` 负责把三者与 GF UIForm 关联起来。

### 1. 使用代码生成器

打开 `ET/Code Creator`，选择 `UIFormCodeCreator`，输入 `Test`。工具生成：

```text
Assets/Scripts/Game/ET/Code/ModelView/Client/Game/UI/UITest/UIFormTest.cs
Assets/Scripts/Game/ET/Code/ModelView/Client/Game/UI/UITest/MonoUIFormTest.cs
Assets/Scripts/Game/ET/Code/HotfixView/Client/Game/UI/UITest/UIFormTestSystem.cs
Assets/Res/UI/UIForm/UIFormTest.prefab
```

脚本重载后，工具会把 `MonoUIFormTest` 自动挂到新预制体。生成器不会写 Excel，UI 表记录仍需手工添加。

### 2. 添加 UI 表记录

例如：

| Id | CSName | AssetName | UIGroupName |
| --- | --- | --- | --- |
| 804 | `UITest` | `UIFormTest` | `Default` |

导表后使用 `UGFUIFormId.UITest`。`CSName` 是常量名，不要求与 ET 类名完全相同，但统一命名能减少查找成本。

### 3. 定义实体与视图

生成的 ModelView 代码结构如下：

```csharp
namespace ET.Client
{
    [ComponentOf(typeof(UIComponent))]
    public class UIFormTest : UGFUIForm<MonoUIFormTest>,
        IAwake, IDestroy, IUGFUIFormOnOpen, IUGFUIFormOnClose
    {
    }
}
```

`[ComponentOf]` 应按实际所有者补齐。若界面作为 Scene 的 `UIComponent` 组件存在，使用 `ComponentOf(typeof(UIComponent))`；允许多实例时更适合创建为 Child。

Mono 视图只负责绑定 Unity 组件：

```csharp
using CodeBind;

namespace ET.Client
{
    [MonoBehaviourBinding]
    public partial class MonoUIFormTest : AETMonoUGFUIForm
    {
    }
}
```

推荐在预制体上使用 CodeBind 生成按钮、文本和输入框字段，System 通过 `self.View` 访问，避免在 HotfixView 中保存 Unity 序列化字段。

### 4. 编写生命周期 System

```csharp
namespace ET.Client
{
    [EntitySystemOf(typeof(UIFormTest))]
    public static partial class UIFormTestSystem
    {
        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormTest self)
        {
            // self.View.TestButton ...
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormTest self, bool isShutdown)
        {
        }
    }
}
```

只有实现了对应接口的实体才会接收回调。例如需要 `UGFUIFormOnUpdate`，类声明必须加入 `IUGFUIFormOnUpdate`。

### 5. 打开与关闭

单实例界面通常作为 `UIComponent` 的组件：

```csharp
UIFormTest form = await scene.GetComponent<UIComponent>()
    .AddUIFormComponentAsync<UIFormTest>(UGFUIFormId.UITest);
```

多实例界面使用 Child：

```csharp
UIFormTest form = await scene.GetComponent<UIComponent>()
    .AddUIFormChildAsync<UIFormTest>(UGFUIFormId.UITest);
```

移除或 Dispose 这个 ET 实体时，`UGFUIForm.Dispose()` 会取消未完成加载并关闭对应 GF UIForm。不要绕过所有者只关闭 GF 实例，否则 ET 实体仍可能存活。

## 生命周期对应关系

| GF/ETUI 回调 | 触发时机 | 常见用途 |
| --- | --- | --- |
| `IAwake` | ET 实体创建 | 初始化纯逻辑状态 |
| `OnOpen` | 每次打开 | 绑定事件、读取参数、刷新数据 |
| `OnUpdate` | 界面更新 | 少量持续刷新逻辑 |
| `OnPause` / `OnResume` | 被覆盖 / 恢复 | 暂停输入或动画 |
| `OnCover` / `OnReveal` | 遮挡状态变化 | 可见性相关处理 |
| `OnRefocus` | 多实例重新聚焦 | 更新焦点状态 |
| `OnDepthChanged` | UI 深度变化 | Canvas 或特效排序 |
| `OnClose` | 关闭 | 解订阅、停止任务 |
| `OnRecycle` | GF 实例回收 | 清理视图缓存 |
| `IDestroy` | ET 实体销毁 | 清理 ET 侧状态 |

## UIWidget

可复用的小型界面使用 ET UIWidget。`UIWidgetCodeCreator` 会生成 Widget 实体、System 与 Mono 视图；运行时可通过 `LoadChildUIWidgetAsync<T>` 从 UIEntity 表加载，或把现有 `AETMonoUGFUIWidget` 绑定为 Child/Component。

UIWidget 的资源配置使用 `UIEntity.xlsx`，生成常量为 `UGFUIEntityId` 或 `UIEntityId`，不要与 UIForm 表混用。

## 常见问题

### 打开后提示 UIGroup 不存在

检查 `UIGroupName` 是否已配置在 `Unity/Assets/Res/GameEntry.prefab` 的 UIComponent 中。编辑器预加载流程会扫描 UI 表并记录错误。

### 修改 Excel 后找不到常量

确认当前模块的 `luban.conf.active` 为 `true`，再执行 ExcelExporter。不要手改 `UIFormId.cs` 或 `UGFUIFormId.cs`。

### ETUI 的 `View` 为 null

确认预制体挂有与泛型参数一致的 `MonoUIForm*`，并继承 `AETMonoUGFUIForm`。代码生成后还需等待脚本重载完成。

### 允许多实例但第二次打开失败

同时检查表中的 `AllowMultiInstance` 与 ET 所有权。GF 必须允许多实例，ET 侧也应使用 Child，而不是同类型只能存在一个的 Component。

## 关键代码

| 作用 | 文件 |
| --- | --- |
| GameHot UI 基类 | `Game/Hot/Code/UI/StarForceUIForm.cs` |
| ETUI 桥接 | `Game/ET/Loader/UGF/UIForm/UGFUIForm.cs` |
| ET UI 所有者扩展 | `Game/ET/Code/ModelView/Client/Module/UI/UIComponentSystem.cs` |
| ETUI 示例 | `Game/ET/Code/ModelView/Client/Demo/UI/UILogin/` |
| UI 常量生成器 | `Share/Tool/ExcelExporter/Generate/GenerateUGFUIFormId.cs` |

## 煮汤界面用法

沿原 `UIGridKitchen`／`GridKitchenBoard` 的注册、Prefab 与 CodeBind 使用锅动作：蛋液碗对锅显示“倒蛋液”，空手对未开始灶台锅显示“煮汤”并提供“直接拿锅”，空碗对批次锅显示“盛一份”。地图与 HUD 共用 `CounterIntent`，进度及份数都读同版投影；“动作已执行”表示启动或盛装提交，煮好另显示“已熟”。锅内容、煮制进度与剩余份数由原受管标签显示，不另建每锅 UI owner。操作与重开证据见 [煮汤执行说明](../openspec/changes/archive/2026-10-13-grid-kitchen-soup-batch-cooking/execution-notes.md)。

## 单机关卡基本控制

Solo 提供“开始营业／暂停／恢复”，读取同版厨房／关卡快照。内存试玩结算显示“下一关”；绑定存档模式在装修确定后显示“保存并继续”，文件失败保留结算并允许重试，持久事实确认后才换关。暂停及结算停止显示插值；按钮冻结 UI／世界／关身份，旧 callback 不写新轮。回执表示收到请求，最终状态由餐厅服务步提交。订单、伙伴、buff及零星结算见[关卡执行说明](../openspec/changes/archive/2026-10-13-solo-restaurant-level-flow/execution-notes.md)，保存范围见[检查点记录](../openspec/changes/archive/2026-10-14-solo-success-checkpoint/execution-notes.md)。

普通 Solo 启动时，ProductRoot 的 SoloSavePresentation / UIComponent 复用已注册 UIGridKitchen、原 CodeBind 和按钮模板展示本机记录；选择前不建 Host。选档加载等待归当前 UIGridKitchen.AsyncOwner，取消时关闭原 owner 并重开选择，Epoch 和 EntityRef 拒绝旧回调。确认后根选择表单先关闭，游戏 HUD 改由 ProductClient 持有；不同时建立第二份 HUD。保存等待归 Match 的 SoloSaveResult.AsyncOwner，关闭游戏 HUD 只停止显示等待；写盘完成经 HostSession 本地消息和餐厅服务步确认。首版只新建、不覆盖、无跨设备或联机领取。

## 准备态工位调整

未暂停 Preparing 时，原 HUD 的“调整 工位名”只选中工位；点击地图格提交移动，“旋转 工位名”提交逆时针 90 度变换，“结束调整”返回普通拿放。选中不发业务请求；移动／旋转固定工位身份、关代次和预期布局版本，非法占地／通路、角色或落物冲突显示拒绝原因。设备的第二个占地格同样映射到原 StationId。成功后原物品、容器内容及厨房作业保留，标签跟随新位置；已经离开新操作位的切配者释放认领，玩家从新位置继续同一作业。营业或暂停时不显示调整入口，后台仍重新校验旧按钮请求。此处复用原模板按钮与受管owner，完整餐厅面板现已由UI change接入。实际运行范围见 [工位执行说明](../openspec/changes/archive/2026-10-13-solo-workstation-layout/execution-notes.md)。

## 烘烤与盘装

将面包片放进兼容烤箱后，原地图与 HUD 通过同一 CounterIntent 显示“烘烤”；点击后才启动，烘烤期间拿取／追加／再次开始拒绝，HUD 和 Board 显示同一厨房进度。完成后原面包片被消费，原工位出现一个新身份烤面包。拿起烤面包对空盘选择“添加”，再拿起盘时手持显示“盘（烤面包）”；满盘或错误材料不转移，不交换。盘装成品可以先暂存，订单交付和得分由顾客模块负责。设备、物品、作业和菜品信息沿原 owner／快照读取，未新增 UI 表行或私有计时，运行范围见 [菜品执行说明](../openspec/changes/archive/2026-10-13-solo-dish-recipe-chain/execution-notes.md)。

## 本轮所属异步

菜谱与首单引导（`kitchen-recipe-guide`）：原操作区的两道“菜谱”按钮在详情区显示生效原料／设备／容器／工序及当前缺口；“关闭菜谱”或“收起详情”关闭说明。Solo 可查看、跳过和重看首单步骤，已备好成品无需重做；Host/Client 可读菜谱，阅读不授予共享控制。说明不提交命令或保存任务进度，旧关／旧 owner callback 无效。UTF 与导表已验收，真人理解观察按用户豁免记 Skipped，效果未验证，见[菜谱记录](../openspec/changes/archive/2026-10-14-kitchen-recipe-guide/execution-notes.md)。

餐厅信息分层（`restaurant-ui-information-hierarchy`）：原 UI1101 与受管 owner 保持，CodeBind 正常生成 23 字段。手持物、选中工位与主要动作在底区分别显示，其他合法操作仍在原滚动区；地图点击继续执行原 CounterIntent，选中的操作在主要动作区突出。原阶段／时间／得分在顶区，订单可折叠；“查看详情”只读取当前内容、加工与餐具，关闭不提交业务命令。详情和订单开关由 GridPresentation 持有，旧 owner／关卡回调仍由原 inputGuard 拒绝。相关 UI 小集与普通 540×960 Launcher 两菜／前厅输入均已通过，实际运行范围见[执行记录](../openspec/changes/archive/2026-10-14-restaurant-ui-information-hierarchy/execution-notes.md)。

餐厅完整HUD仍使用UI1101、原WorldSpace Canvas／GraphicRaycaster／模板按钮，绑定通过安全Prefab编辑和正常菜单生成。顶栏显示阶段／时钟／分星／工作，下面显示订单或准备候选，中间为地图，下方控制和手持／目标滚动区，Completed显示结算。CameraDragArea由实际Canvas相机换算成屏幕范围，厨房初始完整适配；在该区纵向拖动查看前厅，拖动不发命令，HUD不穿透，释放点用当前Board相机换算。

HUD打开／加载等待归GridPresentation.AsyncOwner，轮次由HudLoadEpoch核对；具体操作等待归当前UIGridKitchen.AsyncOwner。关闭使用UIComponent.RemoveComponent，不SetActive受管根；OnClose停止该scope并解绑、禁用／销毁自己创建的按钮，原物理Call继续由原helper收尾，不停共享sender或营业。OpenGridHud重开新ET owner并复用GF Mono；旧EntityRef／世界／关按钮不能写新轮。五项真实资源／Launcher行为UTF和普通Launcher两关均已通过，实际540×960画面已查看，详见[UI记录](../openspec/changes/archive/2026-10-13-solo-restaurant-ui/execution-notes.md)。

`UIGridKitchen` 复用原注册 1101、Prefab 与 CodeBind 引用，由 ProductClient 的 UIComponent 持有；地图由同 Scene 的 GFEntityComponent 持有。动态按钮沿 TargetTemplateButton 创建并在 Close 清除订阅，GridPresentation Dispose 移除所属 HUD／Board，不直接显隐受管根。

Solo 首饮入口（协议21）沿上述受管 owner：新游戏／内存试玩201起步，菜谱读取真实切橙、榨汁、盛杯和贴单步骤；加工中提示锁壶。成品杯放贴单台后显示有效匹配订单的“贴单／换绑”与“解绑饮品”，GridIntent 的 ExpectedOrderId 必须经 SendGridIntent→SendGridAction→CreateGridCommand 完整转交。交付仍用原对应餐桌按钮，一次性杯被消费，顾客离席后空手领取实际清桌。v4保存保留壶中份数及冻结加工，旧 v1–v3 恢复原菜单；具体功能状态见[首饮记录](../openspec/changes/archive/2026-10-14-first-drink-order-binding/execution-notes.md)。

备料样机的场景与 HUD 台面入口均取 GridClient.CounterIntent；目标、源物品、目标物品及已有作业身份在意图内固定。兼容碗执行添加，空手切配台执行开始／继续／本人停止；暂停材料提供直接拿取按钮，手持鸡蛋碗提供开始／继续／停止打蛋。按钮仅随物品关系或作业状态变化重建，进度文字随快照刷新。Hint 区分切配／打蛋的加工、暂停和完成，Hand 显示真实碗内容。真实按钮、场景坐标、重开和输入防穿透证据见 [备料验收](../openspec/changes/archive/2026-10-11-grid-kitchen-prep-processing/execution-notes.md)。

公共 owner 自动建立 scope。业务通过 `owner.RunAsync(context => ...)` 等待，或用 `owner.Run(...)` 启动由入口观察的工作；await 后通过 `context.Apply` 写回。Close/Hide 立即取消旧业务等待，Visible/暂停不取消。详细例子、实际迁移入口与原生退役边界见 [生命周期约定](../references/framework-lifecycle.md#所属异步入口)，证据见 [本次执行说明](../openspec/changes/archive/2026-10-11-manage-owned-async-lifecycle/execution-notes.md)。


格子前厅基础显示（协议 11）：原受管 Board 显示紫色伙伴、橙色顾客、工作百分比和阶段；拖动地图查看前厅。准备态点击“开始营业”后，默认节奏自动来客、入座和伙伴询问，询问完成才开单；暂停冻结实际角色与工作。营业／收尾的有效订单提供“前往 餐桌”和手持时的“交付 菜名 → 餐桌”，必须先在桌的合法操作格交付匹配干净菜品，普通放物不计分。吃完离席后空手领取“清桌”，原餐具转脏池产生“洗碗”；玩家可洗一部分“停手前厅工作”，伙伴随后按岗位继续同一任务。洗净后干净池出现“取回 碗（空）／盘（空）”，固定原 ItemId，不补免费餐具。按钮冻结目标、ClaimSerial 或订单／顾客／物品及世界／关身份，经原所属等待发送；角色渲染对象归原 RenderRoot，Hide／ReleaseBoard 统一释放。原Prefab已按UI change扩展并正常重生成CodeBind，表注册保持原UI1101；完整排版、候选buff和UI生命周期现已验收。真实输入与运行边界见 [服务记录](../openspec/changes/archive/2026-10-13-solo-customer-order-service/execution-notes.md)。

随机加成基础入口（协议12）：准备态提供“隐藏候选加成／查看候选加成”，开关只改变显示，不提交命令或重抽。候选按钮显示名称和耗时比例，选择实际提交后锁定；必选关此后才出现“开始营业”。已选名称从同版投影显示在提示中；旧世界／关按钮和 await 回调核对原身份。真实旧锅不变／新锅加速／暂停与新关重选见 [加成记录](../openspec/changes/archive/2026-10-13-solo-random-level-buffs/execution-notes.md)，完整受管重开UTF和排版已通过UI change验收。

## 厨房设施目标的补充

Solo装修在原结算文字显示固定候选、已选或无候选反馈，原受管目标模板提供“装修新增台面／新增切配台”；当前默认允许不选直接继续，已选后下一Preparing才安装。候选不重抽，选择后旧方向callback、旧关和重开旧owner都不提交；已获得工位沿原“调整”及地图输入移动，并跨关保持身份。未新增Prefab／CodeBind字段、未写盘，实际UI与普通两次跨关记录见[装修记录](../openspec/changes/archive/2026-10-14-solo-interlevel-renovation/execution-notes.md)。

Solo有限供应沿同一个受管目标模板显示三种“采购 原料补货 · 余N包”、Ready时的“接收”和空手时的“拆一份”。运输／待收货来自当前Snapshot，接收满槽显示拒绝并保留货物；包装逐份拆出的原材料可照常制作。空包装持物到垃圾桶后显示投垃圾，不能随身删除。SupplyCommandRequest使用原UIGridKitchen所属等待及两段本地有序MailBox，按钮固定世界／关／绑定和货物／材料身份，重开及旧关callback无效；未新增Prefab或CodeBind字段。实际UTF、普通Launcher耗尽再补货制作及在途换关见[供应记录](../openspec/changes/archive/2026-10-14-solo-supply-delivery/execution-notes.md)。

Solo 水池接水／排液和垃圾桶固体倾倒使用原 UIGridKitchen 的受管目标模板及同一地图输入，没有新增 Prefab、CodeBind 字段或 UI owner。设施按钮在目标列表前列，仍捕获当前 UI／投影／世界／关卡／绑定的 inputGuard；关闭重开不复用旧 callback。到位前显示已接收／接近，成功只跟随已提交结果；垃圾桶不能销毁液体，水池不洗净容器。真实输入及普通入口结果见[设施记录](../openspec/changes/archive/2026-10-14-solo-kitchen-clear-and-discard/execution-notes.md)。

### 三关结算与完成记录

既有 UIGridKitchen 仍由原 owner 管理。Solo 标明第几关／三关，结算显示本关成功和未满足订单数。普通关使用保存并继续；显式终关使用保存完成并返回选档，内存试玩标明不保存。选择器的“查看完成”只读结果，不初始化 Host；新开使用独立三关链。回选择器只释放本轮 Host/Client，根 Repository／ProcessInnerSender 保留，避免停止整个入口后无法再次新开。行为与验收见 [三关记录](../openspec/changes/archive/2026-10-14-solo-first-chapter/execution-notes.md)。

### 原工位升级提示

原装修入口显示具体工位、位置和预期等级，选择经原请求传目标。当前详情从独立工位投影读等级；确认继续前仍显示未安装。关闭视图不取消 Match-owned 保存，重开和换关旧回调受原 epoch／Level guard 保护。所有加工显示读取作业冻结 RequiredTicks，不在 UI 连乘 Buff 和升级比例。行为见 [升级运行记录](../openspec/changes/archive/2026-10-14-interlevel-station-upgrade/execution-notes.md)。
