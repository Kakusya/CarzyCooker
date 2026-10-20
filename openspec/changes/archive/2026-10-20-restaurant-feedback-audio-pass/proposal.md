# Proposal

## Why

项目已有动画和视觉提醒，但本轮检查未找到产品玩法调用声音的链路。拿放、加工完成和交付缺少清楚的听觉层次，容易让玩家感觉按钮改变了数据，却没有产生有分量的动作。

## What Changes

- 为本人成功拿取、放置/入容器/分装、开始加工、实际加工完成、成功交付，以及本关自然完成提供短声画反馈。
- 成功反馈只消费当前已提交 outcome/阶段变化；接受请求、拒绝、重复快照、首装和重连基线不播放成功音。原拒绝说明继续工作。
- 复用原动画和 GF Sound，新增少量本项目制作的短 WAV、Sound 表注册及资源归属，不引入音频中间件、背景音乐、语音或持续循环声。
- 在既有界面提供独立“声音”设置，使用 GF 本机设置保存音效静音/音量；明确即时生效，不混入经营存档或原偏好草稿事务。
- 修改当前视觉提醒“不使用新音频”的条款，仅允许加工完成与本案共享一次音效；到货和临期仍为原可关闭视觉提醒。

## Capabilities

### New Capabilities

- `restaurant-feedback`：经营声画反馈的触发、去重、声音控制与生命周期。

### Modified Capabilities

- `restaurant-ui`：精确修订视觉提醒的音频边界，保留原类别开关和行为。

## Impact

- 使用 `ProductOutcomeHandlers` 已安装的客户端值、`GridPresentationComponent`、`MonoGridKitchenBoard.Bistro` 和 `Game.SoundExtension`；新增反馈显示组件由现有 ProductClient 持有，不新增业务 authority。
- 资产与表源涉及 `Art/CarzyCooker`、`Unity/Assets/Res/Sound`、`Design/Excel/ET/Datas/Game/Sound.xlsx`，沿 Luban、资源集合和 GF Sound 加载链。
- 依赖 [HUD 布局](../2026-10-20-product-hud-readable-detail-layout/proposal.md) 提供可访问的设置/反馈区域；不等待全量美术替换，也不改动作规则或传输协议。
- 主要风险是重放声音、异步加载后串场和音量过响；通过真实执行、拒绝/重连/暂停、owner 退出和实际试听验收。合成音能由我交付，审美与听感需要真人确认。见 [execution-notes.md](execution-notes.md)。
- 2026-10-20：后续已按用户实施授权完成5/5任务及对应运行验收；用户明确回复“已试听，可以接受”，详见[执行记录](execution-notes.md)。未验证专业混音或多设备校准；已归档并获授权本地提交，未推送。
