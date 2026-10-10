## Why

用户已明确授权彻底移除旧编辑器接入，改用 Unity CLI 与项目内对应 Pipeline 包，并核实项目版本的 Editor。现有入口仍描述未接入 CLI，与新的决定冲突；需要同步实际依赖和协作约定。

## What Changes

- 删除旧接入包、Editor 命令和 meta、程序集引用、项目设置及专属宏；清除维护文档中的旧协议。
- 使用已安装 Unity CLI 1.0.0-beta.11，通过官方命令锁定 com.unity.pipeline 0.8.0-exp.1，按项目路径发现实例并核对实际命令。
- 核实 Editor 6000.3.18f1；本机已有有效安装 D:/MyUnityEditor/6000.3.18f1。若确实缺失才下载到用户指定 D:/UnityEditor，不复制或搬移已有安装。
- 同步 AGENTS、手册、项目技能中的能力描述、主规格和缺口记录；新增技能只提供逐项审批材料，不自动实现。

## Capabilities

### New Capabilities

### Modified Capabilities

- `editor-tools`: 编辑器自动化统一使用 Unity CLI/Pipeline，按项目路径发现，验证真实状态。
- `ai-collaboration`: 已授权 CLI 接入与尚待批准的技能改造分开，现有约束按最新意图更新。

## Impact

影响 Unity/Packages、Unity/Assets/Scripts/Game/Editor 的旧接入目录与 asmdef、Unity/ProjectSettings 中专属配置/宏，以及协作文档和技能文字。保留 ET 底座、HybridCLR、CodeBind、ExcelExporter 与服务端 Actor 桥；不实施做饭业务，不运行产品构建、Play 或玩法测试，不提交或推送。
