# 新增 UI 面板指南

## 概述

引导新增一个 UI Panel：继承 `PanelBase` → 实现 UI 逻辑 → 注册到 `GlobalUiMgr` → 在 Canvas 中挂载。

## 模板代码

```csharp
using MCV_Module.UI;
using MCV_Module.GlobalManager;
using UnityEngine;

public class MyNewPanel : PanelBase
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;

    protected override void Awake()
    {
        base.Awake();
        GlobalUiMgr.Instance.RegisterPanel(this);
        closeButton.onClick.AddListener(OnCloseClicked);
    }

    public override void Open()
    {
        base.Open();  // 触发淡入动画
        // 初始化逻辑：加载数据、设置默认值
    }

    public override void Close()
    {
        base.Close();  // 触发淡出动画
        // 清理逻辑
    }

    private void OnCloseClicked()
    {
        GlobalUiMgr.Instance.CloseCurrentPanel();  // 出栈并关闭
    }
}
```

## 关键步骤

1. 新建脚本，继承 `PanelBase`
2. 在 `Awake()` 中 `GlobalUiMgr.Instance.RegisterPanel(this)`
3. 重写 `Open()` / `Close()` 处理初始化和清理
4. 在 Unity Editor 中挂载到 Canvas 下的 Panel GameObject
5. 编写对应 `Controller`（继承 `ControllerBase<MyNewPanel>`）：

```csharp
public class MyNewController : ControllerBase<MyNewPanel>
{
    protected override void Awake()
    {
        base.Awake();
        GlobalControllerMgr.Instance.Register(this);
    }
}
```

6. Controller 手动放置到场景中

## Canvas 层级选择

| 面板类型 | 建议层级 |
|---------|:--:|
| 主界面（Menu、Task） | Layer 0 |
| 功能面板 | Layer 0 或 Layer 1 |
| 弹窗/确认 | Layer 1 |
| 加载过渡 | LoadingCanvas（999） |

具体层级表后续细化。

## 生命周期

```
Awake（注册） → Start → Open（播动画、压栈） → Active → Close（播动画、出栈）
```

→ [UI 框架](../Modules/UI-Framework.md) | [编码规范](Coding-Conventions.md)
