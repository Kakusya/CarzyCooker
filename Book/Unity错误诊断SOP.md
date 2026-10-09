# Unity 错误诊断 SOP

保留参考的有界去重、项目栈帧和错误次数；数据源改为当前 CLI/Pipeline Console。

## 提取当前错误

1. 发现并精确匹配 `<repo>/Unity`，核对实际 console/console_status 命令。
2. 读取 console_status 的 groundTruth、cursor/session 与 compile 标志，再读取当前 Console。单次测试使用开跑前游标，不清 Console。
3. 仅将 Error、Exception、Assert 当失败候选，按消息与来源去重，默认最多 5 项/每项 5 个项目栈帧；保留次数和 Assets/ET/Game 可定位帧。
4. 高频未知错误只输出候选规则和样例，不自动吞掉。修改压缩规则前增加回归，检查项目栈帧仍存在。
5. 报告原始消息/路径、当前覆盖范围和编译/测试状态。压缩是展示，不替代原始失败；reset/dropped 不解释为无错误。

PowerShell 即时读取（UTF-8，不另存 JSON）：

```powershell
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)
$env:PYTHONUTF8 = '1'
unity command console --project-path '<repo>/Unity' --format json |
    python .agents/skills/unity-error-extraction/scripts/compact_console.py
```

## 无法连接或启动失败

Pipeline 未加载、Safe Mode、许可或依赖解析异常时，用现有明确路径读取本次 Editor 原日志及后台 stdout/stderr。只看相关阶段，不扫 Library/Temp/历史目录或输出 token。Editor 进程存在不等于 Pipeline 就绪，不能以清缓存/反复重启掩盖错误。

重现 → 定位最近能采取动作的边界 → 修复源头 → 新运行验证。未知错误不吞；存量与本轮新增分别说明，不把历史失败改成通过。

技能：[unity-error-extraction](../.agents/skills/unity-error-extraction/SKILL.md)。工具回归：`python -m unittest discover -s Tools/AutoTesting/tests -v`。
