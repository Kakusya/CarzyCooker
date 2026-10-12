# Proposal

## Why

长期目标要求备料、制作和交接；`grid-kitchen-prep-processing` 已完成并归档，当前只到番茄块与蛋液。接下来补齐已确认代表菜的煮汤与盛碗，让半成品变成真实成品，为后续订单和出餐提供实物。本轮按用户范围只规划单机。

## What Changes

- 承接备料 change 的真实 ItemId、容器与手工作业，增加煮锅、番茄蛋花汤定义和一条最小多输入自动配方；不重新实现切配或打蛋。
- 手持蛋液碗向台面锅倾倒内容，保留空碗；番茄块可直接加入锅。配方按实际投入识别，默认水不占实物，玩家在灶台显式启动。
- 锅内自动作业由 Solo 权威 Tick 推进；启动后不可取消，输入锁定，搬锅或本地角色退出不停止世界内已有作业。完成前后均不提前创建独立汤物品。
- 完成锅可按份盛入手持空碗，一次一份；最后一份消费原输入并复位同一锅，拒绝或争抢不重复产物、不部分扣份。盛好的碗可普通交接。
- 原受管地图／HUD 显示锅内容、煮制进度、完成与剩余份数，场景和 HUD 对应目标共用动作解析；提供独立的“拿锅”操作。
- **BREAKING**：扩展动作及批次消息，从实施时实际基线递增版本并正常生成，旧版明确拒绝；单机新增动作不自动开放 wire，联网另案。

## Capabilities

### New Capabilities

- `kitchen-batch-cooking`：临时格子厨房的多输入容器自动加工、可搬动批次和按份盛装。与前置 `kitchen-item-processing` 的备料／手工子集分开记录，复用其实物关系。

### Modified Capabilities

无。保留既有空间、成员与单槽合同；本轮新增的倾倒和盛装资格在上述能力中明确限定，不重写另一会话的 delta。

## Impact

- Model／Hotfix 的 `Share/Product/GridKitchen`、既有本地 product handler／投影及 ModelView／HotfixView 的托管 Board／HUD；继续使用四程序集与原 owner。
- 前置计划中的 `Design/Excel/ET/Datas/Product/GridItemConfig.xlsx`，新增 `GridAutoRecipeConfig.xlsx` 和对应 `__tables__.xlsx` 注册，沿用 ET Luban 五目标；`Design/Proto/ET-Client/ZZGridKitchen.proto` 及现有正常生成链。
- 复用 `ProductSessionTests` 与现有 AutoTesting，计划验收真实本地消息和普通 Launcher Solo。本轮不启动 Editor、导表、构建或执行功能测试。
- **实施依赖**：核对 [备料归档证据](../archive/2026-10-11-grid-kitchen-prep-processing/execution-notes.md) 与当前源码。源表、协议、Tick 和 HUD 都是共享文件，必须顺序交接。汤先落地再交正式餐厅流程；若实际顺序改变，按流程树迁移同一厨房，不能复制另一实现。
- 范围外：订单、顾客、伙伴、洗碗、供应、清空／取料完整动作集、正式 RestaurantRuntime／Level、全局暂停／重连、评分／存档、联机与 KCP、手机 Player／AOT、LAN／公网、第二工作区维护。
- 风险集中在实物关系、批次扣份与退出后的作业存活；采用最小相关 UTF 加真实输入验收，不扩大框架或全仓测试。探索依据、样机参数与验证取舍见 [execution-notes.md](execution-notes.md)。

提案初建授权仅建立 change；2026-10-12 用户后续明确要求完成九个 change，已按 apply 实施并取得本地 UTF 与普通 Launcher 制作链结果，详见 execution-notes。仍不自动同步主规格、归档、提交或发布。
