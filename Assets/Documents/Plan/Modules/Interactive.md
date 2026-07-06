# 交互系统

## 概述

`IObj` 接口定义 6 个鼠标事件，`InteractiveBase` 为 `MonoBehaviour` 基类实现。`GlobalInteractiveMgr` 在 `Update()` 中做统一射线检测，自动派发事件。**核心需求**：避免每个交互物体各自一个 Update 周期。

## IObj 接口

| 事件 | 频率 | 用途 |
|------|:--:|------|
| **OnMouseEnter** | 核心 | 鼠标进入，高亮反馈 |
| **OnMouseExit** | 核心 | 鼠标离开，取消高亮 |
| **OnMouseClick** | 核心 | 点击交互 |
| OnMouseDown | 拖拽 | 拖拽起始 |
| OnMouseUp | 拖拽 | 拖拽释放 |
| OnMouseDrag | 拖拽 | 拖拽移动 |

## 交互物体分类

按**功能目的**开放拓展，不限种类：

| 功能目的 | 交互模式 | 示例 |
|---------|---------|------|
| 跳转 Clip | Click | 点门进入下一个实训项目 |
| 打开 Panel | Click | 点仪器弹出参数面板 |
| 连线 | Click + 配对 | 端点点击参与配对 |
| 拖拽匹配 | Drag | 拖拽物体到目标位置 |
| 自由拓展 | 按需 | 新功能目的 + 新模式 |

## 注册机制

```csharp
public class MyClickObj : InteractiveBase
{
    protected override void Awake()
    {
        base.Awake();
        // InteractiveBase.Awake() 已自动调用 GlobalInteractiveMgr.Instance.Register(this)
    }
}
```

## 射线检测流

```
GlobalInteractiveMgr.Update()
  → Physics.Raycast（每帧一次，指定距离和 Layer）
    → 命中 IObj 物体
    → 与上一帧命中对比
      → 不同 → 派发 OnMouseExit（旧）+ OnMouseEnter（新）
      → 相同 → 根据 Input 派发 Click/Down/Up/Drag
```

## 与步骤系统的协同

| Condition | 交互子类 | 交互模式 |
|-----------|---------|---------|
| StepConditionClick | StepClickObj | Click → `onClick` UnityEvent |
| StepConditionDrag | StepDragObj + StepDragTargetObj | Down→Drag→Up 循环 |
| StepConditionLineConnect | LinePointObj + LineElementObj | 端点点击+配对 |

→ [步骤系统](../Business/Step-System.md) | [连线子系统](../Business/LineConnect.md)
