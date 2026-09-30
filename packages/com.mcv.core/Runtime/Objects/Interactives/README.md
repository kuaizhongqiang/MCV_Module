# Objects/Interactives —— 交互物基类与房间/任务页交互件层

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：Elements/README.md

> 路径：`Assets/Scripts/Objects/Interactives/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Objects.Interactives`

交互层的根：`InteractiveBase` 是所有可交互物体的统一基类（元件 `ElementObjBase` 继承它），负责注册、接收 8 类鼠标交互并分发到虚钩子；**框架不做任何高亮表现**（不为插件留抽象），宿主若要悬停高亮就订阅 `MoEnter` / `MoExit`。`RoomDynamic/` 与 `TaskObj/` 是房间动态装饰与任务页专用交互件，两目录暂无独立 README，脚本见下。

## 选文件

- `InteractiveBase.cs` — 可交互物基类：注册 + 8 类鼠标事件（悬停表现由宿主订阅 Mo* 自建）
- `Elements/` — 18 个元件 + 元件基类 + 端子/导线 → `Elements/README.md`
- `RoomDynamic/RoomTVShowObj.cs` — 电视/展示屏图片自动轮播（悬停暂停计时）
- `RoomDynamic/RoomMenuObj.cs` — 项目 HUD 展品标签；点击只发进项目请求
- `RoomDynamic/RoomShowObj.cs` — 展品漂浮 + 悬停惯性减速
- `TaskObj/StructureTaskObj.cs` — 结构页零件；悬停由 TaskStructureController 决定是否出浮动提示
- `TaskObj/InspectionElementPointObj.cs` — 待检测点；只报"哪个元件的哪个点"（点名）
- `TaskObj/InspectionProbeObj.cs` — 红/黑检测笔：平面拖拽、接触与吸附
- `TaskObj/InspectionLineObj.cs` — 巡检线，交给 `LineDraw` 生成管状网格
- `TaskObj/InspectionElementObj.cs` — 被检测元件：配对数据 + 动作态汇总
- `TaskObj/InspectionMultimeterObj.cs` — 万用表表体：按档位/接入/动作态出读数
- `TaskObj/InspectionMultimeterKnobObj.cs` — 万用表档位旋钮：拖拽旋转换档
- `TaskObj/InspectionSwitchObj.cs` — 元器件上可点击部件（试验按钮/手柄）
- `TaskObj/ElementCheckPointPairing.cs` — 检测点配对表与读数解析（规则唯一源）
- `TaskObj/InfoTaskObj.cs` — 信息展示物：自转 + 悬停减速

## 跨文件约定

- 交互方向单向：管理器（射线）→ `InvokeMo*()` → 本层事件 → 子类 `Mo*Event()` 虚钩子；子类只重写钩子，不主动发起交互。
- 交互件必须与拾取用 Collider 同一 GameObject，且不要挂在未激活节点下——未激活物不会注册进 `GlobalInteractiveMgr`，射线/表笔永远判不到。
- 拖拽要持续跟手时别只靠 `MoMove`/`MoUp`（它们只在射线仍命中本物体时派发）：跟手与「松手结束」都必须在 `Update` 里自查左键兜底（表笔/旋钮/可点击部件三处同款）。
- 本层脚本不碰 UI：只报名称与状态、只发事件，面板与操作记录由 Controller/面板侧订阅（如结构页悬停判定在 `TaskStructureController`）。
- 注册与派发在 `Managers/GlobalInteractiveMgr`，载荷 `Event/GlobalInteractionEventData`（对象池 `Get`/`Release`，用完必须归还）；步骤条件经 `EventBus<GlobalInteractionEventData>` 监听点击/按下。
