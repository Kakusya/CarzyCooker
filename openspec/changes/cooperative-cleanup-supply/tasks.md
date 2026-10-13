# Tasks

仅规划，全部 NotRun。实施前取得核心／前厅、Solo 清理及 Solo 供应的实际交付与相同源协议；供应未交付时不勾选任何依赖其完成的功能。

## 1. 设施与物流入口

- [ ] 1.1 接入 FillWater／DrainLiquid／DiscardSolid 的网络及原 UI 值调用，保留完整预期内容／布局；CoopLogisticsCleanup 真实 KCP 验证到位才消费、错设施保物、熟汤引用收尾、卫生／加工锁定及共址洗碗占用，同步设施消息说明。
- [ ] 1.2 接入 SupplyCommand、ReceiveSupply、TakeSupplyPortion 的外部路径和独立请求复制，必要协议正常生成；CoopLogisticsWire 验证合法 Client 可采购、内部／旧关拒绝、重复预留一次及原答复对应，同步实际白名单。

## 2. 公共物资与投影

- [ ] 2.1 接续原供应裁决并补远端边界；CoopLogisticsItems 验证两端争货只生一包、满槽保货、移动后的真实收货点、拆份身份、内容失效拒绝及丢弃不退款，保留原 Solo 配额／规则证据。
- [ ] 2.2 完成供应／包装同版投影与实际 HUD；CoopLogisticsLifetime 验证暂停／Completed 冻结、在途换关、采购提交后失联仍可接收、后加入当前状态及 DTO 回收独立，更新物流 owner 与 UI 边界。

## 3. 补货后继续经营

- [ ] 3.1 普通 Host＋同 Editor 真实 KCP 受管入口耗用初始原料后采购、等待、接收、拆份、制作并交付，实际处理剩液和空包装且保留在途货物换关；逐功能记录结果并更新联机进度，不替原 loopback 双 Editor 任务勾选。
