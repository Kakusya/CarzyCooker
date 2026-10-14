# 逐帧入口索引（2026-10-14）

与[审查结论](2026-10-14-per-frame-performance.md)配套。枚举 2561 个可见 C# 文件，提取 224 个相关候选方法体。下表按源码分组，文件链接和声明行便于复查；抽象／接口声明没有方法体，不计入 224。同名 API 不证明实际每帧调用，目录也不证明运行模式。

检索名称涵盖 Update／LateUpdate／FixedUpdate／OnUpdate／Tick、UGF 生命周期、LSUpdate、帧始／帧末 tick、Unity 渲染回调和 OnGUI；另检索回调注册、逐帧 timer、等待及协程。排除缓存、构建、忽略文件和二进制，不覆盖已安装包／插件内部及任意动态注册。方法表不是调用图，真正产品链路和间接分配见审查正文。

## 判读约定

- `NumericComponent.Update` 是数值重算；SVN 的 Update 是操作命令，均是名字匹配，不能当常驻逐帧。
- DotRecast 的 Update 需要外部调用，本次没有发现产品调用路径。
- SocoTool Runtime 下的 OnGUI 是 Shader 工具编辑器内容；GameTimer 的 Tick／FixedTick／LateTick 按 UNITY_EDITOR 注册，其他 Frame API 需实际消费者。
- OnGUI 可一帧执行多次；窗口、调试器、UI／Entity 和效果均有各自启用条件。
- 固定业务步为 50ms；产品 Update 壳每帧累积时间，业务步不是每渲染帧都执行。

## 产品直接逐帧入口

| 文件 | 候选方法与声明行 |
|---|---|
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductEntitySystems.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductEntitySystems.cs) | `Update` L51 |
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeSystem.cs) | `Update` L34 |
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs) | `Update` L41 |
| [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs) | `Update` L74 |
| [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs) | `Update` L26 |

## Game 流程／UI 容器／UGF→ET 桥

| 文件 | 候选方法与声明行 |
|---|---|
| [Unity/Assets/Scripts/Game/Container/UIWidgetContainer.cs](../Unity/Assets/Scripts/Game/Container/UIWidgetContainer.cs) | `OnUpdate` L266 |
| [Unity/Assets/Scripts/Game/Debugger/ToolDebuggerWindow.cs](../Unity/Assets/Scripts/Game/Debugger/ToolDebuggerWindow.cs) | `OnUpdate` L27 |
| [Unity/Assets/Scripts/Game/ET/Loader/Init.cs](../Unity/Assets/Scripts/Game/ET/Loader/Init.cs) | `Update` L17；`LateUpdate` L27 |
| [Unity/Assets/Scripts/Game/ET/Loader/Mono/GizmosDebug.cs](../Unity/Assets/Scripts/Game/ET/Loader/Mono/GizmosDebug.cs) | `OnDrawGizmos` L17 |
| [Unity/Assets/Scripts/Game/ET/Loader/UGF/Entity/ETMonoUGFEntity.cs](../Unity/Assets/Scripts/Game/ET/Loader/UGF/Entity/ETMonoUGFEntity.cs) | `OnUpdate` L71 |
| [Unity/Assets/Scripts/Game/ET/Loader/UGF/UGFSystemSingleton.cs](../Unity/Assets/Scripts/Game/ET/Loader/UGF/UGFSystemSingleton.cs) | `UGFUIWidgetOnUpdate` L337；`UGFUIFormOnUpdate` L709；`UGFEntityOnUpdate` L957 |
| [Unity/Assets/Scripts/Game/ET/Loader/UGF/UIForm/AETMonoUGFUIForm.cs](../Unity/Assets/Scripts/Game/ET/Loader/UGF/UIForm/AETMonoUGFUIForm.cs) | `OnUpdate` L94 |
| [Unity/Assets/Scripts/Game/ET/Loader/UGF/UIWidget/AETMonoUGFUIWidget.cs](../Unity/Assets/Scripts/Game/ET/Loader/UGF/UIWidget/AETMonoUGFUIWidget.cs) | `OnUpdate` L107 |
| [Unity/Assets/Scripts/Game/ET/Loader/UnityTimeNow.cs](../Unity/Assets/Scripts/Game/ET/Loader/UnityTimeNow.cs) | `Update` L25；`Update` L40 |
| [Unity/Assets/Scripts/Game/Procedure/ProcedureCheckResources.cs](../Unity/Assets/Scripts/Game/Procedure/ProcedureCheckResources.cs) | `OnUpdate` L26 |
| [Unity/Assets/Scripts/Game/Procedure/ProcedureCheckVersion.cs](../Unity/Assets/Scripts/Game/Procedure/ProcedureCheckVersion.cs) | `OnUpdate` L47 |
| [Unity/Assets/Scripts/Game/Procedure/ProcedureInitResources.cs](../Unity/Assets/Scripts/Game/Procedure/ProcedureInitResources.cs) | `OnUpdate` L27 |
| [Unity/Assets/Scripts/Game/Procedure/ProcedureLaunch.cs](../Unity/Assets/Scripts/Game/Procedure/ProcedureLaunch.cs) | `OnUpdate` L52 |
| [Unity/Assets/Scripts/Game/Procedure/ProcedureSplash.cs](../Unity/Assets/Scripts/Game/Procedure/ProcedureSplash.cs) | `OnUpdate` L16 |
| [Unity/Assets/Scripts/Game/Procedure/ProcedureUpdateResources.cs](../Unity/Assets/Scripts/Game/Procedure/ProcedureUpdateResources.cs) | `OnUpdate` L72 |
| [Unity/Assets/Scripts/Game/Procedure/ProcedureUpdateVersion.cs](../Unity/Assets/Scripts/Game/Procedure/ProcedureUpdateVersion.cs) | `OnUpdate` L39 |
| [Unity/Assets/Scripts/Game/Procedure/ProcedureVerifyResources.cs](../Unity/Assets/Scripts/Game/Procedure/ProcedureVerifyResources.cs) | `OnUpdate` L32 |
| [Unity/Assets/Scripts/Game/UI/Common/AExUIForm.cs](../Unity/Assets/Scripts/Game/UI/Common/AExUIForm.cs) | `OnUpdate` L151 |
| [Unity/Assets/Scripts/Game/UI/Common/AExUIWidget.cs](../Unity/Assets/Scripts/Game/UI/Common/AExUIWidget.cs) | `OnUpdate` L314 |
| [Unity/Assets/Scripts/Game/UI/Common/AUIWidget.cs](../Unity/Assets/Scripts/Game/UI/Common/AUIWidget.cs) | `OnUpdate` L204 |

