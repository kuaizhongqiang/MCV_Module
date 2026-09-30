# Utils —— 全工程通用日志与调试呈现工具

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：Pool/README.md

> 路径：`Assets/Scripts/Utils/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Utils`

通用日志与调试呈现工具，被几乎所有层引用，自身几乎不依赖业务。

## 选文件

- `Log.cs` — 统一日志出口：级别 / 时间戳 / tag + 屏幕浮层双通道
- `LogOverlay.cs` — 屏幕调试浮层（`OnGUI`，F1–F5），由 `Log` 自动创建
- `ChnNameMap.cs` — 枚举 → 中文显示名映射（界面与操作记录文案）
- `Pool/` — 通用 GameObject 对象池工具 → `Pool/README.md`

## 跨文件约定

- 业务代码统一经 `Utils/Log`，不要自建日志封装；`Log` 是全工程唯一允许调用 `Debug.Log*` 的地方。
- 高频日志（每帧 / 每交互）用 `Log.Verbose`，发布构建保持 `VerboseEnabled = false`（`Warning` / `Error` 不受门控）。
- 本目录是全工程被引用方（`Managers` / `Controllers` / `UI` / `Objects` / `Steps` / `InputController`），自身不反向依赖业务；`LogOverlay` 的场景/任务上下文只从 `EventBus` 与 `GlobalUIMgr` 取。
