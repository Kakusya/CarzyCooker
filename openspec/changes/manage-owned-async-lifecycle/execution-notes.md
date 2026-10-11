# 执行说明

## 严谨程度与验证取舍

主要风险是公共 owner 退出影响其他对象、取消后仍写状态、未消费任务不清理、池复用使旧结果误匹配、原生资源重复释放，以及退出依赖已停止的更新循环。采用可控完成时机的小集测试加一条真实 Unity 资源路径，先把测试条件定清再施工。已有本地 sender 与产品会话结果仅复用原机制证明，不当作新异步接口验证，也不重复跑全量套件。

明确接受调用者必须使用受管启动、等待和写回；任意直接字段写入、绕过入口或强制中止代码不作保证。业务等待立即结束与原生操作实际回收分开记录；整体退出不等待加载成功，尚未终结的原生句柄不能冒充已释放。若真实原生退出出现异常、残留、阻塞或继续调用旧 manager，针对该路径补验并修订具体适配，不能用模拟通过覆盖。

## 已确认决定

- 2026-10-11：用户要求两个 change，第二项先确定测试条件，再施工。
- 资源加载之外也管理普通异步工作，包括延时和请求等待。
- 采用统一托管调用，公共基类自动接入，迁移本轮相关旧路径；不是可选 token 接口。
- 对象关闭及正常运行退出均立即结束业务等待，不依赖下一帧或加载结束。
- 创建规划产物已获授权；未获本轮源码实施、Editor 启动、测试执行、提交或发布授权。

## 静态证据

ResourceContainer 的版本只在 Create 增加；失败/进度/依赖回调没有轮次检查。Awaitable.ResourceComponent 先等待 IsFinished 再检查 token，且 GetResult 才触发原 ReturnAction。ADynamicUIWidgetLoader 和 UGFComponent 直接调用底层资源等待。GameEntry/Base/ResourceManager 的退出会清除队列和解绑 agent 回调，所以只等底层回调无法覆盖整体退出。

详细条件见 [test-conditions.md](test-conditions.md)；源码和退出树见 [design.md](design.md)。以上是静态核对，不是故障复现或原生清理证明。

## 当前验证状态

规划产物及测试条件齐全，11 项实施任务均未开始。`openspec validate manage-owned-async-lifecycle --strict`：Passed。两个新 change 共 17 份 Markdown、48 个本地引用的存在性/编码/空白检查：Passed；工作区仅新增这两个 change，无已跟踪文件改动。

Unity 编译、UTF、新接口、真实资源关闭/停止、普通任务及 RPC 适配：NotRun。格式通过不表示行为验收通过。Player/AOT、物理 LAN、强制杀进程和全仓异步迁移不在本轮验收内。

后续执行只保存简短结果及原输出路径，原始输出留在原位，不建立 JSON 证据包、完整状态副本或 hash 清单。
