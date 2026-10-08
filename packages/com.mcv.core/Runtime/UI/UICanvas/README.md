# UI/UICanvas —— 按状态划分的 Canvas 层

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：UI/CanvasBase.cs、UI/Panels/*、Managers/GlobalUIMgr、Models/EnumAll.cs

> 路径：`Assets/Scripts/UI/UICanvas/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.UI.UICanvas`

## 选文件
- `StartCanvas.cs` — 开始页：StartPanel；本期未设置过画质时弹画质面板
- `LoginCanvas.cs` — 登录页：LoginPanel
- `MenuCanvas.cs` — 菜单页：重建时 `GetPanel<MenuPanel>()`（面板仍是骨架，见 `../Panels/MenuPanel.md`）
- `ContentCanvas.cs` — 内容页：骨架（业务面板已移除，AI 开启时挂 AiDialogPanel）
- `RoamingCanvas.cs` — 漫游页：骨架（AI 开启时挂 AiDialogPanel）
- `LoadingCanvas.cs` — 常驻加载遮罩画布，不参与状态切换

## 跨文件约定
- **状态事件驱动**：`GlobalUIMgr` 监听 `SceneStateChangeEventData` / `TaskTypeChangeEventData` 后调**无参** `Rebuild()`；Canvas 不接收 state/taskType，任务类型自己读 `GlobalDataMgr.GetCurrentTaskType()`。
- **加载遮挡层必须住在 `LoadingCanvas` 上**（`IsPersistent` 画布被切换逻辑剔除），挂状态画布下会被连根拔掉并闪一下。
- **功能按钮差异集中在此**（`FunctionPanel` 及其 `SetFunctionBtnActive` 已随业务移除，新菜单 / 功能条重写时再定入口）。
- **新增状态**：`EnumAll.cs` 加 `SceneState` 值 → 新建 Canvas 子类 → `GlobalUIMgr` 状态映射同步。
