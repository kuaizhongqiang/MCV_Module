# 漫游系统

## 概述

第一人称漫游，**FPS 模式（WASD 移动 + 鼠标视角）**，通过 `CharacterController` 实现碰撞检测。用户在 3D 实验场景中自由漫游，鼠标点击场景物体时，由 `GlobalInteractiveMgr` 统一射线检测命中 `InteractiveBase` 子类物体，触发 Controller 决策后通过 `GlobalUiMgr` 打开 Panel。**严格遵循 MCV**。

## FPS 移动方案

| 维度 | 方案 | 说明 |
|------|------|------|
| 水平移动 | WASD 键盘输入 | W/S 前后，A/D 左右平移 |
| 垂直移动 | 重力 + 跳跃（Space） | CharacterController 内置重力 |
| 视角旋转 | 鼠标 X/Y 轴 | Yaw 自由度 + Pitch（-90°~+90°） |
| 速度配置 | Inspector 可配 | 移动速度、旋转灵敏度 |

### 鼠标锁定

- 进入漫游场景：`Cursor.lockState = Locked`，鼠标隐藏
- 打开 Panel：`Cursor.lockState = None`，鼠标恢复
- 关闭 Panel：重新锁定
- Esc 临时解锁

### 碰撞检测

CharacterController 与场景 Collider 碰撞。碰撞层和交互层通过 Layer 分离——`Obstacle` 层仅碰撞，`Interactive` 层同时碰撞和射线检测。

**代码参考**：直接复用 Tuanjie 项目 `FirstPersonController.cs` + 对应预制体。

## 射线检测与交互流程

```
鼠标点击 → GlobalInteractiveMgr.Update() 统一射线检测
         → Physics.Raycast（Camera.main.ScreenPointToRay，每帧一次）
         → 命中 InteractiveBase 子类物体
         → IObj.OnMouseClick 触发 UnityEvent
         → Controller 接收事件 → 查询 GlobalDataMgr → GlobalUiMgr 打开 Panel
```

射线最大检测距离：Inspector 可配（建议默认 50m），Layer Mask 过滤仅检测 `Interactive` 层。

## 视觉反馈

| 类型 | 方案 |
|------|------|
| **高亮** | Outline Shader（优先）或材质替换，待后续详细设计 |
| **光标** | 默认指针 → 高亮手型（OnMouseEnter）→ 拖拽抓取（OnMouseDown 期间） |
| **提示** | 可选 World Space Canvas + TextMeshPro 悬浮显示物体名 |

## 交互物体分类

按**功能目的**开放拓展，不限种类：

| 交互目的 | 示例 |
|---------|------|
| 跳转 ProjectClip | 点门进入下一个实验房间 |
| 打开 Panel | 点仪器展示参数面板 |
| 连线配对 | 点击端点参与连线（→ LineConnect 子系统） |
| 拖拽匹配 | 拖拽零件到目标位置 |
| 自由拓展 | 按需新增 |

## 数据流（MCV 纪律）

```
View（GameObject + InteractiveBase）
  → 仅负责检测和上报输入事件
  → 不持有业务数据、不决定展示哪个 Panel、不操作其他 View

Controller
  → 唯一决策者：接收事件 → 查询 GlobalDataMgr → 打开 Panel
  → 不知道 View 的内部实现细节
```

→ [交互系统](../Modules/Interactive.md) | [MCV 模式](../Architecture/MCV.md)
