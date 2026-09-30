# InputController/FocusRotationController —— 聚焦旋转

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：InputControllerBase.cs、Managers/{GlobalInputMgr,GlobalCameraMgr}

> 路径：`Assets/Scripts/InputController/FocusRotationController/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.InputController.FocusRotationController`

## 选文件
- `FocusRotationControl.cs` — 聚焦旋转：绕目标环绕 + 复位 + 自定义缩放

## 跨文件约定
- 本控制器**重写了 `ZoomHandle()`**（缩放不走 Cinemachine FOV）：改缩放改本文件，不要改基类（基类逻辑仍被第一/第三人称使用）。
- 切换聚焦目标用 `Transport(Transform)`，不要在外部直接改相机父子关系；复位用 `ResetPos()`（瞬移）/ `ResetPosSmooth()`（缓动）。
- 必须继承 `InputControllerBase`（注册名 = 类型名），俯仰夹取仍走基类 `TopClamp` / `BottomClamp`。
- 典型调用方：`Steps/Conditions/` 或 `Controllers/` 在进入「观察某元件」步骤时切换本控制器并配合 `Transport` 定位。