## ET 消息／示例／动态事件候选

| 文件 | 候选方法与声明行 |
|---|---|
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Client/LockStep/LSClientUpdaterSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Client/LockStep/LSClientUpdaterSystem.cs) | `Update` L18 |
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Client/LockStep/LSReplayUpdaterSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Client/LockStep/LSReplayUpdaterSystem.cs) | `Update` L16 |
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Client/Test/TestComponentSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Client/Test/TestComponentSystem.cs) | `Update` L17 |
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/LockStep/Room/LSServerUpdaterSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/LockStep/Room/LSServerUpdaterSystem.cs) | `Update` L17 |
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Module/Message/ProcessOuterSenderSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Module/Message/ProcessOuterSenderSystem.cs) | `Update` L35 |
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Module/Router/RouterComponentSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Module/Router/RouterComponentSystem.cs) | `Update` L31 |
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/LockStep/LSInputComponentSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/LockStep/LSInputComponentSystem.cs) | `LSUpdate` L18 |
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/LockStep/RoomSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/LockStep/RoomSystem.cs) | `Update` L34 |
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Module/Message/NetComponentSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Module/Message/NetComponentSystem.cs) | `Update` L41 |
| [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Demo/Opera/OperaComponentSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Demo/Opera/OperaComponentSystem.cs) | `Update` L19 |
| [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Demo/Unit/AnimatorComponentSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Demo/Unit/AnimatorComponentSystem.cs) | `Update` L49 |
| [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/LockStep/Camera/LSCameraComponentSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/LockStep/Camera/LSCameraComponentSystem.cs) | `LateUpdate` L19 |
| [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/LockStep/LSAnimatorComponentSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/LockStep/LSAnimatorComponentSystem.cs) | `Update` L49 |
| [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/LockStep/LSOperaComponentSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/LockStep/LSOperaComponentSystem.cs) | `Update` L18 |
| [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/LockStep/LSUnitViewSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/LockStep/LSUnitViewSystem.cs) | `Update` L31 |
| [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/LockStep/UI/UILSRoom/UIFormLSRoomComponentSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/LockStep/UI/UILSRoom/UIFormLSRoomComponentSystem.cs) | `UGFUIFormOnUpdate` L30 |
| [Unity/Assets/Scripts/Game/ET/Code/Model/Share/LockStep/LSEntitySystemSingleton.cs](../Unity/Assets/Scripts/Game/ET/Code/Model/Share/LockStep/LSEntitySystemSingleton.cs) | `LSUpdate` L102 |
| [Unity/Assets/Scripts/Game/ET/Code/Model/Share/LockStep/LSUpdater.cs](../Unity/Assets/Scripts/Game/ET/Code/Model/Share/LockStep/LSUpdater.cs) | `Update` L13 |
| [Unity/Assets/Scripts/Game/ET/Code/Model/Share/LockStep/LSWorld.cs](../Unity/Assets/Scripts/Game/ET/Code/Model/Share/LockStep/LSWorld.cs) | `Update` L67 |
| [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/DynamicEvent/DynamicEventComponent.cs](../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/DynamicEvent/DynamicEventComponent.cs) | `Update` L19 |
| [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Numeric/NumericComponent.cs](../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Numeric/NumericComponent.cs) | `Update` L75 |

## ET 与 DotNet 调度／消息／网络

| 文件 | 候选方法与声明行 |
|---|---|
| [DotNet/Loader/Init.cs](../DotNet/Loader/Init.cs) | `Update` L37；`LateUpdate` L43 |
| [DotNet/Loader/TimeNow.cs](../DotNet/Loader/TimeNow.cs) | `Update` L12 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/EntitySystem.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/EntitySystem.cs) | `Update` L38；`LateUpdate` L90 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Fiber.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Fiber.cs) | `Update` L69；`LateUpdate` L81；`FrameFinishUpdate` L103 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Actor/ProcessInnerSenderSystem.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Actor/ProcessInnerSenderSystem.cs) | `Update` L74 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/CoroutineLock/CoroutineLockComponent.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/CoroutineLock/CoroutineLockComponent.cs) | `Update` L16 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Timer/TimerComponent.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Timer/TimerComponent.cs) | `Update` L52 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/IKcpTransport.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/IKcpTransport.cs) | `Update` L68；`Update` L179 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/KChannel.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/KChannel.cs) | `Update` L202 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/KService.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/KService.cs) | `Update` L508 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/TService.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/TService.cs) | `Update` L178 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/UWService.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/UWService.cs) | `Update` L35 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/WService.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/WService.cs) | `Update` L51 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/ThreadSynchronizationContext.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/ThreadSynchronizationContext.cs) | `Update` L14 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/World/Module/Fiber/FiberManager.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/World/Module/Fiber/FiberManager.cs) | `Update` L40；`LateUpdate` L45 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/World/Module/Fiber/MainThreadScheduler.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/World/Module/Fiber/MainThreadScheduler.cs) | `Update` L30；`LateUpdate` L72 |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/World/Module/TimeInfo/TimeInfo.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/World/Module/TimeInfo/TimeInfo.cs) | `Update` L45 |

