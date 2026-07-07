# M2 Milestone Review

**Date**: 2026-07-06 | **Version**: 3 | **Reviewer**: PM CodeBuddy

**Scope**: M2 v3 审查 —— 验证 `9f324a7` 修复提交对 v2 发现的 13 项问题的修复情况。

---

## v2 → v3 修复确认

| # | v2 发现 | Severity | v3 状态 |
|:---|:---|:---|:---:|
| 1 | LineEndpoint 点击配对逻辑缺失 | Critical | ✅ **已修复** — 完整配对流程 (static s_SelectedEndpoint + CanConnectTo + Connect + LineConnectedEvent) |
| 2 | InputSystem 双轨制 | Major | ✅ **已修复** — MovementController 改用新 InputSystem (Keyboard/Mouse.current) |
| 3 | LoadingPanel 无 Slider | Major | ✅ **已修复** — 改用 `Slider _progressSlider` |
| 4 | allowSceneActivation 跳过 90% 等待 | Major | ✅ **已修复** — 改为 `false` + 0.9f 等待 + 手动激活 |
| 5 | MenuController 实验选择 TODO | Major | ✅ **已修复** — 实现 `GlobalSceneMgr.Instance.LoadSceneSingle(sceneName)` |
| 6 | EventBus 注释声称 WeakReference | Major | ✅ **已修复** — 注释改为正确描述 |
| 7 | 5 个 Step 事件未预声明 | Major | ✅ **已修复** — GameEvents.cs 全部声明 + TODO M3 |
| 8 | TipsPanel 正则 Bug | Minor | ⚠️ **未覆盖** — 非本次修复范围 |
| 9 | LoadingController.OnSceneLoaded 空 | Minor | ⚠️ **已标记 TODO M3** — 可接受 |
| 10 | TaskDataConverter 全部 TODO | Minor | ⚠️ **已标记 TODO M4** — 可接受 |
| 11 | QuestionData 缺少字段 | Minor | ⚠️ **已标记 TODO M4** — 可接受 |
| 12 | 已定义事件未使用 | Minor | ✅ **新增 TODO 注释** — InteractiveClickEvent/PanelVisibilityEvent/TaskCompletedEvent 均有 TODO |
| 13 | CI InputSystem 配置建议 | Suggestion | ℹ️ 非代码问题 |

---

## 新增正向发现

### InteractiveDrag 相机引用统一

- `InteractiveDrag.MoDownEvent()` 改用 `GlobalCameraMgr.Camera`，与 `GlobalInteractiveMgr` 统一。不再使用 `Camera.main`。

### LineConnectedEvent 新事件类型

- `GameEvents.cs` 新增 `LineConnectedEvent(GroupId, IsConnected)`，连线连接/断开时通过 EventBus 发布。Panel 可订阅此事件实时刷新连线状态。

---

## 发现汇总

| Severity | Count | 说明 |
|----------|:---:|:---|
| Critical | **0** | 全部修复 |
| Major | **0** | 全部修复 |
| Minor | 4 | TipsPanel bug + 3 个已标记 TODO（M3/M4 规划，可接受） |
| Suggestion | 1 | CI InputSystem 检查 |
| **Total** | **5** | 无阻塞项 |

---

## M2 最终完成度

| 子模块 | v2 评分 | v3 评分 | 变化 |
|:---|:---:|:---:|:---|
| M2a EventBus | 85% | **95%** | 注释修正 + Step 事件预声明 |
| M2b 场景加载 | 80% | **95%** | Slider + 90% 等待 + Menu 跳转 |
| M2c 核心 UI | 85% | **90%** | MenuController 跳转完成 |
| M2d Task Panel | 75% | **80%** | LineConnectedEvent 集成 |
| M2e 交互系统 | 60% | **90%** | 连线配对完整 + InputSystem 统一 + Camera 统一 |
| **M2 整体** | **77%** | **90%** | ✅ 核心管线可跑通，连线业务可用 |

---

## 结论

**M2 可以推进到 M3。** 残余的 Minor 项（TipsPanel bug、TaskDataConverter、QuestionData）均为 M3/M4 规划范围内的 TODO，不影响 M2 验收。
