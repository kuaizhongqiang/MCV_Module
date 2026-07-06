using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class DialogController : ControllerBase<DialogPanel>
    {
        protected override void OnViewBound()
        {
            // Dialog 由事件驱动，Controller 通过面板 API 触发
        }

        public void ShowConfirm(string title, string message, System.Action onConfirm)
        {
            View.Show(title, message, onConfirm);
        }

        public void ShowAlert(string title, string message)
        {
            View.Show(title, message);
        }
    }
}
