# Objects/Tools —— 元件动作动画与导线网格绘制

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：无

> 路径：`Assets/Scripts/Objects/Tools/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Objects.Tools`

元件侧的表现工具：动作动画（旋转 / 转动 / 位移）与导线管状网格，只负责「怎么动、怎么画」。

## 选文件

- `ElementAnimation.cs` — 三类动作动画：旋转档位 / 电机转动 / 开关位移
- `LineDraw.cs` — 导线管状网格的生成、更新与释放（`LineDrawData`）

## 跨文件约定

- 动画类不是 MonoBehaviour，由元件/管理器创建并驱动（宿主传 `ElementObjBase`），不要依赖 MonoBehaviour 生命周期。
- 动作档位与开合位移改序列化数据（`ElementRotationStruct` / `moveLimitation`），不要在代码里写死角度。
- `LineDrawData` 是连线外观契约：临时线与常驻线共用同一份（序列化在 `ElementManagerBase` 上），改动影响所有连线外观。
- `UpdateLine` 会被拖线高频调用，调用方须自行做「位移超阈值才重建」的降频（`ElementManagerBase` 已实现）。
- **清线必须走 `LineDraw.ReleaseLine`**：直接丢引用或 Destroy 会在网格注册表里留下脏项（机制见 `LineDraw.md`）。
