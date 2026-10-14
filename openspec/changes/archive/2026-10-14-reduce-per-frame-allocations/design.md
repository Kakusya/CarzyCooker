# Design

## Context

动机和范围见 [proposal.md](proposal.md)，源码依据见[审查](../../../../audit/2026-10-14-per-frame-performance.md)。本案涉及表现刷新及固定业务步，存在性能和生命周期取舍，因此需要 design；它不是新玩法、协议或框架。

当前 GridPresentation 每帧传入独立客户端投影，MonoGridKitchenBoard.Render 再 ToArray／LINQ／拼标签。HUD MapViewport 每次 new 四角数组；正常 HUD 的投影事件与 Update 版本判断都可触发刷新；恢复分支每次 ClearTargets／AddControl。RestaurantRuntime 以 50ms 推进同一厨房，OrderedActors、顾客／工作排序及 BFS 容器在调用时新建。

相关代码仍有其他未提交改动，尤其 cooperative-member-resume 的恢复显示。本案以实施时的当前实现为基线，保护已通过的绑定、反馈与旧回调合同；不覆盖其他 change 的修改或替它们补验双 Editor。

## Goals / Non-Goals

**Goals:** 用实际 owner 的有限缓冲和显示上下文比较消除指定热路径的重复分配；保留每帧输入和移动插值，保证状态变化及时显示，固定步顺序和路线语义不变。

**Non-Goals:** 不建立通用缓存／池／性能框架，不改 UGF UI 结构、消息传输或投影安装；不设整局零 GC 或精确帧率指标。更大范围的候选见 proposal，不隐含进入本案。

## Decisions

### 1. 保持实际 owner 树，缓冲只存派生表现或临时工作数据

```text
Fiber / Root Scene (ProductRoot)
  Components: ProductRuntime / Timer / CoroutineLock / ProcessInnerSender
  Child Scene (ProductHost, Host or Solo)
    Component: HostSession + Ordered MailBox
      Child: HostPeer + Ordered MailBox + LocalDelivery or RemoteDelivery
    Child: Match
      Child: Participant
      Child: RestaurantRuntime                  [business clock]
        Component: GridKitchenComponent          [world state + search/actor buffers]
        Child: Level                            [current-level state]
          Components: FrontLevelState / FrontWorkCommands [their temporary buffers]
          Children: GridActor / Customer / FrontWork / CompanionRole
  Child Scene (ProductClient)
    Component: ClientSession + Ordered MailBox
      Component: GridClientComponent             [independent value projection]
    Component: GridPresentationComponent        [display choices + refresh context]
    Component: UIComponent
      Component: UIGridKitchen -> MonoUIGridKitchen [viewport scratch]
    Component: GFEntityComponent
      Component: GridKitchenBoard -> MonoGridKitchenBoard [render scratch]
```

远端只有客户端一侧；Host/Solo 沿现有启动顺序先建权威餐厅，再建客户端与 Board/HUD，GridReady 仍要求本人确认、完整投影和托管视图就绪。缓冲不保存 Host EntityRef 到客户端，不成为位置、库存、工作进度或成员资格的另一份事实。

Board.Hide／ReleaseBoard 清自身派生缓存；HUD.Close／ReleaseHud 清本轮比较上下文与事件；重开即使复用同一 Mono 也首次完整刷新。GridPresentation Awake／Destroy、换关与新绑定令旧上下文失效；厨房和 Level 缓冲在各自 Awake／退出清有效内容。选择 owner 局部缓冲，避免静态共享容器让多运行／多 Fiber 互相覆盖。

### 2. 渲染分开处理投影变化和逐帧位置

Render 直接使用已有独立值投影的具体集合／索引，或复用预设容量的 List／HashSet 和待删 ID List；不经 IEnumerable 每帧复制整批数组或构造集合差集。先收集待删 ID，再移除和销毁对应表现，避免边遍历边修改字典。

成员／工位／物品增删、路径、高亮、布局和标签内容按相关字段变化更新。Grid.Version 可以触发内容核对，但标签最终比较显示所需字段再格式化，不能每帧先造字符串后比较。地砖保留上次着色集合，只恢复旧路径／高亮并设置新路径／高亮；保持玩家路径、伙伴路径、目标高亮的原覆盖顺序。

