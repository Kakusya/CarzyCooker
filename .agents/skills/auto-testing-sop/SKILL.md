---
name: auto-testing-sop
description: 使用 CarzyCooker Tools/AutoTesting 发现、执行已授权 UTF 回归、等待终态及诊断失败；空集和跳过不冒充通过，原始输出保留原位。
---

# 自动化测试 SOP

本 SOP 专用于编辑器内 EditMode/PlayMode 回归。独立 Player 构建和包体验收使用 [build-acceptance-sop](../build-acceptance-sop/SKILL.md)。

先读 [根入口](../../../AGENTS.md)、[自动化测试 SOP](../../../Book/自动化测试SOP.md) 和 [官方 unity-cli](../unity-cli/SKILL.md)。工具入口 [run.py](../../../Tools/AutoTesting/autotesting/run.py)，Python 3.10+ 标准库；流程正文只维护在 Book。

- 先发现真实用例，执行必须有当轮授权、明确模式与 filter；不存在的产品套件不虚构。
- 默认列测试；执行示例：`python Tools/AutoTesting/autotesting/run.py --run --mode playmode --filter_type testName --filter "<FullName>"`。
- 工程精确匹配，Editor ready、Play stopped、没有运行作业；繁忙拒绝，不自动 cancel/stop/清 Console。
- 解析真实终态，失败输出消息/堆栈；空集 NotRun、跳过 Skipped、Console 覆盖缺口报异常。超时作业可能仍运行，不重发。
- 修复后创建新运行，不续跑或修改失败结果；不保存证据包、状态副本或 hash，不把编译当测试通过。
