# LineConnect 连线子系统

> 步骤系统中 `StepConditionLineConnect` 的底层支撑子系统。

## 概述

用户在 3D 场景中依次连接若干对端点，所有配对完成后步骤通过。适用于电路连接、管线对接、实验器材连线等教学场景。

## 核心组件

| 组件 | 继承 | 职责 |
|------|------|------|
| `LineDirector` | — | 管理一组连线（LineGroup 容器），控制显隐与状态 |
| `LineGroup` | — | 单组连线容器，挂载在 StepConditionLineConnect 下 |
| `LineObj` | — | 单条线物体，GameObject.name 编码配对关系 |
| `LineElementObj` | InteractiveBase | 连线元素（端点父级容器） |
| `LinePointObj` | InteractiveBase | 端点物体，可被点击选中 |

## 配对机制

`LineObj` 的 `GameObject.name` 编码配对关系：

```
格式：Line_Element1_Point1_Element2_Point2
示例：Line_A_1_B_2 → 元素 A 的端点 1 与 元素 B 的端点 2 配对
```

**不限定连线顺序**，用户可按任意顺序完成配对。

## 交互状态机

```
1. 无选中 → 点击端点 → 选中（高亮）
2. 已选中 → 点击配对端点 → 配对成功，显示线
3. 已选中 → 点击不匹配端点 → 取消选中
4. 已选中 → 点击空白处 → 取消选中
5. 已选中 → 点击同一点 → 取消选中
6. 点击已配对端点 → 忽略（该 LinePair.isConnected 已为 true）
```

## 共享元素独立锁定

若步骤需连接 `Line_A_1_B_2` 和 `Line_B_1_C_2`，B 元素上有两个不同端点（B_1 和 B_2）。AB 连接后 B_2 被锁定，但 **B_1 仍可参与 BC 配对**，互不阻塞。

## 架构边界

- `pointGroupRoot` 通过 Inspector 直接挂载到 `StepConditionLineConnect`，Condition 层对 StepDirector 零依赖
- **LineDraw 可修改**：划线逻辑、曲线路径、多点绘制——允许修改代码和逻辑（材质、颜色、线宽、端点位置、配对判定均可自由改动）

## 与交互系统关系

`LinePointObj` 和 `LineElementObj` 均继承 `InteractiveBase`，通过 `GlobalInteractiveMgr` 统一射线检测接入。端点状态由 `StepConditionLineConnect` 管理，不依赖额外 Controller。

→ [步骤系统](Step-System.md) | [交互系统](../Modules/Interactive.md)
