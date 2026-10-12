# 框架托管 UI / Entity 生命周期

**非规格**；冲突以代码/表、进行中 change、已同步主规格及用户最新意图为准。**阅读 ≠ 授权**文中未做能力。

适用于 GF UIForm/UIWidget/Entity/UIEntity 和 ET/GF view。普通 MonoBehaviour、UICell、纯 ET Entity 不机械套用。

| 对象 | 一次初始化 | 本轮进入 | 本轮退出 | 临时显隐 |
|---|---|---|---|---|
| GF UIForm/Widget | OnInit | OnOpen | OnClose | Visible / Pause、Resume |
| GF Entity | OnInit | OnShow | OnHide | Visible |
| ET UI view | IAwake、Mono init | UGF lifecycle | UGF close、owner remove | GF view visibility |
| ET Entity view | IAwake、GF init | IUGFEntityOnShow | IUGFEntityOnHide、IDestroy | GF view visibility |

OnInit/IAwake 一次建立状态，Open/Show 建本轮 userData、订阅和任务，Close/Hide 只清本层本轮所有权。

- 使用 GameEntry.UI/Entity 或 owner 进入/退出，不手调回调、不 SetActive 受管根、不调用 InternalSetVisible；纯表现子节点可显隐。
- Visible 只保留状态临时隐藏，需要释放事件/资源/任务须 Close/Hide。
- AExUIForm/AExEntity 容器负责事件、实体、资源、池；派生层不再全量清一次。
- 进入按基类契约先建立状态，退出先撤销本层直接订阅/CTS 再交 base/owner。一次性按钮绑定不反复解绑。
- ETUI 唯一实例 UIComponent.AddUIFormComponentAsync，多实例 AddUIFormChildAsync。ETEntity 对应 GFEntityComponent.AddGFEntityComponentAsync / AddGFEntityChildAsync。
- UGFUIForm.Dispose 取消在途打开并关闭 GF UI，UGFEntity.Dispose 取消在途显示并隐藏 GF Entity；仅 GF Hide 不等于 ET Dispose。
- 同步 `ShowUIEntity<T>` 委托 UIEntity Type 入口，只查 DTUIEntity 并使用 UIEntity 资源根；缺行警告/null，不回退普通 Entity。逻辑类型、运行 serial ID 和 userData 沿现有入口传递。
- marker 接口与 UGF system 对齐；System 通过 self.View 读 Mono 绑定，等待加载完成再使用 View。

初始规格迁移未运行生命周期检查；本次所属异步的实际小集与原生证据见 [执行说明](../openspec/changes/archive/2026-10-11-manage-owned-async-lifecycle/execution-notes.md)。

更细的 [ET 树销毁与实例引用](../openspec/specs/runtime-foundation/spec.md)、[UI 等待与 Widget 状态](../openspec/specs/ui/spec.md)、[Entity 失败与视图回调](../openspec/specs/entity/spec.md) 已纳入主规格。取消请求不等于底层下载已中止，GF view 的附加/隐藏不等于迁移或销毁 ET owner；资源回调失效范围见 [已知问题](../KnownIssues.md)。

依据：[UI手册](../Book/UI开发.md)、[Entity手册](../Book/Entity开发.md)、[UI规格](../openspec/specs/ui/spec.md)、[Entity规格](../openspec/specs/entity/spec.md)。

## 所属异步入口

`IAsyncOwner.AsyncScope` 表示真实本轮进入，`AsyncContext` 捕获该轮身份。Game 程序集的 `AsyncRuntime` 只登记 scopes 和统一观察错误，不存业务状态。AExUIForm 在 Open 建 scope，AUIWidget 在 Open 建 scope，AEntity（包括 AExEntity、ETMonoUGFEntity）在 Show 建 scope；Close/Hide 先分离旧 scope 再 Stop，Recycle/Destroy 幂等兜底。Visible、Pause/Cover 不结束一轮。