## UGF 模块与表现／资源辅助

| 文件 | 候选方法与声明行 |
|---|---|
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Base/EventPool/EventPool.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Base/EventPool/EventPool.cs) | `Update` L67 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Base/GameFrameworkEntry.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Base/GameFrameworkEntry.cs) | `Update` L25 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Base/TaskPool/TaskPool.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Base/TaskPool/TaskPool.cs) | `Update` L98 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/DataNode/DataNodeManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/DataNode/DataNodeManager.cs) | `Update` L47 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Debugger/DebuggerManager.DebuggerWindowGroup.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Debugger/DebuggerManager.DebuggerWindowGroup.cs) | `OnUpdate` L114 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Debugger/DebuggerManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Debugger/DebuggerManager.cs) | `Update` L70 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Download/DownloadManager.DownloadAgent.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Download/DownloadManager.DownloadAgent.cs) | `Update` L144 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Download/DownloadManager.DownloadCounter.DownloadCounterNode.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Download/DownloadManager.DownloadCounter.DownloadCounterNode.cs) | `Update` L46 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Download/DownloadManager.DownloadCounter.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Download/DownloadManager.DownloadCounter.cs) | `Update` L88 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Download/DownloadManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Download/DownloadManager.cs) | `Update` L221 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Entity/EntityManager.EntityGroup.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Entity/EntityManager.EntityGroup.cs) | `Update` L154 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Entity/EntityManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Entity/EntityManager.cs) | `Update` L162 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Event/EventManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Event/EventManager.cs) | `Update` L66 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/FileSystem/FileSystemManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/FileSystem/FileSystemManager.cs) | `Update` L60 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Fsm/Fsm.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Fsm/Fsm.cs) | `Update` L537 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Fsm/FsmManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Fsm/FsmManager.cs) | `Update` L58 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Fsm/FsmState.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Fsm/FsmState.cs) | `OnUpdate` L47 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Localization/LocalizationManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Localization/LocalizationManager.cs) | `Update` L158 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/ObjectPool/ObjectPoolManager.ObjectPool.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/ObjectPool/ObjectPoolManager.ObjectPool.cs) | `Update` L568 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/ObjectPool/ObjectPoolManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/ObjectPool/ObjectPoolManager.cs) | `Update` L64 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Procedure/ProcedureBase.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Procedure/ProcedureBase.cs) | `OnUpdate` L42 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Procedure/ProcedureManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Procedure/ProcedureManager.cs) | `Update` L79 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Resource/ResourceManager.ResourceLoader.LoadResourceAgent.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Resource/ResourceManager.ResourceLoader.LoadResourceAgent.cs) | `Update` L113 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Resource/ResourceManager.ResourceLoader.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Resource/ResourceManager.ResourceLoader.cs) | `Update` L223 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Resource/ResourceManager.ResourceUpdater.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Resource/ResourceManager.ResourceUpdater.cs) | `Update` L198 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Resource/ResourceManager.ResourceVerifier.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Resource/ResourceManager.ResourceVerifier.cs) | `Update` L62 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Resource/ResourceManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Resource/ResourceManager.cs) | `Update` L753 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Scene/SceneManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Scene/SceneManager.cs) | `Update` L158 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Setting/SettingManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Setting/SettingManager.cs) | `Update` L49 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/Sound/SoundManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/Sound/SoundManager.cs) | `Update` L125 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/UI/UIManager.UIGroup.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/UI/UIManager.UIGroup.cs) | `Update` L144 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/UI/UIManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/UI/UIManager.cs) | `Update` L211 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/WebRequest/WebRequestManager.WebRequestAgent.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/WebRequest/WebRequestManager.WebRequestAgent.cs) | `Update` L81 |
| [Unity/Assets/Scripts/Library/UGF/GameFramework/WebRequest/WebRequestManager.cs](../Unity/Assets/Scripts/Library/UGF/GameFramework/WebRequest/WebRequestManager.cs) | `Update` L145 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Runtime/AssetSet/AssetSetComponent.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Runtime/AssetSet/AssetSetComponent.cs) | `Update` L102 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Runtime/Resource/WebGLLoadResourceAgentHelper.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Runtime/Resource/WebGLLoadResourceAgentHelper.cs) | `Update` L406 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Base/BaseComponent.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Base/BaseComponent.cs) | `Update` L229 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Debugger/DebuggerComponent.ConsoleWindow.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Debugger/DebuggerComponent.ConsoleWindow.cs) | `OnUpdate` L250 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Debugger/DebuggerComponent.FpsCounter.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Debugger/DebuggerComponent.FpsCounter.cs) | `Update` L59 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Debugger/DebuggerComponent.ScrollableDebuggerWindowBase.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Debugger/DebuggerComponent.ScrollableDebuggerWindowBase.cs) | `OnUpdate` L37 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Debugger/DebuggerComponent.SettingsWindow.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Debugger/DebuggerComponent.SettingsWindow.cs) | `OnUpdate` L53 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Debugger/DebuggerComponent.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Debugger/DebuggerComponent.cs) | `Update` L238；`OnGUI` L243 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Download/UnityWebRequestDownloadAgentHelper.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Download/UnityWebRequestDownloadAgentHelper.cs) | `Update` L214 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Download/WWWDownloadAgentHelper.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Download/WWWDownloadAgentHelper.cs) | `Update` L199 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Entity/Entity.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Entity/Entity.cs) | `OnUpdate` L268 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Entity/EntityLogic.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Entity/EntityLogic.cs) | `OnUpdate` L189 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Resource/DefaultLoadResourceAgentHelper.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Resource/DefaultLoadResourceAgentHelper.cs) | `Update` L380 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Resource/EditorResourceComponent.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Resource/EditorResourceComponent.cs) | `Update` L576 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Resource/ResourceComponent.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Resource/ResourceComponent.cs) | `Update` L696 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Sound/DefaultSoundAgentHelper.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Sound/DefaultSoundAgentHelper.cs) | `Update` L380 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/UI/UIForm.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/UI/UIForm.cs) | `OnUpdate` L275 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/UI/UIFormLogic.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/UI/UIFormLogic.cs) | `OnUpdate` L185 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/WebRequest/UnityWebRequestAgentHelper.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/WebRequest/UnityWebRequestAgentHelper.cs) | `Update` L157 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/WebRequest/WWWWebRequestAgentHelper.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/WebRequest/WWWWebRequestAgentHelper.cs) | `Update` L143 |

