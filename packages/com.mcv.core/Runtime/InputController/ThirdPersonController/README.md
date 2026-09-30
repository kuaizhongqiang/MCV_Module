# InputController/ThirdPersonController —— 第三人称操控

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：../InputControllerBase.cs、../Common/InputSystem/、Global{Input,Camera}Mgr

> 路径：`Assets/Scripts/InputController/ThirdPersonController/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.InputController.ThirdPersonController`

## 选文件
- `ThirdPersonController.cs` — 第三人称：移动/转向/跳跃/落地 + 传送

## 跨文件约定
- 必须继承 `InputControllerBase`（注册名 = 类型名）；同场景别放两个同类。
- 输入读 `Common/InputSystem/StarterAssetsInputs` 与 `StarterAssets.inputactions`。
- 视角与混合由 Cinemachine 负责，不要直接驱动 `Camera.transform`。
