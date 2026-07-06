using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class AiDialogController : ControllerBase<AiDialogPanel>
    {
        private int _messageCount;

        protected override void OnViewBound()
        {
            View.OnSendMessage += OnUserMessage;
        }

        private void OnUserMessage(string message)
        {
            _messageCount++;

            // Mock AI 回复
            string response = _messageCount switch
            {
                1 => "您好！有什么可以帮助您的？",
                2 => "请按照实验指导书的步骤进行操作。",
                3 => "如果需要帮助，请参考实验原理部分。",
                _ => $"收到您的消息: \"{message}\"。正在处理，请稍候..."
            };

            View.AddAiResponse(response);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            View.OnSendMessage -= OnUserMessage;
        }
    }
}