## UXTool 表现与计时器（按启用状态）

| 文件 | 候选方法与声明行 |
|---|---|
| [Unity/Assets/Scripts/Library/UXTool/Runtime/Feature/UIBeginnerGuide/BeginnerGuideWidget/GuideGesture.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/Feature/UIBeginnerGuide/BeginnerGuideWidget/GuideGesture.cs) | `LateUpdate` L96 |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/Feature/UIBeginnerGuide/BeginnerGuideWidget/GuideHighLight.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/Feature/UIBeginnerGuide/BeginnerGuideWidget/GuideHighLight.cs) | `LateUpdate` L188 |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/Feature/UIBeginnerGuide/BeginnerGuideWidget/GuideTargetStroke.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/Feature/UIBeginnerGuide/BeginnerGuideWidget/GuideTargetStroke.cs) | `LateUpdate` L60 |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Common/GameTimer.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Common/GameTimer.cs) | `InnerFrameBeginFixedTick` L128；`InnerFrameBeginTick` L139；`InnerFrameEndTick` L150；`FrameBeginTick` L248；`FrameEndTick` L253；`FrameBeginFixedTick` L258；`Tick` L287；`FixedTick` L292；`LateTick` L297；`FrameBeginTick` L336；`FrameBeginFixedTick` L347；`FrameEndTick` L358 |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Components/ColorBlindnessEffect.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Components/ColorBlindnessEffect.cs) | `OnPostRender` L44 |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Components/LXRolling.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Components/LXRolling.cs) | `Update` L89 |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Components/UXRandomInitializer.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Components/UXRandomInitializer.cs) | `Update` L75 |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Components/UXRolling.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Components/UXRolling.cs) | `Update` L89 |

## 第三方 API（需实际调用者，不等于已接入）

| 文件 | 候选方法与声明行 |
|---|---|
| [Unity/Assets/Scripts/Library/ET/ThirdParty/Runtime/DotRecast/Detour.Crowd/DtCrowd.cs](../Unity/Assets/Scripts/Library/ET/ThirdParty/Runtime/DotRecast/Detour.Crowd/DtCrowd.cs) | `Update` L396 |
| [Unity/Assets/Scripts/Library/ET/ThirdParty/Runtime/DotRecast/Detour.Crowd/DtLocalBoundary.cs](../Unity/Assets/Scripts/Library/ET/ThirdParty/Runtime/DotRecast/Detour.Crowd/DtLocalBoundary.cs) | `Update` L95 |
| [Unity/Assets/Scripts/Library/ET/ThirdParty/Runtime/DotRecast/Detour.Crowd/DtPathQueue.cs](../Unity/Assets/Scripts/Library/ET/ThirdParty/Runtime/DotRecast/Detour.Crowd/DtPathQueue.cs) | `Update` L37 |
| [Unity/Assets/Scripts/Library/ET/ThirdParty/Runtime/DotRecast/Detour.Dynamic/DynamicNavMesh.cs](../Unity/Assets/Scripts/Library/ET/ThirdParty/Runtime/DotRecast/Detour.Dynamic/DynamicNavMesh.cs) | `Update` L121；`Update` L175 |
| [Unity/Assets/Scripts/Library/ET/ThirdParty/Runtime/DotRecast/Detour.TileCache/DtTileCache.cs](../Unity/Assets/Scripts/Library/ET/ThirdParty/Runtime/DotRecast/Detour.TileCache/DtTileCache.cs) | `Update` L477 |
| [Unity/Assets/Scripts/Library/ET/ThirdParty/Runtime/Kcp/Kcp.cs](../Unity/Assets/Scripts/Library/ET/ThirdParty/Runtime/Kcp/Kcp.cs) | `Update` L553 |

## 编辑器 Update／GUI 候选

