# UI/Panels —— 面板层（只做显示与交互采集，业务决策上抛 Controller）

读者：AI
类型：功能文档
权威：说明
状态：2026-10-08 增 `MenuPanel` 骨架（细节指向同级 `A.md`）
依赖：UI/{UIBase,PanelBase,TaskPanelBase}、Controllers/、UI/{Tools,Components}、Models/{Project,System}

> 路径：`Assets/Scripts/UI/Panels/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.UI.Panels`

## 选文件
- `StartPanel.cs` — 欢迎/开始界面：抛 `OnStartRequested`
- `RenderQualityPanel.cs` — 画面质量三档 + 硬件信息与建议档位
- `LoginPanel.cs` — 登录：账号/密码 + 用户类型下拉与必填校验（提示与游客隐藏逻辑未实现，见其 `.md`）
- `LoadingPanel.cs` — 加载遮罩：背景/文案/进度 + 呼吸文字
- `MenuPanel.cs` — 菜单页：骨架（只收集 `menuRoot` 下的按钮，显示与交互未接线）
- `DialogPanel.cs` — 通用对话框：正文 + 确认/取消（无标题节点）
- `AiDialogPanel.cs` — AI 对话：气泡列表 + 输入发送（流式）

> 2026-09-30 清理：功能条 / 标题 / 提示 / 内容页外壳 / 漫游外壳 / 成绩预览 / 任务 / 步骤等业务面板（`FunctionPanel`、`TitlePanel`、`TipsPanel`、`ContentFunctionPanel`、`RoamingFunctionPanel`、`ResultSummitPanel`、`Task*Panel`、`Step*Panel`）已移除；旧 `MenuPanel` 随该轮移除，**2026-10-08 由 `MCV Editor/创建/UI Panel` 重新生成骨架**（`Assets/Prefabs/UI/Panels/MenuPanel.prefab`，包条目 `ui_MenuPanel`）。

## 跨文件约定
- **资源名 = 类名**：Prefab 必须在 `Assets/Prefabs/UI/Panels/`（包条目 id `ui_{类名}`，需跑 `MCV Build/UI prefab AB` 才进 UI 包），否则 `UIPrefabUtil.Get` 打 Error 且面板不出现。
- **绑定方式**：优先 `[RequireController(typeof(XxxController))]`；未标注的面板（`AiDialogPanel`）走 `XxxPanel → XxxController` 名字约定，改名时两边必须同步。
- **只抛事件，不做决策**：交互经 `public event Action<...>` 暴露，控制器在 `OnViewBound()` 里先退后订。
- **序列化引用必判空**：`Awake` 中缺关键组件时打 `Log.Error("需要手动挂载组件")` 并 `return`。
- **显隐动画**：覆写 `SetUIActive` / `SetUIActiveImmediately` 时必须保留 `m_TargetActive` 防抖，否则 alpha 0-1-0 抖动；需要跨状态保留的显示状态放 `Controller` / `Global*Mgr`。
