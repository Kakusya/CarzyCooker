# Design

## Context

见 [proposal.md](proposal.md)。原MonoUIGridKitchen在ConfigureHud/AddButton设置字号，GridPresentationSystem的ProcessGridPointer使用固定14像素拖动阈值并将位移交Board.Pan。保留原阈值，仅新增镜头拖动倍率；新本机设置横跨入口、选档与游戏HUD，因此需设计owner和持久边界。

## Goals / Non-Goals

**Goals:** 原输入与文字随本机设置应用、草稿取消/保存/默认清晰。

**Non-Goals:** 不新增输入设备、键位重绑、共享设置服务或Prefab结构改造；与已有长列表可读布局案按最终实现整合，不改变其任务。

## Decisions

### 本机owner和调用交接

```text
Fiber / Scene(ProductRoot)
  +-- Component ProcessInnerSender
  +-- Component product preference state / original local repository boundary
  +-- ProductEntryPresentation / SoloSavePresentation
  |     +-- UIComponent -> UIGridKitchen (entry phase)
  +-- Scene(ProductClient)
        +-- GridPresentationComponent (draft only)
        +-- UIComponent -> UIGridKitchen (game phase)
        +-- GFEntityComponent -> GridKitchenBoard (Pan)
```

设置类型在Model持具名enum及独立值；逻辑落Hotfix，Mono只应用字号/绑定。原Root持本机已提交值，使用原隔离repository的本地文件边界保存独立偏好文件，不写成功记录或云/联机载荷。没有repository时沿当前平台隔离根建立设置owner，不为了看设置创建餐厅或网络服务。

本机查询/保存走Root原sender和有序repository边界，沿现有Design/Proto/ET-Client本地repository消息源追加定义后Proto2CS生成，保留既有opcode，不手写生成消息/注册，不添加远端wire白名单。接收handler验证全体字段、暂存后发布并答复独立值；UI所属等待、消息输入/响应沿原finally及CallLocalValueAsync交接。await后检查EntityRef/epoch，失败保留原已提交参数，草稿只归当前Presentation。保存后通知原Root及当前ProductClient实际消费者应用，不使用SceneType.All广播，不以Event代替文件提交。无远端RPC。

生成器按Proto文件路径Ordinal排序后连续分配opcode，因此不能向排序居中的ZZZZZZZZSoloSave.proto插入消息。本案使用现有ET-Client目录末尾的新源ZZZZZZZZZZZZZZZLocalPreferences.proto，生成后逐项比较既有opcode值；后续新消息继续追加排序尾部，不靠手改生成常量保持编号。

标准/较大/大号倍率首版1/1.15/1.3，拖动慢/标准/快为0.7/1/1.4；均为可逆本机参数。保留14像素手势阈值和UI遮挡判定，拖动倍率只乘Pan位移，不能改变角色目标或将抬手重新识别为点击。字号参数应用于原文字及按钮最小/最大字号，需更新必要布局高度/滚动可用区，最大字号下仍保留关闭及返回；不缩字抵消用户选择。

提醒类别和3/5/10业务秒阈值存在同一设置值中；未实现提醒时开关可以保存但不得伪造已收到事件。首次文件缺失默认标准/提醒开启/5秒；兼容旧缺项默认，未知格式和损坏报可理解错误且不覆盖旧文件。HUD重开从原提交值重新应用，视图不能持第二份长期设置。

## Risks / Trade-offs

- [大字不可读或按钮遮挡] -> 普通540×960三个字号、长中文正文/动作列表及返回关闭真实检查；不把自动缩字视为通过。
- [灵敏度改变误触] -> 鼠标/主触点真实单击、双轴拖动、UI遮挡和暂停验证，原动作只发一次。
- [设置写入影响成功档] -> 受控临时目录真实锁失败、旧设置读取、跨owner重开及平台根隔离断言。

## Migration Plan

独立设置格式从v1开始，不改成功载荷。先接owner与文件，再接入口/HUD/原指针；最后复用原帮助文档说明新支持范围。撤回入口应保留用户偏好文件，不删除。具体验收见 [execution-notes.md](execution-notes.md)。
