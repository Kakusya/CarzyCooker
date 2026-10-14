# reduce-per-frame-allocations

基于[逐帧逻辑与内存分配审查](../../../../audit/2026-10-14-per-frame-performance.md)，减少逐帧及固定业务步中的不必要托管分配和重复工作，降低 GC 压力。本案纳入地图渲染、视口数组、正常／恢复 HUD，以及固定步遍历与 BFS 临时缓冲；消息快照、sender 批次及第三方工具保留为另案候选。

规划入口：[提案](proposal.md)、[设计](design.md)、[任务](tasks.md)、[执行说明](execution-notes.md)。本案保持既有玩法、owner、消息回收和重入安全合同，按纯重构声明 `skip_specs: true`，无 delta specs；九项任务已全部实现并验收，2026-10-14 已归档；运行与性能采样限制见执行说明。