```csharp
await owner.RunAsync(async context =>
{
    var asset = await context.LoadAssetAsync<GameObject>(assetPath);
    await context.Delay(TimeSpan.FromSeconds(1)); // Unity Mono owner
    context.Apply(() => ApplyView(asset));       // 短同步写回，不能在此 await
});
// 不等结果也由同一内部观察者承担登记、错误与资源收尾：
owner.Run(async context => { await context.Delay(TimeSpan.FromSeconds(1)); });
```

资源绑定 scope 自带 ResourceContainer；`ReadAssetAsync<T,TValue>` 在安全期内读取独立值并释放本次引用。直接 ResourceContainer 也必须传有效 IAsyncOwner；UnloadAllAssets 仅结束资源轮次，不停止该 owner 的延时/请求。Clear 使旧请求失效，但仍不能代替已取得资源的正常卸载。`TransferAsset` 只移出容器归属，接收方须显式保存原 backend 并承担释放，不能在无人消费的返回对象上暗做移交。

ET 通过 `entity.GetAsyncOwner()` 创建 Component，保存原 EntityRef/InstanceId；子对象应绑定自身，不把全部工作挂到 Scene。UGFComponent 默认绑定原 Scene，也提供显式 Entity owner overload。Unit 创建绑定 Unit；LSUnit 视图创建绑定 LSUnitViewComponent，并在 Apply 内再次核对原 LSUnit EntityRef。GF Hide 只停止该 GF view scope，不销毁 ET 逻辑 owner。

ET 等待使用原 Fiber 的 `context.DelayAsync(timer, milliseconds)` 和请求适配，保留 Timer/Session/ProcessInnerSender 的原执行路径。异步入口在调用原委托前登记；Stop 返回前对外等待为取消并移除登记。内部忽略取消仍由唯一观察者消费其结束，旧 Apply 被拒绝；任意直接字段写入、跨线程调用和未迁移的第三方 async 不属于保证。

产品本地 Call 的 Peer 在途责任由 `CallLocalValueAsync(..., physicalFinished)` 的内部观察者持有；外层 owner 取消立即结束显示等待，但 EndEntry 要等原答复、原超时或整个 sender.Stop。该回调捕获原 Peer EntityRef，不将新实例当旧实例收尾。未交接的停用 owner／sender 输入由发起边界释放，已交接输入由 handler 释放。此处没有另一份等待表，运行证据见 [本地产品调用](../openspec/changes/solo-local-message-contracts/execution-notes.md)。

已迁移的运行加载：CodeLoader（保留 World 检查）、ConfigReader、Recast、UGFHelper、Tables、HybridCLR。后两者只迁移加载与回收，不修 AOT 返回码，也不改表或生成代码。UI/Entity 显示等待器、Scene 事务、AssetSet 的独立对象池、其他旧业务和外部包 async 保持各自契约；底座 ResourceComponent.LoadAssetAsync 保留为明确所有权的原始入口，取消直接结束等待，晚到引用归还原 manager。

## 正常整体退出和原生尾部

GF Runtime GameEntry.Shutdown 和 Base.OnDestroy 在 GF 模块拆卸前调用捕获的 Base.ShuttingDown。GameEntry 的订阅捕获原 AsyncRuntime，先使该运行全部 scopes 无效，再同步取消；ET.Init.Shutdown 先 Stop 自己的 scope，再执行原产品停止/OnShutdown/World.Dispose。旧运行重复通知只作用于原实例。

DefaultLoadResourceAgentHelper.Reset/Dispose 将未交给 manager 的 bundle 创建请求交给其 completed 事件释放；已交给 manager 的 bundle 与依赖仍只由原 manager/DefaultResourceHelper 释放。NativeResourceRetirement 仅保留原生句柄及待释放 bundle，不是加载器、资源池或缓存；已有资产读取未结束时暂缓原 manager 的 bundle 释放，避免连带依赖被提前卸载。完成尾部不访问 GameEntry、agent 或已关闭的对象池。业务取消不等待这一原生尾部；引擎完整退出后不承诺再次执行 C# 回调。
