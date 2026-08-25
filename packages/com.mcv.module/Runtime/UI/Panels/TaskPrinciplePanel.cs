using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MCV_Module.Models.Project;

namespace MCV_Module.UI.Panel
{
    /// <summary>实验原理任务面板（占位实现，模仿 TaskPurposePanel）。</summary>
    public class TaskPrinciplePanel : TaskPanelBase
    {
        [SerializeField] Text titleText;
        [SerializeField] Transform principleParent;

        readonly List<PrincipleStruct> principleList = new List<PrincipleStruct>();

        public void Init(List<PrincipleStruct> principleStructs)
        {
            if (principleStructs == null) return;
            principleList.Clear();
            principleList.AddRange(principleStructs);
            // TODO: 按 principleList 装配 UI（每个 PrincipleStruct: title/contentText/videoName）
        }

        public void SetPrinciple(List<PrincipleStruct> principleStructs)
        {
            Init(principleStructs);
        }
    }
}
