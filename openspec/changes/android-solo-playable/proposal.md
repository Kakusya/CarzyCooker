# Proposal

## Why

当前保存入口明确拒绝移动平台，竖屏 Editor 图也不能证明触控和 AOT。最后在真实 Android 设备补完整单机交付与平台边界。

## What Changes

- 接入 Android 本平台 Profile／保存目录与格式标识，PC 与 Android 文件互不混用。
- 真实触控支持点击、拖动与界面输入；后台自动暂停，回前台仍暂停并手动继续。
- 在已选单机功能的 Editor 基线上构建并验证资源、热更／AOT和终关／检查点恢复。

目标模式：Android Solo。层次：六：最终设备验收；依赖按[整体路线](../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不做 Android 联机、跨平台互通、跨设备存档、自动后台保存或任意断电保证；不关闭 HybridCLR 规避问题。

## Capabilities

### New Capabilities

- `android-solo`：手机输入、后台暂停与本平台检查点恢复。

### Modified Capabilities

无。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)
- [Unity/Assets/Scripts/Game/HybridCLR/HybridCLRHelper.cs](../../../Unity/Assets/Scripts/Game/HybridCLR/HybridCLRHelper.cs)
- [Book/打包与包体验收SOP.md](../../../Book/打包与包体验收SOP.md)

主要是触控误触、后台继续计时和文件平台错误。Editor 验证可控平台合同，真机验证触控／切后台／杀进程后读最后成功档；不以 Editor 成功替代。验证取舍与逐任务映射见 [execution-notes.md](execution-notes.md)。本轮仅授权规划，功能／UTF／构建均 NotRun；不执行提交、归档或第二工作区操作。

