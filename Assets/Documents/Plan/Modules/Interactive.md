# 交互系统

## 概述

`IObj` 接口定义 6 个鼠标事件（Enter/Exit/Down/Up/Drag/Click），`InteractiveBase` 为 `MonoBehaviour` 基类实现。`GlobalInteractiveMgr` 每帧 Raycast 检测光标下的交互物体，自动派发 Enter/Exit 事件。

## 待完善

- 常见 `InteractiveBase` 子类设计（连线端点、可拖拽元件、按钮、旋钮）
- 事件链与状态管理
- 与步骤系统的交互协同
