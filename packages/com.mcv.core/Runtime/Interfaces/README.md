# Interfaces —— 跨层契约：只有接口与契约（无实现、无状态），把实现细节挡在框架之外

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：实现者分布在 `Objects/`、`Steps/`、`UI/` 与宿主 `Assets/`（`Adapters/`、`Editor/ContentProviders/`）；注册与查找方为 `Managers/GlobalControllerMgr` / `GlobalInputMgr` / `GlobalInteractiveMgr`；内容 AB 契约的命名与打包规约见 `Docs/design_ai/BundlePipeline.md`

> 路径：`Assets/Scripts/Interfaces/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Interfaces`

## 选文件
- `ICondition.cs` — 步骤条件契约：三阶段协程状态机
- `IController.cs` — UI 控制器契约：按名 1:1 绑定 View
- `IObj.cs` — 可交互物体契约：8 个鼠标事件与取组件
- `IElement.cs` — 元件 / 端子 / 导线交互契约
- `IStepPanels.cs` — 三类步骤面板最小契约（工具 / 信息 / 答题）
- `IUiEffect.cs` — UI 悬停 / 点击特效契约
- `IContentProvider.cs` — 内容 AB 流水线契约（Editor-only）

## 跨文件约定
- **接口保持「最小且稳定」**：新增接口前先确认不能用事件总线或现有契约表达。
- **不为插件设抽象**：高亮与视频播放曾各有一个"宿主注入"接口（`IHighlightService` / `UI/Tools/IVideoPlayer`），2026-09-30 已删除——框架不背插件包袱，宿主自己实现表现，框架只给 `InteractiveBase` 的 `MoEnter` / `MoExit` 这类通用事件。
- **契约变更要连带改实现与文档**：本目录改动会波及 `Objects` / `Steps` / `UI` / 宿主资产。
