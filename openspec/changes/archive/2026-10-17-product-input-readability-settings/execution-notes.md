# 执行说明

本案对应十二方向第8项输入与阅读设置，并提供第5项提醒的本机开关；用户已要求全部实施。首版维持既有键鼠/主触点，不擅增手柄和重绑定。已接入独立文件/本地消息和原受管HUD，5项不同UTF有当前适用Passed结果，普通入口已完成，文档/规格按本案实际合同收尾。

## 严谨程度与取舍

主要风险为大字导致入口遮挡、拖动误点击及偏好写失败。默认/文件/旧格式用受控临时目录实测，交互用原输入入口和最小相关PlayMode UTF，另在普通540×960查看最大字号与真实拿放/拖动。现有手势判定和平台隔离未变证据可复用，但新倍率和布局应用必须跑；不凭截图代替输入，也不加全量性能门槛。真人偏好、设备实测和手柄保持未验证，具体可读性/误触问题触发对应补验。

## 功能映射

| 任务/功能 | 实际验证 | 结果 |
| --- | --- | --- |
| 1.1 持久设置 | `EntryPreferencesActualLocalMailboxFileLockRetryAndReopenPreserveSuccessBytes`真实本地消息/临时文件/锁失败保旧重试/重开/成功档字节不变；`EntryPreferencesActualLegacyMissingFieldsUnknownCorruptAndPlatformIsolation`旧缺项/未知/损坏/PC与Android目录隔离；下列UI旅程覆盖取消/明确恢复默认 | Passed |
| 2.1 显示与输入 | `EntryPreferencesActualSelectorSaveCancelGameInputAndPooledReopen`三字号及3种实际Pan比例、保存/取消/默认、实际InputSystem鼠标/主触点双轴拖动与单击、UI射线阻挡、暂停及HUD旧callback；最大字三顶部栏完整/标准恢复，帮助→设置取消恢复原正文 | Passed；测试仅新增对既有Unity.InputSystem的程序集引用，不增加输入框架 |
| 2.1 换关/新运行 | `EntryPreferencesActualLevelContinueAndNewRuntimeRetireOldDraftCallbacks`原实际Continue、明确离开后重建Runtime及新内存餐厅，旧设置回调不再保存或发业务命令 | Passed |
| 2.2 合作与恢复 | `EntryPreferencesActualKcpClientUsesOwnLocalMailboxAndCannotChangeHostSettings`真实Host/KCP各自Root本地消息/文件、Client不越权/不增业务命令、Host字节不变；实际Session失联/手动新绑定及池化后旧保存回调失效 | Passed |
| 2.2 普通入口 | 普通Launcher540×960主页保存大号/慢拖、草稿取消、Stop/Play重读仍34字号不叠加；原选档内存试玩长菜谱滚到底，265字符全部生成/正文高740且字号34；关闭后双轴拖动command0→0/cameraY7.111→6.516，拿碗/放回command1/2 Executed；原7个成功文件仍7，最后明确保存默认并恢复原GameView | Passed；截图已逐一查看，见下 |

## 实施与运行记录

新增ET-Client末尾ZZZZZZZZZZZZZZZLocalPreferences.proto，用既有Bin/Tool.exe --AppType=Proto2CS --Console=1生成Client/ClientServer；原181个opcode逐项比较不变，新增4个本地请求/响应未进远端wire白名单。新格式使用既有MemoryPack；3成员旧布局仅有Version/字号/拖动，读取后明确补提醒默认，不改旧文件。曾尝试VersionTolerant导致测试程序集缺Unsafe编译引用，已改用原项目普通MemoryPack及明确旧布局适配，不引入依赖或改原插件设置。Model数据类按既有Object约定，Deserialize使用实际MemoryPackHelper(Type,bytes,offset,count)接口。

最初核心2/2、选档/游戏/实际输入1/1、KCP隔离1/1 Passed。普通最大字号发现得分栏“未结算”截断；保留字号，将放大态顶部宽度调整为354/246/408逻辑像素，标准恢复408/300/300。新断言后只复验原UI单项1/1 Passed。帮助页返回需使原只读缓存重新显示，补回原正文断言后同一单项新运行1/1 Passed。换关/新运行与扩充实际断线恢复的KCP两项分别以category=ProductPreferencesLifecycle运行2/2 Passed。

最后把锁夹具改为允许读取、拒绝delete-sharing，确保失败发生于实际暂存写入后的File.Replace发布，保留旧文件且只清自有临时文件；补新repository读损坏文件不能继承池化旧偏好。仅重跑受变核心2项2/2 Passed。其余已过行为未变，复用上述适用结果；5个不同用例均有当前有效Passed，均exit0、Console覆盖完整，不把重复运行相加为新增用例。

本案3/3任务完成。已同步原操作帮助、玩家说明、根进度及product-preferences主规格3项要求；49份主规格严格验证通过，本案严格验证及6份Markdown/11个本地引用、UTF-8/尾随空白检查通过。仅保存提醒偏好，提醒事件本身留第5项继续实施，不把开关存在当作事件已交付。

普通截图保留Unity/Assets/Temp/pipeline-screenshots/preferences-largest-{entry,help,recipe,final}.png，header-preview.png为发现问题后的现场预览；编译后实际主页final截图再次确认354/246/408栏宽和列表底部10秒选项，UTF确认同一布局的游戏得分文字完整。最后经原按钮恢复标准/标准/全部提醒/5秒，成功档仍7份；自有Play停止，仅移除自建视图并恢复原index6，不保存视图设置。原始Pipeline输出保留Unity/Temp/pipeline_test_status.json及Unity/Logs/Business-Delivery-Editor.log，未复制或改写；未提交/推送/归档，设备与真人偏好保持未验证。

## 2026-10-17 归档与提交

用户已明确授权本批归档及本地提交。任务和原逐项运行结果保留，主规格已核对；归档后全量严格检查74/74通过（50份主规格、24案）。仅调整规划状态与相对引用，未因归档重跑未变行为。提交范围排除工作区既有Android启动/后台、LAN和HandmadeBistro改动；本机设置与记录元数据依赖的平台存储定义及文件读写保留。不推送，设备、正式平衡和真人体验仍按原未验证边界。
