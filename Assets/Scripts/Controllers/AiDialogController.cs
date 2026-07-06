using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class AiDialogController : ControllerBase<AiDialogPanel>
    {
        protected override void OnViewBound()
        {
            View.OnSendMessage += OnUserMessage;
        }

        private void OnUserMessage(string message)
        {
            // TODO: M4 实现 —— 调用 AI 服务接口获取回复
            View.AddAiResponse($"收到您的消息: \"{message}\"。AI 回复功能待接入。");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            View.OnSendMessage -= OnUserMessage;
        }
    }
}
