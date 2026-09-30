# InputController/FirstPersonController —— 第一人称操控

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：../InputControllerBase.cs、../Common/InputSystem/、Global{Input,Camera}Mgr

> 路径：`Assets/Scripts/InputController/FirstPersonController/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.InputController.FirstPersonController`

## 选文件
- `FirstPersonController.cs` — 第一人称：移动/视角/跳跃/落地 + 传送

## 跨文件约定
- 必须继承 `InputControllerBase`，否则不会被注册、无法 `GetController<T>()` 切换；切 `IsActive`，不要挂第二个同类。
- 输入读 `Common/InputSystem/StarterAssetsInputs` 与 `StarterAssets.inputactions`，不要另起一套。
- 参数改脚本或 `First Person Controller.prefab`，不要在场景里覆盖后再改脚本默认值（会两套参数）。
