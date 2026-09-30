
using MCV_Module.Utils;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.UI.UICanvas
{
    /// <summary>菜单页画布：只装配菜单面板（本页不挂 AI 对话面板）。</summary>
    public class MenuCanvas : CanvasBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnRebuild()
        {
            // 目标 Canvas 已由 SceneStateChangeEventData 选定，这里不再判断状态
            var menuPanel = GetPanel<MenuPanel>();
            menuPanel.SetUIActive(true);
            // WHY: 菜单界面本身就在主菜单，无需「返回主菜单」按钮（该按钮由 FunctionPanel 配置）
            // WHY: 菜单页不挂 AiDialogPanel（只有 ContentCanvas / RoamingCanvas 挂）
            Log.Info("MenuCanvas.OnRebuild: " + menuPanel);
        }
    }
}
