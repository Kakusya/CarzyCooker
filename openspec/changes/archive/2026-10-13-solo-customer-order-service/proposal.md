# Proposal

## Why

只有真实顾客产生订单、接收菜品并留下餐具，制作才形成完整营业流程。需要让桌号、顾客、订单和食物身份分开，避免重复出餐与无限干净碗。

## What Changes

- 单机来客采用配置节奏、有界 FIFO 排队、真实行走与有限桌位；询问工作完成后才开单。
- 汤和盘装吐司订单核对真实顾客、菜品与容器，交付一次消费／计分一次；放普通台面不算交付。
- 顾客用餐继续占桌，离席产生脏餐具；接入伙伴清桌与洗碗，干净餐具保留原身份并有限复用。
- 收尾停止来客，超时未满足订单计零、自然离开；必要前厅工作按真实进度排空。
- 不做特殊客群、货币经济、饮品贴单或联网；用户已授权完成九个 change，按 tasks 实施和验收。

## Capabilities

### New Capabilities

- `orders-front-service`：顾客、餐桌、订单交付与有限餐具回收。

### Modified Capabilities

无。

## Impact

依赖流程、固定伙伴和真实菜品链。新增 CustomerFlowConfig／OrderConfig／TableConfig，扩展物品餐具状态与位置、Level 子对象、消息和 Board 投影。厨房是碗的唯一 owner，顾客和任务只保存稳定引用。[执行说明](execution-notes.md) 定义一条完整 Solo 制作服务回收验收。
