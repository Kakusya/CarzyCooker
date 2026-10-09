---
name: carzycooker-ugfentity
description: 接入 CarzyCooker GF Entity、ET UGFEntity 或 UIEntity，核对类型/实例 ID、视图加载与 owner 生命周期。
---

# Entity 接入

读根 [AGENTS.md](../../../AGENTS.md)、[Entity手册](../../../Book/Entity开发.md)、[生命周期](../../../references/framework-lifecycle.md) 与 [规格](../../../openspec/specs/entity/spec.md)。当前 main 分支的 ET 底座无 GameHot；ET owner 实名是 GFEntityComponent。

`Design/Excel/ET/Datas/Game/Entity.xlsx` → ExcelExporter → `Unity/Assets/Res/Editor/Luban/dtentity.json` → 公共 EntityId / ET UGFEntityId。核对 Id/CSName/AssetName/EntityGroupName/Priority，Prefab `Unity/Assets/Res/Entity/`。类型 ID 不等于 serial/entity ID。导表用 `$carzycooker-luban`。

- ET：ModelView 声明 UGFEntity<TMono> 和 marker，Mono 继承 AETMonoUGFEntity，HotfixView 写 EntitySystemOf + UGFEntitySystem。单例 GFEntityComponent.AddGFEntityComponentAsync，多例 AddGFEntityChildAsync，等 Show 完成再访问 View。Dispose 取消加载并隐藏 GF；仅 GF Hide 不销毁 ET 业务对象。
- UIEntity：独立表、资源路由、UGFUIEntityId 和 ShowUIEntityAsync，不混普通 EntityId。检查 overload 的真实实现，差异见 [已知问题](../../../KnownIssues.md)。

骨架工具是 `Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/UGFEntityCodeCreator.cs`，输出 ModelView/HotfixView 的 Game/UGFEntity/<Name>，类型前缀为 UGFEntity / MonoUGFEntity，不会建 prefab 或填表。Editor 操作用 Unity CLI/Pipeline 按本工程发现实际命令；不声明技能自带自动制资源能力。

验收关注重复 Show/Hide、owner 移除、加载中 Dispose、附加/脱离与池重用。根显隐用 Visible，不 SetActive；派生层不重复清 base 容器。未运行的编译/玩法检查写 NotRun，不新增工具或证据包。

接入顺序：定位 Entity/UIEntity 源表、资源与消费者 → Check/生成 ID → 安全制作 Prefab 与 Mono 绑定 → ModelView/HotfixView 骨架 → owner API 接入 → 生命周期验收。制作流程不声明生成器会创建 Prefab/Excel。资源变更读 [ResourceCollection SOP](../../../Book/ResourceCollection导出SOP.md)；UIEntity 结构另读 [动态 UI SOP](../../../references/dynamic-ui-sop.md)。测试仅按已授权且实际存在的用例执行。
