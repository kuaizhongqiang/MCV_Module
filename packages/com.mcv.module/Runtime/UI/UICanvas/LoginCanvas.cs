
using MCV_Module.Models;
using MCV_Module.Utils;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.UI.UICanvas
{
    /// <summary>登录页画布：只装配登录面板，其余登录流程归 LoginPanel / LoginController。</summary>
    public class LoginCanvas : CanvasBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnRebuild()
        {
            // 目标 Canvas 已由 SceneStateChangeEventData 选定，这里不再判断状态
            var loginPanel = GetPanel<LoginPanel>();
            Log.Info("LoginCanvas.OnRebuild: " + loginPanel);
        }
    }
}
