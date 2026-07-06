# UI 框架

## 概述

三层结构：`UIBase`（CanvasGroup 淡入淡出动画）→ `CanvasBase`（全屏画布层）→ `PanelBase`（画布内面板）。`GlobalUiMgr` 管理所有 Canvas 与 Panel 实例，提供 `GetCanvas<T>()` / `GetPanel<T>()` 类型查找。现有面板：Menu、Title、Task、Function、Dialog、Tips、Copyright。

## 待完善

- Canvas 层级与渲染顺序
- Panel 生命周期（打开/关闭/栈管理）
- 动画参数配置（持续时长、缓动曲线）
