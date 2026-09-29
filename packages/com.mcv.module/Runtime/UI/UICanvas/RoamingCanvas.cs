
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Utils;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.UI.UICanvas
{
    /// <summary>漫游页画布：装配漫游功能面板，AI 开启时再挂 AI 对话面板。</summary>
    public class RoamingCanvas : CanvasBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnRebuild()
        {
            // 目标 Canvas 已由 SceneStateChangeEventData 选定，这里不再判断状态
            if (GlobalAiMgr.Instance.IsAiEnabled)
            {
                var aiPanel = GetPanel<AiDialogPanel>();
                Log.Info("MenuCanvas.OnRebuild: " + aiPanel);
            };
            var roamingFunctionPanel = GetPanel<RoamingFunctionPanel>();
            Log.Info("RoamingCanvas.OnRebuild: " + roamingFunctionPanel);
        }

        /// <summary>按当前任务类型装配任务面板（当前无调用方：漫游页只建功能面板，任务面板归 ContentCanvas）。</summary>
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
                default:
                    break;
            }
        }
    }
}
