# Objects/Interactives —— 交互物基类层

读者：AI
类型：功能文档
权威：说明
状态：2026-09-30（元件/任务/房间业务层已移除，只保留基类）
依赖：Managers/GlobalInteractiveMgr、Event/GlobalInteractionEventData、Interfaces/IObj

> 路径：`Assets/Scripts/Objects/Interactives/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Objects.Interactives`

交互层的根：`InteractiveBase` 是所有可交互物体的统一基类，负责注册、接收 8 类鼠标交互并分发到虚钩子；**框架不做任何高亮表现**（不为插件留抽象），宿主若要悬停高亮就订阅 `MoEnter` / `MoExit`。

> 2026-09-30 清理：元件层 `Elements/`、任务对象 `TaskObj/`、漫游房间对象 `RoomDynamic/`（电视轮播 / HUD 展品标签 / 展品漂浮等）已随业务移除。

## 选文件

- `InteractiveBase.cs` — 可交互物基类：注册 + 8 类鼠标事件（悬停表现由宿主订阅 Mo* 自建）

## 跨文件约定

- 交互方向单向：管理器（射线）→ `InvokeMo*()` → 本层事件 → 子类 `Mo*Event()` 虚钩子；子类只重写钩子，不主动发起交互。
- 交互件必须与拾取用 Collider 同一 GameObject，且不要挂在未激活节点下——未激活物不会注册进 `GlobalInteractiveMgr`，射线永远判不到。
- 拖拽要持续跟手时别只靠 `MoMove`/`MoUp`（它们只在射线仍命中本物体时派发）：跟手与「松手结束」都必须在 `Update` 里自查左键兜底。
- 本层脚本不碰 UI：只报名称与状态、只发事件。
- 注册与派发在 `Managers/GlobalInteractiveMgr`，载荷 `Event/GlobalInteractionEventData`（对象池 `Get`/`Release`，用完必须归还）。
