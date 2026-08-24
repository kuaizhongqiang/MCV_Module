using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panel
{
    public class TaskPurposePanel : TaskPanelBase
    {
        [SerializeField] Text titleText;
        [SerializeField] Text contentText;
        [SerializeField] string showModelKey = "";
    }
}