# Tasks

本案 4/4 已完成；逐项实际结果见 [execution-notes.md](execution-notes.md)。前置合同见 [design.md](design.md)。手机／Player 属后续设备案，不计入本案完成。

## 1. 层次与绑定

- [x] 1.1 在既有 Prefab／Board 与 Mono 源调整区域、标签层次和详情显示，保持原资源 ID／owner，按源重新生成 CodeBind；不改表驱动行为。 验收：核对 Prefab/CodeBind/Canvas/射线与真实引用，并运行受影响关闭重开和池复用用例；记录新区域用途。

## 2. 动作与反馈

- [x] 2.1 接入现有投影和权限的主要动作、状态与拒绝反馈；同一点击仍用原目标身份和命令，保留所有合法动作的可访问入口。 验收：真实点击验证等待/成功/拒绝，UTF 检查单次发令、旧回调无效和 Client 越权拒绝，更新用法。

## 3. 入口集成

- [x] 3.1 运行受影响 SoloRestaurantUi／CoopCoreUi PlayMode 小集，覆盖关闭重开、旧回调、拖动和远端权限；普通 Launcher 检查 540×960 的两菜与前厅操作。 验收：按发现结果执行相关 PlayMode，并在 540×960 普通 Launcher 用真实输入完成两菜/前厅及拖动。

## 4. 范围记录

- [x] 4.1 更新 UI 用法与本案逐功能结果，截图只证明显示范围；真机输入记 NotRun。 验收：核对逐功能运行映射，截图只说明显示；真机触控仍记 NotRun。
