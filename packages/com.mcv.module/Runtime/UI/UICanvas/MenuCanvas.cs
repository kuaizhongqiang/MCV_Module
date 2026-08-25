
using MCV_Module.Models;
using MCV_Module.Utils;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.UI.UICanvas
{
    public class MenuCanvas : CanvasBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnRebuild(SceneState state, TaskType taskType)
        {
            if (state == SceneState.Menu)
            {
                var menuPanel = GetPanel<MenuPanel>();
                var titlePanel = GetPanel<TitlePanel>();
                var functionPanel = GetPanel<FunctionPanel>();
                // 菜单界面本身就在主菜单，无需「返回主菜单」按钮
                functionPanel.SetFunctionBtnActive("BackBtn",false);
                var aiPanel = GetPanel<AiDialogPanel>();
                Log.Info("MenuCanvas.OnRebuild: " + menuPanel + " " + titlePanel + " " + functionPanel + " " + aiPanel);
            }
        }
    }
}
