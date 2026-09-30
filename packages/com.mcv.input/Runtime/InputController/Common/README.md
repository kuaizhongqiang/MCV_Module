# InputController/Common —— 输入层公共组件

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：使用方 FirstPersonController/、ThirdPersonController/

> 路径：`Assets/Scripts/InputController/Common/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：全局（`BasicRigidBodyPush`）与 `MCV_Module.InputController.Common.InputSystem`

## 选文件
- `BasicRigidBodyPush.cs` — 角色推动刚体：按层施水平冲量
- `InputSystem/` — 输入状态容器与资产

## 跨文件约定
- 输入状态统一从 `StarterAssetsInputs` 读。
- 不想被推动的物体（如固定机柜）放到 `pushLayers` 之外。
