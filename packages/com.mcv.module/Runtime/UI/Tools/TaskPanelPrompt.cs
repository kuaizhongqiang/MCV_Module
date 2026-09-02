using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.UI;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.UI
{
    /// <summary>
    /// "当前任务面板"内容提供器（module 侧）：
    /// 依赖具体任务面板类型（Task*Panel / TipsPanel），启动时经 [RuntimeInitializeOnLoadMethod]
    /// 注入 GlobalUIMgr.TaskPanelDescProvider —— core 的 CurrentStateDescription 在
    /// 不引用任何 module 类型的前提下获得任务面板描述（未注入时 core 静默降级）。
    /// </summary>
    public static class TaskPanelPrompt
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRegister()
        {
            GlobalUIMgr.TaskPanelDescProvider = Describe;
        }

        static string Describe()
        {
            var canvas = GlobalUIMgr.GetActiveCanvas();
            if (canvas == null) return "任务面板未激活";

            string content = "";
            switch (GlobalUIMgr.GetCurrentTaskType())
            {
                case TaskType.Purpose:
                    content = SafeContent(canvas.GetPanel<TaskPurposePanel>());
                    break;
                case TaskType.Equipment:
                    content = SafeContent(canvas.GetPanel<TaskEquipmentPanel>());
                    break;
                case TaskType.Principle:
                    content = SafeContent(canvas.GetPanel<TaskPrinciplePanel>());
                    break;
                case TaskType.LineConnection:
                    content = SafeContent(canvas.GetPanel<TaskLineConnectionPanel>());
                    if (!string.IsNullOrEmpty(content))
                    {
                        string tips = SafeTipsText(canvas);
                        if (!string.IsNullOrEmpty(tips)) content += "当前操作提示" + tips;
                    }
                    break;
                case TaskType.Training:
                    content = SafeContent(canvas.GetPanel<TaskTrainingPanel>());
                    if (!string.IsNullOrEmpty(content))
                    {
                        string tips = SafeTipsText(canvas);
                        if (!string.IsNullOrEmpty(tips)) content += "当前操作提示" + tips;
                    }
                    break;
                case TaskType.Test:
                    content = SafeContent(canvas.GetPanel<TaskTestPanel>());
                    break;
                default:
                    content = SafeContent(canvas.GetPanel<TaskDefaultPanel>());
                    break;
            }
            return string.IsNullOrEmpty(content) ? "任务面板暂无内容" : content;
        }

        /// <summary>安全取任务面板内容：面板不存在或返回 null/空/异常时降级为空串，不抛异常。</summary>
        static string SafeContent(TaskPanelBase panel)
        {
            if (panel == null) return "";
            string text = null;
            try
            {
                text = panel.GetPanelContent();
            }
            catch (System.Exception)
            {
                return "";
            }
            return text ?? "";
        }

        /// <summary>安全取 Tips 面板文本：面板不存在或异常时降级为空串。</summary>
        static string SafeTipsText(CanvasBase canvas)
        {
            if (canvas == null) return "";
            TipsPanel tips = null;
            try
            {
                tips = canvas.GetPanel<TipsPanel>();
            }
            catch (System.Exception)
            {
                return "";
            }
            if (tips == null) return "";
            try
            {
                return tips.GetText() ?? "";
            }
            catch (System.Exception)
            {
                return "";
            }
        }
    }
}
