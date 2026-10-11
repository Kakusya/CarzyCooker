# Tasks

本文件是待实施清单，全部未开始。本轮只创建规划。先按 [test-conditions.md](test-conditions.md) 固定断言和受控时序，再施工；[design.md](design.md) 的统一入口及原生收尾边界不能退化为仅加可选 token。

## 1. 测试先行

- [ ] 1.1 在既有 UTF 测试体系中先写 T1–T9 的受控底层夹具和关键断言，覆盖永不回调、晚到通知、轮次复用、无人消费、重入、共享 owner 和无后续业务帧退出；必要的接口声明可一并建立。验证用例可发现、替身只控制底层时序且不代做被测清理；有可运行骨架时记录实际失败，编译失败不冒充行为测试失败。

## 2. 统一 owner 及普通任务核心

- [ ] 2.1 在 Game 现有程序集实现 IAsyncOwner/AsyncScope/AsyncContext、统一 RunAsync、一次性终态和受控 Apply；覆盖立即取消、独立于结果消费的登记/错误观察、原轮次身份与重入隔离，以 T1/T2/T4/T5/T6/T9 的核心小集验证。补充生命周期引用中的调用示例和绕行边界，不创建新调度器、缓存或程序集。

## 3. 资源等待和底层责任

- [ ] 3.1 接入 ResourceContainer 与现有资源等待：所有出口检查原归属，UnloadAllAssets/Clear 使旧资源轮次失效，取消不等待底层或 GetResult，晚到结果由原 backend 归还本请求引用；用 T1–T7 适用资源断言验证 callback/await、旧类型错误、共享引用和无人消费，复用已运行核心结果；同步 AssetSet/生命周期说明及对应 KnownIssues。
- [ ] 3.2 在现有资源 agent/helper 的 Reset/Shutdown 连接最小原生退役交接，保留进行中请求直到终止或安全移交，明确 bundle 创建、资产加载、依赖和原资源管理器各自释放责任；以受控拆卸和晚到完成验证不访问死模块、不重复卸载、不提前复用记录，并记录真实原生验证仍待 7.1，更新资源引用说明。

## 4. 实际调用者接入

- [ ] 4.1 将 AExUIForm、UIWidget、AExEntity 及 ADynamicUIWidgetLoader 接入本轮 scope，父界面关闭时动态加载也失效，Close/Hide 与 Recycle/Destroy 幂等而 Visible/暂停不误取消；用一个真实公共 Mono 入口验证关闭/再进入及晚到结果，复用公共矩阵结果；同步 UI/Entity 手册的 owner 用法。
- [ ] 4.2 在现有 ET 层用 Component/Child API 提供 owner 适配，绑定原 EntityRef/InstanceId，将 UGFComponent 资源桥及当前 Unit/LSUnit 视图创建的必要写回接到实际 owner；以 ET 销毁/同 Id 新实例、子对象先退出和仅 GF Hide 验证隔离，保持逻辑 owner 树；同步 ET 资源桥/Entity 引用说明。
- [ ] 4.3 迁移 design 已定位的 CodeLoader、ConfigReader、Recast、UGFHelper、Tables/HybridCLR 本轮运行所属资源调用，保留原 World 检查及显式资源转移；核对直接消费者并验证启动加载中途停止时没有旧运行写回、无人消费不丢引用。文档列出已迁移入口和保留的底座原始入口，不扩大到独立 AOT 修复或 Scene 事务。

## 5. 延时与请求适配

- [ ] 5.1 为普通延时和代表请求提供 context 适配：复用 Session.Call 的取消及原超时需求，区分 ERR_Cancel，明确池化响应的值化/移交和 Dispose；本地 Call 只结束调用者等待、由单一观察者沿原答复/有界超时收尾，不 Stop 共用 sender。用 T2/T5/T7 验证请求/延时和两 owner 隔离，保持 ActorId/MailBox 路径；同步网络/生命周期引用，不新增 RPC 等待表。

## 6. 无后续帧的整体退出

- [ ] 6.1 在 Runtime GameEntry/Base 的 GF 拆卸前及 ET.Init 的 World 拆卸前连接捕获原运行身份的停止通知，先失效所有所属 scope、禁止新增、立即提交取消，再走既有拆卸；用 T8 及重复退出/新运行断言验证不依赖帧、Timer、加载回调或当前静态 manager。同步退出顺序和原生尾部说明，保持底层依赖方向和原 sender.Stop 契约。

## 7. 必要集成验收

- [ ] 7.1 按既有 AutoTesting SOP 完成必要编译及一条真实进行中 Unity 资源加载的关闭/运行停止验证，核对业务取消、无旧写回、共享引用/原生退役及下一轮加载；仅补尚未覆盖的代表入口，复用前述小集及已有 sender/产品会话证据。拿不到真实原生证据写 NotRun 并保持该验收任务未完成，不拿模拟或 Editor 直读冒充 AssetBundle 验证，不跑双 Editor、Player/AOT/LAN 或全仓套件。
- [ ] 7.2 核对本 change 的源码、相关说明和 delta specs 与 T1–T9 证据一致，执行 OpenSpec 严格验证并在 execution-notes 记录实际输出位置、Passed/Failed/Skipped/NotRun 及接受边界；不以任务勾选替代测试终态，不自动提交、推送、同步主规格或归档。
