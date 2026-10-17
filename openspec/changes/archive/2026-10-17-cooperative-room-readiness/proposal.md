# Proposal

## Why

当前房间由Host确认后直接开餐厅，缺少成员对同一开局内容的准备表达。已确认要求全体当前连接成员准备，避免内容尚未对齐即开局。

## What Changes

- Host发布选定章节/菜单或来源记录的房间提案，所有当前已连接成员（含Host）对同一提案版本明确准备，可取消准备。
- 只有全体已准备且原基线/身份/开局条件有效，Host才可启动；Host不能强开。内容或成员加入/离开/失联/恢复绑定变化清除准备。
- 新房间或餐厅退役回房间后重新准备；进行中餐厅原加入/恢复不新增此门槛，Solo不受影响。
- 旧版本Ready/Start、重复请求及旧UI回调不作用新提案；准备不创建餐厅或提交存档。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `product-entry`：新增共享开局提案、成员准备与无强开门槛。

## Impact

扩展 [ProductRoomSystem](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRoomSystem.cs)、Match/Participant及原房间投影/入口。仍由原HostSession裁决及原本地/Session消息传播，不增加大厅服务；原房间启动UTF与普通入口须随新流程同步。
