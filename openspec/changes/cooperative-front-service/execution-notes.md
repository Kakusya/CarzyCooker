# Execution Notes

## Planning status

2026-10-13 新建前置联机案，承接原案前厅与跨关开发任务。仅规划，0/5，全部功能 NotRun；未运行测试或修改代码。

## Rigor and scope

主要风险是旧顾客目标误交付、并发工作多执行者、餐具复制和换关旧消息，影响本轮分数与厨房。用真实 KCP 的交付、工作、投影和关尾行为小集，加同 Editor 普通入口自然两关覆盖；原 Solo 客流／配方和核心传输已覆盖且未变时直接复用。

接受双 Editor、Player、LAN、联机装修／存档未验证，分别归后续范围。若本案改动实际影响核心请求复制或权限，则补那一项核心回归，不因进入新案重复全部套件。

## Acceptance mapping

下列为拟补行为范围，不是已运行用例。

| 任务／功能 | 拟用例或实际操作 | 结果 |
| --- | --- | --- |
| 1.1 交付／换客／拒绝与内部隔离 | CoopFrontWire | NotRun |
| 1.2 同版前厅投影／实际 HUD | CoopFrontProjection＋受管按钮 | NotRun |
| 2.1 单执行者／接续／原餐具 | CoopFrontWork | NotRun |
| 2.2 暂停／排空／零星／换关身份 | CoopFrontLevel | NotRun |
| 3.1 两端分工服务与自然两关 | 普通 Host＋同 Editor KCP 实操 | NotRun |

## Planning validation

2026-10-13：本案 `openspec validate cooperative-front-service --strict` 退出 0、valid；本轮 25 份规划及入口文档的 UTF-8、尾空白和 164 个本地引用检查通过。没有实施或运行功能，任务保持 0/5、NotRun。