| 文件 | 候选方法与声明行 |
|---|---|
| [Unity/Assets/Scripts/Game/ET/Editor/Build/BuildToolEditor.cs](../Unity/Assets/Scripts/Game/ET/Editor/Build/BuildToolEditor.cs) | `OnGUI` L25 |
| [Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/CodeCreatorEditor.cs](../Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/CodeCreatorEditor.cs) | `OnGUI` L48 |
| [Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/DefaultComponentSystemCodeCreator.cs](../Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/DefaultComponentSystemCodeCreator.cs) | `OnGUI` L32 |
| [Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/UGFEntityCodeCreator.cs](../Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/UGFEntityCodeCreator.cs) | `OnGUI` L21 |
| [Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/UIFormCodeCreator.cs](../Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/UIFormCodeCreator.cs) | `OnGUI` L26 |
| [Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/UIWidgetCodeCreator.cs](../Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/UIWidgetCodeCreator.cs) | `OnGUI` L21 |
| [Unity/Assets/Scripts/Game/ET/Editor/ServerCommandLine/ServerCommandLineEditor.cs](../Unity/Assets/Scripts/Game/ET/Editor/ServerCommandLine/ServerCommandLineEditor.cs) | `OnGUI` L36 |
| [Unity/Assets/Scripts/Game/Editor/Build/BuildToolEditor.cs](../Unity/Assets/Scripts/Game/Editor/Build/BuildToolEditor.cs) | `OnGUI` L40 |
| [Unity/Assets/Scripts/Game/Editor/GUI/ShowAllGUIStyleWindow.cs](../Unity/Assets/Scripts/Game/Editor/GUI/ShowAllGUIStyleWindow.cs) | `OnGUI` L19 |
| [Unity/Assets/Scripts/Game/Editor/Multiplayer/InstanceLaunchWindow.cs](../Unity/Assets/Scripts/Game/Editor/Multiplayer/InstanceLaunchWindow.cs) | `OnGUI` L25 |
| [Unity/Assets/Scripts/Library/FolderTag/Editor/FolderSettings.cs](../Unity/Assets/Scripts/Library/FolderTag/Editor/FolderSettings.cs) | `OnGUI` L170 |
| [Unity/Assets/Scripts/Library/SocoTool/Editor/ShaderVariantCollectionAddVariantWindow.cs](../Unity/Assets/Scripts/Library/SocoTool/Editor/ShaderVariantCollectionAddVariantWindow.cs) | `OnGUI` L77 |
| [Unity/Assets/Scripts/Library/SocoTool/Editor/ShaderVariantCollectionToolWindow.cs](../Unity/Assets/Scripts/Library/SocoTool/Editor/ShaderVariantCollectionToolWindow.cs) | `OnGUI` L243 |
| [Unity/Assets/Scripts/Library/SocoTool/Editor/ShaderVariantsStripperConditionWindow.cs](../Unity/Assets/Scripts/Library/SocoTool/Editor/ShaderVariantsStripperConditionWindow.cs) | `OnGUI` L27 |
| [Unity/Assets/Scripts/Library/SocoTool/Editor/ShaderVariantsStripperWindow.cs](../Unity/Assets/Scripts/Library/SocoTool/Editor/ShaderVariantsStripperWindow.cs) | `OnGUI` L89 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Editor/Build/VersionInfoDataDrawer.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Editor/Build/VersionInfoDataDrawer.cs) | `OnGUI` L10 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Editor/MergeFileToVFS/MergeAssetToVFSWindow.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Editor/MergeFileToVFS/MergeAssetToVFSWindow.cs) | `OnGUI` L193 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Editor/ResourceRule/ResourceRuleEditor.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Editor/ResourceRule/ResourceRuleEditor.cs) | `OnGUI` L117 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Editor/ResourceVersionAnalyzer/ResourceVersionAnalyzerEditor.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Editor/ResourceVersionAnalyzer/ResourceVersionAnalyzerEditor.cs) | `OnGUI` L119 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Editor/Setting/DefaultSettingEditor.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Editor/Setting/DefaultSettingEditor.cs) | `OnGUI` L46 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Editor/ResourceAnalyzer/ResourceAnalyzer.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Editor/ResourceAnalyzer/ResourceAnalyzer.cs) | `OnGUI` L96 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Editor/ResourceBuilder/ResourceBuilder.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Editor/ResourceBuilder/ResourceBuilder.cs) | `Update` L88；`OnGUI` L97 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Editor/ResourceEditor/ResourceEditor.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Editor/ResourceEditor/ResourceEditor.cs) | `OnGUI` L104 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Editor/ResourcePackBuilder/ResourcePackBuilder.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Editor/ResourcePackBuilder/ResourcePackBuilder.cs) | `Update` L67；`OnGUI` L71 |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Editor/ResourceSyncTools/ResourceSyncTools.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Editor/ResourceSyncTools/ResourceSyncTools.cs) | `OnGUI` L43 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Common/TableList/Editor/TF_TableList.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Common/TableList/Editor/TF_TableList.cs) | `Update` L33 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Feature/UIBeginnerGuideEditor/GamePad/UIBeginnerInputActionsDrawer.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Feature/UIBeginnerGuideEditor/GamePad/UIBeginnerInputActionsDrawer.cs) | `OnGUI` L12 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Feature/UIBeginnerGuideEditor/GamePad/UIBeginnerTimeAndKeysDrawer.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Feature/UIBeginnerGuideEditor/GamePad/UIBeginnerTimeAndKeysDrawer.cs) | `OnGUI` L10 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Feature/UIBeginnerGuideEditor/UIBeginnerDataEditor.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Feature/UIBeginnerGuideEditor/UIBeginnerDataEditor.cs) | `OnGUI` L36 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Feature/UIColor/UIColorConfigWindow.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Feature/UIColor/UIColorConfigWindow.cs) | `OnGUI` L52 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/ResourceRepairLogic/AnimationHierachyFixTool.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/ResourceRepairLogic/AnimationHierachyFixTool.cs) | `OnGUI` L67 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/ResourceRepairLogic/AnimationRenameFixTool.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/ResourceRepairLogic/AnimationRenameFixTool.cs) | `OnGUI` L77 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/SVN/SVNContextMenu.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/SVN/SVNContextMenu.cs) | `Update` L30 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/SVN/SVNLogic_Tortoise.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/SVN/SVNLogic_Tortoise.cs) | `Update` L50 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/UICheckerLogic/UIComponentCheck/UIComponentCheckWindow.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/UICheckerLogic/UIComponentCheck/UIComponentCheckWindow.cs) | `OnGUI` L168 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/SettingsEditor/HierarchyManagement/HierarchyManagementSettingWindow.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/SettingsEditor/HierarchyManagement/HierarchyManagementSettingWindow.cs) | `OnGUI` L32 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/SettingsEditor/HierarchyManagement/TagColorDrawer.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/SettingsEditor/HierarchyManagement/TagColorDrawer.cs) | `OnGUI` L9 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/SettingsEditor/UXBuilder/UXBuilderFormSettingStructDrawer.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/SettingsEditor/UXBuilder/UXBuilderFormSettingStructDrawer.cs) | `OnGUI` L17 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Custom/HierarchyManagement/HierarchyManagementWindow.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Custom/HierarchyManagement/HierarchyManagementWindow.cs) | `OnGUI` L141 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Custom/PrefabRepository/PrefabRecentWindow.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Custom/PrefabRepository/PrefabRecentWindow.cs) | `OnGUI` L291 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Custom/PrefabRepository/WidgetRepositoryWindow.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Custom/PrefabRepository/WidgetRepositoryWindow.cs) | `OnGUI` L500 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Custom/Preview/PreviewWindow.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Custom/Preview/PreviewWindow.cs) | `OnGUI` L60 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Editor/SceneView/ResolutionController.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Editor/SceneView/ResolutionController.cs) | `OnGUI` L91 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/_InHouse/Window_Custom/ReferenceFinder/ReferenceFinderWindow.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/_InHouse/Window_Custom/ReferenceFinder/ReferenceFinderWindow.cs) | `OnGUI` L300 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Attributes/Drawer/HideLabelAttributeDrawer.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Attributes/Drawer/HideLabelAttributeDrawer.cs) | `OnGUI` L10 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Attributes/Drawer/InfoBoxDecoratorDrawer.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Attributes/Drawer/InfoBoxDecoratorDrawer.cs) | `OnGUI` L14 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Attributes/Drawer/LabelTextAttributeDrawer.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Attributes/Drawer/LabelTextAttributeDrawer.cs) | `OnGUI` L10 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Attributes/Drawer/PropertyDrawerBase.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Attributes/Drawer/PropertyDrawerBase.cs) | `OnGUI` L8 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Attributes/Drawer/SpecialCasePropertyDrawerBase.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Attributes/Drawer/SpecialCasePropertyDrawerBase.cs) | `OnGUI` L10 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Inspector/InputSlotDrawer.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Inspector/InputSlotDrawer.cs) | `OnGUI` L18；`OnGUI` L46；`OnGUI` L140；`OnGUI` L191；`OnGUI` L233 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Inspector/UIStateAnimatorEditor.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Inspector/UIStateAnimatorEditor.cs) | `OnGUI` L75；`OnGUI` L162 |
| [Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Localization/Searchable/SearchablePopup.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Localization/Searchable/SearchablePopup.cs) | `OnGUI` L225 |

