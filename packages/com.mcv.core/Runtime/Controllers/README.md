# Controllers —— MVC 的 Controller 层：把面板交互翻译成数据操作、场景切换与事件广播

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：`Managers/GlobalControllerMgr`（注册与查找）、`Managers/GlobalDataMgr`（数据，唯一源）、`Managers/GlobalAiMgr`（AI 会话）、`Event/CoreEvent.cs`（事件载荷）、`UI/PanelBase.BindController()` / `UI/RequireControllerAttribute`

> 路径：`Assets/Scripts/Controllers/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Controllers`

## 选文件
- `ControllerBase.cs` — 控制器基类：注册 / 按名绑定 View / 退注
- `AiDialogController.cs` — AI 对话调度：预热门控 + 流式回调重试
- `DialogController.cs` — 通用对话框：等收起动画播完再发结果
- `ContentFunctionController.cs` — 内容页外壳：AI 开关 + 返回二次确认
- `FunctionController.cs` — 底部功能条：七按钮事件与退出 / 返回决策
- `LoadingController.cs` — 加载遮罩编排：进度与最短显示时长
- `LoginController.cs` — 登录流程：校验输入并写用户数据
- `MenuController.cs` — 菜单入口：进项目 / 漫游 / 退出 / 成绩预览
- `RoamingFunctionController.cs` — 漫游页外壳：唤出 / 收起菜单弹层
- `ResultSummitController.cs` — 成绩预览：关闭与提交按钮回调
- `StartController.cs` — 开始界面：进入登录态（单向）
- `RenderQualityController.cs` — 画面质量：点选即确认并落盘 `renderQuality`
- `TaskListController.cs` — 任务列表：装配列表并随任务切换刷新
- `TaskPurposeController.cs` — 任务目的面板占位（TODO：装配目的内容）
- `TaskEquipmentController.cs` — 实验仪器面板占位（TODO：装配仪器内容）
- `TaskPrincipleController.cs` — 实验原理：解析 videoName 交面板播放
- `TaskLineConnectionController.cs` — 电路连接面板占位（TODO：装配连线 UI）
- `TaskTrainingController.cs` — 仿真实验面板占位（TODO：装配仿真内容）
- `TaskTestController.cs` — 小测验面板占位（TODO：装配题目）
- `TaskInspectionController.cs` — 检测页提示：悬停浮动框 + 吸附操作记录
- `TaskStructureController.cs` — 结构调度：三维分解动画装配与播放
- `TaskInfoController.cs` — 简介调度：图集 + 文案 + 器件模型
- `TaskExamController.cs` — 考核：抽题 / 乱序 / 判题 / 推进都在本层
- `StepProcessingController.cs` — 步骤处理空骨架；面板侧已有实现（TODO）
- `TipsController.cs` — 提示条调度：步骤提示 / 操作提示与自动收起
- `TitleController.cs` — 标题骨架：面板自读项目名，暂无调度
- `StepQuestionController.cs` — 步骤答题：按 id 取全局题库并判题
- `StepUIController.cs` — 步骤 UI 说明：分页正文与确认完成

## 跨文件约定
- **控制器不是 MonoBehaviour、不挂场景**：`GlobalControllerMgr.DelayInit` 按类型表统一创建并常驻（`1_Content/ControllerRoot` 下的 27 个实例已删除）。生命周期只有 `OnInit`（登记后一次）与 `OnDispose`（销毁前一次），没有 Awake/OnDestroy 可依赖。
- **协程一律经管理器**：控制器内用 `Run(...)` / `Halt(...)` / `HaltAll()`，宿主是 `GlobalControllerMgr`（DontDestroyOnLoad）。管理器会先停掉本控制器全部协程，再调 `OnDispose`。
- **时序**：登记发生在 `DelayInit`，早于 `1_Content` 加载，因此必然早于首个面板的 `Start → Bind`；这是"面板按名字/类型找得到控制器"的前提，不要挪动创建时机。
- **单向数据流**：`Controller → View`、`Controller → Global*Mgr`；View 不引用具体 Controller 类型（只发 C# 事件），Controller 不持有 Canvas。
- **控制器常驻、面板会重建**：每次绑定都重跑 `OnViewBound`，其中订阅必须**先清后加**；常驻订阅写 `OnInit`、退订写 `OnDispose`。
- **业务判断不写面板**：数据校验、跳转决策、对话框结果处理都在 Controller，面板只显示与抛事件。
- **参数归属**：视图表现参数放面板（如 `LoadingPanel.minShowDuration`）；业务行为参数留控制器，且因控制器不再序列化而降为常量（如 `TaskExamController.QuestionCount`）。
- **`DialogResultEvent` 按 `DialogId` 枚举认领**，而 Controller 常驻跨 Canvas —— 各发布方的对话框 Id 必须取值互不相同（取值即 `EnumAll.cs` 的 `DialogId`，编译期即可排除重复），否则会被多方同时认领。
- 新增面板用编辑器菜单 `MCV Editor/创建/UI Panel` 生成骨架（自动写 `[RequireController]` 与 TODO），再补 `OnViewBound` 业务。
