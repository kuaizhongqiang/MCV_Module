using System.Collections;
using MCV_Module.Controller;
using MCV_Module.Managers;
using MCV_Module.Net;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Controllers
{
    /// <summary>
    /// AI 对话控制器 —— 面板与 GlobalAiMgr 之间的调度层。
    ///
    /// 职责:
    ///   - 订阅面板 OnSendRequested 事件(先清后加, 面板每次重建会重新绑定)
    ///   - 发送 -> 用户气泡 + 助手气泡(流式: 思考/正文逐段回填)
    ///   - 忙碌态控制、错误提示、AiServer 未就绪自动重试
    ///
    /// 注意: Controller 常驻(ControllerRoot), 跨场景不销毁; 面板每次初始化都会重新 Bind。
    /// </summary>
    public class AiDialogController : ControllerBase<AiDialogPanel>
    {
        /// <summary>单次对话最长重试次数(AiServer 未就绪时)</summary>
        const int MAX_RETRY = 3;

        /// <summary>是否有请求进行中</summary>
        bool busy;

        protected override void OnViewBound()
        {
            // 先清后加, 避免面板重建后重复订阅
            View.OnSendRequested -= HandleSend;
            View.OnSendRequested += HandleSend;

            View.SetInputInteractable(!busy);
            if (!View.HasMessage)
            {
                View.AddSystemMessage("你好，我是电路实验助手。可以问我电路原理、实验步骤或接线问题。");
            }
        }

        protected override void OnDestroy()
        {
            if (View != null)
            {
                View.OnSendRequested -= HandleSend;
            }
            base.OnDestroy();
        }

        // ───────────────────────── 发送流程 ─────────────────────────

        void HandleSend(string userText)
        {
            if (busy)
            {
                View.SetInfoText("正在思考中，请稍候…");
                return;
            }

            View.AddUserMessage(userText);
            View.BeginAssistantReply();
            View.SetInfoText("");
            View.SetInputInteractable(false);
            busy = true;

            StartCoroutine(RunChat(userText, MAX_RETRY));
        }

        /// <summary>
        /// 执行一次对话。AiServer 未就绪时最多重试 MAX_RETRY 次(每次间隔 1 秒)。
        /// </summary>
        IEnumerator RunChat(string userText, int retriesLeft)
        {
            yield return GlobalAiMgr.Instance.AskStream(userText,
                onDelta: chunk =>
                {
                    if (View == null) return;
                    if (chunk.HasReasoning)
                        View.AppendAssistantReasoning(chunk.choices[0].delta.reasoningContent);
                    if (chunk.HasContent)
                        View.AppendAssistantContent(chunk.choices[0].delta.content);
                },
                onDone: result =>
                {
                    if (View == null) return;
                    Finish(result.success, result.success ? "" : result.error);
                },
                onError: error =>
                {
                    if (View == null) return;
                    if (error != null && error.Contains("未就绪") && retriesLeft > 0)
                    {
                        // 服务还在启动, 提示后稍等重试
                        View.SetInfoText($"正在连接 AiServer…(剩余重试 {retriesLeft})");
                        StartCoroutine(RetryAfterDelay(userText, retriesLeft - 1));
                    }
                    else
                    {
                        Finish(false, error);
                    }
                });
        }

        IEnumerator RetryAfterDelay(string userText, int retriesLeft)
        {
            yield return new WaitForSeconds(1f);
            yield return RunChat(userText, retriesLeft);
        }

        /// <summary>收尾: 恢复输入, 展示结果/错误; 失败时把 AiServer 日志尾部打到 Unity Console 便于定位</summary>
        void Finish(bool success, string message)
        {
            busy = false;
            View.SetInputInteractable(true);
            View.SelectInput();

            if (success)
            {
                View.SetInfoText("");
            }
            else
            {
                View.SetInfoText("出错了：" + message);
                Debug.LogError($"[AiDialog] AI 请求失败: {message}");
                StartCoroutine(DumpServerLogs());
            }
        }

        /// <summary>拉取 AiServer 最近日志并打到 Unity Console(不黑箱, 快速定位 py 侧问题)</summary>
        IEnumerator DumpServerLogs()
        {
            yield return GlobalAiMgr.Instance.FetchServerLogsAsync(15, text =>
            {
                if (!string.IsNullOrEmpty(text))
                    Debug.LogError("[AiDialog] AiServer 最近日志:\n" + text);
            });
        }
    }
}
