# UI/Panels —— 面板层（只做显示与交互采集，业务决策上抛 Controller）

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：UI/{UIBase,PanelBase,TaskPanelBase}、Controllers/、UI/{Tools,Components}、Models/{Project,System}

> 路径：`Assets/Scripts/UI/Panels/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.UI.Panels`

## 选文件
- `StartPanel.cs` — 欢迎/开始界面：抛 `OnStartRequested`
- `RenderQualityPanel.cs` — 画面质量三档 + 硬件信息与建议档位
- `LoginPanel.cs` — 登录：账号/密码 + 用户类型下拉与必填校验
- `MenuPanel.cs` — 菜单页/漫游弹层：器件按钮 + 漫游退出成绩入口
- `LoadingPanel.cs` — 加载遮罩：背景/文案/进度 + 呼吸文字
- `TitlePanel.cs` — 标题栏：项目中英文名，padding 滑入滑出
- `FunctionPanel.cs` — 底部功能按钮条 + switch 滑入滑出
- `TipsPanel.cs` — 提示条：步骤/操作两套双缓冲内容容器
- `DialogPanel.cs` — 通用对话框：标题正文 + 确认/取消
- `AiDialogPanel.cs` — AI 对话：气泡列表 + 输入发送（流式）
- `RoamingFunctionPanel.cs` — 漫游页外壳：标题版权条 + 返回按钮
- `ContentFunctionPanel.cs` — 内容页外壳：项目名 + AI/返回入口
- `ResultSummitPanel.cs` — 成绩预览：十一行标签值 + 提交/关闭
- `TaskListPanel.cs` — 步骤列表：装配启用任务并上报切换
- `TaskPurposePanel.cs` — 任务目的：标题+正文（其余任务面板模板）
- `TaskEquipmentPanel.cs` — 实验仪器：选中文案 + 讲解音频播报
- `TaskPrinciplePanel.cs` — 实验原理：只做原理视频播放与收起
- `TaskTestPanel.cs` — 小测验（占位）：题干选项答案快照
- `TaskTrainingPanel.cs` — 仿真实验（占位）：只写标题
- `TaskLineConnectionPanel.cs` — 电路连接（占位）：只写标题
- `TaskDefaultPanel.cs` — 默认任务面板：占位文本快照
- `TaskInspectionPanel.cs` — 检测页：跟随鼠标提示框 + 操作记录
- `TaskStructurePanel.cs` — 结构页：跟随鼠标提示框（TipsFloat）
- `TaskInfoPanel.cs` — 简介页：按序填图并隐藏多余图槽
- `TaskExamPanel.cs` — 考核：题目链展示与上报（不判题）
- `StepQuestionPanel.cs` — 步骤答题：一次一题，展示与上报
- `StepUIPanel.cs` — 步骤 UI 说明：分页翻页 + 确认完成
- `StepProcessingPanel.cs` — 步骤处理：生成按钮行并驱动显隐动画

## 跨文件约定
- **资源名 = 类名**：Prefab 必须在 `Assets/Prefabs/UI/Panels/`（包条目 id `ui_{类名}`，需跑 `MCV Build/UI prefab AB` 才进 UI 包），否则 `UIPrefabUtil.Get` 打 Error 且面板不出现。
- **绑定方式**：优先 `[RequireController(typeof(XxxController))]`；未标注的面板（`AiDialogPanel` / `MenuPanel` / `FunctionPanel` / `TaskListPanel` / `TitlePanel`）走 `XxxPanel → XxxController` 名字约定，改名时两边必须同步。
- **只抛事件，不做决策**：交互经 `public event Action<...>` 暴露，控制器在 `OnViewBound()` 里先退后订。
- **序列化引用必判空**：`Awake` 中缺关键组件时打 `Log.Error("需要手动挂载组件")` 并 `return`。
- **显隐动画**：覆写 `SetUIActive` / `SetUIActiveImmediately` 时必须保留 `m_TargetActive` 防抖，否则 alpha 0-1-0 抖动；需要跨状态保留的显示状态放 `Controller` / `Global*Mgr`。
