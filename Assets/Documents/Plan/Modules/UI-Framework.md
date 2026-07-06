# UI 框架

## 概述

三层结构：`UIBase`（CanvasGroup 淡入淡出）→ `CanvasBase`（全屏画布层）→ `PanelBase`（画布内面板）。`GlobalUiMgr` 管理所有 Canvas 与 Panel 实例，通过 `GetCanvas<T>()` / `GetPanel<T>()` 类型查找。

## Canvas 层级

CanvasBase 负责层级管理，当前 2 层 + LoadingCanvas 顶层：

| 层 | Sorting Order | 用途 |
|------|:--:|------|
| Layer 0 | 0 | 基础 Canvas |
| Layer 1 | 10 | 上层 Canvas |
| LoadingCanvas | 999 | 始终最顶层 |

层级表后续细化，根据实际业务调整。

## Panel 管理

- **栈管理**：支持返回导航，打开压栈、关闭出栈
- **单例**：每种 Panel 只有一个实例
- **注册**：Panel 在 `Awake()` 中自行注册：

```csharp
protected override void Awake()
{
    base.Awake();
    GlobalUiMgr.Instance.RegisterPanel(this);
}
```

## Panel 生命周期

```
Awake（注册） → Start → Open（播淡入动画、压栈） → Active（可交互） → Close（播淡出动画、出栈）
```

## 现有 Panel

| Panel | 职责 |
|-------|------|
| Menu | 主菜单 |
| Title | 标题 |
| Task | 任务列表 |
| Function | 功能面板 |
| Dialog | 弹窗/确认 |
| Tips | 提示信息 |
| Copyright | 版权信息 |

## UIBase 动画

CanvasGroup 淡入淡出动画，持续时长和缓动曲线暂不硬定，后续可配置。

## 新增 Panel 步骤

1. 继承 `PanelBase`
2. 在 `Awake()` 中注册到 `GlobalUiMgr`
3. 实现 `Open()` / `Close()` 逻辑
4. 在 Canvas 中挂载

→ [新增 UI 面板指南](../Guides/New-Panel.md)
