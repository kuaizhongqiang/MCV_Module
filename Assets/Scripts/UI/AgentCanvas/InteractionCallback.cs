using System;
using System.Linq;
using MCV_Module.Event;
using MCV_Module.GlobalManager;
using MCV_Module.GlobalManager.CLI;
using UnityEngine.UIElements;

namespace MCV_Module.UI.AgentCanvas
{
    /// <summary>
    /// 交互回调管道 — 处理学生在前端触发的交互事件。
    ///
    /// 流程:
    ///   元件交互 (choice/fill/button 提交)
    ///     → InteractionCallback.SendInteraction()
    ///       → 通过 WebSocket 推送 interaction 事件给 MCP Server
    ///         → Agent 收到后决定下一步
    ///           → 调用 result.show 展示反馈
    ///             → PageRenderer.UpdateElementVisual() 更新反馈区域
    /// </summary>
    public class InteractionCallback
    {
        private readonly CLIWebSocketManager _wsManager;

        public InteractionCallback(CLIWebSocketManager wsManager)
        {
            _wsManager = wsManager;
        }

        /// <summary>
        /// 发送交互回执给 MCP Server（通过 WebSocket）。
        /// </summary>
        public async void SendInteraction(string pageId, string elementId, string action, object data, ElementConfig element)
        {
            if (_wsManager == null)
            {
                UnityEngine.Debug.LogWarning("[InteractionCallback] WebSocket 未就绪，无法发送交互回执");
                return;
            }

            var requestId = $"req_interact_{Guid.NewGuid():N}"[..16];

            var payload = new
            {
                requestId,
                inResponseTo = "",   // MCP Server 回填
                @event = "interaction",
                dialogId = GlobalDataMgr.Instance?.SystemData?.cliConfig?.cliEnabled == true ? "default" : "",
                pageId,
                elementId,
                action,
                data,
            };

            try
            {
                var json = SimpleJson.Serialize(payload);
                await _wsManager.PushReceiptAsync("interact_" + elementId, json);
                UnityEngine.Debug.Log($"[InteractionCallback] 交互回调已发送: {elementId} ({action})");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[InteractionCallback] 发送失败: {e.Message}");
            }
        }

        /// <summary>
        /// 处理 result.show 命令 — 在指定元件上展示答题反馈。
        /// </summary>
        internal static void ApplyResult(PageRenderer renderer, ElementConfig element, SimpleJson.JsonObject resultData)
        {
            if (renderer == null || element == null) return;

            bool isCorrect = resultData?.GetBool("isCorrect", false) ?? false;
            string correctAnswer = resultData?.GetString("correctAnswer", "");
            string explanation = resultData?.GetString("explanation", "");
            string userAnswer = resultData?.GetString("userAnswer", element.data.userAnswer);
            string feedbackHtml = "";

            if (!string.IsNullOrEmpty(element.data.correctAnswer))
            {
                // 客观题 (auto)
                feedbackHtml = $"<b>{(isCorrect ? "✅ 正确!" : "❌ 错误")}</b>";
                if (!isCorrect && !string.IsNullOrEmpty(correctAnswer))
                    feedbackHtml += $"\n正确答案: {correctAnswer}";
            }
            else if (element.data.correctAnswer == null || element.data.correctAnswer == "")
            {
                // 可同时显示主/客观题的统一信息
                feedbackHtml = isCorrect ? "✅ 正确!" : "";
            }

            // 组装完整反馈
            string feedbackText = feedbackHtml;
            if (!string.IsNullOrEmpty(explanation))
                feedbackText += $"\n\n📖 {explanation}";

            // 更新元件的反馈区域
            renderer.UpdateElementVisual(element.id, ve =>
            {
                var feedback = ve.Q<VisualElement>(name: $"{element.id}-feedback");
                if (feedback != null)
                {
                    feedback.Clear();
                    var label = new UnityEngine.UIElements.Label
                    {
                        text = feedbackText,
                        name = $"{element.id}-result"
                    };
                    label.AddToClassList(isCorrect ? "agent-result-correct" : "agent-result-incorrect");
                    feedback.Add(label);
                }

                // 如果是 choice，高亮正确/错误选项
                if (element.type == "choice" && !string.IsNullOrEmpty(correctAnswer))
                {
                    var radioGroup = ve.Q<UnityEngine.UIElements.RadioButtonGroup>();
                    if (radioGroup != null && radioGroup.choices != null)
                    {
                        var choiceList = radioGroup.choices.ToList();
                        for (int i = 0; i < choiceList.Count; i++)
                        {
                            bool isChoiceCorrect = choiceList[i] == correctAnswer;
                            bool wasUserPick = choiceList[i] == userAnswer && !isCorrect;

                            if (i < radioGroup.childCount)
                            {
                                var choiceVE = radioGroup[i];
                                if (isChoiceCorrect)
                                    choiceVE?.AddToClassList("agent-choice-correct");
                                else if (wasUserPick)
                                    choiceVE?.AddToClassList("agent-choice-incorrect");
                            }
                        }
                    }
                }

                // 容器整体添加 correct/incorrect 边框
                ve.AddToClassList(isCorrect ? "agent-choice-correct" : "agent-choice-incorrect");
            });
        }
    }
}
