# ResourceCollection 导出 SOP

## 目的

将当前启用的 ResourceRuleEditor_ET 规则刷新为 UGF `Unity/Assets/Res/Editor/Config/ResourceCollection.xml`，必要时通过既有 Optimize 调整资源。沿用参考“前置 → 操作 → 日志 → 输出 → diff”的顺序。

## 前置

1. Unity 打开本工程并编译完成，无本轮 CS 错误。先读 [启动与验证 SOP](Unity启动与验证SOP.md)。
2. 核对实际配置 `Unity/Assets/Res/Editor/Config/ResourceRuleEditor_ET.asset` 的 m_IsActivate；当前 ET 规则启用。
3. 输出为 `Unity/Assets/Res/Editor/Config/ResourceCollection.xml`。当前保留 ET 与 HybridCLR，不能按参考规则删除热更包。
4. 生成 DLL、导表与其他产物必须已按任务需求完成；缺源资源先报告，不以旧 XML 冒充新导出成功。

## 人工步骤

1. 打开 `Game Framework/Resource Tools/Resource Rule Editor`，确认 ET 的资源规则、目录、过滤、打包与分组。
2. 执行 `Game Framework/Resource Tools/Refresh Activate Resource Collection`；需要优化时执行 `Refresh Activate Resource Collection With Optimize`。
3. Console 应见 `Refresh ResourceCollection.xml success.`；CheckRule 或 SaveCollection 失败会抛异常。Warning 按内容判断，不能直接全部忽略。
4. 核对 XML 与 Git diff：新增/删除资产、资源名、分组和 packed/loadType；UI、Entity、Luban、HybridCLR 与 ET.Code 等实际所需资源必须保留。
5. 输出检查失败停止后续构建，修规则/资源来源再生成，不手改 XML 伪装结果。

## 自动化（Agent）

通过已接入的 CLI/Pipeline 发现实际 `menu` 等接口，按当前 schema 执行上述真实菜单；不是点击含 Build 的按钮替代刷新。执行需要属于已授权的资源变更任务。

没有参考的 ResourceCollectionExport.request/result 轮询文件、ResCollectionEditorConfig 配置或 Export ResourceCollection.xml 菜单。不会自动清除规则死引用；以当前 CheckRule/SaveCollection 行为和原错误为准，不补造成功标志。

## 验收清单

- 刷新完成并有真实成功日志，无未处理错误。
- XML 实际内容与规则及任务范围一致，diff 无意外删包，热更/AOT 所需资源完整。
- 编译、资源刷新、AssetBundle 构建、Player 构建分别报告；未执行构建写 NotRun。

源码：[ResourceRuleTool](../Unity/Assets/Scripts/Game/Editor/Tool/ResourceRuleTool.cs)、[ResourceRuleEditor](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Editor/ResourceRule/ResourceRuleEditor.cs)、[Utility](../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Editor/ResourceRule/ResourceRuleEditorUtility.cs)。构建链见 [一键打包](一键打包.md)。
