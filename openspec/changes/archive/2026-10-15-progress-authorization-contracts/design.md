# Design

## Context

目标文档已确认 Host≠Save Owner、授权分支和主动领取，但签发/验证/撤销、Profile 证明、关闭回执认证和文件故障边界明确未设计；当前只有本机 Solo repository。 动机见 [proposal.md](proposal.md)。

## Goals / Non-Goals

目标是形成可实施的权限/持久协议文档。本案没有运行行为变化，规格显式跳过；不实现或安装任何服务、密码学工具或新持久库。

## Decisions

### 2026-10-15 用户裁决与实施交接

用户明确选择“接受离线授权，按上述限制实现”。同平台玩家主动交换可验证授权文件，导入形成独立分支；没有中心账号服务，原 Owner 不可远程撤回已导入分支，不保证跨设备全局只领取一次。此前已授权完成全部 16 个 change，本案只落实其中的协议文档任务。

具体身份字段、Profile 私钥证明、公钥绑定、有限授权用途、结果签发、持久化 ACK、主动领取提交、normal-close receipt 及文件故障边界统一写在[离线合作进度协议](../../../../Docs/Product/offline-progress-protocol.md)，不在多个案内维护不同协议。运行时采用既有签名 API，密钥本地保管；不引入账号/撤销服务，不扩张成线上资产系统。

下文“尚未设计/须裁决”为建立本案时的背景；本次离线首版范围已满足决策前置。任意断电、中心服务和云同步属于范围外，不能作为下游迟迟不实施的门槛。协议 API 在实际文件/消息 UTF 中核对兼容性，文档本身不冒充其通过。

### 1. 协议交接

已确认产品语义不再重问：Host 不得因世界权威覆盖任意档，导入形成独立分支，正常关闭按各端实际持久回执判断。

### 2. 协议交接

需裁决的最小集：Profile 如何证明与绑定成员；授权载荷的签发者/验证者/撤销边界；关闭回执如何认证；临时发布/覆盖失败时保护旧档的承诺。

### 3. 协议交接

采用用户已确认的同平台、玩家主动文件交换、无中心服务的有限版本；以签名及本端公钥绑定证明身份，密钥留本地，撤销边界见离线协议，不能默认为明文自报身份。

### 4. 协议交接

输出协议字段、消息时序、错误码语义和提交点，继承原 ET sender 与 repository，不另建总线或证据包。

### 身份与消息审阅边界

现有树以 ProductRoot repository、ProductHost/Match/Participant、HostSession/HostPeer 和 ProductClient/ClientSession 为准。此案不创建任何新业务 Entity，下游拟新增结果/权限值只在实施案中落树。

协议稿须画清：谁签发与验证、Profile 如何绑定 Participant、何时冻结资格、HostSession 如何产生结果、本端 repository 何时持久化再 ACK、谁决定领取/写档与正常关闭、Match 终止后哪些纯值仍可离线使用。沿原 sender/Session/有序 MailBox，消息对象仍由原每跳调用者回收。

### 必须裁决后才能交接

本案就是决策前置；2026-10-15 离线授权首版已获明确确认，按上文协议交接下游持久化实现。下游仍须运行权限、失败保旧、幂等和关闭验证；未决的中心服务、云同步或任意断电不进入本轮实现。

## Risks / Trade-offs

风险是把未定义认证当成可用授权或扩大断电保证。使用少量成功/拒绝/重试/关闭序列逐项审阅和链接检查即可；本案纯文档，不添加 UTF。 具体覆盖见 [execution-notes.md](execution-notes.md)。

## Migration Plan

先记录用户明确决定，再同步产品目标正文及下游实施案；保留历史未决出处。不修改既有运行代码/源表/用户档，不提交、push 或归档。

## Sources

- [Docs/Product/specs/save-progress-claims/spec.md](../../../../Docs/Product/specs/save-progress-claims/spec.md)
- [Docs/Product/design.md](../../../../Docs/Product/design.md)
- [Docs/Product/backlog.md](../../../../Docs/Product/backlog.md)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveFile.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveFile.cs)
