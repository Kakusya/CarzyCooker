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
- marker 接口与 UGF system 对齐；System 通过 self.View 读 Mono 绑定，等待加载完成再使用 View。

实施关注重复进入/退出、owner 关闭、加载中退出、旧结果和池重用。本次迁移未运行这些编辑器/玩法检查。

依据：[UI手册](../Book/UI开发.md)、[Entity手册](../Book/Entity开发.md)、[UI规格](../openspec/specs/ui/spec.md)、[Entity规格](../openspec/specs/entity/spec.md)。
