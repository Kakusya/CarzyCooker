# Tasks

## 1. 目标与客流

- [x] 1.1 在伙伴核心模型交接后新增 CustomerFlowConfig／OrderConfig／TableConfig 并正常 Check／生成，创建 Customer／Order／桌目标及 Ask／Clear／Wash 完成接口；SoloCustomerTargets UTF 验证配置引用、迟到询问和目标效果一次提交，同步接口并交伙伴真实工作。
- [x] 1.2 接入有界 FIFO、同一地形真实入座、询问后开单与超时离开；SoloCustomerFlow UTF 验证满队列跳过、等待询问零惩罚、顺序／可达和未满足订单零分，同步客流样机取舍。

## 2. 交付与餐具

- [x] 2.1 接入类型化真实出餐路径、身份／位置／菜品／容器完整预检及一次消费计分；SoloCustomerServe UTF 验证两菜合法交付、错误菜／操作格、重复、同桌新客旧请求及客户端同版投影。
- [x] 2.2 接入厨房唯一餐具、Table／DirtyPool／CleanPool、卫生与有限容量；SoloCustomerUtensils UTF 验证用餐占桌、清桌释放、同 ItemId 洗净复用、脏碗拒绝与总量不增，同步餐具步骤及新增规则。

## 3. 收尾与完整链

- [x] 3.1 接入来客停止和实际顾客／任务排空；SoloCustomerDrain UTF 验证最后洗碗未完不结算、超时自然离开及暂停停钟，同步本轮已裁决人工自然收口。
- [x] 3.2 发现并执行 SoloCustomer 和实际受改变的物品／工作 UTF 小集，逐点记录；与伙伴交接完成后检查真实任务来源与效果，空前厅不算完整验收。
- [x] 3.3 普通 Launcher Solo 完成备料做汤→伙伴询问→交付→用餐离席→玩家清桌→伙伴洗碗→同碗取回，并检查零分订单超时及自然结算；同一运行可验证伙伴和流程，标明点位且不重复跑未变行为。
