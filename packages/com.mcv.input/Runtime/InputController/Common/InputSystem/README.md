# InputController/Common/InputSystem —— 输入状态容器与资产

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：使用方 FirstPersonController/、ThirdPersonController/

> 路径：`Assets/Scripts/InputController/Common/InputSystem/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.InputController.Common.InputSystem`

## 选文件
- `StarterAssetsInputs.cs` — 输入状态容器：move/look/jump/sprint + 光标锁
- `StarterAssets.inputactions` — 输入动作资产

## 跨文件约定
- `StarterAssetsInputs` 是唯一输入状态出口：控制器不要直接 `ReadValue`（会绕过 `analogMovement`、光标锁）；弹窗时放开 `cursorLocked`。
- 必须与 `PlayerInput` 配套挂载；缺驱动时状态恒为默认值（「角色不动」）。
- 改名或增删动作后必须同步回调与各控制器读取字段。
