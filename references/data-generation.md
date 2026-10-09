# 生成链路约定

**非规格**；冲突以代码/表、进行中 change、已同步主规格及用户最新意图为准。**阅读 ≠ 授权**文中未做能力。

Excel、Proto、模板和配置是源，生成 C#/ID/JSON/bin/Localization 和 CodeBind *.Bind.cs 是派生结果；修改源或生成器，不打手工补丁。

## Luban

读取 Design/Excel/ET/luban.conf 的实际 active、targets 和 cmds。当前 ET=true，GameHot 配置工程已随分支移除；导出器中旧模式分支不等于本项目仍支持双模式。不要让并行任务写同一源输出。

Excel/Defines → Share.Tool 发现 active 工程 → 并行 Luban → 多输出复制 → UI/Entity/UIEntity/Scene/Sound ID → 本地化。复制目标可能清空，不混放人工维护文件。

工具已构建且本轮授权校验/导出时，以 Bin 为 cwd：

```powershell
Push-Location Bin
try {
    ./Tool.exe --AppType=ExcelExporter --Console=1 --Customs=Check,ShowCmd
    # 通过且已获导出授权后，执行：
    # ./Tool.exe --AppType=ExcelExporter --Console=1
} finally {
    Pop-Location
}
```

Check 不写产物且跳过本地化输出。缺工具说明前置，不把构建自动加入文档任务。目标与选项见 [Luban手册](../Book/Luban配置.md) 与 [规格](../openspec/specs/data-generation/spec.md)。

非 Check 的失败不会自动跳过复制/ID 派生，也没有回滚保证；复制目标只放该生成链路产物。更细的五目标与失败场景在主规格维护，不在此复制一份契约。

## 协议、本地化与绑定

- Design/Proto/*/proto.conf 选择 active/codeType/起始 Opcode/输出；Proto2CS 排序和消息顺序影响 Opcode。当前配置只有 ET 生成域，使用 ET/MemoryPack。工具保留 UGF 生成器不等于客户端仍有 GF Network packet 运行时；不恢复已删除模式。见 [Proto手册](../Book/Proto生成工具.md)。
- 本地化源 Design/Excel/Localization.xlsx，由 ExcelExporter.Localization.cs 生成资源与常量。见 [多语言](../Book/多语言.md)。
- CodeBind 在 prefab 上生成 Mono partial 引用；ET 骨架工具不会自动填表。STATE_CONTROLLER_CODE_BIND 按依赖配置启用，不由宏推断绑定完成。
- 检查源改动、生成 diff、重复 ID、资源名/分组与目标编译。Editor 刷新需环境和授权，未运行写 NotRun。禁止 SHA/hash 与状态/证据副本。
