# UI —— 视图层基座（Canvas → Panel → Component 三层）

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：Managers/{GlobalUIMgr,GlobalControllerMgr}

> 路径：`Assets/Scripts/UI/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.UI[.*]`

## 选文件
- `UIBase.cs` — 最底层显示基类：CanvasGroup 淡入淡出与收起回调
- `CanvasBase.cs` — Canvas 层：注册 GlobalUIMgr 并清空重建面板
- `PanelBase.cs` — 面板层：Start 绑定 Controller 并管理自身组件
- `TaskPanelBase.cs` — 任务面板基类：只加 GetPanelContent 内容契约
- `ComponentBase.cs` — 组件层：向宿主面板注册，供按类型取用
- `RequireControllerAttribute.cs` — 面板到控制器的编译期强绑定特性
- `UICanvas/` — 按 `SceneState` 划分的 Canvas
- `Panels/` — 全部面板
- `Components/` — 原子组件：文本 / 输入框 / 视频
- `Tools/` — 解耦 UI 逻辑：视频 / 菜单 / 气泡 / 音效

## 跨文件约定
- 面板按类名从 UI 全局包（`UI/ui`，条目 id `ui_{类名}`）取件，走 `Tools/UIPrefabUtil.Get(类名)`；Prefab 文件名必须等于类名，落在 `Assets/Prefabs/UI/Panels/`（碎片在 `Fragments/`）；新增面板走菜单 `MCV Editor/创建/UI Panel`（自动写 `[RequireController]`），改完须重跑 `MCV Build/UI prefab AB`。
- 面板随 Canvas 重建，跨状态保留的显示状态必须放 `Global*Mgr` / `Controller`。
- 面板只抛事件不做决策；Controller 在 `OnViewBound()` 里先退后订。
- 自定义显隐动画必须保留目标状态防抖 `m_TargetActive`，否则 alpha 0-1-0 抖动。
- 多语言文案一律走 `UI/Components/TextComponent`。
- 文本 / 子物体状态变完后刷布局，一律走 `PanelBase.RequestLayoutRebuild`（底层 `Tools/UILayoutRebuilder`）：**不要**同帧 `LayoutRebuilder.ForceRebuildLayoutImmediate` —— TMP 形态下文本晚一帧才落到控件上（量到空文本），且父级 LayoutGroup 取的是子节点当前尺寸、必须自下而上刷。
- 取文本节点走 `TextComponent.NodeOf(字段, 组件)`：TMP 形态下 Legacy `Text` 字段是"假 null"，用它取 `.transform` / 判空会让整段逻辑静默失效。
