# Tasks

已实现并按七项任务实际验收；相关 UTF 与普通 Launcher 保存、退出、选档恢复通过，详见 execution-notes。仅本机 PC Solo 主动新建，覆盖、授权和联机领取不属于本案；Player/AOT仍 NotRun。

## 1. 纯数据与文件提交

- [x] 1.1 新增版本化 Header/持续状态 DTO、保存内引用键与完整值校验；SoloSavePayload UTF 验证未知版本/平台/Owner/损坏/引用/容量拒绝，以及剩余汤、冻结加工、脏净、供应和装修数据，更新载荷说明。
- [x] 1.2 接入受控目录和单机 repository、新建暂存/Flush/不覆盖发布与 Header 幂等；在专属测试目录执行 SoloSaveRepository UTF，断言代表性写入失败/冲突不改旧文件、同结果/Profile仅新建一次，记录明确未覆盖的断电边界。

## 2. 运行边界与恢复

- [x] 2.1 接入本机稳定 Profile及轻量新开/继续选择，仅校验选中基线后建树；SoloSaveSelection UTF 验证浏览不实例化世界、非法/取消选择不创建半餐厅及未绑定档的内存 fixture 兼容，更新入口说明。
- [x] 2.2 在自然 Completed/装修锁定后捕获不可变结果，走结果 owner 的本地保存请求和服务步持久门槛；SoloSaveCommit UTF 验证零星、营业退出无结果、写入失败不换关、重试/响应丢失仅一次安装及关闭 UI 不丢已提交事实，同步消息路径。
- [x] 2.3 实施从完整载荷重建新 World/Level/运行 Id 与下一 Preparing，不注入初始物资；SoloSaveRestore UTF 通过实际文件往返验证唯一物品关系、剩余份数/加工冻结参数、货物进度和装修一次安装，旧命令不得写新运行。

## 3. UI 与真实重启验收

- [x] 3.1 接入受管选档与成功“保存并继续”反馈，必要时改 UI.xlsx/Prefab/CodeBind 源并正常生成；实际 UI UTF 验证加载中取消、关闭重开、失败重试及单次提交，明确首版只新建。
- [x] 3.2 发现并执行相关 SoloSave 与受初始化/Continue影响的小集；普通 Launcher 留剩菜/加工/在途货→自然结算/选装修→保存→退出后重新进入并选档，核对新身份、库存不翻倍和下一准备态，逐功能记录 execution-notes；Player/AOT仍 NotRun。
