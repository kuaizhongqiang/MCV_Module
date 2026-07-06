# 新增交互物体指南

## 概述

引导新增一个交互物体：继承 `InteractiveBase` → 实现 `IObj` 事件 → 自动注册 → 配置 Collider。

## 模板代码

```csharp
using MCV_Module.Interactive;
using MCV_Module.GlobalManager;
using UnityEngine;
using UnityEngine.Events;

public class MyClickableObj : InteractiveBase
{
    [SerializeField] private UnityEvent onCustomClick;

    protected override void Awake()
    {
        base.Awake();
        // InteractiveBase.Awake() 已自动注册到 GlobalInteractiveMgr
        // 无需手动调用 Register
    }

    public override void OnMouseEnter()
    {
        base.OnMouseEnter();
        // 高亮反馈：Outline Shader 启用 / 材质替换
    }

    public override void OnMouseExit()
    {
        base.OnMouseExit();
        // 取消高亮
    }

    public override void OnMouseClick()
    {
        base.OnMouseClick();
        onCustomClick?.Invoke();
    }
}
```

## 分类原则

按**功能目的**分类，不限种类：
- **跳转 Clip 型**：点击后跳转到另一个实训项目
- **打开 Panel 型**：点击后打开对应 UI 面板
- **连线型**：作为连线端点参与配对（继承 `LinePointObj`）
- **拖拽型**：支持拖拽到目标位置
- **自由拓展**：新功能目的 + 新交互模式

## 关键步骤

1. 新建脚本，继承 `InteractiveBase`（或 StepClickObj / StepDragObj 等具体子类）
2. 重写需要的 `IObj` 事件
3. **无需手动注册**——`InteractiveBase.Awake()` 已自动注册
4. 为 GameObject 添加 `Collider`（射线检测依赖，Layer 设为 `Interactive`）
5. 将脚本挂载到三维物体上

## 事件处理推荐

| 需求 | 重写方法 | 注意事项 |
|------|---------|---------|
| 点击响应 | `OnMouseClick` | 最常用，适合按钮、开关 |
| 高亮反馈 | `OnMouseEnter` + `OnMouseExit` | 配合 Outline Shader 或材质替换 |
| 拖拽交互 | `OnMouseDown` + `OnMouseUp` + `OnMouseDrag` | 配合 StepDragObj 和 StepDragTargetObj |

→ [交互系统](../Modules/Interactive.md)
