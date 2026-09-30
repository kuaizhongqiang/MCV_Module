# InputController/CameraControl —— 相机动态背景

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：Managers/{GlobalCameraMgr,GlobalAssetsMgr}、Event/CoreEvent.cs、Assets/Editor/BuildTools/CameraBgBundleTools.cs

> 路径：`Assets/Scripts/InputController/CameraControl/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.InputController.CameraControl`

## 选文件
- `CameraBg.cs` — 相机背景网格：按 FOV/宽高比缩放 + 双纹理淡入淡出

## 跨文件约定
- **材质属性名是契约**：`_Texture_1` / `_Texture_2` / `_TexOrColor`（0=颜色、1=纹理）/ `_CutTex`，自定义 Shader 必须保留这四个属性。
- **`DelayInit` 第一条必须是 `yield return null`**：本组件由 `GlobalCameraMgr.GetCamera()` 实例化 `MainCamera` 时创建，同步取相机会无限递归爆栈。
- 缩放只在 FOV/宽高比/Z 变化时重算；加依赖项时一并加入 `Update` 的变化检测与缓存字段。
- 背景切换入口是 `CameraBgChangeEventData`（`GlobalCameraMgr` 转发），业务侧不要直接调本组件。
- 背景图走 AB 包（菜单 `MCV Build/CameraBg 背景图 AB`）：`BgPackageIds` 与 `CameraBgBundleTools.Ids` 必须同序，`sourceAsset` 必须留空。
