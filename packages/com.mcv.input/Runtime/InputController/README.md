# InputController —— 底层操控层

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：Managers/{GlobalInputMgr,GlobalCameraMgr}、Event/CoreEvent.cs

> 路径：`Assets/Scripts/InputController/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.InputController[.*]`

## 选文件
- `InputControllerBase.cs` — 控制器基类：注册、俯仰夹取与滚轮 FOV 缩放
- `CameraControl/` — 相机动态背景网格
- `FirstPersonController/` — 第一人称控制器（含角色预制体）
- `ThirdPersonController/` — 第三人称控制器
- `FocusRotationController/` — 聚焦旋转控制器
- `Common/` — 公共组件与 InputSystem 资产

## 跨文件约定
- 控制器常驻并注册到 `GlobalInputMgr`；切换视角 = 切 `IsActive`，不要重复挂载同类。
- **协程等待而非 `Awake` 顺序**：`DelayInit` 里 `while (... == null) yield return null;` 等管理器，禁止 `Awake` 直连。
- 缩放改 Cinemachine 虚拟相机 `m_Lens.FieldOfView`，不要改 `Camera.fieldOfView`；子类设 `IsMoving` 触发复位。
- 输入统一用 InputSystem（资产 `Common/InputSystem/StarterAssets.inputactions`）；部分控制器沿用 Unity Starter Assets 样例，改造后统一继承 `InputControllerBase`。
