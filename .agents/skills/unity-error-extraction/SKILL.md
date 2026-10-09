---
name: unity-error-extraction
description: 提取本工程当前 Unity Console 的 Error、Exception、Assert，保留次数和项目栈帧，压缩刷屏错误并诊断单次测试失败。
---

# Unity 错误提取

读 [错误诊断 SOP](../../../Book/Unity错误诊断SOP.md) 与 [官方 unity-cli](../unity-cli/SKILL.md)。仅处理精确匹配本工程的当前 Console，或用户指定的单次原始失败输出；不扫描全部历史目录。

使用 [compact_console.py](scripts/compact_console.py) 适配当前 Pipeline entries；核心 [error_message_compactor.py](scripts/error_message_compactor.py) 保留参考实现，默认 max_entries=5、max_stack_frames=5、dedupe=message_source。只将 Error、Exception、Assert 作为失败候选；保留可定位项目栈帧，并报告 count。

不要运行时自动增加排除规则或吞掉未知错误。遇到高频未知签名时输出候选规则和固定样例；修改默认规则前补充样例、运行 reducer 回归并确认项目栈帧仍被保留。

压缩只用于展示，不替代原始输出或测试结论；console reset/dropped 不代表零错误。Pipeline 未加载时按 SOP 查看已知原 Editor 日志，不调用旧读取协议或创建诊断快照。
