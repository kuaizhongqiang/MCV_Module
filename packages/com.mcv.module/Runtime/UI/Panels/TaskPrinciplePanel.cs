using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MCV_Module.Models.Project;

namespace MCV_Module.UI.Panels
{
    /// <summary>实验原理任务面板（占位实现，模仿 TaskPurposePanel）。</summary>
    public class TaskPrinciplePanel : TaskPanelBase
    {
        [SerializeField] Text titleText;
        [SerializeField] Text contentText;
        [SerializeField] Transform principleParent;
        PrincipleStruct currentPrinciple;

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

        public void SelectPrinciple(string title)
        {
            var principle = principleList.Find(x => x.title == title);
            currentPrinciple = principle;
            titleText.text = currentPrinciple.title;
            contentText.text = currentPrinciple.contentText;
            // TODO: 播放视频
        }

        public override string GetPanelContent()
        {
            string result = "";
            int principleCount = principleList.Count;
            result += "【实验原理页面】\n";
            result += $"当前任务包含 {principleCount} 个实验原理视频。\n";
            result += $"分别是：\n";
            for (int i = 0; i < principleCount; i++)
            {
                var item = principleList[i];
                result += $"{item.title}\n";
            }
            result += $"当前显示的原理是：\n";
            result += $"{currentPrinciple.title}\n";
            result += $"{currentPrinciple.contentText}\n";

            return result;
        }
    }
}
