using MCV_Module.Event;
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
            // 所有步骤完成后自动显示结果
            View.ShowResult("实验完成", 100f, "所有步骤已成功完成！");
        }

        private void OnConfirmResult()
        {
            // TODO: M4 实现 —— 返回主菜单或进入下一实验
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            View.OnConfirm -= OnConfirmResult;
            EventBus<AllStepsCompletedEvent>.Unsubscribe(OnAllStepsCompleted);
        }
    }
}
