## Context

初始工作树已有旧工作流和 Claude 接入的删除，以及两份未跟踪原始资料。现存 Codex hooks/agents 和项目 skills 仍有接入残留。保留这些用户修改，旧知识只从 Git HEAD 只读提取，不恢复文件。

## Goals / Non-Goals

目标：建立只面向 Codex 的可发现入口、项目技能与源码支持的中文规格；保存未来产品设计状态。

非目标：玩法实施、底座重构、Editor 自动化、测试/构建/端口管线、产品验证、提交/推送。

## Decisions

1. AGENTS 只放约束和路由；Book 保留框架手册；Docs 分为 AI、Framework、Development、Product。主规格在 openspec/specs，变更记录在 changes。
2. 官方 CLI 用 npm 全局固定 1.14.1，官方 skills 不改模板；custom 工作流在 core 六项加 verify，仅刷新本项目 Codex。
3. 项目四个技能只描述核实存在的 API/路径；不自带新脚本或暗示能自动生成 prefab、编辑 Excel 或运行 Unity。
4. 先技能融入，再扫描全仓文本；源码索引与关键链路精读分开，扫描不是运行时验证。
5. 产品适用原文与完整候选表迁到本项目文档，删除新文档中的旧工程路径引用；两份原件保持。暂停命令、断线手持物、传输选型不代定。
6. 主基线直接从存量源码建立；ai-collaboration 用增量走验证/同步/归档。旧诊断/测试管线不搬入；只保留适用生命周期、生成、响应式和数据 owner 知识。

## Risks / Trade-offs

- 全局 profile/workflows 影响后续其他项目 init/update，现有文件不变；在安装说明中公开这一范围。
- 源码与手册存在差异：UGFEntity 输出名/路径、ID 开关、Luban 失败退出与 UIEntity overload。记录事实，只修文档，不隐含修代码。
- 原件历史链接保持失效，不用作现行入口；当前正式引用须可解析。
- CLI 格式通过只能证明文档结构；Unity、AOT、玩法、联网和导表均 NotRun。

## Migration Plan

入口/分层 → 固定 CLI 和 Codex workflows → 四技能 → 全仓扫描与基线 → 知识迁出 → 清接入/全局核对 → 验证和归档。没有 reset、恢复删除、清理素材或发布。

## Open Questions

产品待裁决集中在 Docs/Product/backlog.md，不影响本次文档迁移完成。具体新工具审批按根入口执行，本次不新增配套工具。