角色／伙伴／顾客与手持物的插值、跟随位置及必要 sortingOrder 仍每帧更新。工位占地包围盒及同格落物排列只在输入变化时整理，保持按物品 ID 的排列。暂停与 Completed 使用已提交 tick，不能因静态内容短路跳过位置、朝向、标签跟随或清除旧高亮。替代方案“只按整个 Version 渲染”会让插值停顿，因此不采用。

### 3. 先复用视口四角，不扩大布局事件体系

MonoUIGridKitchen 持有一个四角数组，每次 MapViewport 填充并返回 Rect 值；Canvas 缓存绑定当前 HUD，关闭／重新绑定时失效。先保留现有每帧尺寸计算和 SetHudViewport 的已有短路，即可去掉明确数组分配，避免漏掉分辨率、布局或父 Canvas 变化。无需增加全局布局注册表或改 Canvas／CodeBind。

### 4. 正常 HUD 使用一个刷新入口，结构按完整字段比较

保留事件与直接调用的及时反馈，RefreshGridHud 成功展示后记录本轮已显示版本，Update 不再为同一个投影追加刷新。本地选中、详情／订单／菜谱开关、命令结果等即使 Grid.Version 不变，也能明确请求刷新，不能只有版本判断。

替代 HudTargetsKey 长字符串的结构比较使用 owner 内的标量上下文与复用条目列表，覆盖原键全部输入：运行／世界／关／绑定、布局及选择、手持／物品／容器／作业状态、订单、buff、供应／货物、装修／升级、存档和控制资格，以及本地显示选择。保存字段比较值和 ID，不保存消息对象或旧回调。列表只在原排序有语义时复用排序；比较到真实结构变化才调用原 ClearTargets／AddTarget／AddControl。

时间、进度与信息文本和动作结构分别更新；相同显示值先比较再格式化。初始显示、HUD 新实例或关闭重开必须完整刷新。本案保留现有按钮创建／销毁契约，仅减少不必要重建，不新增私有按钮池，也不重构 Prefab/Widget 层级。

### 5. 恢复显示按上下文变化更新，输入资格持续实时核对

每帧保留“是否处于恢复显示”的判断和禁输入逻辑；只有 Stage、AttemptId、Feedback、凭证可用性、原 Client／HUD EntityRef 或其本轮打开上下文变化，才重建显示和捕获回调。条件退出时失效恢复显示记录，正常投影恢复必须完整刷新，不能留下旧恢复按钮。

凭证本身不加入缓存比较、日志或字符串键，只比较可用性和现有运行身份。回调继续实时核对原 Runtime／Client／HUD、尝试号及状态；缓存相等不能授权命令。稳定断线帧复用既有按钮，反馈更新及同状态新尝试仍可显示；选择“只缓存 Stage”会漏掉这些输入，因此不采用。

### 6. 固定步缓冲保留稳定顺序和增删隔离

角色有序工作列表归 GridKitchen，按 MemberId 保持原顺序；CanStop 仅计数，直接遍历可用角色，不重复排序。顾客／前厅工作／伙伴的临时列表归既有 FrontLevelState／FrontWorkCommands，使用原 ID 或工作优先级顺序，容量稳定后 Clear／填充／Sort 复用。

可能 Dispose／增删的调用保留 EntityRef 快照并在使用前解引用；新增对象进入原规定的下一次遍历阶段，不能换成直接枚举 Children 并靠异常容忍。待执行前厅命令仍分为原 Stop 优先、状态推进、Claim 阶段，保持原队列顺序和单次消费。缓冲仅用于同步服务步，不跨 await，不改变本地消息的提交与答复位置。

### 7. BFS 只复用工作区，不共享返回路线

厨房按当前 7×18 地图容量预设搜索队列、访问／前驱工作区；每次搜索重置有效内容。热循环直接产生上、右、下、左四个 GridCell 值，保持既有顺序；公开 Neighbours 的数组返回契约及启动布局校验不必改写。

同步的内部搜索可以填充调用者自己持有的路径或只查询可达性。公开 FindGridPath 仍返回独立结果，外部保存的路线不被下一次搜索覆盖；迁移玩家、顾客、前厅工作和伙伴候选的真实消费者时，最佳接近格的候选路径在下一次搜索前按其 owner 保存。不能把一个共享 List 直接作为每次搜索结果，也不能用连通性缓存替代原动态停驻容量与满格等待。

