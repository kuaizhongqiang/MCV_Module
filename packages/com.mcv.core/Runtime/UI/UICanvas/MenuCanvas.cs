
using MCV_Module.UI.Panels;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.UI.UICanvas
{
    /// <summary>菜单页画布：菜单面板待重写（旧 MenuPanel 已移除）。</summary>
    public class MenuCanvas : CanvasBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnRebuild()
        {
            // 目标 Canvas 已由 SceneStateChangeEventData 选定，这里不再判断状态
            Log.Info("MenuCanvas.OnRebuild（菜单面板待重写）");

            var menu = GetPanel<MenuPanel>();
            Log.Info($"{menu.name} 正在创建");
        }
    }
}
