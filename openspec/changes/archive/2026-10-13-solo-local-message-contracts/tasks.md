# Tasks

已进入 apply，完成状态以各任务实际实现与运行证据为准。范围与路径见 [design.md](design.md)，逐功能证据填写 [execution-notes.md](execution-notes.md)。

## 1. 本地调用寿命

- [x] 1.1 在既有 ProductSessionTests 复现所属等待取消与原 Call 尚未结束的交错，断言对外取消、原 Peer InFlight 和 MailBox 寿命；记录真实失败或未复现结果，不能先把静态风险记成已复现。
- [x] 1.2 将 BeginEntry／EndEntry 的交接移至原物理调用观察者，补执行响应、原超时、同步失败、整体 Stop 和单 owner 取消的最小 UTF；断言响应只收尾一次、其他 owner 正常，更新网络／所属等待用法说明。

## 2. 上下文与业务结果

- [x] 2.1 在原 Model/Hotfix 实现类型化上下文及共享新动作编号，必要类型只追加协议源并正常生成；执行旧世界／绑定／动作载荷冲突拒绝的 UTF，核对当前程序集与生成 diff，更新产品消息约定。
- [x] 2.2 在可信入口实现最新重放返回事实、更早编号过期和接收／最终结果区分；经真实 Peer→HostSession→Tick 执行 UTF，断言不重做、不阻塞其它请求、DTO 回收后投影独立，并补本组行为文档。

## 3. 可重复单机测试流程

- [x] 3.1 扩展现有 fixture 的 Solo 创建、PumpMessages 与 AdvanceTicks，保留活跃 World 拒绝和原清理；UTF 断言零监听、泵消息不推进业务时间、一次业务步无双推进、测试退出只清自己对象，写简短 fixture 用法。
- [x] 3.2 执行实际发现的本地小集及受影响 Grid 重放／所属等待用例，再从普通 Launcher Solo 点击动作、关闭并重新进入；确认新轮没有旧答复、单击单意图。逐功能填真实结果并严格验证 change，不跑 KCP、全仓套件或构建。