伙伴选工作只需要可达性时，不重建完整路径。搜路工作区按同一厨房同步调用使用，结果消费前没有 await／业务通知；若遇到实际重入，保留独立调用者缓冲隔离，不增加全局池。工位几何数组和全量快照分配先保留，不声称固定步完全零分配。

### 8. 消息路径与等待责任不变

```text
HUD input -> ClientSession -> local ProcessInnerSender.Call(HostPeer ActorId)
remote input -> Session / wire copy -> HostPeer Ordered MailBox
HostPeer -> HostSession ActorId / Ordered MailBox -> queue intent
Restaurant 50ms step -> validate / commit world -> independent snapshot
snapshot -> LocalDelivery / RemoteDelivery -> ClientSession MailBox
ProductClient handler -> copy / install GridClient values -> observation event
observation event -> existing GridPresentation HUD refresh entry
```

本地目标仍由 LocalMessage.Peer／HostSession.GetActorId 取得，根 sender 的队列和 MailBox 不变，远端请求仍经 Session 白名单进入同一裁决。状态提交后才发布观察 Event；UGF/ET Invoke 继续承担原框架职责，不新增 SceneType 或消息类型。

handler 释放输入，调用者消费／释放每跳答复，本地通知由接收端收尾，远端发送方在序列化后释放；GridClient 不保存消息集合。等待仍归原 UI AsyncOwner，await 后核对原 EntityRef 和绑定；关闭取消显示等待，原物理请求继续按现有观察者收尾。单成员退出不停止共用 sender，整体退出才结束公共资源。本案不调整这些责任或 ProcessInnerSender.ToArray。

## Risks / Trade-offs

- 缓存漏字段或重开不失效 → 字段比较覆盖现有结构键，并用同版本反馈、变化进度、换关／重绑和旧回调 UTF 验证。
- 快照缓冲迭代中实体退出 → 保留 EntityRef 和原顺序，收集待删后再移除；不会为减少分配删生命周期保护。
- BFS 工作区覆盖尚在使用的结果 → 公开结果独立，热路径填充调用者路径，连续搜索验收覆盖路线稳定性。
- 首次创建、扩容、消息和投影安装仍有分配 → 分开预热和稳定帧；本案只承诺指定重复工作减少，实际收益见 execution-notes。

验证规模与接受的未覆盖项统一见 [execution-notes.md](execution-notes.md)。无需另建性能评分或全量实验矩阵。

## Migration Plan

具体批次与优先级唯一见 [tasks.md](tasks.md#批次与优先级)：基线 → 视口／Board → 恢复／正常 HUD → 固定步／搜索缓冲 → 综合验收／性能复测。前两组 P1 优先，固定步组 P2 随后；每组立即执行其最小相关行为验收，整体收口只补未覆盖点，全部覆盖后做一次对应性能复测。恢复改动复用当前 cooperative-member-resume 结果作为未改协议事实，不替代本案显示新实现的运行。

2026-10-14 用户要求暂时不考虑真机测试，当前执行批次聚焦 Editor、UTF 和真实 Launcher/HUD；本案原有 Player／Android／双 Editor 范围外条件不变。跨 change 的双 Editor 增量和后续设备路线仍各归原案，不把暂缓改成通过或撤销验收。

实现阶段加载 apply、UI、Entity 技能；执行 Unity／UTF 时加载 unity-cli／auto-testing-sop，多 Editor 接入若超出本案范围需另行规划。本案不改 Excel、协议、自动生成绑定或 Prefab，无 Luban 导出任务。同步涉及的实现约定和审查项，保持原主规格不变。

回退仅撤回本案具体优化块并恢复原局部算法／刷新方式，保留其他未提交修改；不 reset 工作树、清用户现场或更换调度／消息框架。

## Sources

- [GridPresentationSystem](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)、[恢复显示](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationResumeSystem.cs)。
- [Board](../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoGridKitchenBoard.cs)、[HUD](../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoUIGridKitchen.cs)、[view owner](../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/GridPresentation.cs)。
- [厨房模型](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)、[GridKitchenSystem](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridKitchenSystem.cs)、[前厅工作](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridFrontWorkSystem.cs)、[伙伴调度](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridCompanionSchedule.cs)。
- [restaurant-ui](../../../specs/restaurant-ui/spec.md)、[grid-kitchen](../../../specs/grid-kitchen/spec.md)、[fixed-companions](../../../specs/fixed-companions/spec.md)、[生命周期](../../../../references/framework-lifecycle.md)。
