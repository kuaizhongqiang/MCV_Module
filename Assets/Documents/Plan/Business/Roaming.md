# 漫游系统

## 概述

第一人称漫游，WASD 移动 + 鼠标控制视角。点击三维场景中的物体时，通过 `GlobalInteractiveMgr` 的 Raycast 检测命中物体，触发对应 UI Panel 展示信息。严格遵循 MCV 模式。

## 待完善

- 移动与控制方案（CharacterController / Rigidbody）
- 鼠标拾取与高亮反馈
- 点击物体到 Panel 展示的数据流
