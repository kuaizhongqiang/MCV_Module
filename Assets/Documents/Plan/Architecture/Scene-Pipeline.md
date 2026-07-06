# 场景加载管线

## 概述

运行时场景采用 **Additive 加载** 模式：`0_Setup → 1_Controller + 2_UI + N（三维实验场景）`。`Setup` 顺序初始化 8 个 GlobalManager，完成后依次加载 1、2，再按需加载实验场景 N。`GlobalSceneMgr` 统一管理场景的加载、卸载与切换。

## 待完善

- Additive 加载具体实现（同步/异步）
- 场景卸载时机（切换实验时保留 1+2，卸载旧 N）
- 加载过渡与进度反馈
