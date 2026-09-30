# Objects —— 场景可交互物与低压电气元件的分层入口

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：Interactives/README.md、Tools/README.md

> 路径：`Assets/Scripts/Objects/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Objects[.*]`

场景可交互物体的分层入口；交互统一由 `Managers/GlobalInteractiveMgr` 的射线检测驱动。

## 选文件

- `ObjectBase.cs` — 场景对象最底层标识基类（无行为）
- `Interactives/` — 交互物基类层 → `Interactives/README.md`

> 2026-09-30 清理：元件层（`Interactives/Elements`）、任务对象（`Interactives/TaskObj`）、漫游房间对象（`Interactives/RoomDynamic`）与 `Tools/`（元件动画 / 连线绘制）已移除，只保留基类。

## 跨文件约定

- 交互方向单向：`GlobalInteractiveMgr` 射线 → `InvokeMo*()` → `Mo*Event` 虚钩子；对象侧不主动查询。
- 射线检测与派发在 `Managers/GlobalInteractiveMgr`（元件/连线注册表 `ElementManagerBase` 已随业务移除）。
- 场景对象不直接读 `Assets/StreamingAssets`，数据经 `Global*Mgr` 取。
- 「关掉一个物体给别的让路」要连碰撞体一起切（只切 `IsInteractable` 射线仍被挡）。
