
using MCV_Module.Models;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.UI.UICanvas
{
    public class LoginCanvas : CanvasBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnRebuild(SceneState state, TaskType taskType)
        {
            if (state == SceneState.Login)
            {                
                var loginPanel = GetPanel<LoginPanel>();
                var titlePanel = GetPanel<TitlePanel>();
                var functionPanel = GetPanel<FunctionPanel>();
                functionPanel.SetFunctionBtnActive("BackBtn",false);
                functionPanel.SetFunctionBtnActive("MuteBtn",false);
                functionPanel.SetFunctionBtnActive("ResourcePanelBtn",false);
                functionPanel.SetFunctionBtnActive("SummitBtn",false);
                functionPanel.SetFunctionBtnActive("RecordBtn",false);
                Debug.Log("LoginCanvas.OnRebuild: " + loginPanel + " " + titlePanel);
            }
        }
    }
}
