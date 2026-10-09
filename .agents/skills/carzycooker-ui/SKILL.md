---
name: carzycooker-ui
description: 接入 CarzyCooker ETUI、GF 托管 UI 或 Widget，核对配置、Prefab、CodeBind、所有者与生命周期。
---

# UI 接入

读根 [AGENTS.md](../../../AGENTS.md)、[UI手册](../../../Book/UI开发.md)、[生命周期](../../../references/framework-lifecycle.md) 与 [规格](../../../openspec/specs/ui/spec.md)。当前 main 分支的 ET 底座的 ProcedurePreset 直接进入 ET；检查 asmdef 和 ET CodeMode，不恢复旧 GameHot UI。

`Design/Excel/ET/Datas/Game/UI.xlsx` → ExcelExporter → `Unity/Assets/Res/Editor/Luban/dtuiform.json` → 公共 UIFormId / ET UGFUIFormId。配置 Id/CSName/AssetName/UIGroupName/AllowMultiInstance/PauseCoveredUIForm；AssetName 相对 `Unity/Assets/Res/UI/UIForm/`，分组须实际配置。导表用 `$carzycooker-luban`。

- ETUI：ModelView 声明 UGFUIForm<TMono>，HotfixView 写 EntitySystemOf + UGFUIFormSystem，Mono 继承 AETMonoUGFUIForm 用 CodeBind partial 引用。唯一实例 UIComponent.AddUIFormComponentAsync，多实例 AddUIFormChildAsync；Dispose/owner remove 退出。
- Widget：查实际 parent API，嵌入与动态 UIEntity 分开；动态资源来自 UIEntity 表，不传普通 Entity ID。

实际 `Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/UIFormCodeCreator.cs` 创建骨架/prefab，重载后挂 Mono，但不填 Excel。Editor 操作使用 Unity CLI/Pipeline，先精确匹配本工程及实际命令；不把自动化连接成功当作 UI 行为验收。

必需绑定修 prefab/生成源，System 不重复 GetComponent/复制状态。不 SetActive 受管根或手调回调。验收关注连续 Open/Close、加载中关闭、池重用、按钮订阅和覆盖 Pause/Resume；运行按授权，未运行写 NotRun。

结构改动先读 [动态 UI SOP](../../../references/dynamic-ui-sop.md)，按“定位资源/表/绑定与消费者 → 安全编辑 Prefab → CodeBind 重生成 → 迁移调用者 → 生命周期验收”推进。表驱动行为与结构工具拆分 change，不伪造尚缺的注册表/runner。行为验收使用 [自动测试 SOP](../../../Book/自动化测试SOP.md) 中实际存在的测试；空集不能当通过。