## 第三方编辑器 GUI（位于 Runtime，受编辑器条件限制）

| 文件 | 候选方法与声明行 |
|---|---|
| [Unity/Assets/Scripts/Library/SocoTool/Runtime/ShaderVariantsStripperCondition.AExistAndBNotExist.cs](../Unity/Assets/Scripts/Library/SocoTool/Runtime/ShaderVariantsStripperCondition.AExistAndBNotExist.cs) | `OnGUI` L47 |
| [Unity/Assets/Scripts/Library/SocoTool/Runtime/ShaderVariantsStripperCondition.HasKeywordCombination.cs](../Unity/Assets/Scripts/Library/SocoTool/Runtime/ShaderVariantsStripperCondition.HasKeywordCombination.cs) | `OnGUI` L113 |
| [Unity/Assets/Scripts/Library/SocoTool/Runtime/ShaderVariantsStripperCondition.MultiCondition.cs](../Unity/Assets/Scripts/Library/SocoTool/Runtime/ShaderVariantsStripperCondition.MultiCondition.cs) | `OnGUI` L114 |
| [Unity/Assets/Scripts/Library/SocoTool/Runtime/ShaderVariantsStripperCondition.PassName.cs](../Unity/Assets/Scripts/Library/SocoTool/Runtime/ShaderVariantsStripperCondition.PassName.cs) | `OnGUI` L55 |
| [Unity/Assets/Scripts/Library/SocoTool/Runtime/ShaderVariantsStripperCondition.PassTypeIs.cs](../Unity/Assets/Scripts/Library/SocoTool/Runtime/ShaderVariantsStripperCondition.PassTypeIs.cs) | `OnGUI` L28 |
| [Unity/Assets/Scripts/Library/SocoTool/Runtime/ShaderVariantsStripperCondition.ShaderName.cs](../Unity/Assets/Scripts/Library/SocoTool/Runtime/ShaderVariantsStripperCondition.ShaderName.cs) | `OnGUI` L55 |
| [Unity/Assets/Scripts/Library/SocoTool/Runtime/ShaderVariantsStripperCondition.True.cs](../Unity/Assets/Scripts/Library/SocoTool/Runtime/ShaderVariantsStripperCondition.True.cs) | `OnGUI` L26 |

## 测试辅助（不是产品逐帧入口）

| 文件 | 候选方法与声明行 |
|---|---|
| [Unity/Assets/Tests/Editor/ET/CoopCoreUiTests.cs](../Unity/Assets/Tests/Editor/ET/CoopCoreUiTests.cs) | `Tick` L95 |
| [Unity/Assets/Tests/Editor/ET/LocalMessageShutdownTests.cs](../Unity/Assets/Tests/Editor/ET/LocalMessageShutdownTests.cs) | `Update` L90；`Update` L107 |

## Editor／渲染事件注册点

下表列出 += 注册位置；解绑、是否启用和实际消费者另读源码。匿名注册也列入。

