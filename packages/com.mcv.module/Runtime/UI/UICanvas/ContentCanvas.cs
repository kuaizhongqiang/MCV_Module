
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Utils;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.UI.UICanvas
{
    /// <summary>内容页画布：装配功能面板、任务列表、当前任务类型对应的任务面板，AI 开启时再挂 AI 对话面板。</summary>
    public class ContentCanvas : CanvasBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnRebuild()
        {
            // 目标 Canvas 已由 SceneStateChangeEventData 选定，这里不再判断状态
            var contentFunctionPanel = GetPanel<ContentFunctionPanel>();
            var taskListPanel = GetPanel<TaskListPanel>();
            CreatePanelByTaskType();
            
            if (GlobalAiMgr.Instance.IsAiEnabled)
            {
                var aiPanel = GetPanel<AiDialogPanel>();
                Log.Info("MenuCanvas.OnRebuild: " + aiPanel);
            }
            Log.Info("ContentCanvas.OnRebuild: " + taskListPanel + " " + contentFunctionPanel);
        }

        /// <summary>按当前任务类型装配任务面板（类型从唯一源读，不在方法间透传）。</summary>
        void CreatePanelByTaskType()
        {
            switch (GlobalDataMgr.GetCurrentTaskType())
            {
                case TaskType.Purpose:
                    var purposePanel = GetPanel<TaskPurposePanel>();
                    Log.Info("CreatePanelByTaskType: " + purposePanel);
                    break;
                case TaskType.Equipment:
                    var equipmentPanel = GetPanel<TaskEquipmentPanel>();
                    Log.Info("CreatePanelByTaskType: " + equipmentPanel);
                    break;
                case TaskType.Principle:
                    var principlePanel = GetPanel<TaskPrinciplePanel>();
                    Log.Info("CreatePanelByTaskType: " + principlePanel);
                    break;
                case TaskType.LineConnection:
                    var lineConnectionPanel = GetPanel<TaskLineConnectionPanel>();
                    var tipsPanel = GetPanel<TipsPanel>();
                    Log.Info("CreatePanelByTaskType: " + lineConnectionPanel + " " + tipsPanel);
                    break;
                case TaskType.Training:
                    var trainingPanel = GetPanel<TaskTrainingPanel>();
                    GetPanel<TipsPanel>();
                    Log.Info("CreatePanelByTaskType: " + trainingPanel);
                    break;
                case TaskType.Test:
                    var testPanel = GetPanel<TaskTestPanel>();
                    Log.Info("CreatePanelByTaskType: " + testPanel);
                    break;
                case TaskType.Info:
                    var infoPanel = GetPanel<TaskInfoPanel>();
                    Log.Info("CreatePanelByTaskType: " + infoPanel);
                    break;
                case TaskType.Structure:
                    var structurePanel = GetPanel<TaskStructurePanel>();
                    Log.Info("CreatePanelByTaskType: " + structurePanel);
                    break;
                case TaskType.Inspection:
                    var inspectionPanel = GetPanel<TaskInspectionPanel>();
                    var inspectionTipsPanel = GetPanel<TipsPanel>();
                    Log.Info("CreatePanelByTaskType: " + inspectionPanel + " " + inspectionTipsPanel);
                    break; 
                case TaskType.Exam:
                    var taskExamPanel = GetPanel<TaskExamPanel>();
                    Log.Info("CreatePanelByTaskType: " + taskExamPanel);
                    break;
                default:
                    break;
            }
        }
    }
}
