# Models/Project —— 项目/菜单/任务/题目数据模型

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：`../README.md`（数据层只读约定）

> 路径：`Assets/Scripts/Models/Project/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Models.Project`

运行时业务**主数据源**：数据来自 `Assets/StreamingAssets/Data/ProjectData.json`（由 `ProjectDataSO` 从 Editor 导出），由 `Managers/GlobalDataMgr` 加载，供 `Controllers` / `UI/Panels` / `Steps` 消费。

## 选文件

- `ProjectData.cs` — 项目根容器：clip 列表 + 当前选择，聚合全部任务数据
- `QuestionData.cs` — 题库根：持 QuestionClip 列表（构造只填元信息）
- `StepContentData.cs` — 按 id 取用的步骤文字内容集（多态 StepContentBase 容器）
- `LineData.cs` — 接线数据空占位类，预留扩展
- `TaskDataConverter.cs` — TaskData 静态互转：按 TaskType 分派 JSON 导入导出

## 跨文件约定

- **任务顺序即流程顺序**：`ProjectClip.Tasks` 的返回顺序（Info → Structure → Principle → Inspection 四步在前，旧实验线在后）是 UI 与步骤流程的既定口径，改动要同步验证 `TaskListPanel` 的 Toggle 与任务切换。
- **多态/多分支必须同步**：新增任务类型要改枚举 + 全部 switch（`ProjectClip.Tasks` / `GetTaskData` / `TaskDataConverter`），否则条目被静默丢弃；新增文字类型 = 加枚举值 + 子类 + converter 分支，容器与消费方不用动。
- **构造器不得加默认条目**：Newtonsoft 是 append 语义，每次 JSON 往返都会翻倍；`TaskData.TaskActive` 必须 public 才会被序列化。
- **本目录保持纯数据**：不读 `GlobalDataMgr`、不发事件。`currentTaskType` 唯一源与读写入口见同级 `ProjectData.md`。
- 加载入口：`Managers/GlobalDataMgr`（`GetProjectClip` / `GetTaskData` / `GetMenuData` / `GetRootMenus` / `GetChildMenus`）。
