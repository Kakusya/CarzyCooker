# Proposal

## Why

玩家看菜谱或前厅时容易错过后厨完工和货物到达。现有加工/物流结果消息已有可信提交事实，可提供可关闭的统一视觉提醒。

## What Changes

- 增加到货、加工完成、订单临期三类视觉提醒，链接原物品/工位、补货或具体订单查看入口。
- 默认开启；可按类关闭并在本机保留设置。订单临期首版采用剩余5个业务秒的可调默认值，不改变订单期限。
- 同一结果只提示一次；暂停冻结临期与显示业务寿命，首次进入/重连不把历史快照批量当作新事件，HUD重开不重复播放。
- 不自动暂停、取货、盛装或交付；不新增音频素材、通知平台、云端历史或额外传输。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-ui`：补充有限经营提醒、去重及显式查看合同。

## Impact

复用 [ProductOutcomeHandlers](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductOutcomeHandlers.cs)已发布的独立值事件及原 GridPresentation owner，不能仅轮询最后一条Outcome漏掉同一步多个结果。本机设置接入本批 [product-input-readability-settings](../2026-10-17-product-input-readability-settings/proposal.md)的原owner持久设置，不重复建库。风险/验收见 [execution-notes.md](execution-notes.md)。
