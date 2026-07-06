# UI 展示

## 概述

纯 UI 驱动的内容展示模式，包括图文排版、视频播放、3D 模型预览（TextureReader）。主要用于 Purpose / Equipment / Principle / Test 等 TaskType。**严格遵循 MCV**——View 为各 Panel，数据由 Controller 从 `GlobalDataMgr` 获取。

## 组件方案

| 组件 | 方案 | 状态 |
|------|------|:--:|
| 图文排版 | TextMeshPro（已有依赖），支持富文本和图片混排 | 待实现 |
| 视频播放 | AVPro 第三方插件 | 计划中，插件未导入 |
| 模型预览 | TextureReader（RenderTexture 拍 3D 模型 → UI RawImage 显示） | 待实现 |

## Controller 与 View 关系

- **一对多**：一个 Controller 可管理多个 Panel
- Controller 从 `GlobalDataMgr` 获取 TaskData 图文类数据，驱动 View 展示
- Panel 通过 GlobalUiMgr 栈管理，支持返回导航

## 视频播放集成点（AVPro 计划）

- 导入后替换 Unity 内置 VideoPlayer
- Panel 内嵌视频区域：AVPro 渲染到 RenderTexture → UI RawImage 显示
- 播放控制：Play/Pause/Seek 通过 Controller 封装

## TextureReader 模型预览

```
场景中独立 Camera（不渲染到屏幕）→ 对准 3D 模型
  → 输出到 RenderTexture
    → UI RawImage 显示在 Panel 中
      → 支持旋转/缩放（拖拽 RawImage 区域）
```

## 数据流

```
GlobalDataMgr（TaskData 图文类）
  → Controller（解析数据）
    → GlobalUiMgr（栈管理）
      → Panel（TextMeshPro / AVPro / TextureReader）
```

严格遵循 MCV：Controller 逻辑驱动，View 纯表现。

→ [UI 框架](../Modules/UI-Framework.md)
