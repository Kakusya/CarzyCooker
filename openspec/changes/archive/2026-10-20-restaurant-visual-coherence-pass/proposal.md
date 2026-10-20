# Proposal

## Why

现有项目已有可编辑 Blender 源、两套图集和角色逐帧动画，但局部风格、尺度和 HUD 图标覆盖未形成统一的成品观感。`MonoUIGridKitchen` 仍使用 21 个手持图标及对应范围判断，也需要与当前生成布局对齐。

## What Changes

- 先完成一个可玩的视觉样板：本人厨师、汤与吐司两条制作链、切配台/炉台/烤箱三类工位，以及配套手持/订单图标。
- 统一上述样板的视角、尺寸、光照、轮廓、手持挂点与容器内容；保留现有四方向八帧动画与真实状态驱动。
- 制定简短的资源样式/导出说明，保留可编辑源和两套图集的明确覆盖映射。样板外资源保留可用回退，不宣称全游戏美术重制。
- HUD 统一标题/正文/主要动作的视觉层次，沿布局案既定结构实现；修复图标仅覆盖前 21 个定义的硬编码，所有当前有效物品可正确取图或明确回退。
- 样板在真实游戏里由用户确认视觉方向，再进入其他章节和角色的后续美术批次；本案不承诺批量扩产。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-art`：增加有限样板一致性、完整图标映射和可再导出的交付要求。

## Impact

- 涉及 `Art/CarzyCooker`、`HandmadeBistro` 的源与导出脚本、Board/HUD 直接美术引用和非生成消费者；图集与 `ArtLayout`/`BistroLayout` 同次导出。
- 依赖 [HUD 布局](../2026-10-20-product-hud-readable-detail-layout/proposal.md) 的结构合同；与 [声画反馈](../2026-10-20-restaurant-feedback-audio-pass/proposal.md) 分工：本案管外观与资源，声画案管触发/音效。不开新 Entity 类型、不改占地/碰撞/配方或生产表。
- 自动化覆盖映射、状态、挂点与资源释放；真实截图和游玩用于确认辨识度。美术方向及商业质量由人判断，不能由 UTF 替代，见 [execution-notes.md](execution-notes.md)。
- 2026-10-20：后续已按用户实施授权实际复现并修复21图标限制，完成5/5任务及运行验收；用户明确接受有限样板方向，详见[执行记录](execution-notes.md)。没有扩产其他章节；已归档并获授权本地提交，未推送。
