# Models —— 数据层：只描述数据结构与读写工具

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：`../README.md`（依赖方向）；子目录各自 README

> 路径：`Assets/Scripts/Models/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Models[.*]`

不含业务行为。数据两条链路：编辑期用 `DataSO` 编辑并导出 JSON；运行期 `JsonReaderWriter` 从 `Assets/StreamingAssets/Data/*.json` 读取。

## 选文件

- `DataBase.cs` — 数据模型公共基类：id、显示名与描述
- `DataSO.cs` — 数据编辑用 ScriptableObject 基类，把数据导出为 JSON
- `IDataExporter.cs` — 数据导出契约，供编辑器批量扫描全部数据 SO
- `SystemDataSO.cs` — 系统数据 SO，导出 `SystemData.json`
- `LanguageDataSO.cs` — 多语言数据 SO，导出 `LanguageData.json`
- `ProjectDataSO.cs` — 项目数据 SO，导出 `ProjectData.json`
- `UserDataSO.cs` — 用户数据 SO，导出 `UserData.json`
- `EnumAll.cs` — 全局枚举唯一集中地（按 `#region` 分组），供 JSON 与 Inspector
- `JsonReaderWriter.cs` — 静态 JSON 读写：编辑期写、运行期同步写、运行期异步读
- `StepAnimation.cs` — 单条步骤动画配置，供 `Steps/StepHandler` 精确控帧

## 跨文件约定

- **数据模型只读**：运行期不要改从 JSON 读出的对象（`ProjectData` 运行态字段标 `[NonSerialized]`）。唯一例外是**设置类** `SystemData.renderQuality`（走 `JsonReaderWriter.WriteRuntime`，PC/Editor 可写，WebGL 只留内存态）。
- **`currentTaskType` 是全项目「当前任务类型」唯一源**：读走 `GlobalDataMgr.GetCurrentTaskType()`，写只走 `SetCurrentTaskType()`（`GlobalUIMgr` 处理 `TaskTypeChangeEventData` 时调用）。
- **枚举一律登记在 `EnumAll.cs`**（按 `#region` 分区）；`[InspectorName("中文")]` 与枚举值都是 JSON 契约，必须逐字保持。
- **新增数据类型**：先加字段（保持旧字段兼容）→ 加/改 `DataSO` 导出 → 更新 JSON；模型类不写访问管理器的逻辑。