| 文件与行 | 注册／等待语句 |
|---|---|
| [Unity/Assets/Scripts/Game/ET/Editor/Helper/EditorLogHelper.cs](../Unity/Assets/Scripts/Game/ET/Editor/Helper/EditorLogHelper.cs) L12 | `EditorApplication.update += CheckCompolingFinish;` |
| [Unity/Assets/Scripts/Game/Editor/Tool/PackageUpdateTool.cs](../Unity/Assets/Scripts/Game/Editor/Tool/PackageUpdateTool.cs) L35 | `EditorApplication.update += PollRequests;` |
| [Unity/Assets/Scripts/Library/FolderTag/Editor/FolderSettings.cs](../Unity/Assets/Scripts/Library/FolderTag/Editor/FolderSettings.cs) L167 | `EditorApplication.projectWindowItemOnGUI += FoldersBrowser.DrawFolderMemos;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Feature/UIBeginnerGuideEditor/UIBeginnerGuideEditor.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Feature/UIBeginnerGuideEditor/UIBeginnerGuideEditor.cs) L84 | `EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Feature/UIBeginnerGuideEditor/UIBeginnerGuideEditor.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Feature/UIBeginnerGuideEditor/UIBeginnerGuideEditor.cs) L87 | `EditorApplication.update += UpdateTransform;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/EdgeSnapLineLogic.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/EdgeSnapLineLogic.cs) L48 | `EditorApplication.update += ListenMoving;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/EdgeSnapLineLogic.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/EdgeSnapLineLogic.cs) L52 | `EditorApplication.update += SnapToFinalPos;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/IntervalLineLogic.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/IntervalLineLogic.cs) L148 | `EditorApplication.update += ListenMoving;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/IntervalLineLogic.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/IntervalLineLogic.cs) L152 | `EditorApplication.update += SnapToFinalPos;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/IntervalLineLogic.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/IntervalLineLogic.cs) L604 | `SceneView.duringSceneGui += (SceneView sceneView) =>` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/IntervalLineLogic.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/IntervalLineLogic.cs) L615 | `EditorApplication.update += () =>` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/LocationLineLogic.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/LocationLineLogic.cs) L48 | `EditorApplication.update += Opened;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/LocationLineLogic.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/LocationLineLogic.cs) L50 | `EditorApplication.update += Judge3D;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/LocationLineLogic.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/LocationLineLogic.cs) L51 | `EditorApplication.update += UpdateLinesScreenViewPos;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/LocationLineLogic.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/LocationLineLogic.cs) L52 | `SceneView.duringSceneGui += OnSceneGUI;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/LocationLineLogic.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/LocationLineLogic.cs) L55 | `EditorApplication.update += SnapToLocationLine;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/LocationLineLogic.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/LocationLineLogic/LocationLineLogic.cs) L83 | `EditorApplication.update += SnapToFinalPos;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/QuickBackGround/QuickBackground.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/QuickBackGround/QuickBackground.cs) L157 | `EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/QuickBackGround/QuickBackground.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/QuickBackGround/QuickBackground.cs) L199 | `EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/UICheckerLogic/UIComponentCheck/UIComponentCheckWindow.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Logic/UICheckerLogic/UIComponentCheck/UIComponentCheckWindow.cs) L132 | `EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Custom/PrefabRepository/PrefabRecentWindow.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Custom/PrefabRepository/PrefabRecentWindow.cs) L67 | `EditorApplication.hierarchyWindowItemOnGUI += (int instanceID, Rect selectionRect) =>` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Custom/PrefabRepository/WidgetRepositoryWindow.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Custom/PrefabRepository/WidgetRepositoryWindow.cs) L91 | `EditorApplication.hierarchyWindowItemOnGUI += (int instanceID, Rect selectionRect) =>` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Editor/SceneView/SceneViewContextMenu.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Editor/SceneView/SceneViewContextMenu.cs) L26 | `SceneView.duringSceneGui += OnSceneGUI;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Editor/SceneView/SceneViewToolBar.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Editor/SceneView/SceneViewToolBar.cs) L37 | `EditorApplication.update += InitFunction;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Editor/SceneView/UXCustomSceneView.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/UXTools/Window_Editor/SceneView/UXCustomSceneView.cs) L26 | `SceneView.duringSceneGui += OnSceneGUI;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/_InHouse/Window_Custom/ReferenceFinder/ReferenceFinderWindow.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/_InHouse/Window_Custom/ReferenceFinder/ReferenceFinderWindow.cs) L93 | `EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/Tools/_InHouse/Window_Editor/ProjectWindow/SVNCustomProjectWindow.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/Tools/_InHouse/Window_Editor/ProjectWindow/SVNCustomProjectWindow.cs) L30 | `EditorApplication.projectWindowItemOnGUI += DrawSVNOperationIcon;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Inspector/UXScrollRectEditor.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Inspector/UXScrollRectEditor.cs) L20 | `EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;` |
| [Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Localization/Searchable/SearchablePopup.cs](../Unity/Assets/Scripts/Library/UXTool/Editor/UXGUI/Localization/Searchable/SearchablePopup.cs) L209 | `EditorApplication.update += Repaint;` |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Common/GameTimer.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Common/GameTimer.cs) L279 | `UnityEditor.EditorApplication.update += FixedTick;` |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Common/GameTimer.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Common/GameTimer.cs) L280 | `UnityEditor.EditorApplication.update += Tick;` |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Common/GameTimer.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Common/GameTimer.cs) L281 | `UnityEditor.EditorApplication.update += LateTick;` |

## 逐帧 timer／PlayerLoop／协程等待点

包含一次性等待与逐帧循环，以便区分。WaitForSeconds 不自动表示逐帧分配，完整启动循环见审查正文。

| 文件与行 | 注册／等待语句 |
|---|---|
| [Unity/Assets/Scripts/Game/Camera/CameraComponent.cs](../Unity/Assets/Scripts/Game/Camera/CameraComponent.cs) L38 | `yield return new WaitForEndOfFrame();` |
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Client/Demo/NetClient/Router/RouterHelper.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Client/Demo/NetClient/Router/RouterHelper.cs) L82 | `await timerComponent.WaitFrameAsync();` |
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Demo/Gate/C2G_LoginGateHandler.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Demo/Gate/C2G_LoginGateHandler.cs) L59 | `await fiber.WaitFrameFinish();` |
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Demo/Map/Transfer/TransferHelper.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Demo/Map/Transfer/TransferHelper.cs) L9 | `await unit.Fiber().WaitFrameFinish();` |
| [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Module/Move/MoveComponentSystem.cs](../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Module/Move/MoveComponentSystem.cs) L186 | `self.MoveTimer = self.Root().GetComponent<TimerComponent>().NewFrameTimer(TimerInvokeType.MoveTimer, self);` |
| [Unity/Assets/Scripts/Game/ET/Loader/ProductBootstrapBridge.cs](../Unity/Assets/Scripts/Game/ET/Loader/ProductBootstrapBridge.cs) L61 | `await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);` |
| [Unity/Assets/Scripts/Game/Platform/PlatformEditor.cs](../Unity/Assets/Scripts/Game/Platform/PlatformEditor.cs) L24 | `yield return new WaitForSeconds(0.5f);` |
| [Unity/Assets/Scripts/Game/UI/UGuiExtension/UGuiExtension.cs](../Unity/Assets/Scripts/Game/UI/UGuiExtension/UGuiExtension.cs) L26 | `yield return s_WaitForEndOfFrame;` |
| [Unity/Assets/Scripts/Game/UI/UGuiExtension/UGuiExtension.cs](../Unity/Assets/Scripts/Game/UI/UGuiExtension/UGuiExtension.cs) L40 | `yield return s_WaitForEndOfFrame;` |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Fiber.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Fiber.cs) L96 | `public async UniTask WaitFrameFinish()` |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Timer/TimerComponent.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Timer/TimerComponent.cs) L203 | `public static UniTask WaitFrameAsync(this TimerComponent self, CancellationToken token = default)` |
| [Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Timer/TimerComponent.cs](../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Timer/TimerComponent.cs) L275 | `public static long NewFrameTimer(this TimerComponent self, int type, object args)` |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Runtime/Awaitable/Awaitable.UniTask.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Runtime/Awaitable/Awaitable.UniTask.cs) L48 | `PlayerLoopHelper.AddAction(timing, result);` |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Procedure/ProcedureComponent.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Procedure/ProcedureComponent.cs) L103 | `yield return new WaitForEndOfFrame();` |
| [Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Sound/DefaultSoundAgentHelper.cs](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Sound/DefaultSoundAgentHelper.cs) L438 | `yield return new WaitForEndOfFrame();` |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/Feature/UIBeginnerGuide/BeginnerGuideManager/UIBeginnerGuideManager.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/Feature/UIBeginnerGuide/BeginnerGuideManager/UIBeginnerGuideManager.cs) L186 | `yield return new WaitForSeconds(duration);` |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/Feature/UIBeginnerGuide/BeginnerGuideWidget/GamePad/UIBeginnerGuideGamePad.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/Feature/UIBeginnerGuide/BeginnerGuideWidget/GamePad/UIBeginnerGuideGamePad.cs) L308 | `yield return new WaitForSeconds(time);` |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/Feature/UIBeginnerGuide/BeginnerGuideWidget/GuideGesture.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/Feature/UIBeginnerGuide/BeginnerGuideWidget/GuideGesture.cs) L251 | `yield return new WaitForSeconds(duration);` |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/Feature/UIBeginnerGuide/BeginnerGuideWidget/GuideGesture.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/Feature/UIBeginnerGuide/BeginnerGuideWidget/GuideGesture.cs) L262 | `yield return null;` |
| [Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Components/ColorBlindnessEffect.cs](../Unity/Assets/Scripts/Library/UXTool/Runtime/UXGUI/Components/ColorBlindnessEffect.cs) L55 | `yield return new WaitForEndOfFrame();` |

## 不叫 Update 的实际循环与间接产品链路

- DotNet/App/Program.cs 的 RunAsync 循环；ET/Core/Runtime/World/Module/Fiber/ThreadScheduler.cs 与 ThreadPoolScheduler.cs 的 Loop（调用同一 Fiber）。
- Awaitable.UniTask.cs 的 UniTaskConfiguredSource.MoveNext：PlayerLoop 轮询各 Awaitable helper；任务源已有池，完成后回收。
- MoveComponentSystem.MoveTimer.Run → MoveForward，由 NewFrameTimer 触发；不是做饭 Grid 移动实现。
- RestaurantRuntime.AdvanceRestaurantStep → AdvanceGridTick／AdvanceKitchenTransitions／AdvanceWork／AdvanceAutoWork／AdvanceBakeWork／AdvanceSupplyShipments，以及 AdvanceCustomers／AdvanceServes／AdvanceFrontWork／ScheduleCompanions；提交后走 GridBroadcast.CreateSnapshot／SendCommitted、ProductDelivery、ProcessInnerSender、GridClient.ApplyGridSnapshot 和投影观察事件。
- GridPresentation.Update → TryGridReady／RefreshConnectionRecovery／MapViewport／Board.Render／RefreshGridHud／ProcessGridPointer。RenderFrontPerson、Label、SetHudViewport、RefreshRestaurantPanels、SelectedTargetDescription／Details、Recipe／Supply／Renovation 等 HUD helper 是这些入口的间接工作。
- UGF 的 Resource helper 调用 UpdateUnityWebRequest／UpdateFileAssetBundleCreateRequest／UpdateBytesAssetBundleCreateRequest／UpdateAssetBundleRequest／UpdateAsyncOperation；有在途任务时才有实际工作。
- Editor GUI 继续调用各 Inspector／PropertyDrawer／TreeView、Hierarchy／Project／SceneView 绘制 helper；其渲染开销不能等同于 Player 稳态。

本索引保留声明与注册事实，不保存业务状态或运行日志，也不把静态发现冒充动态验收。
