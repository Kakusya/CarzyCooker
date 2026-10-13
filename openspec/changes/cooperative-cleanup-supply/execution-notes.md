# Execution Notes

## Planning status

2026-10-13 新建前置联机案，承接原案清理／供应网络适配与物资边界。仅规划，0/5，全部功能 NotRun。Solo 供应同期仍在实施，本案不引用其中间文件作为已验收结果。

## Rigor and scope

主要风险是满槽吞货、重复预留／生货、远端绕设施和跨关补满，后果局限于本轮资源。沿原物品与货物 owner，用真实 KCP 的设施、数量和生命周期小集加一次实际补货营业链覆盖；复用已交付且未变的 Solo 来源／包装／配方校验。

接受仅本机同 Editor 的阶段验证，不要求重新测全部原料平衡或长期网络排列。双 Editor 由原案验收，Player／LAN／联机存档未验证；若更改前置字段或布局规则，才补对应原用例。原始输出留工具原位，不创建证据副本。

## Acceptance mapping

名称仅为拟补范围；执行时发现真实用例、记录每项终态。

| 任务／功能 | 拟用例或实际操作 | 结果 |
| --- | --- | --- |
| 1.1 到位／分类／锁定／原容器 | CoopLogisticsCleanup | NotRun |
| 1.2 wire／可信身份／重复与旧关 | CoopLogisticsWire | NotRun |
| 2.1 争货／满槽／搬工位／包装数量 | CoopLogisticsItems | NotRun |
| 2.2 时钟／换关／失联／后加入／投影 | CoopLogisticsLifetime＋实际 HUD | NotRun |
| 3.1 耗尽补货制作交付／清理／在途换关 | 普通 Host＋同 Editor KCP 实操 | NotRun |

## Planning validation

2026-10-13：本案 `openspec validate cooperative-cleanup-supply --strict` 退出 0、valid；本轮 25 份规划及入口文档的 UTF-8、尾空白和 164 个本地引用检查通过。任务仍为 0/5、NotRun，不把格式通过当作补货链通过。
