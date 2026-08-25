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
    /// 分层原则(Unity 是纯前台):
    ///   - Unity 只负责: 显示气泡、接收用户输入、把 user_text 交给 GlobalAiMgr。
    ///   - 上下文拼接/历史/token 截断/预热 全部在 AiServer(EXE), Unity 不做任何拼接。
    ///
    /// 职责:
    ///   - 订阅面板 OnSendRequested 事件(先清后加, 面板每次重建会重新绑定)
    ///   - 发送 -> 用户气泡 + 助手气泡(流式: 思考/正文逐段回填)
    ///   - 忙碌态控制、错误提示、AiServer 未就绪自动重试
    ///   - 【预热门控】GlobalAiMgr.IsWarmupDone 完成前禁止用户输入。
    ///
    /// 注意: Controller 常驻(ControllerRoot), 跨场景不销毁; 面板每次初始化都会重新 Bind。
    /// </summary>
    public class AiDialogController : ControllerBase<AiDialogPanel>
    {
        /// <summary>单次对话最长重试次数(AiServer 未就绪时)</summary>
        const int MAX_RETRY = 3;

        /// <summary>是否有请求进行中</summary>
        bool busy;

        /// <summary>等待预热完成的协程引用(防止重复启动)</summary>
        Coroutine warmupWaitCoroutine;

        protected override void OnViewBound()
        {
            // 先清后加, 避免面板重建后重复订阅
            View.OnSendRequested -= HandleSend;
            View.OnSendRequested += HandleSend;

            // 预热门控: 预热完成前禁止输入
            if (!IsWarmupDone())
            {
                View.SetInputInteractable(false);
                View.SetInfoText("系统初始化中，请稍候…");
                StartWaitWarmupOnce();
            }
            else
            {
                View.SetInputInteractable(!busy);
            }

            if (!View.HasMessage)
            {
                View.AddSystemMessage("你好，我是你的电路智能教师。可以问我电路原理、实验步骤或接线问题。");
            }
        }

        protected override void OnDestroy()
        {
            if (View != null)
            {
                View.OnSendRequested -= HandleSend;
            }
            if (warmupWaitCoroutine != null)
            {
                StopCoroutine(warmupWaitCoroutine);
                warmupWaitCoroutine = null;
            }
            base.OnDestroy();
        }

        // ───────────────────────── 预热门控 ─────────────────────────

        /// <summary>当前是否已允许用户输入(服务就绪 + 预热完成 + 非忙碌)。</summary>
        bool IsWarmupDone()
        {
            var mgr = GlobalAiMgr.Instance;
            return mgr != null && mgr.IsWarmupDone;
        }

        /// <summary>启动等待预热完成的协程(仅一次)。预热完成后恢复输入。</summary>
        void StartWaitWarmupOnce()
        {
            if (warmupWaitCoroutine != null) return;
            warmupWaitCoroutine = StartCoroutine(WaitWarmupAndEnableInput());
        }

        IEnumerator WaitWarmupAndEnableInput()
        {
            // 轮询等待预热完成(预热是异步后台进行)
            int guard = 0;
            while (!IsWarmupDone() && guard < 300) // 最多等 30 秒(0.1s 间隔)
            {
                yield return new WaitForSeconds(0.1f);
                guard++;
            }

            warmupWaitCoroutine = null;
            if (View == null) yield break;

            if (IsWarmupDone())
            {
                View.SetInfoText("");
                View.SetInputInteractable(!busy);
            }
            else
            {
                View.SetInfoText("AI 服务初始化失败，请稍后重试。");
                View.SetInputInteractable(false);
            }
        }

        // ───────────────────────── 发送流程 ─────────────────────────

        void HandleSend(string userText)
        {
            if (busy)
            {
                View.SetInfoText("正在思考中，请稍候…");
                return;
            }

            if (string.IsNullOrWhiteSpace(userText))
            {
                View.SetInfoText("请输入内容后再发送");
                return;
            }

            if (!IsWarmupDone())
            {
                View.SetInfoText("系统初始化中，请稍候…");
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
        /// 组装发给 AI 的最终用户消息 = 便携提示词(当前界面状态) + 用户输入。
        /// 便携提示词随每次发送动态注入, 让 AI 明确当前用户在哪个界面/任务。
        /// 兜底: 界面状态为空时退化为仅用户输入, 避免孤立分隔词。
        /// </summary>
        string BuildUserText(string userText)
        {
            string state = GlobalUIMgr.CurrentStateDescription();
            string text = userText ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }
            return string.IsNullOrWhiteSpace(state)
                ? text
                : $"{state} 用户输入内容为：{text}";
        }

        /// <summary>
        /// 执行一次对话。AiServer 未就绪时最多重试 MAX_RETRY 次(每次间隔 1 秒)。
        /// 只传 session_id + user_text 给 GlobalAiMgr, 历史拼接在 EXE。
        /// </summary>
        IEnumerator RunChat(string userText, int retriesLeft)
        {
            var mgr = GlobalAiMgr.Instance;
            if (mgr == null)
            {
                Finish(false, "AI 服务未初始化");
                yield break;
            }

            string finalString = BuildUserText(userText);
            var request = new AiChatRequest(mgr.SessionId, finalString, stream: true);

            yield return mgr.ChatAsync(request,
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
                    // 流式结束: 对累积的完整正文/思考一次性做 markdown 转换（核心转换时机）
                    View.FinalizeAssistantReply();
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
            View.SetInputInteractable(!busy);
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

        /// <summary>拉取 AiServer 最近日志并打到 Unity Console(不黑箱, 快速定位问题)</summary>
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
