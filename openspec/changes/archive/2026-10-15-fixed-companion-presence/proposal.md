# Proposal

## Why

已有持续 Companion 和关内 CompanionRole/Growth，当前唯一配置名为“伙伴”；没有稳定可识别称呼、准备态聊天或完成工作后的适度正面反馈。

把已能工作的固定伙伴呈现为可识别的长期搭档，落实准备态可聊天而不增加管理负担。

## What Changes

- 为现有一位伙伴采用已确认称呼“小麦”，复用现有识别表现，不扩大人数。
- 准备态提供不改变数值的可选聊天；询问/洗碗完成及成长解锁有清楚适度反馈。
- 跨关和从成功档新开运行仍使用同一固定定义，工作进度与成长规则保持。

优先级：**P2**；建议顺序：**15**。前置：无新增 change 硬前置，复用已交付业务。优先级表示用户价值和依赖，不要求所有案串行。

范围外：不增加招募/换岗/工资/疲劳/好感维护，不新增剧情系统或额外成长能力；本案按已确认称呼展示，不新增伙伴。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `fixed-companions`：固定称呼、准备态只读聊天与工作正面反馈。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/FrontService.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/FrontService.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/CompanionSystems.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/CompanionSystems.cs)
- [Design/Excel/ET/Datas/Product/CompanionConfig.xlsx](../../../../Design/Excel/ET/Datas/Product/CompanionConfig.xlsx)
- [Docs/Product/design.md](../../../../Docs/Product/design.md)

实施交接：2026-10-15 已确认保留一位伙伴“小麦”，准备态聊天围绕备菜/合作/收店，真实工作后简短正面反馈。文案与识别表现可在该范围内编写，最终伙伴人数仍未确定。

低影响表现改动，主要是误触动作或旧回调重复反馈。使用受管视图/只读聊天和原成长行为小集，一次 Editor 跨关检查即可，不扩大伙伴规则测试。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

本案范围已确认并按后续“完成16案”授权实施，当前4/4任务及实际UTF结果见execution-notes；创建时仅规划的阶段已经结束。真机、物理LAN、Player/AOT与构建保持暂缓；未提交、push或归档。优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
