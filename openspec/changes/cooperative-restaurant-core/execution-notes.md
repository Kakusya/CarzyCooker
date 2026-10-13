# Execution Notes

## Planning status

2026-10-13 用户要求创建缺失的前置联机 change。本案从原 cooperative-restaurant-loopback 拆出核心开发范围；仅规划，0/7，全部功能 NotRun。没有业务代码、表、协议生成、Unity、监听或第二工作区操作。

## Rigor and scope

主要风险是 Host owner 切换、共享控制越权和旧消息写入新关，后果是局内物品／时间或身份错误。采用原 ET 队列与单写者，最小真实 KCP UTF 加一次受管入口两菜实操即可覆盖本案。复用未变 Solo 配方算法与 sender 生命周期证据，不重跑全仓。

明确接受同 Editor 验收边界；双 Editor 归原 loopback 案，Player／LAN／存档／身份恢复未验证。核心协议、owner 或 UI 改变时补对应受影响测试，出现具体晚到消息或重复物品再加局部用例。

## Acceptance mapping

用例名为拟补范围，不是已发现或执行的测试。每组可用一个有多项行为断言的用例覆盖，不要求按表行拆出固定数量。

| 任务／功能 | 拟用例或实际操作 | 结果 |
| --- | --- | --- |
| 1.1 owner／驱动／Solo／Client 边界 | CoopCoreOwnership | NotRun |
| 1.2 wire／本地路径／协议与答复 | CoopCoreWire | NotRun |
| 1.3 各共享控制与拒绝零改动 | CoopCorePermissions | NotRun |
| 2.1 两菜／锁定／末份与重放 | CoopCoreCooking | NotRun |
| 2.2 就绪／独立投影／旧关／UI | CoopCoreProjection＋实际 HUD 生命周期 | NotRun |
| 2.3 失联／Leave／整体退出 | CoopCoreExit | NotRun |
| 3.1 普通 Host 与真实 KCP 受管操作 | 两菜、布局、Buff、暂停恢复 | NotRun |

## Planning validation

2026-10-13：本案 `openspec validate cooperative-restaurant-core --strict` 退出 0、valid。四案与三个入口文档共 25 份 Markdown 的 UTF-8、尾空白及 164 个本地引用检查通过；全部任务保持未勾选。这些只证明规划文件有效，功能仍 NotRun。
