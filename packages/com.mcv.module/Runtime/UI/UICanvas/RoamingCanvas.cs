
using MCV_Module.Models;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.UI.UICanvas
{
    public class RoamingCanvas : CanvasBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnRebuild(SceneState state, TaskType taskType)
        {
            if (state == SceneState.Roaming)
            {
                var titlePanel = GetPanel<TitlePanel>();
                var functionPanel = GetPanel<FunctionPanel>();
                var taskListPanel = GetPanel<TaskListPanel>();                                
                var aiPanel = GetPanel<AiDialogPanel>();
                Debug.Log("ContentCanvas.OnRebuild: " + titlePanel + " " + functionPanel + " " + taskListPanel + " " + aiPanel);
                CreatePanelByTaskType(taskType);
            }
        }

        void CreatePanelByTaskType(TaskType taskType)
        {
            switch (taskType)
            {
                case TaskType.Purpose:
                    var purposePanel = GetPanel<TaskPurposePanel>();
                    Debug.Log("CreatePanelByTaskType: " + purposePanel);
                    break;
                case TaskType.Equipment:
                    var equipmentPanel = GetPanel<TaskEquipmentPanel>();
                    Debug.Log("CreatePanelByTaskType: " + equipmentPanel);
                    break;
                case TaskType.Principle:
                    var principlePanel = GetPanel<TaskPrinciplePanel>();
                    Debug.Log("CreatePanelByTaskType: " + principlePanel);
                    break;
                case TaskType.LineConnection:
                    var lineConnectionPanel = GetPanel<TaskLineConnectionPanel>();
                    var tipsPanel = GetPanel<TipsPanel>();
                    Debug.Log("CreatePanelByTaskType: " + lineConnectionPanel + " " + tipsPanel);
                    break;
                case TaskType.Training:
                    var trainingPanel = GetPanel<TaskTrainingPanel>();
                    GetPanel<TipsPanel>();
                    Debug.Log("CreatePanelByTaskType: " + trainingPanel);
                    break;
                case TaskType.Test:
                    var testPanel = GetPanel<TaskTestPanel>();
                    Debug.Log("CreatePanelByTaskType: " + testPanel);
                    break;
                default:
                    break;
            }
        }
    }
}
