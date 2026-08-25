
using MCV_Module.Models;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.UI.UICanvas
{
    public class StartCanvas : CanvasBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnRebuild(SceneState state, TaskType taskType)
        {
            if (state == SceneState.Start)
            {
                var startPanel = GetPanel<StartPanel>();
                var functionPanel = GetPanel<FunctionPanel>();
                functionPanel.SetFunctionBtnActive("BackBtn",false);
                functionPanel.SetFunctionBtnActive("MuteBtn",false);
                functionPanel.SetFunctionBtnActive("ResourcePanelBtn",false);
                functionPanel.SetFunctionBtnActive("SummitBtn",false);
                functionPanel.SetFunctionBtnActive("RecordBtn",false);
                Debug.Log("StartCanvas.OnRebuild: " + startPanel + " " + functionPanel);
            }
        }
    }
}
