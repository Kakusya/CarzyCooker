# Proposal

## Why

2026-10-20 普通 Launcher 的入口、选档和厨房观察显示：入口中心说明与控制区争空间，厨房被长提示和动作区挤压。已有分组仍缺少稳定的空间层次；本案把原长详情布局案扩展为入口与经营 HUD 的共同布局基础。

## What Changes

- 入口页采用标题、主内容、固定导航三个区域；经营页采用紧凑状态栏、地图、当前目标与动作区，详情按需覆盖并可返回。入口不再依靠结算文字区堆放全部内容。
- 保留原案的长中文正文、独立动作列表滚动、固定返回/关闭、有效滚动位置恢复以及生命周期要求。
- 在 540×960、1920×1080 和一个非零安全区检查布局；覆盖最大已有字号。正常经营态厨房完整可见，查看前厅保留纵向拖动。
- 沿现有 UI1101、UIComponent、CodeBind 和输入消费链迁移全部消费者；合法功能仍可访问，Solo/Host/Client 权限不变。
- 本案只负责结构与绑定；入口推荐路线、动作推荐规则、音频和美术内容分别由后续五案负责，不改表驱动业务、不造 UI 框架或结构 runner。

## Capabilities

### New Capabilities

无。复用已有能力边界。

### Modified Capabilities

- `restaurant-ui`：保留原长详情合同，增加入口/经营空间分区和代表视口的可操作约束。

## Impact

- 受影响：原 `UIGridKitchen.prefab`、`MonoUIGridKitchen`、入口/选档/厨房 Presentation 的布局消费者；程序集仍为 ModelView/HotfixView。UI1101 ID、业务 owner、消息和存档格式保持。
- 本案是 [首局路径](../2026-10-20-product-first-session-path/proposal.md)、[情境动作](../2026-10-20-kitchen-contextual-actions/proposal.md) 和美术 HUD 整合的结构前置；已有各类 inspector 仍接入相同详情容器，不在本案实施那些页面。
- 结构错误可能遮挡输入或使旧绑定失效，因此核对 Prefab/绑定/消费者并做代表视口真实操作及相关 UTF；不扩为全设备或全仓回归。风险、任务映射和能力边界见 [execution-notes.md](execution-notes.md)。

2026-10-20：后续已按用户实施授权完成5/5任务及对应运行验收，详见[执行记录](execution-notes.md)。本案与主规格已同步；已归档并获授权本地提交，未推送。
