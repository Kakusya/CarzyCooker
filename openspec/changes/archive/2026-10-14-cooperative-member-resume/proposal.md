# Proposal

## Why

当前断线保留离线 Participant，但新连接只能创建新成员。用本次运行内的手动恢复补齐短暂断线后的继续体验，不承担账号或跨重启恢复。

## What Changes

- 加入成功后签发仅本轮有效的随机恢复凭证，Host 保存其所属成员关系，Client 运行 owner 持有。
- 意外失联后手动重连到原 Match，重新绑定同一 Participant 和新连接代次。
- 恢复当前关角色及完整投影后再开放输入；断线落物保留，手槽不复制恢复。
- 显式 Leave、运行关闭与凭证不符拒绝恢复；原公共 sender 继续服务其他成员。

目标模式：Host/Client；Host 与 Client 进程均保留。层次：五：完整合作与恢复；依赖按[整体路线](../../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不自动重连、持久凭证、跨进程重启、Host 迁移、联机存档、账号鉴权或公网安全产品化。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `product-session`：内存恢复凭证、手动重绑定与就绪隔离。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/ProductSession.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/ProductSession.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductClientSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductClientSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductPeerSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductPeerSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductAuthority.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductAuthority.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs)

恢复入口涉及冒认成员、重复绑定和迟到回复。用随机能力凭证、Host 单写提交及代表性真实 KCP 负例和失联恢复覆盖；不扩成跨设备身份系统。验证取舍与逐任务映射见 [execution-notes.md](execution-notes.md)。本轮已获三个 change 的实施授权；功能及 UTF 结果逐项回填 execution-notes。提交、归档和第二工作区版本维护仍需对应授权。

