---
name: ui-toolkit
description: Unity UI Toolkit — USS pitfalls, UXML patterns, and UGUI→UITK migration for the MCV_Module project
---

# Unity UI Toolkit Skill

UGUI 开发指引。重点：LLM 熟悉 HTML/CSS 但容易在 USS 上犯错，以及如何将现有 UGUI MVC 架构迁移到 UI Toolkit。

## USS 常见错误

LLM 最容易把 CSS 习惯带到 USS。**不确定的 USS 属性先去[官方文档](https://docs.unity3d.com/Manual/UIE-USS-Properties-Reference.html)确认**，不要凭 CSS 经验直接写。

| ❌ 错误写法 | ✅ 正确写法 | 说明 |
|------------|------------|------|
| `display: block` | 只用 `flex` 或 `none` | USS 无 block/inline |
| `text-align: center` | `-unity-text-align: middle-center` | 属性名和值都不同 |
| `rgba(0,0,0,0.5)` | `#00000080` | USS 不支持颜色函数 |
| `div { }` | `VisualElement { }` | 用 C# 类型名 |
| `#id` | `#name` | 用 name 属性 |
| `box-shadow` | ❌ 不存在 | USS 不支持 |
| `calc(100% - 20px)` | ❌ 不存在 | 手工算好值 |
| `--var: x` | ❌ 不存在 | 无 CSS 变量 |
| `:nth-child()` | ❌ 不存在 | 无结构伪类 |
| `!important` | ❌ 不支持 | 靠选择器优先级 |
| `em` / `rem` / `vh` | 只用 `px` / `%` / `auto` | 无其他单位 |
| `gap: 8px` | 子元素 `margin` | gap 需 Unity 2023.1+ |

## UXML ↔ HTML 映射

| HTML | UXML |
|------|------|
| `<div>` | `<ui:VisualElement>` |
| `<button>` | `<ui:Button>` |
| `<span>` / `<p>` | `<ui:Label>` |
| `<img>` | `<ui:Image>` |
| `<input>` | `<ui:TextField>` |
| `class="..."` | `class="..."` |
| `id="..."` | `name="..."` |

```xml
<div class="container"><button class="btn">登录</button></div>
```
→
```xml
<ui:VisualElement class="container"><ui:Button class="btn" text="登录" /></ui:VisualElement>
```

## UGUI MVC → UI Toolkit 迁移

### 组件映射

| 现有 UGUI 写法 | UI Toolkit 对应 |
|----------------|----------------|
| `Canvas` + `CanvasBase : MonoBehaviour` | `UIDocument` + 根 `VisualElement` |
| `PanelBase : MonoBehaviour` | 自定义 `VisualElement` 子类或 UXML 模板 |
| `ElementBase : MonoBehaviour` | `VisualElement` 子控件或 `CustomControl` |
| `Instantiate(prefab, parent)` | `new Button()` + `parent.Add()` 或 `AssetDatabase.Load` |
| `Resources.Load<GameObject>()` | `AssetDatabase.Load<VisualTreeAsset>()` |
| `CanvasGroup.alpha / interactable` | `style.opacity / pickingMode` |
| `Button.onClick.AddListener()` | `button.RegisterCallback<ClickEvent>()` |
| `gameObject.SetActive(true/false)` | `style.display = DisplayStyle.Flex/None` |
| Inspector 拖拽引用 | UXML 中 `name` 属性 + `Query()` 查找 |
| Animator 控制显隐 | USS transition 或代码操作 `style` |
| `#region` + MonoBehaviour 生命周期 | `AttachToPanelEvent` / `DetachFromPanelEvent` |

### MVC 层变化

```
Controller 层：不变。ControllerBase<TView> 继续用，TView 从 PanelBase 变成 VisualElement。

View 层：重写。
  UGUI：PanelBase : MonoBehaviour，挂 Canvas 下，拖拽引用
  UITK：PanelBase 封装 VisualElement，不继承 MonoBehaviour，用 UXML + USS 定义结构

Model 层：不变。POCO 数据完全独立于 UI 系统。
```

### Controller 获取 View 的方式

```csharp
// UGUI 写法
View = CanvasBase.Instance.GetPanel<TView>();

// UI Toolkit 写法
var uiDoc = GetComponent<UIDocument>();
View = uiDoc.rootVisualElement.Q<TView>("view-name");
// 或通过 UXML template 实例化
var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/LoginPanel.uxml");
var instance = template.Instantiate();
View = instance.Q<LoginPanel>();
```

**核心变化：** View 不再走 MonoBehaviour 继承链，而是 VisualElement 组合。Controller 仍然独立，架构不变。

## 官方文档

- [USS 选择器](https://docs.unity3d.com/Manual/UIE-USS-Selectors.html)
- [USS 属性参考](https://docs.unity3d.com/Manual/UIE-USS-Properties-Reference.html)
- [UXML 结构](https://docs.unity3d.com/Manual/UIE-UXML.html)
- [UI Toolkit 架构概览](https://docs.unity3d.com/Manual/UIE.html)
- [UI Toolkit vs UGUI 对比](https://docs.unity3d.com/Manual/UIE-comparison.html)
- [UI Toolkit + MVC 示例](https://docs.unity3d.com/Manual/UIE-HowTo-CreateMVC.html)
