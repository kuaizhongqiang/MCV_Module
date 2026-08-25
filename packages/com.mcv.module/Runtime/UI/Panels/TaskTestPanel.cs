using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MCV_Module.Models.Project;

namespace MCV_Module.UI.Panel
{
    /// <summary>小测验任务面板（占位实现，模仿 TaskPurposePanel）。</summary>
    public class TaskTestPanel : TaskPanelBase
    {
        [SerializeField] Text titleText;
        [SerializeField] Transform questionParent;

        readonly List<QuestionClip> questionList = new List<QuestionClip>();

        public void Init(List<QuestionClip> questionClips)
        {
            if (questionClips == null) return;
            questionList.Clear();
            questionList.AddRange(questionClips);
            // TODO: 按 questionList 装配测验 UI
        }

        public void SetQuestion(List<QuestionClip> questionClips)
        {
            Init(questionClips);
        }
    }
}
