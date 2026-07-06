using MCV_Module.Event;
using MCV_Module.GlobalManager;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class ResultSummitController : ControllerBase<ResultSummitPanel>
    {
        protected override void OnViewBound()
        {
            View.OnConfirm += OnConfirmResult;
            EventBus<AllStepsCompletedEvent>.Subscribe(OnAllStepsCompleted);
        }

        private void OnAllStepsCompleted(AllStepsCompletedEvent evt)
        {
            View.ShowResult("实验完成", 100f, "所有步骤已成功完成！");
        }

        private void OnConfirmResult()
        {
            GlobalSceneMgr.Instance.LoadSceneSingle("MainMenu");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            View.OnConfirm -= OnConfirmResult;
            EventBus<AllStepsCompletedEvent>.Unsubscribe(OnAllStepsCompleted);
        }
    }
}
