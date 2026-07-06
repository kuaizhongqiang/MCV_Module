using MCV_Module.Data.Project;
using MCV_Module.Event;
using TMPro;

namespace MCV_Module.UI.Panels
{
    public class TitlePanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        private void OnEnable()
        {
            EventBus<TaskChangedEvent>.Subscribe(OnTaskChanged);
        }

        private void OnDisable()
        {
            EventBus<TaskChangedEvent>.Unsubscribe(OnTaskChanged);
        }

        private void OnTaskChanged(TaskChangedEvent evt)
        {
            UpdateTitle(evt.TaskType);
        }

        public void UpdateTitle(TaskType taskType)
        {
            if (_titleText != null)
                _titleText.text = taskType switch
                {
                    TaskType.Purpose => "实验目的",
                    TaskType.Equipment => "实验仪器",
                    TaskType.Principle => "实验原理",
                    TaskType.LineConnection => "电路连接",
                    TaskType.Training => "仿真实验",
                    TaskType.Test => "小测验",
                    _ => "MCV 实训系统"
                };
        }

        public void SetDescription(string desc)
        {
            if (_descriptionText != null)
                _descriptionText.text = desc;
        }
    }
}
